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
using System.Diagnostics.CodeAnalysis;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
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

		internal ValueLocation ValueLocation => valueLocation;
		internal Value Value => value;

		readonly DbgEngineImpl engine;
		readonly ValueLocation valueLocation;
		readonly Value value;
		readonly DbgDotNetRawValue rawValue;
		readonly ValueFlags flags;

		public DbgDotNetValueImpl(DbgEngineImpl engine, ValueLocation valueLocation, Value value) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.valueLocation = valueLocation ?? throw new ArgumentNullException(nameof(valueLocation));
			this.value = value ?? throw new ArgumentNullException(nameof(value));
			Type = MonoValueTypeCreator.CreateType(engine, value, valueLocation.Type);
			rawValue = new DbgDotNetRawValueFactory(engine).Create(value, Type);

			var flags = ValueFlags.None;
			if (value is PrimitiveValue pv && (pv.Value is null || ((Type.IsPointer || Type.IsFunctionPointer) && boxed0L.Equals(pv.Value)))) {
				if (Type.IsByRef)
					flags |= ValueFlags.IsNullByRef;
				else
					flags |= ValueFlags.IsNull;
			}
			this.flags = flags;
		}
		static readonly object boxed0L = 0L;

		public override IDbgDotNetRuntime? TryGetDotNetRuntime() => engine.DotNetRuntime;

		public override DbgDotNetValueResult LoadIndirect() {
			if (!Type.IsByRef)
				return base.LoadIndirect();
			if (IsNullByRef)
				return DbgDotNetValueResult.Create(new SyntheticNullValue(Type.GetElementType()!));
			if (engine.CheckMonoDebugThread())
				return Dereference_MonoDebug();
			return engine.InvokeMonoDebugThread(() => Dereference_MonoDebug());
		}

		DbgDotNetValueResult Dereference_MonoDebug() {
			Debug.Assert(Type.IsByRef && !IsNullByRef);
			engine.VerifyMonoDebugThread();
			return DbgDotNetValueResult.Create(engine.CreateDotNetValue_MonoDebug(valueLocation.Dereference()));
		}

		public override string? StoreIndirect(DbgEvaluationInfo evalInfo, object? value) {
			if (!Type.IsByRef)
				return PredefinedEvaluationErrorMessages.InternalDebuggerError;
			if (engine.CheckMonoDebugThread())
				return StoreIndirect_MonoDebug(evalInfo, value);
			return engine.InvokeMonoDebugThread(() => StoreIndirect_MonoDebug(evalInfo, value));
		}

		string? StoreIndirect_MonoDebug(DbgEvaluationInfo evalInfo, object? value) {
			engine.VerifyMonoDebugThread();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			if (!Type.IsByRef)
				return PredefinedEvaluationErrorMessages.InternalDebuggerError;
			var res = engine.CreateMonoValue_MonoDebug(evalInfo, value, Type.GetElementType()!);
			if (!(res.ErrorMessage is null))
				return res.ErrorMessage;
			return valueLocation.Store(res.Value!);
		}

		public override bool GetArrayCount(out uint elementCount) {
			if (Type.IsArray) {
				if (engine.CheckMonoDebugThread()) {
					elementCount = GetArrayCountCore_MonoDebug();
					return true;
				}
				else {
					elementCount = engine.InvokeMonoDebugThread(() => GetArrayCountCore_MonoDebug());
					return true;
				}
			}

			elementCount = 0;
			return false;
		}

		uint GetArrayCountCore_MonoDebug() {
			Debug.Assert(Type.IsArray);
			engine.VerifyMonoDebugThread();
			var arrayMirror = value as ArrayMirror;
			if (arrayMirror is null)
				return 0;
			return (uint)arrayMirror.Length;
		}

		public override bool GetArrayInfo(out uint elementCount, [NotNullWhen(true)] out DbgDotNetArrayDimensionInfo[]? dimensionInfos) {
			if (Type.IsArray) {
				if (engine.CheckMonoDebugThread())
					return GetArrayInfo_MonoDebug(out elementCount, out dimensionInfos);
				else {
					uint tmpElementCount = 0;
					DbgDotNetArrayDimensionInfo[]? tmpDimensionInfos = null;
					bool res = engine.InvokeMonoDebugThread(() => GetArrayInfo_MonoDebug(out tmpElementCount, out tmpDimensionInfos));
					elementCount = tmpElementCount;
					dimensionInfos = tmpDimensionInfos;
					return res;
				}
			}

			elementCount = 0;
			dimensionInfos = null;
			return false;
		}

		bool GetArrayInfo_MonoDebug(out uint elementCount, [NotNullWhen(true)] out DbgDotNetArrayDimensionInfo[]? dimensionInfos) {
			Debug.Assert(Type.IsArray);
			engine.VerifyMonoDebugThread();
			var arrayMirror = value as ArrayMirror;
			if (arrayMirror is null) {
				elementCount = 0;
				dimensionInfos = null;
				return false;
			}
			elementCount = (uint)arrayMirror.Length;
			var infos = new DbgDotNetArrayDimensionInfo[arrayMirror.Rank];
			for (int i = 0; i < infos.Length; i++)
				infos[i] = new DbgDotNetArrayDimensionInfo(arrayMirror.GetLowerBound(i), (uint)arrayMirror.GetLength(i));
			dimensionInfos = infos;
			return true;
		}

		public override DbgDotNetValueResult GetArrayElementAt(uint index) {
			if (!Type.IsArray)
				return base.GetArrayElementAt(index);
			if (engine.CheckMonoDebugThread())
				return GetArrayElementAt_MonoDebug(index);
			return engine.InvokeMonoDebugThread(() => GetArrayElementAt_MonoDebug(index));
		}

		DbgDotNetValueResult GetArrayElementAt_MonoDebug(uint index) {
			Debug.Assert(Type.IsArray);
			engine.VerifyMonoDebugThread();
			var info = GetArrayElementValueLocation_MonoDebug(index);
			if (!(info.errorMessage is null))
				return DbgDotNetValueResult.CreateError(info.errorMessage);
			return DbgDotNetValueResult.Create(engine.CreateDotNetValue_MonoDebug(info.valueLocation!));
		}

		(ArrayElementValueLocation? valueLocation, string? errorMessage) GetArrayElementValueLocation_MonoDebug(uint index) {
			Debug.Assert(Type.IsArray);
			engine.VerifyMonoDebugThread();
			var arrayMirror = value as ArrayMirror;
			if (arrayMirror is null)
				return (null, PredefinedEvaluationErrorMessages.InternalDebuggerError);
			return (new ArrayElementValueLocation(Type.GetElementType()!, arrayMirror, index), null);
		}

		public override string? SetArrayElementAt(DbgEvaluationInfo evalInfo, uint index, object? value) {
			if (!Type.IsArray)
				return base.SetArrayElementAt(evalInfo, index, value);
			if (engine.CheckMonoDebugThread())
				return SetArrayElementAt_MonoDebug(evalInfo, index, value);
			return engine.InvokeMonoDebugThread(() => SetArrayElementAt_MonoDebug(evalInfo, index, value));
		}

		string? SetArrayElementAt_MonoDebug(DbgEvaluationInfo evalInfo, uint index, object? value) {
			engine.VerifyMonoDebugThread();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			var info = GetArrayElementValueLocation_MonoDebug(index);
			if (!(info.errorMessage is null))
				return info.errorMessage;
			var res = engine.CreateMonoValue_MonoDebug(evalInfo, value, info.valueLocation!.Type);
			if (!(res.ErrorMessage is null))
				return res.ErrorMessage;
			return info.valueLocation.Store(res.Value!);
		}

		public override DbgDotNetValueResult? Box(DbgEvaluationInfo evalInfo) {
			if (engine.CheckMonoDebugThread())
				return Box_MonoDebug(evalInfo);
			return engine.InvokeMonoDebugThread(() => Box_MonoDebug(evalInfo));
		}

		DbgDotNetValueResult? Box_MonoDebug(DbgEvaluationInfo evalInfo) {
			engine.VerifyMonoDebugThread();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			if (!Type.IsValueType)
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			var value = this.value;
			// Even if it's boxed, box the unboxed value. This code path should only be called if
			// the compiler thinks it's an unboxed value, so we must make a new boxed value.
			if (value is ObjectMirror)
				value = ValueUtils.Unbox((ObjectMirror)value, Type);
			return engine.Box_MonoDebug(evalInfo, value, Type);
		}

		public override DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) {
			if (engine.CheckMonoDebugThread())
				return GetRawAddressValue_MonoDebug(onlyDataAddress);
			return engine.InvokeMonoDebugThread(() => GetRawAddressValue_MonoDebug(onlyDataAddress));
		}

		DbgRawAddressValue? GetRawAddressValue_MonoDebug(bool onlyDataAddress) {
			engine.VerifyMonoDebugThread();

			if (IsNull || IsNullByRef || Type.IsByRef)
				return null;

			ulong addr;
			switch (value) {
			case ArrayMirror am:
				addr = (ulong)am.Address;
				if (addr == 0)
					return null;
				var dataAddr = GetArrayAddress(am, Type.GetElementType()!, engine);
				if (onlyDataAddress || dataAddr is null)
					return dataAddr ?? new DbgRawAddressValue(addr, 0);
				var offsetToArrayData = engine.OffsetToArrayData;
				if (offsetToArrayData is null)
					return new DbgRawAddressValue(addr, 0);
				return new DbgRawAddressValue(addr, dataAddr.Value.Length + (uint)offsetToArrayData.Value);

			case StringMirror sm:
				addr = (ulong)sm.Address;
				if (addr == 0)
					return null;
				var s = rawValue.RawValue as string;
				if (s is null)
					return null;
				var offsetToStringData = engine.OffsetToStringData;
				if (offsetToStringData is null)
					return new DbgRawAddressValue(addr, 0);
				if (onlyDataAddress)
					return new DbgRawAddressValue(addr + (uint)offsetToStringData.Value, (uint)s.Length * 2);
				return new DbgRawAddressValue(addr, (uint)offsetToStringData.Value + (uint)s.Length * 2);

			case ObjectMirror om:
				addr = (ulong)om.Address;
				if (addr == 0)
					return null;
				return new DbgRawAddressValue(addr, 0);

			default:
				return null;
			}
		}

		internal static DbgRawAddressValue? GetArrayAddress(ArrayMirror v, DmdType elementType, DbgEngineImpl engine) {
			var offsetToArrayData = engine.OffsetToArrayData;
			if (offsetToArrayData is null)
				return null;
			var addr = (ulong)v.Address;
			if (addr == 0)
				return null;
			var arrayCount = v.Length;
			var startAddr = addr + (uint)offsetToArrayData.Value;
			if (!TryGetSize(elementType, out var elemSize))
				return new DbgRawAddressValue(startAddr, 0);
			ulong totalSize = (uint)elemSize * (ulong)(uint)arrayCount;
			return new DbgRawAddressValue(startAddr, totalSize);
		}

		static bool TryGetSize(DmdType type, out int size) {
			if (!type.IsValueType || type.IsPointer || type.IsFunctionPointer || type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr) {
				size = type.AppDomain.Runtime.PointerSize;
				return true;
			}

			switch (DmdType.GetTypeCode(type)) {
			case TypeCode.Boolean:		size = 1; return true;
			case TypeCode.Char:			size = 2; return true;
			case TypeCode.SByte:		size = 1; return true;
			case TypeCode.Byte:			size = 1; return true;
			case TypeCode.Int16:		size = 2; return true;
			case TypeCode.UInt16:		size = 2; return true;
			case TypeCode.Int32:		size = 4; return true;
			case TypeCode.UInt32:		size = 4; return true;
			case TypeCode.Int64:		size = 8; return true;
			case TypeCode.UInt64:		size = 8; return true;
			case TypeCode.Single:		size = 4; return true;
			case TypeCode.Double:		size = 8; return true;
			default:					size = 0; return false;
			}
		}

		public override DbgDotNetRawValue GetRawValue() => rawValue;

		public override void Dispose() { }
	}
}
