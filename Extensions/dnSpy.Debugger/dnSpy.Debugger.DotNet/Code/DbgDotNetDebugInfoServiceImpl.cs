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
	abstract class DbgDotNetDebugInfoService {
		public abstract Task<MethodDebugInfoResult> GetMethodDebugInfoAsync(DbgModule module, uint token);
	}

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

		public override Task<MethodDebugInfoResult> GetMethodDebugInfoAsync(DbgModule module, uint token) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));
			var tcs = new TaskCompletionSource<MethodDebugInfoResult>();
			UI(() => GetMethodDebugInfo_UI(module, token, tcs));
			return tcs.Task;
		}

		void GetMethodDebugInfo_UI(DbgModule module, uint token, TaskCompletionSource<MethodDebugInfoResult> tcs) {
			uiDispatcher.VerifyAccess();
			try {
				var info = TryGetMethodDebugInfo_UI(module, token);
				tcs.SetResult(info);
			}
			catch (Exception ex) {
				tcs.SetException(ex);
			}
		}

		MethodDebugInfoResult TryGetMethodDebugInfo_UI(DbgModule module, uint token) {
			uiDispatcher.VerifyAccess();
			var tab = documentTabService.Value.GetOrCreateActiveTab();
			var documentViewer = tab.TryGetDocumentViewer();
			var methodDebugService = documentViewer.GetMethodDebugService();
			var moduleId = dbgModuleIdProviderService.GetModuleId(module);
			if (moduleId is null)
				return default;

			var key = new ModuleTokenId(moduleId.Value, token);
			var decompilerDebugInfo = methodDebugService.TryGetMethodDebugInfo(key);
			DbgMethodDebugInfo? debugInfo;
			DbgMethodDebugInfo? stateMachineDebugInfo = null;
			int methodVersion;
			if (decompilerDebugInfo is not null) {
				methodVersion = 1;
				debugInfo = DbgMethodDebugInfoUtils.ToDbgMethodDebugInfo(decompilerDebugInfo);
			}
			else {
				var cancellationToken = CancellationToken.None;
				var result = dbgMethodDebugInfoProvider.Value.GetMethodDebugInfo(decompilerService.Value.Decompiler, module, token, cancellationToken);
				methodVersion = result.MethodVersion;
				debugInfo = result.DebugInfo;
				stateMachineDebugInfo = result.StateMachineDebugInfo;
			}

			return new MethodDebugInfoResult(methodVersion, debugInfo, stateMachineDebugInfo);
		}
	}
}
