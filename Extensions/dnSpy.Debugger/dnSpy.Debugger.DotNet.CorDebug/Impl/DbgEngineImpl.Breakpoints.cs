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
using System.Diagnostics;
using System.Linq;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.CorDebug.Code;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	abstract partial class DbgEngineImpl {
		sealed class BoundBreakpointData : IDisposable {
			public DnCodeBreakpoint Breakpoint { get; }
			public ModuleId Module { get; }
			public DbgEngineBoundCodeBreakpoint EngineBoundCodeBreakpoint { get; set; }
			public DbgEngineImpl Engine { get; }
			public BoundBreakpointData(DbgEngineImpl engine, ModuleId module, DnCodeBreakpoint breakpoint) {
				EngineBoundCodeBreakpoint = null!;
				Engine = engine ?? throw new ArgumentNullException(nameof(engine));
				Module = module;
				Breakpoint = breakpoint ?? throw new ArgumentNullException(nameof(breakpoint));
				Breakpoint.ErrorChanged += Breakpoint_ErrorChanged;
			}
			void Breakpoint_ErrorChanged(object? sender, EventArgs e) =>
				EngineBoundCodeBreakpoint.UpdateMessage(GetBoundBreakpointMessage(Breakpoint));
			public void Dispose() {
				Breakpoint.ErrorChanged -= Breakpoint_ErrorChanged;
				Engine.RemoveBreakpoint(this);
			}
		}

		void SendCodeBreakpointHitMessage_CorDebug(DnCodeBreakpoint breakpoint, DbgThread? thread) {
			debuggerThread.VerifyAccess();
			var bpData = (BoundBreakpointData?)breakpoint.Tag;
			Debug2.Assert(!(bpData is null));
			if (!(bpData is null))
				SendMessage(new DbgMessageBreakpoint(bpData.EngineBoundCodeBreakpoint.BoundCodeBreakpoint, thread, GetMessageFlags()));
			else
				SendMessage(new DbgMessageBreak(thread, GetMessageFlags()));
		}

		void RemoveBreakpoint(BoundBreakpointData bpData) {
			lock (lockObj) {
				pendingBreakpointsToRemove.Add(bpData.Breakpoint);
				if (pendingBreakpointsToRemove.Count == 1)
					CorDebugThread(() => RemoveBreakpoints_CorDebug());
			}
		}
		readonly List<DnBreakpoint> pendingBreakpointsToRemove = new List<DnBreakpoint>();

		void RemoveBreakpoints_CorDebug() {
			debuggerThread.VerifyAccess();
			DnBreakpoint[] breakpointsToRemove;
			lock (lockObj) {
				breakpointsToRemove = pendingBreakpointsToRemove.Count == 0 ? Array.Empty<DnBreakpoint>() : pendingBreakpointsToRemove.ToArray();
				pendingBreakpointsToRemove.Clear();
			}
			foreach (var bp in breakpointsToRemove)
				dnDebugger.RemoveBreakpoint(bp);
		}

		static Dictionary<ModuleId, List<DbgDotNetCodeLocation>> CreateDotNetCodeLocationDictionary(DbgCodeLocation[] locations) {
			var dict = new Dictionary<ModuleId, List<DbgDotNetCodeLocation>>();
			foreach (var location in locations) {
				if (location.IsClosed)
					continue;
				if (location is DbgDotNetCodeLocation loc) {
					if (!dict.TryGetValue(loc.Module, out var list))
						dict.Add(loc.Module, list = new List<DbgDotNetCodeLocation>());
					list.Add(loc);
				}
			}
			return dict;
		}

		Dictionary<ModuleId, List<DbgDotNetNativeCodeLocationImpl>> CreateDotNetNativeCodeLocationDictionary(DbgCodeLocation[] locations) {
			var dict = new Dictionary<ModuleId, List<DbgDotNetNativeCodeLocationImpl>>();
			foreach (var location in locations) {
				if (location.IsClosed)
					continue;
				if (location is DbgDotNetNativeCodeLocationImpl loc) {
					if (loc.CorCode.Object is null || loc.CorCode.Engine != this)
						continue;
					if (!dict.TryGetValue(loc.Module, out var list))
						dict.Add(loc.Module, list = new List<DbgDotNetNativeCodeLocationImpl>());
					list.Add(loc);
				}
			}
			return dict;
		}

		public override void AddBreakpoints(DbgModule[] modules, DbgCodeLocation[] locations, bool includeNonModuleBreakpoints) =>
			CorDebugThread(() => AddBreakpointsCore(modules, locations, includeNonModuleBreakpoints));
		void AddBreakpointsCore(DbgModule[] modules, DbgCodeLocation[] locations, bool includeNonModuleBreakpoints) {
			debuggerThread.VerifyAccess();

			var dict = CreateDotNetCodeLocationDictionary(locations);
			var nativeDict = CreateDotNetNativeCodeLocationDictionary(locations);
			var createdBreakpoints = new List<DbgBoundCodeBreakpointInfo<BoundBreakpointData>>();
			foreach (var module in modules) {
				if (!TryGetModuleData(module, out var data))
					continue;
				if (dict.TryGetValue(data.ModuleId, out var moduleLocations)) {
					foreach (var location in moduleLocations) {
						var ilbp = dnDebugger.CreateBreakpoint(location.Module.ToDnModuleId(), location.Token, location.Offset, null);
						const ulong address = DbgObjectFactory.BoundBreakpointNoAddress;
						var msg = GetBoundBreakpointMessage(ilbp);
						var bpData = new BoundBreakpointData(this, location.Module, ilbp);
						createdBreakpoints.Add(new DbgBoundCodeBreakpointInfo<BoundBreakpointData>(location, module, address, msg, bpData));
					}
				}
				if (nativeDict.TryGetValue(data.ModuleId, out var nativeModuleLocations)) {
					foreach (var location in nativeModuleLocations) {
						if (!(location.CorCode.Object is CorCode code))
							continue;
						var nbp = dnDebugger.CreateNativeBreakpoint(code, (uint)location.NativeAddress.Offset, null);
						var address = location.NativeAddress.IP;
						var msg = GetBoundBreakpointMessage(nbp);
						var bpData = new BoundBreakpointData(this, location.Module, nbp);
						createdBreakpoints.Add(new DbgBoundCodeBreakpointInfo<BoundBreakpointData>(location, module, address, msg, bpData));
					}
				}
			}
			var boundBreakpoints = objectFactory.Create(createdBreakpoints.ToArray());
			foreach (var ebp in boundBreakpoints) {
				if (!ebp.BoundCodeBreakpoint.TryGetData(out BoundBreakpointData? bpData)) {
					Debug.Assert(ebp.BoundCodeBreakpoint.IsClosed);
					continue;
				}
				bpData.EngineBoundCodeBreakpoint = ebp;
				bpData.Breakpoint.Tag = bpData;
			}
		}

		static DbgEngineBoundCodeBreakpointMessage GetBoundBreakpointMessage(DnCodeBreakpoint bp) {
			switch (bp.Error) {
			case DnCodeBreakpointError.None:
				return DbgEngineBoundCodeBreakpointMessage.CreateNoError();
			case DnCodeBreakpointError.FunctionNotFound:
				return DbgEngineBoundCodeBreakpointMessage.CreateFunctionNotFound(GetFunctionName(bp));
			case DnCodeBreakpointError.OtherError:
			case DnCodeBreakpointError.CouldNotCreateBreakpoint:
				return DbgEngineBoundCodeBreakpointMessage.CreateCouldNotCreateBreakpoint();
			default:
				Debug.Fail($"Unknown error: {bp.Error}");
				goto case DnCodeBreakpointError.OtherError;
			}

			static string GetFunctionName(DnCodeBreakpoint cbp) => $"0x{cbp.Token:X8} ({cbp.Module.ModuleName})";
		}

		Dictionary<ModuleId, List<BoundBreakpointData>> CreateBoundBreakpointsDictionary(DbgBoundCodeBreakpoint[] boundBreakpoints) {
			var dict = new Dictionary<ModuleId, List<BoundBreakpointData>>();
			foreach (var bound in boundBreakpoints) {
				if (!bound.TryGetData(out BoundBreakpointData? bpData) || bpData.Engine != this)
					continue;
				if (!dict.TryGetValue(bpData.Module, out var list))
					dict.Add(bpData.Module, list = new List<BoundBreakpointData>());
				list.Add(bpData);
			}
			return dict;
		}

		public override void RemoveBreakpoints(DbgModule[] modules, DbgBoundCodeBreakpoint[] boundBreakpoints, bool includeNonModuleBreakpoints) {
			var dict = CreateBoundBreakpointsDictionary(boundBreakpoints);
			var bpsToRemove = new List<BoundBreakpointData>();
			foreach (var module in modules) {
				if (!TryGetModuleData(module, out var data))
					continue;
				if (!dict.TryGetValue(data.ModuleId, out var bpDataList))
					continue;
				bpsToRemove.AddRange(bpDataList);
			}
			if (bpsToRemove.Count > 0)
				bpsToRemove[0].EngineBoundCodeBreakpoint.Remove(bpsToRemove.Select(a => a.EngineBoundCodeBreakpoint).ToArray());
		}

		internal DnNativeCodeBreakpoint CreateNativeBreakpointForGetReturnValue(CorCode code, uint offset, Action<CorThread?> callback) {
			debuggerThread.VerifyAccess();
			return dnDebugger.CreateNativeBreakpoint(code, offset, ctx => { callback(ctx.E.CorThread); return false; });
		}

		internal void RemoveNativeBreakpointForGetReturnValue(DnNativeCodeBreakpoint breakpoint) {
			debuggerThread.VerifyAccess();
			dnDebugger.RemoveBreakpoint(breakpoint);
		}

		internal DnILCodeBreakpoint CreateBreakpointForStepper(DbgModule module, uint token, uint offset, Func<CorThread?, bool> callback) {
			debuggerThread.VerifyAccess();
			return dnDebugger.CreateBreakpoint(GetModuleId(module).ToDnModuleId(), token, offset, ctx => {
				if (callback(ctx.E.CorThread))
					ctx.E.AddPauseReason(DebuggerPauseReason.AsyncStepperBreakpoint);
				return false;
			});
		}

		internal void RemoveBreakpointForStepper(DnILCodeBreakpoint breakpoint) {
			debuggerThread.VerifyAccess();
			dnDebugger.RemoveBreakpoint(breakpoint);
		}
	}
}
