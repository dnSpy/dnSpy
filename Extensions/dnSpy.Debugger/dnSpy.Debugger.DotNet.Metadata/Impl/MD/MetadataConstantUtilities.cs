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

using dnlib.DotNet;
using dnlib.IO;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	static class MetadataConstantUtilities {
		public static object? GetValue(ElementType etype, ref DataReader reader) {
			switch (etype) {
			case ElementType.Boolean:
				if (reader.Length < 1)
					return false;
				return reader.ReadBoolean();

			case ElementType.Char:
				if (reader.Length < 2)
					return (char)0;
				return reader.ReadChar();

			case ElementType.I1:
				if (reader.Length < 1)
					return (sbyte)0;
				return reader.ReadSByte();

			case ElementType.U1:
				if (reader.Length < 1)
					return (byte)0;
				return reader.ReadByte();

			case ElementType.I2:
				if (reader.Length < 2)
					return (short)0;
				return reader.ReadInt16();

			case ElementType.U2:
				if (reader.Length < 2)
					return (ushort)0;
				return reader.ReadUInt16();

			case ElementType.I4:
				if (reader.Length < 4)
					return (int)0;
				return reader.ReadInt32();

			case ElementType.U4:
				if (reader.Length < 4)
					return (uint)0;
				return reader.ReadUInt32();

			case ElementType.I8:
				if (reader.Length < 8)
					return (long)0;
				return reader.ReadInt64();

			case ElementType.U8:
				if (reader.Length < 8)
					return (ulong)0;
				return reader.ReadUInt64();

			case ElementType.R4:
				if (reader.Length < 4)
					return (float)0;
				return reader.ReadSingle();

			case ElementType.R8:
				if (reader.Length < 8)
					return (double)0;
				return reader.ReadDouble();

			case ElementType.String:
				return reader.ReadUtf16String((int)(reader.Length / 2));

			case ElementType.Class:
				return null;

			default:
				return null;
			}
		}
	}
}
