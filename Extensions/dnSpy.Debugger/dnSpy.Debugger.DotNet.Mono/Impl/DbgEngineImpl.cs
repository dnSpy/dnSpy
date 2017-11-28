/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Metadata.Internal;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Engine.Steppers;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.CallStack;
using dnSpy.Debugger.DotNet.Mono.Impl.Evaluation;
using dnSpy.Debugger.DotNet.Mono.Properties;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl : DbgEngine {
		const int DefaultConnectionTimeoutMilliseconds = 10 * 1000;

		public override DbgStartKind StartKind => DbgStartKind.Start;
		public override DbgEngineRuntimeInfo RuntimeInfo => runtimeInfo;
		public override string[] DebugTags => new[] { PredefinedDebugTags.DotNetDebugger };
		public override string[] Debugging { get; }
		public override event EventHandler<DbgEngineMessage> Message;

		internal DbgObjectFactory ObjectFactory => objectFactory;
		internal VirtualMachine MonoVirtualMachine => vm;

		readonly object lockObj;
		readonly DebuggerThread debuggerThread;
		readonly DbgDotNetCodeRangeService dbgDotNetCodeRangeService;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory;
		readonly DbgManager dbgManager;
		readonly DbgModuleMemoryRefreshedNotifier2 dbgModuleMemoryRefreshedNotifier;
		DmdRuntime dmdRuntime;
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
		VirtualMachine vm;
		int vmPid;
		int? vmDeathExitCode;
		bool gotVMDisconnect;
		bool isUnhandledException;
		DbgObjectFactory objectFactory;
		SafeHandle hProcess_debuggee;
		volatile int suspendCount;
		readonly List<PendingMessage> pendingMessages;
		// The thrown exception. The mono debugger agent keeps it alive until Resume() is called
		ObjectMirror thrownException;

		static DbgEngineImpl() => ThreadMirror.NativeTransitions = true;

		public DbgEngineImpl(DbgEngineImplDependencies deps, DbgManager dbgManager, MonoDebugRuntimeKind monoDebugRuntimeKind) {
			if (deps == null)
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
			debuggerSettings = deps.DebuggerSettings;
			dbgDotNetCodeRangeService = deps.DotNetCodeRangeService;
			dbgDotNetCodeLocationFactory = deps.DbgDotNetCodeLocationFactory;
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			dbgModuleMemoryRefreshedNotifier = deps.DbgModuleMemoryRefreshedNotifier;
			debuggerThread = new DebuggerThread("MonoDebug");
			debuggerThread.CallDispatcherRun();
			dmdDispatcher = new DmdDispatcherImpl(this);
			RawMetadataService = deps.RawMetadataService;
			this.monoDebugRuntimeKind = monoDebugRuntimeKind;
			if (monoDebugRuntimeKind == MonoDebugRuntimeKind.Mono) {
				Debugging = new[] { "MonoCLR" };
				runtimeInfo = new DbgEngineRuntimeInfo(PredefinedDbgRuntimeGuids.DotNetMono_Guid, PredefinedDbgRuntimeKindGuids.DotNet_Guid, "MonoCLR", new DotNetMonoRuntimeId(), monoRuntimeTags);
			}
			else {
				Debug.Assert(monoDebugRuntimeKind == MonoDebugRuntimeKind.Unity);
				Debugging = new[] { "Unity" };
				runtimeInfo = new DbgEngineRuntimeInfo(PredefinedDbgRuntimeGuids.DotNetUnity_Guid, PredefinedDbgRuntimeKindGuids.DotNet_Guid, "Unity", new DotNetMonoRuntimeId(), unityRuntimeTags);
			}
			funcEvalFactory = new FuncEvalFactory(debuggerThread.GetDebugMessageDispatcher());
		}
		static readonly ReadOnlyCollection<string> monoRuntimeTags = new ReadOnlyCollection<string>(new[] {
			PredefinedDotNetDbgRuntimeTags.DotNet,
			PredefinedDotNetDbgRuntimeTags.DotNetMono,
		});
		static readonly ReadOnlyCollection<string> unityRuntimeTags = new ReadOnlyCollection<string>(new[] {
			PredefinedDotNetDbgRuntimeTags.DotNet,
			PredefinedDotNetDbgRuntimeTags.DotNetUnity,
		});

		internal DebuggerThread DebuggerThread => debuggerThread;
		internal bool CheckMonoDebugThread() => debuggerThread.CheckAccess();
		internal void VerifyMonoDebugThread() => debuggerThread.VerifyAccess();
		internal T InvokeMonoDebugThread<T>(Func<T> callback) => debuggerThread.Invoke(callback);
		internal void MonoDebugThread(Action callback) => debuggerThread.BeginInvoke(callback);
		internal DbgRuntime DbgRuntime => objectFactory.Runtime;

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
				return vm != null;
			}
		}

		abstract class PendingMessage {
			public abstract bool MustWaitForRun { get; }
			public abstract bool RaiseMessage();
		}
		sealed class NormalPendingMessage : PendingMessage {
			readonly DbgEngineImpl engine;
			readonly DbgEngineMessage message;
			public override bool MustWaitForRun { get; }
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
			readonly Action actionRaiseMessage;
			readonly Func<bool> funcRaiseMessage;
			public override bool MustWaitForRun { get; }
			public DelegatePendingMessage(bool mustWaitForRun, Action raiseMessage) {
				MustWaitForRun = mustWaitForRun;
				actionRaiseMessage = raiseMessage;
			}
			public DelegatePendingMessage(bool mustWaitForRun, Func<bool> raiseMessage) {
				MustWaitForRun = mustWaitForRun;
				funcRaiseMessage = raiseMessage;
			}
			public override bool RaiseMessage() {
				if (funcRaiseMessage != null)
					return funcRaiseMessage();
				actionRaiseMessage();
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
					if (raisedMessage && pendingMessage.MustWaitForRun) {
						nextSendRunCounter = runCounter + 1;
						return true;
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
			try {
				string connectionAddress;
				ushort connectionPort;
				TimeSpan connectionTimeout;
				int expectedPid;
				string filename;
				if (options is MonoStartDebuggingOptions startOptions) {
					connectionAddress = "127.0.0.1";
					connectionPort = startOptions.ConnectionPort;
					connectionTimeout = startOptions.ConnectionTimeout;
					filename = startOptions.Filename;
					if (string.IsNullOrEmpty(filename))
						throw new Exception("Missing filename");
					if (connectionPort == 0) {
						int port = NetUtils.GetConnectionPort();
						Debug.Assert(port >= 0);
						if (port < 0)
							throw new Exception("All ports are in use");
						connectionPort = (ushort)port;
					}

					var monoExe = startOptions.MonoExePath;
					if (string.IsNullOrEmpty(monoExe))
						monoExe = MonoExeFinder.Find(startOptions.MonoExeOptions);
					if (!File.Exists(monoExe))
						throw new StartException(string.Format(dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotFindFile, MonoExeFinder.MONO_EXE));
					Debug.Assert(!connectionAddress.Contains(" "));
					var psi = new ProcessStartInfo {
						FileName = monoExe,
						Arguments = $"--debug --debugger-agent=transport=dt_socket,server=y,address={connectionAddress}:{connectionPort} \"{startOptions.Filename}\" {startOptions.CommandLine}",
						WorkingDirectory = startOptions.WorkingDirectory,
						UseShellExecute = false,
					};
					var env = new Dictionary<string, string>();
					foreach (var kv in startOptions.Environment.Environment)
						psi.Environment[kv.Key] = kv.Value;
					using (var process = Process.Start(psi))
						expectedPid = process.Id;
				}
				else if (options is MonoConnectStartDebuggingOptionsBase connectOptions &&
					(connectOptions is MonoConnectStartDebuggingOptions || connectOptions is UnityConnectStartDebuggingOptions)) {
					connectionAddress = connectOptions.Address;
					if (string.IsNullOrWhiteSpace(connectionAddress))
						connectionAddress = "127.0.0.1";
					connectionPort = connectOptions.Port;
					connectionTimeout = connectOptions.ConnectionTimeout;
					filename = null;
					expectedPid = -1;
				}
				else {
					// No need to localize it, should be unreachable
					throw new Exception("Invalid start options");
				}

				if (connectionTimeout == TimeSpan.Zero)
					connectionTimeout = TimeSpan.FromMilliseconds(DefaultConnectionTimeoutMilliseconds);

				if (!IPAddress.TryParse(connectionAddress, out var ipAddr)) {
					ipAddr = Dns.GetHostEntry(connectionAddress).AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
					if (ipAddr == null)
						throw new StartException("Invalid IP address" + ": " + connectionAddress);
				}
				var endPoint = new IPEndPoint(ipAddr, connectionPort);

				var startTime = DateTime.UtcNow;
				for (;;) {
					var elapsedTime = DateTime.UtcNow - startTime;
					if (elapsedTime >= connectionTimeout)
						throw new StartException(GetCouldNotConnectErrorMessage(connectionAddress, connectionPort, filename));
					try {
						var asyncConn = VirtualMachineManager.BeginConnect(endPoint, null);
						if (!asyncConn.AsyncWaitHandle.WaitOne(connectionTimeout - elapsedTime)) {
							VirtualMachineManager.CancelConnection(asyncConn);
							throw new StartException(GetCouldNotConnectErrorMessage(connectionAddress, connectionPort, filename));
						}
						else {
							vm = VirtualMachineManager.EndConnect(asyncConn);
							break;
						}
					}
					catch (SocketException sex) when (sex.SocketErrorCode == SocketError.ConnectionRefused) {
						// Retry it in case it takes a while for mono.exe to initialize or if it hasn't started yet
					}
					Thread.Sleep(100);
				}

				var ep = (IPEndPoint)vm.EndPoint;
				var pid = NetUtils.GetProcessIdOfListener(ep.Address.MapToIPv4().GetAddressBytes(), (ushort)ep.Port);
				Debug.Assert(expectedPid == -1 || expectedPid == pid);
				if (pid == null)
					throw new StartException(dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotFindDebuggedProcess);
				vmPid = pid.Value;

				hProcess_debuggee = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)vmPid);

				var events = new List<EventType> {
					EventType.VMStart,
					EventType.VMDeath,
					EventType.ThreadStart,
					EventType.ThreadDeath,
					EventType.AppDomainCreate,
					EventType.AppDomainUnload,
					EventType.AssemblyLoad,
					EventType.AssemblyUnload,
					EventType.UserLog,
				};
				if (!debuggerSettings.IgnoreBreakInstructions)
					events.Add(EventType.UserBreak);
				vm.EnableEvents(events.ToArray(), SuspendPolicy.All);
				uncaughtRequest = vm.CreateExceptionRequest(null, false, true);
				caughtRequest = vm.CreateExceptionRequest(null, true, false);
				uncaughtRequest.Enable();
				caughtRequest.Enable();
				methodEntryEventRequest = vm.CreateMethodEntryRequest();
				methodEntryEventRequest.Enable();

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
				vm = null;

				string msg;
				if (ex is StartException)
					msg = ex.Message;
				else
					msg = dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotConnectToProcess + "\r\n\r\n" + ex.Message;
				SendMessage(new DbgMessageConnected(msg, GetMessageFlags()));
				return;
			}
		}
		ExceptionEventRequest uncaughtRequest;
		ExceptionEventRequest caughtRequest;
		MethodEntryEventRequest methodEntryEventRequest;

		void MonoEventThread() {
			var vm = this.vm;
			Debug.Assert(vm != null);
			if (vm == null)
				throw new InvalidOperationException();
			for (;;) {
				try {
					var eventSet = vm.GetNextEventSet();
					MonoDebugThread(() => OnDebuggerEvents(eventSet));
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
			suspendCount--;
		}

		void OnDebuggerEvents(EventSet eventSet) {
			try {
				OnDebuggerEventsCore(eventSet);
			}
			catch (SocketException) {
			}
			catch (VMDisconnectedException) {
			}
		}

		void OnDebuggerEventsCore(EventSet eventSet) {
			debuggerThread.VerifyAccess();

			Debug.Assert(!gotVMDisconnect);
			if (gotVMDisconnect)
				return;

			int exitCode;
			foreach (var evt in eventSet.Events) {
				switch (evt.EventType) {
				case EventType.VMStart:
					IncrementSuspendCount();
					SendMessage(new DbgMessageConnected((uint)vmPid, GetMessageFlags()));
					break;

				case EventType.VMDeath:
					var vmde = (VMDeathEvent)evt;
					Debug.Assert(vmDeathExitCode == null);
					if (vm.Version.AtLeast(2, 27))
						vmDeathExitCode = vmde.ExitCode;
					else if (TryGetProcessExitCode(out exitCode))
						vmDeathExitCode = exitCode;
					else
						vmDeathExitCode = 0;
					break;

				case EventType.ThreadStart:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					var tse = (ThreadStartEvent)evt;
					InitializeDomain(tse.Thread.Domain);
					SendMessage(new DelegatePendingMessage(true, () => CreateThread(tse.Thread)));
					break;

				case EventType.ThreadDeath:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					var tde = (ThreadDeathEvent)evt;
					// Destroy it before calling IncrementSuspendCount() since it will update thread props
					SendMessage(new DelegatePendingMessage(true, () => DestroyThread(tde.Thread)));
					IncrementSuspendCount();
					break;

				case EventType.AppDomainCreate:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					var adce = (AppDomainCreateEvent)evt;
					SendMessage(new DelegatePendingMessage(true, () => CreateAppDomain(adce.Domain)));
					break;

				case EventType.AppDomainUnload:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					var adue = (AppDomainUnloadEvent)evt;
					SendMessage(new DelegatePendingMessage(true, () => DestroyAppDomain(adue.Domain)));
					break;

				case EventType.MethodEntry:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					Debug.Assert(evt.TryGetRequest() == methodEntryEventRequest);
					if (evt.TryGetRequest() == methodEntryEventRequest) {
						methodEntryEventRequest.Disable();
						// Func-eval doesn't work at first assembly load event for some reason. Should work now though.
						canInitializeObjectConstants = true;
						IncrementSuspendCount();
						ResumeCore();
					}
					break;

				case EventType.AssemblyLoad:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					var ale = (AssemblyLoadEvent)evt;
					InitializeDomain(ale.Assembly.Domain);
					// The debugger agent doesn't support netmodules...
					SendMessage(new DelegatePendingMessage(true, () => CreateModule(ale.Assembly.ManifestModule)));
					break;

				case EventType.AssemblyUnload:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					var aue = (AssemblyUnloadEvent)evt;
					var monoModule = TryGetModuleCore_NoCreate(aue.Assembly.ManifestModule);
					Debug.Assert(monoModule != null);
					if (monoModule != null) {
						foreach (var module in GetAssemblyModules(monoModule)) {
							if (!TryGetModuleData(module, out var data))
								continue;
							var tmp = data.MonoModule;
							SendMessage(new DelegatePendingMessage(true, () => DestroyModule(tmp)));
						}
					}
					break;

				case EventType.Breakpoint:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.Step:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.Exception:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					var ee = (ExceptionEvent)evt;
					thrownException = ee.Exception;
					if (ee.TryGetRequest() == uncaughtRequest)
						isUnhandledException = true;
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
						objectFactory.CreateException(new DbgExceptionId(PredefinedExceptionCategories.DotNet, TryGetExceptionName(exObj) ?? "???"), exFlags, EvalReflectionUtils.TryGetExceptionMessage(exObj), TryGetThread(ee.Thread), TryGetModule(ee.Thread), GetMessageFlags());
					}));
					break;

				case EventType.UserBreak:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					var ube = (UserBreakEvent)evt;
					SendMessage(new DbgMessageBreak(TryGetThread(ube.Thread), GetMessageFlags()));
					break;

				case EventType.UserLog:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					var ule = (UserLogEvent)evt;
					SendMessage(new NormalPendingMessage(this, true, new DbgMessageProgramMessage(ule.Message, TryGetThread(ule.Thread), GetMessageFlags())));
					break;

				case EventType.VMDisconnect:
					if (vmDeathExitCode == null && TryGetProcessExitCode(out exitCode))
						vmDeathExitCode = exitCode;
					if (vmDeathExitCode == null) {
						vmDeathExitCode = -1;
						dbgManager.ShowError(dnSpy_Debugger_DotNet_Mono_Resources.Error_ConnectionWasUnexpectedlyClosed);
					}
					Message?.Invoke(this, new DbgMessageDisconnected(vmDeathExitCode.Value, GetMessageFlags()));
					gotVMDisconnect = true;
					break;

				default:
					Debug.Fail($"Unknown event type: {evt.EventType}");
					break;
				}
			}
		}

		bool TryGetProcessExitCode(out int exitCode) {
			if (!hProcess_debuggee.IsClosed && !hProcess_debuggee.IsInvalid) {
				if (NativeMethods.GetExitCodeProcess(hProcess_debuggee.DangerousGetHandle(), out exitCode))
					return true;
			}

			exitCode = 0;
			return false;
		}

		DbgModule TryGetModule(ThreadMirror thread) {
			var frames = thread.GetFrames();
			if (frames.Length == 0)
				return null;
			return TryGetModule(frames[0].Method?.DeclaringType.Module);
		}

		string TryGetExceptionName(ObjectMirror exObj) {
			var reflectionAppDomain = TryGetEngineAppDomain(exObj.Domain)?.AppDomain.GetReflectionAppDomain();
			Debug.Assert(reflectionAppDomain != null);
			if (reflectionAppDomain == null)
				return exObj.Type.FullName;
			var type = new ReflectionTypeCreator(this, reflectionAppDomain).Create(exObj.Type);
			if (type.IsConstructedGenericType)
				type = type.GetGenericTypeDefinition();
			return type.FullName;
		}

		DbgEngineAppDomain TryGetEngineAppDomain(AppDomainMirror monoAppDomain) {
			if (monoAppDomain == null)
				return null;
			DbgEngineAppDomain engineAppDomain;
			bool b;
			lock (lockObj)
				b = toEngineAppDomain.TryGetValue(monoAppDomain, out engineAppDomain);
			Debug.Assert(b);
			return engineAppDomain;
		}

		int GetAppDomainId(AppDomainMirror monoAppDomain) {
			debuggerThread.VerifyAccess();
			//TODO: Get AD obj, func-eval get_Id()
			return nextAppDomainId++;
		}
		int nextAppDomainId = 1;

		void InitializeDomain(AppDomainMirror monoAppDomain) {
			debuggerThread.VerifyAccess();
			lock (lockObj) {
				if (!appDomainsThatHaveNotBeenInitializedYet.Remove(monoAppDomain))
					return;
				bool b = toEngineAppDomain.TryGetValue(monoAppDomain, out var engineAppDomain);
				Debug.Assert(b);
				if (b)
					engineAppDomain.UpdateName(monoAppDomain.FriendlyName);
			}
			SendMessage(new DelegatePendingMessage(true, () => CreateModule(monoAppDomain.Corlib.ManifestModule)));
		}

		void CreateAppDomain(AppDomainMirror monoAppDomain) {
			debuggerThread.VerifyAccess();
			int appDomainId = GetAppDomainId(monoAppDomain);
			var appDomain = dmdRuntime.CreateAppDomain(appDomainId);
			var internalAppDomain = new DbgMonoDebugInternalAppDomainImpl(appDomain, monoAppDomain);
			var appDomainName = monoAppDomain.FriendlyName;
			var engineAppDomain = objectFactory.CreateAppDomain<object>(internalAppDomain, appDomainName, appDomainId, GetMessageFlags(), data: null, onCreated: engineAppDomain2 => internalAppDomain.SetAppDomain(engineAppDomain2.AppDomain));
			lock (lockObj) {
				appDomainsThatHaveNotBeenInitializedYet.Add(monoAppDomain);
				toEngineAppDomain.Add(monoAppDomain, engineAppDomain);
			}
		}

		void DestroyAppDomain(AppDomainMirror monoAppDomain) {
			debuggerThread.VerifyAccess();
			DbgEngineAppDomain engineAppDomain;
			lock (lockObj) {
				if (toEngineAppDomain.TryGetValue(monoAppDomain, out engineAppDomain)) {
					appDomainsThatHaveNotBeenInitializedYet.Remove(monoAppDomain);
					toEngineAppDomain.Remove(monoAppDomain);
					var appDomain = engineAppDomain.AppDomain;
					dmdRuntime.Remove(((DbgMonoDebugInternalAppDomainImpl)appDomain.InternalAppDomain).ReflectionAppDomain);
					foreach (var kv in toEngineThread.ToArray()) {
						if (kv.Value.Thread.AppDomain == appDomain)
							toEngineThread.Remove(kv.Key);
					}
					foreach (var kv in toEngineModule.ToArray()) {
						if (kv.Value.Module.AppDomain == appDomain)
							toEngineModule.Remove(kv.Key);
					}
				}
			}
			if (engineAppDomain != null)
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
			if (module.TryGetData(out DbgModuleData data))
				return data.ModuleId;
			return null;
		}

		bool TryGetModuleData(DbgModule module, out DbgModuleData data) {
			if (module.TryGetData(out data) && data.Engine == this)
				return true;
			data = null;
			return false;
		}

		int moduleOrder;
		bool CreateModule(ModuleMirror monoModule) {
			debuggerThread.VerifyAccess();

			if (TryGetModuleCore_NoCreate(monoModule) != null)
				return false;

			var appDomain = TryGetEngineAppDomain(monoModule.Assembly.Domain).AppDomain;
			var moduleData = new DbgModuleData(this, monoModule);
			var engineModule = ModuleCreator.CreateModule(this, objectFactory, appDomain, monoModule, moduleOrder++, moduleData);
			moduleData.ModuleId = ModuleIdUtils.Create(engineModule.Module, monoModule);
			lock (lockObj) {
				if (!toAssemblyModules.TryGetValue(monoModule.Assembly, out var modules))
					toAssemblyModules.Add(monoModule.Assembly, modules = new List<ModuleMirror>());
				modules.Add(monoModule);
				toEngineModule.Add(monoModule, engineModule);
			}
			return true;
		}

		void DestroyModule(ModuleMirror monoModule) {
			debuggerThread.VerifyAccess();
			DbgEngineModule engineModule;
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
			if (engineModule != null)
				engineModule.Remove(GetMessageFlags());
		}

		internal DbgModule TryGetModule(ModuleMirror monoModule) {
			if (monoModule == null)
				return null;
			var res = TryGetModuleCore_NoCreate(monoModule);
			if (res != null)
				return res;
			DiscoverNewModules(monoModule);
			res = TryGetModuleCore_NoCreate(monoModule);
			Debug.Assert(res != null);
			return res;
		}

		DbgModule TryGetModuleCore_NoCreate(ModuleMirror monoModule) {
			if (monoModule == null)
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
			if (monoModule != null) {
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
			if (TryGetModuleCore_NoCreate(monoModule) != null)
				return;

			if (suspendCount == 0) {
				try {
					vm.Suspend();
					IncrementSuspendCount();
				}
				catch (Exception ex) {
					Debug.Fail(ex.Message);
					SendMessage(new DbgMessageBreak(ex.Message, GetMessageFlags()));
				}
				Debug.Assert(pendingMessages.Count == 0);
			}
			CreateModule(monoModule);
		}

		internal bool TryGetMonoModule(DbgModule module, out ModuleMirror monoModule) {
			if (module.TryGetData(out DbgModuleData data) && data.Engine == this) {
				monoModule = data.MonoModule;
				return true;
			}
			monoModule = null;
			return false;
		}

		DbgThread GetThreadPreferMain_MonoDebug() {
			debuggerThread.VerifyAccess();
			DbgThread firstThread = null;
			lock (lockObj) {
				foreach (var kv in toEngineThread) {
					var thread = TryGetThread(kv.Key);
					if (firstThread == null)
						firstThread = thread;
					if (thread?.IsMain == true)
						return thread;
				}
			}
			return firstThread;
		}

		DbgThread TryGetThread(ThreadMirror thread) {
			if (thread == null)
				return null;
			DbgEngineThread engineThread;
			lock (lockObj)
				toEngineThread.TryGetValue(thread, out engineThread);
			return engineThread?.Thread;
		}

		sealed class StartException : Exception {
			public StartException(string message) : base(message) { }
		}

		static string GetCouldNotConnectErrorMessage(string address, ushort port, string filenameOpt) {
			string extra = filenameOpt == null ? $" ({address}:{port})" : $" ({address}:{port} = {filenameOpt})";
			return dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotConnectToProcess + extra;
		}

		internal IDbgDotNetRuntime DotNetRuntime => internalRuntime;
		DbgMonoDebugInternalRuntimeImpl internalRuntime;
		public override DbgInternalRuntime CreateInternalRuntime(DbgRuntime runtime) {
			if (internalRuntime != null)
				throw new InvalidOperationException();
			dmdRuntime = DmdRuntimeFactory.CreateRuntime(new DmdEvaluatorImpl(this), runtime.Process.PointerSize == 4 ? DmdImageFileMachine.I386 : DmdImageFileMachine.AMD64);
			return internalRuntime = new DbgMonoDebugInternalRuntimeImpl(this, runtime, dmdRuntime, monoDebugRuntimeKind);
		}

		sealed class RuntimeData {
			public DbgEngineImpl Engine { get; }
			public RuntimeData(DbgEngineImpl engine) => Engine = engine;
		}

		internal static DbgEngineImpl TryGetEngine(DbgRuntime runtime) {
			if (runtime.TryGetData(out RuntimeData data))
				return data.Engine;
			return null;
		}

		internal DbgModule[] GetAssemblyModules(DbgModule module) {
			if (!TryGetModuleData(module, out var data))
				return Array.Empty<DbgModule>();
			lock (lockObj) {
				toAssemblyModules.TryGetValue(data.MonoModule.Assembly, out var modules);
				if (modules == null || modules.Count == 0)
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

			MonoDebugThread(() => {
				if (gotVMDisconnect)
					return;
				Debug.Assert(vm != null);
				if (vm != null) {
					SendMessage(new DelegatePendingMessage(true, () => CreateAppDomain(vm.RootDomain)));
					foreach (var monoThread in vm.GetThreads())
						SendMessage(new DelegatePendingMessage(true, () => CreateThread(monoThread)));
				}
			});
		}

		public override void Break() => MonoDebugThread(BreakCore);
		void BreakCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_MonoDebugThread)
				return;
			try {
				if (suspendCount == 0) {
					vm.Suspend();
					IncrementSuspendCount();
				}
				SendMessage(new DbgMessageBreak(GetThreadPreferMain_MonoDebug(), GetMessageFlags()));
			}
			catch (Exception ex) {
				Debug.Fail(ex.Message);
				SendMessage(new DbgMessageBreak(ex.Message, GetMessageFlags()));
			}
		}

		public override void Run() => MonoDebugThread(RunCore);
		void RunCore() {
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
				if (IsEvaluating)
					return;
				ResumeCore();
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
				vm.Resume();
				DecrementSuspendCount();
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
					vm.Exit(0);
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
				vm.Detach();
				vmDeathExitCode = -1;
			}
			catch (Exception ex) {
				Debug.Fail(ex.Message);
				dbgManager.ShowError(ex.Message);
			}
		}

		public override DbgEngineStepper CreateStepper(DbgThread thread) {
			throw new NotImplementedException();//TODO:
		}

		internal DbgDotNetValue TryGetExceptionValue() {
			debuggerThread.VerifyAccess();
			var exValue = thrownException;
			if (exValue == null)
				return null;
			var reflectionAppDomain = TryGetEngineAppDomain(exValue.Domain)?.AppDomain.GetReflectionAppDomain();
			Debug.Assert(reflectionAppDomain != null);
			if (reflectionAppDomain == null)
				return null;
			var exceptionType = new ReflectionTypeCreator(this, reflectionAppDomain).Create(exValue.Type);
			var valueLocation = new NoValueLocation(exceptionType, exValue);
			return CreateDotNetValue_MonoDebug(valueLocation);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			try {
				if (!gotVMDisconnect)
					vm?.Detach();
			}
			catch {
			}
			hProcess_debuggee?.Close();
		}
	}
}
