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
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.Evaluation.ViewModel;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Locals.Shared {
	abstract class LocalsVMFactory {
		public abstract ILocalsVM Create(LocalsVMOptions localsVMOptions);
	}

	[Export(typeof(LocalsVMFactory))]
	sealed class LocalsVMFactoryImpl : LocalsVMFactory {
		readonly Lazy<DbgManager> dbgManager;
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<ValueNodesVMFactory> valueNodesVMFactory;
		readonly Lazy<DbgLanguageService> dbgLanguageService;
		readonly Lazy<DbgCallStackService> dbgCallStackService;
		readonly Lazy<IMessageBoxService> messageBoxService;

		[ImportingConstructor]
		LocalsVMFactoryImpl(Lazy<DbgManager> dbgManager, UIDispatcher uiDispatcher, Lazy<ValueNodesVMFactory> valueNodesVMFactory, Lazy<DbgLanguageService> dbgLanguageService, Lazy<DbgCallStackService> dbgCallStackService, Lazy<IMessageBoxService> messageBoxService) {
			this.dbgManager = dbgManager;
			this.uiDispatcher = uiDispatcher;
			this.valueNodesVMFactory = valueNodesVMFactory;
			this.dbgLanguageService = dbgLanguageService;
			this.dbgCallStackService = dbgCallStackService;
			this.messageBoxService = messageBoxService;
		}

		public override ILocalsVM Create(LocalsVMOptions localsVMOptions) {
			uiDispatcher.VerifyAccess();
			return new LocalsVM(localsVMOptions, dbgManager, uiDispatcher, valueNodesVMFactory, dbgLanguageService, dbgCallStackService, messageBoxService);
		}
	}

	interface ILocalsVM {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		event EventHandler TreeViewChanged;
		ITreeView TreeView { get; }
		IValueNodesVM VM { get; }
	}

	sealed class LocalsVM : ILocalsVM, ILazyToolWindowVM {
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

		sealed class ValueNodesProviderImpl : ValueNodesProvider {
			public override event EventHandler NodesChanged;
			public override event EventHandler IsReadOnlyChanged;
			public override bool IsReadOnly => isReadOnly;
			public override event EventHandler LanguageChanged;
			public override DbgLanguage Language => language;
			bool isReadOnly;
			bool isOpen;
			DbgLanguage language;

			readonly bool isLocals;
			readonly UIDispatcher uiDispatcher;
			readonly Lazy<DbgManager> dbgManager;
			readonly Lazy<DbgLanguageService> dbgLanguageService;
			readonly Lazy<DbgCallStackService> dbgCallStackService;

			public ValueNodesProviderImpl(bool isLocals, UIDispatcher uiDispatcher, Lazy<DbgManager> dbgManager, Lazy<DbgLanguageService> dbgLanguageService, Lazy<DbgCallStackService> dbgCallStackService) {
				this.isLocals = isLocals;
				this.uiDispatcher = uiDispatcher;
				this.dbgManager = dbgManager;
				this.dbgLanguageService = dbgLanguageService ?? throw new ArgumentNullException(nameof(dbgLanguageService));
				this.dbgCallStackService = dbgCallStackService ?? throw new ArgumentNullException(nameof(dbgCallStackService));
			}

			void UI(Action callback) => uiDispatcher.UI(callback);

			void DbgThread(Action callback) =>
				dbgManager.Value.Dispatcher.BeginInvoke(callback);

			public void Initialize_UI(bool enable) {
				uiDispatcher.VerifyAccess();
				isOpen = enable;
				RefreshNodes_UI();
				DbgThread(() => InitializeDebugger_DbgThread(enable));
			}

			void InitializeDebugger_DbgThread(bool enable) {
				dbgManager.Value.Dispatcher.VerifyAccess();
				if (enable) {
					dbgLanguageService.Value.LanguageChanged += DbgLanguageService_LanguageChanged;
					dbgCallStackService.Value.FramesChanged += DbgCallStackService_FramesChanged;
				}
				else {
					dbgLanguageService.Value.LanguageChanged -= DbgLanguageService_LanguageChanged;
					dbgCallStackService.Value.FramesChanged -= DbgCallStackService_FramesChanged;
				}
			}

			void DbgLanguageService_LanguageChanged(object sender, DbgLanguageChangedEventArgs e) {
				var thread = dbgManager.Value.CurrentThread.Current;
				if (thread == null || thread.Runtime.Guid != e.RuntimeGuid)
					return;
				UI(() => RefreshNodes_UI());
			}

			void DbgCallStackService_FramesChanged(object sender, FramesChangedEventArgs e) =>
				UI(() => RefreshNodes_UI());

			void RefreshNodes_UI() {
				uiDispatcher.VerifyAccess();
				var info = TryGetLanguage();
				if (info.language != language) {
					language = info.language;
					LanguageChanged?.Invoke(this, EventArgs.Empty);
				}
				bool newIsReadOnly = info.frame == null;
				NodesChanged?.Invoke(this, EventArgs.Empty);
				SetIsReadOnly_UI(newIsReadOnly);
			}

			(DbgLanguage language, DbgStackFrame frame) TryGetLanguage() {
				if (!isOpen)
					return (null, null);
				var frame = dbgCallStackService.Value.ActiveFrame;
				if (frame == null)
					return (null, null);
				var language = dbgLanguageService.Value.GetCurrentLanguage(frame.Thread.Runtime.Guid);
				return (language, frame);
			}

			public override DbgValueNodeInfo[] GetNodes() {
				uiDispatcher.VerifyAccess();
				var info = TryGetLanguage();
				if (info.frame == null)
					return Array.Empty<DbgValueNodeInfo>();
				var provider = isLocals ? info.language.LocalsProvider : info.language.AutosProvider;
				return provider.GetNodes(info.frame).Select(a => new DbgValueNodeInfo(a)).ToArray();
			}

			void SetIsReadOnly_UI(bool newIsReadOnly) {
				uiDispatcher.VerifyAccess();
				if (isReadOnly == newIsReadOnly)
					return;
				isReadOnly = newIsReadOnly;
				IsReadOnlyChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		IValueNodesVM ILocalsVM.VM => valueNodesVM;

		readonly LocalsVMOptions localsVMOptions;
		readonly UIDispatcher uiDispatcher;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly ValueNodesProviderImpl valueNodesProvider;
		readonly Lazy<ValueNodesVMFactory> valueNodesVMFactory;
		readonly Lazy<IMessageBoxService> messageBoxService;
		IValueNodesVM valueNodesVM;

		public LocalsVM(LocalsVMOptions localsVMOptions, Lazy<DbgManager> dbgManager, UIDispatcher uiDispatcher, Lazy<ValueNodesVMFactory> valueNodesVMFactory, Lazy<DbgLanguageService> dbgLanguageService, Lazy<DbgCallStackService> dbgCallStackService, Lazy<IMessageBoxService> messageBoxService) {
			uiDispatcher.VerifyAccess();
			this.localsVMOptions = localsVMOptions;
			this.uiDispatcher = uiDispatcher;
			lazyToolWindowVMHelper = new DebuggerLazyToolWindowVMHelper(this, uiDispatcher, dbgManager);
			valueNodesProvider = new ValueNodesProviderImpl(localsVMOptions.VariablesWindowKind == VariablesWindowKind.Locals, uiDispatcher, dbgManager, dbgLanguageService, dbgCallStackService);
			this.valueNodesVMFactory = valueNodesVMFactory;
			this.messageBoxService = messageBoxService;
		}

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
						WindowContentType = localsVMOptions.WindowContentType,
						NameColumnName = localsVMOptions.NameColumnName,
						ValueColumnName = localsVMOptions.ValueColumnName,
						TypeColumnName = localsVMOptions.TypeColumnName,
						VariablesWindowKind = localsVMOptions.VariablesWindowKind,
						VariablesWindowGuid = localsVMOptions.VariablesWindowGuid,
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
