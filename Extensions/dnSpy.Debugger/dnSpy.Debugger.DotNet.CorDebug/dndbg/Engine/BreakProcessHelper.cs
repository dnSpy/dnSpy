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
using System.Diagnostics;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dndbg.Engine {
	sealed class BreakProcessHelper {
		readonly DnDebugger debugger;
		readonly BreakProcessKind type;
		DnBreakpoint? breakpoint;

		public BreakProcessHelper(DnDebugger debugger, BreakProcessKind type) {
			this.debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
			this.type = type;
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
				Debug.Fail($"Unknown BreakProcessKind: {type}");
				break;
			}
		}

		void SetILBreakpoint(DnModuleId moduleId, uint token) {
			Debug2.Assert(token != 0 && breakpoint is null);
			DnBreakpoint? bp = null;
			bp = debugger.CreateBreakpoint(moduleId, token, 0, ctx2 => {
				debugger.RemoveBreakpoint(bp!);
				ctx2.E.AddPauseState(new EntryPointBreakpointPauseState(ctx2.E.CorAppDomain, ctx2.E.CorThread));
				return false;
			});
		}

		bool OnLoadModule(DebugEventBreakpointConditionContext ctx) {
			var lmArgs = (LoadModuleDebugCallbackEventArgs)ctx.EventArgs;
			var mod = lmArgs.CorModule;
			if (mod is null || mod.IsDynamic || mod.IsInMemory)
				return false;
			var filename = mod.Name;
			uint epToken = GetEntryPointToken(filename);
			if ((Table)(epToken >> 24) != Table.Method || (epToken & 0x00FFFFFF) == 0)
				return false;

			debugger.RemoveBreakpoint(breakpoint!);
			breakpoint = null;
			Debug.Assert(!mod.IsDynamic && !mod.IsInMemory);
			// It's not a dyn/in-mem module so id isn't used
			var moduleId = mod.GetModuleId(uint.MaxValue);
			SetILBreakpoint(moduleId, epToken);
			return false;
		}

		static uint GetEntryPointToken(string? filename) {
			try {
				using (var peImage = new PEImage(filename)) {
					var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
					if (dotNetDir.VirtualAddress == 0)
						return 0;
					if (dotNetDir.Size < 0x48)
						return 0;
					var cor20HeaderReader = peImage.CreateReader(dotNetDir.VirtualAddress, 0x48);
					var cor20Header = new ImageCor20Header(ref cor20HeaderReader, true);
					if ((cor20Header.Flags & ComImageFlags.NativeEntryPoint) != 0)
						return 0;
					uint token = cor20Header.EntryPointToken_or_RVA;
					if ((Table)(token >> 24) == Table.Method && (token & 0x00FFFFFF) != 0)
						return token;
				}
			}
			catch {
			}
			return 0;
		}
	}
}
