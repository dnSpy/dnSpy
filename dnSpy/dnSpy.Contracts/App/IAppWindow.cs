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
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Contracts.App {
	/// <summary>
	/// App window
	/// </summary>
	public interface IAppWindow {
		/// <summary>
		/// Raised when the main window is closing
		/// </summary>
		event EventHandler<CancelEventArgs> MainWindowClosing;

		/// <summary>
		/// Raised when the main window has closed
		/// </summary>
		event EventHandler<EventArgs> MainWindowClosed;

		/// <summary>
		/// Gets the main window
		/// </summary>
		Window MainWindow { get; }

		/// <summary>
		/// Gets the <see cref="IWpfCommands"/> instance
		/// </summary>
		IWpfCommands MainWindowCommands { get; }

		/// <summary>
		/// Gets the settings
		/// </summary>
		IAppSettings AppSettings { get; }

		/// <summary>
		/// Gets the <see cref="IAppStatusBar"/> instance
		/// </summary>
		IAppStatusBar StatusBar { get; }

		/// <summary>
		/// Gets the <see cref="IFileTabManager"/> instance
		/// </summary>
		IFileTabManager FileTabManager { get; }

		/// <summary>
		/// Gets the <see cref="IFileTreeView"/> instance
		/// </summary>
		IFileTreeView FileTreeView { get; }

		/// <summary>
		/// Gets the <see cref="IMainToolWindowManager"/> instance
		/// </summary>
		IMainToolWindowManager ToolWindowManager { get; }

		/// <summary>
		/// Gets the <see cref="ILanguageManager"/> instance
		/// </summary>
		ILanguageManager LanguageManager { get; }

		/// <summary>
		/// true if the app has been loaded
		/// </summary>
		bool AppLoaded { get; }

		/// <summary>
		/// Adds <paramref name="info"/> to the window title
		/// </summary>
		/// <param name="info">Some text</param>
		void AddTitleInfo(string info);

		/// <summary>
		/// Removes <paramref name="info"/> from the window title
		/// </summary>
		/// <param name="info">Some text</param>
		void RemoveTitleInfo(string info);

		/// <summary>
		/// Refreshes the toolbar
		/// </summary>
		void RefreshToolBar();

		/// <summary>
		/// Gets the version (stored in an <see cref="AssemblyInformationalVersionAttribute"/> attribute)
		/// </summary>
		string AssemblyInformationalVersion { get; }

		/// <summary>
		/// Gets the command line args
		/// </summary>
		IAppCommandLineArgs CommandLineArgs { get; }
	}
}
