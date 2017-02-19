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
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.Impl {
	[Export(typeof(DbgManager))]
	sealed partial class DbgManagerImpl : DbgManager {
		public override event EventHandler<ProcessesChangedEventArgs> ProcessesChanged;
		public override DbgProcess[] Processes {
			get {
				lock (lockObj)
					return processes.ToArray();
			}
		}
		readonly List<DbgProcessImpl> processes;

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
			public EngineInfo(DbgEngine engine) => Engine = engine;
		}

		Dispatcher Dispatcher => debuggerThread.Dispatcher;

		readonly DebuggerThread debuggerThread;
		readonly object lockObj;
		readonly List<EngineInfo> engines;
		readonly Lazy<DbgEngineProvider, IDbgEngineProviderMetadata>[] dbgEngineProviders;
		readonly Lazy<IDbgManagerStartListener, IDbgManagerStartListenerMetadata>[] dbgManagerStartListeners;
		int hasNotifiedStartListenersCounter;

		[ImportingConstructor]
		DbgManagerImpl([ImportMany] IEnumerable<Lazy<DbgEngineProvider, IDbgEngineProviderMetadata>> dbgEngineProviders, [ImportMany] IEnumerable<Lazy<IDbgManagerStartListener, IDbgManagerStartListenerMetadata>> dbgManagerStartListeners) {
			lockObj = new object();
			engines = new List<EngineInfo>();
			processes = new List<DbgProcessImpl>();
			this.dbgEngineProviders = dbgEngineProviders.OrderBy(a => a.Metadata.Order).ToArray();
			this.dbgManagerStartListeners = dbgManagerStartListeners.OrderBy(a => a.Metadata.Order).ToArray();
			debuggerThread = new DebuggerThread();
			debuggerThread.CallDispatcherRun();
		}

		public override string Start(StartDebuggingOptions options) {
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
					var engine = lz.Value.Start(options);
					if (engine != null) {
						bool raiseIsDebuggingChanged, raiseIsRunningChanged;
						lock (lockObj) {
							var oldIsRunning = cachedIsRunning;
							raiseIsDebuggingChanged = engines.Count == 0;
							engines.Add(new EngineInfo(engine));
							cachedIsRunning = CalculateIsRunning_NoLock();
							raiseIsRunningChanged = oldIsRunning != cachedIsRunning;
						}
						if (raiseIsDebuggingChanged)
							IsDebuggingChanged?.Invoke(this, EventArgs.Empty);
						if (raiseIsRunningChanged)
							IsRunningChanged?.Invoke(this, EventArgs.Empty);
						engine.Message += DbgEngine_Message;
						engine.EnableMessages();
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

		void DbgEngine_Message(object sender, DbgEngineMessage e) {
			if (sender == null)
				throw new ArgumentNullException(nameof(sender));
			if (e == null)
				throw new ArgumentNullException(nameof(e));
			var engine = sender as DbgEngine;
			if (engine == null)
				throw new ArgumentOutOfRangeException(nameof(sender));

			switch (e.MessageKind) {
			case DbgEngineMessageKind.Connected:
				OnConnected(engine, (DbgMessageConnected)e);
				break;

			case DbgEngineMessageKind.Disconnected:
				OnDisconnected(engine, (DbgMessageDisconnected)e);
				break;

			case DbgEngineMessageKind.Break:
				OnBreak(engine, (DbgMessageBreak)e);
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

		DbgProcessImpl GetOrCreateProcess(int pid) {
			DbgProcessImpl process;
			lock (lockObj) {
				foreach (var p in processes) {
					if (p.Id == pid)
						return p;
				}
				process = new DbgProcessImpl(this, pid);
				processes.Add(process);
			}
			ProcessesChanged?.Invoke(this, new ProcessesChangedEventArgs(process, added: true));
			return process;
		}

		void OnConnected(DbgEngine engine, DbgMessageConnected e) {
			if (e.ErrorMessage != null) {
				//TODO: Show error msg
				OnDisconnected(engine);
				return;
			}

			var process = GetOrCreateProcess(e.ProcessId);

			lock (lockObj) {
				var info = GetEngineInfo_NoLock(engine);
				info.Process = process;
				info.EngineState = EngineState.Running;
				breakAllHelper?.OnConnected_NoLock(info);
			}
			process.Add(engine, engine.CreateRuntime(process));
		}

		void OnDisconnected(DbgEngine engine, DbgMessageDisconnected e) => OnDisconnected(engine);

		void OnDisconnected(DbgEngine engine) {
			bool raiseIsDebuggingChanged, raiseIsRunningChanged;
			DbgProcessImpl process;
			lock (lockObj) {
				var oldIsRunning = cachedIsRunning;
				var info = TryGetEngineInfo_NoLock(engine);
				if (info != null)
					engines.Remove(info);
				process = info?.Process;
				raiseIsDebuggingChanged = engines.Count == 0;
				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunningChanged = oldIsRunning != cachedIsRunning;
				breakAllHelper?.OnDisconnected_NoLock(engine);
			}
			DisposeEngine(engine, process);
			// Raise them in reverse order (see Start())
			if (raiseIsRunningChanged)
				IsRunningChanged?.Invoke(this, EventArgs.Empty);
			if (raiseIsDebuggingChanged)
				IsDebuggingChanged?.Invoke(this, EventArgs.Empty);
		}

		void DisposeEngine(DbgEngine engine, DbgProcessImpl process) {
			engine.Message -= DbgEngine_Message;
			engine.Close();
			if (process != null) {
				var info = process.Remove(engine);
				Debug.Assert(info.runtime != null);
				info.runtime?.Close();

				if (!info.hasMoreRuntimes) {
					bool disposeProcess;
					lock (lockObj)
						disposeProcess = process.ExecuteLockedIfNoMoreRuntimes(() => processes.Remove(process), false);
					if (disposeProcess) {
						ProcessesChanged?.Invoke(this, new ProcessesChangedEventArgs(process, added: false));
						process.Close();
					}
				}
			}
		}

		public override void BreakAll() {
			lock (lockObj) {
				if (breakAllHelper != null)
					return;
				breakAllHelper = new BreakAllHelper(this);
				breakAllHelper.Start_NoLock();
			}
		}
		BreakAllHelper breakAllHelper;

		void OnBreak(DbgEngine engine, DbgMessageBreak e) {
			lock (lockObj) {
				var info = TryGetEngineInfo_NoLock(engine);
				// It could've been disconnected
				if (info == null)
					return;
				info.EngineState = EngineState.Paused;
			}
			breakAllHelper?.OnBreak(engine);
		}

		void BreakCompleted(bool success) {
			bool raiseIsRunningChanged;
			lock (lockObj) {
				var oldIsRunning = cachedIsRunning;
				cachedIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunningChanged = oldIsRunning != cachedIsRunning;
			}
			if (raiseIsRunningChanged)
				IsRunningChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
