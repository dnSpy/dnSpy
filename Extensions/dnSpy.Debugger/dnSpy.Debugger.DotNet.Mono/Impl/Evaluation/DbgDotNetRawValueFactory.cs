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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	readonly struct DbgDotNetRawValueFactory {
		readonly DbgEngineImpl engine;

		public DbgDotNetRawValueFactory(DbgEngineImpl engine) => this.engine = engine;

		static DbgDotNetRawValueFactory() {
			var ctor = typeof(DateTime).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(ulong) }, null);
			Debug2.Assert(!(ctor is null));
			if (!(ctor is null)) {
				var dm = new DynamicMethod("DateTime_ctor_UInt64", typeof(DateTime), new[] { typeof(ulong) }, true);
				var ilg = dm.GetILGenerator();
				ilg.Emit(OpCodes.Ldarg_0);
				ilg.Emit(OpCodes.Newobj, ctor);
				ilg.Emit(OpCodes.Ret);
				DateTime_ctor_UInt64 = (Func<ulong, DateTime>)dm.CreateDelegate(typeof(Func<ulong, DateTime>));
			}
		}
		static readonly Func<ulong, DateTime>? DateTime_ctor_UInt64;

		public DbgDotNetRawValue Create(Value value, DmdType type) => Create(value, type, 0);

		DbgDotNetRawValue Create(Value value, DmdType type, int recursionCounter) {
			if (type.IsByRef)
				type = type.GetElementType()!;
			if (recursionCounter > 2 || value is null)
				return GetRawValueDefault(value, type);

			if (value is ObjectMirror && type.IsValueType)
				value = ValueUtils.Unbox((ObjectMirror)value, type);

			switch (value) {
			case PrimitiveValue pv:
				Debug2.Assert((pv.Type == ElementType.Object) == (pv.Value is null));
				switch (pv.Type) {
				case ElementType.Boolean:	return new DbgDotNetRawValue(DbgSimpleValueType.Boolean, pv.Value);
				case ElementType.Char:		return new DbgDotNetRawValue(DbgSimpleValueType.CharUtf16, pv.Value);
				case ElementType.I1:		return new DbgDotNetRawValue(DbgSimpleValueType.Int8, pv.Value);
				case ElementType.U1:		return new DbgDotNetRawValue(DbgSimpleValueType.UInt8, pv.Value);
				case ElementType.I2:		return new DbgDotNetRawValue(DbgSimpleValueType.Int16, pv.Value);
				case ElementType.U2:		return new DbgDotNetRawValue(DbgSimpleValueType.UInt16, pv.Value);
				case ElementType.I4:		return new DbgDotNetRawValue(DbgSimpleValueType.Int32, pv.Value);
				case ElementType.U4:		return new DbgDotNetRawValue(DbgSimpleValueType.UInt32, pv.Value);
				case ElementType.I8:		return new DbgDotNetRawValue(DbgSimpleValueType.Int64, pv.Value);
				case ElementType.U8:		return new DbgDotNetRawValue(DbgSimpleValueType.UInt64, pv.Value);
				case ElementType.R4:		return new DbgDotNetRawValue(DbgSimpleValueType.Float32, pv.Value);
				case ElementType.R8:		return new DbgDotNetRawValue(DbgSimpleValueType.Float64, pv.Value);

				case ElementType.I:
				case ElementType.U:
					if (type.AppDomain.Runtime.PointerSize == 4)
						return new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, (uint)(long)pv.Value!);
					return new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, (ulong)(long)pv.Value!);

				case ElementType.Ptr:
					ulong pval = (ulong)(long)pv.Value!;
					if (pval == 0)
						return new DbgDotNetRawValue(DbgSimpleValueType.Other, null);
					if (type.AppDomain.Runtime.PointerSize == 4)
						return new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, (uint)pval);
					return new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, pval);

				case ElementType.Object:
					// This is a null value
					return new DbgDotNetRawValue(DbgSimpleValueType.Other, null);

				default:
					throw new InvalidOperationException();
				}

			case EnumMirror em:
				if (em.Fields.Length == 1)
					return Create(em.Fields[0], type.GetEnumUnderlyingType(), recursionCounter + 1);
				return GetRawValueDefault(value, type);

			case StructMirror sm:
				if (!type.IsEnum) {
					// Boxed value
					PrimitiveValue? bpv;
					switch (DmdType.GetTypeCode(type)) {
					case TypeCode.Boolean:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is bool)
							return new DbgDotNetRawValue(DbgSimpleValueType.Boolean, bpv.Value);
						break;

					case TypeCode.Char:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is char)
							return new DbgDotNetRawValue(DbgSimpleValueType.CharUtf16, bpv.Value);
						break;

					case TypeCode.SByte:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is sbyte)
							return new DbgDotNetRawValue(DbgSimpleValueType.Int8, bpv.Value);
						break;

					case TypeCode.Byte:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is byte)
							return new DbgDotNetRawValue(DbgSimpleValueType.UInt8, bpv.Value);
						break;

					case TypeCode.Int16:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is short)
							return new DbgDotNetRawValue(DbgSimpleValueType.Int16, bpv.Value);
						break;

					case TypeCode.UInt16:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is ushort)
							return new DbgDotNetRawValue(DbgSimpleValueType.UInt16, bpv.Value);
						break;

					case TypeCode.Int32:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is int)
							return new DbgDotNetRawValue(DbgSimpleValueType.Int32, bpv.Value);
						break;

					case TypeCode.UInt32:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is uint)
							return new DbgDotNetRawValue(DbgSimpleValueType.UInt32, bpv.Value);
						break;

					case TypeCode.Int64:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is long)
							return new DbgDotNetRawValue(DbgSimpleValueType.Int64, bpv.Value);
						break;

					case TypeCode.UInt64:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is ulong)
							return new DbgDotNetRawValue(DbgSimpleValueType.UInt64, bpv.Value);
						break;

					case TypeCode.Single:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is float)
							return new DbgDotNetRawValue(DbgSimpleValueType.Float32, bpv.Value);
						break;

					case TypeCode.Double:
						if (sm.Fields.Length == 1 && !((bpv = sm.Fields[0] as PrimitiveValue) is null) && bpv.Value is double)
							return new DbgDotNetRawValue(DbgSimpleValueType.Float64, bpv.Value);
						break;

					case TypeCode.Decimal:
						return new DbgDotNetRawValue(DbgSimpleValueType.Decimal, ReadDecimal(sm));

					case TypeCode.DateTime:
						return new DbgDotNetRawValue(DbgSimpleValueType.DateTime, ReadDateTime(sm));
					}
				}
				if (type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr) {
					if (sm.Fields.Length == 1 && sm.Fields[0] is PrimitiveValue pv && pv.Value is long) {
						if (type.AppDomain.Runtime.PointerSize == 4)
							return new DbgDotNetRawValue(DbgSimpleValueType.Ptr32, (uint)(long)pv.Value);
						return new DbgDotNetRawValue(DbgSimpleValueType.Ptr64, (ulong)(long)pv.Value);
					}
					return GetRawValueDefault(value, type);
				}
				if (type.IsNullable) {
					var info = GetNullableValues(sm);
					if (info.valueType is null)
						return GetRawValueDefault(value, type);
					if (!info.hasValue)
						return new DbgDotNetRawValue(DbgSimpleValueType.Other, null);
					return Create(info.value, type.GetNullableElementType(), recursionCounter + 1);
				}
				return new DbgDotNetRawValue(DbgSimpleValueType.Other);

			case ArrayMirror am:
				return new DbgDotNetRawValue(DbgSimpleValueType.Other);

			case StringMirror strVal:
				return new DbgDotNetRawValue(DbgSimpleValueType.StringUtf16, strVal.Value);

			case ObjectMirror om:
				return new DbgDotNetRawValue(DbgSimpleValueType.Other);

			default:
				throw new InvalidOperationException();
			}
		}

		static decimal ReadDecimal(StructMirror value) {
			var fields = GetDecimalFields(value);
			if (!(fields is null)) {
				var decimalBits = new int[4];
				decimalBits[0] = fields.Value.lo;
				decimalBits[1] = fields.Value.mid;
				decimalBits[2] = fields.Value.hi;
				decimalBits[3] = fields.Value.flags;
				try {
					return new decimal(decimalBits);
				}
				catch (ArgumentException) {
				}
			}
			return default;
		}

		static (int flags, int hi, int lo, int mid)? GetDecimalFields(StructMirror structMirror) {
			var fields = structMirror.Type.GetFields().Where(a => !a.IsStatic && !a.IsLiteral).ToArray();
			if (fields.Length != 4)
				return default;
			if (fields[0].Name != KnownMemberNames.Decimal_Flags_FieldName || fields[1].Name != KnownMemberNames.Decimal_Hi_FieldName || fields[2].Name != KnownMemberNames.Decimal_Lo_FieldName || fields[3].Name != KnownMemberNames.Decimal_Mid_FieldName)
				return default;
			var fieldValues = structMirror.Fields;
			if (fieldValues.Length != 4)
				return default;
			if (!(fieldValues[0] is PrimitiveValue pvFlags && fieldValues[1] is PrimitiveValue pvHi &&
				fieldValues[2] is PrimitiveValue pvLo && fieldValues[3] is PrimitiveValue pvMid))
				return default;
			// Mono using .NET Core source code
			if (pvFlags.Value is int && pvHi.Value is int && pvLo.Value is int && pvMid.Value is int)
				return ((int)pvFlags.Value, (int)pvHi.Value, (int)pvLo.Value, (int)pvMid.Value);
			// Unity and older Mono
			if (pvFlags.Value is uint && pvHi.Value is uint && pvLo.Value is uint && pvMid.Value is uint)
				return ((int)(uint)pvFlags.Value, (int)(uint)pvHi.Value, (int)(uint)pvLo.Value, (int)(uint)pvMid.Value);
			return default;
		}

		static DateTime ReadDateTime(StructMirror structMirror) {
			var fields = structMirror.Type.GetFields().Where(a => !a.IsStatic && !a.IsLiteral).ToArray();
			var values = structMirror.Fields;
			if (fields.Length != values.Length)
				return default;
			if (fields.Length == 1) {
				// Newer Mono using .NET Core source code

				if (fields[0].Name != KnownMemberNames.DateTime_DateData_FieldName1 && fields[0].Name != KnownMemberNames.DateTime_DateData_FieldName2)
					return default;
				if (values[0] is PrimitiveValue pv && pv.Value is ulong) {
					if (!(DateTime_ctor_UInt64 is null))
						return DateTime_ctor_UInt64((ulong)pv.Value);
					return default;
				}
			}
			else if (fields.Length == 2) {
				// Unity and older Mono

				if (fields[0].Name != KnownMemberNames.DateTime_Ticks_FieldName_Mono || fields[1].Name != KnownMemberNames.DateTime_Kind_FieldName_Mono)
					return default;
				var ticksValue = values[0] as StructMirror;
				var kindValue = values[1] as EnumMirror;
				if (ticksValue is null || kindValue is null)
					return default;

				if (ticksValue.Fields.Length != 1 || !(ticksValue.Fields[0] is PrimitiveValue ticksPM) || !(ticksPM.Value is long))
					return default;
				if (kindValue.Fields.Length != 1 || !(kindValue.Fields[0] is PrimitiveValue kindPM) || !(kindPM.Value is int))
					return default;

				try {
					return new DateTime((long)ticksPM.Value, (DateTimeKind)(int)kindPM.Value);
				}
				catch {
				}
				return default;
			}
			return default;
		}

		(bool hasValue, Value value, TypeMirror valueType) GetNullableValues(StructMirror structMirror) {
			var values = structMirror.Fields;
			var fields = structMirror.Type.GetFields().Where(a => !a.IsStatic && !a.IsLiteral).ToArray();
			if (values.Length != fields.Length)
				return default;
			if (values.Length != 2)
				return default;
			Value hasValue, value;
			TypeMirror valueType;
			if ((fields[0].Name == KnownMemberNames.Nullable_HasValue_FieldName_Mono || fields[0].Name == KnownMemberNames.Nullable_HasValue_FieldName) && fields[1].Name == KnownMemberNames.Nullable_Value_FieldName) {
				hasValue = values[0];
				value = values[1];
				valueType = fields[1].FieldType;
			}
			else if ((fields[1].Name == KnownMemberNames.Nullable_HasValue_FieldName_Mono || fields[1].Name == KnownMemberNames.Nullable_HasValue_FieldName) && fields[0].Name == KnownMemberNames.Nullable_Value_FieldName) {
				hasValue = values[1];
				value = values[0];
				valueType = fields[0].FieldType;
			}
			else
				return default;
			if (hasValue is PrimitiveValue pv && pv.Value is bool)
				return ((bool)pv.Value, value, valueType);
			return default;
		}

		DbgDotNetRawValue GetRawValueDefault(Value? value, DmdType type) {
			if (value is PrimitiveValue pv && pv.Value is null)
				return new DbgDotNetRawValue(DbgSimpleValueType.Other, null);
			return new DbgDotNetRawValue(DbgSimpleValueType.Other);
		}
	}
}
