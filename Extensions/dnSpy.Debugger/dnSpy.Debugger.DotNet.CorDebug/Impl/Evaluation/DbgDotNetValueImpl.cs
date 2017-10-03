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
using System.Runtime.CompilerServices;
using System.Threading;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	sealed class DbgDotNetValueImpl : DbgDotNetValue {
		public override DmdType Type => value.Type;
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
		}

		internal DbgCorValueHolder CorValueHolder => value;

		readonly DbgEngineImpl engine;
		readonly DbgCorValueHolder value;
		readonly DbgDotNetRawValue rawValue;
		readonly ValueFlags flags;
		volatile int disposed;

		public DbgDotNetValueImpl(DbgEngineImpl engine, DbgCorValueHolder value) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.value = value ?? throw new ArgumentNullException(nameof(value));
			var corValue = value.CorValue;
			rawValue = new DbgDotNetRawValueFactory(engine).Create(corValue, value.Type);

			var flags = ValueFlags.None;
			if (corValue.IsReference) {
				flags |= ValueFlags.IsReference;
				if (corValue.IsNull)
					flags |= ValueFlags.IsNullReference;
			}
			if (corValue.IsBox)
				flags |= ValueFlags.IsBox;
			if (corValue.IsArray)
				flags |= ValueFlags.IsArray;
			this.flags = flags;
		}

		public override IDbgDotNetRuntime TryGetDotNetRuntime() => engine.DotNetRuntime;

		internal CorValue TryGetCorValue() {
			try {
				return value.CorValue;
			}
			catch (ObjectDisposedException) {
				return null;
			}
		}

		public override ulong? GetReferenceAddress() {
			if (!IsReference || IsNullReference)
				return null;
			if (engine.CheckCorDebugThread())
				return value.CorValue.ReferenceAddress;
			return engine.InvokeCorDebugThread<ulong?>(() => value.CorValue.ReferenceAddress);
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
			var dereferencedValue = value.CorValue.DereferencedValue;
			if (dereferencedValue == null)
				return null;
			return engine.CreateDotNetValue_CorDebug(dereferencedValue, value.Type.AppDomain, tryCreateStrongHandle: true);
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
			var boxedValue = value.CorValue.BoxedValue;
			if (boxedValue == null)
				return null;
			return engine.CreateDotNetValue_CorDebug(boxedValue, value.Type.AppDomain, tryCreateStrongHandle: true);
		}

		public override bool GetArrayCount(out uint elementCount) {
			if (IsArray) {
				if (engine.CheckCorDebugThread()) {
					elementCount = value.CorValue.ArrayCount;
					return true;
				}
				else {
					elementCount = engine.InvokeCorDebugThread(() => value.CorValue.ArrayCount);
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
			var corValue = value.CorValue;
			elementCount = corValue.ArrayCount;
			var baseIndexes = (corValue.HasBaseIndicies ? corValue.BaseIndicies : null) ?? Array.Empty<uint>();
			var dimensions = corValue.Dimensions;
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
			var elemValue = value.CorValue.GetElementAtPosition(index, out int hr);
			if (elemValue == null)
				return null;
			return engine.CreateDotNetValue_CorDebug(elemValue, value.Type.AppDomain, tryCreateStrongHandle: true);
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

			var v = value.CorValue;
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
			var etype = v.ElementType;
			if (!onlyDataAddress || v.IsValueClass || (CorElementType.Boolean <= etype && etype <= CorElementType.R8) || etype == CorElementType.I || etype == CorElementType.U)
				return new DbgRawAddressValue(addr, size);

			switch (etype) {
			case CorElementType.String:
				uint offsetToStringData;
				if (engine.DebuggeeVersion.StartsWith("v2."))
					offsetToStringData = IntPtr.Size == 4 ? OffsetToStringData32_CLR2 : OffsetToStringData64_CLR2;
				else {
					offsetToStringData = IntPtr.Size == 4 ? OffsetToStringData32 : OffsetToStringData64;
					Debug.Assert(offsetToStringData == RuntimeHelpers.OffsetToStringData);
				}
				uint stringLength = v.StringLength;
				Debug.Assert((ulong)offsetToStringData + stringLength * 2 <= size);
				if (offsetToStringData > size)
					return null;
				return new DbgRawAddressValue(addr + offsetToStringData, stringLength * 2);

			case CorElementType.Array:
			case CorElementType.SZArray:
				return GetArrayAddress(v);
			}

			return new DbgRawAddressValue(addr, size);
		}

		internal static DbgRawAddressValue? GetArrayAddress(CorValue v) {
			var addr = v.Address;
			if (addr == 0)
				return null;
			var arrayCount = v.ArrayCount;
			if (arrayCount == 0)
				return new DbgRawAddressValue(addr, 0);
			var elemValue = v.GetElementAtPosition(0, out int hr);
			ulong elemSize = elemValue?.Size ?? 0;
			ulong elemAddr = elemValue?.Address ?? 0;
			ulong totalSize = elemSize * arrayCount;
			if (elemAddr == 0 || elemAddr < addr)
				return null;
			return new DbgRawAddressValue(elemAddr, totalSize);
		}

		public override DbgDotNetRawValue GetRawValue() => rawValue;

		public override void Dispose() {
			if (Interlocked.Exchange(ref disposed, 1) == 0)
				value.Release();
		}
	}
}
