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
	/// A field
	/// </summary>
	public abstract class BufferField {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Data type</param>
		protected BufferField(BufferData data) {
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			Data = data;
		}

		/// <summary>
		/// Gets the name
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the data type
		/// </summary>
		public BufferData Data { get; }

		/// <summary>
		/// Writes the field name
		/// </summary>
		/// <param name="writer">Writer</param>
		public abstract void WriteName(BufferFieldWriter writer);
	}

	/// <summary>
	/// A field in a structure
	/// </summary>
	public class StructField : BufferField {
		/// <summary>
		/// Gets the field name
		/// </summary>
		public sealed override string Name => name;
		readonly string name;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="data">Data type</param>
		public StructField(string name, BufferData data)
			: base(data) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			this.name = name;
		}

		/// <summary>
		/// Writes the field name
		/// </summary>
		/// <param name="writer">Writer</param>
		public sealed override void WriteName(BufferFieldWriter writer) => writer.WriteField(Name);
	}

	/// <summary>
	/// A field in a structure
	/// </summary>
	public class StructField<TData> : StructField where TData : BufferData {
		/// <summary>
		/// Gets the data type
		/// </summary>
		public new TData Data => (TData)base.Data;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="data">Data type</param>
		public StructField(string name, TData data)
			: base(name, data) {
		}
	}

	/// <summary>
	/// An array field
	/// </summary>
	public class ArrayField : BufferField {
		readonly uint index;
		readonly int bits;

		/// <summary>
		/// Gets the name
		/// </summary>
		public sealed override string Name => index.ToString();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Data type</param>
		/// <param name="index">Array index</param>
		/// <param name="bits">Size of index in bits or 0 to use default</param>
		public ArrayField(BufferData data, uint index, int bits)
			: base(data) {
			this.index = index;
			this.bits = bits;
		}

		/// <summary>
		/// Writes the field name
		/// </summary>
		/// <param name="writer">Writer</param>
		public sealed override void WriteName(BufferFieldWriter writer) => writer.WriteArrayField(index, bits);
	}

	/// <summary>
	/// An array field
	/// </summary>
	public class ArrayField<TData> : ArrayField where TData : BufferData {
		/// <summary>
		/// Gets the data type
		/// </summary>
		public new TData Data => (TData)base.Data;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Data type</param>
		/// <param name="index">Array index</param>
		/// <param name="bits">Size of index in bits or 0 to use default</param>
		public ArrayField(TData data, uint index, int bits)
			: base(data, index, bits) {
		}
	}
}
