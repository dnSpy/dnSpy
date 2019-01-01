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
using System.Diagnostics;
using System.Windows;
using dnSpy.Contracts.Controls;

namespace dnSpy.Controls {
	/// <summary>
	/// Raises events whenever the DPI of a window is changed
	/// </summary>
	interface IDpiService {
		/// <summary>
		/// Gets the DPI of the screen that contains the main window
		/// </summary>
		Size MainWindowDpi { get; }

		/// <summary>
		/// Raised when the DPI has changed
		/// </summary>
		event EventHandler<WindowDpiChangedEventArgs> DpiChanged;
	}

	[Export(typeof(IDpiService))]
	sealed class DpiService : IDpiService {
		public Size MainWindowDpi { get; private set; }
		public event EventHandler<WindowDpiChangedEventArgs> DpiChanged;

		DpiService() {
			Debug.Assert(Application.Current == null || Application.Current.Windows.Count == 0);
			MetroWindow.MetroWindowCreated += MetroWindow_MetroWindowCreated;
		}

		void MetroWindow_MetroWindowCreated(object sender, MetroWindowCreatedEventArgs e) {
			e.MetroWindow.WindowDpiChanged += MetroWindow_WindowDpiChanged;
			e.MetroWindow.Closed += MetroWindow_Closed;
			UpdateMainWindowDpi(e.MetroWindow);
		}

		void UpdateMainWindowDpi(MetroWindow window) {
			if (window == Application.Current.MainWindow)
				MainWindowDpi = window.WindowDpi;
		}

		void MetroWindow_WindowDpiChanged(object sender, EventArgs e) {
			var window = (MetroWindow)sender;
			UpdateMainWindowDpi(window);
			DpiChanged?.Invoke(this, new WindowDpiChangedEventArgs(window));
		}

		void MetroWindow_Closed(object sender, EventArgs e) {
			var window = (MetroWindow)sender;
			window.WindowDpiChanged -= MetroWindow_WindowDpiChanged;
			window.Closed -= MetroWindow_Closed;
		}
	}
}
