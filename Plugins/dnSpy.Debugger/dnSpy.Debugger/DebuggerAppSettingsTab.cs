/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;
using dndbg.Engine;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Debugger.Dialogs;
using dnSpy.Debugger.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger {
	[Export(typeof(IAppSettingsTabCreator))]
	sealed class DebuggerAppSettingsTabCreator : IAppSettingsTabCreator {
		readonly DebuggerSettingsImpl debuggerSettingsImpl;

		[ImportingConstructor]
		DebuggerAppSettingsTabCreator(DebuggerSettingsImpl debuggerSettingsImpl) {
			this.debuggerSettingsImpl = debuggerSettingsImpl;
		}

		public IEnumerable<IAppSettingsTab> Create() {
			yield return new DebuggerAppSettingsTab(debuggerSettingsImpl, new PickFilename());
		}
	}

	sealed class DebuggerAppSettingsTab : ViewModelBase, IAppSettingsTab {
		readonly DebuggerSettingsImpl _global_settings;
		readonly IPickFilename pickFilename;

		public DebuggerSettings Settings {
			get { return debuggerSettings; }
		}
		readonly DebuggerSettings debuggerSettings;

		public double Order {
			get { return AppSettingsConstants.ORDER_DEBUGGER_TAB_DISPLAY; }
		}

		public string Title {
			get { return dnSpy_Debugger_Resources.DebuggerOptDlgTab; }
		}

		public object UIObject {
			get { return this; }
		}

		public EnumListVM BreakProcessKindVM {
			get { return breakProcessKindVM; }
		}
		readonly EnumListVM breakProcessKindVM = new EnumListVM(DebugProcessVM.breakProcessKindList);

		public BreakProcessKind BreakProcessKind {
			get { return (BreakProcessKind)BreakProcessKindVM.SelectedItem; }
			set { BreakProcessKindVM.SelectedItem = value; }
		}

		public ICommand PickCoreCLRDbgShimFilenameCommand {
			get { return new RelayCommand(a => PickNewCoreCLRDbgShimFilename()); }
		}

		public DebuggerAppSettingsTab(DebuggerSettingsImpl debuggerSettingsImpl, IPickFilename pickFilename) {
			this._global_settings = debuggerSettingsImpl;
			this.debuggerSettings = debuggerSettingsImpl.Clone();
			this.BreakProcessKind = debuggerSettingsImpl.BreakProcessKind;
			this.pickFilename = pickFilename;
		}

		void PickNewCoreCLRDbgShimFilename() {
			var filter = string.Format("dbgshim.dll|dbgshim.dll|{0} (*.*)|*.*", dnSpy_Debugger_Resources.AllFiles);
			var newFilename = pickFilename.GetFilename(Settings.CoreCLRDbgShimFilename, "exe", filter);
			if (newFilename == null)
				return;

			Settings.CoreCLRDbgShimFilename = newFilename;
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			if (!saveSettings)
				return;
			debuggerSettings.CopyTo(_global_settings);
			_global_settings.BreakProcessKind = this.BreakProcessKind;
		}
	}
}
