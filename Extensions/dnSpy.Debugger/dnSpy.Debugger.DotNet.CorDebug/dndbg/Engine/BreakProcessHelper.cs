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
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnlib.PE;

namespace dndbg.Engine {
	sealed class BreakProcessHelper {
		readonly DnDebugger debugger;
		readonly BreakProcessKind type;
		readonly string filename;
		DnBreakpoint breakpoint;

		public BreakProcessHelper(DnDebugger debugger, BreakProcessKind type, string filename) {
			this.debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
			this.type = type;
			this.filename = filename;
			AddStartupBreakpoint();
		}

		void AddStartupBreakpoint() {
			switch (type) {
			case BreakProcessKind.None:
				break;

			case BreakProcessKind.EntryPoint:
				breakpoint = debugger.CreateBreakpoint(DebugEventBreakpointKind.LoadModule, OnLoadModule);
				break;

			default:
				Debug.Fail(string.Format("Unknown BreakProcessKind: {0}", type));
				break;
			}
		}

		void CreateStartupDebugBreakEvent(DebugEventBreakpointKind evt, Func<DebugEventBreakpointConditionContext, bool> cond = null) {
			Debug.Assert(debugger.ProcessState == DebuggerProcessState.Starting);
			DnDebugEventBreakpoint bp = null;
			bp = debugger.CreateBreakpoint(evt, ctx => {
				if (cond == null || cond(ctx)) {
					debugger.RemoveBreakpoint(bp);
					return true;
				}
				return false;
			});
		}

		void CreateStartupAnyDebugBreakEvent(Func<AnyDebugEventBreakpointConditionContext, bool> cond = null) {
			Debug.Assert(debugger.ProcessState == DebuggerProcessState.Starting);
			DnAnyDebugEventBreakpoint bp = null;
			bp = debugger.CreateAnyDebugEventBreakpoint(ctx => {
				if (cond == null || cond(ctx)) {
					debugger.RemoveBreakpoint(bp);
					return true;
				}
				return false;
			});
		}

		bool IsOurModule(CorModule module) => IsModule(module, filename);
		static bool IsModule(CorModule module, string filename) => module != null && !module.IsDynamic && !module.IsInMemory && StringComparer.OrdinalIgnoreCase.Equals(module.Name, filename);

		void SetILBreakpoint(in DnModuleId moduleId, uint token) {
			Debug.Assert(token != 0 && breakpoint == null);
			DnBreakpoint bp = null;
			bp = debugger.CreateBreakpoint(moduleId, token, 0, ctx2 => {
				debugger.RemoveBreakpoint(bp);
				ctx2.E.AddPauseState(new EntryPointBreakpointPauseState(ctx2.E.CorAppDomain, ctx2.E.CorThread));
				return false;
			});
		}

		bool OnLoadModule(DebugEventBreakpointConditionContext ctx) {
			var lmArgs = (LoadModuleDebugCallbackEventArgs)ctx.EventArgs;
			var mod = lmArgs.CorModule;
			if (!IsOurModule(mod))
				return false;
			debugger.RemoveBreakpoint(breakpoint);
			breakpoint = null;
			Debug.Assert(!mod.IsDynamic && !mod.IsInMemory);
			// It's not a dyn/in-mem module so id isn't used
			var moduleId = mod.GetModuleId(uint.MaxValue);

			uint epToken = GetEntryPointToken(filename, out string otherModuleName);
			if (epToken != 0) {
				if ((Table)(epToken >> 24) == Table.Method) {
					SetILBreakpoint(moduleId, epToken);
					return false;
				}

				if (otherModuleName != null) {
					Debug.Assert((Table)(epToken >> 24) == Table.File);
					otherModuleFullName = GetOtherModuleFullName(otherModuleName);
					if (otherModuleFullName != null) {
						thisAssembly = mod.Assembly;
						breakpoint = debugger.CreateBreakpoint(DebugEventBreakpointKind.LoadModule, OnLoadOtherModule);
						return false;
					}
				}
			}

			// Failed to set BP. Break to debugger.
			return true;
		}
		CorAssembly thisAssembly;
		string otherModuleFullName;

		bool OnLoadOtherModule(DebugEventBreakpointConditionContext ctx) {
			var lmArgs = (LoadModuleDebugCallbackEventArgs)ctx.EventArgs;
			var mod = lmArgs.CorModule;
			if (!IsModule(mod, otherModuleFullName) || mod.Assembly != thisAssembly)
				return false;
			debugger.RemoveBreakpoint(breakpoint);
			breakpoint = null;

			uint epToken = GetEntryPointToken(otherModuleFullName, out string otherModuleName);
			if (epToken != 0 && (Table)(epToken >> 24) == Table.Method) {
				Debug.Assert(!mod.IsDynamic && !mod.IsInMemory);
				// It's not a dyn/in-mem module so id isn't used
				SetILBreakpoint(mod.GetModuleId(uint.MaxValue), epToken);
				return false;
			}

			return true;
		}

		string GetOtherModuleFullName(string name) {
			try {
				return Path.Combine(Path.GetDirectoryName(filename), name);
			}
			catch {
			}
			return null;
		}

		static uint GetEntryPointToken(string filename, out string otherModuleName) {
			otherModuleName = null;
			IImageStream cor20HeaderStream = null;
			try {
				using (var peImage = new PEImage(filename)) {
					var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
					if (dotNetDir.VirtualAddress == 0)
						return 0;
					if (dotNetDir.Size < 0x48)
						return 0;
					var cor20Header = new ImageCor20Header(cor20HeaderStream = peImage.CreateStream(dotNetDir.VirtualAddress, 0x48), true);
					if ((cor20Header.Flags & ComImageFlags.NativeEntryPoint) != 0)
						return 0;
					uint token = cor20Header.EntryPointToken_or_RVA;
					if ((Table)(token >> 24) != Table.File)
						return token;

					using (var mod = ModuleDefMD.Load(peImage)) {
						var file = mod.ResolveFile(token & 0x00FFFFFF);
						if (file == null || !file.ContainsMetaData)
							return 0;

						otherModuleName = file.Name;
						return token;
					}
				}
			}
			catch {
			}
			finally {
				if (cor20HeaderStream != null)
					cor20HeaderStream.Dispose();
			}
			return 0;
		}
	}
}
