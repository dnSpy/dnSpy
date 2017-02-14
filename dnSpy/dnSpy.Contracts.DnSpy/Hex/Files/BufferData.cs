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
using System.Diagnostics;

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
			if (span.IsDefault)
				throw new ArgumentException();
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
		/// <param name="formatter">Formatter</param>
		public abstract void WriteValue(HexFieldFormatter formatter);

		/// <summary>
		/// Returns the span the field value references or null. The span can be empty.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		public virtual HexSpan? GetFieldReferenceSpan(HexBufferFile file) => null;
	}

	/// <summary>
	/// Base class of structures and arrays
	/// </summary>
	public abstract class ComplexData : BufferData {
		/// <summary>
		/// Gets the name or an empty string
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="span">Span</param>
		protected ComplexData(string name, HexBufferSpan span)
			: base(span) => Name = name ?? throw new ArgumentNullException(nameof(name));

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public BufferField this[int index] => GetFieldByIndex(index);

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public BufferField this[string name] => GetFieldByName(name);

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
		/// Gets a field
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <returns></returns>
		public abstract BufferField GetFieldByName(string name);

		/// <summary>
		/// Writes the name
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public abstract void WriteName(HexFieldFormatter formatter);

		/// <summary>
		/// Returns the first field (recursively) that contains a <see cref="SimpleData"/> or null if none was found.
		/// This field could be contained in a nested <see cref="ComplexData"/> instance.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public BufferField GetSimpleField(HexPosition position) {
			var structure = this;
			for (;;) {
				var field = structure.GetFieldByPosition(position);
				if (field == null)
					return null;
				structure = field.Data as ComplexData;
				if (structure == null) {
					Debug.Assert(field.Data is SimpleData);
					return field.Data is SimpleData ? field : null;
				}
			}
		}
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
		/// Gets the fields
		/// </summary>
		protected abstract BufferField[] Fields { get; }

		/// <summary>
		/// Gets the field count
		/// </summary>
		public sealed override int FieldCount => Fields.Length;

		/// <summary>
		/// Gets a field by index
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public sealed override BufferField GetFieldByIndex(int index) => Fields[index];

		/// <summary>
		/// Gets a field by position
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public sealed override BufferField GetFieldByPosition(HexPosition position) {
			foreach (var field in Fields) {
				if (field.Data.Span.Span.Contains(position))
					return field;
			}
			return null;
		}

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <returns></returns>
		public override BufferField GetFieldByName(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			foreach (var field in Fields) {
				if (field.Name == name)
					return field;
			}
			return null;
		}

		/// <summary>
		/// Writes the name
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteName(HexFieldFormatter formatter) => formatter.WriteStructure(Name);
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
		/// Creates a virtual <see cref="byte"/> array
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static VirtualArrayData<ByteData> CreateVirtualByteArray(HexBufferSpan span, string name = null) =>
			new VirtualArrayData<ByteData>(name ?? string.Empty, span, 1, createByteData);
		static readonly Func<HexBufferPoint, ByteData> createByteData = p => new ByteData(p.Buffer, p.Position);

		/// <summary>
		/// Creates a virtual <see cref="ushort"/> array
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static VirtualArrayData<UInt16Data> CreateVirtualUInt16Array(HexBufferSpan span, string name = null) {
			if ((span.Length.ToUInt64() & 1) != 0)
				throw new ArgumentOutOfRangeException(nameof(span));
			return new VirtualArrayData<UInt16Data>(name ?? string.Empty, span, 2, createUInt16Data);
		}
		static readonly Func<HexBufferPoint, UInt16Data> createUInt16Data = p => new UInt16Data(p.Buffer, p.Position);

		/// <summary>
		/// Creates a virtual <see cref="uint"/> array
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static VirtualArrayData<UInt32Data> CreateVirtualUInt32Array(HexBufferSpan span, string name = null) {
			if ((span.Length.ToUInt64() & 3) != 0)
				throw new ArgumentOutOfRangeException(nameof(span));
			return new VirtualArrayData<UInt32Data>(name ?? string.Empty, span, 4, createUInt32Data);
		}
		static readonly Func<HexBufferPoint, UInt32Data> createUInt32Data = p => new UInt32Data(p.Buffer, p.Position);

		/// <summary>
		/// Creates a <see cref="byte"/> array
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="elements">Number of elements</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static ArrayData<ByteData> CreateByteArray(HexBuffer buffer, HexPosition position, int elements, string name = null) {
			var fields = new ArrayField<ByteData>[elements];
			var currPos = position;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<ByteData>(new ByteData(buffer, currPos), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			return new ArrayData<ByteData>(name ?? string.Empty, new HexBufferSpan(buffer, HexSpan.FromBounds(position, currPos)), fields);
		}

		/// <summary>
		/// Creates a <see cref="ushort"/> array
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="elements">Number of elements</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static ArrayData<UInt16Data> CreateUInt16Array(HexBuffer buffer, HexPosition position, int elements, string name = null) {
			var fields = new ArrayField<UInt16Data>[elements];
			var currPos = position;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<UInt16Data>(new UInt16Data(buffer, currPos), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			return new ArrayData<UInt16Data>(name ?? string.Empty, new HexBufferSpan(buffer, HexSpan.FromBounds(position, currPos)), fields);
		}

		/// <summary>
		/// Creates a <see cref="uint"/> array
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="elements">Number of elements</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static ArrayData<UInt32Data> CreateUInt32Array(HexBuffer buffer, HexPosition position, int elements, string name = null) {
			var fields = new ArrayField<UInt32Data>[elements];
			var currPos = position;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<UInt32Data>(new UInt32Data(buffer, currPos), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			return new ArrayData<UInt32Data>(name ?? string.Empty, new HexBufferSpan(buffer, HexSpan.FromBounds(position, currPos)), fields);
		}

		/// <summary>
		/// Creates a <see cref="ulong"/> array
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="elements">Number of elements</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static ArrayData<UInt64Data> CreateUInt64Array(HexBuffer buffer, HexPosition position, int elements, string name = null) {
			var fields = new ArrayField<UInt64Data>[elements];
			var currPos = position;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<UInt64Data>(new UInt64Data(buffer, currPos), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			return new ArrayData<UInt64Data>(name ?? string.Empty, new HexBufferSpan(buffer, HexSpan.FromBounds(position, currPos)), fields);
		}

		/// <summary>
		/// Creates a <see cref="sbyte"/> array
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="elements">Number of elements</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static ArrayData<SByteData> CreateSByteArray(HexBuffer buffer, HexPosition position, int elements, string name = null) {
			var fields = new ArrayField<SByteData>[elements];
			var currPos = position;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<SByteData>(new SByteData(buffer, currPos), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			return new ArrayData<SByteData>(name ?? string.Empty, new HexBufferSpan(buffer, HexSpan.FromBounds(position, currPos)), fields);
		}

		/// <summary>
		/// Creates a <see cref="short"/> array
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="elements">Number of elements</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static ArrayData<Int16Data> CreateInt16Array(HexBuffer buffer, HexPosition position, int elements, string name = null) {
			var fields = new ArrayField<Int16Data>[elements];
			var currPos = position;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<Int16Data>(new Int16Data(buffer, currPos), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			return new ArrayData<Int16Data>(name ?? string.Empty, new HexBufferSpan(buffer, HexSpan.FromBounds(position, currPos)), fields);
		}

		/// <summary>
		/// Creates a <see cref="int"/> array
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="elements">Number of elements</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static ArrayData<Int32Data> CreateInt32Array(HexBuffer buffer, HexPosition position, int elements, string name = null) {
			var fields = new ArrayField<Int32Data>[elements];
			var currPos = position;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<Int32Data>(new Int32Data(buffer, currPos), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			return new ArrayData<Int32Data>(name ?? string.Empty, new HexBufferSpan(buffer, HexSpan.FromBounds(position, currPos)), fields);
		}

		/// <summary>
		/// Creates a <see cref="long"/> array
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="elements">Number of elements</param>
		/// <param name="name">Array name or null</param>
		/// <returns></returns>
		public static ArrayData<Int64Data> CreateInt64Array(HexBuffer buffer, HexPosition position, int elements, string name = null) {
			var fields = new ArrayField<Int64Data>[elements];
			var currPos = position;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<Int64Data>(new Int64Data(buffer, currPos), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			return new ArrayData<Int64Data>(name ?? string.Empty, new HexBufferSpan(buffer, HexSpan.FromBounds(position, currPos)), fields);
		}

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <returns></returns>
		public override BufferField GetFieldByName(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (!int.TryParse(name, out int index))
				return null;
			// Don't throw if it's outside the range, it's a look up by name that should return null if the name doesn't exist
			if ((uint)index >= (uint)FieldCount)
				return null;
			return GetFieldByIndex(index);
		}

		/// <summary>
		/// Writes the name
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteName(HexFieldFormatter formatter) => formatter.WriteArray(Name);
	}

	/// <summary>
	/// An array whose elements all have the same size
	/// </summary>
	/// <typeparam name="TData">Type of data</typeparam>
	public class ArrayData<TData> : ArrayData where TData : BufferData {
		readonly ArrayField<TData>[] fields;

		/// <summary>
		/// Gets the field at <paramref name="index"/>
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public new ArrayField<TData> this[int index] => fields[index];

		/// <summary>
		/// Gets the field count
		/// </summary>
		public override int FieldCount => fields.Length;

		/// <summary>
		/// Constructor, see eg. <see cref="ArrayData.CreateByteArray(HexBuffer, HexPosition, int, string)"/>
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="span">Array span</param>
		/// <param name="fields">Array elements</param>
		public ArrayData(string name, HexBufferSpan span, ArrayField<TData>[] fields)
			: base(name, span) {
			if (fields == null)
				throw new ArgumentNullException(nameof(fields));
#if DEBUG
			for (int i = 1; i < fields.Length; i++) {
				if (fields[i - 1].Data.Span.Length != fields[i].Data.Span.Length)
					throw new ArgumentException();
				if (fields[i - 1].Data.Span.End != fields[i].Data.Span.Start)
					throw new ArgumentException();
			}
			if (fields.Length > 0) {
				if (fields[0].Data.Span.Start != span.Start || fields[fields.Length - 1].Data.Span.End != span.End)
					throw new ArgumentException();
			}
#endif
			this.fields = fields;
		}

		/// <summary>
		/// Gets a field by index
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public override BufferField GetFieldByIndex(int index) => fields[index];

		/// <summary>
		/// Gets a field by position
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public override BufferField GetFieldByPosition(HexPosition position) {
			if (!Span.Contains(position))
				return null;
			int index = (int)((position - Span.Start).ToUInt64() / fields[0].Data.Span.Length.ToUInt64());
			return fields[index];
		}
	}

	/// <summary>
	/// An array whose elements have different sizes
	/// </summary>
	/// <typeparam name="TData">Type of data</typeparam>
	public class VariableLengthArrayData<TData> : ArrayData where TData : BufferData {
		readonly ArrayField<TData>[] fields;

		/// <summary>
		/// Gets the field at <paramref name="index"/>
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public new ArrayField<TData> this[int index] => fields[index];

		/// <summary>
		/// Gets the field count
		/// </summary>
		public override int FieldCount => fields.Length;

		/// <summary>
		/// Constructor, see eg. <see cref="ArrayData.CreateByteArray(HexBuffer, HexPosition, int, string)"/>
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="span">Array span</param>
		/// <param name="fields">Array elements</param>
		public VariableLengthArrayData(string name, HexBufferSpan span, ArrayField<TData>[] fields)
			: base(name, span) => this.fields = fields ?? throw new ArgumentNullException(nameof(fields));

		/// <summary>
		/// Gets a field by index
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public override BufferField GetFieldByIndex(int index) => fields[index];

		/// <summary>
		/// Gets a field by position
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public override BufferField GetFieldByPosition(HexPosition position) {
			if (!Span.Contains(position))
				return null;
			foreach (var field in fields) {
				if (field.Data.Span.Span.Contains(position))
					return field;
			}
			return null;
		}
	}

	/// <summary>
	/// An array whose elements all have the same size and where each element is only created when needed.
	/// The elements aren't cached so calling eg. <see cref="GetFieldByIndex(int)"/> multiple times with
	/// the same input will always return new instances.
	/// </summary>
	/// <typeparam name="TData">Type of data</typeparam>
	public class VirtualArrayData<TData> : ArrayData where TData : BufferData {
		/// <summary>
		/// Gets the field at <paramref name="index"/>
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public new ArrayField<TData> this[int index] {
			get {
				if ((uint)index >= (uint)FieldCount)
					throw new ArgumentOutOfRangeException(nameof(index));
				return new ArrayField<TData>(createElement(Span.Start + elementLength * (uint)index), (uint)index);
			}
		}

		/// <summary>
		/// Gets the field count
		/// </summary>
		public override int FieldCount { get; }

		readonly ulong elementLength;
		readonly Func<HexBufferPoint, TData> createElement;

		/// <summary>
		/// Constructor, see eg. <see cref="ArrayData.CreateByteArray(HexBuffer, HexPosition, int, string)"/>
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="span">Array span</param>
		/// <param name="elementLength">Size of each element in bytes</param>
		/// <param name="createElement">Creates new elements; input parameter is the position of the data</param>
		public VirtualArrayData(string name, HexBufferSpan span, ulong elementLength, Func<HexBufferPoint, TData> createElement)
			: base(name, span) {
			if (elementLength == 0)
				throw new ArgumentOutOfRangeException(nameof(elementLength));
			this.elementLength = elementLength;
			this.createElement = createElement ?? throw new ArgumentNullException(nameof(createElement));
			ulong fieldCount = span.Length.ToUInt64() / elementLength;
			if (fieldCount * elementLength != span.Length)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (fieldCount > int.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(span));
			FieldCount = (int)fieldCount;
		}

		/// <summary>
		/// Gets a field by index
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public override BufferField GetFieldByIndex(int index) => this[index];

		/// <summary>
		/// Gets a field by position
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public override BufferField GetFieldByPosition(HexPosition position) {
			if (!Span.Contains(position))
				return null;
			int index = (int)((position - Span.Start).ToUInt64() / elementLength);
			return this[index];
		}
	}
}
