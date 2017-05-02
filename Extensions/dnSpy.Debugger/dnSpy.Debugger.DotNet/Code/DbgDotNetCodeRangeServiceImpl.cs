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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.UI;

namespace dnSpy.Debugger.DotNet.Code {
	[Export(typeof(DbgDotNetCodeRangeService))]
	sealed class DbgDotNetCodeRangeServiceImpl : DbgDotNetCodeRangeService {
		readonly UIDispatcher uiDispatcher;
		readonly DbgModuleIdProviderService dbgModuleIdProviderService;
		readonly DbgMetadataService dbgMetadataService;
		readonly Lazy<IDocumentTabService> documentTabService;

		[ImportingConstructor]
		DbgDotNetCodeRangeServiceImpl(UIDispatcher uiDispatcher, DbgModuleIdProviderService dbgModuleIdProviderService, DbgMetadataService dbgMetadataService, Lazy<IDocumentTabService> documentTabService) {
			this.uiDispatcher = uiDispatcher;
			this.dbgModuleIdProviderService = dbgModuleIdProviderService;
			this.dbgMetadataService = dbgMetadataService;
			this.documentTabService = documentTabService;
		}

		void UI(Action callback) => uiDispatcher.UI(callback);

		public override void GetCodeRanges(DbgModule module, uint token, uint offset, Action<GetCodeRangeResult> callback) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			UI(() => GetCodeRanges_UI(module, token, offset, callback));
		}

		void GetCodeRanges_UI(DbgModule module, uint token, uint offset, Action<GetCodeRangeResult> callback) {
			uiDispatcher.VerifyAccess();
			var ranges = TryGetCodeRanges_UI(module, token, offset);
			if (ranges != null)
				callback(new GetCodeRangeResult(ranges));
			else
				callback(new GetCodeRangeResult(false, Array.Empty<DbgCodeRange>()));
		}

		DbgCodeRange[] TryGetCodeRanges_UI(DbgModule module, uint token, uint offset) {
			uiDispatcher.VerifyAccess();
			var tab = documentTabService.Value.GetOrCreateActiveTab();
			var documentViewer = tab.TryGetDocumentViewer();
			var methodDebugService = documentViewer.GetMethodDebugService();
			var moduleId = dbgModuleIdProviderService.GetModuleId(module);
			if (moduleId == null)
				return null;

			bool specialIpOffset;
			if (offset == EPILOG) {
				specialIpOffset = true;
				var mod = dbgMetadataService.TryGetMetadata(module, DbgLoadModuleOptions.AutoLoaded);
				if (mod?.ResolveToken(token) is MethodDef md && md.Body != null && md.Body.Instructions.Count > 0)
					offset = md.Body.Instructions[md.Body.Instructions.Count - 1].Offset;
				else
					return null;
			}
			else if (offset == PROLOG) {
				specialIpOffset = true;
				offset = 0;
			}
			else
				specialIpOffset = false;

			var key = new ModuleTokenId(moduleId.Value, token);
			var info = methodDebugService.TryGetMethodDebugInfo(key);
			if (info == null) {
				var md = dbgMetadataService.TryGetMetadata(module, DbgLoadModuleOptions.AutoLoaded);
				var mdMethod = md?.ResolveToken(token) as MethodDef;
				if (mdMethod == null)
					return null;

				tab.FollowReference(mdMethod);
				JumpToCurrentStatement(tab, mdMethod, key, offset, specialIpOffset);
				documentViewer = tab.TryGetDocumentViewer();
				methodDebugService = documentViewer.GetMethodDebugService();
				info = methodDebugService.TryGetMethodDebugInfo(key);
				if (info == null)
					return null;
			}

			var sourceStatement = info.GetSourceStatementByCodeOffset(offset);
			uint[] ranges;
			if (sourceStatement == null)
				ranges = info.GetUnusedRanges();
			else
				ranges = info.GetRanges(sourceStatement.Value);

			if (ranges.Length == 0)
				return null;
			return CreateStepRanges(ranges);
		}

