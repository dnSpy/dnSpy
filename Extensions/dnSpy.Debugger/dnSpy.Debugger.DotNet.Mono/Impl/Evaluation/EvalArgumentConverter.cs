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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	readonly struct EvalArgumentResult {
		public string ErrorMessage { get; }
		public Value Value { get; }
		public EvalArgumentResult(string errorMessage) {
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
			Value = null;
		}
		public EvalArgumentResult(Value value) {
			ErrorMessage = null;
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}
		public static EvalArgumentResult Create(InvokeResult result) {
			if (result.Result == null)
				return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			return new EvalArgumentResult(result.Result);
		}
	}

	readonly struct EvalArgumentConverter {
		readonly DbgEngineImpl engine;
		readonly FuncEval funcEval;
		readonly AppDomainMirror appDomain;
		readonly DmdAppDomain reflectionAppDomain;

		public EvalArgumentConverter(DbgEngineImpl engine, FuncEval funcEval, AppDomainMirror appDomain, DmdAppDomain reflectionAppDomain) {
			this.engine = engine;
			this.funcEval = funcEval;
			this.appDomain = appDomain;
			this.reflectionAppDomain = reflectionAppDomain;
		}

		TypeMirror GetType(DmdType type) => MonoDebugTypeCreator.GetType(engine, type, null);

		public EvalArgumentResult Convert(object value, DmdType defaultType, out DmdType type) {
			var vm = engine.MonoVirtualMachine;
			if (value == null)
				return new EvalArgumentResult(CreateNullValue(defaultType, out type));
			if (value is DbgValue dbgValue) {
				value = dbgValue.InternalValue;
				if (value == null)
					return new EvalArgumentResult(CreateNullValue(defaultType, out type));
			}
			if (value is DbgDotNetValueImpl dnValueImpl) {
				type = dnValueImpl.Type;
				return new EvalArgumentResult(dnValueImpl.Value);
			}
			var origType = defaultType;
			if (value is DbgDotNetValue dnValue) {
				var rawValue = dnValue.GetRawValue();
				if (rawValue.HasRawValue) {
					value = rawValue.RawValue;
					if (value == null)
						return new EvalArgumentResult(CreateNullValue(defaultType, out type));
				}
				origType = dnValue.Type;
			}
			if (value is string s) {
				type = reflectionAppDomain.System_String;
				return new EvalArgumentResult(appDomain.CreateString(s));
			}
			var res = ConvertCore(value, origType, out type);
			if (res.ErrorMessage != null)
				return res;
			if (origType.IsEnum) {
				type = origType;
				return new EvalArgumentResult(vm.CreateEnumMirror(GetType(origType), (PrimitiveValue)res.Value));
			}
			return res;
		}

		unsafe EvalArgumentResult ConvertCore(object value, DmdType defaultType, out DmdType type) {
			var vm = engine.MonoVirtualMachine;
			switch (Type.GetTypeCode(value.GetType())) {
			case TypeCode.Boolean:
				type = reflectionAppDomain.System_Boolean;
				return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.Boolean, value));

			case TypeCode.Char:
				type = reflectionAppDomain.System_Char;
				return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.Char, value));

			case TypeCode.SByte:
				type = reflectionAppDomain.System_SByte;
				return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.I1, value));

			case TypeCode.Byte:
				type = reflectionAppDomain.System_Byte;
				return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.U1, value));

			case TypeCode.Int16:
				type = reflectionAppDomain.System_Int16;
				return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.I2, value));

			case TypeCode.UInt16:
				type = reflectionAppDomain.System_UInt16;
				return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.U2, value));

			case TypeCode.Int32:
				type = reflectionAppDomain.System_Int32;
				return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.I4, value));

			case TypeCode.UInt32:
				if (defaultType.IsPointer || defaultType.IsFunctionPointer || defaultType == defaultType.AppDomain.System_IntPtr || defaultType == defaultType.AppDomain.System_UIntPtr)
					return new EvalArgumentResult(CreatePointerLikeValue(defaultType, (uint)value, out type));
				else {
					type = reflectionAppDomain.System_UInt32;
					return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.U4, value));
				}

			case TypeCode.Int64:
				type = reflectionAppDomain.System_Int64;
				return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.I8, value));

			case TypeCode.UInt64:
				if (defaultType.IsPointer || defaultType.IsFunctionPointer || defaultType == defaultType.AppDomain.System_IntPtr || defaultType == defaultType.AppDomain.System_UIntPtr)
					return new EvalArgumentResult(CreatePointerLikeValue(defaultType, (long)(ulong)value, out type));
				else {
					type = reflectionAppDomain.System_UInt64;
					return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.U8, value));
				}

			case TypeCode.Single:
				type = reflectionAppDomain.System_Single;
				return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.R4, value));

			case TypeCode.Double:
				type = reflectionAppDomain.System_Double;
				return new EvalArgumentResult(new PrimitiveValue(vm, ElementType.R8, value));

			case TypeCode.Decimal:
				type = reflectionAppDomain.System_Decimal;
				return CreateDecimal((decimal)value);

			default:
				if (value.GetType() == typeof(IntPtr))
					return new EvalArgumentResult(CreatePointerLikeValue(defaultType, ((IntPtr)value).ToInt64(), out type));
				if (value.GetType() == typeof(UIntPtr))
					return new EvalArgumentResult(CreatePointerLikeValue(defaultType, (long)((UIntPtr)value).ToUInt64(), out type));
				if (value is Array array && array.Rank == 1 && value.GetType().GetElementType().MakeArrayType() == value.GetType()) {
					switch (Type.GetTypeCode(value.GetType().GetElementType())) {
					case TypeCode.Boolean:
						var ba = (bool[])value;
						fixed (void* p = ba)
							return ConvertSZArray(p, ba.Length, 1, reflectionAppDomain.System_Boolean, out type);

					case TypeCode.Char:
						var bc = (char[])value;
						fixed (void* p = bc)
							return ConvertSZArray(p, bc.Length, 2, reflectionAppDomain.System_Char, out type);

					case TypeCode.SByte:
						var bsb = (sbyte[])value;
						fixed (void* p = bsb)
							return ConvertSZArray(p, bsb.Length, 1, reflectionAppDomain.System_SByte, out type);

					case TypeCode.Byte:
						var bb = (byte[])value;
						fixed (void* p = bb)
							return ConvertSZArray(p, bb.Length, 1, reflectionAppDomain.System_Byte, out type);

					case TypeCode.Int16:
						var bi16 = (short[])value;
						fixed (void* p = bi16)
							return ConvertSZArray(p, bi16.Length, 2, reflectionAppDomain.System_Int16, out type);

					case TypeCode.UInt16:
						var bu16 = (ushort[])value;
						fixed (void* p = bu16)
							return ConvertSZArray(p, bu16.Length, 2, reflectionAppDomain.System_UInt16, out type);

					case TypeCode.Int32:
						var bi32 = (int[])value;
						fixed (void* p = bi32)
							return ConvertSZArray(p, bi32.Length, 4, reflectionAppDomain.System_Int32, out type);

					case TypeCode.UInt32:
						var bu32 = (uint[])value;
						fixed (void* p = bu32)
							return ConvertSZArray(p, bu32.Length, 4, reflectionAppDomain.System_UInt32, out type);

					case TypeCode.Int64:
						var bi64 = (long[])value;
						fixed (void* p = bi64)
							return ConvertSZArray(p, bi64.Length, 8, reflectionAppDomain.System_Int64, out type);

					case TypeCode.UInt64:
						var bu64 = (ulong[])value;
						fixed (void* p = bu64)
							return ConvertSZArray(p, bu64.Length, 8, reflectionAppDomain.System_UInt64, out type);

					case TypeCode.Single:
						var br4 = (float[])value;
						fixed (void* p = br4)
							return ConvertSZArray(p, br4.Length, 4, reflectionAppDomain.System_Single, out type);

					case TypeCode.Double:
						var br8 = (double[])value;
						fixed (void* p = br8)
							return ConvertSZArray(p, br8.Length, 8, reflectionAppDomain.System_Double, out type);

					case TypeCode.String:
						return ConvertSZArray((string[])value, out type);

					default:
						break;
					}
				}
				break;
			}

			type = defaultType;
			return new EvalArgumentResult($"Func-eval: Can't convert type {value.GetType()} to a debugger value");
		}

		Value CreateNullValue(DmdType defaultType, out DmdType type) {
			if (defaultType.IsPointer || defaultType.IsFunctionPointer || defaultType == defaultType.AppDomain.System_IntPtr || defaultType == defaultType.AppDomain.System_UIntPtr)
				return CreatePointerLikeValue(defaultType, 0, out type);
			else {
				var vm = engine.MonoVirtualMachine;
				type = defaultType;
				return new PrimitiveValue(vm, ElementType.Object, null);
			}
		}

		Value CreatePointerLikeValue(DmdType defaultType, long value, out DmdType type) {
			var vm = engine.MonoVirtualMachine;
			if (defaultType.IsPointer || defaultType.IsFunctionPointer) {
				type = defaultType;
				return new PrimitiveValue(vm, ElementType.Ptr, value);
			}
			else {
				if (defaultType == defaultType.AppDomain.System_IntPtr || defaultType == defaultType.AppDomain.System_UIntPtr)
					type = defaultType;
				else
					type = defaultType.AppDomain.System_IntPtr;
				var monoType = GetType(type);
				var monoValues = new Value[] { new PrimitiveValue(vm, ElementType.Ptr, value) };
				return vm.CreateStructMirror(monoType, monoValues);
			}
		}

		EvalArgumentResult CreateDecimal(decimal value) {
			var type = reflectionAppDomain.System_Decimal;
			var monoType = GetType(type);
			var fields = GetFields(monoType, 4);
			if (fields == null)
				return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			if (fields[0].Name != "flags" || fields[1].Name != "hi" || fields[2].Name != "lo" || fields[3].Name != "mid")
				return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);

			var bits = Decimal.GetBits(value);
			var lo = bits[0];
			var mid = bits[1];
			var hi = bits[2];
			var flags = bits[3];

			Value[] values;
			var vm = engine.MonoVirtualMachine;
			if (fields[0].FieldType.FullName == "System.Int32") {
				values = new Value[4] {
					new PrimitiveValue(vm, ElementType.I4, flags),
					new PrimitiveValue(vm, ElementType.I4, hi),
					new PrimitiveValue(vm, ElementType.I4, lo),
					new PrimitiveValue(vm, ElementType.I4, mid),
				};
			}
			else if (fields[0].FieldType.FullName == "System.UInt32") {
				values = new Value[4] {
					new PrimitiveValue(vm, ElementType.U4, (uint)flags),
					new PrimitiveValue(vm, ElementType.U4, (uint)hi),
					new PrimitiveValue(vm, ElementType.U4, (uint)lo),
					new PrimitiveValue(vm, ElementType.U4, (uint)mid),
				};
			}
			else
				return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);

			return new EvalArgumentResult(vm.CreateStructMirror(monoType, values));
		}

		static FieldInfoMirror[] GetFields(TypeMirror monoType, int length) {
			var fields = new FieldInfoMirror[length];
			int w = 0;
			foreach (var f in monoType.GetFields()) {
				if (f.IsStatic || f.IsLiteral)
					continue;
				if (w >= fields.Length)
					return null;
				fields[w++] = f;
			}
			if (w != length)
				return null;
			return fields;
		}

		static bool IsInitialized<T>(T[] array) where T : class {
			for (int i = 0; i < array.Length; i++) {
				if (array[i] != null)
					return true;
			}
			return false;
		}

		static unsafe bool IsInitialized(void* array, int length) {
			if (IntPtr.Size == 4) {
				var p = (uint*)array;
				while (length >= 4) {
					if (*p != 0)
						return true;
					length -= 4;
					p++;
				}
				var pb = (byte*)p;
				while (length > 0) {
					if (*pb != 0)
						return true;
					pb++;
					length--;
				}
			}
			else {
				var p = (ulong*)array;
				while (length >= 8) {
					if (*p != 0)
						return true;
					length -= 8;
					p++;
				}
				var pb = (byte*)p;
				while (length > 0) {
					if (*pb != 0)
						return true;
					pb++;
					length--;
				}
			}
			return false;
		}

		EvalArgumentResult CreateSZArray(DmdType elementType, int length) {
			var monoElementType = GetType(elementType);
			var methodCreateInstance = reflectionAppDomain.System_Array.GetMethod(nameof(Array.CreateInstance),
				DmdSignatureCallingConvention.Default, 0, reflectionAppDomain.System_Array,
				new DmdType[2] { reflectionAppDomain.System_Type, reflectionAppDomain.System_Int32 },
				throwOnError: true);
			var args = new Value[2] {
				monoElementType.GetTypeObject(),
				new PrimitiveValue(engine.MonoVirtualMachine, ElementType.I4, length),
			};
			var result = funcEval.CallMethod(MethodCache.GetMethod(methodCreateInstance, null), null, args, FuncEvalOptions.None);
			return EvalArgumentResult.Create(result);
		}

		EvalArgumentResult ConvertSZArray(string[] array, out DmdType type) {
			var elementType = reflectionAppDomain.System_String;
			type = elementType.MakeArrayType();
			var res = CreateSZArray(elementType, array.Length);
			if (res.ErrorMessage != null)
				return res;
			if (!IsInitialized(array))
				return res;
			Debug.Assert(array.Length > 0);

			var arrayValue = (ArrayMirror)res.Value;
			for (int i = 0; i < array.Length; i++) {
				var s = array[i];
				if (s == null)
					continue;

				var stringValueRes = Convert(s, elementType, out var type2);
				if (stringValueRes.ErrorMessage != null)
					return stringValueRes;

				arrayValue[i] = stringValueRes.Value;
			}

			return new EvalArgumentResult(arrayValue);
		}

		unsafe EvalArgumentResult ConvertSZArray(void* array, int length, int elementSize, DmdType elementType, out DmdType type) {
			type = elementType.MakeArrayType();
			var res = CreateSZArray(elementType, length);
			if (res.ErrorMessage != null)
				return res;
			if (!IsInitialized(array, length * elementSize))
				return res;

			Debug.Assert(length > 0);
			var arrayValue = (ArrayMirror)res.Value;
			var addr = DbgDotNetValueImpl.GetArrayAddress(arrayValue, elementType, engine);
			if (addr == null)
				return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);

			engine.DbgRuntime.Process.WriteMemory(addr.Value.Address, array, length * elementSize);
			return new EvalArgumentResult(arrayValue);
		}
	}
}
