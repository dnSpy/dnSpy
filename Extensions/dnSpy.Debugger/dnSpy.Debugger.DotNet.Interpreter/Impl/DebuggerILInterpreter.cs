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
using System.Runtime.CompilerServices;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Interpreter.Impl {
	sealed class DebuggerILInterpreter {
		const int MAX_LOCALLOC_SIZE = 1 * 1024 * 1024;
		const int MAX_INTERPRETED_INSTRUCTIONS = 5000;
		DebuggerRuntime debuggerRuntime;
		int totalInstructionCount;
		readonly List<ILValue> ilValueStack;
		public DebuggerILInterpreter() => ilValueStack = new List<ILValue>();

		public ILValue Execute(DebuggerRuntime debuggerRuntime, ILVMExecuteStateImpl state) {
			try {
				Debug.Assert(ilValueStack.Count == 0);
				this.debuggerRuntime = debuggerRuntime;
				totalInstructionCount = 0;
				return ExecuteLoop(state);
			}
			catch (IndexOutOfRangeException ex) {
				// Possible reasons:
				//	- We access an invalid index in method body
				throw new InvalidMethodBodyInterpreterException(ex);
			}
			catch (ArgumentException ex) {
				// Possible reasons:
				//	- IL value stack underflow (we let the List<T> check for invalid indexes)
				//	- BitConverter throws when reading eg. a Single
				//	- ResolveString() gets called with an invalid offset
				throw new InvalidMethodBodyInterpreterException(ex);
			}
			finally {
				ilValueStack.Clear();
				this.debuggerRuntime = null;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ILValue Pop1() {
			int index = ilValueStack.Count - 1;
			// ArgumentOutOfRangeException gets thrown if it underflows
			var value = ilValueStack[index];
			ilValueStack.RemoveAt(index);
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void Pop2(out ILValue a, out ILValue b) {
			int index = ilValueStack.Count - 1;
			// ArgumentOutOfRangeException gets thrown if it underflows
			b = ilValueStack[index];
			ilValueStack.RemoveAt(index--);
			a = ilValueStack[index];
			ilValueStack.RemoveAt(index);
		}

		ILValue[] PopMethodArguments(DmdMethodSignature sig) {
			var args = sig.GetParameterTypes();
			var varArgs = sig.GetVarArgsParameterTypes();
			int total = args.Count + varArgs.Count;
			if (total == 0)
				return Array.Empty<ILValue>();
			var res = new ILValue[total];
			for (int i = total - 1; i >= 0; i--) {
				var type = i < args.Count ? args[i] : varArgs[i - args.Count];
				res[i] = Convert(Pop1(), type);
			}
			return res;
		}

		ILValue Convert(ILValue value, DmdType targetType) {
			long l;
			double d;
			switch (DmdType.GetTypeCode(targetType)) {
			case TypeCode.Boolean:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue((byte)l);

			case TypeCode.Char:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue((char)l);

			case TypeCode.SByte:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue((sbyte)l);

			case TypeCode.Byte:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue((byte)l);

			case TypeCode.Int16:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue((short)l);

			case TypeCode.UInt16:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue((ushort)l);

			case TypeCode.Int32:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue((int)l);

			case TypeCode.UInt32:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue((int)l);

			case TypeCode.Int64:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt64ILValue(l);

			case TypeCode.UInt64:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt64ILValue(l);

			case TypeCode.Single:
				if (!GetValue(value, out d))
					return value;
				return new ConstantFloatILValue((float)d);

			case TypeCode.Double:
				if (!GetValue(value, out d))
					return value;
				return new ConstantFloatILValue(d);

			default:
				if (targetType == targetType.AppDomain.System_IntPtr || targetType == targetType.AppDomain.System_UIntPtr) {
					if (GetValue(value, out l))
						return debuggerRuntime.PointerSize == 4 ? ConstantNativeIntILValue.Create32((int)l) : ConstantNativeIntILValue.Create64(l);
				}
				break;
			}

			return value;
		}

		bool GetValue(ILValue value, out long result) {
			if (value is ConstantInt32ILValue c32) {
				result = c32.Value;
				return true;
			}
			else if (value is ConstantInt64ILValue c64) {
				result = c64.Value;
				return true;
			}
			else if (value is ConstantNativeIntILValue cni) {
				result = debuggerRuntime.PointerSize == 4 ? cni.Value32 : cni.Value64;
				return true;
			}

			result = 0;
			return false;
		}

		static bool GetValue(ILValue value, out double result) {
			if (value is ConstantFloatILValue f) {
				result = f.Value;
				return true;
			}

			result = 0;
			return false;
		}

		void ThrowInvalidMethodBodyInterpreterException() => throw new InvalidMethodBodyInterpreterException();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ToUInt16(byte[] a, ref int pos) => a[pos++] | (a[pos++] << 8);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ToInt32(byte[] a, ref int pos) => a[pos++] | (a[pos++] << 8) | (a[pos++] << 16) | (a[pos++] << 24);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		long ToInt64(byte[] a, ref int pos) => (uint)(a[pos++] | (a[pos++] << 8) | (a[pos++] << 16) | (a[pos++] << 24)) | ((long)a[pos++] << 32) | ((long)a[pos++] << 40) | ((long)a[pos++] << 48) | ((long)a[pos++] << 56);

		ILValue ExecuteLoop(ILVMExecuteStateImpl state) {
			var currentMethod = state.Method;
			var body = state.Body;
			if (body == null)
				ThrowInvalidMethodBodyInterpreterException();
			var bodyBytes = state.ILBytes;
			var exceptionHandlingClauses = body.ExceptionHandlingClauses;
			int methodBodyPos = 0;
			DmdType constrainedType = null;
			for (;;) {
				if (totalInstructionCount++ >= MAX_INTERPRETED_INSTRUCTIONS)
					throw new TooManyInstructionsInterpreterException();

				int i, j;
				long l;
				ILValue v1, v2, v3;
				DmdType type;
				DmdFieldInfo field;
				DmdMethodBase method;
				DmdMethodSignature methodSig;
				ILValue[] args;
				bool isPrefix;

				isPrefix = false;
				i = bodyBytes[methodBodyPos++];
				switch ((OpCode)i) {
				case OpCode.Prefix1:
					i = bodyBytes[methodBodyPos++];
					switch ((OpCodeFE)i) {
					case OpCodeFE.Ldarg:
						v1 = debuggerRuntime.GetArgument(ToUInt16(bodyBytes, ref methodBodyPos));
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1.Clone());
						break;

					case OpCodeFE.Ldarga:
						v1 = debuggerRuntime.GetArgumentAddress(ToUInt16(bodyBytes, ref methodBodyPos));
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1.Clone());
						break;

					case OpCodeFE.Ldloc:
						v1 = debuggerRuntime.GetLocal(ToUInt16(bodyBytes, ref methodBodyPos));
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1.Clone());
						break;

					case OpCodeFE.Ldloca:
						v1 = debuggerRuntime.GetLocalAddress(ToUInt16(bodyBytes, ref methodBodyPos));
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1.Clone());
						break;

					case OpCodeFE.Starg:
						if (!debuggerRuntime.SetArgument(ToUInt16(bodyBytes, ref methodBodyPos), Pop1()))
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case OpCodeFE.Stloc:
						if (!debuggerRuntime.SetLocal(ToUInt16(bodyBytes, ref methodBodyPos), Pop1()))
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case OpCodeFE.Sizeof:
						type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
						ilValueStack.Add(ILValueConstants.GetInt32Constant(GetSizeOf(type)));
						break;

					case OpCodeFE.Ceq:
						Pop2(out v1, out v2);
						ilValueStack.Add(ILValueConstants.GetInt32Constant(CompareSigned(v1, v2, isEquals: true) == 0 ? 1 : 0));
						break;

					case OpCodeFE.Cgt:
						Pop2(out v1, out v2);
						ilValueStack.Add(ILValueConstants.GetInt32Constant(CompareSigned(v1, v2, isEquals: false) > 0 ? 1 : 0));
						break;

					case OpCodeFE.Cgt_Un:
						Pop2(out v1, out v2);
						ilValueStack.Add(ILValueConstants.GetInt32Constant(CompareUnsigned(v1, v2, isEquals: false) > 0 ? 1 : 0));
						break;

					case OpCodeFE.Clt:
						Pop2(out v1, out v2);
						ilValueStack.Add(ILValueConstants.GetInt32Constant(CompareSigned(v1, v2, isEquals: false) < 0 ? 1 : 0));
						break;

					case OpCodeFE.Clt_Un:
						Pop2(out v1, out v2);
						ilValueStack.Add(ILValueConstants.GetInt32Constant(CompareUnsigned(v1, v2, isEquals: false) < 0 ? 1 : 0));
						break;

					case OpCodeFE.Cpblk:
						Pop2(out v2, out v3);
						v1 = Pop1();
						if (!debuggerRuntime.CopyMemory(v1, v2, GetInt32OrNativeInt(v3)))
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case OpCodeFE.Initblk:
						Pop2(out v2, out v3);
						v1 = Pop1();
						i = GetByte(v2);
						l = GetInt32OrNativeInt(v3);
						if (v1.InitializeMemory((byte)i, l)) {
							// nothing
						}
						else if (!debuggerRuntime.InitializeMemory(v1, (byte)i, l))
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case OpCodeFE.Initobj:
						type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
						v1 = Pop1();
						if (!debuggerRuntime.InitializeObject(v1, type))
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case OpCodeFE.Ldftn:
						method = currentMethod.Module.ResolveMethod(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
						ilValueStack.Add(new FunctionPointerILValue(method));
						break;

					case OpCodeFE.Ldvirtftn:
						method = currentMethod.Module.ResolveMethod(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
						ilValueStack.Add(new FunctionPointerILValue(method, Pop1()));
						break;

					case OpCodeFE.Readonly:
					case OpCodeFE.Tailcall:
					case OpCodeFE.Volatile:
						isPrefix = true;
						break;

					case OpCodeFE.Unaligned:
						isPrefix = true;
						methodBodyPos++;
						break;

					case OpCodeFE.Constrained:
						isPrefix = true;
						constrainedType = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
						break;

					case OpCodeFE.No:
						isPrefix = true;
						goto default;

					case OpCodeFE.Localloc:
						l = GetInt32OrNativeInt(Pop1());
						if ((ulong)l >= MAX_LOCALLOC_SIZE)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(new NativeMemoryILValue(checked((int)l)));
						break;

					case OpCodeFE.Arglist:
					case OpCodeFE.Endfilter:
					case OpCodeFE.Refanytype:
					case OpCodeFE.Rethrow:
					default:
						throw new InstructionNotSupportedInterpreterException("Unsupported IL opcode 0xFE" + i.ToString("X2"));
					}
					break;

				case OpCode.Ldc_I4:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(ToInt32(bodyBytes, ref methodBodyPos)));
					break;

				case OpCode.Ldc_I4_S:
					ilValueStack.Add(ILValueConstants.GetInt32Constant((sbyte)bodyBytes[methodBodyPos++]));
					break;

				case OpCode.Ldc_I4_0:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(0));
					break;

				case OpCode.Ldc_I4_1:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(1));
					break;

				case OpCode.Ldc_I4_2:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(2));
					break;

				case OpCode.Ldc_I4_3:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(3));
					break;

				case OpCode.Ldc_I4_4:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(4));
					break;

				case OpCode.Ldc_I4_5:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(5));
					break;

				case OpCode.Ldc_I4_6:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(6));
					break;

				case OpCode.Ldc_I4_7:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(7));
					break;

				case OpCode.Ldc_I4_8:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(8));
					break;

				case OpCode.Ldc_I4_M1:
					ilValueStack.Add(ILValueConstants.GetInt32Constant(-1));
					break;

				case OpCode.Ldc_I8:
					ilValueStack.Add(new ConstantInt64ILValue(ToInt64(bodyBytes, ref methodBodyPos)));
					break;

				case OpCode.Ldc_R4:
					ilValueStack.Add(new ConstantFloatILValue(BitConverter.ToSingle(bodyBytes, methodBodyPos)));
					methodBodyPos += 4;
					break;

				case OpCode.Ldc_R8:
					ilValueStack.Add(new ConstantFloatILValue(BitConverter.ToDouble(bodyBytes, methodBodyPos)));
					methodBodyPos += 8;
					break;

				case OpCode.Ldstr:
					ilValueStack.Add(new ConstantStringILValue(currentMethod.Module.ResolveString(ToInt32(bodyBytes, ref methodBodyPos))));
					break;

				case OpCode.Ldnull:
					ilValueStack.Add(NullObjectRefILValue.Instance);
					break;

				case OpCode.Ldarg_0:
				case OpCode.Ldarg_1:
				case OpCode.Ldarg_2:
				case OpCode.Ldarg_3:
					v1 = debuggerRuntime.GetArgument(i - (int)OpCode.Ldarg_0);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldarg_S:
					v1 = debuggerRuntime.GetArgument(bodyBytes[methodBodyPos++]);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldarga_S:
					v1 = debuggerRuntime.GetArgumentAddress(bodyBytes[methodBodyPos++]);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldloc_0:
				case OpCode.Ldloc_1:
				case OpCode.Ldloc_2:
				case OpCode.Ldloc_3:
					v1 = debuggerRuntime.GetLocal(i - (int)OpCode.Ldloc_0);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldloc_S:
					v1 = debuggerRuntime.GetLocal(bodyBytes[methodBodyPos++]);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldloca_S:
					v1 = debuggerRuntime.GetLocalAddress(bodyBytes[methodBodyPos++]);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Stloc_0:
				case OpCode.Stloc_1:
				case OpCode.Stloc_2:
				case OpCode.Stloc_3:
					if (!debuggerRuntime.SetLocal(i - (int)OpCode.Stloc_0, Pop1()))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Starg_S:
					if (!debuggerRuntime.SetArgument(bodyBytes[methodBodyPos++], Pop1()))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stloc_S:
					if (!debuggerRuntime.SetLocal(bodyBytes[methodBodyPos++], Pop1()))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Ldlen:
					if (!debuggerRuntime.GetSZArrayLength(Pop1(), out l))
						ThrowInvalidMethodBodyInterpreterException();
					if (debuggerRuntime.PointerSize == 4)
						v1 = ConstantNativeIntILValue.Create32((int)l);
					else
						v1 = ConstantNativeIntILValue.Create64(l);
					ilValueStack.Add(v1);
					break;

				case OpCode.Ldelem:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.Ref, v1, GetInt32OrNativeInt(v2), type);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_I:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.I, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_I1:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.I1, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_I2:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.I2, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_I4:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.I4, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_I8:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.I8, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_R4:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.R4, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_R8:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.R8, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_Ref:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.Ref, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_U1:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.U1, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_U2:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.U2, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_U4:
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElement(PointerOpCodeType.U4, v1, GetInt32OrNativeInt(v2), null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelema:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					Pop2(out v1, out v2);
					v1 = debuggerRuntime.GetSZArrayElementAddress(v1, GetInt32OrNativeInt(v2), type);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Stelem:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					Pop2(out v1, out v2);
					if (!debuggerRuntime.SetSZArrayElement(PointerOpCodeType.Ref, Pop1(), GetInt32OrNativeInt(v1), v2, type))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_I:
					Pop2(out v1, out v2);
					if (!debuggerRuntime.SetSZArrayElement(PointerOpCodeType.I, Pop1(), GetInt32OrNativeInt(v1), v2, null))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_I1:
					Pop2(out v1, out v2);
					if (!debuggerRuntime.SetSZArrayElement(PointerOpCodeType.I1, Pop1(), GetInt32OrNativeInt(v1), v2, null))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_I2:
					Pop2(out v1, out v2);
					if (!debuggerRuntime.SetSZArrayElement(PointerOpCodeType.I2, Pop1(), GetInt32OrNativeInt(v1), v2, null))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_I4:
					Pop2(out v1, out v2);
					if (!debuggerRuntime.SetSZArrayElement(PointerOpCodeType.I4, Pop1(), GetInt32OrNativeInt(v1), v2, null))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_I8:
					Pop2(out v1, out v2);
					if (!debuggerRuntime.SetSZArrayElement(PointerOpCodeType.I8, Pop1(), GetInt32OrNativeInt(v1), v2, null))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_R4:
					Pop2(out v1, out v2);
					if (!debuggerRuntime.SetSZArrayElement(PointerOpCodeType.R4, Pop1(), GetInt32OrNativeInt(v1), v2, null))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_R8:
					Pop2(out v1, out v2);
					if (!debuggerRuntime.SetSZArrayElement(PointerOpCodeType.R8, Pop1(), GetInt32OrNativeInt(v1), v2, null))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_Ref:
					Pop2(out v1, out v2);
					if (!debuggerRuntime.SetSZArrayElement(PointerOpCodeType.Ref, Pop1(), GetInt32OrNativeInt(v1), v2, null))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Ldfld:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					v1 = debuggerRuntime.GetField(field, Pop1());
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldflda:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					v1 = debuggerRuntime.GetFieldAddress(field, Pop1());
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldsfld:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (!field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					v1 = debuggerRuntime.GetField(field, null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldsflda:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (!field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					v1 = debuggerRuntime.GetFieldAddress(field, null);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Stfld:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					Pop2(out v1, out v2);
					if (!debuggerRuntime.SetField(field, v1, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stsfld:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (!field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					if (!debuggerRuntime.SetField(field, null, Pop1()))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Ldind_I:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.I, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.I, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_I1:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.I1, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.I1, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_I2:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.I2, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.I2, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_I4:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.I4, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.I4, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_I8:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.I8, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.I8, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_R4:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.R4, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.R4, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_R8:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.R8, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.R8, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_Ref:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.Ref, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.Ref, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_U1:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.U1, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.U1, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_U2:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.U2, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.U2, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_U4:
					v2 = Pop1();
					if ((v1 = v2.ReadPointer(PointerOpCodeType.U4, debuggerRuntime.PointerSize)) != null) {
						// Nothing
					}
					else
						v1 = debuggerRuntime.ReadPointer(PointerOpCodeType.U4, v2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Stind_I:
					Pop2(out v1, out v2);
					if (v1.WritePointer(PointerOpCodeType.I, v2, debuggerRuntime.PointerSize)) {
						// Nothing
					}
					else if (!debuggerRuntime.WritePointer(PointerOpCodeType.I, v1, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_I1:
					Pop2(out v1, out v2);
					if (v1.WritePointer(PointerOpCodeType.I1, v2, debuggerRuntime.PointerSize)) {
						// Nothing
					}
					else if (!debuggerRuntime.WritePointer(PointerOpCodeType.I1, v1, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_I2:
					Pop2(out v1, out v2);
					if (v1.WritePointer(PointerOpCodeType.I2, v2, debuggerRuntime.PointerSize)) {
						// Nothing
					}
					else if (!debuggerRuntime.WritePointer(PointerOpCodeType.I2, v1, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_I4:
					Pop2(out v1, out v2);
					if (v1.WritePointer(PointerOpCodeType.I4, v2, debuggerRuntime.PointerSize)) {
						// Nothing
					}
					else if (!debuggerRuntime.WritePointer(PointerOpCodeType.I4, v1, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_I8:
					Pop2(out v1, out v2);
					if (v1.WritePointer(PointerOpCodeType.I8, v2, debuggerRuntime.PointerSize)) {
						// Nothing
					}
					else if (!debuggerRuntime.WritePointer(PointerOpCodeType.I8, v1, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_R4:
					Pop2(out v1, out v2);
					if (v1.WritePointer(PointerOpCodeType.R4, v2, debuggerRuntime.PointerSize)) {
						// Nothing
					}
					else if (!debuggerRuntime.WritePointer(PointerOpCodeType.R4, v1, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_R8:
					Pop2(out v1, out v2);
					if (v1.WritePointer(PointerOpCodeType.R8, v2, debuggerRuntime.PointerSize)) {
						// Nothing
					}
					else if (!debuggerRuntime.WritePointer(PointerOpCodeType.R8, v1, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_Ref:
					Pop2(out v1, out v2);
					if (v1.WritePointer(PointerOpCodeType.Ref, v2, debuggerRuntime.PointerSize)) {
						// Nothing
					}
					else if (!debuggerRuntime.WritePointer(PointerOpCodeType.Ref, v1, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Ldobj:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = debuggerRuntime.LoadTypeObject(Pop1(), type);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Stobj:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					Pop2(out v1, out v2);
					if (!debuggerRuntime.StoreTypeObject(v1, type, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Ldtoken:
					var member = currentMethod.Module.ResolveMember(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					switch (member.MemberType) {
					case DmdMemberTypes.TypeInfo:
					case DmdMemberTypes.NestedType:
						v1 = debuggerRuntime.CreateRuntimeTypeHandle((DmdType)member);
						break;

					case DmdMemberTypes.Field:
						v1 = debuggerRuntime.CreateRuntimeFieldHandle((DmdFieldInfo)member);
						break;

					case DmdMemberTypes.Method:
					case DmdMemberTypes.Constructor:
						v1 = debuggerRuntime.CreateRuntimeMethodHandle((DmdMethodBase)member);
						break;

					default:
						v1 = null;
						break;
					}
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Box:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = debuggerRuntime.Box(Pop1(), type);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Break:
					break;

				case OpCode.Call:
				case OpCode.Callvirt:
					method = currentMethod.Module.ResolveMethod(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					methodSig = method.GetMethodSignature();
					args = PopMethodArguments(methodSig);
					v1 = methodSig.HasThis ? Pop1() : null;
					if ((object)constrainedType != null) {
						if (i != (int)OpCode.Callvirt)
							ThrowInvalidMethodBodyInterpreterException();
						v1 = FixConstrainedType(constrainedType, method, v1);
					}
					if (!debuggerRuntime.Call(i == (int)OpCode.Callvirt, method, v1, args, out v1))
						ThrowInvalidMethodBodyInterpreterException();
					if (methodSig.ReturnType != currentMethod.AppDomain.System_Void)
						ilValueStack.Add(Convert(v1.Clone(), methodSig.ReturnType));
					break;

				case OpCode.Calli:
					methodSig = currentMethod.Module.ResolveMethodSignature(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v2 = Pop1();
					args = PopMethodArguments(methodSig);
					v1 = methodSig.HasThis ? Pop1() : null;
					if (!debuggerRuntime.CallIndirect(methodSig, v2, v1, args, out v1))
						ThrowInvalidMethodBodyInterpreterException();
					if (methodSig.ReturnType != currentMethod.AppDomain.System_Void)
						ilValueStack.Add(Convert(v1.Clone(), methodSig.ReturnType));
					break;

				case OpCode.Dup:
					ilValueStack.Add(ilValueStack[ilValueStack.Count - 1].Clone());
					break;

				case OpCode.Nop:
					break;

				case OpCode.Pop:
					Pop1();
					break;

				case OpCode.Castclass:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = Pop1();
					if (!v1.IsNull && !type.IsAssignableFrom(v1.GetType(type.AppDomain)))
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Isinst:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = Pop1();
					ilValueStack.Add(ILValueConstants.GetInt32Constant(type.IsAssignableFrom(v1.GetType(type.AppDomain)) ? 1 : 0));
					break;

				case OpCode.Newarr:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = debuggerRuntime.CreateSZArray(type, GetInt32OrNativeInt(Pop1()));
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Newobj:
					method = currentMethod.Module.ResolveMethod(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = debuggerRuntime.CreateTypeNoConstructor(method.ReflectedType);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					methodSig = method.GetMethodSignature();
					args = PopMethodArguments(methodSig);
					if (!(method is DmdConstructorInfo) || !methodSig.HasThis || methodSig.ReturnType != currentMethod.DeclaringType.AppDomain.System_Void)
						ThrowInvalidMethodBodyInterpreterException();
					if (!debuggerRuntime.Call(false, method, v1, args, out v2))
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Unbox:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = Pop1();
					//TODO:
					ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Unbox_Any:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = Pop1();
					if (v1.IsNull)
						v1 = type.IsNullable ? debuggerRuntime.CreateTypeNoConstructor(type) : v1;
					else if (v1 is BoxedValueTypeILValue) {
						v1 = ((BoxedValueTypeILValue)v1).Value;
						if (type.IsNullable) {
							method = type.GetConstructor(new[] { type.GetNullableElementType() });
							if ((object)method == null)
								ThrowInvalidMethodBodyInterpreterException();
							v2 = debuggerRuntime.CreateTypeNoConstructor(type);
							methodSig = method.GetMethodSignature();
							args = new[] { v1 };
							if (!debuggerRuntime.Call(false, method, v2, args, out v1))
								ThrowInvalidMethodBodyInterpreterException();
							v1 = v2;
						}
					}
					else if (v1.Kind != ILValueKind.ObjectRef)
						v1 = debuggerRuntime.UnboxAny(v1, type) ?? v1;
					ilValueStack.Add(v1);
					break;

				case OpCode.Cpobj:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					Pop2(out v1, out v2);
					if (!debuggerRuntime.CopyObject(v1, v2.Clone(), type))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Add:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(((ConstantInt32ILValue)v1).Value + ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if ((v3 = v2.PointerAdd(((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantInt32ILValue)v1).Value + ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantInt32ILValue)v1).Value + ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if ((v3 = v2.PointerAdd(((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else {
								v3 = debuggerRuntime.BinaryAdd(v1, v2);
								if (v3 == null)
									ThrowInvalidMethodBodyInterpreterException();
							}
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(((ConstantInt64ILValue)v1).Value + ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(((ConstantFloatILValue)v1).Value + ((ConstantFloatILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerAdd(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 + ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 + ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerAdd(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && (v3 = v2.PointerAdd(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 + ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 + ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if (v1 is ConstantNativeIntILValue && (v3 = v2.PointerAdd(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else {
								v3 = debuggerRuntime.BinaryAdd(v1, v2);
								if (v3 == null)
									ThrowInvalidMethodBodyInterpreterException();
							}
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerAdd(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerAdd(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.ByRef:
							v3 = debuggerRuntime.BinaryAdd(v1, v2);
							if (v3 == null)
								ThrowInvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Add_Ovf:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(checked(((ConstantInt32ILValue)v1).Value + ((ConstantInt32ILValue)v2).Value));
							break;

						case ILValueKind.NativeInt:
							if ((v3 = v2.PointerAddOvf(((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(checked(((ConstantInt32ILValue)v1).Value + ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(checked(((ConstantInt32ILValue)v1).Value + ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if ((v3 = v2.PointerAddOvf(((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else {
								v3 = debuggerRuntime.BinaryAddOvf(v1, v2);
								if (v3 == null)
									ThrowInvalidMethodBodyInterpreterException();
							}
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(checked(((ConstantInt64ILValue)v1).Value + ((ConstantInt64ILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(checked(((ConstantFloatILValue)v1).Value + ((ConstantFloatILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerAddOvf(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(checked(((ConstantNativeIntILValue)v1).Value32 + ((ConstantInt32ILValue)v2).Value));
								else
									v3 = ConstantNativeIntILValue.Create64(checked(((ConstantNativeIntILValue)v1).Value64 + ((ConstantInt32ILValue)v2).Value));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerAddOvf(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && (v3 = v2.PointerAddOvf(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(checked(((ConstantNativeIntILValue)v1).Value32 + ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(checked(((ConstantNativeIntILValue)v1).Value64 + ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if (v1 is ConstantNativeIntILValue && (v3 = v2.PointerAddOvf(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else {
								v3 = debuggerRuntime.BinaryAddOvf(v1, v2);
								if (v3 == null)
									ThrowInvalidMethodBodyInterpreterException();
							}
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerAddOvf(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerAddOvf(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.ByRef:
							v3 = debuggerRuntime.BinaryAddOvf(v1, v2);
							if (v3 == null)
								ThrowInvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Add_Ovf_Un:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant((int)checked(((ConstantInt32ILValue)v1).UnsignedValue + ((ConstantInt32ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.NativeInt:
							if ((v3 = v2.PointerAddOvfUn(((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)checked(((ConstantInt32ILValue)v1).UnsignedValue + ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64((long)checked(((ConstantInt32ILValue)v1).UnsignedValue + ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if ((v3 = v2.PointerAddOvfUn(((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else {
								v3 = debuggerRuntime.BinaryAddOvfUn(v1, v2);
								if (v3 == null)
									ThrowInvalidMethodBodyInterpreterException();
							}
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue((long)checked(((ConstantInt64ILValue)v1).UnsignedValue + ((ConstantInt64ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerAddOvfUn(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 + ((ConstantInt32ILValue)v2).UnsignedValue));
								else
									v3 = ConstantNativeIntILValue.Create64((long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 + ((ConstantInt32ILValue)v2).UnsignedValue));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerAddOvfUn(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && (v3 = v2.PointerAddOvfUn(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 + ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64((long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 + ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if (v1 is ConstantNativeIntILValue && (v3 = v2.PointerAddOvfUn(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else {
								v3 = debuggerRuntime.BinaryAddOvfUn(v1, v2);
								if (v3 == null)
									ThrowInvalidMethodBodyInterpreterException();
							}
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerAddOvfUn(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerAddOvfUn(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.ByRef:
							v3 = debuggerRuntime.BinaryAddOvfUn(v1, v2);
							if (v3 == null)
								ThrowInvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Sub:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(((ConstantInt32ILValue)v1).Value - ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantInt32ILValue)v1).Value - ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantInt32ILValue)v1).Value - ((ConstantNativeIntILValue)v2).Value64);
							}
							else {
								v3 = debuggerRuntime.BinarySub(v1, v2);
								if (v3 == null)
									ThrowInvalidMethodBodyInterpreterException();
							}
							break;

						case ILValueKind.ByRef:
						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(((ConstantInt64ILValue)v1).Value - ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(((ConstantFloatILValue)v1).Value - ((ConstantFloatILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerSub(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 - ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 - ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerSub(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 - ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 - ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							v3 = debuggerRuntime.BinarySub(v1, v2);
							if (v3 == null)
								ThrowInvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerSub(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerSub(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.ByRef:
							v3 = debuggerRuntime.BinarySub(v1, v2);
							if (v3 == null)
								ThrowInvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Sub_Ovf:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(checked(((ConstantInt32ILValue)v1).Value - ((ConstantInt32ILValue)v2).Value));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(checked(((ConstantInt32ILValue)v1).Value - ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(checked(((ConstantInt32ILValue)v1).Value - ((ConstantNativeIntILValue)v2).Value64));
							}
							else {
								v3 = debuggerRuntime.BinarySubOvf(v1, v2);
								if (v3 == null)
									ThrowInvalidMethodBodyInterpreterException();
							}
							break;

						case ILValueKind.ByRef:
						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(checked(((ConstantInt64ILValue)v1).Value - ((ConstantInt64ILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(checked(((ConstantFloatILValue)v1).Value - ((ConstantFloatILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerSubOvf(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(checked(((ConstantNativeIntILValue)v1).Value32 - ((ConstantInt32ILValue)v2).Value));
								else
									v3 = ConstantNativeIntILValue.Create64(checked(((ConstantNativeIntILValue)v1).Value64 - ((ConstantInt32ILValue)v2).Value));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerSubOvf(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(checked(((ConstantNativeIntILValue)v1).Value32 - ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(checked(((ConstantNativeIntILValue)v1).Value64 - ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							v3 = debuggerRuntime.BinarySubOvf(v1, v2);
							if (v3 == null)
								ThrowInvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerSubOvf(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerSubOvf(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.ByRef:
							v3 = debuggerRuntime.BinarySubOvf(v1, v2);
							if (v3 == null)
								ThrowInvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Sub_Ovf_Un:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant((int)checked(((ConstantInt32ILValue)v1).UnsignedValue - ((ConstantInt32ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)checked(((ConstantInt32ILValue)v1).UnsignedValue - ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64((long)checked(((ConstantInt32ILValue)v1).UnsignedValue - ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else {
								v3 = debuggerRuntime.BinarySubOvfUn(v1, v2);
								if (v3 == null)
									ThrowInvalidMethodBodyInterpreterException();
							}
							break;

						case ILValueKind.ByRef:
						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue((long)checked(((ConstantInt64ILValue)v1).UnsignedValue - ((ConstantInt64ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerSubOvfUn(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 - ((ConstantInt32ILValue)v2).UnsignedValue));
								else
									v3 = ConstantNativeIntILValue.Create64((long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 - ((ConstantInt32ILValue)v2).UnsignedValue));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerSubOvfUn(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 - ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64((long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 - ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							v3 = debuggerRuntime.BinarySubOvfUn(v1, v2);
							if (v3 == null)
								ThrowInvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.PointerSubOvfUn(((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.PointerSubOvfUn(debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.ByRef:
							v3 = debuggerRuntime.BinarySubOvfUn(v1, v2);
							if (v3 == null)
								ThrowInvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Mul:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(((ConstantInt32ILValue)v1).Value * ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantInt32ILValue)v1).Value * ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantInt32ILValue)v1).Value * ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(((ConstantInt64ILValue)v1).Value * ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(((ConstantFloatILValue)v1).Value * ((ConstantFloatILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 * ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 * ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 * ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 * ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Mul_Ovf:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(checked(((ConstantInt32ILValue)v1).Value * ((ConstantInt32ILValue)v2).Value));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(checked(((ConstantInt32ILValue)v1).Value * ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(checked(((ConstantInt32ILValue)v1).Value * ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(checked(((ConstantInt64ILValue)v1).Value * ((ConstantInt64ILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(checked(((ConstantFloatILValue)v1).Value * ((ConstantFloatILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(checked(((ConstantNativeIntILValue)v1).Value32 * ((ConstantInt32ILValue)v2).Value));
								else
									v3 = ConstantNativeIntILValue.Create64(checked(((ConstantNativeIntILValue)v1).Value64 * ((ConstantInt32ILValue)v2).Value));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(checked(((ConstantNativeIntILValue)v1).Value32 * ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(checked(((ConstantNativeIntILValue)v1).Value64 * ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Mul_Ovf_Un:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant((int)checked(((ConstantInt32ILValue)v1).UnsignedValue * ((ConstantInt32ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)checked(((ConstantInt32ILValue)v1).UnsignedValue * ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64((long)checked(((ConstantInt32ILValue)v1).UnsignedValue * ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue((long)checked(((ConstantInt64ILValue)v1).UnsignedValue * ((ConstantInt64ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 * ((ConstantInt32ILValue)v2).UnsignedValue));
								else
									v3 = ConstantNativeIntILValue.Create64((long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 * ((ConstantInt32ILValue)v2).UnsignedValue));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 * ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64((long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 * ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Div:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(((ConstantInt32ILValue)v1).Value / ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantInt32ILValue)v1).Value / ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantInt32ILValue)v1).Value / ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(((ConstantInt64ILValue)v1).Value / ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(((ConstantFloatILValue)v1).Value / ((ConstantFloatILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 / ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 / ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 / ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 / ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Div_Un:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant((int)(((ConstantInt32ILValue)v1).UnsignedValue / ((ConstantInt32ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)(((ConstantInt32ILValue)v1).UnsignedValue / ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64((long)(((ConstantInt32ILValue)v1).UnsignedValue64 / ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue((long)(((ConstantInt64ILValue)v1).UnsignedValue / ((ConstantInt64ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)(((ConstantNativeIntILValue)v1).UnsignedValue32 / ((ConstantInt32ILValue)v2).UnsignedValue));
								else
									v3 = ConstantNativeIntILValue.Create64((long)(((ConstantNativeIntILValue)v1).UnsignedValue64 / ((ConstantInt32ILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)(((ConstantNativeIntILValue)v1).UnsignedValue32 / ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64((long)(((ConstantNativeIntILValue)v1).UnsignedValue64 / ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Rem:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(((ConstantInt32ILValue)v1).Value % ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantInt32ILValue)v1).Value % ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantInt32ILValue)v1).Value % ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(((ConstantInt64ILValue)v1).Value % ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(((ConstantFloatILValue)v1).Value % ((ConstantFloatILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 % ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 % ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 % ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 % ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Rem_Un:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant((int)(((ConstantInt32ILValue)v1).UnsignedValue % ((ConstantInt32ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)(((ConstantInt32ILValue)v1).UnsignedValue % ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64((long)(((ConstantInt32ILValue)v1).UnsignedValue64 % ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue((long)(((ConstantInt64ILValue)v1).UnsignedValue % ((ConstantInt64ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)(((ConstantNativeIntILValue)v1).UnsignedValue32 % ((ConstantInt32ILValue)v2).UnsignedValue));
								else
									v3 = ConstantNativeIntILValue.Create64((long)(((ConstantNativeIntILValue)v1).UnsignedValue64 % ((ConstantInt32ILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((int)(((ConstantNativeIntILValue)v1).UnsignedValue32 % ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64((long)(((ConstantNativeIntILValue)v1).UnsignedValue64 % ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Neg:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(-((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(-((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = new ConstantFloatILValue(-((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(-((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = ConstantNativeIntILValue.Create64(-((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Shl:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(((ConstantInt32ILValue)v1).Value << (int)GetInt32OrNativeInt(v2));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(((ConstantInt64ILValue)v1).Value << (int)GetInt32OrNativeInt(v2));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 << (int)GetInt32OrNativeInt(v2));
							else
								v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 << (int)GetInt32OrNativeInt(v2));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Shr:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(((ConstantInt32ILValue)v1).Value >> (int)GetInt32OrNativeInt(v2));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(((ConstantInt64ILValue)v1).Value >> (int)GetInt32OrNativeInt(v2));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 >> (int)GetInt32OrNativeInt(v2));
							else
								v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 >> (int)GetInt32OrNativeInt(v2));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Shr_Un:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant((int)(((ConstantInt32ILValue)v1).UnsignedValue >> (int)GetInt32OrNativeInt(v2)));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue((long)(((ConstantInt64ILValue)v1).UnsignedValue >> (int)GetInt32OrNativeInt(v2)));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32((int)(((ConstantNativeIntILValue)v1).UnsignedValue32 >> (int)GetInt32OrNativeInt(v2)));
							else
								v3 = ConstantNativeIntILValue.Create64((long)(((ConstantNativeIntILValue)v1).UnsignedValue64 >> (int)GetInt32OrNativeInt(v2)));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.And:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(((ConstantInt32ILValue)v1).Value & ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantInt32ILValue)v1).Value & ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantInt32ILValue)v1).Value & ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(((ConstantInt64ILValue)v1).Value & ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.NativeInt:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 & ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 & ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 & ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 & ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Or:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(((ConstantInt32ILValue)v1).Value | ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantInt32ILValue)v1).Value | ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64((long)((ConstantInt32ILValue)v1).Value | ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(((ConstantInt64ILValue)v1).Value | ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.NativeInt:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32((((ConstantNativeIntILValue)v1).Value32 | ((ConstantInt32ILValue)v2).Value));
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 | (long)((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 | ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 | ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Xor:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = ILValueConstants.GetInt32Constant(((ConstantInt32ILValue)v1).Value ^ ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantInt32ILValue)v1).Value ^ ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantInt32ILValue)v1).Value ^ ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(((ConstantInt64ILValue)v1).Value ^ ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.NativeInt:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 ^ ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 ^ ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(((ConstantNativeIntILValue)v1).Value32 ^ ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(((ConstantNativeIntILValue)v1).Value64 ^ ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.ObjectRef:
						case ILValueKind.ValueType:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Not:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(~((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(~((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(~((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = ConstantNativeIntILValue.Create64(~((ConstantNativeIntILValue)v1).Value64);
						}
						else
							throw new InvalidMethodBodyInterpreterException();
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_I:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(((ConstantInt32ILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64(((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32((int)((ConstantInt64ILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64(((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32((int)((ConstantFloatILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64((long)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						v3 = v1;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
						v3 = debuggerRuntime.ConvI(v1);
						if (v3 == null)
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(checked(((ConstantInt32ILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64(checked(((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(checked((int)((ConstantInt64ILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64(checked(((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(checked((int)((ConstantFloatILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64(checked((long)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						v3 = v1;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
						v3 = debuggerRuntime.ConvOvfI(v1);
						if (v3 == null)
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_U:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32((int)(uint)((ConstantInt32ILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64((long)(uint)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32((int)(uint)((ConstantInt64ILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64((long)(ulong)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32((int)(uint)((ConstantFloatILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64((long)(ulong)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						v3 = v1;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
						v3 = debuggerRuntime.ConvU(v1);
						if (v3 == null)
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32((int)checked((uint)((ConstantInt32ILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64((long)checked((ulong)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32((int)checked((uint)((ConstantInt64ILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64((long)checked((ulong)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32((int)checked((uint)((ConstantFloatILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64((long)checked((ulong)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32((int)checked((uint)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = ConstantNativeIntILValue.Create64((long)checked((ulong)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
						v3 = debuggerRuntime.ConvOvfU(v1);
						if (v3 == null)
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(checked((int)((ConstantInt32ILValue)v1).UnsignedValue));
						else
							v3 = ConstantNativeIntILValue.Create64(checked((long)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(checked((int)((ConstantInt64ILValue)v1).UnsignedValue));
						else
							v3 = ConstantNativeIntILValue.Create64(checked((long)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(checked((int)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = ConstantNativeIntILValue.Create64(checked((long)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
						v3 = debuggerRuntime.ConvOvfIUn(v1);
						if (v3 == null)
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32((int)checked((uint)((ConstantInt32ILValue)v1).UnsignedValue));
						else
							v3 = ConstantNativeIntILValue.Create64((long)checked((ulong)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32((int)checked((uint)((ConstantInt64ILValue)v1).UnsignedValue));
						else
							v3 = ConstantNativeIntILValue.Create64((long)checked((ulong)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32((int)checked((uint)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = ConstantNativeIntILValue.Create64((long)checked((ulong)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
						v3 = debuggerRuntime.ConvOvfUUn(v1);
						if (v3 == null)
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_I1:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant((sbyte)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant((sbyte)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant((sbyte)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant((sbyte)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = ILValueConstants.GetInt32Constant((sbyte)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I1:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(checked((sbyte)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant(checked((sbyte)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant(checked((sbyte)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant(checked((sbyte)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = ILValueConstants.GetInt32Constant(checked((sbyte)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I1_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(checked((sbyte)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant(checked((sbyte)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant(checked((sbyte)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = ILValueConstants.GetInt32Constant(checked((sbyte)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_I2:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant((short)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant((short)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant((short)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant((short)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = ILValueConstants.GetInt32Constant((short)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I2:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(checked((short)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant(checked((short)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant(checked((short)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant(checked((short)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = ILValueConstants.GetInt32Constant(checked((short)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I2_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(checked((short)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant(checked((short)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant(checked((short)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = ILValueConstants.GetInt32Constant(checked((short)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_I4:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = v1;
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant((int)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant((int)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant((int)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = ILValueConstants.GetInt32Constant((int)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I4:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = v1;
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant(checked((int)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant(checked((int)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant(checked((int)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = ILValueConstants.GetInt32Constant(checked((int)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I4_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(checked((int)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant(checked((int)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant(checked((int)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = ILValueConstants.GetInt32Constant(checked((int)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_I8:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue(((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = v1;
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt64ILValue((long)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue(((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantInt64ILValue(((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I8:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue(checked(((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = v1;
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt64ILValue(checked((long)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue(checked(((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = new ConstantInt64ILValue(checked(((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I8_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue(checked((long)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(checked((long)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue(checked((long)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = new ConstantInt64ILValue(checked((long)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_U1:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant((byte)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant((byte)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant((byte)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant((byte)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = ILValueConstants.GetInt32Constant((byte)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U1:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(checked((byte)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant(checked((byte)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant(checked((byte)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant(checked((byte)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = ILValueConstants.GetInt32Constant(checked((byte)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U1_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(checked((byte)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant(checked((byte)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant(checked((byte)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = ILValueConstants.GetInt32Constant(checked((byte)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_U2:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant((ushort)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant((ushort)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant((ushort)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant((ushort)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = ILValueConstants.GetInt32Constant((ushort)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U2:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(checked((ushort)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant(checked((ushort)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant(checked((ushort)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant(checked((ushort)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = ILValueConstants.GetInt32Constant(checked((ushort)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U2_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant(checked((ushort)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant(checked((ushort)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant(checked((ushort)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = ILValueConstants.GetInt32Constant(checked((ushort)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_U4:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = v1;
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant((int)(uint)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant((int)(uint)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant((int)(uint)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = ILValueConstants.GetInt32Constant((int)(uint)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U4:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant((int)checked((uint)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant((int)checked((uint)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = ILValueConstants.GetInt32Constant((int)checked((uint)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant((int)checked((uint)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = ILValueConstants.GetInt32Constant((int)checked((uint)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U4_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = ILValueConstants.GetInt32Constant((int)checked((uint)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = ILValueConstants.GetInt32Constant((int)checked((uint)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ILValueConstants.GetInt32Constant((int)checked((uint)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = ILValueConstants.GetInt32Constant((int)checked((uint)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_U8:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue((long)(ulong)(uint)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = v1;
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt64ILValue((long)(ulong)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue((long)(uint)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantInt64ILValue((long)(ulong)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U8:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue((long)checked((ulong)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue((long)checked((ulong)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt64ILValue((long)checked((ulong)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue((long)checked((ulong)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = new ConstantInt64ILValue((long)checked((ulong)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U8_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue((long)checked((ulong)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue((long)checked((ulong)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue((long)checked((ulong)(((ConstantNativeIntILValue)v1).UnsignedValue32)));
							else
								v3 = new ConstantInt64ILValue((long)checked((ulong)(((ConstantNativeIntILValue)v1).UnsignedValue64)));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_R4:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantFloatILValue((float)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantFloatILValue((float)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = new ConstantFloatILValue((float)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantFloatILValue((float)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantFloatILValue((float)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_R8:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantFloatILValue((double)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantFloatILValue((double)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = v1;
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantFloatILValue((double)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantFloatILValue((double)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_R_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantFloatILValue((double)((ConstantInt32ILValue)v1).UnsignedValue);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantFloatILValue((double)((ConstantInt64ILValue)v1).UnsignedValue);
						break;

					case ILValueKind.Float:
						v3 = v1;
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantFloatILValue((double)((ConstantNativeIntILValue)v1).UnsignedValue32);
							else
								v3 = new ConstantFloatILValue((double)((ConstantNativeIntILValue)v1).UnsignedValue64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.ObjectRef:
					case ILValueKind.ValueType:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Beq:
				case OpCode.Beq_S:
					i = (i != (int)OpCode.Beq ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareSigned(v1, v2, isEquals: true) == 0)
						methodBodyPos = i;
					break;

				case OpCode.Bge:
				case OpCode.Bge_S:
					i = (i != (int)OpCode.Bge ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareSigned(v1, v2, isEquals: false) >= 0)
						methodBodyPos = i;
					break;

				case OpCode.Bge_Un:
				case OpCode.Bge_Un_S:
					i = (i != (int)OpCode.Bge_Un ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareUnsigned(v1, v2, isEquals: false) >= 0)
						methodBodyPos = i;
					break;

				case OpCode.Bgt:
				case OpCode.Bgt_S:
					i = (i != (int)OpCode.Bgt ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareSigned(v1, v2, isEquals: false) > 0)
						methodBodyPos = i;
					break;

				case OpCode.Bgt_Un:
				case OpCode.Bgt_Un_S:
					i = (i != (int)OpCode.Bgt_Un ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareUnsigned(v1, v2, isEquals: false) > 0)
						methodBodyPos = i;
					break;

				case OpCode.Ble:
				case OpCode.Ble_S:
					i = (i != (int)OpCode.Ble ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareSigned(v1, v2, isEquals: false) <= 0)
						methodBodyPos = i;
					break;

				case OpCode.Ble_Un:
				case OpCode.Ble_Un_S:
					i = (i != (int)OpCode.Ble_Un ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareUnsigned(v1, v2, isEquals: false) <= 0)
						methodBodyPos = i;
					break;

				case OpCode.Blt:
				case OpCode.Blt_S:
					i = (i != (int)OpCode.Blt ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareSigned(v1, v2, isEquals: false) < 0)
						methodBodyPos = i;
					break;

				case OpCode.Blt_Un:
				case OpCode.Blt_Un_S:
					i = (i != (int)OpCode.Blt_Un ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareUnsigned(v1, v2, isEquals: false) < 0)
						methodBodyPos = i;
					break;

				case OpCode.Bne_Un:
				case OpCode.Bne_Un_S:
					i = (i != (int)OpCode.Bne_Un ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareUnsigned(v1, v2, isEquals: true) != 0)
						methodBodyPos = i;
					break;

				case OpCode.Br:
				case OpCode.Br_S:
					methodBodyPos = (i != (int)OpCode.Br ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					break;

				case OpCode.Leave:
				case OpCode.Leave_S:
					methodBodyPos = (i != (int)OpCode.Leave ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					ilValueStack.Clear();
					break;

				case OpCode.Endfinally:
					ilValueStack.Clear();
					goto default;

				case OpCode.Brfalse:
				case OpCode.Brfalse_S:
					i = (i != (int)OpCode.Brfalse ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					if (CompareFalse(Pop1()))
						methodBodyPos = i;
					break;

				case OpCode.Brtrue:
				case OpCode.Brtrue_S:
					i = (i != (int)OpCode.Brtrue ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					if (!CompareFalse(Pop1()))
						methodBodyPos = i;
					break;

				case OpCode.Switch:
					v1 = Pop1();
					l = GetInt64ForSwitch(v1);
					if (debuggerRuntime.PointerSize == 4)
						i = (int)l;
					else
						i = l < 0 || l > int.MaxValue ? -1 : (int)l;
					j = ToInt32(bodyBytes, ref methodBodyPos);
					if ((uint)i >= (uint)j || (uint)i >= 0x40000000U)
						methodBodyPos += j * 4;
					else {
						i = methodBodyPos + i * 4;
						i = ToInt32(bodyBytes, ref i);
						methodBodyPos += j * 4 + i;
					}
					break;

				case OpCode.Ckfinite:
					v1 = Pop1();
					if (v1.Kind != ILValueKind.Float)
						ThrowInvalidMethodBodyInterpreterException();
					if (!CheckFinite(((ConstantFloatILValue)v1).Value))
						throw new OverflowException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Ret:
					if (currentMethod.GetMethodSignature().ReturnType == currentMethod.AppDomain.System_Void) {
						if (ilValueStack.Count != 0)
							ThrowInvalidMethodBodyInterpreterException();
						return NullObjectRefILValue.Instance;
					}
					else {
						if (ilValueStack.Count != 1)
							ThrowInvalidMethodBodyInterpreterException();
						return Convert(Pop1(), currentMethod.GetMethodSignature().ReturnType);
					}

				case OpCode.Jmp:
				case OpCode.Mkrefany:
				case OpCode.Refanyval:
				case OpCode.Throw:
				case OpCode.Prefix2:
				case OpCode.Prefix3:
				case OpCode.Prefix4:
				case OpCode.Prefix5:
				case OpCode.Prefix6:
				case OpCode.Prefix7:
				case OpCode.Prefixref:
				default:
					throw new InstructionNotSupportedInterpreterException("Unsupported IL opcode 0x" + i.ToString("X2"));
				}
				if (!isPrefix)
					constrainedType = null;
			}
		}

		static bool CheckFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

		int CompareSigned(ILValue v1, ILValue v2, bool isEquals) {
			switch (v1.Kind) {
			case ILValueKind.Int32:
				switch (v2.Kind) {
				case ILValueKind.Int32:
					return ((ConstantInt32ILValue)v1).Value.CompareTo(((ConstantInt32ILValue)v2).Value);

				case ILValueKind.NativeInt:
					if (v2 is ConstantNativeIntILValue) {
						if (debuggerRuntime.PointerSize == 4)
							return (((ConstantInt32ILValue)v1).Value).CompareTo(((ConstantNativeIntILValue)v2).Value32);
						return ((long)((ConstantInt32ILValue)v1).Value).CompareTo(((ConstantNativeIntILValue)v2).Value64);
					}
					goto case ILValueKind.ByRef;

				case ILValueKind.ByRef:
					if (v1 == v2)
						return 0;
					if (isEquals) {
						var res = debuggerRuntime.Equals(v1, v2);
						if (res == null)
							throw new InvalidMethodBodyInterpreterException();
						return res.Value ? 0 : 1;
					}
					return debuggerRuntime.CompareSigned(v1, v2) ?? throw new InvalidMethodBodyInterpreterException();

				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.ObjectRef:
				case ILValueKind.ValueType:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.Int64:
				switch (v2.Kind) {
				case ILValueKind.Int64:
					return ((ConstantInt64ILValue)v1).Value.CompareTo(((ConstantInt64ILValue)v2).Value);

				case ILValueKind.Int32:
				case ILValueKind.Float:
				case ILValueKind.NativeInt:
				case ILValueKind.ByRef:
				case ILValueKind.ObjectRef:
				case ILValueKind.ValueType:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.Float:
				switch (v2.Kind) {
				case ILValueKind.Float:
					return ((ConstantFloatILValue)v1).Value.CompareTo(((ConstantFloatILValue)v2).Value);

				case ILValueKind.Int32:
				case ILValueKind.Int64:
				case ILValueKind.NativeInt:
				case ILValueKind.ByRef:
				case ILValueKind.ObjectRef:
				case ILValueKind.ValueType:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.NativeInt:
				switch (v2.Kind) {
				case ILValueKind.Int32:
					if (v1 is ConstantNativeIntILValue) {
						if (debuggerRuntime.PointerSize == 4)
							return ((ConstantNativeIntILValue)v1).Value32.CompareTo(((ConstantInt32ILValue)v2).Value);
						return ((ConstantNativeIntILValue)v1).Value64.CompareTo(((ConstantInt32ILValue)v2).Value);
					}
					goto case ILValueKind.ByRef;

				case ILValueKind.NativeInt:
					if (v1 == v2)
						return 0;
					if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
						if (debuggerRuntime.PointerSize == 4)
							return ((ConstantNativeIntILValue)v1).Value32.CompareTo(((ConstantNativeIntILValue)v2).Value32);
						return ((ConstantNativeIntILValue)v1).Value64.CompareTo(((ConstantNativeIntILValue)v2).Value64);
					}
					goto case ILValueKind.ByRef;

				case ILValueKind.ByRef:
					if (v1 == v2)
						return 0;
					if (isEquals) {
						var res = debuggerRuntime.Equals(v1, v2);
						if (res == null)
							throw new InvalidMethodBodyInterpreterException();
						return res.Value ? 0 : 1;
					}
					return debuggerRuntime.CompareSigned(v1, v2) ?? throw new InvalidMethodBodyInterpreterException();

				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.ObjectRef:
				case ILValueKind.ValueType:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.ByRef:
				switch (v2.Kind) {
				case ILValueKind.Int32:
				case ILValueKind.NativeInt:
				case ILValueKind.ByRef:
					if (v1 == v2)
						return 0;
					if (isEquals) {
						var res = debuggerRuntime.Equals(v1, v2);
						if (res == null)
							throw new InvalidMethodBodyInterpreterException();
						return res.Value ? 0 : 1;
					}
					return debuggerRuntime.CompareSigned(v1, v2) ?? throw new InvalidMethodBodyInterpreterException();

				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.ObjectRef:
				case ILValueKind.ValueType:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.ObjectRef:
				if (v1 == v2)
					return 0;
				if (isEquals) {
					var res = debuggerRuntime.Equals(v1, v2);
					if (res == null)
						throw new InvalidMethodBodyInterpreterException();
					return res.Value ? 0 : 1;
				}
				return v1 == v2 ? 0 : throw new InvalidMethodBodyInterpreterException();

			case ILValueKind.ValueType:
			default:
				throw new InvalidMethodBodyInterpreterException();
			}
		}

		int CompareUnsigned(ILValue v1, ILValue v2, bool isEquals) {
			switch (v1.Kind) {
			case ILValueKind.Int32:
				switch (v2.Kind) {
				case ILValueKind.Int32:
					return ((ConstantInt32ILValue)v1).UnsignedValue.CompareTo(((ConstantInt32ILValue)v2).UnsignedValue);

				case ILValueKind.NativeInt:
					if (v2 is ConstantNativeIntILValue) {
						if (debuggerRuntime.PointerSize == 4)
							return ((ConstantInt32ILValue)v1).UnsignedValue.CompareTo((((ConstantNativeIntILValue)v2).UnsignedValue32));
						return ((ulong)((ConstantInt32ILValue)v1).Value).CompareTo(((ConstantNativeIntILValue)v2).UnsignedValue64);
					}
					goto case ILValueKind.ByRef;

				case ILValueKind.ByRef:
					if (v1 == v2)
						return 0;
					if (isEquals) {
						var res = debuggerRuntime.Equals(v1, v2);
						if (res == null)
							throw new InvalidMethodBodyInterpreterException();
						return res.Value ? 0 : 1;
					}
					return debuggerRuntime.CompareSigned(v1, v2) ?? throw new InvalidMethodBodyInterpreterException();

				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.ObjectRef:
				case ILValueKind.ValueType:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.Int64:
				switch (v2.Kind) {
				case ILValueKind.Int64:
					return ((ulong)((ConstantInt64ILValue)v1).Value).CompareTo((ulong)((ConstantInt64ILValue)v2).Value);

				case ILValueKind.Int32:
				case ILValueKind.Float:
				case ILValueKind.NativeInt:
				case ILValueKind.ByRef:
				case ILValueKind.ObjectRef:
				case ILValueKind.ValueType:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.Float:
				switch (v2.Kind) {
				case ILValueKind.Float:
					return ((ConstantFloatILValue)v1).Value.CompareTo(((ConstantFloatILValue)v2).Value);

				case ILValueKind.Int32:
				case ILValueKind.Int64:
				case ILValueKind.NativeInt:
				case ILValueKind.ByRef:
				case ILValueKind.ObjectRef:
				case ILValueKind.ValueType:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.NativeInt:
				switch (v2.Kind) {
				case ILValueKind.Int32:
					if (v1 is ConstantNativeIntILValue) {
						if (debuggerRuntime.PointerSize == 4)
							return ((ConstantNativeIntILValue)v1).UnsignedValue32.CompareTo(((ConstantInt32ILValue)v2).UnsignedValue);
						return ((ConstantNativeIntILValue)v1).UnsignedValue64.CompareTo(((ConstantInt32ILValue)v2).UnsignedValue);
					}
					goto case ILValueKind.ByRef;

				case ILValueKind.NativeInt:
					if (v1 == v2)
						return 0;
					if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
						if (debuggerRuntime.PointerSize == 4)
							return ((ConstantNativeIntILValue)v1).UnsignedValue32.CompareTo(((ConstantNativeIntILValue)v2).UnsignedValue32);
						return ((ConstantNativeIntILValue)v1).UnsignedValue64.CompareTo(((ConstantNativeIntILValue)v2).UnsignedValue64);
					}
					goto case ILValueKind.ByRef;

				case ILValueKind.ByRef:
					if (v1 == v2)
						return 0;
					if (isEquals) {
						var res = debuggerRuntime.Equals(v1, v2);
						if (res == null)
							throw new InvalidMethodBodyInterpreterException();
						return res.Value ? 0 : 1;
					}
					return debuggerRuntime.CompareSigned(v1, v2) ?? throw new InvalidMethodBodyInterpreterException();

				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.ObjectRef:
				case ILValueKind.ValueType:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.ByRef:
				switch (v2.Kind) {
				case ILValueKind.Int32:
				case ILValueKind.NativeInt:
				case ILValueKind.ByRef:
					if (v1 == v2)
						return 0;
					if (isEquals) {
						var res = debuggerRuntime.Equals(v1, v2);
						if (res == null)
							throw new InvalidMethodBodyInterpreterException();
						return res.Value ? 0 : 1;
					}
					return debuggerRuntime.CompareSigned(v1, v2) ?? throw new InvalidMethodBodyInterpreterException();

				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.ObjectRef:
				case ILValueKind.ValueType:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.ObjectRef:
				if (v1 == v2)
					return 0;
				if (isEquals) {
					var res = debuggerRuntime.Equals(v1, v2);
					if (res == null)
						throw new InvalidMethodBodyInterpreterException();
					return res.Value ? 0 : 1;
				}
				return v1 == v2 ? 0 : throw new InvalidMethodBodyInterpreterException();

			case ILValueKind.ValueType:
			default:
				throw new InvalidMethodBodyInterpreterException();
			}
		}

		bool CompareFalse(ILValue v1) {
			if (v1.IsNull)
				return true;
			switch (v1.Kind) {
			case ILValueKind.Int32:
				return ((ConstantInt32ILValue)v1).Value == 0;

			case ILValueKind.Int64:
				return ((ConstantInt64ILValue)v1).Value == 0;

			case ILValueKind.Float:
				return ((ConstantFloatILValue)v1).Value == 0;

			case ILValueKind.NativeInt:
				if (v1 is ConstantNativeIntILValue) {
					if (debuggerRuntime.PointerSize == 4)
						return ((ConstantNativeIntILValue)v1).Value32 == 0;
					return ((ConstantNativeIntILValue)v1).Value64 == 0;
				}
				goto case ILValueKind.ByRef;

			case ILValueKind.ByRef:
			case ILValueKind.ObjectRef:
				return false;

			case ILValueKind.ValueType:
			default:
				throw new InvalidMethodBodyInterpreterException();
			}
		}

		int GetSizeOf(DmdType type) {
			if (!type.IsValueType)
				return debuggerRuntime.PointerSize;

			switch (DmdType.GetTypeCode(type)) {
			case TypeCode.Boolean:	return 1;
			case TypeCode.Char:		return 2;
			case TypeCode.SByte:
			case TypeCode.Byte:		return 1;
			case TypeCode.Int16:
			case TypeCode.UInt16:	return 2;
			case TypeCode.Int32:
			case TypeCode.UInt32:	return 4;
			case TypeCode.Int64:
			case TypeCode.UInt64:	return 8;
			case TypeCode.Single:	return 4;
			case TypeCode.Double:	return 8;
			case TypeCode.Decimal:	return 16;
			case TypeCode.DateTime:	return 8;
			}

			if (type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr)
				return debuggerRuntime.PointerSize;

			//TODO:
			throw new InvalidMethodBodyInterpreterException();
		}

		long GetInt32OrNativeInt(ILValue v) {
			switch (v.Kind) {
			case ILValueKind.Int32:
				return ((ConstantInt32ILValue)v).Value;

			case ILValueKind.NativeInt:
				if (v is ConstantNativeIntILValue cv) {
					if (debuggerRuntime.PointerSize == 4)
						return cv.Value32;
					return cv.Value64;
				}
				break;
			}

			throw new InvalidMethodBodyInterpreterException();
		}

		long GetInt64ForSwitch(ILValue v) {
			switch (v.Kind) {
			case ILValueKind.Int32:
				return ((ConstantInt32ILValue)v).Value;

			case ILValueKind.Int64:
				return ((ConstantInt64ILValue)v).Value;

			case ILValueKind.Float:
				if (debuggerRuntime.PointerSize == 4) {
					// Should always use 'default' switch case
					return -1;
				}
				return (int)((ConstantFloatILValue)v).Value;

			case ILValueKind.NativeInt:
				var cv = v as ConstantNativeIntILValue;
				if (cv != null) {
					if (debuggerRuntime.PointerSize == 4)
						return cv.Value32;
					return cv.Value64;
				}
				break;
			}

			throw new InvalidMethodBodyInterpreterException();
		}

		byte GetByte(ILValue v) {
			switch (v.Kind) {
			case ILValueKind.Int32:
				return (byte)((ConstantInt32ILValue)v).Value;

			default:
				throw new InvalidMethodBodyInterpreterException();
			}
		}

		ILValue FixConstrainedType(DmdType constrainedType, DmdMethodBase method, ILValue v1) {
			Debug.Assert((object)constrainedType != null);
			Debug.Assert((object)method != null);
			if (v1 == null)
				ThrowInvalidMethodBodyInterpreterException();
			if (constrainedType.IsValueType) {
				if (ImplementsMethod(constrainedType, method.Name, method.GetMethodSignature()))
					return v1;
				else
					return debuggerRuntime.Box(debuggerRuntime.ReadPointer(PointerOpCodeType.Ref, v1), constrainedType);
			}
			else
				return debuggerRuntime.ReadPointer(PointerOpCodeType.Ref, v1);
		}

		static bool ImplementsMethod(DmdType type, string name, DmdMethodSignature methodSig) {
			foreach (var m in type.DeclaredMethods) {
				if (!m.IsVirtual)
					continue;
				if (m.Name != name)
					continue;
				if (m.GetMethodSignature() != methodSig)
					continue;

				return true;
			}

			return false;
		}
	}
}
