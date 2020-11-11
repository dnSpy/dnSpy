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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Exceptions;
using dnSpy.Debugger.Shared;

namespace dnSpy.Debugger.Impl {
	[Export(typeof(DbgManager))]
	sealed partial class DbgManagerImpl : DbgManager, IIsRunningProvider {
		static int currentProcessId = Process.GetCurrentProcess().Id;

		public override event EventHandler<DbgMessageEventArgs>? Message;
		public override event EventHandler<DbgMessageProcessCreatedEventArgs>? MessageProcessCreated;
		public override event EventHandler<DbgMessageProcessExitedEventArgs>? MessageProcessExited;
		public override event EventHandler<DbgMessageRuntimeCreatedEventArgs>? MessageRuntimeCreated;
		public override event EventHandler<DbgMessageRuntimeExitedEventArgs>? MessageRuntimeExited;
		public override event EventHandler<DbgMessageAppDomainLoadedEventArgs>? MessageAppDomainLoaded;
		public override event EventHandler<DbgMessageAppDomainUnloadedEventArgs>? MessageAppDomainUnloaded;
		public override event EventHandler<DbgMessageModuleLoadedEventArgs>? MessageModuleLoaded;
		public override event EventHandler<DbgMessageModuleUnloadedEventArgs>? MessageModuleUnloaded;
		public override event EventHandler<DbgMessageThreadCreatedEventArgs>? MessageThreadCreated;
		public override event EventHandler<DbgMessageThreadExitedEventArgs>? MessageThreadExited;
		public override event EventHandler<DbgMessageExceptionThrownEventArgs>? MessageExceptionThrown;
		public override event EventHandler<DbgMessageEntryPointBreakEventArgs>? MessageEntryPointBreak;
		public override event EventHandler<DbgMessageProgramMessageEventArgs>? MessageProgramMessage;
		public override event EventHandler<DbgMessageBoundBreakpointEventArgs>? MessageBoundBreakpoint;
		public override event EventHandler<DbgMessageProgramBreakEventArgs>? MessageProgramBreak;
		public override event EventHandler<DbgMessageStepCompleteEventArgs>? MessageStepComplete;
		public override event EventHandler<DbgMessageSetIPCompleteEventArgs>? MessageSetIPComplete;
		public override event EventHandler<DbgMessageUserMessageEventArgs>? MessageUserMessage;
		public override event EventHandler<DbgMessageBreakEventArgs>? MessageBreak;
		public override event EventHandler<DbgMessageAsyncProgramMessageEventArgs>? MessageAsyncProgramMessage;

		void RaiseMessage_DbgThread(ref DbgBreakInfoCollectionBuilder builder, DbgMessageEventArgs e) {
			Dispatcher.VerifyAccess();
			Message?.Invoke(this, e);
			switch (e.Kind) {
			case DbgMessageKind.ProcessCreated:
				MessageProcessCreated?.Invoke(this, (DbgMessageProcessCreatedEventArgs)e);
				break;

			case DbgMessageKind.ProcessExited:
				MessageProcessExited?.Invoke(this, (DbgMessageProcessExitedEventArgs)e);
				break;

			case DbgMessageKind.RuntimeCreated:
				MessageRuntimeCreated?.Invoke(this, (DbgMessageRuntimeCreatedEventArgs)e);
				break;

			case DbgMessageKind.RuntimeExited:
				MessageRuntimeExited?.Invoke(this, (DbgMessageRuntimeExitedEventArgs)e);
				break;

			case DbgMessageKind.AppDomainLoaded:
				MessageAppDomainLoaded?.Invoke(this, (DbgMessageAppDomainLoadedEventArgs)e);
				break;

			case DbgMessageKind.AppDomainUnloaded:
				MessageAppDomainUnloaded?.Invoke(this, (DbgMessageAppDomainUnloadedEventArgs)e);
				break;

			case DbgMessageKind.ModuleLoaded:
				MessageModuleLoaded?.Invoke(this, (DbgMessageModuleLoadedEventArgs)e);
				break;

			case DbgMessageKind.ModuleUnloaded:
				MessageModuleUnloaded?.Invoke(this, (DbgMessageModuleUnloadedEventArgs)e);
				break;

			case DbgMessageKind.ThreadCreated:
				MessageThreadCreated?.Invoke(this, (DbgMessageThreadCreatedEventArgs)e);
				break;

			case DbgMessageKind.ThreadExited:
				MessageThreadExited?.Invoke(this, (DbgMessageThreadExitedEventArgs)e);
				break;

			case DbgMessageKind.ExceptionThrown:
				MessageExceptionThrown?.Invoke(this, (DbgMessageExceptionThrownEventArgs)e);
				break;

			case DbgMessageKind.EntryPointBreak:
				MessageEntryPointBreak?.Invoke(this, (DbgMessageEntryPointBreakEventArgs)e);
				break;

			case DbgMessageKind.ProgramMessage:
				MessageProgramMessage?.Invoke(this, (DbgMessageProgramMessageEventArgs)e);
				break;

			case DbgMessageKind.BoundBreakpoint:
				MessageBoundBreakpoint?.Invoke(this, (DbgMessageBoundBreakpointEventArgs)e);
				break;

			case DbgMessageKind.ProgramBreak:
				MessageProgramBreak?.Invoke(this, (DbgMessageProgramBreakEventArgs)e);
				break;

			case DbgMessageKind.StepComplete:
				MessageStepComplete?.Invoke(this, (DbgMessageStepCompleteEventArgs)e);
				break;

			case DbgMessageKind.SetIPComplete:
				MessageSetIPComplete?.Invoke(this, (DbgMessageSetIPCompleteEventArgs)e);
				break;

			case DbgMessageKind.UserMessage:
				MessageUserMessage?.Invoke(this, (DbgMessageUserMessageEventArgs)e);
				break;

			case DbgMessageKind.Break:
				MessageBreak?.Invoke(this, (DbgMessageBreakEventArgs)e);
				break;

			case DbgMessageKind.AsyncProgramMessage:
				MessageAsyncProgramMessage?.Invoke(this, (DbgMessageAsyncProgramMessageEventArgs)e);
				break;

			default:
				throw new InvalidOperationException();
			}
			if (e.Pause)
				builder.Add(e);
		}

