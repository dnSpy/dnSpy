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
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger {
	static class DebugUtils {
		public static void GoToIL(IModuleIdCreator moduleIdCreator, IFileTabManager fileTabManager, IModuleLoader moduleLoader, ModuleId moduleId, uint token, uint ilOffset, bool newTab) {
			var file = moduleLoader.LoadModule(moduleId, canLoadDynFile: true, diskFileOk: false, isAutoLoaded: true);
			GoToIL(moduleIdCreator, fileTabManager, file, token, ilOffset, newTab);
		}

		public static bool GoToIL(IModuleIdCreator moduleIdCreator, IFileTabManager fileTabManager, IDnSpyFile file, uint token, uint ilOffset, bool newTab) {
			if (file == null)
				return false;

			var method = file.ModuleDef.ResolveToken(token) as MethodDef;
			if (method == null)
				return false;

			var modId = moduleIdCreator.Create(method.Module);
			var key = new ModuleTokenId(modId, method.MDToken);

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

		public static bool MoveCaretTo(IDocumentViewer documentViewer, ModuleTokenId key, uint ilOffset) {
			if (documentViewer == null)
				return false;

			MethodDebugService methodDebugService;
			if (!VerifyAndGetCurrentDebuggedMethod(documentViewer, key, out methodDebugService))
				return false;

			var sourceStatement = methodDebugService.TryGetMethodDebugInfo(key).GetSourceStatementByCodeOffset(ilOffset);
			if (sourceStatement == null)
				return false;

			documentViewer.MoveCaretToPosition(sourceStatement.Value.TextSpan.Start);
			return true;
		}

		public static bool VerifyAndGetCurrentDebuggedMethod(IDocumentViewer documentViewer, ModuleTokenId serToken, out MethodDebugService methodDebugService) {
			methodDebugService = documentViewer.GetMethodDebugService();
			return methodDebugService.TryGetMethodDebugInfo(serToken) != null;
		}
	}
}
