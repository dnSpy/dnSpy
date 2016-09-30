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

using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.Locals {
	interface ILocalsContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ListView ListView { get; }
		ILocalsVM LocalsVM { get; }
	}

	[Export(typeof(ILocalsContent))]
	sealed class LocalsContent : ILocalsContent {
		public object UIObject => localsControl;
		public IInputElement FocusedElement => localsControl.ListView;
		public FrameworkElement ZoomElement => localsControl;
		public ListView ListView => localsControl.ListView;
		public ILocalsVM LocalsVM => vmLocals;

		readonly LocalsControl localsControl;
		readonly ILocalsVM vmLocals;
		double zoomLevel;

		[ImportingConstructor]
		LocalsContent(IWpfCommandService wpfCommandService, IThemeService themeService, IDpiService dpiService, ILocalsVM localsVM) {
			this.localsControl = new LocalsControl();
			this.vmLocals = localsVM;
			this.localsControl.DataContext = this.vmLocals;
			themeService.ThemeChanged += ThemeService_ThemeChanged;
			dpiService.DpiChanged += DpiService_DpiChanged;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_LOCALS_CONTROL, localsControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_LOCALS_LISTVIEW, localsControl.ListView);
			UpdateImageOptions();
		}

		void UpdateImageOptions() {
			var options = new ImageOptions {
				Zoom = new Size(zoomLevel, zoomLevel),
				DpiObject = localsControl,
			};
			vmLocals.SetImageOptions(options);
		}

		void DpiService_DpiChanged(object sender, WindowDpiChangedEventArgs e) {
			if (e.Window == Window.GetWindow(localsControl))
				vmLocals.RefreshThemeFields();
		}

		void ThemeService_ThemeChanged(object sender, ThemeChangedEventArgs e) => vmLocals.RefreshThemeFields();
		public void Focus() => UIUtilities.FocusSelector(localsControl.ListView);
		public void OnClose() => vmLocals.IsEnabled = false;
		public void OnShow() => vmLocals.IsEnabled = true;
		public void OnHidden() => vmLocals.IsVisible = false;
		public void OnVisible() => vmLocals.IsVisible = true;

		public void OnZoomChanged(double value) {
			if (zoomLevel == value)
				return;
			zoomLevel = value;
			UpdateImageOptions();
		}
	}
}
