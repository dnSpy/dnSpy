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
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	readonly struct DbgDotNetRawValueFactory {
		readonly DbgEngineImpl engine;

		public DbgDotNetRawValueFactory(DbgEngineImpl engine) => this.engine = engine;

		static DbgDotNetRawValueFactory() {
			var ctor = typeof(DateTime).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(ulong) }, null);
			Debug.Assert(ctor != null);
			if (ctor != null) {
				var dm = new DynamicMethod("DateTime_ctor_UInt64", typeof(DateTime), new[] { typeof(ulong) }, true);
				var ilg = dm.GetILGenerator();
				ilg.Emit(OpCodes.Ldarg_0);
				ilg.Emit(OpCodes.Newobj, ctor);
				ilg.Emit(OpCodes.Ret);
				DateTime_ctor_UInt64 = (Func<ulong, DateTime>)dm.CreateDelegate(typeof(Func<ulong, DateTime>));
			}
		}
		static readonly Func<ulong, DateTime> DateTime_ctor_UInt64;

		public DbgDotNetRawValue Create(CorValue value, DmdType type) => Create(value, type, 0);

		DbgDotNetRawValue Create(CorValue value, DmdType type, int recursionCounter) {
			if (recursionCounter > 1)
				return GetRawValueDefault(value, type);

			if (value.IsNull)
				return GetRawValueDefault(value, type);

			if (type.IsByRef) {
				value = value.GetDereferencedValue(out int hr);
				if (value == null)
					return new DbgDotNetRawValue(DbgSimpleValueType.Other);
				type = GetType(type.AppDomain, value);
			}

			if (value.IsReference) {
				if (value.ElementType == CorElementType.Ptr || value.ElementType == CorElementType.FnPtr) {
					if (type.AppDomain.Runtime.PointerSize == 4)
						return new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, (uint)value.ReferenceAddress);
					return new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, value.ReferenceAddress);
				}
				value = value.GetDereferencedValue(out int hr);
				if (value == null)
					return new DbgDotNetRawValue(DbgSimpleValueType.Other);
				type = GetType(type.AppDomain, value);
			}

			if (value.IsBox) {
				value = value.GetBoxedValue(out int hr);
				if (value == null)
					return new DbgDotNetRawValue(DbgSimpleValueType.Other);
				type = GetType(type.AppDomain, value);
			}

			if (value.IsReference)
				return new DbgDotNetRawValue(DbgSimpleValueType.Other);
			Debug.Assert(value.IsArray == type.IsArray);
			Debug.Assert(value.IsString == (type == type.AppDomain.System_String));
			if (value.IsBox || value.IsArray)
				return new DbgDotNetRawValue(DbgSimpleValueType.Other);
			if (value.IsString) {
				if (type == type.AppDomain.System_String)
					return new DbgDotNetRawValue(DbgSimpleValueType.StringUtf16, value.String);
				return new DbgDotNetRawValue(DbgSimpleValueType.Other);
			}

			var valueType = type.IsEnum ? type.GetEnumUnderlyingType() : type;
			byte[] data;
			switch (DmdType.GetTypeCode(valueType)) {
			case TypeCode.Boolean:
				if (value.Size != 1)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.Boolean, data[0] != 0);

			case TypeCode.Char:
				if (value.Size != 2)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.CharUtf16, BitConverter.ToChar(data, 0));

			case TypeCode.SByte:
				if (value.Size != 1)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.Int8, (sbyte)data[0]);

			case TypeCode.Byte:
				if (value.Size != 1)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.UInt8, data[0]);

			case TypeCode.Int16:
				if (value.Size != 2)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.Int16, BitConverter.ToInt16(data, 0));

			case TypeCode.UInt16:
				if (value.Size != 2)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.UInt16, BitConverter.ToUInt16(data, 0));

			case TypeCode.Int32:
				if (value.Size != 4)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.Int32, BitConverter.ToInt32(data, 0));

			case TypeCode.UInt32:
				if (value.Size != 4)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.UInt32, BitConverter.ToUInt32(data, 0));

			case TypeCode.Int64:
				if (value.Size != 8)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.Int64, BitConverter.ToInt64(data, 0));

			case TypeCode.UInt64:
				if (value.Size != 8)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.UInt64, BitConverter.ToUInt64(data, 0));

			case TypeCode.Single:
				if (value.Size != 4)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.Float32, BitConverter.ToSingle(data, 0));

			case TypeCode.Double:
				if (value.Size != 8)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new DbgDotNetRawValue(DbgSimpleValueType.Float64, BitConverter.ToDouble(data, 0));

			case TypeCode.Decimal:
				if (value.Size != 16)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;

				var decimalBits = new int[4];
				decimalBits[3] = BitConverter.ToInt32(data, 0);
				decimalBits[2] = BitConverter.ToInt32(data, 4);
				decimalBits[0] = BitConverter.ToInt32(data, 8);
				decimalBits[1] = BitConverter.ToInt32(data, 12);
				try {
					return new DbgDotNetRawValue(DbgSimpleValueType.Decimal, new decimal(decimalBits));
				}
				catch (ArgumentException) {
				}
				return new DbgDotNetRawValue(DbgSimpleValueType.Decimal, default(decimal));

			case TypeCode.DateTime:
				if (value.Size != 8)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				if (DateTime_ctor_UInt64 != null)
					return new DbgDotNetRawValue(DbgSimpleValueType.DateTime, DateTime_ctor_UInt64(BitConverter.ToUInt64(data, 0)));
				return new DbgDotNetRawValue(DbgSimpleValueType.DateTime, default(DateTime));

			default:
				if (type == type.AppDomain.System_IntPtr) {
					if (value.Size != (uint)type.AppDomain.Runtime.PointerSize)
						break;
					data = value.ReadGenericValue();
					if (data == null)
						break;
					if (type.AppDomain.Runtime.PointerSize == 4)
						return new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, (uint)BitConverter.ToInt32(data, 0));
					return new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, (ulong)BitConverter.ToInt64(data, 0));
				}
				else if (type == type.AppDomain.System_UIntPtr) {
					if (value.Size != (uint)type.AppDomain.Runtime.PointerSize)
						break;
					data = value.ReadGenericValue();
					if (data == null)
						break;
					if (type.AppDomain.Runtime.PointerSize == 4)
						return new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, BitConverter.ToUInt32(data, 0));
					return new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, BitConverter.ToUInt64(data, 0));
				}
				else if (type.IsNullable) {
					if (!GetNullableValue(type, value, out var nullableValue))
						break;
					if (nullableValue == null)
						return new DbgDotNetRawValue(DbgSimpleValueType.Other, null);
					return Create(nullableValue, type.GetNullableElementType(), recursionCounter + 1);
				}
				break;
			}

			return GetRawValueDefault(value, type);
		}

		DmdType GetType(DmdAppDomain appDomain, CorValue value) =>
			new ReflectionTypeCreator(engine, appDomain).Create(value.ExactType);

		bool GetNullableValue(DmdType nullableType, CorValue nullableValue, out CorValue value) {
			value = null;
			var info = NullableTypeUtils.TryGetNullableFields(nullableType);
			if ((object)info.hasValueField == null)
				return false;

			var cls = nullableValue.ExactType?.Class;
			var hasValueValue = nullableValue.GetFieldValue(cls, (uint)info.hasValueField.MetadataToken);
			if (hasValueValue == null)
				return false;
			var rawValue = hasValueValue.ReadGenericValue();
			if (rawValue == null || rawValue.Length != 1)
				return false;
			if (rawValue[0] == 0)
				return true;

			var valueValue = nullableValue.GetFieldValue(cls, (uint)info.valueField.MetadataToken);
			if (valueValue == null)
				return false;
			value = valueValue;
			return true;
		}

		DbgDotNetRawValue GetRawValueDefault(CorValue value, DmdType type) {
			if (value.IsNull)
				return new DbgDotNetRawValue(DbgSimpleValueType.Other, null);
			return new DbgDotNetRawValue(DbgSimpleValueType.Other);
		}
	}
}
