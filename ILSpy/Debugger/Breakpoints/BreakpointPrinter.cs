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

using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointPrinter {
		readonly ITextOutput output;
		readonly bool useHex;

		public BreakpointPrinter(ITextOutput output, bool useHex) {
			this.output = output;
			this.useHex = useHex;
		}

		static AssemblyDef GetAssembly(ILCodeBreakpoint bp) {
			var asmName = bp.Assembly;
			if (string.IsNullOrEmpty(asmName))
				return null;
			var loadedAsm = MainWindow.Instance.CurrentAssemblyList.FindAssemblyByFileName(asmName);
			if (loadedAsm != null)
				return loadedAsm.AssemblyDefinition;
			if (!File.Exists(asmName))
				return null;
			loadedAsm = MainWindow.Instance.LoadAssembly(asmName);
			return loadedAsm == null ? null : loadedAsm.AssemblyDefinition;
		}

		static ModuleDef GetModule(ILCodeBreakpoint bp) {
			var loadedAsm = MainWindow.Instance.LoadAssembly(bp.Assembly, bp.MethodKey.Module);
			return loadedAsm == null ? null : loadedAsm.ModuleDefinition;
		}

		static Language Language {
			get { return MainWindow.Instance.CurrentLanguage; }
		}

		static Language MethodLanguage {
			get {
				var lang = Language;
				if (lang.Name != "VB")
					return lang;
				// VB's WriteToolTip() hasn't been implemented for methods so use C# instead
				return Languages.GetLanguage("C#");
			}
		}

		static string GetHexFormatUInt16() {
			if (Language.Name == "VB")
				return "&H{0:X4}";
			else
				return "0x{0:X4}";
		}

		static string GetHexFormatUInt32() {
			if (Language.Name == "VB")
				return "&H{0:X8}";
			else
				return "0x{0:X8}";
		}

		static void WriteILOffset(ITextOutput output, uint offset) {
			// Offsets are always in hex
			if (offset <= ushort.MaxValue)
				output.Write(string.Format(GetHexFormatUInt16(), offset), TextTokenType.Number);
			else
				output.Write(string.Format(GetHexFormatUInt32(), offset), TextTokenType.Number);
		}

		static void WriteToken(ITextOutput output, uint token) {
			// Tokens are always in hex
			output.Write(string.Format(GetHexFormatUInt32(), token), TextTokenType.Number);
		}

		public void WriteName(BreakpointVM vm) {
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp != null) {
				var module = GetModule(ilbp) as ModuleDefMD;
				if (BreakpointSettings.Instance.ShowTokens) {
					WriteToken(output, ilbp.MethodKey.Token);
					output.WriteSpace();
				}
				var method = module == null ? null : module.ResolveToken(ilbp.MethodKey.Token) as IMemberRef;
				if (method == null)
					output.Write(string.Format("0x{0:X8}", ilbp.MethodKey.Token), TextTokenType.Number);
				else
					MethodLanguage.WriteToolTip(output, method, null);
				output.WriteSpace();
				output.Write('+', TextTokenType.Operator);
				output.WriteSpace();
				WriteILOffset(output, ilbp.ILOffset);
				return;
			}

			var debp = vm.Breakpoint as DebugEventBreakpoint;
			if (debp != null) {
				//TODO:
				return;
			}

			Debug.Fail(string.Format("Unknown breakpoint type: {0}", vm.Breakpoint.GetType()));
		}

		public void WriteAssembly(BreakpointVM vm) {
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp != null) {
				var asm = GetAssembly(ilbp);
				if (asm != null)
					output.Write(asm);
				else
					output.WriteFilename(ilbp.Assembly);
				return;
			}

			var debp = vm.Breakpoint as DebugEventBreakpoint;
			if (debp != null) {
				// nothing
				return;
			}
		}

		static string ModulePathToModuleName(string path) {
			try {
				return Path.GetFileName(path);
			}
			catch {
			}
			return path;
		}

		public void WriteModule(BreakpointVM vm) {
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp != null) {
				// Always use the filename since it matches the module names in the call stack and
				// modules windows
				output.WriteModule(ModulePathToModuleName(ilbp.MethodKey.Module.Name));
				return;
			}

			var debp = vm.Breakpoint as DebugEventBreakpoint;
			if (debp != null) {
				// nothing
				return;
			}
		}

		public void WriteFile(BreakpointVM vm) {
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp != null) {
				output.WriteFilename(ilbp.MethodKey.Module.Name);
				return;
			}

			var debp = vm.Breakpoint as DebugEventBreakpoint;
			if (debp != null) {
				// nothing
				return;
			}
		}
	}
}
