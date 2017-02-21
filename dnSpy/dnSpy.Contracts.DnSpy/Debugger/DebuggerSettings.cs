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

using System.ComponentModel;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Global debugger settings. This class is thread safe. Listeners will be notified
	/// on a random thread.
	/// </summary>
	public abstract class DebuggerSettings : INotifyPropertyChanged {
		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propName">Name of property that got changed</param>
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		/// <summary>
		/// true to use hexadecimal numbers, false to use decimal numbers
		/// </summary>
		public abstract bool UseHexadecimal { get; set; }

		/// <summary>
		/// true to colorize debugger tool windows and other debugger UI objects
		/// </summary>
		public abstract bool SyntaxHighlight { get; set; }

		/// <summary>
		/// true to auto open the locals window when the debugger starts
		/// </summary>
		public abstract bool AutoOpenLocalsWindow { get; set; }

		/// <summary>
		/// Use modules loaded from memory instead of files. Useful if a module gets decrypted at runtime.
		/// </summary>
		public abstract bool UseMemoryModules { get; set; }

		/// <summary>
		/// true if properties and methods can be executed (used by locals / watch windows)
		/// </summary>
		public abstract bool PropertyEvalAndFunctionCalls { get; set; }

		/// <summary>
		/// Use ToString() or similar method to get a string representation of an object (used by locals / watch windows)
		/// </summary>
		public abstract bool UseStringConversionFunction { get; set; }

		/// <summary>
		/// true to disable detection of managed debuggers
		/// </summary>
		public abstract bool DisableManagedDebuggerDetection { get; set; }

		/// <summary>
		/// true to ignore break instructions and <see cref="System.Diagnostics.Debugger.Break"/> method calls
		/// </summary>
		public abstract bool IgnoreBreakInstructions { get; set; }
	}
}
