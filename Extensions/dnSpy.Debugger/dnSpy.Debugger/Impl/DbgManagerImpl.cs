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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.Impl {
	[Export(typeof(DbgManager))]
	sealed partial class DbgManagerImpl : DbgManager {
		public override DispatcherThread DispatcherThread => dispatcherThread;
		readonly DispatcherThreadImpl dispatcherThread;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgProcess>> ProcessesChanged;
		public override DbgProcess[] Processes {
			get {
				lock (lockObj)
					return processes.ToArray();
			}
		}
		readonly List<DbgProcessImpl> processes;

		public override DbgDebuggingContext DebuggingContext {
			get {
				lock (lockObj)
					return debuggingContext;
			}
		}
		DbgDebuggingContextImpl debuggingContext;

		public override event EventHandler IsDebuggingChanged;
		public override bool IsDebugging {
			get {
				lock (lockObj)
					return engines.Count > 0;
			}
		}

		public override event EventHandler IsRunningChanged;
		public override bool IsRunning {
			get {
				lock (lockObj)
					return cachedIsRunning;
			}
		}
		bool CalculateIsRunning_NoLock() {
			foreach (var info in engines) {
				// If at least one engine is paused, pretend all of them are
				if (info.EngineState == EngineState.Paused)
					return false;
			}
			return engines.Count != 0;
		}
		bool cachedIsRunning;

		public override event EventHandler<DbgCollectionChangedEventArgs<string>> DebugTagsChanged;
		public override string[] DebugTags {
			get {
				lock (lockObj)
					return debugTags.Tags;
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
			public DbgProcessImpl Process { get; set; }
			public EngineState EngineState { get; set; } = EngineState.Starting;
			public string[] DebugTags { get; }
			public EngineInfo(DbgEngine engine) {
				Engine = engine;
				DebugTags = (string[])engine.DebugTags.Clone();
			}
		}

		readonly object lockObj;
		readonly List<EngineInfo> engines;
		readonly Lazy<DbgEngineProvider, IDbgEngineProviderMetadata>[] dbgEngineProviders;
		readonly Lazy<IDbgManagerStartListener, IDbgManagerStartListenerMetadata>[] dbgManagerStartListeners;
		readonly List<StartDebuggingOptions> restartOptions;
		int hasNotifiedStartListenersCounter;

		[ImportingConstructor]
		DbgManagerImpl([ImportMany] IEnumerable<Lazy<DbgEngineProvider, IDbgEngineProviderMetadata>> dbgEngineProviders, [ImportMany] IEnumerable<Lazy<IDbgManagerStartListener, IDbgManagerStartListenerMetadata>> dbgManagerStartListeners) {
			lockObj = new object();
			engines = new List<EngineInfo>();
			processes = new List<DbgProcessImpl>();
			debugTags = new TagsCollection();
			dispatcherThread = new DispatcherThreadImpl();
			restartOptions = new List<StartDebuggingOptions>();
			this.dbgEngineProviders = dbgEngineProviders.OrderBy(a => a.Metadata.Order).ToArray();
			this.dbgManagerStartListeners = dbgManagerStartListeners.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public override string Start(StartDebuggingOptions options) {
			var clonedOptions = options.Clone();
			// Make sure Clone() works correctly by only using a clone of the input.
			// If it's buggy, the owner will notice something's wrong and fix it.
			options = clonedOptions.Clone();

			lock (dbgManagerStartListeners) {
				if (hasNotifiedStartListenersCounter == 0) {
					hasNotifiedStartListenersCounter++;
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
					if (engine != null) {
						DispatcherThread.Invoke(() => {
							bool raiseIsDebuggingChanged, raiseIsRunningChanged;
							string[] addedDebugTags;
							lock (lockObj) {
								var oldIsRunning = cachedIsRunning;
								raiseIsDebuggingChanged = engines.Count == 0;
								var engineInfo = new EngineInfo(engine);
								if (engine.StartKind == DbgStartKind.Start)
									restartOptions.Add(clonedOptions);
								engines.Add(engineInfo);
								addedDebugTags = debugTags.Add(engineInfo.DebugTags);
								cachedIsRunning = CalculateIsRunning_NoLock();
								raiseIsRunningChanged = oldIsRunning != cachedIsRunning;
								if (raiseIsDebuggingChanged) {
									Debug.Assert(debuggingContext == null);
									debuggingContext = new DbgDebuggingContextImpl();
								}
							}
							if (raiseIsDebuggingChanged)
								IsDebuggingChanged?.Invoke(this, EventArgs.Empty);
							if (raiseIsRunningChanged)
								IsRunningChanged?.Invoke(this, EventArgs.Empty);
							if (addedDebugTags.Length > 0)
								DebugTagsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<string>(addedDebugTags, added: true));
						});

						engine.Message += DbgEngine_Message;
						engine.Start(options);
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

		void ExecOnDbgThread(Action action) => DispatcherThread.BeginInvoke(action);

		bool IsOurEngine(DbgEngine engine) {
			lock (lockObj) {
				foreach (var info in engines) {
					if (info.Engine == engine)
						return true;
				}
			}
			return false;
		}

		void DbgEngine_Message(object sender, DbgEngineMessage e) {
			if (sender == null)
				throw new ArgumentNullException(nameof(sender));
			if (e == null)
				throw new ArgumentNullException(nameof(e));
			var engine = sender as DbgEngine;
			if (engine == null)
				throw new ArgumentOutOfRangeException(nameof(sender));
			ExecOnDbgThread(() => DbgEngine_Message_DbgThread(engine, e));
		}

		void DbgEngine_Message_DbgThread(DbgEngine engine, DbgEngineMessage e) {
			DispatcherThread.VerifyAccess();
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

			default:
				//TODO:
				break;
			}
		}

		EngineInfo TryGetEngineInfo_NoLock(DbgEngine engine) {
			foreach (var info in engines) {
				if (info.Engine == engine)
					return info;
			}
			return null;
		}

		EngineInfo GetEngineInfo_NoLock(DbgEngine engine) {
			var info = TryGetEngineInfo_NoLock(engine);
			if (info == null)
				throw new InvalidOperationException("Unknown debug engine");
			return info;
		}

		DbgProcessImpl GetOrCreateProcess_DbgThread(int pid, DbgStartKind startKind) {
			DispatcherThread.VerifyAccess();
			DbgProcessImpl process;
			lock (lockObj) {
				foreach (var p in processes) {
					if (p.Id == pid)
						return p;
				}
				bool shouldDetach = startKind == DbgStartKind.Attach;
				process = new DbgProcessImpl(this, pid, CalculateProcessState(null), shouldDetach);
				processes.Add(process);
			}
			ProcessesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgProcess>(process, added: true));
			return process;
		}

		void OnConnected_DbgThread(DbgEngine engine, DbgMessageConnected e) {
			DispatcherThread.VerifyAccess();
			if (e.ErrorMessage != null) {
				//TODO: Show error msg
				OnDisconnected_DbgThread(engine);
				return;
			}

			var process = GetOrCreateProcess_DbgThread(e.ProcessId, engine.StartKind);

			DbgProcessState processState;
			lock (lockObj) {
				var info = GetEngineInfo_NoLock(engine);
				info.Process = process;
				info.EngineState = EngineState.Running;
				processState = CalculateProcessState(info.Process);
				breakAllHelper?.OnConnected_DbgThread_NoLock(info);
			}
			process.Add_DbgThread(engine, engine.CreateRuntime(process), processState);
		}

		DbgProcessState CalculateProcessState(DbgProcess process) {
			lock (lockObj) {
				int count = 0;
				if (process != null) {
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
			DispatcherThread.VerifyAccess();
			OnDisconnected_DbgThread(engine);
		}

		void OnDisconnected_DbgThread(DbgEngine engine) {
			DispatcherThread.VerifyAccess();
			DbgProcessImpl processToDispose = null;
			DbgRuntime runtime = null;
			bool raiseIsDebuggingChanged, raiseIsRunningChanged;
			string[] removedDebugTags;
			DbgProcessImpl process;
			DbgDebuggingContext debuggingContextToClose = null;
			lock (lockObj) {
				var oldIsRunning = cachedIsRunning;
				var info = TryGetEngineInfo_NoLock(engine);
				if (info != null)
					engines.Remove(info);
				process = info?.Process;
				raiseIsDebuggingChanged = engines.Count == 0;
				if (raiseIsDebuggingChanged) {
					Debug.Assert(debuggingContext != null);
					debuggingContextToClose = debuggingContext;
					debuggingContext = null;
					restartOptions.Clear();
				}
				removedDebugTags = debugTags.Remove(info.DebugTags);
				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunningChanged = oldIsRunning != cachedIsRunning;

				engine.Message -= DbgEngine_Message;
				if (process != null) {
					var pinfo = process.Remove_DbgThread(engine);
					runtime = pinfo.runtime;
					Debug.Assert(runtime != null);

					if (!pinfo.hasMoreRuntimes) {
						bool disposeProcess;
						disposeProcess = process.ExecuteLockedIfNoMoreRuntimes(() => processes.Remove(process), false);
						if (disposeProcess)
							processToDispose = process;
					}
				}

				breakAllHelper?.OnDisconnected_DbgThread_NoLock(engine);
			}

			process?.NotifyRuntimesChanged_DbgThread(runtime);

			if (processToDispose != null) {
				processToDispose.UpdateState_DbgThread(DbgProcessState.Terminated);
				ProcessesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgProcess>(processToDispose, added: false));
			}

			engine.Close(DispatcherThread);
			runtime?.Close(DispatcherThread);
			processToDispose?.Close(DispatcherThread);

			// Raise them in reverse order (see Start())
			if (removedDebugTags.Length != 0)
				DebugTagsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<string>(removedDebugTags, added: false));
			if (raiseIsRunningChanged)
				IsRunningChanged?.Invoke(this, EventArgs.Empty);
			if (raiseIsDebuggingChanged)
				IsDebuggingChanged?.Invoke(this, EventArgs.Empty);
			debuggingContextToClose?.Close(DispatcherThread);
		}

		public override bool CanRestart {
			get {
				lock (lockObj) {
					if (breakAllHelper != null)
						return false;
					if (stopDebuggingHelper != null)
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
						Debug.Assert(stopDebuggingHelper != null);
						stopDebuggingHelper = null;
					}
					if (success) {
						foreach (var options in restartOptionsCopy)
							Start(options);
					}
					else {
						//TODO: Notify user that it timed out
					}
				});
			}
			stopDebuggingHelper.Initialize();
		}
		StopDebuggingHelper stopDebuggingHelper;

		public override void BreakAll() {
			lock (lockObj) {
				if (breakAllHelper != null)
					return;
				if (stopDebuggingHelper != null)
					return;
				breakAllHelper = new BreakAllHelper(this);
				breakAllHelper.Start_NoLock();
			}
		}
		BreakAllHelper breakAllHelper;

		void OnBreak_DbgThread(DbgEngine engine, DbgMessageBreak e) {
			DispatcherThread.VerifyAccess();
			DbgProcessState processState;
			DbgProcessImpl process;
			lock (lockObj) {
				var info = TryGetEngineInfo_NoLock(engine);
				// It could've been disconnected
				if (info == null)
					return;

				if (e.ErrorMessage != null) {
					//TODO: Log the error
				}
				else
					info.EngineState = EngineState.Paused;
				processState = CalculateProcessState(info.Process);
				process = info.Process;
			}
			breakAllHelper?.OnBreak_DbgThread(engine);
			process.UpdateState_DbgThread(processState);
		}

		void BreakCompleted_DbgThread(bool success) {
			DispatcherThread.VerifyAccess();
			bool raiseIsRunning;
			lock (lockObj) {
				var oldIsRunning = cachedIsRunning;
				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunning = oldIsRunning != cachedIsRunning;
			}
			if (raiseIsRunning)
				IsRunningChanged?.Invoke(this, EventArgs.Empty);
		}

		public override void RunAll() =>
			ExecOnDbgThread(() => RunAll_DbgThread());

		void RunAll_DbgThread() {
			DispatcherThread.VerifyAccess();
			bool raiseIsRunning;
			var processes = new List<DbgProcessImpl>();
			lock (lockObj) {
				var oldIsRunning = cachedIsRunning;

				// If we're trying to break the processes, don't do a thing
				if (breakAllHelper != null)
					return;

				// Make a copy of it in the unlikely event that an engine gets disconnected
				// when we call Run() inside the lock
				foreach (var info in engines.ToArray()) {
					if (info.EngineState == EngineState.Paused) {
						if (!processes.Contains(info.Process))
							processes.Add(info.Process);
						info.EngineState = EngineState.Running;
						info.Engine.Run();
					}
				}

				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunning = oldIsRunning != cachedIsRunning;
			}
			if (raiseIsRunning)
				IsRunningChanged?.Invoke(this, EventArgs.Empty);
			foreach (var process in processes)
				process.UpdateState_DbgThread(CalculateProcessState(process));
		}

		public override void StopDebuggingAll() {
			lock (lockObj) {
				// Make a copy of it in the unlikely event that an engine gets disconnected
				// when we call Terminate()/Detach() inside the lock
				foreach (var info in engines.ToArray()) {
					if (info.Process == null)
						continue;
					if (info.Process.ShouldDetach)
						info.Engine.Detach();
					else
						info.Engine.Terminate();
				}
			}
		}

		public override void TerminateAll() {
			lock (lockObj) {
				// Make a copy of it in the unlikely event that an engine gets disconnected
				// when we call Terminate() inside the lock
				foreach (var info in engines.ToArray())
					info.Engine.Terminate();
			}
		}

		public override void DetachAll() {
			lock (lockObj) {
				// Make a copy of it in the unlikely event that an engine gets disconnected
				// when we call Detach() inside the lock
				foreach (var info in engines.ToArray())
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

		internal void Detach(DbgProcessImpl process) {
			lock (lockObj) {
				// Make a copy of it in the unlikely event that an engine gets disconnected
				// when we call Detach() inside the lock
				foreach (var info in engines.ToArray()) {
					if (info.Process == process)
						info.Engine.Detach();
				}
			}
		}

		internal void Terminate(DbgProcessImpl process) {
			lock (lockObj) {
				// Make a copy of it in the unlikely event that an engine gets disconnected
				// when we call Terminate() inside the lock
				foreach (var info in engines.ToArray()) {
					if (info.Process == process)
						info.Engine.Terminate();
				}
			}
		}
	}
}
