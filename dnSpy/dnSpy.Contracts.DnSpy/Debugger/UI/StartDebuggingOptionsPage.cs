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
using System.ComponentModel;

namespace dnSpy.Contracts.Debugger.UI {
	/// <summary>
	/// A page shown in the 'debug an application' dialog box. It provides a UI object
	/// and creates a <see cref="StartDebuggingOptions"/> instance that is used to start
	/// the application.
	/// </summary>
	public abstract class StartDebuggingOptionsPage : INotifyPropertyChanged {
		/// <summary>
		/// Raised after a property is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propName">Name of property that got changed</param>
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		/// <summary>
		/// Guid of this page
		/// </summary>
		public abstract Guid Guid { get; }

		/// <summary>
		/// Display order of the UI object compared to other instances, see <see cref="PredefinedStartDebuggingOptionsPageDisplayOrders"/>
		/// </summary>
		public abstract double DisplayOrder { get; }

		/// <summary>
		/// Name of debugger engine shown in the UI, eg. ".NET Framework" or ".NET Core" or "Mono"
		/// </summary>
		public abstract string DisplayName { get; }

		/// <summary>
		/// Gets the UI object
		/// </summary>
		public abstract object UIObject { get; }

		/// <summary>
		/// true if all options are valid and <see cref="GetOptions"/> can be called.
		/// <see cref="PropertyChanged"/> gets raised when this property is changed.
		/// </summary>
		public abstract bool IsValid { get; }

		/// <summary>
		/// Initializes this instance to previous options
		/// </summary>
		/// <param name="options">Options</param>
		public abstract void InitializePreviousOptions(StartDebuggingOptions options);

		/// <summary>
		/// Initializes this instance to default options. If the current filename is not an EXE
		/// file, then <paramref name="options"/> should be used to initialize this instance,
		/// else <paramref name="options"/> should be ignored.
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="options">Options or null</param>
		public abstract void InitializeDefaultOptions(string filename, StartDebuggingOptions options);

		/// <summary>
		/// Gets all options. This method is only called if <see cref="IsValid"/> returns true
		/// </summary>
		/// <returns></returns>
		public abstract StartDebuggingOptionsInfo GetOptions();

		/// <summary>
		/// Called when the dialog box gets closed
		/// </summary>
		public virtual void OnClose() { }
	}

	/// <summary>
	/// Contains the options and an optional filename
	/// </summary>
	public struct StartDebuggingOptionsInfo {
		/// <summary>
		/// Gets the options
		/// </summary>
		public StartDebuggingOptions Options { get; }

		/// <summary>
		/// Filename or null
		/// </summary>
		public string Filename { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		/// <param name="filename">Filename or null</param>
		public StartDebuggingOptionsInfo(StartDebuggingOptions options, string filename = null) {
			Options = options ?? throw new ArgumentNullException(nameof(options));
			Filename = filename;
		}
	}
}
