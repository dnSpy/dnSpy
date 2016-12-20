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

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Any data
	/// </summary>
	public abstract class BufferData {
		/// <summary>
		/// Gets the span
		/// </summary>
		public HexBufferSpan Span { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		protected BufferData(HexBufferSpan span) {
			Span = span;
		}
	}

	/// <summary>
	/// Simple data that contains no fields
	/// </summary>
	public abstract class SimpleData : BufferData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		protected SimpleData(HexBufferSpan span)
			: base(span) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer">Writer</param>
		public abstract void WriteValue(BufferFieldWriter writer);
	}

	/// <summary>
	/// Base class of structures and arrays
	/// </summary>
	public abstract class ComplexData : BufferData {
		/// <summary>
		/// Gets the name
		/// </summary>
		protected string Name { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="span">Span</param>
		protected ComplexData(string name, HexBufferSpan span)
			: base(span) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			Name = name;
		}

		/// <summary>
		/// Gets the field count
		/// </summary>
		public abstract int FieldCount { get; }

		/// <summary>
		/// Gets a field by index
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public abstract BufferField GetFieldByIndex(int index);

		/// <summary>
		/// Gets a field by position
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract BufferField GetFieldByPosition(HexPosition position);

		/// <summary>
		/// Writes the name
		/// </summary>
		/// <param name="writer">Writer</param>
		public abstract void WriteName(BufferFieldWriter writer);
	}

	/// <summary>
	/// A structure
	/// </summary>
	public abstract class StructureData : ComplexData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="span">Span</param>
		protected StructureData(string name, HexBufferSpan span)
			: base(name, span) {
		}

		/// <summary>
		/// Writes the name
		/// </summary>
		/// <param name="writer">Writer</param>
		public sealed override void WriteName(BufferFieldWriter writer) => writer.WriteStructure(Name);
	}

	/// <summary>
	/// An array
	/// </summary>
	public abstract class ArrayData : ComplexData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="span">Span</param>
		protected ArrayData(string name, HexBufferSpan span)
			: base(name, span) {
		}

		/// <summary>
		/// Writes the name
		/// </summary>
		/// <param name="writer">Writer</param>
		public sealed override void WriteName(BufferFieldWriter writer) => writer.WriteArray(Name);
	}
}
