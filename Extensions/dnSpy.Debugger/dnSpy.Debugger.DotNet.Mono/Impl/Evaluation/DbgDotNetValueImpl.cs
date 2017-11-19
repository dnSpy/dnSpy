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
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;
using SD = System.Diagnostics;

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
			if (value is PrimitiveValue pv && pv.Value == null) {
				if (Type.IsByRef)
					flags |= ValueFlags.IsNullByRef;
				else
					flags |= ValueFlags.IsNull;
			}
			this.flags = flags;
		}

		public override IDbgDotNetRuntime TryGetDotNetRuntime() => engine.DotNetRuntime;

		public override DbgDotNetValue LoadIndirect() {
			if (!Type.IsByRef)
				return null;
			if (IsNullByRef)
				return new SyntheticNullValue(Type.GetElementType());
			if (engine.CheckMonoDebugThread())
				return Dereference_MonoDebug();
			return engine.InvokeMonoDebugThread(() => Dereference_MonoDebug());
		}

		DbgDotNetValue Dereference_MonoDebug() {
			SD.Debug.Assert(Type.IsByRef && !IsNullByRef);
			engine.VerifyMonoDebugThread();
			return engine.CreateDotNetValue_MonoDebug(valueLocation.Dereference());
		}

		public override string StoreIndirect(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken) {
			if (!Type.IsByRef)
				return ErrorHelper.InternalError;
			if (engine.CheckMonoDebugThread())
				return StoreIndirect_MonoDebug(context, frame, value, cancellationToken);
			return engine.InvokeMonoDebugThread(() => StoreIndirect_MonoDebug(context, frame, value, cancellationToken));
		}

		string StoreIndirect_MonoDebug(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken) {
			engine.VerifyMonoDebugThread();
			cancellationToken.ThrowIfCancellationRequested();
			return PredefinedEvaluationErrorMessages.InternalDebuggerError;//TODO:
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
			SD.Debug.Assert(Type.IsArray);
			engine.VerifyMonoDebugThread();
			var arrayMirror = value as ArrayMirror;
			if (arrayMirror == null)
				return 0;
			return (uint)arrayMirror.Length;
		}

		public override bool GetArrayInfo(out uint elementCount, out DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			if (Type.IsArray) {
				if (engine.CheckMonoDebugThread())
					return GetArrayInfo_MonoDebug(out elementCount, out dimensionInfos);
				else {
					uint tmpElementCount = 0;
					DbgDotNetArrayDimensionInfo[] tmpDimensionInfos = null;
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

		bool GetArrayInfo_MonoDebug(out uint elementCount, out DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			SD.Debug.Assert(Type.IsArray);
			engine.VerifyMonoDebugThread();
			var arrayMirror = value as ArrayMirror;
			if (arrayMirror == null) {
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

		public override DbgDotNetValue GetArrayElementAt(uint index) {
			if (!Type.IsArray)
				return null;
			if (engine.CheckMonoDebugThread())
				return GetArrayElementAt_MonoDebug(index);
			return engine.InvokeMonoDebugThread(() => GetArrayElementAt_MonoDebug(index));
		}

		DbgDotNetValue GetArrayElementAt_MonoDebug(uint index) {
			SD.Debug.Assert(Type.IsArray);
			engine.VerifyMonoDebugThread();
			var arrayMirror = value as ArrayMirror;
			if (arrayMirror == null)
				return null;
			var valueLocation = new ArrayElementValueLocation(Type.GetElementType(), arrayMirror, index);
			return engine.CreateDotNetValue_MonoDebug(valueLocation);
		}

		public override string SetArrayElementAt(DbgEvaluationContext context, DbgStackFrame frame, uint index, object value, CancellationToken cancellationToken) {
			if (!Type.IsArray)
				return base.SetArrayElementAt(context, frame, index, value, cancellationToken);
			if (engine.CheckMonoDebugThread())
				return SetArrayElementAt_MonoDebug(context, frame, index, value, cancellationToken);
			return engine.InvokeMonoDebugThread(() => SetArrayElementAt_MonoDebug(context, frame, index, value, cancellationToken));
		}

		string SetArrayElementAt_MonoDebug(DbgEvaluationContext context, DbgStackFrame frame, uint index, object value, CancellationToken cancellationToken) {
			engine.VerifyMonoDebugThread();
			cancellationToken.ThrowIfCancellationRequested();
			return PredefinedEvaluationErrorMessages.InternalDebuggerError;//TODO:
		}

		public override DbgDotNetValue Box(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (engine.CheckMonoDebugThread())
				return Box_MonoDebug(context, frame, cancellationToken);
			return engine.InvokeMonoDebugThread(() => Box_MonoDebug(context, frame, cancellationToken));
		}

		DbgDotNetValue Box_MonoDebug(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			engine.VerifyMonoDebugThread();
			cancellationToken.ThrowIfCancellationRequested();
			return null;//TODO:
		}

		public override DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) {
			if (engine.CheckMonoDebugThread())
				return GetRawAddressValue_MonoDebug(onlyDataAddress);
			return engine.InvokeMonoDebugThread(() => GetRawAddressValue_MonoDebug(onlyDataAddress));
		}

		DbgRawAddressValue? GetRawAddressValue_MonoDebug(bool onlyDataAddress) {
			engine.VerifyMonoDebugThread();
			return null;//TODO:
		}

		internal static DbgRawAddressValue? GetArrayAddress(ArrayMirror v, DmdType elementType) {
			var addr = (ulong)v.Address;
			if (addr == 0)
				return null;
			var arrayCount = v.Length;
			if (arrayCount == 0)
				return new DbgRawAddressValue(addr, 0);
			int pointerSize = elementType.AppDomain.Runtime.PointerSize;
			var startAddr = addr + (uint)ObjectConstants.GetOffsetToArrayData(pointerSize);
			if (!TryGetSize(elementType, pointerSize, out var elemSize))
				return null;
			ulong totalSize = (uint)elemSize * (ulong)(uint)arrayCount;
			return new DbgRawAddressValue(startAddr, totalSize);
		}

		static bool TryGetSize(DmdType type, int pointerSize, out int size) {
			if (!type.IsValueType || type.IsPointer || type.IsFunctionPointer || type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr) {
				size = pointerSize;
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

		public override void Dispose() { }//TODO:
	}
}