		void RaiseUserMessage_DbgThread(UserMessageKind messageKind, string message) {
			Dispatcher.VerifyAccess();
			var msg = new DbgMessageUserMessageEventArgs(messageKind, message);
			Message?.Invoke(this, msg);
			MessageUserMessage?.Invoke(this, msg);
		}

		public override DbgDispatcher Dispatcher => dbgDispatcherProvider.Dispatcher;
		Dispatcher InternalDispatcher => dbgDispatcherProvider.InternalDispatcher;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgProcess>>? ProcessesChanged;
		public override DbgProcess[] Processes {
			get {
				lock (lockObj)
					return processes.ToArray();
			}
		}
		readonly List<DbgProcessImpl> processes;

		public override event EventHandler? IsDebuggingChanged;
		public override bool IsDebugging {
			get {
				lock (lockObj)
					return engines.Count > 0;
			}
		}

		bool IIsRunningProvider.IsRunning => IsRunning == true;
		event EventHandler? IIsRunningProvider.IsRunningChanged {
			add => IIsRunningProvider_IsRunningChanged += value;
			remove => IIsRunningProvider_IsRunningChanged -= value;
		}
		EventHandler? IIsRunningProvider_IsRunningChanged;

		void RaiseIsRunningChanged_DbgThread() {
			Dispatcher.VerifyAccess();
			IsRunningChanged?.Invoke(this, EventArgs.Empty);
			IIsRunningProvider_IsRunningChanged?.Invoke(this, EventArgs.Empty);
		}

		public override event EventHandler? DelayedIsRunningChanged;
		public override event EventHandler? IsRunningChanged;
		public override bool? IsRunning {
			get {
				lock (lockObj)
					return cachedIsRunning;
			}
		}
		bool? CalculateIsRunning_NoLock() {
			if (engines.Count == 0)
				return false;
			int pausedCounter = 0;
			foreach (var info in engines) {
				if (info.EngineState == EngineState.Paused)
					pausedCounter++;
			}
			if (pausedCounter == engines.Count)
				return false;
			if (pausedCounter == 0)
				return true;
			return null;
		}
		bool? cachedIsRunning;

		public override event EventHandler<DbgCollectionChangedEventArgs<string>>? DebugTagsChanged;
		public override string[] DebugTags {
			get {
				lock (lockObj)
					return debugTags.Tags.ToArray();
			}
		}
		readonly TagsCollection debugTags;

		enum EngineState {
			/// <summary>
			/// Original state which it has before it gets connected (<see cref="DbgMessageConnected"/>)
			/// </summary>
			Starting,

			/// <summary>
			/// It's running
			/// </summary>
			Running,

			/// <summary>
			/// It's paused due to some pause event, eg. <see cref="DbgMessageBreak"/>
			/// </summary>
			Paused,
		}

		sealed class EngineInfo {
			public DbgEngine Engine { get; }
			public DbgProcessImpl? Process { get; set; }
			public DbgRuntimeImpl? Runtime { get; set; }
			public EngineState EngineState { get; set; }
			public string[] DebugTags { get; }
			public DbgObjectFactoryImpl? ObjectFactory { get; set; }
			public DbgException? Exception { get; set; }
			public bool DelayedIsRunning { get; set; }
			public string? BreakKind { get; }
			public EngineInfo(DbgEngine engine, string? breakKind) {
				Engine = engine;
				DebugTags = (string[])engine.DebugTags.Clone();
				EngineState = EngineState.Starting;
				DelayedIsRunning = true;
				BreakKind = breakKind;
			}
		}

		readonly object lockObj;
		readonly DbgDispatcherProvider dbgDispatcherProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<BoundCodeBreakpointsService> boundCodeBreakpointsService;
		readonly BoundBreakpointsManager boundBreakpointsManager;
		readonly List<EngineInfo> engines;
		readonly Lazy<DbgEngineProvider, IDbgEngineProviderMetadata>[] dbgEngineProviders;
		readonly Lazy<IDbgManagerStartListener>[] dbgManagerStartListeners;
		readonly Lazy<DbgModuleMemoryRefreshedNotifier>[] dbgModuleMemoryRefreshedNotifiers;
		readonly List<StartDebuggingOptions> restartOptions;
		readonly HashSet<ProcessKey> debuggedRuntimes;
		readonly List<DbgObject> objsToClose;
		int hasNotifiedStartListenersCounter;

		[ImportingConstructor]
		DbgManagerImpl(DbgDispatcherProvider dbgDispatcherProvider, DebuggerSettings debuggerSettings, Lazy<BoundCodeBreakpointsService> boundCodeBreakpointsService, [ImportMany] IEnumerable<Lazy<DbgEngineProvider, IDbgEngineProviderMetadata>> dbgEngineProviders, [ImportMany] IEnumerable<Lazy<IDbgManagerStartListener>> dbgManagerStartListeners, [ImportMany] IEnumerable<Lazy<DbgModuleMemoryRefreshedNotifier>> dbgModuleMemoryRefreshedNotifiers) {
			lockObj = new object();
			this.dbgDispatcherProvider = dbgDispatcherProvider;
			this.debuggerSettings = debuggerSettings;
			this.boundCodeBreakpointsService = boundCodeBreakpointsService;
			boundBreakpointsManager = new BoundBreakpointsManager(this);
			engines = new List<EngineInfo>();
			processes = new List<DbgProcessImpl>();
			debugTags = new TagsCollection();
			restartOptions = new List<StartDebuggingOptions>();
			debuggedRuntimes = new HashSet<ProcessKey>();
			dbgCurrentProcess = new DbgCurrentProcess(this);
			dbgCurrentRuntime = new DbgCurrentRuntime(this);
			dbgCurrentThread = new DbgCurrentThread(this);
			objsToClose = new List<DbgObject>();
			this.dbgEngineProviders = dbgEngineProviders.OrderBy(a => a.Metadata.Order).ToArray();
			this.dbgManagerStartListeners = dbgManagerStartListeners.ToArray();
			this.dbgModuleMemoryRefreshedNotifiers = dbgModuleMemoryRefreshedNotifiers.ToArray();
			new DelayedIsRunningHelper(this, InternalDispatcher, RaiseDelayedIsRunningChanged_DbgThread);
		}

