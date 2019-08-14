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

namespace dnSpy.Debugger.ToolWindows.CallStack {
	/// <summary>
	/// Call stack display settings
	/// </summary>
	abstract class CallStackDisplaySettings : INotifyPropertyChanged {
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
		/// Show return types
		/// </summary>
		public abstract bool ShowReturnTypes { get; set; }

		/// <summary>
		/// Show parameter types
		/// </summary>
		public abstract bool ShowParameterTypes { get; set; }

		/// <summary>
		/// Show parameter names
		/// </summary>
		public abstract bool ShowParameterNames { get; set; }

		/// <summary>
		/// Show parameter values (parameters will be evaluated and formatted)
		/// </summary>
		public abstract bool ShowParameterValues { get; set; }

		/// <summary>
		/// Show the offset of the IP relative to the start of the function
		/// </summary>
		public abstract bool ShowFunctionOffset { get; set; }

		/// <summary>
		/// Show module names
		/// </summary>
		public abstract bool ShowModuleNames { get; set; }

		/// <summary>
		/// Show declaring types
		/// </summary>
		public abstract bool ShowDeclaringTypes { get; set; }

		/// <summary>
		/// Show namespaces
		/// </summary>
		public abstract bool ShowNamespaces { get; set; }

		/// <summary>
		/// Show intrinsic type keywords (eg. int instead of Int32)
		/// </summary>
		public abstract bool ShowIntrinsicTypeKeywords { get; set; }

		/// <summary>
		/// Show tokens
		/// </summary>
		public abstract bool ShowTokens { get; set; }
	}
}
