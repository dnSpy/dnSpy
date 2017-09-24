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
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	sealed class DbgDotNetValueImpl : DbgDotNetValue {
		public override DmdType Type => type;
		public override bool IsReference => (flags & ValueFlags.IsReference) != 0;
		public override bool IsNullReference => (flags & ValueFlags.IsNullReference) != 0;
		public override bool IsBox => (flags & ValueFlags.IsBox) != 0;
		public override bool IsArray => (flags & ValueFlags.IsArray) != 0;

		[Flags]
		enum ValueFlags : byte {
			None				= 0,
			IsReference			= 0x01,
			IsNullReference		= 0x02,
			IsBox				= 0x04,
			IsArray				= 0x08,
			HasFreedHandle		= 0x80,
		}

		internal CorValue Value => value;

		readonly DbgEngineImpl engine;
		readonly CorValue value;
		readonly DmdType type;
		readonly DbgDotNetRawValue rawValue;
		ValueFlags flags;

		public DbgDotNetValueImpl(DbgEngineImpl engine, CorValue value, DmdType type) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.value = value ?? throw new ArgumentNullException(nameof(value));
			this.type = type ?? throw new ArgumentNullException(nameof(type));
			rawValue = GetRawValue(value, type);

			var flags = ValueFlags.None;
			if (value.IsReference) {
				flags |= ValueFlags.IsReference;
				if (value.IsNull)
					flags |= ValueFlags.IsNullReference;
			}
			if (value.IsBox)
				flags |= ValueFlags.IsBox;
			if (value.IsArray)
				flags |= ValueFlags.IsArray;
			this.flags = flags;
		}

		public override ulong? GetReferenceAddress() {
			if (!IsReference || IsNullReference)
				return null;
			if (engine.CheckCorDebugThread())
				return value.ReferenceAddress;
			return engine.InvokeCorDebugThread<ulong?>(() => value.ReferenceAddress);
		}

		public override DbgDotNetValue Dereference() {
			if (!IsReference || IsNullReference)
				return null;
			if (engine.CheckCorDebugThread())
				return Dereference_CorDebug();
			return engine.InvokeCorDebugThread(() => Dereference_CorDebug());
		}

		DbgDotNetValue Dereference_CorDebug() {
			Debug.Assert(IsReference && !IsNullReference);
			engine.VerifyCorDebugThread();
			var dereferencedValue = value.DereferencedValue;
			if (dereferencedValue == null)
				return null;
			return engine.CreateDotNetValue_CorDebug(dereferencedValue, type.AppDomain, tryCreateStrongHandle: true);
		}

		public override DbgDotNetValue Unbox() {
			if (!IsBox)
				return null;
			if (engine.CheckCorDebugThread())
				return Unbox_CorDebug();
			return engine.InvokeCorDebugThread(() => Unbox_CorDebug());
		}

		DbgDotNetValue Unbox_CorDebug() {
			Debug.Assert(IsBox);
			engine.VerifyCorDebugThread();
			var boxedValue = value.BoxedValue;
			if (boxedValue == null)
				return null;
			return engine.CreateDotNetValue_CorDebug(boxedValue, type.AppDomain, tryCreateStrongHandle: true);
		}

		public override bool GetArrayCount(out uint elementCount) {
			if (IsArray) {
				if (engine.CheckCorDebugThread()) {
					elementCount = value.ArrayCount;
					return true;
				}
				else {
					elementCount = engine.InvokeCorDebugThread(() => value.ArrayCount);
					return true;
				}
			}

			elementCount = 0;
			return false;
		}

		public override bool GetArrayInfo(out uint elementCount, out DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			if (IsArray) {
				if (engine.CheckCorDebugThread())
					return GetArrayInfo_CorDebug(out elementCount, out dimensionInfos);
				else {
					uint tmpElementCount = 0;
					DbgDotNetArrayDimensionInfo[] tmpDimensionInfos = null;
					bool res = engine.InvokeCorDebugThread(() => GetArrayInfo_CorDebug(out tmpElementCount, out tmpDimensionInfos));
					elementCount = tmpElementCount;
					dimensionInfos = tmpDimensionInfos;
					return res;
				}
			}

			elementCount = 0;
			dimensionInfos = null;
			return false;
		}

		bool GetArrayInfo_CorDebug(out uint elementCount, out DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			Debug.Assert(IsArray);
			engine.VerifyCorDebugThread();
			elementCount = value.ArrayCount;
			var baseIndexes = (value.HasBaseIndicies ? value.BaseIndicies : null) ?? Array.Empty<uint>();
			var dimensions = value.Dimensions;
			if (dimensions != null) {
				var infos = new DbgDotNetArrayDimensionInfo[dimensions.Length];
				for (int i = 0; i < infos.Length; i++)
					infos[i] = new DbgDotNetArrayDimensionInfo((int)(i < baseIndexes.Length ? baseIndexes[i] : 0), dimensions[i]);
				dimensionInfos = infos;
				return true;
			}
			else {
				dimensionInfos = null;
				return false;
			}
		}

		public override DbgDotNetValue GetArrayElementAt(uint index) {
			if (!IsArray)
				return null;
			if (engine.CheckCorDebugThread())
				return GetArrayElementAt_CorDebug(index);
			return engine.InvokeCorDebugThread(() => GetArrayElementAt_CorDebug(index));
		}

		DbgDotNetValue GetArrayElementAt_CorDebug(uint index) {
			Debug.Assert(IsArray);
			engine.VerifyCorDebugThread();
			var elemValue = value.GetElementAtPosition(index);
			if (elemValue == null)
				return null;
			return engine.CreateDotNetValue_CorDebug(elemValue, type.AppDomain, tryCreateStrongHandle: true);
		}

		public override DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) {
			if (engine.CheckCorDebugThread())
				return GetRawAddressValue_CorDebug(onlyDataAddress);
			return engine.InvokeCorDebugThread(() => GetRawAddressValue_CorDebug(onlyDataAddress));
		}

		// See System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData
		const uint OffsetToStringData32_CLR2 = 12;
		const uint OffsetToStringData64_CLR2 = 16;
		// CLR 4 and .NET Core 1.0 - 2.0
		const uint OffsetToStringData32 = 8;
		const uint OffsetToStringData64 = 12;
		DbgRawAddressValue? GetRawAddressValue_CorDebug(bool onlyDataAddress) {
			engine.VerifyCorDebugThread();

			var v = value;
			if (v.IsNull)
				return null;
			if (v.IsReference) {
				if (v.ElementType == CorElementType.Ptr || v.ElementType == CorElementType.FnPtr)
					return null;
				v = v.DereferencedValue;
				if (v == null)
					return null;
			}
			if (v.IsBox) {
				v = v.BoxedValue;
				if (v == null)
					return null;
			}
			var addr = v.Address;
			var size = v.Size;
			if (addr == 0)
				return null;
			if (!onlyDataAddress || v.IsValueClass || (CorElementType.Boolean <= v.ElementType && v.ElementType <= CorElementType.R8) || v.ElementType == CorElementType.I || v.ElementType == CorElementType.U)
				return new DbgRawAddressValue(addr, size);

			switch (v.ElementType) {
			case CorElementType.String:
				uint offsetToStringData;
				if (engine.DebuggeeVersion.StartsWith("v2."))
					offsetToStringData = IntPtr.Size == 4 ? OffsetToStringData32_CLR2 : OffsetToStringData64_CLR2;
				else
					offsetToStringData = IntPtr.Size == 4 ? OffsetToStringData32 : OffsetToStringData64;
				uint stringLength = v.StringLength;
				Debug.Assert(offsetToStringData + stringLength * 2 <= size);
				Debug.Assert(offsetToStringData <= size);
				if (offsetToStringData > size)
					return null;
				return new DbgRawAddressValue(addr + offsetToStringData, stringLength * 2);

			case CorElementType.Array:
			case CorElementType.SZArray:
				var arryCount = v.ArrayCount;
				if (arryCount == 0)
					return new DbgRawAddressValue(addr, 0);
				var elemValue = v.GetElementAtPosition(0);
				ulong elemSize = elemValue?.Size ?? 0;
				ulong elemAddr = elemValue?.Address ?? 0;
				ulong totalSize = elemSize * arryCount;
				if (elemAddr == 0 || elemAddr < addr)
					return null;
				return new DbgRawAddressValue(elemAddr, totalSize);
			}

			return new DbgRawAddressValue(addr, size);
		}

		public override DbgDotNetRawValue GetRawValue() => rawValue;

		static DbgDotNetRawValue GetRawValue(CorValue value, DmdType type) {
			if (type.IsByRef) {
				if (value.IsNull)
					return GetRawValueDefault(value, type);

				value = value.DereferencedValue;
				Debug.Assert(value != null);
				if (value == null)
					return new DbgDotNetRawValue(DbgSimpleValueType.Other);
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
			case TypeCode.Decimal:	return new DbgDotNetRawValue(DbgSimpleValueType.Decimal, value.Value.Value ?? default(decimal));
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
				if (type == type.AppDomain.System_DateTime)
					return new DbgDotNetRawValue(DbgSimpleValueType.DateTime, value.Value.Value ?? default(DateTime));
				return GetRawValueDefault(value, type);
			}
		}

		static DbgDotNetRawValue GetRawValueDefault(CorValue value, DmdType type) {
			if (value.IsNull)
				return new DbgDotNetRawValue(DbgSimpleValueType.Other, null);
			return new DbgDotNetRawValue(DbgSimpleValueType.Other);
		}

		public override void Dispose() {
			if ((flags & ValueFlags.HasFreedHandle) == 0)
				engine.Close(this);
		}

		internal void Dispose_CorDebug() {
			Debug.Assert(engine.CheckCorDebugThread());
			if ((flags & ValueFlags.HasFreedHandle) != 0)
				return;
			flags |= ValueFlags.HasFreedHandle;
			engine.DisposeHandle_CorDebug(value);
		}
	}
}
