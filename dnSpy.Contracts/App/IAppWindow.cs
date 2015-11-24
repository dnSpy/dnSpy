/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Controls;

namespace dnSpy.Contracts.App {
	/// <summary>
	/// App window
	/// </summary>
	public interface IAppWindow {
		/// <summary>
		/// Raised when the text formatter setting has been updated
		/// </summary>
		event EventHandler<EventArgs> TextFormatterChanged;

		/// <summary>
		/// Raised when the main window is closing
		/// </summary>
		event EventHandler<CancelEventArgs> MainWindowClosing;

		/// <summary>
		/// Raised when the main window has closed
		/// </summary>
		event EventHandler<EventArgs> MainWindowClosed;

		/// <summary>
		/// Gets the settings
		/// </summary>
		IAppSettings AppSettings { get; }

		/// <summary>
		/// Gets the <see cref="IAppStatusBar"/> instance
		/// </summary>
		IAppStatusBar StatusBar { get; }

		/// <summary>
		/// Gets the <see cref="IWpfCommandManager"/> instance
		/// </summary>
		IWpfCommandManager WpfCommandManager { get; }
	}
}
