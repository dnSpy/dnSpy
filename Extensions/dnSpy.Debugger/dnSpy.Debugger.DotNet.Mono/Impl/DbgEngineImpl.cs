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
		readonly DbgRawMetadataService rawMetadataService;
		readonly MonoDebugRuntimeKind monoDebugRuntimeKind;
		readonly DbgEngineRuntimeInfo runtimeInfo;
		VirtualMachine vm;
		int vmPid;
		int? vmDeathExitCode;
		bool gotVMDisconnect;
		DbgObjectFactory objectFactory;
		SafeHandle hProcess_debuggee;

		public DbgEngineImpl(DbgEngineImplDependencies deps, DbgManager dbgManager, MonoDebugRuntimeKind monoDebugRuntimeKind) {
			if (deps == null)
				throw new ArgumentNullException(nameof(deps));
			lockObj = new object();
			debuggerSettings = deps.DebuggerSettings;
			dbgDotNetCodeRangeService = deps.DotNetCodeRangeService;
			dbgDotNetCodeLocationFactory = deps.DbgDotNetCodeLocationFactory;
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			dbgModuleMemoryRefreshedNotifier = deps.DbgModuleMemoryRefreshedNotifier;
			debuggerThread = new DebuggerThread("MonoDebug");
			debuggerThread.CallDispatcherRun();
			dmdRuntime = DmdRuntimeFactory.CreateRuntime(new DmdEvaluatorImpl(this), IntPtr.Size == 4 ? DmdImageFileMachine.I386 : DmdImageFileMachine.AMD64);
			dmdDispatcher = new DmdDispatcherImpl(this);
			rawMetadataService = deps.RawMetadataService;
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

		DbgEngineMessageFlags GetMessageFlags(bool pause = false) {
			VerifyMonoDebugThread();
			var flags = DbgEngineMessageFlags.None;
			if (pause)
				flags |= DbgEngineMessageFlags.Pause;
			bool isEvaluating = false;//TODO:
			if (isEvaluating)
				flags |= DbgEngineMessageFlags.Continue;
			return flags;
		}

		bool HasConnected_MonoDebugThread {
			get {
				debuggerThread.VerifyAccess();
				return vm != null;
			}
		}

		void SendMessage(DbgEngineMessage message) => Message?.Invoke(this, message);

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
					EventType.MethodEntry,
					EventType.MethodExit,
					EventType.AssemblyLoad,
					EventType.AssemblyUnload,
					//TODO: Should only be enabled if it's a dynamic assembly
					EventType.TypeLoad,
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
				if (ex is StartException startExcetpion)
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
					SendMessage(new DbgMessageDisconnected(-1, DbgEngineMessageFlags.None));
					return;
				}
			}
		}

		void OnDebuggerEvents(EventSet eventSet) {
			debuggerThread.VerifyAccess();

			foreach (var evt in eventSet.Events) {
				switch (evt.EventType) {
				case EventType.VMStart:
					SendMessage(new DbgMessageConnected((uint)vmPid, GetMessageFlags()));
					break;

				case EventType.VMDeath:
					var vmde = (VMDeathEvent)evt;
					Debug.Assert(vmDeathExitCode == null);
					vmDeathExitCode = vmde.ExitCode;
					break;

				case EventType.ThreadStart:
					break;//TODO:

				case EventType.ThreadDeath:
					break;//TODO:

				case EventType.AppDomainCreate:
					break;//TODO:

				case EventType.AppDomainUnload:
					break;//TODO:

				case EventType.MethodEntry:
					break;//TODO:

				case EventType.MethodExit:
					break;//TODO:

				case EventType.AssemblyLoad:
					break;//TODO:

				case EventType.AssemblyUnload:
					break;//TODO:

				case EventType.Breakpoint:
					break;//TODO:

				case EventType.Step:
					break;//TODO:

				case EventType.TypeLoad:
					break;//TODO:

				case EventType.Exception:
					break;//TODO:

				case EventType.KeepAlive:
					break;

				case EventType.UserBreak:
					break;//TODO:

				case EventType.UserLog:
					break;//TODO:

				case EventType.VMDisconnect:
					Debug.Assert(!gotVMDisconnect);
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

		public override void OnConnected(DbgObjectFactory objectFactory, DbgRuntime runtime) {
			Debug.Assert(objectFactory.Runtime == runtime);
			Debug.Assert(Array.IndexOf(objectFactory.Process.Runtimes, runtime) < 0);
			this.objectFactory = objectFactory;
			runtime.GetOrCreateData(() => new RuntimeData(this));
		}

		public override void Break() => MonoDebugThread(BreakCore);
		void BreakCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_MonoDebugThread)
				return;
			try {
				vm.Suspend();
				DbgThread thread = null;//TODO: Get main thread
				SendMessage(new DbgMessageBreak(thread, GetMessageFlags()));
			}
			catch (Exception ex) {
				Debug.Fail(ex.Message);
				dbgManager.ShowError(ex.Message);
			}
		}

		public override void Run() => MonoDebugThread(RunCore);
		void RunCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_MonoDebugThread)
				return;
			try {
				vm.Resume();
			}
			catch (VMNotSuspendedException) {
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
