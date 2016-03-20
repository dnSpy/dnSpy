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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Themes;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Exceptions {
	interface IExceptionsContent {
		object UIObject { get; }
		IInputElement FocusedElement { get; }
		FrameworkElement ScaleElement { get; }
		void Focus();
		void FocusSearchTextBox();
		ListBox ListBox { get; }
		IExceptionsVM ExceptionsVM { get; }
	}

	[Export, Export(typeof(IExceptionsContent)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ExceptionsContent : IExceptionsContent {
		public object UIObject {
			get { return ExceptionsControl; }
		}

		public IInputElement FocusedElement {
			get { return ExceptionsControl.ListBox; }
		}

		public FrameworkElement ScaleElement {
			get { return ExceptionsControl; }
		}

		public ListBox ListBox {
			get { return ExceptionsControl.ListBox; }
		}

		public IExceptionsVM ExceptionsVM {
			get { return vmExceptions.Value; }
		}

		ExceptionsControl ExceptionsControl {
			get {
				if (exceptionsControl.DataContext == null) {
					vmExceptions.Value.Initialize(new SelectedItemsProvider<ExceptionVM>(exceptionsControl.ListBox));
					exceptionsControl.DataContext = this.vmExceptions.Value;
				}
				return exceptionsControl;
			}
		}
		readonly ExceptionsControl exceptionsControl;

		readonly Lazy<IExceptionsVM> vmExceptions;

		[ImportingConstructor]
		ExceptionsContent(IWpfCommandManager wpfCommandManager, IThemeManager themeManager, Lazy<IExceptionsVM> exceptionsVM) {
			this.exceptionsControl = new ExceptionsControl();
			this.vmExceptions = exceptionsVM;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;

			wpfCommandManager.Add(CommandConstants.GUID_DEBUGGER_EXCEPTIONS_CONTROL, exceptionsControl);
			wpfCommandManager.Add(CommandConstants.GUID_DEBUGGER_EXCEPTIONS_LISTVIEW, exceptionsControl.ListBox);
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			vmExceptions.Value.RefreshThemeFields();
		}

		public void Focus() {
			UIUtils.FocusSelector(ExceptionsControl.ListBox);
		}

		public void FocusSearchTextBox() {
			ExceptionsControl.SearchTextBox.Focus();
			ExceptionsControl.SearchTextBox.SelectAll();
		}
	}
}