		// DbgManager thread
		void RaiseDelayedIsRunningChanged_DbgThread() {
			Dispatcher.VerifyAccess();
			if (IsRunning == true)
				DelayedIsRunningChanged?.Invoke(this, EventArgs.Empty);
		}

		public override string? Start(DebugProgramOptions options) {
			var clonedOptions = options.Clone();
			// Make sure Clone() works correctly by only using a clone of the input.
			// If it's buggy, the owner will notice something's wrong and fix it.
			options = clonedOptions.Clone();

			lock (dbgManagerStartListeners) {
				if (hasNotifiedStartListenersCounter == 0) {
					hasNotifiedStartListenersCounter++;
					boundBreakpointsManager.Initialize();
					foreach (var lz in dbgModuleMemoryRefreshedNotifiers)
						lz.Value.ModulesRefreshed += DbgModuleMemoryRefreshedNotifier_ModulesRefreshed;
					foreach (var lz in dbgManagerStartListeners)
						lz.Value.OnStart(this);
					hasNotifiedStartListenersCounter++;
				}
				else if (hasNotifiedStartListenersCounter != 2)
					throw new InvalidOperationException("Recursive call: Start()");
			}
			try {
				foreach (var lz in dbgEngineProviders) {
					var engine = lz.Value.Create(this, options);
					if (engine is not null) {
						DbgThread(() => Start_DbgThread(engine, options, clonedOptions));
						return null;
					}
				}
			}
			catch (Exception ex) {
				return ex.ToString();
			}
			Debug.Fail("Couldn't create a debug engine");
			// Doesn't need to be localized, should be considered a bug if this is ever reached
			return "Couldn't create a debug engine";
		}

		void Start_DbgThread(DbgEngine engine, DebugProgramOptions options, DebugProgramOptions clonedOptions) {
			Dispatcher.VerifyAccess();
			bool raiseIsDebuggingChanged, raiseIsRunningChanged;
			string[] addedDebugTags;
			lock (lockObj) {
				var oldIsRunning = cachedIsRunning;
				raiseIsDebuggingChanged = engines.Count == 0;
				string? breakKind = null;
				if (clonedOptions is StartDebuggingOptions startOptions) {
					restartOptions.Add(startOptions);
					breakKind = startOptions.BreakKind;
				}
				var engineInfo = new EngineInfo(engine, breakKind);
				engines.Add(engineInfo);
				addedDebugTags = debugTags.Add(engineInfo.DebugTags);
				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunningChanged = oldIsRunning != cachedIsRunning;
			}
			if (raiseIsDebuggingChanged)
				IsDebuggingChanged?.Invoke(this, EventArgs.Empty);
			if (raiseIsRunningChanged)
				RaiseIsRunningChanged_DbgThread();
			if (addedDebugTags.Length > 0)
				DebugTagsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<string>(addedDebugTags, added: true));

			engine.Message += DbgEngine_Message;
			engine.Start(options);
		}

		void DbgThread(Action callback) => Dispatcher.BeginInvoke(callback);

		bool IsOurEngine(DbgEngine engine) {
			lock (lockObj) {
				foreach (var info in engines) {
					if (info.Engine == engine)
						return true;
				}
			}
			return false;
		}

		void DbgEngine_Message(object? sender, DbgEngineMessage e) {
			if (sender is null)
				throw new ArgumentNullException(nameof(sender));
			if (e is null)
				throw new ArgumentNullException(nameof(e));
			var engine = sender as DbgEngine;
			if (engine is null)
				throw new ArgumentOutOfRangeException(nameof(sender));
			DbgThread(() => DbgEngine_Message_DbgThread(engine, e));
		}

		void DbgEngine_Message_DbgThread(DbgEngine engine, DbgEngineMessage e) {
			Dispatcher.VerifyAccess();
			if (!IsOurEngine(engine))
				return;
			switch (e.MessageKind) {
			case DbgEngineMessageKind.Connected:
				OnConnected_DbgThread(engine, (DbgMessageConnected)e);
				break;

			case DbgEngineMessageKind.Disconnected:
				OnDisconnected_DbgThread(engine, (DbgMessageDisconnected)e);
				break;

			case DbgEngineMessageKind.Break:
				OnBreak_DbgThread(engine, (DbgMessageBreak)e);
				break;

			case DbgEngineMessageKind.EntryPointBreak:
				OnEntryPointBreak_DbgThread(engine, (DbgMessageEntryPointBreak)e);
				break;

			case DbgEngineMessageKind.ProgramMessage:
				OnProgramMessage_DbgThread(engine, (DbgMessageProgramMessage)e);
				break;

			case DbgEngineMessageKind.Breakpoint:
				OnBreakpoint_DbgThread(engine, (DbgMessageBreakpoint)e);
				break;

			case DbgEngineMessageKind.ProgramBreak:
				OnProgramBreak_DbgThread(engine, (DbgMessageProgramBreak)e);
				break;

			case DbgEngineMessageKind.SetIPComplete:
				OnSetIPComplete_DbgThread(engine, (DbgMessageSetIPComplete)e);
				break;

			case DbgEngineMessageKind.AsyncProgramMessage:
				OnAsyncProgramMessage_DbgThread(engine, (DbgMessageAsyncProgramMessage)e);
				break;

			default:
				Debug.Fail($"Unknown message: {e.MessageKind}");
				break;
			}
		}

		EngineInfo? TryGetEngineInfo_NoLock(DbgEngine? engine) {
			foreach (var info in engines) {
				if (info.Engine == engine)
					return info;
			}
			return null;
		}

		EngineInfo GetEngineInfo_NoLock(DbgEngine engine) {
			var info = TryGetEngineInfo_NoLock(engine);
			if (info is null)
				throw new InvalidOperationException("Unknown debug engine");
			return info;
		}

		DbgRuntime? GetRuntime(DbgEngine engine) {
			lock (lockObj)
				return GetEngineInfo_NoLock(engine).Runtime;
		}

