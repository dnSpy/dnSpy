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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
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
	[Export(typeof(DbgDotNetDebugInfoService))]
	sealed class DbgDotNetDebugInfoServiceImpl : DbgDotNetDebugInfoService {
		readonly UIDispatcher uiDispatcher;
		readonly DbgModuleIdProviderService dbgModuleIdProviderService;
		readonly DbgMetadataService dbgMetadataService;
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<DbgMethodDebugInfoProvider> dbgMethodDebugInfoProvider;
		readonly Lazy<IDecompilerService> decompilerService;

		[ImportingConstructor]
		DbgDotNetDebugInfoServiceImpl(UIDispatcher uiDispatcher, DbgModuleIdProviderService dbgModuleIdProviderService, DbgMetadataService dbgMetadataService, Lazy<IDocumentTabService> documentTabService, Lazy<DbgMethodDebugInfoProvider> dbgMethodDebugInfoProvider, Lazy<IDecompilerService> decompilerService) {
			this.uiDispatcher = uiDispatcher;
			this.dbgModuleIdProviderService = dbgModuleIdProviderService;
			this.dbgMetadataService = dbgMetadataService;
			this.documentTabService = documentTabService;
			this.dbgMethodDebugInfoProvider = dbgMethodDebugInfoProvider;
			this.decompilerService = decompilerService;
		}

		void UI(Action callback) => uiDispatcher.UI(callback);

		public override Task<GetMethodDebugInfoResult> GetMethodDebugInfoAsync(DbgModule module, uint token, uint offset) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			var tcs = new TaskCompletionSource<GetMethodDebugInfoResult>();
			UI(() => GetMethodDebugInfo_UI(module, token, offset, tcs));
			return tcs.Task;
		}

		void GetMethodDebugInfo_UI(DbgModule module, uint token, uint offset, TaskCompletionSource<GetMethodDebugInfoResult> tcs) {
			uiDispatcher.VerifyAccess();
			try {
				var info = TryGetMethodDebugInfo_UI(module, token, offset);
				tcs.SetResult(info);
			}
			catch (Exception ex) {
				tcs.SetException(ex);
			}
		}

		GetMethodDebugInfoResult TryGetMethodDebugInfo_UI(DbgModule module, uint token, uint offset) {
			uiDispatcher.VerifyAccess();
			var tab = documentTabService.Value.GetOrCreateActiveTab();
			var documentViewer = tab.TryGetDocumentViewer();
			var methodDebugService = documentViewer.GetMethodDebugService();
			var moduleId = dbgModuleIdProviderService.GetModuleId(module);
			if (moduleId == null)
				return default;

			uint refNavOffset;
			if (offset == DbgDotNetInstructionOffsetConstants.EPILOG) {
				refNavOffset = DotNetReferenceNavigator.EPILOG;
				var mod = dbgMetadataService.TryGetMetadata(module, DbgLoadModuleOptions.AutoLoaded);
				if (mod?.ResolveToken(token) is MethodDef md && md.Body != null && md.Body.Instructions.Count > 0)
					offset = md.Body.Instructions[md.Body.Instructions.Count - 1].Offset;
				else
					return default;
			}
			else if (offset == DbgDotNetInstructionOffsetConstants.PROLOG) {
				refNavOffset = DotNetReferenceNavigator.PROLOG;
				offset = 0;
			}
			else
				refNavOffset = offset;

			var key = new ModuleTokenId(moduleId.Value, token);
			var decompilerDebugInfo = methodDebugService.TryGetMethodDebugInfo(key);
			DbgMethodDebugInfo debugInfo;
			DbgMethodDebugInfo stateMachineDebugInfoOrNull = null;
			if (decompilerDebugInfo != null)
				debugInfo = DbgMethodDebugInfoUtils.ToDbgMethodDebugInfo(decompilerDebugInfo);
			else {
				var cancellationToken = CancellationToken.None;
				var result = dbgMethodDebugInfoProvider.Value.GetMethodDebugInfo(decompilerService.Value.Decompiler, module, token, cancellationToken);
				debugInfo = result.DebugInfoOrNull;
				stateMachineDebugInfoOrNull = result.StateMachineDebugInfoOrNull;
			}

			return new GetMethodDebugInfoResult(debugInfo, stateMachineDebugInfoOrNull);
		}
	}
}
