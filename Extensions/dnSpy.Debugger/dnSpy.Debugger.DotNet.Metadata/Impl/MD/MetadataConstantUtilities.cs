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
using System.Text;
using dnlib.DotNet;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	static class MetadataConstantUtilities {
		public static object GetValue(ElementType etype, byte[] data) {
			switch (etype) {
			case ElementType.Boolean:
				if (data == null || data.Length < 1)
					return false;
				return BitConverter.ToBoolean(data, 0);

			case ElementType.Char:
				if (data == null || data.Length < 2)
					return (char)0;
				return BitConverter.ToChar(data, 0);

			case ElementType.I1:
				if (data == null || data.Length < 1)
					return (sbyte)0;
				return (sbyte)data[0];

			case ElementType.U1:
				if (data == null || data.Length < 1)
					return (byte)0;
				return data[0];

			case ElementType.I2:
				if (data == null || data.Length < 2)
					return (short)0;
				return BitConverter.ToInt16(data, 0);

			case ElementType.U2:
				if (data == null || data.Length < 2)
					return (ushort)0;
				return BitConverter.ToUInt16(data, 0);

			case ElementType.I4:
				if (data == null || data.Length < 4)
					return (int)0;
				return BitConverter.ToInt32(data, 0);

			case ElementType.U4:
				if (data == null || data.Length < 4)
					return (uint)0;
				return BitConverter.ToUInt32(data, 0);

			case ElementType.I8:
				if (data == null || data.Length < 8)
					return (long)0;
				return BitConverter.ToInt64(data, 0);

			case ElementType.U8:
				if (data == null || data.Length < 8)
					return (ulong)0;
				return BitConverter.ToUInt64(data, 0);

			case ElementType.R4:
				if (data == null || data.Length < 4)
					return (float)0;
				return BitConverter.ToSingle(data, 0);

			case ElementType.R8:
				if (data == null || data.Length < 8)
					return (double)0;
				return BitConverter.ToDouble(data, 0);

			case ElementType.String:
				if (data == null)
					return string.Empty;
				return Encoding.Unicode.GetString(data, 0, data.Length / 2 * 2);

			case ElementType.Class:
				return null;

			default:
				return null;
			}
		}
	}
}
