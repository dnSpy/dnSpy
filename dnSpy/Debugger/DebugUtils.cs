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
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Files;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace dnSpy.Debugger {
	static class DebugUtils {
		public static void GoToIL(SerializedDnSpyModule serAsm, uint token, uint ilOffset, bool newTab) {
			var file = ModuleLoader.Instance.LoadModule(serAsm, true);
			GoToIL(file, token, ilOffset, newTab);
		}

		public static bool GoToIL(DnSpyFile file, uint token, uint ilOffset, bool newTab) {
			if (file == null)
				return false;

			var md = file.ModuleDef.ResolveToken(token) as MethodDef;
			if (md == null)
				return false;

			if (newTab)
				MainWindow.Instance.OpenNewEmptyTab();
			return JumpToStatement(md, ilOffset, null);
		}

		static bool JumpToStatement(MethodDef method, uint ilOffset, DecompilerTextView textView) {
			if (method == null)
				return false;
			var serMod = method.Module.ToSerializedDnSpyModule();
			var key = new SerializedDnSpyToken(serMod, method.MDToken);
			if (textView == null)
				textView = MainWindow.Instance.SafeActiveTextView;

			bool found = MainWindow.Instance.DnSpyFileListTreeNode.FindModuleNode(method.Module) != null;
			if (found) {
				return MainWindow.Instance.JumpToReference(textView, method, (success, hasMovedCaret) => {
					if (success)
						return MoveCaretTo(textView, key, ilOffset);
					return false;
				});
			}

			MainWindow.Instance.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				MainWindow.Instance.JumpToReference(textView, method, (success, hasMovedCaret) => {
					if (success)
						return MoveCaretTo(textView, key, ilOffset);
					return false;
				});
			}));
			return true;
		}

		public static bool MoveCaretTo(DecompilerTextView textView, SerializedDnSpyToken key, uint ilOffset) {
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
