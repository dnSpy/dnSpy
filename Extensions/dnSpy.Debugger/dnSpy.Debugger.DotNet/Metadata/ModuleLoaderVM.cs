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
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using dnlib.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.DotNet.Properties;
using dnSpy.Debugger.DotNet.UI;

namespace dnSpy.Debugger.DotNet.Metadata {
	sealed class ModuleLoaderVM : ViewModelBase, IDisposable {
		public ICommand CancelCommand => new RelayCommand(a => AskCancel(), a => CanCancel);

		readonly UIDispatcher uiDispatcher;
		readonly DbgRuntime runtime;
		readonly DbgDynamicModuleProvider dbgDynamicModuleProvider;
		readonly DynamicModuleDefDocument[] documents;
		readonly IMessageBoxService messageBoxService;
		readonly CancellationTokenSource cancellationTokenSource;
		readonly CancellationToken cancellationToken;

		sealed class CancellationTokenImpl : ICancellationToken {
			/*readonly*/ CancellationToken cancellationToken;
			public CancellationTokenImpl(CancellationToken cancellationToken) => this.cancellationToken = cancellationToken;
			public void ThrowIfCancellationRequested() => cancellationToken.ThrowIfCancellationRequested();
		}

		public ModuleLoaderVM(UIDispatcher uiDispatcher, IMessageBoxService messageBoxService, DbgRuntime runtime, DbgDynamicModuleProvider dbgDynamicModuleProvider, DynamicModuleDefDocument[] documents) {
			this.uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.dbgDynamicModuleProvider = dbgDynamicModuleProvider ?? throw new ArgumentNullException(nameof(dbgDynamicModuleProvider));
			this.documents = documents ?? throw new ArgumentNullException(nameof(documents));
			this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;
			runtime.Closed += DbgRuntime_Closed;

			if (runtime.IsClosed)
				Cancel();
			else
				dbgDynamicModuleProvider.BeginInvoke(LoadFiles_EngineThread);
		}

		void DbgRuntime_Closed(object? sender, EventArgs e) => Cancel();

		public bool CanCancel => cancelling == 0;
		volatile int cancelling;

		public void AskCancel() {
			uiDispatcher.VerifyAccess();
			if (cancelling != 0)
				return;

			if (!HasCompleted) {
				var res = messageBoxService.Show(dnSpy_Debugger_DotNet_Resources.CancelLoadingModulesMessage, MsgBoxButton.Yes | MsgBoxButton.No);
				if (res != MsgBoxButton.Yes)
					return;
			}

			Cancel();
		}

		void UI(Action callback) => uiDispatcher.UI(callback);

		void Cancel() {
			if (Interlocked.Exchange(ref cancelling, 1) != 0)
				return;
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
			UI(() => OnPropertyChanged(nameof(CanCancel)));
		}

		public void Dispose() {
			runtime.Closed -= DbgRuntime_Closed;
			Cancel();
		}

		public bool WasCanceled {
			get => wasCanceled;
			set {
				uiDispatcher.VerifyAccess();
				if (wasCanceled != value) {
					wasCanceled = value;
					OnPropertyChanged(nameof(WasCanceled));
				}
			}
		}
		bool wasCanceled;

		public bool HasCompleted {
			get => hasCompleted;
			set {
				uiDispatcher.VerifyAccess();
				if (hasCompleted != value) {
					hasCompleted = value;
					OnPropertyChanged(nameof(HasCompleted));
				}
			}
		}
		bool hasCompleted;

		public string? CurrentItemName {
			get => currentItemName;
			set {
				uiDispatcher.VerifyAccess();
				if (currentItemName != value) {
					currentItemName = value;
					OnPropertyChanged(nameof(CurrentItemName));
				}
			}
		}
		string? currentItemName;

		void LoadFiles_EngineThread() {
			bool wasCanceled;
			try {
				wasCanceled = LoadFilesCore_EngineThread();
			}
			catch (Exception ex) {
				wasCanceled = false;
				UI(() => messageBoxService.Show(ex));
			}
			UI(() => {
				WasCanceled = wasCanceled;
				HasCompleted = true;
				OnCompleted?.Invoke(this, EventArgs.Empty);
			});
		}

		bool LoadFilesCore_EngineThread() {
			bool wasCanceled = false;
			var modules = documents.Select(a => a.DbgModule).ToArray();
			dbgDynamicModuleProvider.LoadEverything(modules, started: true);
			try {
				foreach (var document in documents) {
					try {
						cancellationToken.ThrowIfCancellationRequested();
						if (document.DbgModule.IsClosed)
							continue;

						UI(() => CurrentItemName = CalculateCurrentItemName(document));
						document.ModuleDef!.LoadEverything(new CancellationTokenImpl(cancellationToken));

						// Make sure the cache is cleared since there could be new types
						if (document.ModuleDef.EnableTypeDefFindCache) {
							document.ModuleDef.EnableTypeDefFindCache = false;
							document.ModuleDef.EnableTypeDefFindCache = true;
						}
					}
					catch (OperationCanceledException) {
						wasCanceled = true;
						break;
					}
				}
			}
			finally {
				dbgDynamicModuleProvider.LoadEverything(modules, started: false);
			}
			return wasCanceled;
		}

		public event EventHandler? OnCompleted;

		string CalculateCurrentItemName(DynamicModuleDefDocument document) {
			var module = document.ModuleDef!;
			var sb = new StringBuilder();
			sb.Append($"({Array.IndexOf(documents, document) + 1}/{documents.Length}): ");

			var asm = module.Assembly;
			if (asm is not null) {
				if (module.IsManifestModule)
					sb.Append(asm.FullName);
				else
					sb.Append($"{module.Name} ({asm.FullName})");
			}
			else
				sb.Append(module.Name);

			return sb.ToString();
		}
	}
}
