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
	sealed class DbgManagerImpl : DbgManager {
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

		sealed class EngineInfo {
			public DbgEngine Engine { get; }
			public DbgProcessImpl Process { get; set; }
			public EngineInfo(DbgEngine engine) {
				Engine = engine;
			}
		}

		readonly object lockObj;
		readonly List<EngineInfo> engines;
		readonly Lazy<DbgEngineProvider, IDbgEngineProviderMetadata>[] dbgEngineProviders;

		[ImportingConstructor]
		DbgManagerImpl([ImportMany] IEnumerable<Lazy<DbgEngineProvider, IDbgEngineProviderMetadata>> dbgEngineProviders) {
			lockObj = new object();
			engines = new List<EngineInfo>();
			processes = new List<DbgProcessImpl>();
			this.dbgEngineProviders = dbgEngineProviders.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public override string Start(StartDebuggingOptions options) {
			try {
				foreach (var lz in dbgEngineProviders) {
					var engine = lz.Value.Start(options);
					if (engine != null) {
						bool raiseEvent;
						lock (lockObj) {
							raiseEvent = engines.Count == 0;
							engines.Add(new EngineInfo(engine));
						}
						if (raiseEvent)
							IsDebuggingChanged?.Invoke(this, EventArgs.Empty);
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
				//TODO: Show error msg, clean up
				return;
			}

			var process = GetOrCreateProcess(e.ProcessId);

			lock (lockObj) {
				var info = GetEngineInfo_NoLock(engine);
				info.Process = process;
			}
			process.Add(engine, engine.CreateRuntime(process));
		}

		void OnDisconnected(DbgEngine engine, DbgMessageDisconnected e) {
			//TODO:
		}
	}
}