		static DbgCodeRange[] CreateStepRanges(uint[] ilSpans) {
			var stepRanges = new DbgCodeRange[ilSpans.Length / 2];
			if (stepRanges.Length == 0)
				return null;
			for (int i = 0; i < stepRanges.Length; i++)
				stepRanges[i] = new DbgCodeRange(ilSpans[i * 2], ilSpans[i * 2 + 1]);
			return stepRanges;
		}

		void JumpToCurrentStatement(IDocumentTab tab, MethodDef method, ModuleTokenId module, uint offset, bool specialIpOffset) =>
			JumpToCurrentStatement(tab, method, module, offset, specialIpOffset, canRefreshMethods: true);

		void JumpToCurrentStatement(IDocumentTab tab, MethodDef method, ModuleTokenId module, uint offset, bool specialIpOffset, bool canRefreshMethods) {
			if (tab == null || method == null)
				return;

			// The file could've been added lazily to the list so add a short delay before we select it
			uiDispatcher.UIBackground(() => {
				tab.FollowReference(method, false, e => {
					Debug.Assert(e.Tab == tab);
					Debug.Assert(e.Tab.UIContext is IDocumentViewer);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretToCurrentStatement(e.Tab.UIContext as IDocumentViewer, method, module, offset, specialIpOffset, canRefreshMethods);
						e.HasMovedCaret = true;
					}
				});
			});
		}

		bool MoveCaretToCurrentStatement(IDocumentViewer documentViewer, MethodDef method, ModuleTokenId module, uint offset, bool specialIpOffset, bool canRefreshMethods) {
			if (documentViewer == null)
				return false;
			if (MoveCaretTo(documentViewer, module, offset))
				return true;
			if (!canRefreshMethods)
				return false;

			RefreshMethodBodies(documentViewer, method, module, offset, specialIpOffset);

			return false;
		}

		static bool MoveCaretTo(IDocumentViewer documentViewer, ModuleTokenId module, uint offset) {
			if (documentViewer == null)
				return false;

			if (!VerifyAndGetCurrentDebuggedMethod(documentViewer, module, out var methodDebugService))
				return false;

			var sourceStatement = methodDebugService.TryGetMethodDebugInfo(module).GetSourceStatementByCodeOffset(offset);
			if (sourceStatement == null)
				return false;

			documentViewer.MoveCaretToPosition(sourceStatement.Value.TextSpan.Start);
			return true;
		}

		static bool VerifyAndGetCurrentDebuggedMethod(IDocumentViewer documentViewer, ModuleTokenId token, out IMethodDebugService methodDebugService) {
			methodDebugService = documentViewer.GetMethodDebugService();
			return methodDebugService.TryGetMethodDebugInfo(token) != null;
		}

		void RefreshMethodBodies(IDocumentViewer documentViewer, MethodDef method, ModuleTokenId module, uint offset, bool specialIpOffset) {
			// If it's in the prolog/epilog, ignore it
			if (specialIpOffset)
				return;
			if (module.Module.IsDynamic)
				return;

			var body = method.Body;
			if (body == null)
				return;
			// If the offset is a valid instruction in the body, the method is probably not encrypted
			if (body.Instructions.Any(i => i.Offset == offset))
				return;

			var modNode = documentTabService.Value.DocumentTreeView.FindNode(method.Module);
			if (modNode == null)
				return;
			if (modNode.Document is MemoryModuleDefDocument memFile)
				memFile.UpdateMemory();
			else {
				var mod = dbgMetadataService.TryGetMetadata(module.Module, DbgLoadModuleOptions.ForceMemory | DbgLoadModuleOptions.AutoLoaded);
				method = mod?.ResolveToken(module.Token) as MethodDef;
				if (method == null)
					return;
			}

			JumpToCurrentStatement(documentViewer.DocumentTab, method, module, offset, specialIpOffset, canRefreshMethods: false);
		}
	}
}
