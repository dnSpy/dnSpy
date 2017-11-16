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

		readonly DbgEngineImpl engine;
		readonly Value value;
		readonly DbgDotNetRawValue rawValue;
		readonly ValueFlags flags;

		public DbgDotNetValueImpl(DbgEngineImpl engine, Value value, DmdType slotType) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.value = value ?? throw new ArgumentNullException(nameof(value));
			Type = GetType(engine, value, slotType);
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

		static DmdType GetType(DbgEngineImpl engine, Value value, DmdType slotType) {
			var reflectionAppDomain = slotType.AppDomain;
			switch (value) {
			case PrimitiveValue pv:
				switch (pv.Type) {
				case ElementType.Boolean:	return reflectionAppDomain.System_Boolean;
				case ElementType.Char:		return reflectionAppDomain.System_Char;
				case ElementType.I1:		return reflectionAppDomain.System_SByte;
				case ElementType.U1:		return reflectionAppDomain.System_Byte;
				case ElementType.I2:		return reflectionAppDomain.System_Int16;
				case ElementType.U2:		return reflectionAppDomain.System_UInt16;
				case ElementType.I4:		return reflectionAppDomain.System_Int32;
				case ElementType.U4:		return reflectionAppDomain.System_UInt32;
				case ElementType.I8:		return reflectionAppDomain.System_Int64;
				case ElementType.U8:		return reflectionAppDomain.System_UInt64;
				case ElementType.R4:		return reflectionAppDomain.System_Single;
				case ElementType.R8:		return reflectionAppDomain.System_Double;
				case ElementType.I:			return reflectionAppDomain.System_IntPtr;
				case ElementType.U:			return reflectionAppDomain.System_UIntPtr;
				case ElementType.Ptr:		return slotType.IsPointer ? slotType : reflectionAppDomain.System_Void.MakePointerType();
				case ElementType.Object:	return slotType;// This is a null value
				default:					throw new InvalidOperationException();
				}

			case EnumMirror em:
				return new ReflectionTypeCreator(engine, reflectionAppDomain).Create(em.Type);

			case StructMirror sm:
				return new ReflectionTypeCreator(engine, reflectionAppDomain).Create(sm.Type);

			case ArrayMirror am:
				return new ReflectionTypeCreator(engine, reflectionAppDomain).Create(am.Type);

			case StringMirror strVal:
				return reflectionAppDomain.System_String;

			case ObjectMirror om:
				return new ReflectionTypeCreator(engine, reflectionAppDomain).Create(om.Type);

			default:
				throw new InvalidOperationException();
			}
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
			return null;//TODO:
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
			return 0;//TODO:
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
			//TODO:
			elementCount = 0;
			dimensionInfos = null;
			return false;
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
			return null;//TODO:
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

		public override DbgDotNetRawValue GetRawValue() => rawValue;

		public override void Dispose() { }//TODO:
	}
}
