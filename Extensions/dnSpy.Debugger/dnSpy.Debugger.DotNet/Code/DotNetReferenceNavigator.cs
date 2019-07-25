/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.UI;

namespace dnSpy.Debugger.DotNet.Code {
	abstract class DotNetReferenceNavigator : ReferenceNavigator {
		public const uint EPILOG = 0xFFFFFFFF;
		public const uint PROLOG = 0xFFFFFFFE;
		public abstract void GoToLocation(IDocumentTab tab, MethodDef method, ModuleTokenId module, uint offset, bool newTab);
	}

	[ExportReferenceNavigator]
	[Export(typeof(DotNetReferenceNavigator))]
	sealed class DotNetReferenceNavigatorImpl : DotNetReferenceNavigator {
		readonly UIDispatcher uiDispatcher;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<DbgMetadataService> dbgMetadataService;

		[ImportingConstructor]
		DotNetReferenceNavigatorImpl(UIDispatcher uiDispatcher, IDocumentTabService documentTabService, Lazy<DbgMetadataService> dbgMetadataService) {
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

		ModuleDef? GetModule(ModuleId module, ReadOnlyCollection<object> options) =>
			dbgMetadataService.Value.TryGetMetadata(module, DbgLoadModuleOptions.None);

		bool GoTo(DotNetMethodBodyReference bodyRef, ReadOnlyCollection<object> options) {
			bool newTab = options.Any(a => StringComparer.Ordinal.Equals(PredefinedReferenceNavigatorOptions.NewTab, a));
			var module = GetModule(bodyRef.Module, options);
			if (module is null)
				return false;

			var method = module.ResolveToken(bodyRef.Token) as MethodDef;
			if (method is null)
				return false;

			uint offset = bodyRef.Offset;
			if (offset == DotNetMethodBodyReference.PROLOG)
				offset = PROLOG;
			else if (offset == DotNetMethodBodyReference.EPILOG)
				offset = EPILOG;

			var tab = documentTabService.GetOrCreateActiveTab();
			GoToLocation(tab, method, new ModuleTokenId(bodyRef.Module, bodyRef.Token), offset, newTab);
			return true;
		}

		bool GoTo(DotNetTokenReference tokenRef, ReadOnlyCollection<object> options) {
			bool newTab = options.Any(a => StringComparer.Ordinal.Equals(PredefinedReferenceNavigatorOptions.NewTab, a));
			var module = GetModule(tokenRef.Module, options);
			if (module is null)
				return false;

			var def = module.ResolveToken(tokenRef.Token) as IMemberDef;
			if (def is null)
				return false;

			bool found = !(documentTabService.DocumentTreeView.FindNode(def.Module) is null);
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

		static bool MoveCaretTo(IDocumentViewer? documentViewer, IMemberDef def) {
			if (documentViewer is null)
				return false;
			var data = documentViewer.ReferenceCollection.FirstOrNull(a => a.Data.IsDefinition && a.Data.Reference == def);
			if (data is null)
				return false;
			documentViewer.MoveCaretToPosition(data.Value.Span.Start);
			return true;
		}

		public override void GoToLocation(IDocumentTab tab, MethodDef method, ModuleTokenId module, uint offset, bool newTab) {
			bool specialIpOffset;
			if (offset == EPILOG) {
				specialIpOffset = true;
				var mod = dbgMetadataService.Value.TryGetMetadata(module.Module, DbgLoadModuleOptions.AutoLoaded);
				if (mod?.ResolveToken(module.Token) is MethodDef md && !(md.Body is null) && md.Body.Instructions.Count > 0)
					offset = md.Body.Instructions[md.Body.Instructions.Count - 1].Offset;
				else
					return;
			}
			else if (offset == PROLOG) {
				specialIpOffset = true;
				offset = 0;
			}
			else
				specialIpOffset = false;

			GoToLocationCore(tab, method, module, offset, specialIpOffset, newTab, canRefreshMethods: true);
		}

		void GoToLocationCore(IDocumentTab tab, MethodDef method, ModuleTokenId module, uint offset, bool specialIpOffset, bool newTab, bool canRefreshMethods) {
			uiDispatcher.VerifyAccess();
			if (tab is null || method is null)
				return;

			// The file could've been added lazily to the list so add a short delay before we select it
			uiDispatcher.UIBackground(() => {
				tab.FollowReference(method, newTab, e => {
					Debug.Assert(e.Tab.UIContext is IDocumentViewer);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretToCurrentStatement(e.Tab.UIContext as IDocumentViewer, method, module, offset, specialIpOffset, canRefreshMethods, newTab: false);
						e.HasMovedCaret = true;
					}
				});
			});
		}

		bool MoveCaretToCurrentStatement(IDocumentViewer? documentViewer, MethodDef method, ModuleTokenId module, uint offset, bool specialIpOffset, bool canRefreshMethods, bool newTab) {
			if (documentViewer is null)
				return false;
			if (MoveCaretTo(documentViewer, module, offset))
				return true;
			if (!canRefreshMethods)
				return false;

			RefreshMethodBodies(documentViewer, method, module, offset, specialIpOffset, newTab: false);

			return false;
		}

		static bool MoveCaretTo(IDocumentViewer documentViewer, ModuleTokenId module, uint offset) {
			if (documentViewer is null)
				return false;

			if (!VerifyAndGetCurrentDebuggedMethod(documentViewer, module, out var methodDebugService))
				return false;

			var sourceStatement = methodDebugService.TryGetMethodDebugInfo(module)!.GetSourceStatementByCodeOffset(offset);
			if (sourceStatement is null)
				return false;

			documentViewer.MoveCaretToPosition(sourceStatement.Value.TextSpan.Start);
			return true;
		}

		static bool VerifyAndGetCurrentDebuggedMethod(IDocumentViewer documentViewer, ModuleTokenId token, out IMethodDebugService methodDebugService) {
			methodDebugService = documentViewer.GetMethodDebugService();
			return !(methodDebugService.TryGetMethodDebugInfo(token) is null);
		}

		void RefreshMethodBodies(IDocumentViewer documentViewer, MethodDef method, ModuleTokenId module, uint offset, bool specialIpOffset, bool newTab) {
			// If it's in the prolog/epilog, ignore it
			if (specialIpOffset)
				return;
			if (module.Module.IsDynamic)
				return;

			var body = method.Body;
			if (body is null)
				return;
			// If the offset is a valid instruction in the body, the method is probably not encrypted
			if (body.Instructions.Any(i => i.Offset == offset))
				return;

			var modNode = documentTabService.DocumentTreeView.FindNode(method.Module);
			if (modNode is null)
				return;
			if (modNode.Document is MemoryModuleDefDocument memFile)
				memFile.UpdateMemory();
			else {
				var mod = dbgMetadataService.Value.TryGetMetadata(module.Module, DbgLoadModuleOptions.ForceMemory | DbgLoadModuleOptions.AutoLoaded);
				var md = mod?.ResolveToken(module.Token) as MethodDef;
				if (md is null)
					return;
				method = md;
			}

			GoToLocationCore(documentViewer.DocumentTab!, method, module, offset, specialIpOffset, newTab, canRefreshMethods: false);
		}
	}
}
