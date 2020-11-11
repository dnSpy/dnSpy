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
using System.Collections.Generic;
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	readonly struct EvalArgumentResult {
		public string? ErrorMessage { get; }
		public CorValue? CorValue { get; }
		public EvalArgumentResult(string errorMessage) {
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
			CorValue = null;
		}
		public EvalArgumentResult(CorValue? corValue) {
			ErrorMessage = null;
			CorValue = corValue ?? throw new ArgumentNullException(nameof(corValue));
		}
		internal static EvalArgumentResult Create(EvalResult? res, int hr) {
			if (res is null || res.Value.WasException)
				return new EvalArgumentResult(CordbgErrorHelper.GetErrorMessage(hr));
			if (res.Value.WasCustomNotification)
				return new EvalArgumentResult(CordbgErrorHelper.FuncEvalRequiresAllThreadsToRun);
			if (res.Value.WasCancelled)
				return new EvalArgumentResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			return new EvalArgumentResult(res.Value.ResultOrException!);
		}
	}

	readonly struct EvalArgumentConverter {
		readonly DbgEngineImpl engine;
		readonly DnEval dnEval;
		readonly CorAppDomain appDomain;
		readonly DmdAppDomain reflectionAppDomain;
		readonly List<CorValue> createdValues;

		public EvalArgumentConverter(DbgEngineImpl engine, DnEval dnEval, CorAppDomain appDomain, DmdAppDomain reflectionAppDomain, List<CorValue> createdValues) {
			this.engine = engine;
			this.dnEval = dnEval;
			this.appDomain = appDomain;
			this.reflectionAppDomain = reflectionAppDomain;
			this.createdValues = createdValues;
		}

		public unsafe EvalArgumentResult Convert(object? value, DmdType defaultType, out DmdType type) {
			if (value is null) {
				type = defaultType;
				return new EvalArgumentResult(dnEval.CreateNull());
			}
			if (value is DbgValue dbgValue) {
				value = dbgValue.InternalValue;
				if (value is null) {
					type = defaultType;
					return new EvalArgumentResult(dnEval.CreateNull());
				}
			}
			if (value is DbgDotNetValueImpl dnValueImpl) {
				type = dnValueImpl.Type;
				var corValue = dnValueImpl.TryGetCorValue();
				if (corValue is not null)
					return new EvalArgumentResult(corValue);
				return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
			DmdType? origType = null;
			if (value is DbgDotNetValue dnValue) {
				var rawValue = dnValue.GetRawValue();
				if (rawValue.HasRawValue) {
					value = rawValue.RawValue;
					if (value is null) {
						type = defaultType;
						return new EvalArgumentResult(dnEval.CreateNull());
					}
				}
				origType = dnValue.Type;
			}
			if (value is string s) {
				type = reflectionAppDomain.System_String;
				var res = dnEval.CreateString(s, out var hr);
				if (res?.ResultOrException is CorValue corValue)
					return new EvalArgumentResult(AddValue(reflectionAppDomain.System_String, corValue));
				return EvalArgumentResult.Create(res, hr);
			}

			switch (Type.GetTypeCode(value.GetType())) {
			case TypeCode.Boolean:		return CreateByte(type = origType ?? reflectionAppDomain.System_Boolean, (byte)((bool)value ? 1 : 0));
			case TypeCode.Char:			return CreateUInt16(type = origType ?? reflectionAppDomain.System_Char, (char)value);
			case TypeCode.SByte:		return CreateByte(type = origType ?? reflectionAppDomain.System_SByte, (byte)(sbyte)value);
			case TypeCode.Byte:			return CreateByte(type = origType ?? reflectionAppDomain.System_Byte, (byte)value);
			case TypeCode.Int16:		return CreateUInt16(type = origType ?? reflectionAppDomain.System_Int16, (ushort)(short)value);
			case TypeCode.UInt16:		return CreateUInt16(type = origType ?? reflectionAppDomain.System_UInt16, (ushort)value);
			case TypeCode.Int32:		return CreateUInt32(type = origType ?? reflectionAppDomain.System_Int32, (uint)(int)value);
			case TypeCode.UInt32:		return CreateUInt32(type = origType ?? reflectionAppDomain.System_UInt32, (uint)value);
			case TypeCode.Int64:		return CreateUInt64(type = origType ?? reflectionAppDomain.System_Int64, (ulong)(long)value);
			case TypeCode.UInt64:		return CreateUInt64(type = origType ?? reflectionAppDomain.System_UInt64, (ulong)value);
			case TypeCode.Single:
				type = origType ?? reflectionAppDomain.System_Single;
				return CreateSingle((float)value);
			case TypeCode.Double:
				type = origType ?? reflectionAppDomain.System_Double;
				return CreateDouble((double)value);
			case TypeCode.Decimal:
				type = reflectionAppDomain.System_Decimal;
				return CreateDecimal((decimal)value);
			default:
				if (value.GetType() == typeof(IntPtr)) {
					type = origType ?? reflectionAppDomain.System_IntPtr;
					if (IntPtr.Size == 4)
						return CreateUInt32(reflectionAppDomain.System_IntPtr, (uint)((IntPtr)value).ToInt32());
					return CreateUInt64(reflectionAppDomain.System_IntPtr, (ulong)((IntPtr)value).ToInt64());
				}
				if (value.GetType() == typeof(UIntPtr)) {
					type = origType ?? reflectionAppDomain.System_UIntPtr;
					if (IntPtr.Size == 4)
						return CreateUInt32(reflectionAppDomain.System_UIntPtr, ((UIntPtr)value).ToUInt32());
					return CreateUInt64(reflectionAppDomain.System_UIntPtr, ((UIntPtr)value).ToUInt64());
				}
				if (value is Array array && array.Rank == 1 && value.GetType().GetElementType()!.MakeArrayType() == value.GetType()) {
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

		EvalArgumentResult ConvertSZArray(string[] array, out DmdType type) {
			var elementType = reflectionAppDomain.System_String;
			type = elementType.MakeArrayType();
			var corElementType = GetType(elementType);
			var res = dnEval.CreateSZArray(corElementType, array.Length, out int hr);
			if (res is null || !res.Value.NormalResult)
				return EvalArgumentResult.Create(res, hr);
			if (!IsInitialized(array))
				return EvalArgumentResult.Create(res, hr);
			Debug.Assert(array.Length > 0);

			CorValue? elem = null;
			bool error = true;
			try {
				var arrayValue = res.Value.ResultOrException!;
				for (int i = 0; i < array.Length; i++) {
					var s = array[i];
					if (s is null)
						continue;

					var stringValueRes = Convert(s, elementType, out var type2);
					if (stringValueRes.ErrorMessage is not null)
						return stringValueRes;
					if (!stringValueRes.CorValue!.IsReference)
						return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);

					CorValue? av = arrayValue;
					if (av.IsReference) {
						av = av.GetDereferencedValue(out hr);
						if (av is null)
							return new EvalArgumentResult(CordbgErrorHelper.GetErrorMessage(hr));
					}
					if (av?.IsArray != true)
						return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);

					Debug2.Assert(elem is null);
					elem = av.GetElementAtPosition(i, out hr);
					if (elem is null)
						return new EvalArgumentResult(CordbgErrorHelper.GetErrorMessage(hr));

					hr = elem.SetReferenceAddress(stringValueRes.CorValue.ReferenceAddress);
					if (hr != 0)
						return new EvalArgumentResult(CordbgErrorHelper.GetErrorMessage(hr));

					engine.DisposeHandle_CorDebug(elem);
					elem = null;
				}

				var eaRes = new EvalArgumentResult(AddValue(type, res.Value.ResultOrException!));
				error = false;
				return eaRes;
			}
			finally {
				if (error)
					engine.DisposeHandle_CorDebug(res.Value.ResultOrException);
				engine.DisposeHandle_CorDebug(elem);
			}
		}

		unsafe EvalArgumentResult ConvertSZArray(void* array, int length, int elementSize, DmdType elementType, out DmdType type) {
			type = elementType.MakeArrayType();
			var corElementType = GetType(elementType);
			var res = dnEval.CreateSZArray(corElementType, length, out int hr);
			if (res is null || !res.Value.NormalResult)
				return EvalArgumentResult.Create(res, hr);
			if (!IsInitialized(array, length * elementSize))
				return EvalArgumentResult.Create(res, hr);

			bool error = true;
			try {
				Debug.Assert(length > 0);
				CorValue? arrayValue = res.Value.ResultOrException!;
				if (arrayValue.IsReference) {
					arrayValue = arrayValue.GetDereferencedValue(out hr);
					if (arrayValue is not null)
						return new EvalArgumentResult(CordbgErrorHelper.GetErrorMessage(hr));
				}
				Debug.Assert(arrayValue?.IsArray == true);
				if (arrayValue?.IsArray != true)
					return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
				var addr = DbgDotNetValueImpl.GetArrayAddress(arrayValue);
				if (addr is null)
					return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);

				if (!(appDomain.Process is CorProcess process))
					return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
				hr = process.WriteMemory(addr.Value.Address, array, length * elementSize, out int sizeWritten);
				if (hr < 0 || sizeWritten != length * elementSize)
					return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);

				var eaRes = new EvalArgumentResult(AddValue(type, res.Value.ResultOrException));
				error = false;
				return eaRes;
			}
			finally {
				if (error)
					engine.DisposeHandle_CorDebug(res.Value.ResultOrException);
			}
		}

		static bool IsInitialized<T>(T[] array) where T : class {
			for (int i = 0; i < array.Length; i++) {
				if (array[i] is not null)
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

		CorType GetType(DmdType type) => CorDebugTypeCreator.GetType(engine, appDomain, type);

		CorValue? AddValue(DmdType type, CorValue? value) {
			if (value is not null && !value.IsNull && !value.IsHandle && value.IsReference && !type.IsPointer && !type.IsFunctionPointer && !type.IsByRef)
				value = value.GetDereferencedValue(out int hr)?.CreateHandle(CorDebugHandleType.HANDLE_STRONG) ?? value;
			if (value is not null) {
				try {
					createdValues.Add(value);
				}
				catch {
					engine.DisposeHandle_CorDebug(value);
					throw;
				}
			}
			return value;
		}

		EvalArgumentResult CreateNoConstructor(DmdType type) {
			var res = dnEval.CreateDontCallConstructor(GetType(type), out int hr);
			var argRes = EvalArgumentResult.Create(res, hr);
			var value = AddValue(type, argRes.CorValue);
			if (value is not null)
				return new EvalArgumentResult(value);
			return argRes;
		}

		EvalArgumentResult CreateByte(DmdType type, byte value) {
			var res = CreateNoConstructor(type);
			if (res.ErrorMessage is not null)
				return res;
			Debug2.Assert(res.CorValue!.DereferencedValue is not null && res.CorValue.DereferencedValue.BoxedValue is not null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(new byte[1] { value });
			return res;
		}

		EvalArgumentResult CreateUInt16(DmdType type, ushort value) {
			var res = CreateNoConstructor(type);
			if (res.ErrorMessage is not null)
				return res;
			Debug2.Assert(res.CorValue!.DereferencedValue is not null && res.CorValue.DereferencedValue.BoxedValue is not null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(BitConverter.GetBytes(value));
			return res;
		}

		EvalArgumentResult CreateUInt32(DmdType type, uint value) {
			var res = CreateNoConstructor(type);
			if (res.ErrorMessage is not null)
				return res;
			Debug2.Assert(res.CorValue!.DereferencedValue is not null && res.CorValue.DereferencedValue.BoxedValue is not null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(BitConverter.GetBytes(value));
			return res;
		}

		EvalArgumentResult CreateUInt64(DmdType type, ulong value) {
			var res = CreateNoConstructor(type);
			if (res.ErrorMessage is not null)
				return res;
			Debug2.Assert(res.CorValue!.DereferencedValue is not null && res.CorValue.DereferencedValue.BoxedValue is not null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(BitConverter.GetBytes(value));
			return res;
		}

		EvalArgumentResult CreateSingle(float value) {
			var res = CreateNoConstructor(reflectionAppDomain.System_Single);
			if (res.ErrorMessage is not null)
				return res;
			Debug2.Assert(res.CorValue!.DereferencedValue is not null && res.CorValue.DereferencedValue.BoxedValue is not null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(BitConverter.GetBytes(value));
			return res;
		}

		EvalArgumentResult CreateDouble(double value) {
			var res = CreateNoConstructor(reflectionAppDomain.System_Double);
			if (res.ErrorMessage is not null)
				return res;
			Debug2.Assert(res.CorValue!.DereferencedValue is not null && res.CorValue.DereferencedValue.BoxedValue is not null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(BitConverter.GetBytes(value));
			return res;
		}

		EvalArgumentResult CreateDecimal(decimal value) {
			var res = CreateNoConstructor(reflectionAppDomain.System_Decimal);
			if (res.ErrorMessage is not null)
				return res;
			Debug2.Assert(res.CorValue!.DereferencedValue is not null && res.CorValue.DereferencedValue.BoxedValue is not null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(GetBytes(value));
			return res;
		}

		static byte[] GetBytes(decimal d) {
			var decimalBits = decimal.GetBits(d);
			var bytes = new byte[16];
			WriteInt32(bytes, 0, decimalBits[3]);
			WriteInt32(bytes, 4, decimalBits[2]);
			WriteInt32(bytes, 8, decimalBits[0]);
			WriteInt32(bytes, 12, decimalBits[1]);
			return bytes;
		}

		static void WriteInt32(byte[] dest, int index, int v) {
			dest[index + 0] = (byte)v;
			dest[index + 1] = (byte)(v >> 8);
			dest[index + 2] = (byte)(v >> 16);
			dest[index + 3] = (byte)(v >> 24);
		}
	}
}
