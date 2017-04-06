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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.Breakpoints.Code;

namespace dnSpy.Debugger.Impl {
	sealed partial class DbgManagerImpl {
		sealed class BoundBreakpointsManager {
			readonly DbgManagerImpl owner;
			BoundCodeBreakpointsService BoundCodeBreakpointsService => owner.boundCodeBreakpointsService.Value;
			DispatcherThread DispatcherThread => owner.DispatcherThread;

			public BoundBreakpointsManager(DbgManagerImpl owner) => this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

			internal void Initialize() {
				BoundCodeBreakpointsService.BreakpointsChanged += BoundCodeBreakpointsService_BreakpointsChanged;
				BoundCodeBreakpointsService.BreakpointsModified += BoundCodeBreakpointsService_BreakpointsModified;
			}

			bool IsOurEngine(DbgEngine engine) => owner.IsOurEngine(engine);

			internal void RemoveAllBoundBreakpoints_DbgThread(DbgRuntime runtime) {
				DispatcherThread.VerifyAccess();
				var boundBreakpoints = BoundCodeBreakpointsService.RemoveBoundBreakpoints_DbgThread(runtime);
				foreach (var bp in boundBreakpoints)
					bp.Close(DispatcherThread);
			}

			internal void RemoveBoundCodeBreakpoints_DbgThread(DbgRuntimeImpl runtime, DbgEngineBoundCodeBreakpointImpl[] breakpoints) {
				DispatcherThread.VerifyAccess();
				Debug.Assert(IsOurEngine(runtime.Engine));
				if (!owner.IsOurEngine(runtime.Engine)) {
					foreach (var bp in breakpoints)
						bp.BoundCodeBreakpoint.Close(DispatcherThread);
					return;
				}
				var bps = breakpoints.Select(a => a.BoundCodeBreakpoint).ToArray();
				BoundCodeBreakpointsService.RemoveBoundBreakpoints_DbgThread(bps);
				foreach (var bp in bps)
					bp.Close(DispatcherThread);
			}

			internal void AddBoundCodeBreakpoints_DbgThread(DbgRuntimeImpl runtime, DbgEngineBoundCodeBreakpointImpl[] breakpoints) {
				DispatcherThread.VerifyAccess();
				Debug.Assert(IsOurEngine(runtime.Engine));
				if (!IsOurEngine(runtime.Engine)) {
					foreach (var bp in breakpoints)
						bp.BoundCodeBreakpoint.Close(DispatcherThread);
					return;
				}

				var bpsToKeep = new List<DbgBoundCodeBreakpoint>(breakpoints.Length);
				foreach (var bp in breakpoints) {
					var bound = bp.BoundCodeBreakpoint;
					if (bound.Runtime.IsClosed || bound.Module?.IsClosed == true || bound.Breakpoint.IsClosed)
						bound.Close(DispatcherThread);
					else
						bpsToKeep.Add(bound);
				}

				if (bpsToKeep.Count > 0) {
					var objsToClose = BoundCodeBreakpointsService.AddBoundBreakpoints_DbgThread(bpsToKeep);
					foreach (var bp in objsToClose)
						bp.Close(DispatcherThread);
				}
			}

			void BoundCodeBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) {
				DispatcherThread.VerifyAccess();
				if (e.Added)
					AddBoundBreakpoints_DbgThread(e.Objects);
				else
					RemoveBoundBreakpoints_DbgThread(e.Objects);
			}

			void BoundCodeBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) {
				DispatcherThread.VerifyAccess();
				if (!owner.IsDebugging)
					return;
				List<DbgCodeBreakpoint> newEnabledBreakpoints = null;
				List<DbgCodeBreakpoint> newDisabledBreakpoints = null;
				foreach (var info in e.Breakpoints) {
					var oldIsEnabled = info.OldSettings.IsEnabled;
					var newIsEnabled = info.Breakpoint.Settings.IsEnabled;
					if (oldIsEnabled == newIsEnabled)
						continue;
					if (newIsEnabled) {
						if (newEnabledBreakpoints == null)
							newEnabledBreakpoints = new List<DbgCodeBreakpoint>();
						newEnabledBreakpoints.Add(info.Breakpoint);
					}
					else {
						if (newDisabledBreakpoints == null)
							newDisabledBreakpoints = new List<DbgCodeBreakpoint>();
						newDisabledBreakpoints.Add(info.Breakpoint);
					}
				}
				if (newDisabledBreakpoints != null)
					RemoveBoundBreakpoints_DbgThread(newDisabledBreakpoints);
				if (newEnabledBreakpoints != null)
					AddBoundBreakpoints_DbgThread(newEnabledBreakpoints);
			}

