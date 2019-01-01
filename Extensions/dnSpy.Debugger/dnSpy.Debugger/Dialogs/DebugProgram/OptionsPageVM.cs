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
using System.ComponentModel;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.StartDebugging.Dialog;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.Dialogs.DebugProgram {
	sealed class OptionsPageVM : ViewModelBase {
		internal StartDebuggingOptionsPage StartDebuggingOptionsPage { get; }
		internal Guid PageGuid => StartDebuggingOptionsPage.Guid;

		public bool IsValid => StartDebuggingOptionsPage.IsValid;
		public event EventHandler IsValidChanged;

		public object UIObject => StartDebuggingOptionsPage.UIObject;
		public string Name => StartDebuggingOptionsPage.DisplayName;

		public OptionsPageVM(StartDebuggingOptionsPage page) {
			StartDebuggingOptionsPage = page ?? throw new ArgumentNullException(nameof(page));
			StartDebuggingOptionsPage.PropertyChanged += StartDebuggingOptionsPage_PropertyChanged;
		}

		void StartDebuggingOptionsPage_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			Debug.Assert(sender == StartDebuggingOptionsPage);
			if (e.PropertyName == nameof(StartDebuggingOptionsPage.IsValid))
				IsValidChanged?.Invoke(this, EventArgs.Empty);
		}

		public void Close() {
			StartDebuggingOptionsPage.PropertyChanged -= StartDebuggingOptionsPage_PropertyChanged;
			StartDebuggingOptionsPage.OnClose();
		}
	}
}
