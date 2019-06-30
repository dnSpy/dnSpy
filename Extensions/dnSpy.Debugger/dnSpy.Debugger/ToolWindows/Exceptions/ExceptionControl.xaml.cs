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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	sealed partial class ExceptionsControl : UserControl {
		public ListView ListView => listView;
		public TextBox SearchTextBox => searchTextBox;

		public ExceptionsControl() {
			InitializeComponent();
			SearchTextBox.GotKeyboardFocus += SearchTextBox_GotKeyboardFocus;
		}

		void SearchTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
			Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => SearchTextBox.SelectAll()));

		public void FocusSearchTextBox() {
			SearchTextBox.Focus();
			SearchTextBox.SelectAll();
		}

		void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (!UIUtilities.IsLeftDoubleClick<ListViewItem>(listView, e))
				return;
			ExceptionsListViewDoubleClick?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler ExceptionsListViewDoubleClick;
	}
}
