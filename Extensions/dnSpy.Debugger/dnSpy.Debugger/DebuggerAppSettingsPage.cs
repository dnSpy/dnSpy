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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;
using dndbg.Engine;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Debugger.Dialogs;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger {
	[Export(typeof(IAppSettingsPageProvider))]
	sealed class DebuggerAppSettingsPageProvider : IAppSettingsPageProvider {
		readonly DebuggerSettingsImpl debuggerSettingsImpl;

		[ImportingConstructor]
		DebuggerAppSettingsPageProvider(DebuggerSettingsImpl debuggerSettingsImpl, IPickFilename pickFilename) {
			this.debuggerSettingsImpl = debuggerSettingsImpl;
		}

		public IEnumerable<AppSettingsPage> Create() {
			yield return new DebuggerAppSettingsPage(debuggerSettingsImpl, new PickFilename());
		}
	}

	sealed class DebuggerAppSettingsPage : AppSettingsPage {
		readonly DebuggerSettingsImpl _global_settings;
		readonly IPickFilename pickFilename;

		public override Guid Guid => new Guid("8D2BC2FB-5CA4-4907-84C7-F4F705327AC8");
		public DebuggerSettings Settings { get; }
		public override double Order => AppSettingsConstants.ORDER_DEBUGGER;
		public override string Title => dnSpy_Debugger_Resources.DebuggerOptDlgTab;
		public override object UIObject => this;

		public EnumListVM BreakProcessKindVM => breakProcessKindVM;
		readonly EnumListVM breakProcessKindVM = new EnumListVM(DebugProcessVM.breakProcessKindList);

		public BreakProcessKind BreakProcessKind {
			get { return (BreakProcessKind)BreakProcessKindVM.SelectedItem; }
			set { BreakProcessKindVM.SelectedItem = value; }
		}

		public ICommand PickCoreCLRDbgShimFilenameCommand => new RelayCommand(a => PickNewCoreCLRDbgShimFilename());

		public DebuggerAppSettingsPage(DebuggerSettingsImpl debuggerSettingsImpl, IPickFilename pickFilename) {
			this._global_settings = debuggerSettingsImpl;
			this.Settings = debuggerSettingsImpl.Clone();
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

		public override void OnApply() {
			Settings.CopyTo(_global_settings);
			_global_settings.BreakProcessKind = this.BreakProcessKind;
		}
	}
}
