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
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Debugger.Breakpoints.Code {
	/// <summary>
	/// Formats some columns in the code breakpoints window
	/// </summary>
	public abstract class DbgBreakpointLocationFormatter : INotifyPropertyChanged, IDisposable {
		/// <summary>
		/// Name of the Name property
		/// </summary>
		public const string NameProperty = nameof(NameProperty);

		/// <summary>
		/// Name of the Module property
		/// </summary>
		public const string ModuleProperty = nameof(ModuleProperty);

		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		/// <summary>
		/// Called when the name needs to be reformatted
		/// </summary>
		protected void RaiseNameChanged() => OnPropertyChanged(NameProperty);

		/// <summary>
		/// Called when the module needs to be reformatted
		/// </summary>
		protected void RaiseModuleChanged() => OnPropertyChanged(ModuleProperty);

		/// <summary>
		/// Writes the name shown in the Name column
		/// </summary>
		/// <param name="output">Output</param>
		public abstract void WriteName(ITextColorWriter output);

		/// <summary>
		/// Writes the module shown in the Module column
		/// </summary>
		/// <param name="output">Output</param>
		public abstract void WriteModule(ITextColorWriter output);

		/// <summary>
		/// Called when this instance isn't needed anymore
		/// </summary>
		public abstract void Dispose();
	}
}
