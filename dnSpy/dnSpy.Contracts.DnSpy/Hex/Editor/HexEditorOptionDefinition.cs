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
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Hex editor option definition
	/// </summary>
	public abstract class HexEditorOptionDefinition : VSTE.EditorOptionDefinition {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexEditorOptionDefinition() { }
	}

	/// <summary>
	/// Hex editor option definition
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class HexEditorOptionDefinition<T> : HexEditorOptionDefinition {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexEditorOptionDefinition() { }

		/// <summary>
		/// Gets the value type
		/// </summary>
		public sealed override Type ValueType => typeof(T);

		/// <summary>
		/// Gets the name of the option
		/// </summary>
		public sealed override string Name => Key.Name;

		/// <summary>
		/// Gets the default value
		/// </summary>
		public sealed override object DefaultValue => Default;

		/// <summary>
		/// Gets the option key
		/// </summary>
		public abstract VSTE.EditorOptionKey<T> Key { get; }

		/// <summary>
		/// Gets the default value
		/// </summary>
		public virtual T Default => default;

		/// <summary>
		/// Checks whether the new value is valid
		/// </summary>
		/// <param name="proposedValue">Proposed value</param>
		/// <returns></returns>
		public sealed override bool IsValid(ref object proposedValue) {
			if (!(proposedValue is T))
				return false;
			var t = (T)proposedValue;
			var res = IsValid(ref t);
			proposedValue = t;
			return res;
		}

		/// <summary>
		/// Checks whether the new value is valid
		/// </summary>
		/// <param name="proposedValue">Proposed value</param>
		/// <returns></returns>
		public virtual bool IsValid(ref T proposedValue) => true;
	}

	/// <summary>
	/// <see cref="HexView"/> option definition
	/// </summary>
	public abstract class HexViewOptionDefinition<T> : HexEditorOptionDefinition<T> {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexViewOptionDefinition() { }

		/// <summary>
		/// Returns true if <paramref name="scope"/> is a <see cref="HexView"/>
		/// </summary>
		/// <param name="scope">Scope</param>
		/// <returns></returns>
		public override bool IsApplicableToScope(VSUTIL.IPropertyOwner scope) => scope is HexView;
	}

	/// <summary>
	/// <see cref="WpfHexView"/> option definition
	/// </summary>
	public abstract class WpfHexViewOptionDefinition<T> : HexEditorOptionDefinition<T> {
		/// <summary>
		/// Constructor
		/// </summary>
		protected WpfHexViewOptionDefinition() { }

		/// <summary>
		/// Returns true if <paramref name="scope"/> is a <see cref="WpfHexView"/>
		/// </summary>
		/// <param name="scope">Scope</param>
		/// <returns></returns>
		public override bool IsApplicableToScope(VSUTIL.IPropertyOwner scope) => scope is WpfHexView;
	}
}
