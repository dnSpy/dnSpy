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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.Exceptions {
	interface IExceptionsContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		void FocusSearchTextBox();
		ListBox ListBox { get; }
		IExceptionsVM ExceptionsVM { get; }
	}

	[Export(typeof(IExceptionsContent))]
	sealed class ExceptionsContent : IExceptionsContent {
		public object UIObject => ExceptionsControl;
		public IInputElement FocusedElement => ExceptionsControl.ListBox;
		public FrameworkElement ZoomElement => ExceptionsControl;
		public ListBox ListBox => ExceptionsControl.ListBox;

		public IExceptionsVM ExceptionsVM {
			get {
				if (!initd) {
					initd = true;
					UpdateImageOptions(vmExceptions.Value);
				}
				return vmExceptions.Value;
			}
		}
		bool initd;
		readonly Lazy<IExceptionsVM> vmExceptions;

		ExceptionsControl ExceptionsControl {
			get {
				if (exceptionsControl.DataContext == null) {
					ExceptionsVM.Initialize(new SelectedItemsProvider<ExceptionVM>(exceptionsControl.ListBox));
					exceptionsControl.DataContext = ExceptionsVM;
				}
				return exceptionsControl;
			}
		}
		readonly ExceptionsControl exceptionsControl;

		double zoomLevel;

		[ImportingConstructor]
		ExceptionsContent(IWpfCommandService wpfCommandService, IThemeService themeService, IDpiService dpiService, Lazy<IExceptionsVM> exceptionsVM) {
			this.exceptionsControl = new ExceptionsControl();
			this.vmExceptions = exceptionsVM;
			themeService.ThemeChanged += ThemeService_ThemeChanged;
			dpiService.DpiChanged += DpiService_DpiChanged;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_EXCEPTIONS_CONTROL, exceptionsControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_EXCEPTIONS_LISTVIEW, exceptionsControl.ListBox);
		}

		void UpdateImageOptions(IExceptionsVM vm) {
			var options = new ImageOptions {
				Zoom = new Size(zoomLevel, zoomLevel),
				DpiObject = exceptionsControl,
			};
			vm.ImageOptions = options;
		}

		void DpiService_DpiChanged(object sender, WindowDpiChangedEventArgs e) {
			if (e.Window == Window.GetWindow(exceptionsControl))
				ExceptionsVM.RefreshThemeFields();
		}

		void ThemeService_ThemeChanged(object sender, ThemeChangedEventArgs e) => ExceptionsVM.RefreshThemeFields();
		public void Focus() => UIUtilities.FocusSelector(ExceptionsControl.ListBox);

		public void FocusSearchTextBox() {
			ExceptionsControl.SearchTextBox.Focus();
			ExceptionsControl.SearchTextBox.SelectAll();
		}

		public void OnShow() { }
		public void OnClose() { }
		public void OnHidden() { }

		public void OnVisible() {
			// Make sure the images have been refreshed (the DPI could've changed while the control was closed)
			ExceptionsVM.RefreshThemeFields();
		}

		public void OnZoomChanged(double value) {
			if (zoomLevel == value)
				return;
			zoomLevel = value;
			if (initd)
				UpdateImageOptions(ExceptionsVM);
		}
	}
}
