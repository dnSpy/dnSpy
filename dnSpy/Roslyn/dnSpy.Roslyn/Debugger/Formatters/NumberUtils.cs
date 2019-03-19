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

using System;

namespace dnSpy.Roslyn.Debugger.Formatters {
	static class NumberUtils {
		public static bool TryConvertIntegerToUInt64ZeroExtend(object obj, out ulong result) {
			if (obj == null) {
				result = 0;
				return false;
			}

			switch (Type.GetTypeCode(obj.GetType())) {
			case TypeCode.Boolean:
				result = (bool)obj ? 1UL : 0;
				return true;

			case TypeCode.Char:
				result = (char)obj;
				return true;

			case TypeCode.SByte:
				result = (byte)(sbyte)obj;
				return true;

			case TypeCode.Byte:
				result = (byte)obj;
				return true;

			case TypeCode.Int16:
				result = (ushort)(short)obj;
				return true;

			case TypeCode.UInt16:
				result = (ushort)obj;
				return true;

			case TypeCode.Int32:
				result = (uint)(int)obj;
				return true;

			case TypeCode.UInt32:
				result = (uint)obj;
				return true;

			case TypeCode.Int64:
				result = (ulong)(long)obj;
				return true;

			case TypeCode.UInt64:
				result = (ulong)obj;
				return true;
			}

			result = 0;
			return false;
		}
	}
}
