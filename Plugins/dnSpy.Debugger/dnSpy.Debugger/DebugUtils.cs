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

using System;
using System.Diagnostics;
using System.Windows.Threading;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Debugger {
	static class DebugUtils {
		public static void GoToIL(IFileTabManager fileTabManager, IModuleLoader moduleLoader, SerializedDnModule serAsm, uint token, uint ilOffset, bool newTab) {
			var file = moduleLoader.LoadModule(serAsm, canLoadDynFile: true, diskFileOk: false, isAutoLoaded: true);
			GoToIL(fileTabManager, file, token, ilOffset, newTab);
		}

		public static bool GoToIL(IFileTabManager fileTabManager, IDnSpyFile file, uint token, uint ilOffset, bool newTab) {
			if (file == null)
				return false;

			var method = file.ModuleDef.ResolveToken(token) as MethodDef;
			if (method == null)
				return false;

			var serMod = SerializedDnModuleCreator.Create(fileTabManager.FileTreeView,  method.Module);
			var key = new SerializedDnToken(serMod, method.MDToken);

			bool found = fileTabManager.FileTreeView.FindNode(method.Module) != null;
			if (found) {
				fileTabManager.FollowReference(method, newTab, true, e => {
					Debug.Assert(e.Tab.UIContext is IDocumentViewer);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretTo(e.Tab.UIContext as IDocumentViewer, key, ilOffset);
						e.HasMovedCaret = true;
					}
				});
				return true;
			}

			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				fileTabManager.FollowReference(method, newTab, true, e => {
					Debug.Assert(e.Tab.UIContext is IDocumentViewer);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretTo(e.Tab.UIContext as IDocumentViewer, key, ilOffset);
						e.HasMovedCaret = true;
					}
				});
			}));
			return true;
		}

		public static bool MoveCaretTo(IDocumentViewer documentViewer, SerializedDnToken key, uint ilOffset) {
			if (documentViewer == null)
				return false;

			CodeMappings cm;
			if (!VerifyAndGetCurrentDebuggedMethod(documentViewer, key, out cm))
				return false;

			TextPosition location, endLocation;
			if (!cm.TryGetMapping(key).GetInstructionByTokenAndOffset(ilOffset, out location, out endLocation))
				return false;

			documentViewer.ScrollAndMoveCaretTo(location.Line, location.Column);
			return true;
		}

		public static bool VerifyAndGetCurrentDebuggedMethod(IDocumentViewer documentViewer, SerializedDnToken serToken, out CodeMappings codeMappings) {
			codeMappings = documentViewer.GetCodeMappings();
			return codeMappings.TryGetMapping(serToken) != null;
		}
	}
}
