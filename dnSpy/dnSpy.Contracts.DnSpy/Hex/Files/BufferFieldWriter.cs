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

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Writes fields and values
	/// </summary>
	public abstract class BufferFieldWriter {
		/// <summary>
		/// Constructor
		/// </summary>
		protected BufferFieldWriter() { }

		/// <summary>
		/// Writes a structure name
		/// </summary>
		/// <param name="name">Name</param>
		public abstract void WriteStructure(string name);

		/// <summary>
		/// Writes an array name
		/// </summary>
		/// <param name="name">Name</param>
		public abstract void WriteArray(string name);

		/// <summary>
		/// Writes a field name
		/// </summary>
		/// <param name="name">Name</param>
		public abstract void WriteField(string name);

		/// <summary>
		/// Writes an array field name
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="bits">Size of index in bits or 0 to use default</param>
		public abstract void WriteArrayField(uint index, int bits);

		/// <summary>
		/// Writes a signed integer
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="bits">Size of value in bits</param>
		public abstract void WriteSignedInteger(long value, int bits);

		/// <summary>
		/// Writes an unsigned integer
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="bits">Size of value in bits</param>
		public abstract void WriteUnsignedInteger(ulong value, int bits);

		/// <summary>
		/// Writes a <see cref="byte"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteByte(byte value) => WriteUnsignedInteger(value, 8);

		/// <summary>
		/// Writes a <see cref="ushort"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteUInt16(ushort value) => WriteUnsignedInteger(value, 16);

		/// <summary>
		/// Writes a 24-bit <see cref="uint"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteUInt24(uint value) => WriteUnsignedInteger(value, 24);

		/// <summary>
		/// Writes a <see cref="uint"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteUInt32(uint value) => WriteUnsignedInteger(value, 32);

		/// <summary>
		/// Writes a <see cref="ulong"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteUInt64(ulong value) => WriteUnsignedInteger(value, 64);

		/// <summary>
		/// Writes a <see cref="sbyte"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteSByte(sbyte value) => WriteSignedInteger(value, 8);

		/// <summary>
		/// Writes a <see cref="short"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteInt16(short value) => WriteSignedInteger(value, 16);

		/// <summary>
		/// Writes a <see cref="int"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteInt32(int value) => WriteSignedInteger(value, 32);

		/// <summary>
		/// Writes a <see cref="long"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteInt64(long value) => WriteSignedInteger(value, 64);

		/// <summary>
		/// Writes a <see cref="float"/>
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteSingle(float value);

		/// <summary>
		/// Writes a <see cref="double"/>
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteDouble(double value);

		/// <summary>
		/// Writes a <see cref="string"/>
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteString(string value);

		/// <summary>
		/// Writes 8-bit flags
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="flagInfos">Flag infos</param>
		public void WriteFlags(byte value, FlagInfo[] flagInfos) => WriteFlags(value, flagInfos, 8);

		/// <summary>
		/// Writes 16-bit flags
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="flagInfos">Flag infos</param>
		public void WriteFlags(ushort value, FlagInfo[] flagInfos) => WriteFlags(value, flagInfos, 16);

		/// <summary>
		/// Writes 32-bit flags
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="flagInfos">Flag infos</param>
		public void WriteFlags(uint value, FlagInfo[] flagInfos) => WriteFlags(value, flagInfos, 32);

		/// <summary>
		/// Writes 64-bit flags
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="flagInfos">Flag infos</param>
		public void WriteFlags(ulong value, FlagInfo[] flagInfos) => WriteFlags(value, flagInfos, 64);

		/// <summary>
		/// Writes flags
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="flagInfos">Flag infos</param>
		/// <param name="bits">Size of bit field in bits</param>
		public abstract void WriteFlags(ulong value, FlagInfo[] flagInfos, int bits);

		/// <summary>
		/// Writes an 8-bit enum
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public void WriteEnum(byte value, EnumFieldInfo[] enumFieldInfos) => WriteEnum(value, enumFieldInfos, 8);

		/// <summary>
		/// Writes a 16-bit enum
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public void WriteEnum(ushort value, EnumFieldInfo[] enumFieldInfos) => WriteEnum(value, enumFieldInfos, 16);

		/// <summary>
		/// Writes a 32-bit enum
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public void WriteEnum(uint value, EnumFieldInfo[] enumFieldInfos) => WriteEnum(value, enumFieldInfos, 32);

		/// <summary>
		/// Writes a 64-bit enum
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		public void WriteEnum(ulong value, EnumFieldInfo[] enumFieldInfos) => WriteEnum(value, enumFieldInfos, 64);

		/// <summary>
		/// Writes an enum
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="enumFieldInfos">Enum field infos</param>
		/// <param name="bits">Size of enum in bits</param>
		public abstract void WriteEnum(ulong value, EnumFieldInfo[] enumFieldInfos, int bits);
	}
}
