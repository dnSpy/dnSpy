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
using System.ComponentModel;
using dnSpy.Contracts.Debugger.Text;

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
		/// <param name="options">Options</param>
		public abstract void WriteName(IDbgTextWriter output, DbgBreakpointLocationFormatterOptions options);

		/// <summary>
		/// Writes the module shown in the Module column
		/// </summary>
		/// <param name="output">Output</param>
		public abstract void WriteModule(IDbgTextWriter output);

		/// <summary>
		/// Called when this instance isn't needed anymore
		/// </summary>
		public abstract void Dispose();
	}

	/// <summary>
	/// Formatter options
	/// </summary>
	[Flags]
	public enum DbgBreakpointLocationFormatterOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Show metadata tokens
		/// </summary>
		Tokens					= 0x00000001,

		/// <summary>
		/// Show module names
		/// </summary>
		ModuleNames				= 0x00000002,

		/// <summary>
		/// Show parameter types
		/// </summary>
		ParameterTypes			= 0x00000004,

		/// <summary>
		/// Show parameter names
		/// </summary>
		ParameterNames			= 0x00000008,

		/// <summary>
		/// Show declaring types
		/// </summary>
		DeclaringTypes			= 0x00000010,

		/// <summary>
		/// Show return types
		/// </summary>
		ReturnTypes				= 0x00000020,

		/// <summary>
		/// Show namespaces
		/// </summary>
		Namespaces				= 0x00000040,

		/// <summary>
		/// Show intrinsic type keywords (eg. int instead of Int32)
		/// </summary>
		IntrinsicTypeKeywords	= 0x00000080,

		/// <summary>
		/// Use digit separators
		/// </summary>
		DigitSeparators			= 0x00000100,

		/// <summary>
		/// Use decimal instead of hexadecimal
		/// </summary>
		Decimal					= 0x00000200,
	}
}
