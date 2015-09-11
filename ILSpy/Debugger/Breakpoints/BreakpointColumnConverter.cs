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
using System.Globalization;
using System.IO;
using System.Windows.Data;
using dnlib.DotNet;
using dnSpy.Images;
using dnSpy.NRefactory;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = value as BreakpointVM;
			if (vm == null)
				return null;
			var s = parameter as string;
			if (s == null)
				return null;

			if (StringComparer.OrdinalIgnoreCase.Equals(s, "Image")) {
				string img = vm.IsEnabled ? "Breakpoint" : "DisabledBreakpoint";
				return ImageCache.Instance.GetImage(img, BackgroundType.GridViewItem);
			}

			var gen = UISyntaxHighlighter.Create(DebuggerSettings.Instance.SyntaxHighlightBreakpoints);
			if (StringComparer.OrdinalIgnoreCase.Equals(s, "Name"))
				ToName(gen, vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Assembly"))
				ToAssembly(gen, vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Module"))
				ToModule(gen, vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "File"))
				ToFile(gen, vm);
			else
				return null;

			return gen.CreateTextBlock(true);
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

		void ToName(UISyntaxHighlighter gen, BreakpointVM vm) {
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp != null) {
				var module = GetModule(ilbp) as ModuleDefMD;
				if (BreakpointSettings.Instance.ShowTokens) {
					WriteToken(gen.TextOutput, ilbp.MethodKey.Token);
					gen.TextOutput.WriteSpace();
				}
				var method = module == null ? null : module.ResolveToken(ilbp.MethodKey.Token) as IMemberRef;
				if (method == null)
					gen.TextOutput.Write(string.Format("0x{0:X8}", ilbp.MethodKey.Token), TextTokenType.Number);
				else
					MethodLanguage.WriteToolTip(gen.TextOutput, method, null);
				gen.TextOutput.WriteSpace();
				gen.TextOutput.Write('+', TextTokenType.Operator);
				gen.TextOutput.WriteSpace();
				WriteILOffset(gen.TextOutput, ilbp.ILOffset);
				return;
			}

			var debp = vm.Breakpoint as DebugEventBreakpoint;
			if (debp != null) {
				//TODO:
				return;
			}

			Debug.Fail(string.Format("Unknown breakpoint type: {0}", vm.Breakpoint.GetType()));
		}

		void ToAssembly(UISyntaxHighlighter gen, BreakpointVM vm) {
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp != null) {
				var asm = GetAssembly(ilbp);
				if (asm != null)
					gen.TextOutput.Write(asm);
				else
					gen.TextOutput.WriteFileName(ilbp.Assembly);
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

		void ToModule(UISyntaxHighlighter gen, BreakpointVM vm) {
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp != null) {
				// Always use the filename since it matches the module names in the call stack and
				// modules windows
				gen.TextOutput.WriteModule(ModulePathToModuleName(ilbp.MethodKey.Module.Name));
				return;
			}

			var debp = vm.Breakpoint as DebugEventBreakpoint;
			if (debp != null) {
				// nothing
				return;
			}
		}

		void ToFile(UISyntaxHighlighter gen, BreakpointVM vm) {
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp != null) {
				gen.TextOutput.WriteFileName(ilbp.MethodKey.Module.Name);
				return;
			}

			var debp = vm.Breakpoint as DebugEventBreakpoint;
			if (debp != null) {
				// nothing
				return;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
