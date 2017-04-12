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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using dnSpy.UI;

namespace dnSpy.Documents.Tabs {
	[ExportReferenceNavigator]
	sealed class DotNetReferenceNavigator : ReferenceNavigator {
		readonly UIDispatcher uiDispatcher;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<DbgMetadataService> dbgMetadataService;

		[ImportingConstructor]
		DotNetReferenceNavigator(UIDispatcher uiDispatcher, IDocumentTabService documentTabService, Lazy<DbgMetadataService> dbgMetadataService) {
			this.uiDispatcher = uiDispatcher;
			this.documentTabService = documentTabService;
			this.dbgMetadataService = dbgMetadataService;
		}

		public override bool GoTo(object reference, ReadOnlyCollection<object> options) {
			switch (reference) {
			case DotNetMethodBodyReference bodyRef:
				GoTo(bodyRef, options);
				// Always return true even if GoTo() fails (eg. module not found). Otherwise an empty
				// page will be shown when the default handler tries to show the reference.
				return true;

			case DotNetTokenReference tokenRef:
				GoTo(tokenRef, options);
				// Always return true, see comment above
				return true;
			}

			return false;
		}

		ModuleDef GetModule(ModuleId module, ReadOnlyCollection<object> options) =>
			dbgMetadataService.Value.TryGetMetadata(module, DbgLoadModuleOptions.None);

		bool GoTo(DotNetMethodBodyReference bodyRef, ReadOnlyCollection<object> options) {
			bool newTab = options.Any(a => StringComparer.Ordinal.Equals(PredefinedReferenceNavigatorOptions.NewTab, a));
			var module = GetModule(bodyRef.Module, options);
			if (module == null)
				return false;

			var method = module.ResolveToken(bodyRef.Token) as MethodDef;
			if (method == null)
				return false;

			bool found = documentTabService.DocumentTreeView.FindNode(method.Module) != null;
			if (found) {
				documentTabService.FollowReference(method, newTab, true, e => {
					Debug.Assert(e.Tab.UIContext is IDocumentViewer);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretTo(e.Tab.UIContext as IDocumentViewer, bodyRef.Module, bodyRef.Token, bodyRef.Offset);
						e.HasMovedCaret = true;
					}
				});
				return true;
			}

			// If it wasn't found, it will be added to the treeview with a slight delay, make sure our code gets executed
			// after it's happened.
			uiDispatcher.UIBackground(() => {
				documentTabService.FollowReference(method, newTab, true, e => {
					Debug.Assert(e.Tab.UIContext is IDocumentViewer);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretTo(e.Tab.UIContext as IDocumentViewer, bodyRef.Module, bodyRef.Token, bodyRef.Offset);
						e.HasMovedCaret = true;
					}
				});
			});
			return true;
		}

		static bool MoveCaretTo(IDocumentViewer documentViewer, ModuleId module, uint token, uint offset) {
			if (documentViewer == null)
				return false;

			var key = new ModuleTokenId(module, token);
			if (!VerifyAndGetCurrentDebuggedMethod(documentViewer, key, out var methodDebugService))
				return false;

			var sourceStatement = methodDebugService.TryGetMethodDebugInfo(key).GetSourceStatementByCodeOffset(offset);
			if (sourceStatement == null)
				return false;

			documentViewer.MoveCaretToPosition(sourceStatement.Value.TextSpan.Start);
			return true;
		}

		static bool VerifyAndGetCurrentDebuggedMethod(IDocumentViewer documentViewer, ModuleTokenId token, out IMethodDebugService methodDebugService) {
			methodDebugService = documentViewer.GetMethodDebugService();
			return methodDebugService.TryGetMethodDebugInfo(token) != null;
		}

		bool GoTo(DotNetTokenReference tokenRef, ReadOnlyCollection<object> options) {
			bool newTab = options.Any(a => StringComparer.Ordinal.Equals(PredefinedReferenceNavigatorOptions.NewTab, a));
			var module = GetModule(tokenRef.Module, options);
			if (module == null)
				return false;

			var def = module.ResolveToken(tokenRef.Token) as IMemberDef;
			if (def == null)
				return false;

			bool found = documentTabService.DocumentTreeView.FindNode(def.Module) != null;
			if (found) {
				documentTabService.FollowReference(def, newTab, true, e => {
					Debug.Assert(e.Tab.UIContext is IDocumentViewer);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretTo(e.Tab.UIContext as IDocumentViewer, def);
						e.HasMovedCaret = true;
					}
				});
				return true;
			}

			// If it wasn't found, it will be added to the treeview with a slight delay, make sure our code gets executed
			// after it's happened.
			uiDispatcher.UIBackground(() => {
				documentTabService.FollowReference(def, newTab, true, e => {
					Debug.Assert(e.Tab.UIContext is IDocumentViewer);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretTo(e.Tab.UIContext as IDocumentViewer, def);
						e.HasMovedCaret = true;
					}
				});
			});
			return true;
		}

		static bool MoveCaretTo(IDocumentViewer documentViewer, IMemberDef def) {
			if (documentViewer == null)
				return false;
			var data = documentViewer.ReferenceCollection.FirstOrNull(a => a.Data.IsDefinition && a.Data.Reference == def);
			if (data == null)
				return false;
			documentViewer.MoveCaretToPosition(data.Value.Span.Start);
			return true;
		}
	}
}