			List<DbgModule> GetAllModules() {
				var modules = new List<DbgModule>();
				lock (owner.lockObj) {
					foreach (var info in owner.engines)
						modules.AddRange(info.Runtime?.Modules ?? Array.Empty<DbgModule>());
				}
				return modules;
			}

			void AddBoundBreakpoints_DbgThread(IList<DbgCodeBreakpoint> breakpoints) {
				DispatcherThread.VerifyAccess();
				if (!owner.IsDebugging)
					return;
				AddBoundBreakpoints_DbgThread(GetAllModules(), breakpoints);
			}

			void RemoveBoundBreakpoints_DbgThread(IList<DbgCodeBreakpoint> breakpoints) {
				DispatcherThread.VerifyAccess();
				if (!owner.IsDebugging)
					return;
				RemoveBoundBreakpoints_DbgThread(GetAllModules(), breakpoints);
			}

			internal void AddBoundBreakpoints_DbgThread(IList<DbgModule> modules) {
				DispatcherThread.VerifyAccess();
				AddBoundBreakpoints_DbgThread(modules, BoundCodeBreakpointsService.Breakpoints);
			}

			internal void RemoveBoundBreakpoints_DbgThread(IList<DbgModule> modules) {
				DispatcherThread.VerifyAccess();
				RemoveBoundBreakpoints_DbgThread(modules, BoundCodeBreakpointsService.Breakpoints);
			}

			Dictionary<DbgEngine, List<DbgModule>> GetEngineModules(IList<DbgModule> modules) {
				var dict = new Dictionary<DbgEngine, List<DbgModule>>();
				lock (owner.lockObj) {
					DbgRuntime lastRuntime = null;
					List<DbgModule> lastList = null;
					foreach (var module in modules) {
						var runtime = module.Runtime;
						if (runtime != lastRuntime) {
							var engine = GetEngine_NoLock(runtime);
							Debug.Assert(engine != null);
							if (engine == null)
								continue;
							lastRuntime = runtime;
							if (!dict.TryGetValue(engine, out lastList))
								dict.Add(engine, lastList = new List<DbgModule>());
						}
						lastList.Add(module);
					}
				}
				return dict;

				DbgEngine GetEngine_NoLock(DbgRuntime runtime) {
					foreach (var info in owner.engines) {
						if (info.Runtime == runtime)
							return info.Engine;
					}
					return null;
				}
			}

			void AddBoundBreakpoints_DbgThread(IList<DbgModule> modules, IList<DbgCodeBreakpoint> breakpoints) {
				DispatcherThread.VerifyAccess();
				if (modules.Count == 0 || breakpoints.Count == 0)
					return;
				var locations = breakpoints.Where(a => a.IsEnabled).Select(a => a.Location).ToArray();
				if (locations.Length == 0)
					return;
				foreach (var kv in GetEngineModules(modules)) {
					var engine = kv.Key;
					var engineModules = kv.Value;
					engine.AddBreakpoints(engineModules.ToArray(), locations);
				}
			}

			void RemoveBoundBreakpoints_DbgThread(IList<DbgModule> modules, IList<DbgCodeBreakpoint> breakpoints) {
				DispatcherThread.VerifyAccess();
				if (modules.Count == 0 || breakpoints.Count == 0)
					return;
				var boundBreakpoints = breakpoints.SelectMany(a => a.BoundBreakpoints).ToArray();
				foreach (var kv in GetEngineModules(modules))
					kv.Key.RemoveBreakpoints(kv.Value.ToArray(), boundBreakpoints);
			}

			internal void ReAddBreakpoints_DbgThread(DbgModule[] modules) {
				DispatcherThread.VerifyAccess();
				var breakpoints = BoundCodeBreakpointsService.Breakpoints;
				RemoveBoundBreakpoints_DbgThread(modules, breakpoints);
				AddBoundBreakpoints_DbgThread(modules, breakpoints);
			}
		}

		internal void RemoveBoundCodeBreakpoints_DbgThread(DbgRuntimeImpl runtime, DbgEngineBoundCodeBreakpointImpl[] breakpoints) =>
			boundBreakpointsManager.RemoveBoundCodeBreakpoints_DbgThread(runtime, breakpoints);

		internal void AddBoundCodeBreakpoints_DbgThread(DbgRuntimeImpl runtime, DbgEngineBoundCodeBreakpointImpl[] breakpoints) =>
			boundBreakpointsManager.AddBoundCodeBreakpoints_DbgThread(runtime, breakpoints);
	}
}
