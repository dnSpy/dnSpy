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

using System.Collections.Generic;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Structure info provider
	/// </summary>
	public abstract class HexStructureInfoProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexStructureInfoProvider() { }

		/// <summary>
		/// Gets all related fields. It's enough to return the span of the current field at
		/// <paramref name="position"/> and the span of the full structure that contains the field.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract IEnumerable<HexStructureField> GetFields(HexPosition position);

		/// <summary>
		/// Gets a tooltip or null
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract object GetToolTip(HexPosition position);

		/// <summary>
		/// Gets a reference or null. The reference can be used to look up a high level
		/// representation of the data, eg. the C# statement in decompiled code.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract object GetReference(HexPosition position);
	}

	/// <summary>
	/// Field kind
	/// </summary>
	public enum HexStructureFieldKind {
		/// <summary>
		/// Some other kind
		/// </summary>
		Other,

		/// <summary>
		/// Span is the full structure
		/// </summary>
		Structure,

		/// <summary>
		/// Span is a sub structure
		/// </summary>
		SubStructure,

		/// <summary>
		/// Span is a field
		/// </summary>
		Field,

		/// <summary>
		/// Span is the current field
		/// </summary>
		CurrentField,
	}

	/// <summary>
	/// Structure field
	/// </summary>
	public struct HexStructureField {
		/// <summary>
		/// Span of field
		/// </summary>
		public HexBufferSpan BufferSpan { get; }

		/// <summary>
		/// Field kind
		/// </summary>
		public HexStructureFieldKind Kind { get; }

		/// <summary>
		/// true if it's the current field
		/// </summary>
		public bool IsCurrentField => Kind == HexStructureFieldKind.CurrentField;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bufferSpan">Span of field</param>
		/// <param name="kind">Field kind</param>
		public HexStructureField(HexBufferSpan bufferSpan, HexStructureFieldKind kind) {
			BufferSpan = bufferSpan;
			Kind = kind;
		}
	}
}
