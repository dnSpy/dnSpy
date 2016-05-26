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
using System.Diagnostics;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Editor option definition
	/// </summary>
	/// <typeparam name="T">Value type</typeparam>
	public abstract class EditorOptionDefinition<T> : EditorOptionDefinition {
		/// <summary>
		/// Gets the name of the option
		/// </summary>
		public sealed override string Name => Key.Name;

		/// <summary>
		/// Gets the default value
		/// </summary>
		public virtual T Default => default(T);

		/// <summary>
		/// Gets the default value
		/// </summary>
		public sealed override object DefaultValue => Default;

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public sealed override Type ValueType => typeof(T);

		/// <summary>
		/// Gets the editor option key
		/// </summary>
		public abstract EditorOptionKey<T> Key { get; }

		/// <summary>
		/// Returns true if the proposed value is valid
		/// </summary>
		/// <param name="proposedValue">Proposed value, can be modified by callee</param>
		/// <returns></returns>
		public sealed override bool IsValid(ref object proposedValue) {
			Debug.Assert(proposedValue is T);
			if (!(proposedValue is T))
				return false;

			var newValue = (T)proposedValue;
			bool isValid = IsValid(ref newValue);
			proposedValue = newValue;
			return isValid;
		}

		/// <summary>
		/// Returns true if the proposed value is valid
		/// </summary>
		/// <param name="proposedValue">Proposed value, can be modified by callee</param>
		/// <returns></returns>
		public virtual bool IsValid(ref T proposedValue) => true;
	}
}
