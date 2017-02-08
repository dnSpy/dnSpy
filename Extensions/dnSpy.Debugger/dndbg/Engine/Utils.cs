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
using dndbg.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	static class Utils {
		public static bool IsDebuggee32Bit => IntPtr.Size == 4;

		public static int DebuggeeIntPtrSize => IntPtr.Size;

		public static bool GetSystemNullableFields(this CorType type, out TokenAndName hasValueInfo, out TokenAndName valueInfo) {
			hasValueInfo = new TokenAndName();
			valueInfo = new TokenAndName();
			if (type == null || !type.IsSystemNullable)
				return false;
			var cls = type.Class;
			var mdi = cls?.Module?.GetMetaDataInterface<IMetaDataImport>();
			var fields = MetaDataUtils.GetFields(mdi, cls?.Token ?? 0);
			if (fields.Count != 2)
				return false;
			if (fields[0].Name != "hasValue")
				return false;
			if (fields[1].Name != "value")
				return false;

			hasValueInfo = fields[0];
			valueInfo = fields[1];
			return true;
		}

		public static bool GetSystemNullableFields(this CorType type, out TokenAndName hasValueInfo, out TokenAndName valueInfo, out CorType nullableElemType) {
			nullableElemType = null;
			if (!type.GetSystemNullableFields(out hasValueInfo, out valueInfo))
				return false;
			nullableElemType = type.FirstTypeParameter;
			return nullableElemType != null;
		}

		public static bool IsSystemNullable(this GenericInstSig gis) {
			if (gis == null)
				return false;
			if (gis.GenericArguments.Count != 1)
				return false;
			var type = gis.GenericType as ValueTypeSig;
			if (type == null)
				return false;

			var mdip = type.TypeDefOrRef as IMetaDataImportProvider;
			if (mdip == null)
				return false;
			return MetaDataUtils.IsSystemNullable(mdip.MetaDataImport, mdip.MDToken.Raw);
		}

		public static ulong? IntegerToUInt64ZeroExtend(object o) {
			if (o == null)
				return null;
			switch (Type.GetTypeCode(o.GetType())) {
			case TypeCode.Boolean:	return (bool)o ? 1UL : 0UL;
			case TypeCode.Char:		return (char)o;
			case TypeCode.SByte:	return (byte)(sbyte)o;
			case TypeCode.Int16:	return (ushort)(short)o;
			case TypeCode.Int32:	return (uint)(int)o;
			case TypeCode.Int64:	return (ulong)(long)o;
			case TypeCode.Byte:		return (byte)o;
			case TypeCode.UInt16:	return (ushort)o;
			case TypeCode.UInt32:	return (uint)o;
			case TypeCode.UInt64:	return (ulong)o;
			}
			if (o is IntPtr)
				return (ulong)((IntPtr)o).ToInt64();
			if (o is UIntPtr)
				return ((UIntPtr)o).ToUInt64();
			return null;
		}

		public static object ConvertValue(ulong value, Type type) {
			switch (Type.GetTypeCode(type)) {
			case TypeCode.Boolean:	return value != 0;
			case TypeCode.Char:		return (char)value;
			case TypeCode.SByte:	return (sbyte)value;
			case TypeCode.Int16:	return (short)value;
			case TypeCode.Int32:	return (int)value;
			case TypeCode.Int64:	return (long)value;
			case TypeCode.Byte:		return (byte)value;
			case TypeCode.UInt16:	return (ushort)value;
			case TypeCode.UInt32:	return (uint)value;
			case TypeCode.UInt64:	return value;
			}
			if (type == typeof(IntPtr)) {
				if (IntPtr.Size == 4)
					return new IntPtr((int)value);
				return new IntPtr((long)value);
			}
			if (type == typeof(UIntPtr)) {
				if (IntPtr.Size == 4)
					return new UIntPtr((uint)value);
				return new UIntPtr(value);
			}
			Debug.Fail("Unsupported type");
			return null;
		}
	}
}