		DbgProcessImpl GetOrCreateProcess_DbgThread(int pid, DbgStartKind startKind, out bool createdProcess) {
			Dispatcher.VerifyAccess();
			DbgProcessImpl process;
			lock (lockObj) {
				foreach (var p in processes) {
					if (p.Id == pid) {
						createdProcess = false;
						return p;
					}
				}
				bool shouldDetach = startKind == DbgStartKind.Attach;
				process = new DbgProcessImpl(this, InternalDispatcher, pid, CalculateProcessState(null), shouldDetach);
				processes.Add(process);
			}
			ProcessesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgProcess>(process, added: true));
			createdProcess = true;
			return process;
		}

		void OnConnected_DbgThread(DbgEngine engine, DbgMessageConnected e) {
			Dispatcher.VerifyAccess();
			if (e.ErrorMessage is not null) {
				RaiseUserMessage_DbgThread(UserMessageKind.CouldNotConnect, e.ErrorMessage ?? "???");
				OnDisconnected_DbgThread(engine);
				return;
			}

			var process = GetOrCreateProcess_DbgThread(e.ProcessId, engine.StartKind, out var createdProcess);
			var runtime = new DbgRuntimeImpl(this, process, engine);
			var objectFactory = new DbgObjectFactoryImpl(this, runtime, engine, boundCodeBreakpointsService);

			DbgProcessState processState;
			bool pauseProgram = (e.MessageFlags & DbgEngineMessageFlags.Pause) != 0;
			bool otherPauseProgram;
			lock (lockObj) {
				var info = GetEngineInfo_NoLock(engine);
				pauseProgram |= info.BreakKind == PredefinedBreakKinds.CreateProcess;
				info.Process = process;
				info.Runtime = runtime;
				info.ObjectFactory = objectFactory;
				info.EngineState = EngineState.Paused;
				info.DelayedIsRunning = false;
				info.Runtime.SetBreakThread(null);
				processState = CalculateProcessState(info.Process);
				// Compare it before notifying the helper since it could clear it
				otherPauseProgram = breakAllHelper is not null;
				breakAllHelper?.OnConnected_DbgThread_NoLock(info);
				bool b = debuggedRuntimes.Add(new ProcessKey(e.ProcessId, runtime.Id));
				Debug.Assert(b);
			}
			// Call OnConnected() before we add the runtime to the process so the engine can add
			// data to the runtime before RuntimesChanged is raised.
			engine.OnConnected(objectFactory, runtime);
			process.Add_DbgThread(engine, runtime, processState);

			var builder = new DbgBreakInfoCollectionBuilder();
			if (createdProcess)
				RaiseMessage_DbgThread(ref builder, new DbgMessageProcessCreatedEventArgs(process));
			RaiseMessage_DbgThread(ref builder, new DbgMessageRuntimeCreatedEventArgs(runtime));

			boundBreakpointsManager.InitializeBoundBreakpoints_DbgThread(engine);

			bool pausedByListener = !builder.IsEmpty;
			if (pauseProgram || otherPauseProgram)
				builder.Add(new DbgBreakInfo(DbgBreakInfoKind.Connected, null));
			if (!builder.IsEmpty) {
				lock (lockObj) {
					var info = GetEngineInfo_NoLock(engine);
					info.Runtime!.SetBreakInfos_DbgThread(builder.Create());
				}
				OnEnginePaused_DbgThread(engine, process, thread: null, setCurrentProcess: pauseProgram || pausedByListener);
			}
			else
				Run_DbgThread(engine);
		}

		DbgProcessState CalculateProcessState(DbgProcess? process) {
			lock (lockObj) {
				int count = 0;
				if (process is not null) {
					foreach (var info in engines) {
						if (info.Process != process)
							continue;
						count++;
						if (info.EngineState != EngineState.Paused)
							return DbgProcessState.Running;
					}
				}
				return count == 0 ? DbgProcessState.Running : DbgProcessState.Paused;
			}
		}

		void OnDisconnected_DbgThread(DbgEngine engine, DbgMessageDisconnected e) {
			Dispatcher.VerifyAccess();
			OnDisconnected_DbgThread(engine);
		}

		void OnDisconnected_DbgThread(DbgEngine engine) {
			Dispatcher.VerifyAccess();
			DbgProcessImpl? processToDispose = null;
			DbgRuntimeImpl? runtime = null;
			var objectsToClose = new List<DbgObject>();
			bool raiseIsDebuggingChanged, raiseIsRunningChanged;
			string[] removedDebugTags;
			DbgProcessImpl? process;
			lock (lockObj) {
				var oldIsRunning = cachedIsRunning;
				var info = TryGetEngineInfo_NoLock(engine);
				if (info is not null) {
					if (info.Exception is not null)
						objectsToClose.Add(info.Exception);
					engines.Remove(info);
					raiseIsDebuggingChanged = engines.Count == 0;
					info.ObjectFactory?.Dispose();
				}
				else
					raiseIsDebuggingChanged = false;
				process = info?.Process;
				if (raiseIsDebuggingChanged)
					restartOptions.Clear();
				removedDebugTags = debugTags.Remove(info?.DebugTags ?? Array.Empty<string>());
				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunningChanged = oldIsRunning != cachedIsRunning;

				engine.Message -= DbgEngine_Message;
				if (process is not null) {
					var pinfo = process.Remove_DbgThread(engine);
					runtime = pinfo.runtime;
					Debug2.Assert(runtime is not null);
					runtime.SetBreakInfos_DbgThread(Array.Empty<DbgBreakInfo>());

					bool b = debuggedRuntimes.Remove(new ProcessKey(process.Id, runtime.Id));
					Debug.Assert(b);

					if (!pinfo.hasMoreRuntimes) {
						bool disposeProcess;
						disposeProcess = process.ExecuteLockedIfNoMoreRuntimes(() => processes.Remove(process), false);
						if (disposeProcess)
							processToDispose = process;
					}
				}

				breakAllHelper?.OnDisconnected_DbgThread_NoLock(engine);
			}

			if (runtime is not null)
				boundBreakpointsManager.RemoveAllBoundBreakpoints_DbgThread(runtime);

			foreach (var obj in objectsToClose)
				obj.Close(Dispatcher);

			process?.NotifyRuntimesChanged_DbgThread(runtime!);

			var builder = new DbgBreakInfoCollectionBuilder();
			if (runtime is not null)
				RaiseMessage_DbgThread(ref builder, new DbgMessageRuntimeExitedEventArgs(runtime));

			if (processToDispose is not null) {
				processToDispose.UpdateState_DbgThread(DbgProcessState.Terminated);
				ProcessesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgProcess>(processToDispose, added: false));
				int exitCode = processToDispose.GetExitCode();
				RaiseMessage_DbgThread(ref builder, new DbgMessageProcessExitedEventArgs(processToDispose, exitCode));
			}

			RecheckAndUpdateCurrentProcess_DbgThread();

			runtime?.Close(Dispatcher);
			engine.Close(Dispatcher);
			processToDispose?.Close(Dispatcher);

			// Raise them in reverse order (see Start())
			if (removedDebugTags.Length != 0)
				DebugTagsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<string>(removedDebugTags, added: false));
			if (raiseIsRunningChanged)
				RaiseIsRunningChanged_DbgThread();
			if (raiseIsDebuggingChanged)
				IsDebuggingChanged?.Invoke(this, EventArgs.Empty);
			if (!builder.IsEmpty)
				BreakAllProcessesIfNeeded_DbgThread();
		}

		public override bool CanRestart {
			get {
				lock (lockObj) {
					if (breakAllHelper is not null)
						return false;
					if (stopDebuggingHelper is not null)
						return false;
					if (restartOptions.Count == 0)
						return false;
					return true;
				}
			}
		}

		public override void Restart() {
			lock (lockObj) {
				if (!CanRestart)
					return;
				var restartOptionsCopy = restartOptions.ToArray();
				stopDebuggingHelper = new StopDebuggingHelper(this, success => {
					lock (lockObj) {
						Debug2.Assert(stopDebuggingHelper is not null);
						stopDebuggingHelper = null;
					}
					// Don't restart the programs in this thread since we're inside a ProcessesChanged callback.
					// That will mess up notifying about eg. IsDebuggingChanged and other events that should be,
					// but haven't yet been raised.
					DbgThread(() => {
						if (success) {
							foreach (var options in restartOptionsCopy)
								Start(options);
						}
						else {
							//TODO: Notify user that it timed out
						}
					});
				});
			}
			stopDebuggingHelper.Initialize();
		}
		StopDebuggingHelper? stopDebuggingHelper;

		public override void BreakAll() {
			lock (lockObj) {
				if (breakAllHelper is not null)
					return;
				if (stopDebuggingHelper is not null)
					return;
				breakAllHelper = new BreakAllHelper(this);
				breakAllHelper.Start_NoLock();
			}
		}
		BreakAllHelper? breakAllHelper;

		void RaiseIsRunningChangedIfNeeded_DbgThread() {
			Dispatcher.VerifyAccess();
			bool raiseIsRunningChanged;
			lock (lockObj) {
				var oldCachedIsRunning = cachedIsRunning;
				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunningChanged = oldCachedIsRunning != cachedIsRunning;
			}
			if (raiseIsRunningChanged)
				RaiseIsRunningChanged_DbgThread();
		}

		void BreakAllProcessesIfNeeded_DbgThread() {
			Dispatcher.VerifyAccess();
			RaiseIsRunningChangedIfNeeded_DbgThread();
			if (debuggerSettings.BreakAllProcesses)
				BreakAll();
		}

		void OnBreak_DbgThread(DbgEngine engine, DbgMessageBreak e) {
			Dispatcher.VerifyAccess();
			DbgProcessState processState;
			DbgProcessImpl? process;
			bool raiseIsRunningChanged;
			DbgThread? thread = null;
			bool forceSetCurrentProcess = false;
			lock (lockObj) {
				var oldCachedIsRunning = cachedIsRunning;
				var info = TryGetEngineInfo_NoLock(engine);
				// It could've been disconnected
				if (info is null)
					return;

				if (e.ErrorMessage is null) {
					var builder = new DbgBreakInfoCollectionBuilder();
					if (info.Runtime is not null)
						builder.Add(new DbgMessageBreakEventArgs(info.Runtime, e.Thread));
					else
						builder.Add(new DbgBreakInfo(DbgBreakInfoKind.Unknown, null));
					forceSetCurrentProcess = info.EngineState != EngineState.Paused && (dbgCurrentProcess.currentProcess.Current is null || dbgCurrentProcess.currentProcess.Current?.CurrentRuntime.Current?.Engine == engine);
					info.EngineState = EngineState.Paused;
					info.Runtime?.SetBreakInfos_DbgThread(builder.Create());
					info.DelayedIsRunning = false;
					thread = info.Runtime?.SetBreakThread((DbgThreadImpl?)e.Thread, true);
				}
				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunningChanged = oldCachedIsRunning != cachedIsRunning;
				processState = CalculateProcessState(info.Process);
				process = info.Process;
			}
			breakAllHelper?.OnBreak_DbgThread(engine);
			process?.UpdateState_DbgThread(processState);
			if (e.ErrorMessage is null)
				OnEnginePaused_DbgThread(engine, process, thread, setCurrentProcess: forceSetCurrentProcess);
			else
				RaiseUserMessage_DbgThread(UserMessageKind.CouldNotBreak, e.ErrorMessage);
			if (raiseIsRunningChanged)
				RaiseIsRunningChanged_DbgThread();
		}

		public override event EventHandler<ProcessPausedEventArgs>? ProcessPaused;
		void OnEnginePaused_DbgThread(DbgEngine engine, DbgProcess? process, DbgThread? thread, bool setCurrentProcess) {
			lock (lockObj) {
				var info = GetEngineInfo_NoLock(engine);
				info.Process?.SetPaused_DbgThread(info.Runtime);
			}
			bool didSetCurrentProcess = SetCurrentEngineIfCurrentIsNull_DbgThread(engine, setCurrentProcess);
			BreakAllProcessesIfNeeded_DbgThread();
			if (didSetCurrentProcess && process is not null)
				ProcessPaused?.Invoke(this, new ProcessPausedEventArgs(process, thread));
		}

		void BreakCompleted_DbgThread(bool success) {
			Dispatcher.VerifyAccess();
			bool raiseIsRunning;
			lock (lockObj) {
				var oldIsRunning = cachedIsRunning;
				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunning = oldIsRunning != cachedIsRunning;
			}
			if (raiseIsRunning)
				RaiseIsRunningChanged_DbgThread();
		}

		void OnEntryPointBreak_DbgThread(DbgEngine engine, DbgMessageEntryPointBreak e) {
			Dispatcher.VerifyAccess();
			var runtime = GetRuntime(engine);
			Debug2.Assert(runtime is not null);
			if (runtime is null)
				return;
			var ep = new DbgMessageEntryPointBreakEventArgs(runtime, e.Thread);
			OnConditionalBreak_DbgThread(engine, ep, ep.Thread, e.MessageFlags | DbgEngineMessageFlags.Pause);
		}

		void OnProgramMessage_DbgThread(DbgEngine engine, DbgMessageProgramMessage e) {
			Dispatcher.VerifyAccess();
			var runtime = GetRuntime(engine);
			Debug2.Assert(runtime is not null);
			if (runtime is null)
				return;
			var ep = new DbgMessageProgramMessageEventArgs(e.Message, runtime, e.Thread);
			OnConditionalBreak_DbgThread(engine, ep, ep.Thread, e.MessageFlags);
		}

		void OnAsyncProgramMessage_DbgThread(DbgEngine engine, DbgMessageAsyncProgramMessage e) {
			Dispatcher.VerifyAccess();
			var runtime = GetRuntime(engine);
			Debug2.Assert(runtime is not null);
			if (runtime is null)
				return;
			var ep = new DbgMessageAsyncProgramMessageEventArgs(e.Source, e.Message, runtime);
			OnConditionalBreak_DbgThread(engine, ep, null, e.MessageFlags | DbgEngineMessageFlags.Running);
		}

		void OnBreakpoint_DbgThread(DbgEngine engine, DbgMessageBreakpoint e) {
			Dispatcher.VerifyAccess();
			var eb = new DbgMessageBoundBreakpointEventArgs(e.BoundBreakpoint, e.Thread);
			OnConditionalBreak_DbgThread(engine, eb, eb.Thread, e.MessageFlags);
		}

		void OnProgramBreak_DbgThread(DbgEngine engine, DbgMessageProgramBreak e) {
			Dispatcher.VerifyAccess();
			var runtime = GetRuntime(engine);
			Debug2.Assert(runtime is not null);
			if (runtime is null)
				return;
			var eb = new DbgMessageProgramBreakEventArgs(runtime, e.Thread);
			var flags = e.MessageFlags;
			if (!debuggerSettings.IgnoreBreakInstructions && (e.MessageFlags & DbgEngineMessageFlags.Continue) == 0)
				flags |= DbgEngineMessageFlags.Pause;
			OnConditionalBreak_DbgThread(engine, eb, eb.Thread, flags);
		}

		void OnSetIPComplete_DbgThread(DbgEngine engine, DbgMessageSetIPComplete e) {
			Dispatcher.VerifyAccess();
			if (e.Thread.IsClosed)
				return;
			var es = new DbgMessageSetIPCompleteEventArgs(e.Thread, e.FramesInvalidated, e.Error);
			es.Pause = (e.MessageFlags & DbgEngineMessageFlags.Continue) == 0;
			OnConditionalBreak_DbgThread(engine, es, es.Thread, e.MessageFlags);
		}

		internal void AddAppDomain_DbgThread(DbgRuntimeImpl runtime, DbgAppDomainImpl appDomain, DbgEngineMessageFlags messageFlags) {
			Dispatcher.VerifyAccess();
			Debug.Assert(IsOurEngine(runtime.Engine));
			if (!IsOurEngine(runtime.Engine))
				return;
			runtime.Add_DbgThread(appDomain);
			var e = new DbgMessageAppDomainLoadedEventArgs(appDomain);
			OnConditionalBreak_DbgThread(runtime.Engine, e, null, messageFlags);
		}

		internal void AddModule_DbgThread(DbgRuntimeImpl runtime, DbgModuleImpl module, DbgEngineMessageFlags messageFlags) {
			Dispatcher.VerifyAccess();
			Debug.Assert(IsOurEngine(runtime.Engine));
			if (!IsOurEngine(runtime.Engine))
				return;
			runtime.Add_DbgThread(module);
			boundBreakpointsManager.AddBoundBreakpoints_DbgThread(new[] { module });
			var e = new DbgMessageModuleLoadedEventArgs(module);
			OnConditionalBreak_DbgThread(runtime.Engine, e, null, messageFlags);
		}

		internal void AddThread_DbgThread(DbgRuntimeImpl runtime, DbgThreadImpl thread, DbgEngineMessageFlags messageFlags) {
			Dispatcher.VerifyAccess();
			Debug.Assert(IsOurEngine(runtime.Engine));
			if (!IsOurEngine(runtime.Engine))
				return;
			runtime.Add_DbgThread(thread);
			var e = new DbgMessageThreadCreatedEventArgs(thread);
			OnConditionalBreak_DbgThread(runtime.Engine, e, e.Thread, messageFlags);
		}

		internal void RemoveAppDomain_DbgThread(DbgRuntimeImpl runtime, DbgAppDomainImpl appDomain, DbgEngineMessageFlags messageFlags) {
			Dispatcher.VerifyAccess();
			Debug.Assert(IsOurEngine(runtime.Engine));
			if (!IsOurEngine(runtime.Engine))
				return;
			var e = new DbgMessageAppDomainUnloadedEventArgs(appDomain);
			OnConditionalBreak_DbgThread(runtime.Engine, e, null, messageFlags);
		}

		internal void RemoveModule_DbgThread(DbgRuntimeImpl runtime, DbgModuleImpl module, DbgEngineMessageFlags messageFlags) {
			Dispatcher.VerifyAccess();
			Debug.Assert(IsOurEngine(runtime.Engine));
			if (!IsOurEngine(runtime.Engine))
				return;
			boundBreakpointsManager.RemoveBoundBreakpoints_DbgThread(new[] { module });
			var e = new DbgMessageModuleUnloadedEventArgs(module);
			OnConditionalBreak_DbgThread(runtime.Engine, e, null, messageFlags);
		}

		internal void RemoveThread_DbgThread(DbgRuntimeImpl runtime, DbgThreadImpl thread, DbgEngineMessageFlags messageFlags) {
			Dispatcher.VerifyAccess();
			Debug.Assert(IsOurEngine(runtime.Engine));
			if (!IsOurEngine(runtime.Engine))
				return;
			int exitCode = thread.GetExitCode();
			var e = new DbgMessageThreadExitedEventArgs(thread, exitCode);
			OnConditionalBreak_DbgThread(runtime.Engine, e, null, messageFlags);
		}

		internal void AddException_DbgThread(DbgRuntimeImpl runtime, DbgExceptionImpl exception, DbgEngineMessageFlags messageFlags) {
			Dispatcher.VerifyAccess();
			Debug.Assert(IsOurEngine(runtime.Engine));
			if (!IsOurEngine(runtime.Engine))
				return;
			var e = new DbgMessageExceptionThrownEventArgs(exception);
			OnConditionalBreak_DbgThread(runtime.Engine, e, e.Exception.Thread, messageFlags, exception);
		}

		void OnConditionalBreak_DbgThread(DbgEngine engine, DbgMessageEventArgs e, DbgThread? thread, DbgEngineMessageFlags messageFlags, DbgExceptionImpl? exception = null) {
			Dispatcher.VerifyAccess();

			bool pauseProgram = (messageFlags & DbgEngineMessageFlags.Pause) != 0;
			bool isRunning = (messageFlags & DbgEngineMessageFlags.Running) != 0;
			var builder = new DbgBreakInfoCollectionBuilder();
			RaiseMessage_DbgThread(ref builder, e);
			// Don't try to break it if it's already running. It's running if it can't be paused,
			// eg. it's Unity and a ThreadDeath event. If we try to pause it, it could hang the game.
			if (isRunning)
				return;
			bool otherPauseProgram = false;
			if (!pauseProgram && builder.IsEmpty) {
				lock (lockObj) {
					otherPauseProgram |= breakAllHelper is not null;
					// If we're func-eval'ing, don't pause it
					if (!otherPauseProgram && (messageFlags & DbgEngineMessageFlags.Continue) == 0)
						otherPauseProgram |= GetEngineInfo_NoLock(engine).EngineState == EngineState.Paused;
				}
			}
			bool pausedByListener = !builder.IsEmpty;
			if (pauseProgram || otherPauseProgram)
				builder.Add(e);
			if (!builder.IsEmpty) {
				DbgProcessState processState;
				DbgProcessImpl? process;
				bool wasPaused;
				lock (lockObj) {
					var info = GetEngineInfo_NoLock(engine);
					// If it's already paused, don't set a new process and raise ProcessPaused (this
					// happens when we get a SetIPComplete message)
					wasPaused = info.EngineState == EngineState.Paused;
					info.EngineState = EngineState.Paused;
					info.Runtime?.SetBreakInfos_DbgThread(builder.Create());
					info.DelayedIsRunning = false;
					var newThread = info.Runtime?.SetBreakThread((DbgThreadImpl?)thread);
					if (thread is null)
						thread = newThread;
					// If we get eg. SetIPComplete, the saved exception shouldn't be cleared
					if (exception is not null) {
						Debug2.Assert(info.Exception is null);
						info.Exception?.Close(Dispatcher);
						info.Exception = exception;
					}
					processState = CalculateProcessState(info.Process);
					process = info.Process;
				}
				breakAllHelper?.OnBreak_DbgThread(engine);
				process?.UpdateState_DbgThread(processState);
				OnEnginePaused_DbgThread(engine, process, thread, setCurrentProcess: (pauseProgram || pausedByListener) && !wasPaused);
			}
			else {
				exception?.Close(Dispatcher);
				lock (lockObj) {
					var info = GetEngineInfo_NoLock(engine);
					// If we got an event while func evaluating, then do not close all DbgObjects
					// in the 'close on continue' list. We should only do this if it's a real continue
					// caused by the user.
					if (info.EngineState == EngineState.Paused && (messageFlags & DbgEngineMessageFlags.Continue) == 0)
						info.Runtime?.OnBeforeContinuing_DbgThread();
				}
				engine.Run();
			}
		}

		public override void RunAll() =>
			DbgThread(() => RunAll_DbgThread());

		void RunAll_DbgThread() {
			Dispatcher.VerifyAccess();
			EngineInfo[] engineInfos;
			lock (lockObj)
				engineInfos = engines.ToArray();
			RunEngines_DbgThread(engineInfos);
		}

		public override void Run(DbgProcess process) {
			if (process is null)
				throw new ArgumentNullException(nameof(process));
			if (debuggerSettings.BreakAllProcesses)
				RunAll();
			else
				process.Run();
		}

		void RunEngines_DbgThread(EngineInfo[] engineInfos) =>
			RunEngines_DbgThread(engineInfos, defaultRunEngines_runEngine);
		static readonly Action<EngineInfo> defaultRunEngines_runEngine = info => info.Engine.Run();

		void RunEngines_DbgThread(EngineInfo[] engineInfos, Action<EngineInfo> runEngine) {
			Dispatcher.VerifyAccess();

			List<DbgException>? exceptions = null;
			lock (lockObj) {
				foreach (var info in engineInfos) {
					if (info.EngineState == EngineState.Paused && info.Exception is not null) {
						if (exceptions is null)
							exceptions = new List<DbgException>();
						exceptions.Add(info.Exception);
						info.Exception = null;
					}
				}
			}
			if (exceptions is not null) {
				foreach (var exception in exceptions)
					exception.Close(Dispatcher);
			}

			bool raiseIsRunning;
			var processes = new List<DbgProcessImpl>();
			lock (lockObj) {
				var oldIsRunning = cachedIsRunning;

				// If we're trying to break the processes, don't do a thing
				if (breakAllHelper is not null)
					return;

				foreach (var info in engineInfos) {
					if (info.EngineState == EngineState.Paused) {
						if (info.Process is not null && !processes.Contains(info.Process))
							processes.Add(info.Process);
						info.EngineState = EngineState.Running;
						info.Runtime?.SetBreakInfos_DbgThread(Array.Empty<DbgBreakInfo>());
						info.Process?.SetRunning_DbgThread(info.Runtime);
						Debug2.Assert(info.Exception is null);
						info.Runtime?.OnBeforeContinuing_DbgThread();
						runEngine(info);
					}
				}

				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunning = oldIsRunning != cachedIsRunning;
			}
			if (raiseIsRunning)
				RaiseIsRunningChanged_DbgThread();
			foreach (var process in processes)
				process.UpdateState_DbgThread(CalculateProcessState(process));
			RecheckAndUpdateCurrentProcess_DbgThread();
		}

		internal void SetDelayedIsRunning_DbgThread(DbgEngine[] engines) {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				foreach (var engine in engines) {
					var info = TryGetEngineInfo_NoLock(engine);
					if (info is not null) {
						info.Runtime?.ClearBreakThread();
						info.DelayedIsRunning = true;
					}
				}
			}
			RecheckAndUpdateCurrentProcess_DbgThread();
		}

		internal bool? GetDelayedIsRunning_DbgThread(DbgEngine engine) {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				foreach (var info in engines) {
					if (info.Engine == engine)
						return info.DelayedIsRunning;
				}
			}
			return null;
		}

		void Run_DbgThread(DbgEngine engine) {
			Dispatcher.VerifyAccess();
			EngineInfo? engineInfo;
			lock (lockObj)
				engineInfo = TryGetEngineInfo_NoLock(engine);
			if (engineInfo is not null)
				RunEngines_DbgThread(new[] { engineInfo });
		}

		public override void StopDebuggingAll() => DbgThread(() => StopDebuggingAll_DbgThread());
		void StopDebuggingAll_DbgThread() {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				foreach (var info in engines) {
					// Process could be null if it hasn't been connected yet
					if (info.Process?.ShouldDetach ?? info.Engine.StartKind == DbgStartKind.Attach)
						info.Engine.Detach();
					else
						info.Engine.Terminate();
				}
			}
		}

		public override void TerminateAll() => DbgThread(() => TerminateAll_DbgThread());
		void TerminateAll_DbgThread() {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				foreach (var info in engines)
					info.Engine.Terminate();
			}
		}

		public override void DetachAll() => DbgThread(() => DetachAll_DbgThread());
		void DetachAll_DbgThread() {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				foreach (var info in engines)
					info.Engine.Detach();
			}
		}

		public override bool CanDetachWithoutTerminating {
			get {
				lock (lockObj) {
					foreach (var info in engines) {
						if (!info.Engine.CanDetach)
							return false;
					}
					return true;
				}
			}
		}

		internal void Detach(DbgProcessImpl process) => DbgThread(() => Detach_DbgThread(process));
		void Detach_DbgThread(DbgProcessImpl process) {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				foreach (var info in engines) {
					if (info.Process == process)
						info.Engine.Detach();
				}
			}
		}

		internal void Terminate(DbgProcessImpl process) => DbgThread(() => Terminate_DbgThread(process));
		void Terminate_DbgThread(DbgProcessImpl process) {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				foreach (var info in engines) {
					if (info.Process == process)
						info.Engine.Terminate();
				}
			}
		}

		internal void Break(DbgProcessImpl process) => DbgThread(() => Break_DbgThread(process));
		void Break_DbgThread(DbgProcessImpl process) {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				foreach (var info in engines) {
					if (info.Process == process && info.EngineState == EngineState.Running)
						info.Engine.Break();
				}
			}
		}

		internal void Run(DbgProcessImpl process) => DbgThread(() => Run_DbgThread(process));
		void Run_DbgThread(DbgProcessImpl process) {
			Dispatcher.VerifyAccess();
			var engineInfos = new List<EngineInfo>();
			lock (lockObj) {
				foreach (var info in engines) {
					if (info.Process == process && info.EngineState == EngineState.Paused)
						engineInfos.Add(info);
				}
			}
			if (engineInfos.Count != 0)
				RunEngines_DbgThread(engineInfos.ToArray());
		}

		public override bool CanDebugRuntime(int pid, RuntimeId rid) {
			if (rid is null)
				throw new ArgumentNullException(nameof(rid));
			if (pid == currentProcessId)
				return false;
			lock (lockObj)
				return !debuggedRuntimes.Contains(new ProcessKey(pid, rid));
		}

		void DbgModuleMemoryRefreshedNotifier_ModulesRefreshed(object? sender, ModulesRefreshedEventArgs e) =>
			DbgThread(() => DbgModuleMemoryRefreshedNotifier_ModulesRefreshed_DbgThread(e));

		public override event EventHandler<ModulesRefreshedEventArgs>? ModulesRefreshed;
		void DbgModuleMemoryRefreshedNotifier_ModulesRefreshed_DbgThread(ModulesRefreshedEventArgs e) {
			Dispatcher.VerifyAccess();
			foreach (var module in e.Modules)
				((DbgModuleImpl)module).RaiseRefreshed();
			ModulesRefreshed?.Invoke(this, e);
			boundBreakpointsManager.ReAddBreakpoints_DbgThread(e.Modules);
		}

		public override void Close(DbgObject obj) {
			if (obj is null)
				throw new ArgumentNullException(nameof(obj));
			bool start;
			lock (objsToClose) {
				start = objsToClose.Count == 0;
				objsToClose.Add(obj);
			}
			if (start)
				DbgThread(() => CloseObjects_DbgThread());
		}

		public override void Close(IEnumerable<DbgObject> objs) {
			if (objs is null)
				throw new ArgumentNullException(nameof(objs));
			bool start;
			lock (objsToClose) {
				int origCount = objsToClose.Count;
				objsToClose.AddRange(objs);
				start = origCount == 0 && objsToClose.Count > 0;
			}
			if (start)
				DbgThread(() => CloseObjects_DbgThread());
		}

		void CloseObjects_DbgThread() {
			Dispatcher.VerifyAccess();
			DbgObject[] objs;
			lock (objsToClose) {
				objs = objsToClose.ToArray();
				objsToClose.Clear();
			}
			foreach (var obj in objs)
				obj.Close(Dispatcher);
		}

		public override event EventHandler<DbgManagerMessageEventArgs>? DbgManagerMessage;
		public override void WriteMessage(string messageKind, string message) {
			if (messageKind is null)
				throw new ArgumentNullException(nameof(messageKind));
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			DbgManagerMessage?.Invoke(this, new DbgManagerMessageEventArgs(messageKind, message));
		}
	}
}
