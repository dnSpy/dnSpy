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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Editor option definition
	/// </summary>
	public abstract class EditorOptionDefinition {
		/// <summary>
		/// Gets the name of the option
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the default value
		/// </summary>
		public abstract object DefaultValue { get; }

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public abstract Type ValueType { get; }

		/// <summary>
		/// Returns true if this option is applicable to <paramref name="scope"/>
		/// </summary>
		/// <param name="scope">Scope</param>
		/// <returns></returns>
		public virtual bool IsApplicableToScope(IPropertyOwner scope) => true;

		/// <summary>
		/// Returns true if the proposed value is valid
		/// </summary>
		/// <param name="proposedValue">Proposed value, can be modified by callee</param>
		/// <returns></returns>
		public virtual bool IsValid(ref object proposedValue) => true;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => (obj as EditorOptionDefinition)?.Name == Name;

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => Name.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => Name;
	}
}
