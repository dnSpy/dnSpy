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
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.Evaluation.ViewModel;
using dnSpy.Debugger.ToolWindows;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.Evaluation.UI {
	abstract class VariablesWindowVMFactory {
		public abstract IVariablesWindowVM Create(VariablesWindowVMOptions variablesWindowVMOptions);
	}

	[Export(typeof(VariablesWindowVMFactory))]
	sealed class VariablesWindowVMFactoryImpl : VariablesWindowVMFactory {
		readonly Lazy<DbgManager> dbgManager;
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<ValueNodesVMFactory> valueNodesVMFactory;
		readonly Lazy<DbgLanguageService> dbgLanguageService;
		readonly Lazy<DbgCallStackService> dbgCallStackService;
		readonly Lazy<IMessageBoxService> messageBoxService;

		[ImportingConstructor]
		VariablesWindowVMFactoryImpl(Lazy<DbgManager> dbgManager, UIDispatcher uiDispatcher, Lazy<ValueNodesVMFactory> valueNodesVMFactory, Lazy<DbgLanguageService> dbgLanguageService, Lazy<DbgCallStackService> dbgCallStackService, Lazy<IMessageBoxService> messageBoxService) {
			this.dbgManager = dbgManager;
			this.uiDispatcher = uiDispatcher;
			this.valueNodesVMFactory = valueNodesVMFactory;
			this.dbgLanguageService = dbgLanguageService;
			this.dbgCallStackService = dbgCallStackService;
			this.messageBoxService = messageBoxService;
		}

		public override IVariablesWindowVM Create(VariablesWindowVMOptions variablesWindowVMOptions) {
			uiDispatcher.VerifyAccess();
			return new VariablesWindowVM(variablesWindowVMOptions, dbgManager, uiDispatcher, valueNodesVMFactory, dbgLanguageService, dbgCallStackService, messageBoxService);
		}
	}

	interface IVariablesWindowVM {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		event EventHandler TreeViewChanged;
		ITreeView TreeView { get; }
		IValueNodesVM VM { get; }
	}

	sealed class VariablesWindowVM : IVariablesWindowVM, ILazyToolWindowVM {
		public bool IsOpen {
			get => lazyToolWindowVMHelper.IsOpen;
			set => lazyToolWindowVMHelper.IsOpen = value;
		}

		public bool IsVisible {
			get => lazyToolWindowVMHelper.IsVisible;
			set => lazyToolWindowVMHelper.IsVisible = value;
		}

		public event EventHandler TreeViewChanged;
		public ITreeView TreeView => valueNodesVM.TreeView;

		IValueNodesVM IVariablesWindowVM.VM => valueNodesVM;

		readonly VariablesWindowVMOptions variablesWindowVMOptions;
		readonly Lazy<DbgManager> dbgManager;
		readonly UIDispatcher uiDispatcher;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly ValueNodesProviderImpl valueNodesProvider;
		readonly Lazy<ValueNodesVMFactory> valueNodesVMFactory;
		readonly Lazy<IMessageBoxService> messageBoxService;
		IValueNodesVM valueNodesVM;

		public VariablesWindowVM(VariablesWindowVMOptions variablesWindowVMOptions, Lazy<DbgManager> dbgManager, UIDispatcher uiDispatcher, Lazy<ValueNodesVMFactory> valueNodesVMFactory, Lazy<DbgLanguageService> dbgLanguageService, Lazy<DbgCallStackService> dbgCallStackService, Lazy<IMessageBoxService> messageBoxService) {
			uiDispatcher.VerifyAccess();
			this.variablesWindowVMOptions = variablesWindowVMOptions;
			this.dbgManager = dbgManager;
			this.uiDispatcher = uiDispatcher;
			lazyToolWindowVMHelper = new DebuggerLazyToolWindowVMHelper(this, uiDispatcher, dbgManager);
			valueNodesProvider = new ValueNodesProviderImpl(variablesWindowVMOptions.VariablesWindowValueNodesProvider, uiDispatcher, dbgManager, dbgLanguageService, dbgCallStackService);
			this.valueNodesVMFactory = valueNodesVMFactory;
			this.messageBoxService = messageBoxService;
		}

		// random thread
		void DbgThread(Action callback) =>
			dbgManager.Value.Dispatcher.BeginInvoke(callback);

		// random thread
		void UI(Action callback) => uiDispatcher.UI(callback);

		void ILazyToolWindowVM.Show() {
			uiDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: true);
		}

		void ILazyToolWindowVM.Hide() {
			uiDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: false);
		}

		void InitializeDebugger_UI(bool enable) {
			uiDispatcher.VerifyAccess();
			if (enable) {
				valueNodesProvider.Initialize_UI(enable);
				if (valueNodesVM == null) {
					var options = new ValueNodesVMOptions() {
						NodesProvider = valueNodesProvider,
						ShowMessageBox = ShowMessageBox,
						WindowContentType = variablesWindowVMOptions.WindowContentType,
						NameColumnName = variablesWindowVMOptions.NameColumnName,
						ValueColumnName = variablesWindowVMOptions.ValueColumnName,
						TypeColumnName = variablesWindowVMOptions.TypeColumnName,
						VariablesWindowKind = variablesWindowVMOptions.VariablesWindowKind,
						VariablesWindowGuid = variablesWindowVMOptions.VariablesWindowGuid,
					};
					valueNodesVM = valueNodesVMFactory.Value.Create(options);
				}
				valueNodesVM.Show();
				TreeViewChanged?.Invoke(this, EventArgs.Empty);
			}
			else {
				valueNodesVM?.Hide();
				TreeViewChanged?.Invoke(this, EventArgs.Empty);
				valueNodesProvider.Initialize_UI(enable);
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			if (enable)
				dbgManager.Value.DelayedIsRunningChanged += DbgManager_DelayedIsRunningChanged;
			else
				dbgManager.Value.DelayedIsRunningChanged -= DbgManager_DelayedIsRunningChanged;
		}

		// DbgManager thread
		void DbgManager_DelayedIsRunningChanged(object sender, EventArgs e) {
			// If all processes are running and the window is hidden, hide it now
			if (!IsVisible)
				UI(() => lazyToolWindowVMHelper.TryHideWindow());
		}

		bool ShowMessageBox(string message, ShowMessageBoxButtons buttons) {
			MsgBoxButton mbb;
			MsgBoxButton resButton;
			switch (buttons) {
			case ShowMessageBoxButtons.YesNo:
				mbb = MsgBoxButton.Yes | MsgBoxButton.No;
				resButton = MsgBoxButton.Yes;
				break;
			case ShowMessageBoxButtons.OK:
				mbb = MsgBoxButton.OK;
				resButton = MsgBoxButton.OK;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(buttons));
			}
			return messageBoxService.Value.Show(message, mbb) == resButton;
		}
	}
}
