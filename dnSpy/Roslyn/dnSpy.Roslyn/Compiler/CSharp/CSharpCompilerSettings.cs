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

using System.ComponentModel;

namespace dnSpy.Roslyn.Compiler.CSharp {
	/// <summary>
	/// C# compiler settings
	/// </summary>
	abstract class CSharpCompilerSettings : INotifyPropertyChanged {
		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propName">Name of property that got changed</param>
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		/// <summary>
		/// Conditional compilation symbols, separated by ';' or ','
		/// </summary>
		public abstract string PreprocessorSymbols { get; set; }

		/// <summary>
		/// Optimize the code (release builds)
		/// </summary>
		public abstract bool Optimize { get; set; }

		/// <summary>
		/// Add overflow checks
		/// </summary>
		public abstract bool CheckOverflow { get; set; }

		/// <summary>
		/// Allow unsafe code
		/// </summary>
		public abstract bool AllowUnsafe { get; set; }
	}
}
