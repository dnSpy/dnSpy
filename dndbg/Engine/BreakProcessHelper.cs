/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dndbg.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnlib.PE;

namespace dndbg.Engine {
	sealed class BreakProcessHelper {
		readonly DnDebugger debugger;
		readonly BreakProcessType type;
		readonly string filename;
		DnBreakpoint breakpoint;

		public BreakProcessHelper(DnDebugger debugger, BreakProcessType type, string filename) {
			if (debugger == null)
				throw new ArgumentNullException();
			this.debugger = debugger;
			this.type = type;
			this.filename = filename;
			AddStartupBreakpoint();
		}

		void AddStartupBreakpoint() {
			switch (type) {
			case BreakProcessType.None:
				break;

			case BreakProcessType.CreateProcess:
				CreateStartupDebugBreakEvent(DebugEventBreakpointType.CreateProcess);
				break;

			case BreakProcessType.CreateAppDomain:
				CreateStartupDebugBreakEvent(DebugEventBreakpointType.CreateAppDomain);
				break;

			case BreakProcessType.CreateThread:
				CreateStartupDebugBreakEvent(DebugEventBreakpointType.CreateThread);
				break;

			case BreakProcessType.LoadModule:
				CreateStartupDebugBreakEvent(DebugEventBreakpointType.LoadModule);
				break;

			case BreakProcessType.LoadClass:
				bool oldLoadClass = debugger.Options.ModuleClassLoadCallbacks;
				debugger.Options.ModuleClassLoadCallbacks = true;
				CreateStartupDebugBreakEvent(DebugEventBreakpointType.LoadClass, ctx => {
					ctx.Debugger.Options.ModuleClassLoadCallbacks = oldLoadClass;
					return true;
				});
				break;

			case BreakProcessType.ExeLoadClass:
				CreateStartupAnyDebugBreakEvent(ctx => {
					if (ctx.EventArgs.Type == DebugCallbackType.LoadModule) {
						var lm = (LoadModuleDebugCallbackEventArgs)ctx.EventArgs;
						var mod = lm.CorModule;
						if (IsOurModule(mod))
							mod.EnableClassLoadCallbacks(true);
					}
					else if (ctx.EventArgs.Type == DebugCallbackType.LoadClass) {
						var lc = (LoadClassDebugCallbackEventArgs)ctx.EventArgs;
						var cls = lc.CorClass;
						var mod = cls == null ? null : cls.Module;
						return IsOurModule(mod);
					}

					return false;
				});
				break;

			case BreakProcessType.ExeLoadModule:
				CreateStartupDebugBreakEvent(DebugEventBreakpointType.LoadModule, ctx => {
					var e = (LoadModuleDebugCallbackEventArgs)ctx.EventArgs;
					var mod = e.CorModule;
					if (mod == null)
						return false;
					return IsOurModule(mod);
				});
				break;

			case BreakProcessType.ModuleCctorOrEntryPoint:
			case BreakProcessType.EntryPoint:
				breakpoint = debugger.CreateBreakpoint(DebugEventBreakpointType.LoadModule, OnLoadModule);
				break;

			default:
				Debug.Fail(string.Format("Unknown BreakProcessType: {0}", type));
				break;
			}
		}

		void CreateStartupDebugBreakEvent(DebugEventBreakpointType evt, Predicate<DebugEventBreakpointConditionContext> cond = null) {
			Debug.Assert(debugger.ProcessState == DebuggerProcessState.Starting);
			DnDebugEventBreakpoint bp = null;
			bp = debugger.CreateBreakpoint(evt, ctx => {
				if (cond == null || cond((DebugEventBreakpointConditionContext)ctx)) {
					debugger.RemoveBreakpoint(bp);
					return true;
				}
				return false;
			});
		}

		void CreateStartupAnyDebugBreakEvent(Predicate<AnyDebugEventBreakpointConditionContext> cond = null) {
			Debug.Assert(debugger.ProcessState == DebuggerProcessState.Starting);
			DnAnyDebugEventBreakpoint bp = null;
			bp = debugger.CreateAnyDebugEventBreakpoint(ctx => {
				if (cond == null || cond((AnyDebugEventBreakpointConditionContext)ctx)) {
					debugger.RemoveBreakpoint(bp);
					return true;
				}
				return false;
			});
		}

		bool IsOurModule(CorModule module) {
			return IsModule(module, filename);
		}

		static bool IsModule(CorModule module, string filename) {
			return module != null && module.SerializedDnModule == new SerializedDnModule(filename);
		}

		void SetILBreakpoint(SerializedDnModuleWithAssembly serModAsm, uint token) {
			Debug.Assert(token != 0 && breakpoint == null);
			DnBreakpoint bp = null;
			bp = debugger.CreateBreakpoint(serModAsm, token, 0, ctx2 => {
				debugger.RemoveBreakpoint(bp);
				return true;
			});
		}

		bool OnLoadModule(BreakpointConditionContext context) {
			var ctx = (DebugEventBreakpointConditionContext)context;
			var lmArgs = (LoadModuleDebugCallbackEventArgs)ctx.EventArgs;
			var mod = lmArgs.CorModule;
			if (!IsOurModule(mod))
				return false;
			debugger.RemoveBreakpoint(breakpoint);
			breakpoint = null;
			var serModAsm = mod.SerializedDnModuleWithAssembly;

			if (type == BreakProcessType.ModuleCctorOrEntryPoint) {
				uint cctorToken = MetaDataUtils.GetGlobalStaticConstructor(mod.GetMetaDataInterface<IMetaDataImport>());
				if (cctorToken != 0) {
					SetILBreakpoint(serModAsm, cctorToken);
					return false;
				}
			}

			string otherModuleName;
			uint epToken = GetEntryPointToken(filename, out otherModuleName);
			if (epToken != 0) {
				if ((Table)(epToken >> 24) == Table.Method) {
					SetILBreakpoint(serModAsm, epToken);
					return false;
				}

				if (otherModuleName != null) {
					Debug.Assert((Table)(epToken >> 24) == Table.File);
					otherModuleFullName = GetOtherModuleFullName(otherModuleName);
					if (otherModuleFullName != null) {
						thisAssembly = mod.Assembly;
						breakpoint = debugger.CreateBreakpoint(DebugEventBreakpointType.LoadModule, OnLoadOtherModule);
						return false;
					}
				}
			}

			// Failed to set BP. Break to debugger.
			return true;
		}
		CorAssembly thisAssembly;
		string otherModuleFullName;

		bool OnLoadOtherModule(BreakpointConditionContext context) {
			var ctx = (DebugEventBreakpointConditionContext)context;
			var lmArgs = (LoadModuleDebugCallbackEventArgs)ctx.EventArgs;
			var mod = lmArgs.CorModule;
			if (!IsModule(mod, otherModuleFullName) || mod.Assembly != thisAssembly)
				return false;
			debugger.RemoveBreakpoint(breakpoint);
			breakpoint = null;

			string otherModuleName;
			uint epToken = GetEntryPointToken(otherModuleFullName, out otherModuleName);
			if (epToken != 0 && (Table)(epToken >> 24) == Table.Method) {
				SetILBreakpoint(mod.SerializedDnModuleWithAssembly, epToken);
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
