/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger {
	static class DebugUtils {
		public static void GoToIL(IModuleIdProvider moduleIdProvider, IDocumentTabService documentTabService, IModuleLoader moduleLoader, ModuleId moduleId, uint token, uint ilOffset, bool newTab) {
			var file = moduleLoader.LoadModule(moduleId, canLoadDynFile: true, diskFileOk: false, isAutoLoaded: true);
			GoToIL(moduleIdProvider, documentTabService, file, token, ilOffset, newTab);
		}

		public static bool GoToIL(IModuleIdProvider moduleIdProvider, IDocumentTabService documentTabService, IDsDocument document, uint token, uint ilOffset, bool newTab) {
			if (document == null)
				return false;

			var method = document.ModuleDef.ResolveToken(token) as MethodDef;
			if (method == null)
				return false;

			var modId = moduleIdProvider.Create(method.Module);
			var key = new ModuleTokenId(modId, method.MDToken);

			bool found = documentTabService.DocumentTreeView.FindNode(method.Module) != null;
			if (found) {
				documentTabService.FollowReference(method, newTab, true, e => {
					Debug.Assert(e.Tab.UIContext is IDocumentViewer);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretTo(e.Tab.UIContext as IDocumentViewer, key, ilOffset);
						e.HasMovedCaret = true;
					}
				});
				return true;
			}

			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				documentTabService.FollowReference(method, newTab, true, e => {
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

			if (!VerifyAndGetCurrentDebuggedMethod(documentViewer, key, out var methodDebugService))
				return false;

			var sourceStatement = methodDebugService.TryGetMethodDebugInfo(key).GetSourceStatementByCodeOffset(ilOffset);
			if (sourceStatement == null)
				return false;

			documentViewer.MoveCaretToPosition(sourceStatement.Value.TextSpan.Start);
			return true;
		}

		public static bool VerifyAndGetCurrentDebuggedMethod(IDocumentViewer documentViewer, ModuleTokenId serToken, out IMethodDebugService methodDebugService) {
			methodDebugService = documentViewer.GetMethodDebugService();
			return methodDebugService.TryGetMethodDebugInfo(serToken) != null;
		}
	}
}
