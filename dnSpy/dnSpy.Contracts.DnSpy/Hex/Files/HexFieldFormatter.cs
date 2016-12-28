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

using dnSpy.Contracts.Hex.Text;

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Formats fields and values
	/// </summary>
	public abstract class HexFieldFormatter : HexTextWriter {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexFieldFormatter() { }

		/// <summary>
		/// Writes an equals sign and optional spaces
		/// </summary>
		public abstract void WriteEquals();

		/// <summary>
		/// Writes an array name
		/// </summary>
		/// <param name="name">Name</param>
		public void WriteArray(string name) => Write(name, PredefinedClassifiedTextTags.ArrayName);

		/// <summary>
		/// Writes a structure name
		/// </summary>
		/// <param name="name">Name</param>
		public void WriteStructure(string name) => Write(name, PredefinedClassifiedTextTags.StructureName);

		/// <summary>
		/// Writes a field name
		/// </summary>
		/// <param name="name">Name</param>
		public abstract void WriteField(string name);

		/// <summary>
		/// Writes an array field
		/// </summary>
		/// <param name="index">Index</param>
		public abstract void WriteArrayField(uint index);

		/// <summary>
		/// Writes the field at <paramref name="position"/>
		/// </summary>
		/// <param name="structure">Owner structure</param>
		/// <param name="position">Position of field within <paramref name="structure"/></param>
		public abstract void WriteField(ComplexData structure, HexPosition position);

		/// <summary>
		/// Writes the field value
		/// </summary>
		/// <param name="structure">Owner structure</param>
		/// <param name="position">Position of field within <paramref name="structure"/></param>
		public abstract void WriteValue(ComplexData structure, HexPosition position);

		/// <summary>
		/// Writes the field and value
		/// </summary>
		/// <param name="structure">Owner structure</param>
		/// <param name="position">Position of field within <paramref name="structure"/></param>
		public virtual void WriteFieldAndValue(ComplexData structure, HexPosition position) {
			WriteField(structure, position);
			WriteEquals();
			WriteValue(structure, position);
		}

		/// <summary>
		/// Writes a .NET metadata token
		/// </summary>
		/// <param name="token">Token</param>
		public abstract void WriteToken(uint token);

		/// <summary>
		/// Writes a <see cref="bool"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteBoolean(bool value);

		/// <summary>
		/// Writes a <see cref="char"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteChar(char value);

		/// <summary>
		/// Writes a <see cref="byte"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteByte(byte value);

		/// <summary>
		/// Writes a <see cref="ushort"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteUInt16(ushort value);

		/// <summary>
		/// Writes a 24-bit <see cref="uint"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteUInt24(uint value);

		/// <summary>
		/// Writes a <see cref="uint"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteUInt32(uint value);

		/// <summary>
		/// Writes a <see cref="ulong"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteUInt64(ulong value);

		/// <summary>
		/// Writes a <see cref="sbyte"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteSByte(sbyte value);

		/// <summary>
		/// Writes a <see cref="short"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteInt16(short value);

		/// <summary>
		/// Writes a <see cref="int"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteInt32(int value);

		/// <summary>
		/// Writes a <see cref="long"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteInt64(long value);

		/// <summary>
		/// Writes a <see cref="float"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteSingle(float value);

		/// <summary>
		/// Writes a <see cref="double"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteDouble(double value);

		/// <summary>
		/// Writes a <see cref="decimal"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteDecimal(decimal value);

		/// <summary>
		/// Writes a <see cref="string"/> value
		/// </summary>
		/// <param name="value">Value</param>
		public abstract void WriteString(string value);

		/// <summary>
		/// Writes flags
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="infos">Flag infos</param>
		public abstract void WriteFlags(ulong value, FlagInfo[] infos);

		/// <summary>
		/// Writes an enum value
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="infos">Enum field infos</param>
		public abstract void WriteEnum(ulong value, EnumFieldInfo[] infos);

		/// <summary>
		/// Writes a filename which could contain path separators
		/// </summary>
		/// <param name="filename">Filename with or without path separators</param>
		public abstract void WriteFilename(string filename);

		/// <summary>
		/// Writes an unknown value, eg. "???"
		/// </summary>
		public abstract void WriteUnknownValue();
	}
}
