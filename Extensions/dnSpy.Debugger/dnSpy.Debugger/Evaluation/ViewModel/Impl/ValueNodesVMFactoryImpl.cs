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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.UI;
using dnSpy.Debugger.UI.Wpf;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class ValueNodesVMFactoryImpl_DbgManagerStartListener : IDbgManagerStartListener {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<ValueNodesVMFactoryImpl> valueNodesVMFactoryImpl;

		[ImportingConstructor]
		ValueNodesVMFactoryImpl_DbgManagerStartListener(UIDispatcher uiDispatcher, Lazy<ValueNodesVMFactoryImpl> valueNodesVMFactoryImpl) {
			this.uiDispatcher = uiDispatcher;
			this.valueNodesVMFactoryImpl = valueNodesVMFactoryImpl;
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => uiDispatcher.UI(() => valueNodesVMFactoryImpl.Value.OnStart(dbgManager));
	}

	[Export(typeof(ValueNodesVMFactory))]
	[Export(typeof(ValueNodesVMFactoryImpl))]
	sealed class ValueNodesVMFactoryImpl : ValueNodesVMFactory {
		readonly UIDispatcher uiDispatcher;
		readonly ITreeViewService treeViewService;
		readonly LanguageEditValueProviderFactory languageEditValueProviderFactory;
		readonly DbgValueNodeImageReferenceService dbgValueNodeImageReferenceService;
		readonly DebuggerSettings debuggerSettings;
		readonly DbgEvalFormatterSettings dbgEvalFormatterSettings;
		readonly DbgObjectIdService dbgObjectIdService;
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly ITextBlockContentInfoFactory textBlockContentInfoFactory;
		readonly IMenuService menuService;
		readonly IWpfCommandService wpfCommandService;
		readonly List<ValueNodesVM> allValueNodesVMs;

		[ImportingConstructor]
		ValueNodesVMFactoryImpl(UIDispatcher uiDispatcher, ITreeViewService treeViewService, LanguageEditValueProviderFactory languageEditValueProviderFactory, DbgValueNodeImageReferenceService dbgValueNodeImageReferenceService, DebuggerSettings debuggerSettings, DbgEvalFormatterSettings dbgEvalFormatterSettings, DbgObjectIdService dbgObjectIdService, IClassificationFormatMapService classificationFormatMapService, ITextBlockContentInfoFactory textBlockContentInfoFactory, IMenuService menuService, IWpfCommandService wpfCommandService) {
			uiDispatcher.VerifyAccess();
			this.uiDispatcher = uiDispatcher;
			this.treeViewService = treeViewService;
			this.languageEditValueProviderFactory = languageEditValueProviderFactory;
			this.dbgValueNodeImageReferenceService = dbgValueNodeImageReferenceService;
			this.debuggerSettings = debuggerSettings;
			this.dbgEvalFormatterSettings = dbgEvalFormatterSettings;
			this.dbgObjectIdService = dbgObjectIdService;
			this.classificationFormatMapService = classificationFormatMapService;
			this.textBlockContentInfoFactory = textBlockContentInfoFactory;
			this.menuService = menuService;
			this.wpfCommandService = wpfCommandService;
			allValueNodesVMs = new List<ValueNodesVM>();
		}

		public override IValueNodesVM Create(ValueNodesVMOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (options.NodesProvider == null)
				throw new ArgumentException();
			if (options.WindowContentType == null)
				throw new ArgumentException();
			if (options.NameColumnName == null)
				throw new ArgumentException();
			if (options.ValueColumnName == null)
				throw new ArgumentException();
			if (options.TypeColumnName == null)
				throw new ArgumentException();
			if (options.VariablesWindowKind == VariablesWindowKind.None)
				throw new ArgumentException();
			if (options.VariablesWindowGuid == Guid.Empty)
				throw new ArgumentException();
			if (options.ShowMessageBox == null)
				throw new ArgumentException();
			var vm = new ValueNodesVM(uiDispatcher, options, treeViewService, languageEditValueProviderFactory, dbgValueNodeImageReferenceService, debuggerSettings, dbgEvalFormatterSettings, dbgObjectIdService, classificationFormatMapService, textBlockContentInfoFactory, menuService, wpfCommandService);
			allValueNodesVMs.Add(vm);
			vm.OnVariableChanged += ValueNodesVM_OnVariableChanged;
			return vm;
		}

		void ValueNodesVM_OnVariableChanged(object sender, EventArgs e) {
			uiDispatcher.VerifyAccess();
			foreach (var vm in allValueNodesVMs) {
				if (vm != sender)
					vm.RecreateRootChildren_UI();
			}
		}

		internal void OnStart(DbgManager dbgManager) => dbgManager.ModulesRefreshed += DbgManager_ModulesRefreshed;

		void DbgManager_ModulesRefreshed(object sender, ModulesRefreshedEventArgs e) =>
			uiDispatcher.UI(() => OnModulesRefreshed());

		void OnModulesRefreshed() {
			uiDispatcher.VerifyAccess();
			foreach (var vm in allValueNodesVMs)
				vm.RefreshAllNodes_UI();
		}
	}
}
