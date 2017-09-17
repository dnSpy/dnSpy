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
using System.Collections.Generic;
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	struct EvalArgumentResult {
		public string ErrorMessage { get; }
		public CorValue CorValue { get; }
		public EvalArgumentResult(string errorMessage) {
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
			CorValue = null;
		}
		public EvalArgumentResult(CorValue corValue) {
			ErrorMessage = null;
			CorValue = corValue ?? throw new ArgumentNullException(nameof(corValue));
		}
		internal static EvalArgumentResult Create(EvalResult? res, int hr) {
			if (res == null || res.Value.WasException)
				return new EvalArgumentResult(CordbgErrorHelper.GetErrorMessage(hr));
			Debug.Assert(res.Value.ResultOrException.IsString);
			return new EvalArgumentResult(res.Value.ResultOrException);
		}
	}

	struct EvalArgumentConverter {
		readonly DbgEngineImpl engine;
		readonly DnEval dnEval;
		readonly CorAppDomain appDomain;
		readonly DmdAppDomain reflectionAppDomain;
		readonly List<CorValue> createdStrongHandles;

		public IList<CorValue> CreatedStrongHandles => (IList<CorValue>)createdStrongHandles ?? Array.Empty<CorValue>();

		public EvalArgumentConverter(DbgEngineImpl engine, DnEval dnEval, CorAppDomain appDomain, DmdAppDomain reflectionAppDomain, List<CorValue> createdStrongHandles) {
			this.engine = engine;
			this.dnEval = dnEval;
			this.appDomain = appDomain;
			this.reflectionAppDomain = reflectionAppDomain;
			this.createdStrongHandles = createdStrongHandles;
		}

		public EvalArgumentResult Convert(object value) {
			if (value == null)
				return new EvalArgumentResult(dnEval.CreateNull());
			if (value is DbgValue dbgValue)
				value = dbgValue.InternalValue;
			if (value is DbgDotNetValueImpl dnValueImpl)
				return new EvalArgumentResult(dnValueImpl.Value);
			if (value is string s) {
				var res = dnEval.CreateString(s, out var hr);
				return EvalArgumentResult.Create(res, hr);
			}

			switch (Type.GetTypeCode(value.GetType())) {
			case TypeCode.Boolean:		return CreateByte(reflectionAppDomain.System_Boolean, (byte)((bool)value ? 1 : 0));
			case TypeCode.Char:			return CreateUInt16(reflectionAppDomain.System_Char, (char)value);
			case TypeCode.SByte:		return CreateByte(reflectionAppDomain.System_SByte, (byte)(sbyte)value);
			case TypeCode.Byte:			return CreateByte(reflectionAppDomain.System_Byte, (byte)value);
			case TypeCode.Int16:		return CreateUInt16(reflectionAppDomain.System_Int16, (ushort)(short)value);
			case TypeCode.UInt16:		return CreateUInt16(reflectionAppDomain.System_UInt16, (ushort)value);
			case TypeCode.Int32:		return CreateUInt32(reflectionAppDomain.System_Int32, (uint)(int)value);
			case TypeCode.UInt32:		return CreateUInt32(reflectionAppDomain.System_UInt32, (uint)value);
			case TypeCode.Int64:		return CreateUInt64(reflectionAppDomain.System_Int64, (ulong)(long)value);
			case TypeCode.UInt64:		return CreateUInt64(reflectionAppDomain.System_UInt64, (ulong)value);
			case TypeCode.Single:		return CreateSingle((float)value);
			case TypeCode.Double:		return CreateDouble((double)value);
			case TypeCode.Decimal:		return CreateDecimal((decimal)value);
			default:
				if (value.GetType() == typeof(IntPtr)) {
					if (IntPtr.Size == 4)
						return CreateUInt32(reflectionAppDomain.System_IntPtr, (uint)((IntPtr)value).ToInt32());
					return CreateUInt64(reflectionAppDomain.System_IntPtr, (ulong)((IntPtr)value).ToInt64());
				}
				if (value.GetType() == typeof(UIntPtr)) {
					if (IntPtr.Size == 4)
						return CreateUInt32(reflectionAppDomain.System_UIntPtr, ((UIntPtr)value).ToUInt32());
					return CreateUInt64(reflectionAppDomain.System_UIntPtr, ((UIntPtr)value).ToUInt64());
				}
				//TODO: Check for a few more things, eg. arrays of common types
				break;
			}

			return new EvalArgumentResult($"Func-eval: Can't convert type {value.GetType()} to a debugger value");
		}

		CorType GetType(DmdType type) => CorDebugTypeCreator.GetType(engine, appDomain, type);

		EvalArgumentResult CreateNoConstructor(DmdType type) {
			var res = dnEval.CreateDontCallConstructor(GetType(type), out int hr);
			var argRes = EvalArgumentResult.Create(res, hr);
			var value = argRes.CorValue;
			if (value != null && !value.IsNull && !value.IsHandle && value.IsReference && !type.IsPointer && !type.IsFunctionPointer && !type.IsByRef) {
				var strongHandle = value.DereferencedValue?.CreateHandle(CorDebugHandleType.HANDLE_STRONG);
				Debug.Assert(strongHandle != null || type == type.AppDomain.System_TypedReference);
				if (strongHandle != null) {
					try {
						createdStrongHandles.Add(strongHandle);
					}
					catch {
						strongHandle.DisposeHandle();
						throw;
					}
					return new EvalArgumentResult(strongHandle);
				}
			}
			return argRes;
		}

		EvalArgumentResult CreateByte(DmdType type, byte value) {
			var res = CreateNoConstructor(type);
			if (res.ErrorMessage != null)
				return res;
			Debug.Assert(res.CorValue.DereferencedValue != null && res.CorValue.DereferencedValue.BoxedValue != null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(new byte[1] { value });
			return res;
		}

		EvalArgumentResult CreateUInt16(DmdType type, ushort value) {
			var res = CreateNoConstructor(type);
			if (res.ErrorMessage != null)
				return res;
			Debug.Assert(res.CorValue.DereferencedValue != null && res.CorValue.DereferencedValue.BoxedValue != null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(BitConverter.GetBytes(value));
			return res;
		}

		EvalArgumentResult CreateUInt32(DmdType type, uint value) {
			var res = CreateNoConstructor(type);
			if (res.ErrorMessage != null)
				return res;
			Debug.Assert(res.CorValue.DereferencedValue != null && res.CorValue.DereferencedValue.BoxedValue != null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(BitConverter.GetBytes(value));
			return res;
		}

		EvalArgumentResult CreateUInt64(DmdType type, ulong value) {
			var res = CreateNoConstructor(type);
			if (res.ErrorMessage != null)
				return res;
			Debug.Assert(res.CorValue.DereferencedValue != null && res.CorValue.DereferencedValue.BoxedValue != null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(BitConverter.GetBytes(value));
			return res;
		}

		EvalArgumentResult CreateSingle(float value) {
			var res = CreateNoConstructor(reflectionAppDomain.System_Single);
			if (res.ErrorMessage != null)
				return res;
			Debug.Assert(res.CorValue.DereferencedValue != null && res.CorValue.DereferencedValue.BoxedValue != null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(BitConverter.GetBytes(value));
			return res;
		}

		EvalArgumentResult CreateDouble(double value) {
			var res = CreateNoConstructor(reflectionAppDomain.System_Double);
			if (res.ErrorMessage != null)
				return res;
			Debug.Assert(res.CorValue.DereferencedValue != null && res.CorValue.DereferencedValue.BoxedValue != null);
			if (value != 0)
				res.CorValue.DereferencedValue.BoxedValue.WriteGenericValue(BitConverter.GetBytes(value));
			return res;
		}

		EvalArgumentResult CreateDecimal(decimal value) {
			var res = CreateNoConstructor(reflectionAppDomain.System_Decimal);
			if (res.ErrorMessage != null)
				return res;
			Debug.Assert(res.CorValue.DereferencedValue != null && res.CorValue.DereferencedValue.BoxedValue != null);
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
