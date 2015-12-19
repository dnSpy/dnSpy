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
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Shared.UI.Files;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;

namespace dnSpy.Debugger {
	static class DebugUtils {
		public static void GoToIL(IFileTabManager fileTabManager, IModuleLoader moduleLoader, SerializedDnSpyModule serAsm, uint token, uint ilOffset, bool newTab) {
			var file = moduleLoader.LoadModule(serAsm, true);
			GoToIL(fileTabManager, file, token, ilOffset, newTab);
		}

		public static bool GoToIL(IFileTabManager fileTabManager, IDnSpyFile file, uint token, uint ilOffset, bool newTab) {
			if (file == null)
				return false;

			var method = file.ModuleDef.ResolveToken(token) as MethodDef;
			if (method == null)
				return false;

			var serMod = SerializedDnSpyModuleCreator.Create(fileTabManager.FileTreeView,  method.Module);
			var key = new SerializedDnSpyToken(serMod, method.MDToken);

			bool found = fileTabManager.FileTreeView.FindNode(method.Module) != null;
			if (found) {
				fileTabManager.FollowReference(method, newTab, e => {
					Debug.Assert(e.Tab.UIContext is ITextEditorUIContext);
					if (e.Success)
						MoveCaretTo(e.Tab.UIContext as ITextEditorUIContext, key, ilOffset);
				});
				return true;
			}

			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				fileTabManager.FollowReference(method, newTab, e => {
					Debug.Assert(e.Tab.UIContext is ITextEditorUIContext);
					if (e.Success)
						MoveCaretTo(e.Tab.UIContext as ITextEditorUIContext, key, ilOffset);
				});
			}));
			return true;
		}

		public static bool MoveCaretTo(ITextEditorUIContext uiContext, SerializedDnSpyToken key, uint ilOffset) {
			if (uiContext == null)
				return false;

			CodeMappings cm;
			if (!VerifyAndGetCurrentDebuggedMethod(uiContext, key, out cm))
				return false;

			TextLocation location, endLocation;
			if (!cm.TryGetMapping(key).GetInstructionByTokenAndOffset(ilOffset, out location, out endLocation))
				return false;

			uiContext.ScrollAndMoveCaretTo(location.Line, location.Column);
			return true;
		}

		public static bool VerifyAndGetCurrentDebuggedMethod(ITextEditorUIContext uiContext, SerializedDnSpyToken serToken, out CodeMappings codeMappings) {
			codeMappings = uiContext.GetCodeMappings();
			return codeMappings.TryGetMapping(serToken) != null;
		}
	}
}
