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
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Metadata;
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

		readonly object lockObj;
		readonly DebuggerThread debuggerThread;
		readonly DbgDotNetCodeRangeService dbgDotNetCodeRangeService;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory;
		readonly DbgManager dbgManager;
		readonly DbgModuleMemoryRefreshedNotifier2 dbgModuleMemoryRefreshedNotifier;
		readonly DmdRuntime dmdRuntime;
		readonly DmdDispatcherImpl dmdDispatcher;
		internal DbgRawMetadataService RawMetadataService { get; }
		readonly MonoDebugRuntimeKind monoDebugRuntimeKind;
		readonly DbgEngineRuntimeInfo runtimeInfo;
		readonly Dictionary<AppDomainMirror, DbgEngineAppDomain> toEngineAppDomain;
		readonly Dictionary<ModuleMirror, DbgEngineModule> toEngineModule;
		readonly Dictionary<ThreadMirror, DbgEngineThread> toEngineThread;
		readonly Dictionary<AssemblyMirror, List<ModuleMirror>> toAssemblyModules;
		VirtualMachine vm;
		int vmPid;
		int? vmDeathExitCode;
		bool gotVMDisconnect;
		DbgObjectFactory objectFactory;
		SafeHandle hProcess_debuggee;
		int suspendCount;
		readonly List<PendingMessage> pendingMessages;
		bool canSendNextMessage;

		static DbgEngineImpl() => ThreadMirror.NativeTransitions = true;

		public DbgEngineImpl(DbgEngineImplDependencies deps, DbgManager dbgManager, MonoDebugRuntimeKind monoDebugRuntimeKind) {
			if (deps == null)
				throw new ArgumentNullException(nameof(deps));
			lockObj = new object();
			suspendCount = 0;
			this.pendingMessages = new List<PendingMessage>();
			canSendNextMessage = true;
			toEngineAppDomain = new Dictionary<AppDomainMirror, DbgEngineAppDomain>();
			toEngineModule = new Dictionary<ModuleMirror, DbgEngineModule>();
			toEngineThread = new Dictionary<ThreadMirror, DbgEngineThread>();
			toAssemblyModules = new Dictionary<AssemblyMirror, List<ModuleMirror>>();
			debuggerSettings = deps.DebuggerSettings;
			dbgDotNetCodeRangeService = deps.DotNetCodeRangeService;
			dbgDotNetCodeLocationFactory = deps.DbgDotNetCodeLocationFactory;
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			dbgModuleMemoryRefreshedNotifier = deps.DbgModuleMemoryRefreshedNotifier;
			debuggerThread = new DebuggerThread("MonoDebug");
			debuggerThread.CallDispatcherRun();
			dmdRuntime = DmdRuntimeFactory.CreateRuntime(new DmdEvaluatorImpl(this), IntPtr.Size == 4 ? DmdImageFileMachine.I386 : DmdImageFileMachine.AMD64);
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
		}
		static readonly ReadOnlyCollection<string> monoRuntimeTags = new ReadOnlyCollection<string>(new[] {
			PredefinedDotNetDbgRuntimeTags.DotNet,
			PredefinedDotNetDbgRuntimeTags.DotNetMono,
		});
		static readonly ReadOnlyCollection<string> unityRuntimeTags = new ReadOnlyCollection<string>(new[] {
			PredefinedDotNetDbgRuntimeTags.DotNet,
			PredefinedDotNetDbgRuntimeTags.DotNetUnity,
		});

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
			public abstract void RaiseMessage();
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
			public override void RaiseMessage() => engine.Message?.Invoke(engine, message);
		}
		sealed class DelegatePendingMessage : PendingMessage {
			readonly Action raiseMessage;
			public override bool MustWaitForRun { get; }
			public DelegatePendingMessage(bool mustWaitForRun, Action raiseMessage) {
				MustWaitForRun = mustWaitForRun;
				this.raiseMessage = raiseMessage;
			}
			public override void RaiseMessage() => raiseMessage();
		}

		void SendMessage(DbgEngineMessage message, bool mustWaitForRun = false) =>
			SendMessage(new NormalPendingMessage(this, mustWaitForRun, message));
		void SendMessage(PendingMessage message) {
			debuggerThread.VerifyAccess();
			pendingMessages.Add(message);
			SendNextMessage();
		}

		bool SendNextMessage() {
			debuggerThread.VerifyAccess();
			if (!canSendNextMessage)
				return false;
			for (;;) {
				if (pendingMessages.Count == 0) {
					canSendNextMessage = pendingMessages.Count == 0;
					return false;
				}
				var pendingMessage = pendingMessages[0];
				pendingMessages.RemoveAt(0);
				pendingMessage.RaiseMessage();
				if (pendingMessage.MustWaitForRun) {
					canSendNextMessage = pendingMessages.Count == 0;
					return true;
				}
			}
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

				var events = new EventType[] {
					EventType.VMStart,
					EventType.VMDeath,
					EventType.ThreadStart,
					EventType.ThreadDeath,
					EventType.AppDomainCreate,
					EventType.AppDomainUnload,
					EventType.AssemblyLoad,
					EventType.AssemblyUnload,
					EventType.Exception,
					EventType.UserBreak,
					EventType.UserLog,
				};
				vm.EnableEvents(events, SuspendPolicy.All);

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
			if (suspendCount == 1)
				UpdateThreadProperties_MonoDebug();
		}

		void DecrementSuspendCount() {
			debuggerThread.VerifyAccess();
			suspendCount--;
		}

		void OnDebuggerEvents(EventSet eventSet) {
			debuggerThread.VerifyAccess();

			Debug.Assert(!gotVMDisconnect);
			if (gotVMDisconnect)
				return;

			foreach (var evt in eventSet.Events) {
				switch (evt.EventType) {
				case EventType.VMStart:
					IncrementSuspendCount();
					SendMessage(new DbgMessageConnected((uint)vmPid, GetMessageFlags()));
					break;

				case EventType.VMDeath:
					var vmde = (VMDeathEvent)evt;
					Debug.Assert(vmDeathExitCode == null);
					vmDeathExitCode = vmde.ExitCode;
					break;

				case EventType.ThreadStart:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.ThreadDeath:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.AppDomainCreate:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					var adce = (AppDomainCreateEvent)evt;
					SendMessage(new DelegatePendingMessage(true, () => CreateAppDomain(adce.Domain)));
					SendMessage(new DelegatePendingMessage(true, () => CreateModule(adce.Domain, adce.Domain.Corlib.ManifestModule)));
					break;

				case EventType.AppDomainUnload:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.AssemblyLoad:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					var ale = (AssemblyLoadEvent)evt;
					// The debugger agent doesn't support netmodules...
					SendMessage(new DelegatePendingMessage(true, () => CreateModule(ale.Assembly.Domain, ale.Assembly.ManifestModule)));
					break;

				case EventType.AssemblyUnload:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.Breakpoint:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.Step:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.TypeLoad:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.Exception:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.UserBreak:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.UserLog:
					Debug.Assert(eventSet.SuspendPolicy == SuspendPolicy.All);
					IncrementSuspendCount();
					break;//TODO:

				case EventType.VMDisconnect:
					gotVMDisconnect = true;
					if (vmDeathExitCode == null && (!hProcess_debuggee.IsClosed && !hProcess_debuggee.IsInvalid && NativeMethods.GetExitCodeProcess(hProcess_debuggee.DangerousGetHandle(), out int exitCode)))
						vmDeathExitCode = exitCode;
					if (vmDeathExitCode == null) {
						vmDeathExitCode = -1;
						dbgManager.ShowError(dnSpy_Debugger_DotNet_Mono_Resources.Error_ConnectionWasUnexpectedlyClosed);
					}
					SendMessage(new DbgMessageDisconnected(vmDeathExitCode.Value, GetMessageFlags()));
					break;

				default:
					Debug.Fail($"Unknown event type: {evt.EventType}");
					break;
				}
			}
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

		void CreateAppDomain(AppDomainMirror monoAppDomain) {
			debuggerThread.VerifyAccess();
			int appDomainId = GetAppDomainId(monoAppDomain);
			var appDomain = dmdRuntime.CreateAppDomain(appDomainId);
			var internalAppDomain = new DbgMonoDebugInternalAppDomainImpl(appDomain);
			var engineAppDomain = objectFactory.CreateAppDomain<object>(internalAppDomain, monoAppDomain.FriendlyName, appDomainId, GetMessageFlags(), data: null, onCreated: engineAppDomain2 => internalAppDomain.SetAppDomain(engineAppDomain2.AppDomain));
			lock (lockObj)
				toEngineAppDomain.Add(monoAppDomain, engineAppDomain);
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
		void CreateModule(AppDomainMirror monoAppDomain, ModuleMirror monoModule) {
			debuggerThread.VerifyAccess();
			Debug.Assert(monoAppDomain == monoModule.Assembly.Domain);

			var appDomain = TryGetEngineAppDomain(monoAppDomain)?.AppDomain;
			var moduleData = new DbgModuleData(this, monoModule);
			var engineModule = ModuleCreator.CreateModule(this, objectFactory, appDomain, monoModule, moduleOrder++, moduleData);
			moduleData.ModuleId = ModuleIdUtils.Create(engineModule.Module, monoModule);
			lock (lockObj) {
				if (!toAssemblyModules.TryGetValue(monoModule.Assembly, out var modules))
					toAssemblyModules.Add(monoModule.Assembly, modules = new List<ModuleMirror>());
				modules.Add(monoModule);
				toEngineModule.Add(monoModule, engineModule);
			}
		}

		internal DbgModule TryGetModule(ModuleMirror monoModule) {
			if (monoModule == null)
				return null;
			lock (lockObj) {
				if (toEngineModule.TryGetValue(monoModule, out var engineModule))
					return engineModule.Module;
			}
			return null;
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
				Debug.Assert(vm != null);
				if (vm != null) {
					SendMessage(new DelegatePendingMessage(true, () => CreateAppDomain(vm.RootDomain)));
					SendMessage(new DelegatePendingMessage(true, () => CreateModule(vm.RootDomain, vm.RootDomain.Corlib.ManifestModule)));
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
				if (!IsEvaluating)
					CloseDotNetValues_MonoDebug();
				canSendNextMessage = true;
				if (SendNextMessage())
					return;
				while (suspendCount > 0) {
					vm.Resume();
					DecrementSuspendCount();
				}
			}
			catch (Exception ex) {
				Debug.Fail(ex.Message);
				dbgManager.ShowError(ex.Message);
			}
		}

		public override void Terminate() => MonoDebugThread(TerminateCore);
		void TerminateCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_MonoDebugThread)
				return;
			try {
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
