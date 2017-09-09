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
using dndbg.Engine;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	sealed class DbgDotNetValueImpl : DbgDotNetValue {
		public override DmdType Type => type;

		readonly DbgEngineImpl engine;
		readonly CorValue value;
		readonly DmdType type;
		readonly DbgDotNetRawValue rawValue;

		public DbgDotNetValueImpl(DbgEngineImpl engine, CorValue value, DmdType type) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.value = value ?? throw new ArgumentNullException(nameof(value));
			this.type = type ?? throw new ArgumentNullException(nameof(type));
			rawValue = GetRawValue(value, type);
		}

		public override DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) {
			return null;//TODO:
		}

		public override DbgDotNetRawValue GetRawValue() => rawValue;

		static DbgDotNetRawValue GetRawValue(CorValue value, DmdType type) {
			if (type.IsByRef) {
				if (value.IsNull)
					return GetRawValueDefault(value, type);

				value = value.DereferencedValue;
				Debug.Assert(value != null);
				if (value == null)
					return new DbgDotNetRawValue(DbgSimpleValueType.OtherReferenceType);
				type = type.GetElementType();
			}

			switch (DmdType.GetTypeCode(type)) {
			case TypeCode.Boolean:	return new DbgDotNetRawValue(DbgSimpleValueType.Boolean, value.Value.Value);
			case TypeCode.Char:		return new DbgDotNetRawValue(DbgSimpleValueType.CharUtf16, value.Value.Value);
			case TypeCode.SByte:	return new DbgDotNetRawValue(DbgSimpleValueType.Int8, value.Value.Value);
			case TypeCode.Byte:		return new DbgDotNetRawValue(DbgSimpleValueType.UInt8, value.Value.Value);
			case TypeCode.Int16:	return new DbgDotNetRawValue(DbgSimpleValueType.Int16, value.Value.Value);
			case TypeCode.UInt16:	return new DbgDotNetRawValue(DbgSimpleValueType.UInt16, value.Value.Value);
			case TypeCode.Int32:	return new DbgDotNetRawValue(DbgSimpleValueType.Int32, value.Value.Value);
			case TypeCode.UInt32:	return new DbgDotNetRawValue(DbgSimpleValueType.UInt32, value.Value.Value);
			case TypeCode.Int64:	return new DbgDotNetRawValue(DbgSimpleValueType.Int64, value.Value.Value);
			case TypeCode.UInt64:	return new DbgDotNetRawValue(DbgSimpleValueType.UInt64, value.Value.Value);
			case TypeCode.Single:	return new DbgDotNetRawValue(DbgSimpleValueType.Float32, value.Value.Value);
			case TypeCode.Double:	return new DbgDotNetRawValue(DbgSimpleValueType.Float64, value.Value.Value);
			case TypeCode.Decimal:	return new DbgDotNetRawValue(DbgSimpleValueType.Decimal, value.Value.Value);
			case TypeCode.String:	return new DbgDotNetRawValue(DbgSimpleValueType.StringUtf16, value.Value.Value);

			case TypeCode.Empty:
			case TypeCode.Object:
			case TypeCode.DBNull:
			case TypeCode.DateTime:
			default:
				if (type.IsPointer || type.IsFunctionPointer) {
					var objValue = value.Value.Value;
					if (IntPtr.Size == 4)
						return new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, objValue == null ? 0U : (uint)objValue);
					return new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, objValue == null ? 0UL : (ulong)objValue);
				}
				if (type == type.AppDomain.System_UIntPtr) {
					if (IntPtr.Size == 4)
						return new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, ((UIntPtr)value.Value.Value).ToUInt32());
					return new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, ((UIntPtr)value.Value.Value).ToUInt64());
				}
				if (type == type.AppDomain.System_IntPtr) {
					if (IntPtr.Size == 4)
						return new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, (uint)((IntPtr)value.Value.Value).ToInt32());
					return new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, (ulong)((IntPtr)value.Value.Value).ToInt64());
				}
				return GetRawValueDefault(value, type);
			}
		}

		static DbgDotNetRawValue GetRawValueDefault(CorValue value, DmdType type) {
			if (type.IsValueType) {
				// Could be null if it's a TypedReference
				if (value.IsNull)
					return new DbgDotNetRawValue(DbgSimpleValueType.OtherValueType, null);
				return new DbgDotNetRawValue(DbgSimpleValueType.OtherValueType);
			}
			if (value.IsNull)
				return new DbgDotNetRawValue(DbgSimpleValueType.OtherReferenceType, null);
			return new DbgDotNetRawValue(DbgSimpleValueType.OtherReferenceType);
		}

		public override void Dispose() {
			//TODO: Close CorValue
		}
	}
}
