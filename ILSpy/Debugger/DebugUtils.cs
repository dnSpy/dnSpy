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

using dndbg.Engine;
using dnlib.DotNet;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace dnSpy.Debugger {
	static class DebugUtils {
		public static bool GoToIL(SerializedDnModuleWithAssembly serAsm, uint token, uint ilOffset) {
			var loadedAsm = MainWindow.Instance.LoadAssembly(serAsm);
			if (loadedAsm == null)
				return false;

			var mod = loadedAsm.ModuleDefinition as ModuleDefMD;
			if (mod == null)
				return false;

			var md = mod.ResolveToken(token) as MethodDef;
			if (md == null)
				return false;

			return JumpToStatement(md, ilOffset);
		}

		public static bool JumpToStatement(MethodDef method, uint ilOffset, DecompilerTextView textView = null) {
			if (method == null)
				return false;
			var key = MethodKey.Create(method);
			if (key == null)
				return false;
			if (textView == null)
				textView = MainWindow.Instance.SafeActiveTextView;
			return MainWindow.Instance.JumpToReference(textView, method, (success, hasMovedCaret) => {
				if (success)
					return MoveCaretTo(textView, key.Value, ilOffset);
				return false;
			});
		}

		public static bool MoveCaretTo(DecompilerTextView textView, MethodKey key, uint ilOffset) {
			if (textView == null)
				return false;
			TextLocation location, endLocation;
			var cm = textView.CodeMappings;
			if (cm == null || !cm.ContainsKey(key))
				return false;
			if (!cm[key].GetInstructionByTokenAndOffset(ilOffset, out location, out endLocation)) {
				//TODO: Missing IL ranges
				return false;
			}
			else {
				textView.ScrollAndMoveCaretTo(location.Line, location.Column);
				return true;
			}
		}
	}
}
