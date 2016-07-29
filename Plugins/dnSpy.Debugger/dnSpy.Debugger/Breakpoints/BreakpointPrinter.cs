/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointPrinter {
		readonly ITextColorWriter output;
		readonly bool useHex;
		readonly ILanguage language;

		public BreakpointPrinter(ITextColorWriter output, bool useHex, ILanguage language) {
			this.output = output;
			this.useHex = useHex;
			this.language = language;
		}

		ILanguage MethodLanguage => language;

		string GetHexFormatUInt16() {
			if (language.GenericGuid == LanguageConstants.LANGUAGE_VISUALBASIC)
				return "&H{0:X4}";
			else
				return "0x{0:X4}";
		}

		string GetHexFormatUInt32() {
			if (language.GenericGuid == LanguageConstants.LANGUAGE_VISUALBASIC)
				return "&H{0:X8}";
			else
				return "0x{0:X8}";
		}

		void WriteILOffset(ITextColorWriter output, uint offset) {
			// Offsets are always in hex
			if (offset <= ushort.MaxValue)
				output.Write(BoxedTextColor.Number, string.Format(GetHexFormatUInt16(), offset));
			else
				output.Write(BoxedTextColor.Number, string.Format(GetHexFormatUInt32(), offset));
		}

		void WriteToken(ITextColorWriter output, uint token) {
			// Tokens are always in hex
			output.Write(BoxedTextColor.Number, string.Format(GetHexFormatUInt32(), token));
		}

		public void WriteName(BreakpointVM vm) {
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp != null) {
				vm.NameError = false;
				bool printedToken = false;
				if (vm.Context.ShowTokens) {
					WriteToken(output, ilbp.MethodToken.Token);
					output.WriteSpace();
					printedToken = true;
				}
				// If this is a method in a dynamic module and the module hasn't been loaded yet,
				// this call will try to load it, and then open a dialog box showing the progress.
				// But in rare cases we can't show the dialog box because of Dispatcher limitations,
				// so if we must load the module, fail. Passing in false will prevent loading
				// dynamic modules.
				var method = vm.GetMethodDef(false);
				if (method == null) {
					vm.NameError = true;
					if (printedToken)
						output.Write(BoxedTextColor.Error, "???");
					else
						output.Write(BoxedTextColor.Number, string.Format("0x{0:X8}", ilbp.MethodToken.Token));
				}
				else
					MethodLanguage.Write(output, method, GetFlags(vm.Context));
				output.WriteSpace();
				output.Write(BoxedTextColor.Operator, "+");
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

		static SimplePrinterFlags GetFlags(IBreakpointContext ctx) {
			SimplePrinterFlags flags = 0;
			if (ctx.ShowModuleNames)	flags |= SimplePrinterFlags.ShowModuleNames;
			if (ctx.ShowParameterTypes)	flags |= SimplePrinterFlags.ShowParameterTypes;
			if (ctx.ShowParameterNames)	flags |= SimplePrinterFlags.ShowParameterNames;
			if (ctx.ShowOwnerTypes)		flags |= SimplePrinterFlags.ShowOwnerTypes;
			if (ctx.ShowReturnTypes)	flags |= SimplePrinterFlags.ShowReturnTypes;
			if (ctx.ShowNamespaces)		flags |= SimplePrinterFlags.ShowNamespaces;
			if (ctx.ShowTypeKeywords)	flags |= SimplePrinterFlags.ShowTypeKeywords;
			return flags;
		}

		public void WriteAssembly(BreakpointVM vm) {
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp != null) {
				output.Write(new AssemblyNameInfo(ilbp.MethodToken.Module.AssemblyFullName));
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
				output.WriteModule(ModulePathToModuleName(ilbp.MethodToken.Module.ModuleName));
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
				output.WriteFilename(ilbp.MethodToken.Module.ModuleName);
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
