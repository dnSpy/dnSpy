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
using System.Linq;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Attach.Dialogs;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.Dialogs.AttachToProcess {
	[Export(typeof(ShowAttachToProcessDialog))]
	sealed class ShowAttachToProcessDialogImpl : ShowAttachToProcessDialog {
		readonly IAppWindow appWindow;
		readonly Lazy<UIDispatcher> uiDispatcher;
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly ITextElementProvider textElementProvider;
		readonly Lazy<AttachProgramOptionsAggregatorFactory> attachProgramOptionsAggregatorFactory;
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<DebuggerSettings> debuggerSettings;
		readonly Lazy<ProgramFormatterProvider> programFormatterProvider;
		readonly IMessageBoxService messageBoxService;
		string? lastFilterText;

		[ImportingConstructor]
		ShowAttachToProcessDialogImpl(IAppWindow appWindow, Lazy<UIDispatcher> uiDispatcher, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider, Lazy<AttachProgramOptionsAggregatorFactory> attachProgramOptionsAggregatorFactory, Lazy<DbgManager> dbgManager, Lazy<DebuggerSettings> debuggerSettings, Lazy<ProgramFormatterProvider> programFormatterProvider, IMessageBoxService messageBoxService) {
			this.appWindow = appWindow;
			this.uiDispatcher = uiDispatcher;
			this.classificationFormatMapService = classificationFormatMapService;
			this.textElementProvider = textElementProvider;
			this.attachProgramOptionsAggregatorFactory = attachProgramOptionsAggregatorFactory;
			this.dbgManager = dbgManager;
			this.debuggerSettings = debuggerSettings;
			this.programFormatterProvider = programFormatterProvider;
			this.messageBoxService = messageBoxService;
		}

		public override AttachToProgramOptions[] Show(ShowAttachToProcessDialogOptions? options) {
			AttachToProcessVM? vm = null;
			try {
				var dlg = new AttachToProcessDlg();
				vm = new AttachToProcessVM(options, uiDispatcher.Value, dbgManager.Value, debuggerSettings.Value, programFormatterProvider.Value, classificationFormatMapService, textElementProvider, attachProgramOptionsAggregatorFactory.Value, () => SearchHelp(vm!, dlg));
				vm.FilterText = lastFilterText ?? string.Empty;
				dlg.DataContext = vm;
				dlg.Owner = appWindow.MainWindow;
				var res = dlg.ShowDialog();
				if (res != true)
					return Array.Empty<AttachToProgramOptions>();
				lastFilterText = vm.FilterText;
				return vm.SelectedItems.Select(a => a.AttachProgramOptions.GetOptions()).ToArray();
			}
			finally {
				vm?.Dispose();
			}
		}

		void SearchHelp(AttachToProcessVM vm, DependencyObject control) => messageBoxService.Show(vm.GetSearchHelpText(), ownerWindow: Window.GetWindow(control));

		public override void Attach(ShowAttachToProcessDialogOptions? options) {
			var attachOptions = Show(options);
			foreach (var o in attachOptions)
				dbgManager.Value.Start(o);
		}
	}
}
