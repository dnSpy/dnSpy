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
using System.Text;

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// A <see cref="byte"/>
	/// </summary>
	public class ByteData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public ByteData(HexBufferSpan span)
			: base(span) {
			if (span.Length != 1)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public ByteData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 1))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public byte ReadValue() => Span.Buffer.ReadByte(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteByte(ReadValue());
	}

	/// <summary>
	/// A <see cref="ushort"/>
	/// </summary>
	public class UInt16Data : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public UInt16Data(HexBufferSpan span)
			: base(span) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public UInt16Data(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 2))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public ushort ReadValue() => Span.Buffer.ReadUInt16(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteUInt16(ReadValue());
	}

	/// <summary>
	/// A 24-bit <see cref="uint"/>
	/// </summary>
	public class UInt24Data : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public UInt24Data(HexBufferSpan span)
			: base(span) {
			if (span.Length != 3)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public UInt24Data(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 3))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public uint ReadValue() => Span.Buffer.ReadUInt16(Span.Start) | ((uint)Span.Buffer.ReadByte(Span.Start + 2) << 16);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteUInt24(ReadValue());
	}

	/// <summary>
	/// A <see cref="uint"/>
	/// </summary>
	public class UInt32Data : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public UInt32Data(HexBufferSpan span)
			: base(span) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public UInt32Data(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public uint ReadValue() => Span.Buffer.ReadUInt32(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteUInt32(ReadValue());
	}

	/// <summary>
	/// A <see cref="ulong"/>
	/// </summary>
	public class UInt64Data : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public UInt64Data(HexBufferSpan span)
			: base(span) {
			if (span.Length != 8)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public UInt64Data(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 8))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public ulong ReadValue() => Span.Buffer.ReadUInt64(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteUInt64(ReadValue());
	}

	/// <summary>
	/// A <see cref="sbyte"/>
	/// </summary>
	public class SByteData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public SByteData(HexBufferSpan span)
			: base(span) {
			if (span.Length != 1)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public SByteData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 1))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public sbyte ReadValue() => Span.Buffer.ReadSByte(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteSByte(ReadValue());
	}

	/// <summary>
	/// A <see cref="short"/>
	/// </summary>
	public class Int16Data : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public Int16Data(HexBufferSpan span)
			: base(span) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public Int16Data(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 2))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public short ReadValue() => Span.Buffer.ReadInt16(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteInt16(ReadValue());
	}

	/// <summary>
	/// A <see cref="int"/>
	/// </summary>
	public class Int32Data : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public Int32Data(HexBufferSpan span)
			: base(span) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public Int32Data(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public int ReadValue() => Span.Buffer.ReadInt32(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteInt32(ReadValue());
	}

	/// <summary>
	/// A <see cref="long"/>
	/// </summary>
	public class Int64Data : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public Int64Data(HexBufferSpan span)
			: base(span) {
			if (span.Length != 8)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public Int64Data(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 8))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public long ReadValue() => Span.Buffer.ReadInt64(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteInt64(ReadValue());
	}

	/// <summary>
	/// A <see cref="float"/>
	/// </summary>
	public class SingleData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public SingleData(HexBufferSpan span)
			: base(span) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public SingleData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public float ReadValue() => Span.Buffer.ReadSingle(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteSingle(ReadValue());
	}

	/// <summary>
	/// A <see cref="double"/>
	/// </summary>
	public class DoubleData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public DoubleData(HexBufferSpan span)
			: base(span) {
			if (span.Length != 8)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public DoubleData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 8))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public double ReadValue() => Span.Buffer.ReadDouble(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteDouble(ReadValue());
	}

	/// <summary>
	/// A <see cref="byte"/> flags field
	/// </summary>
	public class ByteFlagsData : SimpleData {
		readonly FlagInfo[] flagInfos;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="flagInfos">Flag infos</param>
		public ByteFlagsData(HexBufferSpan span, FlagInfo[] flagInfos)
			: base(span) {
			if (span.Length != 1)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (flagInfos == null)
				throw new ArgumentNullException(nameof(flagInfos));
			this.flagInfos = flagInfos;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="flagInfos">Flag infos</param>
		public ByteFlagsData(HexBuffer buffer, HexPosition position, FlagInfo[] flagInfos)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 1)), flagInfos) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public byte ReadValue() => Span.Buffer.ReadByte(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteFlags(ReadValue(), flagInfos);
	}

	/// <summary>
	/// A <see cref="ushort"/> flags field
	/// </summary>
	public class UInt16FlagsData : SimpleData {
		readonly FlagInfo[] flagInfos;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt16FlagsData(HexBufferSpan span, FlagInfo[] flagInfos)
			: base(span) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (flagInfos == null)
				throw new ArgumentNullException(nameof(flagInfos));
			this.flagInfos = flagInfos;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt16FlagsData(HexBuffer buffer, HexPosition position, FlagInfo[] flagInfos)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 2)), flagInfos) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public ushort ReadValue() => Span.Buffer.ReadUInt16(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteFlags(ReadValue(), flagInfos);
	}

	/// <summary>
	/// A <see cref="uint"/> flags field
	/// </summary>
	public class UInt32FlagsData : SimpleData {
		readonly FlagInfo[] flagInfos;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt32FlagsData(HexBufferSpan span, FlagInfo[] flagInfos)
			: base(span) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (flagInfos == null)
				throw new ArgumentNullException(nameof(flagInfos));
			this.flagInfos = flagInfos;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt32FlagsData(HexBuffer buffer, HexPosition position, FlagInfo[] flagInfos)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4)), flagInfos) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public uint ReadValue() => Span.Buffer.ReadUInt32(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteFlags(ReadValue(), flagInfos);
	}

	/// <summary>
	/// A <see cref="ulong"/> flags field
	/// </summary>
	public class UInt64FlagsData : SimpleData {
		readonly FlagInfo[] flagInfos;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt64FlagsData(HexBufferSpan span, FlagInfo[] flagInfos)
			: base(span) {
			if (span.Length != 8)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (flagInfos == null)
				throw new ArgumentNullException(nameof(flagInfos));
			this.flagInfos = flagInfos;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt64FlagsData(HexBuffer buffer, HexPosition position, FlagInfo[] flagInfos)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 8)), flagInfos) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public ulong ReadValue() => Span.Buffer.ReadUInt64(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteFlags(ReadValue(), flagInfos);
	}

	/// <summary>
	/// A <see cref="byte"/> enum field
	/// </summary>
	public class ByteEnumData : SimpleData {
		readonly EnumFieldInfo[] enumFieldInfos;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public ByteEnumData(HexBufferSpan span, EnumFieldInfo[] enumFieldInfos)
			: base(span) {
			if (span.Length != 1)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (enumFieldInfos == null)
				throw new ArgumentNullException(nameof(enumFieldInfos));
			this.enumFieldInfos = enumFieldInfos;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public ByteEnumData(HexBuffer buffer, HexPosition position, EnumFieldInfo[] enumFieldInfos)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 1)), enumFieldInfos) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public byte ReadValue() => Span.Buffer.ReadByte(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteEnum(ReadValue(), enumFieldInfos);
	}

	/// <summary>
	/// A <see cref="ushort"/> enum field
	/// </summary>
	public class UInt16EnumData : SimpleData {
		readonly EnumFieldInfo[] enumFieldInfos;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt16EnumData(HexBufferSpan span, EnumFieldInfo[] enumFieldInfos)
			: base(span) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (enumFieldInfos == null)
				throw new ArgumentNullException(nameof(enumFieldInfos));
			this.enumFieldInfos = enumFieldInfos;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt16EnumData(HexBuffer buffer, HexPosition position, EnumFieldInfo[] enumFieldInfos)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 2)), enumFieldInfos) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public ushort ReadValue() => Span.Buffer.ReadUInt16(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteEnum(ReadValue(), enumFieldInfos);
	}

	/// <summary>
	/// A <see cref="uint"/> enum field
	/// </summary>
	public class UInt32EnumData : SimpleData {
		readonly EnumFieldInfo[] enumFieldInfos;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt32EnumData(HexBufferSpan span, EnumFieldInfo[] enumFieldInfos)
			: base(span) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (enumFieldInfos == null)
				throw new ArgumentNullException(nameof(enumFieldInfos));
			this.enumFieldInfos = enumFieldInfos;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt32EnumData(HexBuffer buffer, HexPosition position, EnumFieldInfo[] enumFieldInfos)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4)), enumFieldInfos) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public uint ReadValue() => Span.Buffer.ReadUInt32(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteEnum(ReadValue(), enumFieldInfos);
	}

	/// <summary>
	/// A <see cref="ulong"/> enum field
	/// </summary>
	public class UInt64EnumData : SimpleData {
		readonly EnumFieldInfo[] enumFieldInfos;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt64EnumData(HexBufferSpan span, EnumFieldInfo[] enumFieldInfos)
			: base(span) {
			if (span.Length != 8)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (enumFieldInfos == null)
				throw new ArgumentNullException(nameof(enumFieldInfos));
			this.enumFieldInfos = enumFieldInfos;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt64EnumData(HexBuffer buffer, HexPosition position, EnumFieldInfo[] enumFieldInfos)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 8)), enumFieldInfos) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public ulong ReadValue() => Span.Buffer.ReadUInt64(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteEnum(ReadValue(), enumFieldInfos);
	}

	/// <summary>
	/// A <see cref="string"/>
	/// </summary>
	public class StringData : SimpleData {
		/// <summary>
		/// Gets the encoding
		/// </summary>
		public Encoding Encoding { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="encoding">Encoding</param>
		public StringData(HexBufferSpan span, Encoding encoding)
			: base(span) {
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding));
			Encoding = encoding;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="byteLength">String length in bytes</param>
		/// <param name="encoding">Encoding</param>
		public StringData(HexBuffer buffer, HexPosition position, int byteLength, Encoding encoding)
			: this(new HexBufferSpan(buffer, new HexSpan(position, (uint)byteLength)), encoding) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public string ReadValue() => Encoding.GetString(Span.GetData());

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="writer"></param>
		public sealed override void WriteValue(BufferFieldWriter writer) => writer.WriteString(ReadValue());
	}
}
