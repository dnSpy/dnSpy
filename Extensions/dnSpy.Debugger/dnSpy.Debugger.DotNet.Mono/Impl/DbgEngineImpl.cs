/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Metadata.Internal;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.DotNet.Steppers.Engine;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.CallStack;
using dnSpy.Debugger.DotNet.Mono.Impl.Attach;
using dnSpy.Debugger.DotNet.Mono.Impl.Evaluation;
using dnSpy.Debugger.DotNet.Mono.Properties;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl : DbgEngine {
		static readonly TimeSpan defaultConnectionTimeout = TimeSpan.FromSeconds(10);
		static readonly TimeSpan maxConnectionTimeout = TimeSpan.FromMinutes(5);

		public override DbgStartKind StartKind => wasAttach ? DbgStartKind.Attach : DbgStartKind.Start;
		public override DbgEngineRuntimeInfo RuntimeInfo => runtimeInfo;
		public override string[] DebugTags => new[] { PredefinedDebugTags.DotNetDebugger };
		public override string[] Debugging { get; }
		public override event EventHandler<DbgEngineMessage>? Message;

		internal DbgObjectFactory ObjectFactory => objectFactory!;
		internal VirtualMachine MonoVirtualMachine => vm!;

		readonly object lockObj;
		readonly DebuggerThread debuggerThread;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory;
		readonly DbgEngineStepperFactory dbgEngineStepperFactory;
		readonly DbgManager dbgManager;
		readonly DbgModuleMemoryRefreshedNotifier2 dbgModuleMemoryRefreshedNotifier;
		DmdRuntime? dmdRuntime;
		readonly DmdDispatcherImpl dmdDispatcher;
		internal DbgRawMetadataService RawMetadataService { get; }
		readonly MonoDebugRuntimeKind monoDebugRuntimeKind;
		readonly DbgEngineRuntimeInfo runtimeInfo;
		readonly Dictionary<AppDomainMirror, DbgEngineAppDomain> toEngineAppDomain;
		readonly Dictionary<ModuleMirror, DbgEngineModule> toEngineModule;
		readonly Dictionary<ThreadMirror, DbgEngineThread> toEngineThread;
		readonly Dictionary<AssemblyMirror, List<ModuleMirror>> toAssemblyModules;
		readonly HashSet<AppDomainMirror> appDomainsThatHaveNotBeenInitializedYet;
		internal readonly StackFrameData stackFrameData;
		readonly List<DbgDotNetValueImpl> dotNetValuesToCloseOnContinue;
		readonly FuncEvalFactory funcEvalFactory;
		readonly List<Action> execOnPauseList;
		readonly Dictionary<StepEventRequest, StepperInfo> toStepper;
		readonly DotNetMonoRuntimeId monoRuntimeId;
		bool wasAttach;
		bool processWasRunningOnAttach;
		VirtualMachine? vm;
		int vmPid;
		int? vmDeathExitCode;
		volatile bool gotVMDisconnect;
		bool isUnhandledException;
		DbgObjectFactory? objectFactory;
		SafeHandle? hProcess_debuggee;
		volatile int suspendCount;
		readonly List<PendingMessage> pendingMessages;
		// The thrown exception. The mono debugger agent keeps it alive until Resume() is called,
		// but Unity uses a buggy debugger agent that doesn't keep it alive so it could get GC'd.
		ObjectMirror? thrownException;
		BreakOnEntryPointData? breakOnEntryPointData;
		ConsoleOutputReaderInfo? consoleStdOut;
		ConsoleOutputReaderInfo? consoleStdErr;

		sealed class BreakOnEntryPointData {
			public BreakpointEventRequest? Breakpoint;
			public string? Filename;
		}

		static DbgEngineImpl() => ThreadMirror.NativeTransitions = true;

		public DbgEngineImpl(DbgEngineImplDependencies deps, DbgManager dbgManager, MonoDebugRuntimeKind monoDebugRuntimeKind) {
			if (deps is null)
				throw new ArgumentNullException(nameof(deps));
			lockObj = new object();
			suspendCount = 0;
			pendingMessages = new List<PendingMessage>();
			toEngineAppDomain = new Dictionary<AppDomainMirror, DbgEngineAppDomain>();
			toEngineModule = new Dictionary<ModuleMirror, DbgEngineModule>();
			toEngineThread = new Dictionary<ThreadMirror, DbgEngineThread>();
			toAssemblyModules = new Dictionary<AssemblyMirror, List<ModuleMirror>>();
			appDomainsThatHaveNotBeenInitializedYet = new HashSet<AppDomainMirror>();
			stackFrameData = new StackFrameData();
			dotNetValuesToCloseOnContinue = new List<DbgDotNetValueImpl>();
			execOnPauseList = new List<Action>();
			toStepper = new Dictionary<StepEventRequest, StepperInfo>();
			debuggerSettings = deps.DebuggerSettings;
			dbgDotNetCodeLocationFactory = deps.DbgDotNetCodeLocationFactory;
			dbgEngineStepperFactory = deps.EngineStepperFactory;
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			dbgModuleMemoryRefreshedNotifier = deps.DbgModuleMemoryRefreshedNotifier;
			debuggerThread = new DebuggerThread("MonoDebug");
			debuggerThread.CallDispatcherRun();
			dmdDispatcher = new DmdDispatcherImpl(this);
			monoRuntimeId = new DotNetMonoRuntimeId();
			RawMetadataService = deps.RawMetadataService;
			this.monoDebugRuntimeKind = monoDebugRuntimeKind;
			if (monoDebugRuntimeKind == MonoDebugRuntimeKind.Mono) {
				Debugging = new[] { "MonoCLR" };
				runtimeInfo = new DbgEngineRuntimeInfo(PredefinedDbgRuntimeGuids.DotNetMono_Guid, PredefinedDbgRuntimeKindGuids.DotNet_Guid, "MonoCLR", monoRuntimeId, monoRuntimeTags);
			}
			else {
				Debug.Assert(monoDebugRuntimeKind == MonoDebugRuntimeKind.Unity);
				Debugging = new[] { "Unity" };
				runtimeInfo = new DbgEngineRuntimeInfo(PredefinedDbgRuntimeGuids.DotNetUnity_Guid, PredefinedDbgRuntimeKindGuids.DotNet_Guid, "Unity", monoRuntimeId, unityRuntimeTags);
			}
			funcEvalFactory = new FuncEvalFactory(debuggerThread.GetDebugMessageDispatcher());
		}
		static readonly ReadOnlyCollection<string> monoRuntimeTags = new ReadOnlyCollection<string>(new[] {
			PredefinedDotNetDbgRuntimeTags.DotNetBase,
			PredefinedDotNetDbgRuntimeTags.DotNetMono,
		});
		static readonly ReadOnlyCollection<string> unityRuntimeTags = new ReadOnlyCollection<string>(new[] {
			PredefinedDotNetDbgRuntimeTags.DotNetBase,
			PredefinedDotNetDbgRuntimeTags.DotNetUnity,
		});

		internal DebuggerThread DebuggerThread => debuggerThread;
		internal bool CheckMonoDebugThread() => debuggerThread.CheckAccess();
		internal void VerifyMonoDebugThread() => debuggerThread.VerifyAccess();
		internal T InvokeMonoDebugThread<T>(Func<T> callback) => debuggerThread.Invoke(callback);
		internal void MonoDebugThread(Action callback) => debuggerThread.BeginInvoke(callback);
		internal DbgRuntime DbgRuntime => objectFactory!.Runtime;

		internal DbgEngineMessageFlags GetMessageFlags(bool pause = false) {
			VerifyMonoDebugThread();
			var flags = DbgEngineMessageFlags.None;
			if (pause)
				flags |= DbgEngineMessageFlags.Pause;
			if (IsEvaluating)
				flags |= DbgEngineMessageFlags.Continue;
			return flags;
		}

		bool HasConnected_MonoDebugThread {
			get {
				debuggerThread.VerifyAccess();
				return vm is not null;
			}
		}

		abstract class PendingMessage {
			public abstract bool MustWaitForRun { get; }
			public abstract bool RequireResumeVM { get; }
			public abstract bool RaiseMessage();
		}
		sealed class NormalPendingMessage : PendingMessage {
			readonly DbgEngineImpl engine;
			readonly DbgEngineMessage message;
			public override bool MustWaitForRun { get; }
			public override bool RequireResumeVM => true;
			public NormalPendingMessage(DbgEngineImpl engine, bool mustWaitForRun, DbgEngineMessage message) {
				this.engine = engine;
				MustWaitForRun = mustWaitForRun;
				this.message = message;
			}
			public override bool RaiseMessage() {
				engine.Message?.Invoke(engine, message);
				return true;
			}
		}
		sealed class DelegatePendingMessage : PendingMessage {
			readonly Action? actionRaiseMessage;
			readonly Func<bool>? funcRaiseMessage;
			public override bool MustWaitForRun { get; }
			public override bool RequireResumeVM { get; }
			public DelegatePendingMessage(bool mustWaitForRun, Action raiseMessage) {
				MustWaitForRun = mustWaitForRun;
				RequireResumeVM = true;
				actionRaiseMessage = raiseMessage;
			}
			public DelegatePendingMessage(bool mustWaitForRun, bool requireResumeVM, Func<bool> raiseMessage) {
				MustWaitForRun = mustWaitForRun;
				RequireResumeVM = requireResumeVM;
				funcRaiseMessage = raiseMessage;
			}
			public override bool RaiseMessage() {
				if (funcRaiseMessage is not null)
					return funcRaiseMessage();
				actionRaiseMessage!();
				return true;
			}
		}

		void SendMessage(DbgEngineMessage message, bool mustWaitForRun = false) =>
			SendMessage(new NormalPendingMessage(this, mustWaitForRun, message));
		void SendMessage(PendingMessage message) {
			debuggerThread.VerifyAccess();
			pendingMessages.Add(message);
			SendNextMessage();
		}

		uint runCounter;
		uint nextSendRunCounter;
		bool SendNextMessage() {
			debuggerThread.VerifyAccess();
			if (gotVMDisconnect)
				return true;
			if (runCounter != nextSendRunCounter)
				return false;
			try {
				for (;;) {
					if (pendingMessages.Count == 0) {
						nextSendRunCounter = runCounter;
						return false;
					}
					var pendingMessage = pendingMessages[0];
					pendingMessages.RemoveAt(0);
					bool raisedMessage = pendingMessage.RaiseMessage();
					if (pendingMessage.MustWaitForRun) {
						if (raisedMessage) {
							nextSendRunCounter = runCounter + 1;
							return true;
						}
						else if (pendingMessage.RequireResumeVM) {
							if (!DecrementAndResume())
								break;
						}
					}
				}
			}
			catch (VMDisconnectedException) {
			}
			catch {
			}
			return true;
		}

		public override void Start(DebugProgramOptions options) => MonoDebugThread(() => StartCore(options));
		void StartCore(DebugProgramOptions options) {
			debuggerThread.VerifyAccess();
			string? defaultCouldNotConnectMessage = null;
			try {
				string? connectionAddress;
				ushort connectionPort;
				TimeSpan connectionTimeout;
				int expectedPid;
				string? filename;
				if (options is MonoStartDebuggingOptions startMonoOptions) {
					connectionAddress = "127.0.0.1";
					connectionPort = startMonoOptions.ConnectionPort;
					connectionTimeout = startMonoOptions.ConnectionTimeout;
					filename = startMonoOptions.Filename;
					if (string2.IsNullOrEmpty(filename))
						throw new Exception("Missing filename");
					Debug2.Assert(startMonoOptions.Filename is not null);
					if (connectionPort == 0) {
						int port = NetUtils.GetConnectionPort();
						Debug.Assert(port >= 0);
						if (port < 0)
							throw new Exception("All ports are in use");
						connectionPort = (ushort)port;
					}

					var monoExe = startMonoOptions.MonoExePath;
					if (string2.IsNullOrEmpty(monoExe))
						monoExe = MonoExeFinder.Find(startMonoOptions.MonoExeOptions);
					if (!File.Exists(monoExe))
						throw new StartException(string.Format(dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotFindFile, MonoExeFinder.MONO_EXE));
					Debug2.Assert(monoExe is not null);
					Debug.Assert(!connectionAddress.Contains(" "));
					var psi = new ProcessStartInfo {
						FileName = monoExe,
						Arguments = $"--debug --debugger-agent=transport=dt_socket,server=y,address={connectionAddress}:{connectionPort} \"{startMonoOptions.Filename}\" {startMonoOptions.CommandLine}",
						WorkingDirectory = startMonoOptions.WorkingDirectory ?? string.Empty,
						UseShellExecute = false,
					};
					if (debuggerSettings.RedirectGuiConsoleOutput && PortableExecutableFileHelpers.IsGuiApp(startMonoOptions.Filename)) {
						psi.RedirectStandardOutput = true;
						psi.RedirectStandardError = true;
					}
					var env = new Dictionary<string, string>();
					foreach (var kv in startMonoOptions.Environment.Environment)
						psi.Environment[kv.Key] = kv.Value;
					using (var process = Process.Start(psi)!) {
						expectedPid = process.Id;
						ReadConsoleOutput(psi, process);
					}

					if (startMonoOptions.BreakKind == PredefinedBreakKinds.EntryPoint)
						breakOnEntryPointData = new BreakOnEntryPointData { Filename = Path.GetFullPath(startMonoOptions.Filename) };
				}
				else if (options is UnityStartDebuggingOptions startUnityOptions) {
					connectionAddress = "127.0.0.1";
					connectionPort = startUnityOptions.ConnectionPort;
					connectionTimeout = startUnityOptions.ConnectionTimeout;
					filename = startUnityOptions.Filename;
					if (string2.IsNullOrEmpty(filename))
						throw new Exception("Missing filename");
					if (connectionPort == 0) {
						int port = NetUtils.GetConnectionPort();
						Debug.Assert(port >= 0);
						if (port < 0)
							throw new Exception("All ports are in use");
						connectionPort = (ushort)port;
					}

					var psi = new ProcessStartInfo {
						FileName = startUnityOptions.Filename!,
						Arguments = startUnityOptions.CommandLine ?? string.Empty,
						WorkingDirectory = startUnityOptions.WorkingDirectory ?? string.Empty,
						UseShellExecute = false,
					};
					if (debuggerSettings.RedirectGuiConsoleOutput && PortableExecutableFileHelpers.IsGuiApp(startUnityOptions.Filename)) {
						psi.RedirectStandardOutput = true;
						psi.RedirectStandardError = true;
					}
					var env = new Dictionary<string, string>();
					foreach (var kv in startUnityOptions.Environment.Environment)
						psi.Environment[kv.Key] = kv.Value;

					// Which version is it? Who knows, set both env vars.
					const string ENV_VAR_NAME_V0 = "DNSPY_UNITY_DBG";
					const string ENV_VAR_NAME_V1 = "DNSPY_UNITY_DBG2";
					string envVarValue;
					Debug.Assert(!connectionAddress.Contains(" "));

					// Unity 4.x - 2018.x+ (.NET 2.0-3.5 assemblies)
					envVarValue = $"--debugger-agent=transport=dt_socket,server=y,address={connectionAddress}:{connectionPort},defer=y";
					if (!debuggerSettings.PreventManagedDebuggerDetection)
						envVarValue += ",no-hide-debugger";
					psi.Environment[ENV_VAR_NAME_V0] = envVarValue;

					// Unity 5.5+ versions (.NET 4.x assemblies)
					envVarValue = $"--debugger-agent=transport=dt_socket,server=y,address={connectionAddress}:{connectionPort},suspend=n";
					if (!debuggerSettings.PreventManagedDebuggerDetection)
						envVarValue += ",no-hide-debugger";
					psi.Environment[ENV_VAR_NAME_V1] = envVarValue;

					using (var process = Process.Start(psi)!) {
						expectedPid = process.Id;
						ReadConsoleOutput(psi, process);
					}

					defaultCouldNotConnectMessage = GetCouldNotConnectErrorMessage(connectionAddress, connectionPort, filename) + "\r\n\r\n" +
						dnSpy_Debugger_DotNet_Mono_Resources.CouldNotConnectToUnityGame_MakeSureMonoDllFileIsPatched;
				}
				else if (options is MonoConnectStartDebuggingOptionsBase connectOptions &&
					(connectOptions is MonoConnectStartDebuggingOptions || connectOptions is UnityConnectStartDebuggingOptions)) {
					connectionAddress = connectOptions.Address;
					if (string2.IsNullOrWhiteSpace(connectionAddress))
						connectionAddress = "127.0.0.1";
					connectionPort = connectOptions.Port;
					connectionTimeout = connectOptions.ConnectionTimeout;
					filename = null;
					expectedPid = -1;
					wasAttach = true;
					processWasRunningOnAttach = !connectOptions.ProcessIsSuspended;
				}
				else if (options is MonoAttachToProgramOptionsBase attachOptions) {
					connectionAddress = attachOptions.Address;
					if (string2.IsNullOrWhiteSpace(connectionAddress))
						connectionAddress = "127.0.0.1";
					connectionPort = attachOptions.Port;
					connectionTimeout = attachOptions.ConnectionTimeout;
					filename = null;
					expectedPid = -1;
					wasAttach = true;
				}
				else {
					// No need to localize it, should be unreachable
					throw new Exception("Invalid start options");
				}
				monoRuntimeId.Address = connectionAddress;
				monoRuntimeId.Port = connectionPort;

				if (connectionTimeout == TimeSpan.Zero)
					connectionTimeout = defaultConnectionTimeout;
				if (connectionTimeout > maxConnectionTimeout)
					connectionTimeout = maxConnectionTimeout;

				if (!IPAddress.TryParse(connectionAddress, out var ipAddr)) {
					ipAddr = Dns.GetHostEntry(connectionAddress).AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
					if (ipAddr is null)
						throw new StartException("Invalid IP address" + ": " + connectionAddress);
				}
				var endPoint = new IPEndPoint(ipAddr, connectionPort);

				var startTime = DateTime.UtcNow;
				for (;;) {
					var elapsedTime = DateTime.UtcNow - startTime;
					if (elapsedTime >= connectionTimeout)
						throw new CouldNotConnectException(GetCouldNotConnectErrorMessage(connectionAddress, connectionPort, filename));
					try {
						var cts = new CancellationTokenSource(connectionTimeout - elapsedTime);
						var asyncConn = VirtualMachineManager.ConnectAsync(endPoint, null, cts.Token);
						if (!asyncConn.Wait(connectionTimeout - elapsedTime))
							throw new CouldNotConnectException(GetCouldNotConnectErrorMessage(connectionAddress, connectionPort, filename));
						vm = asyncConn.Result;
						break;
					}
					catch (SocketException sex) when (sex.SocketErrorCode == SocketError.ConnectionRefused) {
						// Retry it in case it takes a while for mono.exe to initialize or if it hasn't started yet
					}
					Thread.Sleep(100);
				}

				var ep = (IPEndPoint)vm.EndPoint;
				var pid = NetUtils.GetProcessIdOfListener(ep.Address.MapToIPv4().GetAddressBytes(), (ushort)ep.Port);
				Debug.Assert(expectedPid == -1 || expectedPid == pid);
				if (pid is null)
					throw new StartException(dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotFindDebuggedProcess);
				vmPid = pid.Value;

				hProcess_debuggee = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)vmPid);

				var eventThread = new Thread(MonoEventThread);
				eventThread.IsBackground = true;
				eventThread.Name = "MonoDebugEvent";
				eventThread.Start();
			}
			catch (Exception ex) {
				try {
					vm?.Detach();
				}
				catch { }
				vm = null!;

				string msg;
				if (ex is CouldNotConnectException)
					msg = defaultCouldNotConnectMessage ?? ex.Message;
				else if (ex is OperationCanceledException)
					msg = dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotConnectToProcess;
				else if (ex is StartException)
					msg = ex.Message;
				else {
					msg = defaultCouldNotConnectMessage ??
						dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotConnectToProcess + "\r\n\r\n" + ex.Message;
				}
				SendMessage(new DbgMessageConnected(msg, GetMessageFlags()));
				return;
			}
		}
		ExceptionEventRequest? uncaughtRequest;
		ExceptionEventRequest? caughtRequest;
		MethodEntryEventRequest? methodEntryEventRequest;

		sealed class ConsoleOutputReaderInfo {
			readonly StreamReader streamReader;
			readonly char[] buffer;
			Task<int>? task;
			const int bufferSize = 0x200;

			public ConsoleOutputReaderInfo(StreamReader streamReader) {
				this.streamReader = streamReader;
				buffer = new char[bufferSize];
			}

			public Task<int> Read() {
				if (task is null)
					task = streamReader.ReadAsync(buffer, 0, buffer.Length);
				return task;
			}

			public string? TryGetString() {
				var t = task;
				task = null;
				int length = t!.GetAwaiter().GetResult();
				return length == 0 ? null : new string(buffer, 0, length);
			}

			public void Dispose() => streamReader.Dispose();
		}

		void ReadConsoleOutput(ProcessStartInfo psi, Process process) {
			if (!psi.RedirectStandardOutput && !psi.RedirectStandardError)
				return;
			consoleStdOut = new ConsoleOutputReaderInfo(process.StandardOutput);
			consoleStdErr = new ConsoleOutputReaderInfo(process.StandardError);
		}

		async void ReadConsoleOutputAsync() {
			var waitTasks = new Task[2];
			var consoleStdOut = this.consoleStdOut;
			var consoleStdErr = this.consoleStdErr;
			for (;;) {
				if (gotVMDisconnect)
					return;
				var outputTask = consoleStdOut!.Read();
				var errorTask = consoleStdErr!.Read();
				waitTasks[0] = outputTask;
				waitTasks[1] = errorTask;
				Debug.Assert(waitTasks.Length == 2);
				var task = await Task.WhenAny(waitTasks);
				if (gotVMDisconnect)
					return;
				ConsoleOutputReaderInfo pipe;
				if (task == outputTask)
					pipe = consoleStdOut;
				else if (task == errorTask)
					pipe = consoleStdErr;
				else
					throw new InvalidOperationException();
				var text = pipe.TryGetString();
				if (text is null)
					return;

				var source = task == outputTask ? AsyncProgramMessageSource.StandardOutput : AsyncProgramMessageSource.StandardError;
				SendMessage(new DbgMessageAsyncProgramMessage(source, text));
			}
		}

		void MonoEventThread() {
			var vm = this.vm;
			Debug2.Assert(vm is not null);
			if (vm is null)
				throw new InvalidOperationException();
			for (;;) {
				try {
					var eventSet = vm.GetNextEventSet();
					bool start;
					lock (pendingEventSets) {
						start = pendingEventSets.Count == 0;
						pendingEventSets.Add(eventSet);
					}
					if (start)
						MonoDebugThread(() => OnDebuggerEvents());
					foreach (var evt in eventSet.Events) {
						if (evt.EventType == EventType.VMDisconnect)
							return;
					}
				}
				catch (Exception ex) {
					Debug.Fail(ex.ToString());
					dbgManager.ShowError("Sorry, I crashed, but don't blame me, I'm innocent\n\n" + ex.GetType().FullName + "\n\n" + ex.ToString());
					try {
						vm.Detach();
					}
					catch { }
					Message?.Invoke(this, new DbgMessageDisconnected(-1, DbgEngineMessageFlags.None));
					return;
				}
			}
		}
		readonly List<EventSet> pendingEventSets = new List<EventSet>();

		void IncrementSuspendCount() {
			debuggerThread.VerifyAccess();
			suspendCount++;
			if (suspendCount == 1) {
				UpdateThreadProperties_MonoDebug();
				InitializeObjectConstants_MonoDebug();
				RunExecOnPauseDelegates_MonoDebug();
			}
		}

		void DecrementSuspendCount() {
			debuggerThread.VerifyAccess();
			Debug.Assert(suspendCount > 0);
			suspendCount--;
		}

		bool DecrementAndResume() {
			debuggerThread.VerifyAccess();
			try {
				ResumeVirtualMachine();
				DecrementSuspendCount();
				return true;
			}
			catch (VMDisconnectedException) {
			}
			return false;
		}

		void ResumeVirtualMachine() {
			debuggerThread.VerifyAccess();
			try {
				vm!.Resume();
			}
			catch (VMNotSuspendedException) {
			}
		}

		bool IncrementAndSuspend() {
			debuggerThread.VerifyAccess();
			try {
				vm!.Suspend();
				IncrementSuspendCount();
				return true;
			}
			catch (VMDisconnectedException) {
			}
			return false;
		}

		void EnableEvent(EventType evt, SuspendPolicy suspendPolicy) => vm!.EnableEvents(new[] { evt }, suspendPolicy);

		void InitializeVirtualMachine() {
			try {
				EnableEvent(EventType.AppDomainCreate, SuspendPolicy.All);
				EnableEvent(EventType.AppDomainUnload, SuspendPolicy.All);
				EnableEvent(EventType.AssemblyLoad, SuspendPolicy.All);
				EnableEvent(EventType.AssemblyUnload, SuspendPolicy.All);
				EnableEvent(EventType.ThreadStart, SuspendPolicy.All);
				EnableEvent(EventType.TypeLoad, SuspendPolicy.All);
				EnableEvent(EventType.ThreadDeath, SuspendPolicy.None);

				if (vm!.Version.AtLeast(2, 5)) {
					EnableEvent(EventType.UserLog, SuspendPolicy.All);
					if (!debuggerSettings.IgnoreBreakInstructions)
						EnableEvent(EventType.UserBreak, SuspendPolicy.All);
				}

				if (vm.Version.AtLeast(2, 1)) {
					uncaughtRequest = vm.CreateExceptionRequest(null, false, true);
					caughtRequest = vm.CreateExceptionRequest(null, true, false);
				}
				else
					caughtRequest = vm.CreateExceptionRequest(null, true, true);
				uncaughtRequest?.Enable();
				caughtRequest?.Enable();
				if (processWasRunningOnAttach)
					canInitializeObjectConstants = true;
				else {
					methodEntryEventRequest = vm.CreateMethodEntryRequest();
					methodEntryEventRequest.Enable();
				}
			}
			catch (VMDisconnectedException) {
			}
		}

		void OnDebuggerEvents() {
			for (;;) {
				EventSet? eventSet = null;
				EventSet[]? eventSets = null;
				lock (pendingEventSets) {
					if (pendingEventSets.Count == 0)
						return;
					if (pendingEventSets.Count == 1)
						eventSet = pendingEventSets[0];
					else
						eventSets = pendingEventSets.ToArray();
					pendingEventSets.Clear();
				}
				if (eventSet is not null)
					OnDebuggerEvents(eventSet);
				else {
					foreach (var e in eventSets!)
						OnDebuggerEvents(e);
				}
			}
		}

		void OnDebuggerEvents(EventSet eventSet) {
			try {
				eventHandlerRecursionCounter++;
				OnDebuggerEventsCore(eventSet);
			}
			catch (SocketException) {
			}
			catch (ObjectDisposedException) {
			}
			catch (VMDisconnectedException) {
			}
			finally {
				eventHandlerRecursionCounter--;
			}
		}
		int eventHandlerRecursionCounter;

		SuspendPolicy GetSuspendPolicy(EventSet eventSet) {
			var spolicy = eventSet.SuspendPolicy;
			// If it's the old debugger agent used by Unity, we can trust it
			if (monoDebugRuntimeKind == MonoDebugRuntimeKind.Unity)
				return spolicy;

			// The latest one sends one value but then possibly changes it and uses that value to
			// decide if it should suspend the process...
			foreach (var e in eventSet.Events) {
				switch (e.EventType) {
				case EventType.VMStart:
					spolicy = processWasRunningOnAttach ? SuspendPolicy.None : SuspendPolicy.All;
					break;

				case EventType.VMDeath:
					spolicy = SuspendPolicy.None;
					break;

				case EventType.ThreadStart:
				case EventType.ThreadDeath:
				case EventType.AppDomainCreate:
				case EventType.AppDomainUnload:
				case EventType.MethodEntry:
				case EventType.MethodExit:
				case EventType.AssemblyLoad:
				case EventType.AssemblyUnload:
				case EventType.Breakpoint:
				case EventType.Step:
				case EventType.TypeLoad:
				case EventType.Exception:
				case EventType.KeepAlive:
				case EventType.UserBreak:
				case EventType.UserLog:
				case EventType.VMDisconnect:
					break;

				default:
					Debug.Fail($"Unknown event {e.EventType}");
					break;
				}
			}
			return spolicy;
		}

		void OnDebuggerEventsCore(EventSet eventSet) {
			debuggerThread.VerifyAccess();

			Debug.Assert(!gotVMDisconnect);
			if (gotVMDisconnect)
				return;

			bool wasRunning = suspendCount == 0;
			var spolicy = GetSuspendPolicy(eventSet);
			if (spolicy == SuspendPolicy.All)
				IncrementSuspendCount();

			int exitCode;
			int suspCounter = 0;
			for (int i = 0; i < eventSet.Events.Length; i++) {
				var evt = eventSet.Events[i];
				SuspendPolicy expectedSuspendPolicy;
				switch (evt.EventType) {
				case EventType.VMStart:
					expectedSuspendPolicy = SuspendPolicy.All;
					processWasRunningOnAttach = spolicy == SuspendPolicy.None;
					SendMessage(new DbgMessageConnected(vmPid, GetMessageFlags()));
					break;

				case EventType.VMDeath:
					expectedSuspendPolicy = SuspendPolicy.None;
					var vmde = (VMDeathEvent)evt;
					if (vmDeathExitCode is not null)
						break;
					if (vm!.Version.AtLeast(2, 27))
						vmDeathExitCode = vmde.ExitCode;
					else if (TryGetProcessExitCode(out exitCode))
						vmDeathExitCode = exitCode;
					else
						vmDeathExitCode = 0;
					break;

				case EventType.ThreadStart:
					expectedSuspendPolicy = SuspendPolicy.All;
					var tse = (ThreadStartEvent)evt;
					SendMessage(new DelegatePendingMessage(true, false, () => InitializeDomain(tse.Thread.Domain)));
					SendMessage(new DelegatePendingMessage(true, true, () => CreateThread(tse.Thread)));
					break;

				case EventType.ThreadDeath:
					expectedSuspendPolicy = SuspendPolicy.None;
					var tde = (ThreadDeathEvent)evt;
					SendMessage(new DelegatePendingMessage(false, () => DestroyThread(TryGetThreadMirror(tde))));
					break;

				case EventType.AppDomainCreate:
					expectedSuspendPolicy = SuspendPolicy.All;
					var adce = (AppDomainCreateEvent)evt;
					SendMessage(new DelegatePendingMessage(true, true, () => CreateAppDomain(adce.Domain, isNewAppDomainEvent: true)));
					break;

				case EventType.AppDomainUnload:
					expectedSuspendPolicy = SuspendPolicy.All;
					var adue = (AppDomainUnloadEvent)evt;
					SendMessage(new DelegatePendingMessage(true, () => DestroyAppDomain(adue.Domain)));
					break;

				case EventType.MethodEntry:
					expectedSuspendPolicy = SuspendPolicy.None;
					Debug.Assert(evt.TryGetRequest() == methodEntryEventRequest);
					if (methodEntryEventRequest is not null && evt.TryGetRequest() == methodEntryEventRequest) {
						methodEntryEventRequest.Disable();
						methodEntryEventRequest = null;
						// Func-eval doesn't work at first assembly load event for some reason. Should work now though.
						canInitializeObjectConstants = true;
						InitializeObjectConstants_MonoDebug();
					}
					break;

				case EventType.AssemblyLoad:
					expectedSuspendPolicy = SuspendPolicy.All;
					var ale = (AssemblyLoadEvent)evt;
					SendMessage(new DelegatePendingMessage(true, false, () => InitializeDomain(ale.Assembly.Domain)));
					// The debugger agent doesn't support netmodules...
					SendMessage(new DelegatePendingMessage(true, true, () => CreateModule(ale.Assembly.ManifestModule)));
					break;

				case EventType.AssemblyUnload:
					expectedSuspendPolicy = SuspendPolicy.All;
					var aue = (AssemblyUnloadEvent)evt;
					var monoModule = TryGetModuleCore_NoCreate(aue.Assembly.ManifestModule);
					if (monoModule is null)
						expectedSuspendPolicy = SuspendPolicy.None;
					else {
						foreach (var module in GetAssemblyModules(monoModule)) {
							if (!TryGetModuleData(module, out var data))
								continue;
							var tmp = data.MonoModule;
							SendMessage(new DelegatePendingMessage(true, () => DestroyModule(tmp)));
						}
					}
					break;

				case EventType.Breakpoint:
					expectedSuspendPolicy = SuspendPolicy.All;
					var be = (BreakpointEvent)evt;
					var bpReq = be.TryGetRequest() as BreakpointEventRequest;
					if (bpReq is not null) {
						if (breakOnEntryPointData?.Breakpoint == bpReq) {
							bpReq.Disable();
							breakOnEntryPointData = null;
							SendMessage(new DbgMessageEntryPointBreak(TryGetThread(be.Thread), GetMessageFlags()));
						}
						else {
							if (!SendCodeBreakpointHitMessage_MonoDebug(bpReq, TryGetThread(be.Thread)))
								expectedSuspendPolicy = SuspendPolicy.None;
						}
					}
					else {
						// It's a removed BP. Repro: Step and stop on a BP, remove the BP, step again
						expectedSuspendPolicy = SuspendPolicy.None;
					}
					break;

				case EventType.Step:
					var se = (StepEvent)evt;
					if (OnStep(se.TryGetRequest() as StepEventRequest))
						expectedSuspendPolicy = SuspendPolicy.All;
					else
						expectedSuspendPolicy = SuspendPolicy.None;
					break;

				case EventType.TypeLoad:
					expectedSuspendPolicy = SuspendPolicy.None;
					var tle = (TypeLoadEvent)evt;

					// Add it to the cache
					var reflectionAppDomain = TryGetEngineAppDomain(tle.Type.Assembly.Domain)?.AppDomain.GetReflectionAppDomain();
					if (reflectionAppDomain is not null) {
						try {
							GetReflectionType(reflectionAppDomain, tle.Type, null);
						}
						catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
						}
					}

					InitializeBreakpoints(tle.Type);
					break;

				case EventType.Exception:
					var ee = (ExceptionEvent)evt;
					// Unhandled exceptions in Unity seems to be ignored by Unity. Report them to the user but
					// don't set isUnhandledException to true since we won't be able to func-eval.
					if (ee.TryGetRequest() == uncaughtRequest && monoDebugRuntimeKind == MonoDebugRuntimeKind.Mono)
						isUnhandledException = true;
					if (IsEvaluating && !isUnhandledException) {
						expectedSuspendPolicy = SuspendPolicy.None;
						break;
					}
					expectedSuspendPolicy = SuspendPolicy.All;
					thrownException = ee.Exception;
					SendMessage(new DelegatePendingMessage(true, () => {
						var req = ee.TryGetRequest() as ExceptionEventRequest;
						DbgExceptionEventFlags exFlags;
						if (req == caughtRequest)
							exFlags = DbgExceptionEventFlags.FirstChance;
						else if (req == uncaughtRequest)
							exFlags = DbgExceptionEventFlags.SecondChance | DbgExceptionEventFlags.Unhandled;
						else {
							Debug.Fail("Unknown exception request");
							exFlags = DbgExceptionEventFlags.FirstChance;
						}
						var exObj = ee.Exception;
						objectFactory!.CreateException(new DbgExceptionId(PredefinedExceptionCategories.DotNet, TryGetExceptionName(exObj) ?? "???"), exFlags, EvalReflectionUtils.TryGetExceptionMessage(exObj), TryGetThread(ee.Thread), TryGetModule(ee.Thread), GetMessageFlags());
					}));
					break;

				case EventType.UserBreak:
					expectedSuspendPolicy = SuspendPolicy.All;
					var ube = (UserBreakEvent)evt;
					SendMessage(new DbgMessageBreak(TryGetThread(ube.Thread), GetMessageFlags()));
					break;

				case EventType.UserLog:
					expectedSuspendPolicy = SuspendPolicy.All;
					var ule = (UserLogEvent)evt;
					SendMessage(new NormalPendingMessage(this, true, new DbgMessageProgramMessage(ule.Message, TryGetThread(ule.Thread), GetMessageFlags())));
					break;

				case EventType.VMDisconnect:
					expectedSuspendPolicy = SuspendPolicy.None;
					if (vmDeathExitCode is null && TryGetProcessExitCode(out exitCode))
						vmDeathExitCode = exitCode;
					if (vmDeathExitCode is null) {
						vmDeathExitCode = -1;
						dbgManager.ShowError(dnSpy_Debugger_DotNet_Mono_Resources.Error_ConnectionWasUnexpectedlyClosed);
					}
					Message?.Invoke(this, new DbgMessageDisconnected(vmDeathExitCode.Value, GetMessageFlags()));
					gotVMDisconnect = true;
					consoleStdOut?.Dispose();
					consoleStdErr?.Dispose();
					consoleStdOut = null;
					consoleStdErr = null;
					break;

				default:
					expectedSuspendPolicy = SuspendPolicy.None;
					Debug.Fail($"Unknown event type: {evt.EventType}");
					break;
				}

				// If it's the first iteration, don't suspend it if it must be suspended since
				// it was suspended by the debugger agent.
				int suspDir;
				if (expectedSuspendPolicy == SuspendPolicy.All && spolicy == SuspendPolicy.All)
					suspDir = i == 0 ? 0 : 1;
				else if (expectedSuspendPolicy == SuspendPolicy.All && spolicy == SuspendPolicy.None)
					suspDir = 1;
				else if (expectedSuspendPolicy == SuspendPolicy.None && spolicy == SuspendPolicy.All)
					suspDir = i == 0 ? -1 : 0;
				else if (expectedSuspendPolicy == SuspendPolicy.None && spolicy == SuspendPolicy.None)
					suspDir = 0;
				else {
					Debug.Fail("Shouldn't be here");
					suspDir = 0;
				}
				suspCounter += suspDir;
			}

			if (suspCounter < 0) {
				Debug.Assert(suspCounter == -1);
				while (suspCounter++ < 0) {
					if (!DecrementAndResume())
						break;
				}
			}
			else if (suspCounter > 0) {
				while (suspCounter-- > 0) {
					if (!IncrementAndSuspend())
						break;
				}
			}
			if (wasRunning && pendingRunCore && eventHandlerRecursionCounter == 1) {
				pendingRunCore = false;
				RunCore();
			}
		}
		bool pendingRunCore;

		ThreadMirror? TryGetThreadMirror(ThreadDeathEvent tde2) {
			try {
				return tde2.Thread;
			}
			catch (ObjectCollectedException) {
				Debug.Assert(!vm!.Version.AtLeast(2, 2));
				return null;
			}
		}

		bool TryGetProcessExitCode(out int exitCode) {
			if (!hProcess_debuggee!.IsClosed && !hProcess_debuggee.IsInvalid) {
				if (NativeMethods.GetExitCodeProcess(hProcess_debuggee.DangerousGetHandle(), out exitCode))
					return true;
			}

			exitCode = 0;
			return false;
		}

		DbgModule? TryGetModule(ThreadMirror thread) {
			var frames = thread.GetFrames();
			if (frames.Length == 0)
				return null;
			return TryGetModule(frames[0].Method?.DeclaringType.Module);
		}

		string? TryGetExceptionName(ObjectMirror exObj) {
			var reflectionAppDomain = TryGetEngineAppDomain(exObj.Domain)?.AppDomain.GetReflectionAppDomain();
			if (reflectionAppDomain is null)
				return exObj.Type.FullName;
			var type = GetReflectionType(reflectionAppDomain, exObj.Type, null);
			if (type.IsConstructedGenericType)
				type = type.GetGenericTypeDefinition();
			return type.FullName;
		}

		DbgEngineAppDomain? TryGetEngineAppDomain(AppDomainMirror monoAppDomain) {
			if (monoAppDomain is null)
				return null;
			DbgEngineAppDomain? engineAppDomain;
			bool b;
			lock (lockObj)
				b = toEngineAppDomain.TryGetValue(monoAppDomain, out engineAppDomain);
			if (!b) {
				//TODO: This sometimes fails
			}
			return engineAppDomain;
		}

		int GetAppDomainId(AppDomainMirror monoAppDomain) {
			debuggerThread.VerifyAccess();
			// We don't func-eval because of Unity func-eval crashes, just use an ID that's probably correct
			return nextAppDomainId++;
		}
		int nextAppDomainId = 1;

		bool InitializeDomain(AppDomainMirror monoAppDomain) {
			debuggerThread.VerifyAccess();
			DbgEngineAppDomain? engineAppDomain;
			bool b;
			lock (lockObj) {
				if (!appDomainsThatHaveNotBeenInitializedYet.Remove(monoAppDomain))
					return false;
				b = toEngineAppDomain.TryGetValue(monoAppDomain, out engineAppDomain);
			}
			Debug.Assert(b);
			if (b)
				engineAppDomain!.UpdateName(monoAppDomain.FriendlyName);
			return CreateModule(monoAppDomain.Corlib.ManifestModule);
		}

		bool CreateAppDomain(AppDomainMirror monoAppDomain, bool isNewAppDomainEvent) {
			debuggerThread.VerifyAccess();
			lock (lockObj) {
				if (toEngineAppDomain.ContainsKey(monoAppDomain))
					return false;
			}
			int appDomainId = GetAppDomainId(monoAppDomain);
			var appDomain = dmdRuntime!.CreateAppDomain(appDomainId);
			var internalAppDomain = new DbgMonoDebugInternalAppDomainImpl(appDomain, monoAppDomain);
			var appDomainName = monoAppDomain.FriendlyName;
			var engineAppDomain = objectFactory!.CreateAppDomain<object>(internalAppDomain, appDomainName, appDomainId, GetMessageFlags(), data: null, onCreated: engineAppDomain2 => internalAppDomain.SetAppDomain(engineAppDomain2.AppDomain));
			lock (lockObj) {
				if (isNewAppDomainEvent)
					appDomainsThatHaveNotBeenInitializedYet.Add(monoAppDomain);
				toEngineAppDomain.Add(monoAppDomain, engineAppDomain);
			}
			return true;
		}

		void DestroyAppDomain(AppDomainMirror monoAppDomain) {
			debuggerThread.VerifyAccess();
			DbgEngineAppDomain? engineAppDomain;
			lock (lockObj) {
				if (toEngineAppDomain.TryGetValue(monoAppDomain, out engineAppDomain)) {
					appDomainsThatHaveNotBeenInitializedYet.Remove(monoAppDomain);
					toEngineAppDomain.Remove(monoAppDomain);
					var appDomain = engineAppDomain.AppDomain;
					dmdRuntime!.Remove(((DbgMonoDebugInternalAppDomainImpl)appDomain.InternalAppDomain).ReflectionAppDomain);
					foreach (var kv in toEngineThread.ToArray()) {
						if (kv.Value.Thread.AppDomain == appDomain)
							toEngineThread.Remove(kv.Key);
					}
					foreach (var kv in toEngineModule.ToArray()) {
						if (kv.Value.Module.AppDomain == appDomain) {
							toEngineModule.Remove(kv.Key);
							kv.Value.Remove(GetMessageFlags());
						}
					}
				}
			}
			if (engineAppDomain is not null)
				engineAppDomain.Remove(GetMessageFlags());
		}

		sealed class DbgModuleData {
			public DbgEngineImpl Engine { get; }
			public ModuleMirror MonoModule { get; }
			public ModuleId ModuleId { get; set; }
			public DbgModuleData(DbgEngineImpl engine, ModuleMirror monoModule) {
				Engine = engine;
				MonoModule = monoModule;
			}
		}

		internal ModuleId GetModuleId(DbgModule module) {
			if (TryGetModuleData(module, out var data))
				return data.ModuleId;
			throw new InvalidOperationException();
		}

		internal static ModuleId? TryGetModuleId(DbgModule module) {
			if (module.TryGetData(out DbgModuleData? data))
				return data.ModuleId;
			return null;
		}

		bool TryGetModuleData(DbgModule module, [NotNullWhen(true)] out DbgModuleData? data) {
			if (module.TryGetData(out data) && data.Engine == this)
				return true;
			data = null;
			return false;
		}

		int moduleOrder;
		bool CreateModule(ModuleMirror monoModule) {
			debuggerThread.VerifyAccess();

			if (TryGetModuleCore_NoCreate(monoModule) is not null)
				return false;

			var appDomain = TryGetEngineAppDomain(monoModule.Assembly.Domain)?.AppDomain;
			if (appDomain is null)
				return false;

			var moduleData = new DbgModuleData(this, monoModule);
			var engineModule = ModuleCreator.CreateModule(this, objectFactory!, appDomain, monoModule, ++moduleOrder, moduleData);
			moduleData.ModuleId = ModuleIdUtils.Create(engineModule.Module, monoModule);
			lock (lockObj) {
				if (!toAssemblyModules.TryGetValue(monoModule.Assembly, out var modules))
					toAssemblyModules.Add(monoModule.Assembly, modules = new List<ModuleMirror>());
				modules.Add(monoModule);
				toEngineModule.Add(monoModule, engineModule);
			}

			if (breakOnEntryPointData is not null && breakOnEntryPointData.Breakpoint is null &&
				StringComparer.OrdinalIgnoreCase.Equals(breakOnEntryPointData.Filename, engineModule.Module.Filename)) {
				try {
					CreateEntryPointBreakpoint(monoModule.Assembly.EntryPoint);
				}
				catch (Exception ex) {
					Debug.Fail(ex.ToString());
				}
			}

			return true;
		}

		void CreateEntryPointBreakpoint(MethodMirror monoMethod) {
			if (monoMethod is null)
				return;
			breakOnEntryPointData!.Breakpoint = vm!.CreateBreakpointRequest(monoMethod, 0);
			breakOnEntryPointData.Breakpoint.Enable();
		}

		void DestroyModule(ModuleMirror monoModule) {
			debuggerThread.VerifyAccess();
			DbgEngineModule? engineModule;
			lock (lockObj) {
				if (toAssemblyModules.TryGetValue(monoModule.Assembly, out var modules)) {
					modules.Remove(monoModule);
					if (modules.Count == 0)
						toAssemblyModules.Remove(monoModule.Assembly);
				}
				if (toEngineModule.TryGetValue(monoModule, out engineModule)) {
					toEngineModule.Remove(monoModule);
					((DbgMonoDebugInternalModuleImpl)engineModule.Module.InternalModule).Remove();
				}
			}
			if (engineModule is not null)
				engineModule.Remove(GetMessageFlags());
		}

		internal DbgModule? TryGetModule(ModuleMirror? monoModule) {
			if (monoModule is null)
				return null;
			var res = TryGetModuleCore_NoCreate(monoModule);
			if (res is not null)
				return res;
			DiscoverNewModules(monoModule);
			res = TryGetModuleCore_NoCreate(monoModule);
			Debug2.Assert(res is not null);
			return res;
		}

		DbgModule? TryGetModuleCore_NoCreate(ModuleMirror monoModule) {
			if (monoModule is null)
				return null;
			lock (lockObj) {
				if (toEngineModule.TryGetValue(monoModule, out var engineModule))
					return engineModule.Module;
			}
			return null;
		}

		// The debugger agent doesn't send assembly load events for assemblies that have already been
		// loaded in some other AppDomain. This method discovers these assemblies. It should be called
		// when we've found a new module.
		void DiscoverNewModules(ModuleMirror monoModule) {
			debuggerThread.VerifyAccess();
			if (monoModule is not null) {
				Debug.Assert(monoModule.Assembly.ManifestModule == monoModule);
				AddNewModule(monoModule);
			}
			KeyValuePair<AppDomainMirror, DbgEngineAppDomain>[] appDomains;
			lock (lockObj)
				appDomains = toEngineAppDomain.ToArray();
			foreach (var kv in appDomains) {
				foreach (var monoAssembly in kv.Key.GetAssemblies())
					AddNewModule(monoAssembly.ManifestModule);
			}
		}

		void AddNewModule(ModuleMirror monoModule) {
			debuggerThread.VerifyAccess();
			if (TryGetModuleCore_NoCreate(monoModule) is not null)
				return;

			if (suspendCount == 0) {
				try {
					IncrementAndSuspend();
				}
				catch (Exception ex) {
					Debug.Fail(ex.Message);
					SendMessage(new DbgMessageBreak(ex.Message, GetMessageFlags()));
				}
				Debug.Assert(pendingMessages.Count == 0);
			}
			CreateModule(monoModule);
		}

		internal bool TryGetMonoModule(DbgModule module, [NotNullWhen(true)] out ModuleMirror? monoModule) {
			if (module.TryGetData(out DbgModuleData? data) && data.Engine == this) {
				monoModule = data.MonoModule;
				return true;
			}
			monoModule = null;
			return false;
		}

		DbgThread? GetThreadPreferMain_MonoDebug() {
			debuggerThread.VerifyAccess();
			DbgThread? firstThread = null;
			lock (lockObj) {
				foreach (var kv in toEngineThread) {
					var thread = TryGetThread(kv.Key);
					if (firstThread is null)
						firstThread = thread;
					if (thread?.IsMain == true)
						return thread;
				}
			}
			return firstThread;
		}

		internal DbgThread? TryGetThread(ThreadMirror thread) {
			if (thread is null)
				return null;
			DbgEngineThread? engineThread;
			lock (lockObj)
				toEngineThread.TryGetValue(thread, out engineThread);
			return engineThread?.Thread;
		}

		class StartException : Exception {
			public StartException(string message) : base(message) { }
		}
		sealed class CouldNotConnectException : StartException {
			public CouldNotConnectException(string message) : base(message) { }
		}

		static string GetCouldNotConnectErrorMessage(string address, ushort port, string? filenameOpt) {
			string extra = filenameOpt is null ? $" ({address}:{port})" : $" ({address}:{port} = {filenameOpt})";
			return dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotConnectToProcess + extra;
		}

		internal IDbgDotNetRuntime DotNetRuntime => internalRuntime!;
		DbgMonoDebugInternalRuntimeImpl? internalRuntime;
		public override DbgInternalRuntime CreateInternalRuntime(DbgRuntime runtime) {
			if (internalRuntime is not null)
				throw new InvalidOperationException();
			dmdRuntime = DmdRuntimeFactory.CreateRuntime(new DmdEvaluatorImpl(this), runtime.Process.PointerSize == 4 ? DmdImageFileMachine.I386 : DmdImageFileMachine.AMD64);
			return internalRuntime = new DbgMonoDebugInternalRuntimeImpl(this, runtime, dmdRuntime, monoDebugRuntimeKind);
		}

		sealed class RuntimeData {
			public DbgEngineImpl Engine { get; }
			public RuntimeData(DbgEngineImpl engine) => Engine = engine;
		}

		internal static DbgEngineImpl? TryGetEngine(DbgRuntime runtime) {
			if (runtime.TryGetData(out RuntimeData? data))
				return data.Engine;
			return null;
		}

		internal DbgModule[] GetAssemblyModules(DbgModule module) {
			if (!TryGetModuleData(module, out var data))
				return Array.Empty<DbgModule>();
			lock (lockObj) {
				toAssemblyModules.TryGetValue(data.MonoModule.Assembly, out var modules);
				if (modules is null || modules.Count == 0)
					return Array.Empty<DbgModule>();
				var res = new List<DbgModule>(modules.Count);
				foreach (var monoModule in modules) {
					if (toEngineModule.TryGetValue(monoModule, out var engineModule))
						res.Add(engineModule.Module);
				}
				return res.ToArray();
			}
		}

		public override void OnConnected(DbgObjectFactory objectFactory, DbgRuntime runtime) {
			Debug.Assert(objectFactory.Runtime == runtime);
			Debug.Assert(Array.IndexOf(objectFactory.Process.Runtimes, runtime) < 0);
			this.objectFactory = objectFactory;
			runtime.GetOrCreateData(() => new RuntimeData(this));

			MonoDebugThread(() => OnConnected_MonoDebug());
		}

		void OnConnected_MonoDebug() {
			debuggerThread.VerifyAccess();
			try {
				if (gotVMDisconnect)
					return;
				if (consoleStdOut is not null)
					ReadConsoleOutputAsync();
				Debug2.Assert(vm is not null);
				if (vm is not null) {
					InitializeVirtualMachine();
					// Create the root AppDomain now since we want it to get id=1, which isn't guaranteed
					// if it's an attach and we wait for AppDomainCreate events.
					SendMessage(new DelegatePendingMessage(true, false, () => CreateAppDomain(vm.RootDomain, isNewAppDomainEvent: !processWasRunningOnAttach)));
					// We need to add all threads even if it's an attach. Unity notifies us of all threads,
					// except it sends the same thread N times in a row (N = number of threads).
					foreach (var monoThread in vm.GetThreads()) {
						SendMessage(new DelegatePendingMessage(true, false, () => CreateAppDomain(monoThread.Domain, isNewAppDomainEvent: !processWasRunningOnAttach)));
						SendMessage(new DelegatePendingMessage(true, false, () => CreateModule(monoThread.Domain.Corlib.ManifestModule)));
						SendMessage(new DelegatePendingMessage(true, false, () => CreateThread(monoThread)));
					}
				}
			}
			catch (VMDisconnectedException) {
			}
		}

		public override void Break() => MonoDebugThread(BreakCore);
		void BreakCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_MonoDebugThread)
				return;
			try {
				bool sendMsg = true;
				if (suspendCount == 0)
					sendMsg = IncrementAndSuspend();
				if (sendMsg)
					SendMessage(new DbgMessageBreak(GetThreadPreferMain_MonoDebug(), GetMessageFlags()));
			}
			catch (Exception ex) {
				Debug.Fail(ex.Message);
				SendMessage(new DbgMessageBreak(ex.Message, GetMessageFlags()));
			}
		}

		public override void Run() => MonoDebugThread(RunCore);
		internal void RunCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_MonoDebugThread)
				return;
			try {
				continueCounter++;
				if (!IsEvaluating)
					CloseDotNetValues_MonoDebug();
				if (runCounter != nextSendRunCounter)
					runCounter++;
				if (SendNextMessage())
					return;
				if (IsEvaluating) {
					pendingRunCore = true;
					return;
				}
				ResumeCore();
			}
			catch (VMDisconnectedException) {
			}
			catch (Exception ex) {
				Debug.Fail(ex.Message);
				dbgManager.ShowError(ex.Message);
			}
		}
		internal uint ContinueCounter => continueCounter;
		volatile uint continueCounter;

		internal bool IsPaused => suspendCount > 0;
		void ResumeCore() {
			debuggerThread.VerifyAccess();
			while (suspendCount > 0) {
				thrownException = null;
				DecrementAndResume();
			}
		}

		public override void Terminate() => MonoDebugThread(TerminateCore);
		void TerminateCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_MonoDebugThread)
				return;
			try {
				// If we got an unhandled exception, the next event is VMDisconnect so just Resume() it
				if (isUnhandledException)
					ResumeCore();
				else
					vm!.Exit(0);
			}
			catch (VMDisconnectedException) {
			}
			catch (Exception ex) {
				Debug.Fail(ex.Message);
				dbgManager.ShowError(ex.Message);
			}
		}

		public override bool CanDetach => true;

		public override void Detach() => MonoDebugThread(DetachCore);
		void DetachCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_MonoDebugThread)
				return;
			try {
				vm!.Detach();
				vmDeathExitCode = -1;
			}
			catch (VMDisconnectedException) {
			}
			catch (Exception ex) {
				Debug.Fail(ex.Message);
				dbgManager.ShowError(ex.Message);
			}
		}

		internal DbgDotNetValue? TryGetExceptionValue() {
			debuggerThread.VerifyAccess();
			var exValue = thrownException;
			if (exValue is null)
				return null;
			var reflectionAppDomain = TryGetEngineAppDomain(exValue.Domain)?.AppDomain.GetReflectionAppDomain();
			if (reflectionAppDomain is null)
				return null;
			var exceptionType = GetReflectionType(reflectionAppDomain, exValue.Type, null);
			var valueLocation = new NoValueLocation(exceptionType, exValue);
			return CreateDotNetValue_MonoDebug(valueLocation);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			debuggerThread.Terminate();
			try {
				if (!gotVMDisconnect)
					vm?.Detach();
			}
			catch {
			}
			try {
				vm?.ForceDisconnect();
			}
			catch {
			}
			hProcess_debuggee?.Close();
			foreach (var kv in toStepper)
				kv.Value.OnStep(new StepCompleteEventArgs(kv.Key, true, false));
			toStepper.Clear();
		}

		readonly struct TempBreakHelper : IDisposable {
			readonly DbgEngineImpl engine;
			readonly bool pausedIt;

			public TempBreakHelper(DbgEngineImpl engine) {
				this.engine = engine;
				bool pausedIt = engine.suspendCount == 0;
				if (pausedIt)
					engine.vm!.Suspend();
				this.pausedIt = pausedIt;
			}

			public void Dispose() {
				if (pausedIt)
					engine.ResumeVirtualMachine();
			}
		}

		TempBreakHelper TempBreak() => new TempBreakHelper(this);

		internal DmdType GetReflectionType(DmdAppDomain reflectionAppDomain, TypeMirror monoType, DmdType? couldBeRealTypeOpt) {
			// Older debugger agents (eg. the one used by Unity) can't return the generic arguments, so we
			// can't create the correct instantiated generic type. We can't create a generic DmdType from a
			// TypeMirror. If we cache the generic DmdType, we'll be able to look it up later when we get
			// a generic TypeMirror.
			if (couldBeRealTypeOpt is not null && !vm!.Version.AtLeast(2, 15)) {
				try {
					MonoDebugTypeCreator.GetType(this, couldBeRealTypeOpt, null);
				}
				catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				}
			}

			return new ReflectionTypeCreator(this, reflectionAppDomain).Create(monoType);
		}
	}
}
