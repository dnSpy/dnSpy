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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	static class SyntheticValueFactory {
		static DmdType? TryGetType(DmdAppDomain appDomain, object value) {
			if (value is null)
				return null;

			var type = value.GetType();
			switch (Type.GetTypeCode(type)) {
			case TypeCode.Boolean:		return appDomain.System_Boolean;
			case TypeCode.Char:			return appDomain.System_Char;
			case TypeCode.SByte:		return appDomain.System_SByte;
			case TypeCode.Byte:			return appDomain.System_Byte;
			case TypeCode.Int16:		return appDomain.System_Int16;
			case TypeCode.UInt16:		return appDomain.System_UInt16;
			case TypeCode.Int32:		return appDomain.System_Int32;
			case TypeCode.UInt32:		return appDomain.System_UInt32;
			case TypeCode.Int64:		return appDomain.System_Int64;
			case TypeCode.UInt64:		return appDomain.System_UInt64;
			case TypeCode.Single:		return appDomain.System_Single;
			case TypeCode.Double:		return appDomain.System_Double;
			case TypeCode.Decimal:		return appDomain.System_Decimal;
			case TypeCode.DateTime:		return appDomain.System_DateTime;
			case TypeCode.String:		return appDomain.System_String;
			}
			if (type == typeof(IntPtr))
				return appDomain.System_IntPtr;
			if (type == typeof(UIntPtr))
				return appDomain.System_UIntPtr;

			return null;
		}

		public static DbgDotNetValue? TryCreateSyntheticValue(DmdType type, object? constant) {
			switch (DmdType.GetTypeCode(type)) {
			case TypeCode.Boolean:
				if (constant is bool)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Boolean, constant));
				break;

			case TypeCode.Char:
				if (constant is char)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.CharUtf16, constant));
				break;

			case TypeCode.SByte:
				if (constant is sbyte)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Int8, constant));
				break;

			case TypeCode.Byte:
				if (constant is byte)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.UInt8, constant));
				break;

			case TypeCode.Int16:
				if (constant is short)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Int16, constant));
				break;

			case TypeCode.UInt16:
				if (constant is ushort)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.UInt16, constant));
				break;

			case TypeCode.Int32:
				if (constant is int)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Int32, constant));
				break;

			case TypeCode.UInt32:
				if (constant is uint)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.UInt32, constant));
				break;

			case TypeCode.Int64:
				if (constant is long)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Int64, constant));
				break;

			case TypeCode.UInt64:
				if (constant is ulong)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.UInt64, constant));
				break;

			case TypeCode.Single:
				if (constant is float)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Float32, constant));
				break;

			case TypeCode.Double:
				if (constant is double)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Float64, constant));
				break;

			case TypeCode.String:
				if (constant is string || constant is null)
					return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.StringUtf16, constant));
				break;

			default:
				if (type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr) {
					if (type.AppDomain.Runtime.PointerSize == 4) {
						if (constant is int)
							return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, (uint)(int)constant));
						else if (constant is uint)
							return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, constant));
					}
					else {
						if (constant is long)
							return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, (ulong)(long)constant));
						else if (constant is ulong)
							return new SyntheticValue(type, new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, constant));
					}
				}
				else if (constant is null && !type.IsValueType)
					return new SyntheticNullValue(type);
				break;
			}
			return null;
		}
	}

	sealed class SyntheticValue : DbgDotNetValue {
		public override DmdType Type { get; }
		readonly DbgDotNetRawValue rawValue;

		public SyntheticValue(DmdType type, DbgDotNetRawValue rawValue) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.rawValue = rawValue;
		}

		public override DbgDotNetRawValue GetRawValue() => rawValue;
	}

	sealed class SyntheticNullValue : DbgDotNetValue {
		public override DmdType Type { get; }
		public override bool IsNull => true;

		public SyntheticNullValue(DmdType type) =>
			Type = type ?? throw new ArgumentNullException(nameof(type));

		public override DbgDotNetRawValue GetRawValue() => new DbgDotNetRawValue(DbgSimpleValueType.Other, null);
	}
}
