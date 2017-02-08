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
using System.Collections.ObjectModel;
using System.Text;

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// A <see cref="bool"/>
	/// </summary>
	public class BooleanData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public BooleanData(HexBufferSpan span)
			: base(span) {
			if (span.Length != 1)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public BooleanData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 1))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public bool ReadValue() => Span.Buffer.ReadByte(Span.Start) != 0;

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteBoolean(ReadValue());
	}

	/// <summary>
	/// A <see cref="char"/>
	/// </summary>
	public class CharData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public CharData(HexBufferSpan span)
			: base(span) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public CharData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 2))) {
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public char ReadValue() => Span.Buffer.ReadChar(Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteChar(ReadValue());
	}

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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteByte(ReadValue());
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteUInt16(ReadValue());
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteUInt24(ReadValue());
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteUInt32(ReadValue());
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteUInt64(ReadValue());
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteSByte(ReadValue());
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteInt16(ReadValue());
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteInt32(ReadValue());
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteInt64(ReadValue());
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteSingle(ReadValue());
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteDouble(ReadValue());
	}

	/// <summary>
	/// A <see cref="decimal"/>
	/// </summary>
	public class DecimalData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public DecimalData(HexBufferSpan span)
			: base(span) {
			if (span.Length != 16)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public DecimalData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 16))) {
		}

		static decimal ReadDecimal(HexBuffer buffer, HexPosition position) {
			var bits = new int[4] {
				buffer.ReadInt32(position),			// lo
				buffer.ReadInt32(position + 4),		// mid
				buffer.ReadInt32(position + 8),		// hi
				buffer.ReadInt32(position + 0x0C),	// flags
			};
			try {
				return new decimal(bits);
			}
			catch (ArgumentException) {
			}
			return decimal.Zero;
		}

		/// <summary>
		/// Reads the value
		/// </summary>
		/// <returns></returns>
		public decimal ReadValue() => ReadDecimal(Span.Buffer, Span.Start);

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteDecimal(ReadValue());
	}

	/// <summary>
	/// Flags data
	/// </summary>
	public abstract class FlagsData : SimpleData {
		/// <summary>
		/// Gets all flag infos
		/// </summary>
		public ReadOnlyCollection<FlagInfo> FlagInfos { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="flagInfos">Flag infos</param>
		protected FlagsData(HexBufferSpan span, ReadOnlyCollection<FlagInfo> flagInfos)
			: base(span) {
			if (flagInfos == null)
				throw new ArgumentNullException(nameof(flagInfos));
			FlagInfos = flagInfos;
		}
	}

	/// <summary>
	/// A <see cref="byte"/> flags field
	/// </summary>
	public class ByteFlagsData : FlagsData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="flagInfos">Flag infos</param>
		public ByteFlagsData(HexBufferSpan span, ReadOnlyCollection<FlagInfo> flagInfos)
			: base(span, flagInfos) {
			if (span.Length != 1)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="flagInfos">Flag infos</param>
		public ByteFlagsData(HexBuffer buffer, HexPosition position, ReadOnlyCollection<FlagInfo> flagInfos)
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteFlags(ReadValue(), FlagInfos);
	}

	/// <summary>
	/// A <see cref="ushort"/> flags field
	/// </summary>
	public class UInt16FlagsData : FlagsData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt16FlagsData(HexBufferSpan span, ReadOnlyCollection<FlagInfo> flagInfos)
			: base(span, flagInfos) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt16FlagsData(HexBuffer buffer, HexPosition position, ReadOnlyCollection<FlagInfo> flagInfos)
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteFlags(ReadValue(), FlagInfos);
	}

	/// <summary>
	/// A <see cref="uint"/> flags field
	/// </summary>
	public class UInt32FlagsData : FlagsData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt32FlagsData(HexBufferSpan span, ReadOnlyCollection<FlagInfo> flagInfos)
			: base(span, flagInfos) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt32FlagsData(HexBuffer buffer, HexPosition position, ReadOnlyCollection<FlagInfo> flagInfos)
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteFlags(ReadValue(), FlagInfos);
	}

	/// <summary>
	/// A <see cref="ulong"/> flags field
	/// </summary>
	public class UInt64FlagsData : FlagsData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt64FlagsData(HexBufferSpan span, ReadOnlyCollection<FlagInfo> flagInfos)
			: base(span, flagInfos) {
			if (span.Length != 8)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="flagInfos">Flag infos</param>
		public UInt64FlagsData(HexBuffer buffer, HexPosition position, ReadOnlyCollection<FlagInfo> flagInfos)
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteFlags(ReadValue(), FlagInfos);
	}

	/// <summary>
	/// Enum data
	/// </summary>
	public abstract class EnumData : SimpleData {
		/// <summary>
		/// Gets all enum field infos
		/// </summary>
		public ReadOnlyCollection<EnumFieldInfo> EnumFieldInfos { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		protected EnumData(HexBufferSpan span, ReadOnlyCollection<EnumFieldInfo> enumFieldInfos)
			: base(span) {
			if (enumFieldInfos == null)
				throw new ArgumentNullException(nameof(enumFieldInfos));
			EnumFieldInfos = enumFieldInfos;
		}
	}

	/// <summary>
	/// A <see cref="byte"/> enum field
	/// </summary>
	public class ByteEnumData : EnumData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public ByteEnumData(HexBufferSpan span, ReadOnlyCollection<EnumFieldInfo> enumFieldInfos)
			: base(span, enumFieldInfos) {
			if (span.Length != 1)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public ByteEnumData(HexBuffer buffer, HexPosition position, ReadOnlyCollection<EnumFieldInfo> enumFieldInfos)
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteEnum(ReadValue(), EnumFieldInfos);
	}

	/// <summary>
	/// A <see cref="ushort"/> enum field
	/// </summary>
	public class UInt16EnumData : EnumData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt16EnumData(HexBufferSpan span, ReadOnlyCollection<EnumFieldInfo> enumFieldInfos)
			: base(span, enumFieldInfos) {
			if (span.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt16EnumData(HexBuffer buffer, HexPosition position, ReadOnlyCollection<EnumFieldInfo> enumFieldInfos)
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteEnum(ReadValue(), EnumFieldInfos);
	}

	/// <summary>
	/// A <see cref="uint"/> enum field
	/// </summary>
	public class UInt32EnumData : EnumData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt32EnumData(HexBufferSpan span, ReadOnlyCollection<EnumFieldInfo> enumFieldInfos)
			: base(span, enumFieldInfos) {
			if (span.Length != 4)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt32EnumData(HexBuffer buffer, HexPosition position, ReadOnlyCollection<EnumFieldInfo> enumFieldInfos)
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteEnum(ReadValue(), EnumFieldInfos);
	}

	/// <summary>
	/// A <see cref="ulong"/> enum field
	/// </summary>
	public class UInt64EnumData : EnumData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt64EnumData(HexBufferSpan span, ReadOnlyCollection<EnumFieldInfo> enumFieldInfos)
			: base(span, enumFieldInfos) {
			if (span.Length != 8)
				throw new ArgumentOutOfRangeException(nameof(span));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public UInt64EnumData(HexBuffer buffer, HexPosition position, ReadOnlyCollection<EnumFieldInfo> enumFieldInfos)
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteEnum(ReadValue(), EnumFieldInfos);
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
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteString(ReadValue());
	}
}
