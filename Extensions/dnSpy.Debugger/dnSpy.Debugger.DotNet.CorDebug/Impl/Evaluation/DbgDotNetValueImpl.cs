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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	sealed class DbgDotNetValueImpl : DbgDotNetValue {
		public override DmdType Type { get; }
		public override bool IsNull => (flags & ValueFlags.IsNull) != 0;
		bool IsNullByRef => (flags & ValueFlags.IsNullByRef) != 0;

		[Flags]
		enum ValueFlags : byte {
			None				= 0,
			IsNull				= 0x01,
			IsNullByRef			= 0x02,
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
			Type = value.Type;
			var corValue = value.CorValue;
			rawValue = new DbgDotNetRawValueFactory(engine).Create(corValue, Type);

			var flags = ValueFlags.None;
			if (corValue.IsNull) {
				if (Type.IsByRef)
					flags |= ValueFlags.IsNullByRef;
				else
					flags |= ValueFlags.IsNull;
			}
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

		public override DbgDotNetValue LoadIndirect() {
			if (!Type.IsByRef)
				return null;
			if (IsNullByRef)
				return new SyntheticNullValue(Type.GetElementType());
			if (engine.CheckCorDebugThread())
				return Dereference_CorDebug();
			return engine.InvokeCorDebugThread(() => Dereference_CorDebug());
		}

		DbgDotNetValue Dereference_CorDebug() {
			Debug.Assert(Type.IsByRef && !IsNullByRef);
			engine.VerifyCorDebugThread();
			var dereferencedValue = TryGetCorValue()?.DereferencedValue;
			// We sometimes get 0x80131c49 = CORDBG_E_READVIRTUAL_FAILURE
			if (dereferencedValue == null)
				return new SyntheticNullValue(Type.GetElementType());
			return engine.CreateDotNetValue_CorDebug(dereferencedValue, Type.AppDomain, tryCreateStrongHandle: true);
		}

		public override string StoreIndirect(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken) {
			if (!Type.IsByRef)
				return CordbgErrorHelper.InternalError;
			if (engine.CheckCorDebugThread())
				return StoreIndirect_CorDebug(context, frame, value, cancellationToken);
			return engine.InvokeCorDebugThread(() => StoreIndirect_CorDebug(context, frame, value, cancellationToken));
		}

		string StoreIndirect_CorDebug(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken) {
			engine.VerifyCorDebugThread();
			cancellationToken.ThrowIfCancellationRequested();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return CordbgErrorHelper.InternalError;
			if (!Type.IsByRef)
				return CordbgErrorHelper.InternalError;
			Func<CreateCorValueResult> createTargetValue = () => {
				var objValue = TryGetCorValue();
				if (objValue == null)
					return new CreateCorValueResult(null, -1);
				Debug.Assert(objValue.ElementType == CorElementType.ByRef);
				if (objValue.ElementType == CorElementType.ByRef) {
					var derefencedValue = objValue.DereferencedValue;
					if (derefencedValue == null)
						return new CreateCorValueResult(null, -1);
					if (!derefencedValue.IsReference) {
						if (derefencedValue.IsGeneric)
							return new CreateCorValueResult(derefencedValue, 0, canDispose: true);
						engine.DisposeHandle_CorDebug(derefencedValue);
						return new CreateCorValueResult(null, -1);
					}
					return new CreateCorValueResult(derefencedValue, 0, canDispose: true);
				}
				else
					return new CreateCorValueResult(null, -1);
			};
			return engine.StoreValue_CorDebug(context, frame.Thread, ilFrame, createTargetValue, Type.GetElementType(), value, cancellationToken);
		}

		struct ArrayObjectValue : IDisposable {
			readonly DbgEngineImpl engine;
			public readonly CorValue Value;
			readonly bool ownsValue;
			public ArrayObjectValue(DbgEngineImpl engine, CorValue value) {
				this.engine = engine;
				Debug.Assert(!value.IsNull);
				if (value.IsReference) {
					Value = value.DereferencedValue;
					ownsValue = true;
				}
				else {
					Value = value;
					ownsValue = false;
				}
				// Value is sometimes null, DereferencedValue can fail with 0x80131305 = CORDBG_E_BAD_REFERENCE_VALUE
				Debug.Assert(Value == null || Value.IsArray);
			}

			public void Dispose() {
				if (ownsValue)
					engine.DisposeHandle_CorDebug(Value);
			}
		}

		public override bool GetArrayCount(out uint elementCount) {
			if (Type.IsArray) {
				if (engine.CheckCorDebugThread()) {
					elementCount = GetArrayCountCore_CorDebug();
					return true;
				}
				else {
					elementCount = engine.InvokeCorDebugThread(() => GetArrayCountCore_CorDebug());
					return true;
				}
			}

			elementCount = 0;
			return false;
		}

		uint GetArrayCountCore_CorDebug() {
			Debug.Assert(Type.IsArray);
			engine.VerifyCorDebugThread();
			var corValue = TryGetCorValue();
			if (corValue == null || corValue.IsNull)
				return 0;
			using (var obj = new ArrayObjectValue(engine, corValue))
				return obj.Value?.ArrayCount ?? 0;
		}

		public override bool GetArrayInfo(out uint elementCount, out DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			if (Type.IsArray) {
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
			Debug.Assert(Type.IsArray);
			engine.VerifyCorDebugThread();
			var corValue = TryGetCorValue();
			if (corValue == null || corValue.IsNull) {
				elementCount = 0;
				dimensionInfos = null;
				return false;
			}
			using (var obj = new ArrayObjectValue(engine, corValue)) {
				if (obj.Value == null) {
					elementCount = 0;
					dimensionInfos = null;
					return false;
				}
				elementCount = obj.Value.ArrayCount;
				var baseIndexes = (obj.Value.HasBaseIndicies ? obj.Value.BaseIndicies : null) ?? Array.Empty<uint>();
				var dimensions = obj.Value.Dimensions;
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
		}

		public override DbgDotNetValue GetArrayElementAt(uint index) {
			if (!Type.IsArray)
				return null;
			if (engine.CheckCorDebugThread())
				return GetArrayElementAt_CorDebug(index);
			return engine.InvokeCorDebugThread(() => GetArrayElementAt_CorDebug(index));
		}

		DbgDotNetValue GetArrayElementAt_CorDebug(uint index) {
			Debug.Assert(Type.IsArray);
			engine.VerifyCorDebugThread();
			var corValue = TryGetCorValue();
			if (corValue == null || corValue.IsNull)
				return null;
			using (var obj = new ArrayObjectValue(engine, corValue)) {
				var elemValue = obj.Value?.GetElementAtPosition(index, out int hr);
				if (elemValue == null)
					return null;
				return engine.CreateDotNetValue_CorDebug(elemValue, Type.AppDomain, tryCreateStrongHandle: true);
			}
		}

		public override string SetArrayElementAt(DbgEvaluationContext context, DbgStackFrame frame, uint index, object value, CancellationToken cancellationToken) {
			if (!Type.IsArray)
				return base.SetArrayElementAt(context, frame, index, value, cancellationToken);
			if (engine.CheckCorDebugThread())
				return SetArrayElementAt_CorDebug(context, frame, index, value, cancellationToken);
			return engine.InvokeCorDebugThread(() => SetArrayElementAt_CorDebug(context, frame, index, value, cancellationToken));
		}

		string SetArrayElementAt_CorDebug(DbgEvaluationContext context, DbgStackFrame frame, uint index, object value, CancellationToken cancellationToken) {
			engine.VerifyCorDebugThread();
			cancellationToken.ThrowIfCancellationRequested();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return CordbgErrorHelper.InternalError;
			Func<CreateCorValueResult> createTargetValue = () => {
				var corValue = TryGetCorValue();
				if (corValue == null || corValue.IsNull)
					return new CreateCorValueResult(null, -1);
				using (var obj = new ArrayObjectValue(engine, corValue)) {
					if (obj.Value == null)
						return new CreateCorValueResult(null, -1);
					var elemValue = obj.Value.GetElementAtPosition(index, out int hr);
					return new CreateCorValueResult(elemValue, hr);
				}
			};
			return engine.StoreValue_CorDebug(context, frame.Thread, ilFrame, createTargetValue, Type.GetElementType(), value, cancellationToken);
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

			var v = TryGetCorValue();
			if (v == null)
				return null;

			if (Type.IsByRef) {
				if (IsNullByRef)
					return null;
				return new DbgRawAddressValue(v.Address, (uint)Type.AppDomain.Runtime.PointerSize);
			}

			if (IsNull)
				return null;

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
