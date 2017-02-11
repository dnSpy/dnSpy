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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.UI;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.Dialogs.DebugProgram {
	sealed class DebugProgramVM : ViewModelBase {
		public object OptionsPages => optionsPages;
		readonly ObservableCollection<OptionsPageVM> optionsPages;

		public object OptionsPages_SelectedItem {
			get { return optionsPages_selectedItem; }
			set {
				if (optionsPages_selectedItem == value)
					return;
				optionsPages_selectedItem = (OptionsPageVM)value;
				OnPropertyChanged(nameof(OptionsPages_SelectedItem));
				HasErrorUpdated();
			}
		}
		OptionsPageVM optionsPages_selectedItem;

		public StartDebuggingOptions StartDebuggingOptions {
			get {
				Debug.Assert(optionsPages_selectedItem?.IsValid == true);
				return optionsPages_selectedItem.StartDebuggingOptionsPage.GetOptions();
			}
		}

		public Guid SelectedPageGuid => optionsPages_selectedItem?.PageGuid ?? Guid.Empty;

		public DebugProgramVM(StartDebuggingOptionsPage[] pages, Guid selectedPageGuid) {
			if (pages == null)
				throw new ArgumentNullException(nameof(pages));
			Debug.Assert(pages.Length != 0);
			optionsPages = new ObservableCollection<OptionsPageVM>(pages.Select(a => new OptionsPageVM(a)));
			OptionsPages_SelectedItem =
				optionsPages.FirstOrDefault(a => a.PageGuid == selectedPageGuid) ??
				optionsPages.FirstOrDefault(a => a.PageGuid == dotNetFrameworkPageGuid) ??
				optionsPages.FirstOrDefault();
			foreach (var page in optionsPages)
				page.IsValidChanged += OptionsPageVM_IsValidChanged;
		}
		static readonly Guid dotNetFrameworkPageGuid = new Guid("3FB8FCB5-AECE-443A-ABDE-601F2C23F1C1");

		void OptionsPageVM_IsValidChanged(object sender, EventArgs e) => HasErrorUpdated();
		public override bool HasError => optionsPages_selectedItem?.IsValid != true;

		public void Close() {
			foreach (var page in optionsPages) {
				page.IsValidChanged -= OptionsPageVM_IsValidChanged;
				page.Close();
			}
		}
	}
}
