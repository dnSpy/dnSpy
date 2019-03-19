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

		ILValue Convert(ILValue value, DmdType targetType, bool boxIfNeeded = true) {
			// We want to return the same ILValue, if possible, since it can contain extra information,
			// such as address of value that the caller (debugger) would like to keep.
			var type = value.Type;
			if (targetType.IsAssignableFrom(type)) {
				if (boxIfNeeded && type.IsValueType && type != targetType)
					value = debuggerRuntime.Box(value, type) ?? value;
				return value;
			}

			long l;
			double d;
			switch (DmdType.GetTypeCode(targetType)) {
			case TypeCode.Boolean:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue(targetType, (byte)l);

			case TypeCode.Char:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue(targetType, (char)l);

			case TypeCode.SByte:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue(targetType, (sbyte)l);

			case TypeCode.Byte:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue(targetType, (byte)l);

			case TypeCode.Int16:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue(targetType, (short)l);

			case TypeCode.UInt16:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue(targetType, (ushort)l);

			case TypeCode.Int32:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue(targetType, (int)l);

			case TypeCode.UInt32:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt32ILValue(targetType, (int)l);

			case TypeCode.Int64:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt64ILValue(targetType, l);

			case TypeCode.UInt64:
				if (!GetValue(value, out l))
					return value;
				return new ConstantInt64ILValue(targetType, l);

			case TypeCode.Single:
				if (!GetValue(value, out d))
					return value;
				return new ConstantFloatILValue(targetType, (float)d);

			case TypeCode.Double:
				if (!GetValue(value, out d))
					return value;
				return new ConstantFloatILValue(targetType, d);

			default:
				if (targetType == targetType.AppDomain.System_IntPtr || targetType == targetType.AppDomain.System_UIntPtr) {
					if (GetValue(value, out l))
						return debuggerRuntime.PointerSize == 4 ? ConstantNativeIntILValue.Create32(targetType, (int)l) : ConstantNativeIntILValue.Create64(targetType, l);
				}
				if ((object)type != null && (type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr) && (targetType.IsPointer || targetType.IsFunctionPointer)) {
					if (GetValue(value, out l))
						return debuggerRuntime.PointerSize == 4 ? ConstantNativeIntILValue.Create32(targetType, (int)l) : ConstantNativeIntILValue.Create64(targetType, l);
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
			debuggerRuntime.Initialize(currentMethod, body);
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
						v1 = debuggerRuntime.LoadArgument(ToUInt16(bodyBytes, ref methodBodyPos));
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1.Clone());
						break;

					case OpCodeFE.Ldarga:
						i = ToUInt16(bodyBytes, ref methodBodyPos);
						v1 = debuggerRuntime.LoadArgumentAddress(i, currentMethod.GetParameters()[i].ParameterType);
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1.Clone());
						break;

					case OpCodeFE.Ldloc:
						v1 = debuggerRuntime.LoadLocal(ToUInt16(bodyBytes, ref methodBodyPos));
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1.Clone());
						break;

					case OpCodeFE.Ldloca:
						i = ToUInt16(bodyBytes, ref methodBodyPos);
						v1 = debuggerRuntime.LoadLocalAddress(i, state.Body.LocalVariables[i].LocalType);
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1.Clone());
						break;

					case OpCodeFE.Starg:
						i = ToUInt16(bodyBytes, ref methodBodyPos);
						type = currentMethod.GetParameters()[i].ParameterType;
						if (!debuggerRuntime.StoreArgument(i, type, Convert(Pop1(), type)))
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case OpCodeFE.Stloc:
						i = ToUInt16(bodyBytes, ref methodBodyPos);
						type = state.Body.LocalVariables[i].LocalType;
						if (!debuggerRuntime.StoreLocal(i, type, Convert(Pop1(), type)))
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case OpCodeFE.Sizeof:
						type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
						ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, GetSizeOf(type)));
						break;

					case OpCodeFE.Ceq:
						Pop2(out v1, out v2);
						ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, CompareEquals(v1, v2) ? 1 : 0));
						break;

					case OpCodeFE.Cgt:
						Pop2(out v1, out v2);
						ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, CompareSigned(v1, v2) > 0 ? 1 : 0));
						break;

					case OpCodeFE.Cgt_Un:
						Pop2(out v1, out v2);
						ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, CompareUnsigned(v1, v2) > 0 ? 1 : 0));
						break;

					case OpCodeFE.Clt:
						Pop2(out v1, out v2);
						ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, CompareSigned(v1, v2) < 0 ? 1 : 0));
						break;

					case OpCodeFE.Clt_Un:
						Pop2(out v1, out v2);
						ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, CompareUnsigned(v1, v2) < 0 ? 1 : 0));
						break;

					case OpCodeFE.Cpblk:
						Pop2(out v2, out v3);
						v1 = Pop1();
						l = GetInt32OrNativeInt(v3);
						if (!v1.CopyMemory(v2, l))
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case OpCodeFE.Initblk:
						Pop2(out v2, out v3);
						v1 = Pop1();
						i = GetByte(v2);
						l = GetInt32OrNativeInt(v3);
						if (!v1.InitializeMemory((byte)i, l))
							ThrowInvalidMethodBodyInterpreterException();
						break;

					case OpCodeFE.Initobj:
						type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
						v1 = Pop1();
						if (!v1.InitializeObject(type))
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
						ilValueStack.Add(new NativeMemoryILValue(currentMethod.AppDomain, checked((int)l)));
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
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, ToInt32(bodyBytes, ref methodBodyPos)));
					break;

				case OpCode.Ldc_I4_S:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, (sbyte)bodyBytes[methodBodyPos++]));
					break;

				case OpCode.Ldc_I4_0:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, 0));
					break;

				case OpCode.Ldc_I4_1:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, 1));
					break;

				case OpCode.Ldc_I4_2:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, 2));
					break;

				case OpCode.Ldc_I4_3:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, 3));
					break;

				case OpCode.Ldc_I4_4:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, 4));
					break;

				case OpCode.Ldc_I4_5:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, 5));
					break;

				case OpCode.Ldc_I4_6:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, 6));
					break;

				case OpCode.Ldc_I4_7:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, 7));
					break;

				case OpCode.Ldc_I4_8:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, 8));
					break;

				case OpCode.Ldc_I4_M1:
					ilValueStack.Add(new ConstantInt32ILValue(currentMethod.AppDomain, -1));
					break;

				case OpCode.Ldc_I8:
					ilValueStack.Add(new ConstantInt64ILValue(currentMethod.AppDomain, ToInt64(bodyBytes, ref methodBodyPos)));
					break;

				case OpCode.Ldc_R4:
					ilValueStack.Add(new ConstantFloatILValue(currentMethod.AppDomain.System_Single, BitConverter.ToSingle(bodyBytes, methodBodyPos)));
					methodBodyPos += 4;
					break;

				case OpCode.Ldc_R8:
					ilValueStack.Add(new ConstantFloatILValue(currentMethod.AppDomain.System_Double, BitConverter.ToDouble(bodyBytes, methodBodyPos)));
					methodBodyPos += 8;
					break;

				case OpCode.Ldstr:
					v1 = debuggerRuntime.LoadString(currentMethod.AppDomain.System_String, currentMethod.Module.ResolveString(ToInt32(bodyBytes, ref methodBodyPos)));
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Ldnull:
					ilValueStack.Add(new NullObjectRefILValue());
					break;

				case OpCode.Ldarg_0:
				case OpCode.Ldarg_1:
				case OpCode.Ldarg_2:
				case OpCode.Ldarg_3:
					v1 = debuggerRuntime.LoadArgument(i - (int)OpCode.Ldarg_0);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldarg_S:
					v1 = debuggerRuntime.LoadArgument(bodyBytes[methodBodyPos++]);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldarga_S:
					i = bodyBytes[methodBodyPos++];
					v1 = debuggerRuntime.LoadArgumentAddress(i, currentMethod.GetParameters()[i].ParameterType);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldloc_0:
				case OpCode.Ldloc_1:
				case OpCode.Ldloc_2:
				case OpCode.Ldloc_3:
					v1 = debuggerRuntime.LoadLocal(i - (int)OpCode.Ldloc_0);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldloc_S:
					v1 = debuggerRuntime.LoadLocal(bodyBytes[methodBodyPos++]);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldloca_S:
					i = bodyBytes[methodBodyPos++];
					v1 = debuggerRuntime.LoadLocalAddress(i, state.Body.LocalVariables[i].LocalType);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Stloc_0:
				case OpCode.Stloc_1:
				case OpCode.Stloc_2:
				case OpCode.Stloc_3:
					i -= (int)OpCode.Stloc_0;
					type = state.Body.LocalVariables[i].LocalType;
					if (!debuggerRuntime.StoreLocal(i, type, Convert(Pop1(), type)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Starg_S:
					i = bodyBytes[methodBodyPos++];
					type = currentMethod.GetParameters()[i].ParameterType;
					if (!debuggerRuntime.StoreArgument(i, type, Convert(Pop1(), type)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stloc_S:
					i = bodyBytes[methodBodyPos++];
					type = state.Body.LocalVariables[i].LocalType;
					if (!debuggerRuntime.StoreLocal(i, type, Convert(Pop1(), type)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Ldlen:
					v1 = Pop1();
					if (!v1.GetSZArrayLength(out l))
						ThrowInvalidMethodBodyInterpreterException();
					if (debuggerRuntime.PointerSize == 4)
						v1 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)l);
					else
						v1 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, l);
					ilValueStack.Add(v1);
					break;

				case OpCode.Ldelem:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(GetLoadValueType(type), l, type);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_I:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.I, l, currentMethod.AppDomain.System_IntPtr);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_I1:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.I1, l, currentMethod.AppDomain.System_SByte);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_I2:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.I2, l, currentMethod.AppDomain.System_Int16);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_I4:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.I4, l, currentMethod.AppDomain.System_Int32);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_I8:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.I8, l, currentMethod.AppDomain.System_Int64);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_R4:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.R4, l, currentMethod.AppDomain.System_Single);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_R8:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.R8, l, currentMethod.AppDomain.System_Double);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_Ref:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.Ref, l, currentMethod.AppDomain.System_Object);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_U1:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.U1, l, currentMethod.AppDomain.System_Byte);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_U2:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.U2, l, currentMethod.AppDomain.System_UInt16);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelem_U4:
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElement(LoadValueType.U4, l, currentMethod.AppDomain.System_UInt32);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldelema:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					Pop2(out v1, out v2);
					l = GetInt32OrNativeInt(v2);
					v1 = v1.LoadSZArrayElementAddress(l, type);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Stelem:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					Pop2(out v1, out v2);
					v3 = Pop1();
					l = GetInt32OrNativeInt(v1);
					if (!v3.StoreSZArrayElement(GetLoadValueType(type), l, Convert(v2, type), type))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_I:
					Pop2(out v1, out v2);
					v3 = Pop1();
					l = GetInt32OrNativeInt(v1);
					if (!v3.StoreSZArrayElement(LoadValueType.I, l, Convert(v2, currentMethod.AppDomain.System_IntPtr), currentMethod.AppDomain.System_IntPtr))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_I1:
					Pop2(out v1, out v2);
					v3 = Pop1();
					l = GetInt32OrNativeInt(v1);
					if (!v3.StoreSZArrayElement(LoadValueType.I1, l, Convert(v2, currentMethod.AppDomain.System_SByte), currentMethod.AppDomain.System_SByte))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_I2:
					Pop2(out v1, out v2);
					v3 = Pop1();
					l = GetInt32OrNativeInt(v1);
					if (!v3.StoreSZArrayElement(LoadValueType.I2, l, Convert(v2, currentMethod.AppDomain.System_Int16), currentMethod.AppDomain.System_Int16))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_I4:
					Pop2(out v1, out v2);
					v3 = Pop1();
					l = GetInt32OrNativeInt(v1);
					if (!v3.StoreSZArrayElement(LoadValueType.I4, l, Convert(v2, currentMethod.AppDomain.System_Int32), currentMethod.AppDomain.System_Int32))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_I8:
					Pop2(out v1, out v2);
					v3 = Pop1();
					l = GetInt32OrNativeInt(v1);
					if (!v3.StoreSZArrayElement(LoadValueType.I8, l, Convert(v2, currentMethod.AppDomain.System_Int64), currentMethod.AppDomain.System_Int64))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_R4:
					Pop2(out v1, out v2);
					v3 = Pop1();
					l = GetInt32OrNativeInt(v1);
					if (!v3.StoreSZArrayElement(LoadValueType.R4, l, Convert(v2, currentMethod.AppDomain.System_Single), currentMethod.AppDomain.System_Single))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_R8:
					Pop2(out v1, out v2);
					v3 = Pop1();
					l = GetInt32OrNativeInt(v1);
					if (!v3.StoreSZArrayElement(LoadValueType.R8, l, Convert(v2, currentMethod.AppDomain.System_Double), currentMethod.AppDomain.System_Double))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stelem_Ref:
					Pop2(out v1, out v2);
					v3 = Pop1();
					l = GetInt32OrNativeInt(v1);
					if (!v3.StoreSZArrayElement(LoadValueType.Ref, l, Convert(v2, currentMethod.AppDomain.System_Object), currentMethod.AppDomain.System_Object))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Ldfld:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					v1 = Pop1();
					v1 = v1.LoadField(field);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldflda:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					v1 = Pop1();
					v1 = v1.LoadFieldAddress(field);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldsfld:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (!field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					v1 = debuggerRuntime.LoadStaticField(field);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldsflda:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (!field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					v1 = debuggerRuntime.LoadStaticFieldAddress(field);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Stfld:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					Pop2(out v1, out v2);
					if (!v1.StoreField(field, Convert(v2, field.FieldType)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stsfld:
					field = currentMethod.Module.ResolveField(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					if (!field.IsStatic)
						ThrowInvalidMethodBodyInterpreterException();
					if (!debuggerRuntime.StoreStaticField(field, Convert(Pop1(), field.FieldType)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Ldind_I:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_IntPtr, LoadValueType.I);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_I1:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_SByte, LoadValueType.I1);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_I2:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_Int16, LoadValueType.I2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_I4:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_Int32, LoadValueType.I4);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_I8:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_Int64, LoadValueType.I8);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_R4:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_Single, LoadValueType.R4);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_R8:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_Double, LoadValueType.R8);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_Ref:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_Object, LoadValueType.Ref);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_U1:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_Byte, LoadValueType.U1);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_U2:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_UInt16, LoadValueType.U2);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Ldind_U4:
					v2 = Pop1();
					v1 = v2.LoadIndirect(currentMethod.AppDomain.System_UInt32, LoadValueType.U4);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Stind_I:
					Pop2(out v1, out v2);
					if (!v1.StoreIndirect(currentMethod.AppDomain.System_IntPtr, LoadValueType.I, Convert(v2, currentMethod.AppDomain.System_IntPtr)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_I1:
					Pop2(out v1, out v2);
					if (!v1.StoreIndirect(currentMethod.AppDomain.System_SByte, LoadValueType.I1, Convert(v2, currentMethod.AppDomain.System_SByte)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_I2:
					Pop2(out v1, out v2);
					if (!v1.StoreIndirect(currentMethod.AppDomain.System_Int16, LoadValueType.I2, Convert(v2, currentMethod.AppDomain.System_Int16)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_I4:
					Pop2(out v1, out v2);
					if (!v1.StoreIndirect(currentMethod.AppDomain.System_Int32, LoadValueType.I4, Convert(v2, currentMethod.AppDomain.System_Int32)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_I8:
					Pop2(out v1, out v2);
					if (!v1.StoreIndirect(currentMethod.AppDomain.System_Int64, LoadValueType.I8, Convert(v2, currentMethod.AppDomain.System_Int64)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_R4:
					Pop2(out v1, out v2);
					if (!v1.StoreIndirect(currentMethod.AppDomain.System_Single, LoadValueType.R4, Convert(v2, currentMethod.AppDomain.System_Single)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_R8:
					Pop2(out v1, out v2);
					if (!v1.StoreIndirect(currentMethod.AppDomain.System_Double, LoadValueType.R8, Convert(v2, currentMethod.AppDomain.System_Double)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Stind_Ref:
					Pop2(out v1, out v2);
					if (!v1.StoreIndirect(currentMethod.AppDomain.System_Object, LoadValueType.Ref, Convert(v2, currentMethod.AppDomain.System_Object)))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Ldobj:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = Pop1();
					v1 = v1.LoadIndirect(type, GetLoadValueType(type));
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1.Clone());
					break;

				case OpCode.Stobj:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					Pop2(out v1, out v2);
					if (!v1.StoreIndirect(type, GetLoadValueType(type), Convert(v2, type)))
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
					v1 = Pop1();
					v1 = v1.Box(type) ?? debuggerRuntime.Box(v1, type);
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
					if (methodSig.HasThis) {
						v1 = Pop1();
						if ((object)constrainedType != null) {
							if (i != (int)OpCode.Callvirt)
								ThrowInvalidMethodBodyInterpreterException();
							v1 = FixConstrainedType(constrainedType, method, v1);
							if (v1 == null)
								ThrowInvalidMethodBodyInterpreterException();
						}
						if (!v1.Call(i == (int)OpCode.Callvirt, method, args, out v3))
							ThrowInvalidMethodBodyInterpreterException();
					}
					else {
						if (!debuggerRuntime.CallStatic(method, args, out v3))
							ThrowInvalidMethodBodyInterpreterException();
					}
					if (methodSig.ReturnType != currentMethod.AppDomain.System_Void)
						ilValueStack.Add(Convert(v3.Clone(), methodSig.ReturnType));
					break;

				case OpCode.Calli:
					methodSig = currentMethod.Module.ResolveMethodSignature(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v2 = Pop1();
					args = PopMethodArguments(methodSig);
					if (methodSig.HasThis) {
						if (methodSig.ExplicitThis) {
							if (args.Length == 0)
								ThrowInvalidMethodBodyInterpreterException();
							v1 = args[0];
							args = Skip1(args);
						}
						else
							v1 = Pop1();
						if (!v1.CallIndirect(methodSig, v2, args, out v1))
							ThrowInvalidMethodBodyInterpreterException();
					}
					else {
						if (!debuggerRuntime.CallStaticIndirect(methodSig, v2, args, out v1))
							ThrowInvalidMethodBodyInterpreterException();
					}
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
					if (!v1.IsNull && !(v1.Kind == ILValueKind.Type && type.IsAssignableFrom(v1.Type)))
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Isinst:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = Pop1();
					ilValueStack.Add(!v1.IsNull && type.IsAssignableFrom(v1.Type) ? v1 : new NullObjectRefILValue());
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
					methodSig = method.GetMethodSignature();
					if (!(method is DmdConstructorInfo) || !methodSig.HasThis || methodSig.ReturnType != currentMethod.DeclaringType.AppDomain.System_Void)
						ThrowInvalidMethodBodyInterpreterException();
					args = PopMethodArguments(methodSig);
					v1 = debuggerRuntime.CreateInstance((DmdConstructorInfo)method, args);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Unbox:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = Pop1();
					if (v1.IsNull || !type.IsValueType)
						ThrowInvalidMethodBodyInterpreterException();
					v1 = v1.Unbox(type);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Unbox_Any:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					v1 = Pop1();
					if (v1.IsNull)
						v1 = type.IsNullable ? debuggerRuntime.CreateTypeNoConstructor(type) : v1;
					else
						v1 = v1.UnboxAny(type) ?? v1;
					ilValueStack.Add(v1);
					break;

				case OpCode.Cpobj:
					type = currentMethod.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
					Pop2(out v1, out v2);
					if (!v1.CopyObject(type, v2))
						ThrowInvalidMethodBodyInterpreterException();
					break;

				case OpCode.Add:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value + ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if ((v3 = v2.Add(AddOpCodeKind.Add, ((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value + ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value + ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if ((v3 = v2.Add(AddOpCodeKind.Add, ((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							throw new InvalidMethodBodyInterpreterException();

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value + ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(currentMethod.AppDomain, ((ConstantFloatILValue)v1).Value + ((ConstantFloatILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Add(AddOpCodeKind.Add, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 + ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 + ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Add(AddOpCodeKind.Add, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && (v3 = v2.Add(AddOpCodeKind.Add, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 + ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 + ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if (v1 is ConstantNativeIntILValue && (v3 = v2.Add(AddOpCodeKind.Add, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							throw new InvalidMethodBodyInterpreterException();

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Add(AddOpCodeKind.Add, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Add(AddOpCodeKind.Add, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value + ((ConstantInt32ILValue)v2).Value));
							break;

						case ILValueKind.NativeInt:
							if ((v3 = v2.Add(AddOpCodeKind.Add_Ovf, ((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value + ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value + ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if ((v3 = v2.Add(AddOpCodeKind.Add_Ovf, ((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							throw new InvalidMethodBodyInterpreterException();

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, checked(((ConstantInt64ILValue)v1).Value + ((ConstantInt64ILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(currentMethod.AppDomain, checked(((ConstantFloatILValue)v1).Value + ((ConstantFloatILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Add(AddOpCodeKind.Add_Ovf, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value32 + ((ConstantInt32ILValue)v2).Value));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value64 + ((ConstantInt32ILValue)v2).Value));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Add(AddOpCodeKind.Add_Ovf, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && (v3 = v2.Add(AddOpCodeKind.Add_Ovf, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value32 + ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value64 + ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if (v1 is ConstantNativeIntILValue && (v3 = v2.Add(AddOpCodeKind.Add_Ovf, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							throw new InvalidMethodBodyInterpreterException();

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Add(AddOpCodeKind.Add_Ovf, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Add(AddOpCodeKind.Add_Ovf, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, (int)checked(((ConstantInt32ILValue)v1).UnsignedValue + ((ConstantInt32ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.NativeInt:
							if ((v3 = v2.Add(AddOpCodeKind.Add_Ovf_Un, ((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)checked(((ConstantInt32ILValue)v1).UnsignedValue + ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)checked(((ConstantInt32ILValue)v1).UnsignedValue + ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if ((v3 = v2.Add(AddOpCodeKind.Add_Ovf_Un, ((ConstantInt32ILValue)v1).Value, debuggerRuntime.PointerSize)) != null)
								break;
							throw new InvalidMethodBodyInterpreterException();

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, (long)checked(((ConstantInt64ILValue)v1).UnsignedValue + ((ConstantInt64ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Add(AddOpCodeKind.Add_Ovf_Un, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 + ((ConstantInt32ILValue)v2).UnsignedValue));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 + ((ConstantInt32ILValue)v2).UnsignedValue));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Add(AddOpCodeKind.Add_Ovf_Un, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && (v3 = v2.Add(AddOpCodeKind.Add_Ovf_Un, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 + ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 + ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.ByRef:
							if (v1 is ConstantNativeIntILValue && (v3 = v2.Add(AddOpCodeKind.Add_Ovf_Un, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v1).Value32 : ((ConstantNativeIntILValue)v1).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							throw new InvalidMethodBodyInterpreterException();

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Add(AddOpCodeKind.Add_Ovf_Un, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Add(AddOpCodeKind.Add_Ovf_Un, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Sub:
					Pop2(out v1, out v2);
					if ((v3 = v1.Sub(SubOpCodeKind.Sub, v2, debuggerRuntime.PointerSize)) != null) {
						ilValueStack.Add(v3);
						break;
					}
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value - ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value - ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value - ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								throw new InvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.ByRef:
						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value - ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(currentMethod.AppDomain, ((ConstantFloatILValue)v1).Value - ((ConstantFloatILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Sub(SubOpCodeKind.Sub, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 - ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 - ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Sub(SubOpCodeKind.Sub, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 - ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 - ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Sub(SubOpCodeKind.Sub, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Sub(SubOpCodeKind.Sub, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Sub_Ovf:
					Pop2(out v1, out v2);
					if ((v3 = v1.Sub(SubOpCodeKind.Sub_Ovf, v2, debuggerRuntime.PointerSize)) != null) {
						ilValueStack.Add(v3);
						break;
					}
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value - ((ConstantInt32ILValue)v2).Value));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value - ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value - ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								throw new InvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.ByRef:
						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, checked(((ConstantInt64ILValue)v1).Value - ((ConstantInt64ILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(currentMethod.AppDomain, checked(((ConstantFloatILValue)v1).Value - ((ConstantFloatILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Sub(SubOpCodeKind.Sub_Ovf, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value32 - ((ConstantInt32ILValue)v2).Value));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value64 - ((ConstantInt32ILValue)v2).Value));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Sub(SubOpCodeKind.Sub_Ovf, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value32 - ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value64 - ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Sub(SubOpCodeKind.Sub_Ovf, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Sub(SubOpCodeKind.Sub_Ovf, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Sub_Ovf_Un:
					Pop2(out v1, out v2);
					if ((v3 = v1.Sub(SubOpCodeKind.Sub_Ovf_Un, v2, debuggerRuntime.PointerSize)) != null) {
						ilValueStack.Add(v3);
						break;
					}
					switch (v1.Kind) {
					case ILValueKind.Int32:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, (int)checked(((ConstantInt32ILValue)v1).UnsignedValue - ((ConstantInt32ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)checked(((ConstantInt32ILValue)v1).UnsignedValue - ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)checked(((ConstantInt32ILValue)v1).UnsignedValue - ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								throw new InvalidMethodBodyInterpreterException();
							break;

						case ILValueKind.ByRef:
						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, (long)checked(((ConstantInt64ILValue)v1).UnsignedValue - ((ConstantInt64ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Sub(SubOpCodeKind.Sub_Ovf_Un, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 - ((ConstantInt32ILValue)v2).UnsignedValue));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 - ((ConstantInt32ILValue)v2).UnsignedValue));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Sub(SubOpCodeKind.Sub_Ovf_Un, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							else if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 - ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 - ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if ((v3 = v1.Sub(SubOpCodeKind.Sub_Ovf_Un, ((ConstantInt32ILValue)v2).Value, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue && (v3 = v1.Sub(SubOpCodeKind.Sub_Ovf_Un, debuggerRuntime.PointerSize == 4 ? ((ConstantNativeIntILValue)v2).Value32 : ((ConstantNativeIntILValue)v2).Value64, debuggerRuntime.PointerSize)) != null)
								break;
							goto case ILValueKind.ByRef;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value * ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value * ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value * ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value * ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(currentMethod.AppDomain, ((ConstantFloatILValue)v1).Value * ((ConstantFloatILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 * ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 * ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 * ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 * ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value * ((ConstantInt32ILValue)v2).Value));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value * ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value * ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, checked(((ConstantInt64ILValue)v1).Value * ((ConstantInt64ILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(currentMethod.AppDomain, checked(((ConstantFloatILValue)v1).Value * ((ConstantFloatILValue)v2).Value));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value32 * ((ConstantInt32ILValue)v2).Value));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value64 * ((ConstantInt32ILValue)v2).Value));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value32 * ((ConstantNativeIntILValue)v2).Value32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantNativeIntILValue)v1).Value64 * ((ConstantNativeIntILValue)v2).Value64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, (int)checked(((ConstantInt32ILValue)v1).UnsignedValue * ((ConstantInt32ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)checked(((ConstantInt32ILValue)v1).UnsignedValue * ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)checked(((ConstantInt32ILValue)v1).UnsignedValue * ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, (long)checked(((ConstantInt64ILValue)v1).UnsignedValue * ((ConstantInt64ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
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
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 * ((ConstantInt32ILValue)v2).UnsignedValue));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 * ((ConstantInt32ILValue)v2).UnsignedValue));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)checked(((ConstantNativeIntILValue)v1).UnsignedValue32 * ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)checked(((ConstantNativeIntILValue)v1).UnsignedValue64 * ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value / ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value / ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value / ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value / ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(currentMethod.AppDomain, ((ConstantFloatILValue)v1).Value / ((ConstantFloatILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 / ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 / ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 / ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 / ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, (int)(((ConstantInt32ILValue)v1).UnsignedValue / ((ConstantInt32ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)(((ConstantInt32ILValue)v1).UnsignedValue / ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)(((ConstantInt32ILValue)v1).UnsignedValue64 / ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, (long)(((ConstantInt64ILValue)v1).UnsignedValue / ((ConstantInt64ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
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
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)(((ConstantNativeIntILValue)v1).UnsignedValue32 / ((ConstantInt32ILValue)v2).UnsignedValue));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)(((ConstantNativeIntILValue)v1).UnsignedValue64 / ((ConstantInt32ILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)(((ConstantNativeIntILValue)v1).UnsignedValue32 / ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)(((ConstantNativeIntILValue)v1).UnsignedValue64 / ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value % ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value % ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value % ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value % ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
						switch (v2.Kind) {
						case ILValueKind.Float:
							v3 = new ConstantFloatILValue(currentMethod.AppDomain, ((ConstantFloatILValue)v1).Value % ((ConstantFloatILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.Int64:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 % ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 % ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 % ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 % ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, (int)(((ConstantInt32ILValue)v1).UnsignedValue % ((ConstantInt32ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)(((ConstantInt32ILValue)v1).UnsignedValue % ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)(((ConstantInt32ILValue)v1).UnsignedValue64 % ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, (long)(((ConstantInt64ILValue)v1).UnsignedValue % ((ConstantInt64ILValue)v2).UnsignedValue));
							break;

						case ILValueKind.Int32:
						case ILValueKind.Float:
						case ILValueKind.NativeInt:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
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
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)(((ConstantNativeIntILValue)v1).UnsignedValue32 % ((ConstantInt32ILValue)v2).UnsignedValue));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)(((ConstantNativeIntILValue)v1).UnsignedValue64 % ((ConstantInt32ILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)(((ConstantNativeIntILValue)v1).UnsignedValue32 % ((ConstantNativeIntILValue)v2).UnsignedValue32));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)(((ConstantNativeIntILValue)v1).UnsignedValue64 % ((ConstantNativeIntILValue)v2).UnsignedValue64));
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Neg:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain, -((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain, -((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = new ConstantFloatILValue(currentMethod.AppDomain, -((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, -((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, -((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Shl:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value << (int)GetInt32OrNativeInt(v2));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value << (int)GetInt32OrNativeInt(v2));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 << (int)GetInt32OrNativeInt(v2));
							else
								v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 << (int)GetInt32OrNativeInt(v2));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Shr:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value >> (int)GetInt32OrNativeInt(v2));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value >> (int)GetInt32OrNativeInt(v2));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 >> (int)GetInt32OrNativeInt(v2));
							else
								v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 >> (int)GetInt32OrNativeInt(v2));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Shr_Un:
					Pop2(out v1, out v2);
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain, (int)(((ConstantInt32ILValue)v1).UnsignedValue >> (int)GetInt32OrNativeInt(v2)));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain, (long)(((ConstantInt64ILValue)v1).UnsignedValue >> (int)GetInt32OrNativeInt(v2)));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)(((ConstantNativeIntILValue)v1).UnsignedValue32 >> (int)GetInt32OrNativeInt(v2)));
							else
								v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)(((ConstantNativeIntILValue)v1).UnsignedValue64 >> (int)GetInt32OrNativeInt(v2)));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value & ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value & ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value & ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value & ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.NativeInt:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 & ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 & ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 & ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 & ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value | ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value | ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)((ConstantInt32ILValue)v1).Value | ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value | ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.NativeInt:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (((ConstantNativeIntILValue)v1).Value32 | ((ConstantInt32ILValue)v2).Value));
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 | (long)((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 | ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 | ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
							v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value ^ ((ConstantInt32ILValue)v2).Value);
							break;

						case ILValueKind.NativeInt:
							if (v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value ^ ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value ^ ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Int64:
						switch (v2.Kind) {
						case ILValueKind.Int64:
							v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value ^ ((ConstantInt64ILValue)v2).Value);
							break;

						case ILValueKind.Int32:
						case ILValueKind.NativeInt:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.NativeInt:
						switch (v2.Kind) {
						case ILValueKind.Int32:
							if (v1 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 ^ ((ConstantInt32ILValue)v2).Value);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 ^ ((ConstantInt32ILValue)v2).Value);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.NativeInt:
							if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
								if (debuggerRuntime.PointerSize == 4)
									v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value32 ^ ((ConstantNativeIntILValue)v2).Value32);
								else
									v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantNativeIntILValue)v1).Value64 ^ ((ConstantNativeIntILValue)v2).Value64);
							}
							else
								goto case ILValueKind.ByRef;
							break;

						case ILValueKind.Int64:
						case ILValueKind.Float:
						case ILValueKind.ByRef:
						case ILValueKind.Type:
						default:
							throw new InvalidMethodBodyInterpreterException();
						}
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Not:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain, ~((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain, ~((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ~((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ~((ConstantNativeIntILValue)v1).Value64);
						}
						else
							throw new InvalidMethodBodyInterpreterException();
						break;

					case ILValueKind.Float:
					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)((ConstantInt64ILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, ((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, (int)((ConstantFloatILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, (long)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						v3 = v1;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
						v3 = v1.Conv(ConvOpCodeKind.Conv_I) ?? v1;
						break;

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
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked((int)((ConstantInt64ILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked(((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked((int)((ConstantFloatILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked((long)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						v3 = v1;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
						v3 = v1.Conv(ConvOpCodeKind.Conv_Ovf_I) ?? v1;
						break;

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
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain.System_UIntPtr, (int)(uint)((ConstantInt32ILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain.System_UIntPtr, (long)(uint)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain.System_UIntPtr, (int)(uint)((ConstantInt64ILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain.System_UIntPtr, (long)(ulong)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain.System_UIntPtr, (int)(uint)((ConstantFloatILValue)v1).Value);
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain.System_UIntPtr, (long)(ulong)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						v3 = v1;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
						v3 = v1.Conv(ConvOpCodeKind.Conv_U) ?? v1;
						break;

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
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain.System_UIntPtr, (int)checked((uint)((ConstantInt32ILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain.System_UIntPtr, (long)checked((ulong)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain.System_UIntPtr, (int)checked((uint)((ConstantInt64ILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain.System_UIntPtr, (long)checked((ulong)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain.System_UIntPtr, (int)checked((uint)((ConstantFloatILValue)v1).Value));
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain.System_UIntPtr, (long)checked((ulong)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain.System_UIntPtr, (int)checked((uint)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain.System_UIntPtr, (long)checked((ulong)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
						v3 = v1.Conv(ConvOpCodeKind.Conv_Ovf_U) ?? v1;
						break;

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
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked((int)((ConstantInt32ILValue)v1).UnsignedValue));
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked((long)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked((int)((ConstantInt64ILValue)v1).UnsignedValue));
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked((long)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain, checked((int)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain, checked((long)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
						v3 = v1.Conv(ConvOpCodeKind.Conv_Ovf_I_Un) ?? v1;
						break;

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
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain.System_UIntPtr, (int)checked((uint)((ConstantInt32ILValue)v1).UnsignedValue));
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain.System_UIntPtr, (long)checked((ulong)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						if (debuggerRuntime.PointerSize == 4)
							v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain.System_UIntPtr, (int)checked((uint)((ConstantInt64ILValue)v1).UnsignedValue));
						else
							v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain.System_UIntPtr, (long)checked((ulong)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = ConstantNativeIntILValue.Create32(currentMethod.AppDomain.System_UIntPtr, (int)checked((uint)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = ConstantNativeIntILValue.Create64(currentMethod.AppDomain.System_UIntPtr, (long)checked((ulong)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
						v3 = v1.Conv(ConvOpCodeKind.Conv_Ovf_U_Un) ?? v1;
						break;

					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_I1:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, (sbyte)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, (sbyte)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, (sbyte)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, (sbyte)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, (sbyte)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I1:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, checked((sbyte)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, checked((sbyte)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, checked((sbyte)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, checked((sbyte)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, checked((sbyte)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I1_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, checked((sbyte)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, checked((sbyte)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, checked((sbyte)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_SByte, checked((sbyte)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_I2:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, (short)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, (short)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, (short)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, (short)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, (short)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I2:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, checked((short)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, checked((short)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, checked((short)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, checked((short)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, checked((short)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I2_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, checked((short)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, checked((short)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, checked((short)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int16, checked((short)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, (int)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, (int)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, (int)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, (int)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, checked((int)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, checked((int)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, checked((int)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, checked((int)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I4_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, checked((int)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, checked((int)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, checked((int)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Int32, checked((int)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_I8:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, ((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = v1;
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, (long)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, ((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, ((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I8:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, checked(((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = v1;
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, checked((long)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, checked(((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, checked(((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_I8_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, checked((long)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, checked((long)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, checked((long)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_Int64, checked((long)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_U1:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, (byte)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, (byte)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, (byte)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, (byte)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, (byte)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U1:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, checked((byte)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, checked((byte)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, checked((byte)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, checked((byte)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, checked((byte)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U1_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, checked((byte)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, checked((byte)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, checked((byte)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_Byte, checked((byte)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_U2:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, (ushort)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, (ushort)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, (ushort)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, (ushort)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, (ushort)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U2:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, checked((ushort)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, checked((ushort)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, checked((ushort)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, checked((ushort)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, checked((ushort)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U2_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, checked((ushort)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, checked((ushort)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, checked((ushort)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt16, checked((ushort)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
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
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)(uint)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)(uint)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)(uint)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)(uint)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U4:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)checked((uint)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)checked((uint)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)checked((uint)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)checked((uint)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)checked((uint)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U4_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)checked((uint)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)checked((uint)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)checked((uint)((ConstantNativeIntILValue)v1).UnsignedValue32));
							else
								v3 = new ConstantInt32ILValue(currentMethod.AppDomain.System_UInt32, (int)checked((uint)((ConstantNativeIntILValue)v1).UnsignedValue64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_U8:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)(ulong)(uint)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = v1;
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)(ulong)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)(uint)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)(ulong)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U8:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)checked((ulong)((ConstantInt32ILValue)v1).Value));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)checked((ulong)((ConstantInt64ILValue)v1).Value));
						break;

					case ILValueKind.Float:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)checked((ulong)((ConstantFloatILValue)v1).Value));
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)checked((ulong)((ConstantNativeIntILValue)v1).Value32));
							else
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)checked((ulong)((ConstantNativeIntILValue)v1).Value64));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_Ovf_U8_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)checked((ulong)((ConstantInt32ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Int64:
						v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)checked((ulong)((ConstantInt64ILValue)v1).UnsignedValue));
						break;

					case ILValueKind.Float:
						throw new InvalidMethodBodyInterpreterException();

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)checked((ulong)(((ConstantNativeIntILValue)v1).UnsignedValue32)));
							else
								v3 = new ConstantInt64ILValue(currentMethod.AppDomain.System_UInt64, (long)checked((ulong)(((ConstantNativeIntILValue)v1).UnsignedValue64)));
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_R4:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Single, (float)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Single, (float)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Single, (float)((ConstantFloatILValue)v1).Value);
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Single, (float)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Single, (float)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_R8:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Double, (double)((ConstantInt32ILValue)v1).Value);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Double, (double)((ConstantInt64ILValue)v1).Value);
						break;

					case ILValueKind.Float:
						v3 = v1;
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Double, (double)((ConstantNativeIntILValue)v1).Value32);
							else
								v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Double, (double)((ConstantNativeIntILValue)v1).Value64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Conv_R_Un:
					v1 = Pop1();
					switch (v1.Kind) {
					case ILValueKind.Int32:
						v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Double, (double)((ConstantInt32ILValue)v1).UnsignedValue);
						break;

					case ILValueKind.Int64:
						v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Double, (double)((ConstantInt64ILValue)v1).UnsignedValue);
						break;

					case ILValueKind.Float:
						v3 = v1;
						break;

					case ILValueKind.NativeInt:
						if (v1 is ConstantNativeIntILValue) {
							if (debuggerRuntime.PointerSize == 4)
								v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Double, (double)((ConstantNativeIntILValue)v1).UnsignedValue32);
							else
								v3 = new ConstantFloatILValue(currentMethod.AppDomain.System_Double, (double)((ConstantNativeIntILValue)v1).UnsignedValue64);
						}
						else
							goto case ILValueKind.ByRef;
						break;

					case ILValueKind.ByRef:
					case ILValueKind.Type:
					default:
						throw new InvalidMethodBodyInterpreterException();
					}
					ilValueStack.Add(v3);
					break;

				case OpCode.Beq:
				case OpCode.Beq_S:
					i = (i != (int)OpCode.Beq ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareEquals(v1, v2))
						methodBodyPos = i;
					break;

				case OpCode.Bge:
				case OpCode.Bge_S:
					i = (i != (int)OpCode.Bge ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareSigned(v1, v2) >= 0)
						methodBodyPos = i;
					break;

				case OpCode.Bge_Un:
				case OpCode.Bge_Un_S:
					i = (i != (int)OpCode.Bge_Un ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareUnsigned(v1, v2) >= 0)
						methodBodyPos = i;
					break;

				case OpCode.Bgt:
				case OpCode.Bgt_S:
					i = (i != (int)OpCode.Bgt ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareSigned(v1, v2) > 0)
						methodBodyPos = i;
					break;

				case OpCode.Bgt_Un:
				case OpCode.Bgt_Un_S:
					i = (i != (int)OpCode.Bgt_Un ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareUnsigned(v1, v2) > 0)
						methodBodyPos = i;
					break;

				case OpCode.Ble:
				case OpCode.Ble_S:
					i = (i != (int)OpCode.Ble ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareSigned(v1, v2) <= 0)
						methodBodyPos = i;
					break;

				case OpCode.Ble_Un:
				case OpCode.Ble_Un_S:
					i = (i != (int)OpCode.Ble_Un ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareUnsigned(v1, v2) <= 0)
						methodBodyPos = i;
					break;

				case OpCode.Blt:
				case OpCode.Blt_S:
					i = (i != (int)OpCode.Blt ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareSigned(v1, v2) < 0)
						methodBodyPos = i;
					break;

				case OpCode.Blt_Un:
				case OpCode.Blt_Un_S:
					i = (i != (int)OpCode.Blt_Un ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (CompareUnsigned(v1, v2) < 0)
						methodBodyPos = i;
					break;

				case OpCode.Bne_Un:
				case OpCode.Bne_Un_S:
					i = (i != (int)OpCode.Bne_Un ? (sbyte)bodyBytes[methodBodyPos++] : ToInt32(bodyBytes, ref methodBodyPos)) + methodBodyPos;
					Pop2(out v1, out v2);
					if (!CompareEquals(v1, v2))
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
						return new NullObjectRefILValue();
					}
					else {
						if (ilValueStack.Count != 1)
							ThrowInvalidMethodBodyInterpreterException();
						const bool IsLastEmulatedMethod = true;
						return Convert(Pop1(), currentMethod.GetMethodSignature().ReturnType, boxIfNeeded: !IsLastEmulatedMethod);
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

		enum ZeroResult {
			Unknown,
			Zero,
			NonZero,
		}

		ZeroResult IsIntegerZeroOrNull(ILValue v) {
			if (v.IsNull)
				return ZeroResult.Zero;
			switch (v.Kind) {
			case ILValueKind.Int32:
				return ((ConstantInt32ILValue)v).Value == 0 ? ZeroResult.Zero : ZeroResult.NonZero;

			case ILValueKind.Int64:
				return ((ConstantInt64ILValue)v).Value == 0 ? ZeroResult.Zero : ZeroResult.NonZero;

			case ILValueKind.Float:
				return ZeroResult.Unknown;

			case ILValueKind.NativeInt:
				if (v is ConstantNativeIntILValue) {
					if (debuggerRuntime.PointerSize == 4)
						return ((ConstantNativeIntILValue)v).Value32 == 0 ? ZeroResult.Zero : ZeroResult.NonZero;
					return ((ConstantNativeIntILValue)v).Value64 == 0 ? ZeroResult.Zero : ZeroResult.NonZero;
				}
				return ZeroResult.NonZero;

			case ILValueKind.ByRef:
				return ZeroResult.NonZero;

			case ILValueKind.Type:
				return ZeroResult.NonZero;

			default:
				throw new InvalidOperationException();
			}
		}

		bool CompareEquals(ILValue v1, ILValue v2) {
			if (v1 == v2)
				return true;

			var v1z = IsIntegerZeroOrNull(v1);
			var v2z = IsIntegerZeroOrNull(v2);
			if (v1z != v2z)
				return false;
			if (v1z == ZeroResult.Zero && v2z == ZeroResult.Zero)
				return true;

			switch (v1.Kind) {
			case ILValueKind.Int32:
				switch (v2.Kind) {
				case ILValueKind.Int32:
					return ((ConstantInt32ILValue)v1).Value.Equals(((ConstantInt32ILValue)v2).Value);

				case ILValueKind.NativeInt:
					if (v2 is ConstantNativeIntILValue) {
						if (debuggerRuntime.PointerSize == 4)
							return (((ConstantInt32ILValue)v1).Value).Equals(((ConstantNativeIntILValue)v2).Value32);
						return ((long)((ConstantInt32ILValue)v1).Value).Equals(((ConstantNativeIntILValue)v2).Value64);
					}
					goto case ILValueKind.ByRef;

				case ILValueKind.ByRef:
				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.Type:
				default:
					break;
				}
				break;

			case ILValueKind.Int64:
				switch (v2.Kind) {
				case ILValueKind.Int64:
					return ((ConstantInt64ILValue)v1).Value.Equals(((ConstantInt64ILValue)v2).Value);

				case ILValueKind.Int32:
				case ILValueKind.Float:
				case ILValueKind.NativeInt:
				case ILValueKind.ByRef:
				case ILValueKind.Type:
				default:
					break;
				}
				break;

			case ILValueKind.Float:
				switch (v2.Kind) {
				case ILValueKind.Float:
					return ((ConstantFloatILValue)v1).Value.Equals(((ConstantFloatILValue)v2).Value);

				case ILValueKind.Int32:
				case ILValueKind.Int64:
				case ILValueKind.NativeInt:
				case ILValueKind.ByRef:
				case ILValueKind.Type:
				default:
					break;
				}
				break;

			case ILValueKind.NativeInt:
				switch (v2.Kind) {
				case ILValueKind.Int32:
					if (v1 is ConstantNativeIntILValue) {
						if (debuggerRuntime.PointerSize == 4)
							return ((ConstantNativeIntILValue)v1).Value32.Equals(((ConstantInt32ILValue)v2).Value);
						return ((ConstantNativeIntILValue)v1).Value64.Equals(((ConstantInt32ILValue)v2).Value);
					}
					goto case ILValueKind.ByRef;

				case ILValueKind.NativeInt:
					if (v1 is ConstantNativeIntILValue && v2 is ConstantNativeIntILValue) {
						if (debuggerRuntime.PointerSize == 4)
							return ((ConstantNativeIntILValue)v1).Value32.Equals(((ConstantNativeIntILValue)v2).Value32);
						return ((ConstantNativeIntILValue)v1).Value64.Equals(((ConstantNativeIntILValue)v2).Value64);
					}
					goto case ILValueKind.ByRef;

				case ILValueKind.ByRef:
				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.Type:
				default:
					break;
				}
				break;

			case ILValueKind.ByRef:
			case ILValueKind.Type:
			default:
				break;
			}

			var res = debuggerRuntime.Equals(v1, v2);
			if (res != null)
				return res.Value;

			throw new InvalidMethodBodyInterpreterException();
		}

		int CompareSigned(ILValue v1, ILValue v2) {
			if (v1 == v2)
				return 0;

			var res = debuggerRuntime.CompareSigned(v1, v2);
			if (res != null)
				return res.Value;

			var v1z = IsIntegerZeroOrNull(v1);
			var v2z = IsIntegerZeroOrNull(v2);
			if (v1z == ZeroResult.Zero && v2z == ZeroResult.Zero)
				return 0;

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
				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.Type:
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
				case ILValueKind.Type:
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
				case ILValueKind.Type:
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
				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.Type:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.ByRef:
			case ILValueKind.Type:
			default:
				throw new InvalidMethodBodyInterpreterException();
			}
		}

		int CompareUnsigned(ILValue v1, ILValue v2) {
			if (v1 == v2)
				return 0;

			var res = debuggerRuntime.CompareUnsigned(v1, v2);
			if (res != null)
				return res.Value;

			var v1z = IsIntegerZeroOrNull(v1);
			var v2z = IsIntegerZeroOrNull(v2);
			if (v2z == ZeroResult.Zero) {
				if (v1z == ZeroResult.Zero)
					return 0;
				if (v1z == ZeroResult.NonZero)
					return 1;
			}
			if (v1z == ZeroResult.Zero) {
				if (v2z == ZeroResult.NonZero)
					return -1;
			}

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
				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.Type:
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
				case ILValueKind.Type:
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
				case ILValueKind.Type:
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
				case ILValueKind.Int64:
				case ILValueKind.Float:
				case ILValueKind.Type:
				default:
					throw new InvalidMethodBodyInterpreterException();
				}

			case ILValueKind.ByRef:
			case ILValueKind.Type:
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
			case ILValueKind.Type:
				return false;

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
			}
			if (type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr)
				return debuggerRuntime.PointerSize;

			return debuggerRuntime.GetSizeOfValueType(type);
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
				var value = v1.LoadIndirect(constrainedType, GetLoadValueType(constrainedType));
				if (value == null)
					return null;
				return value.Box(constrainedType) ?? debuggerRuntime.Box(value, constrainedType);
			}
			else
				return v1.LoadIndirect(constrainedType.AppDomain.System_Object, LoadValueType.Ref);
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

		static LoadValueType GetLoadValueType(DmdType type) {
			switch (DmdType.GetTypeCode(type)) {
			case TypeCode.Boolean:		return LoadValueType.U1;
			case TypeCode.Char:			return LoadValueType.U2;
			case TypeCode.SByte:		return LoadValueType.I1;
			case TypeCode.Byte:			return LoadValueType.U1;
			case TypeCode.Int16:		return LoadValueType.I2;
			case TypeCode.UInt16:		return LoadValueType.U2;
			case TypeCode.Int32:		return LoadValueType.I4;
			case TypeCode.UInt32:		return LoadValueType.U4;
			case TypeCode.Int64:		return LoadValueType.I8;
			case TypeCode.UInt64:		return LoadValueType.I8;
			case TypeCode.Single:		return LoadValueType.R4;
			case TypeCode.Double:		return LoadValueType.R8;
			default:
				if (type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr)
					return LoadValueType.I;
				return LoadValueType.Ref;
			}
		}

		static ILValue[] Skip1(ILValue[] a) {
			Debug.Assert(a.Length >= 1);
			if (a.Length == 1)
				return Array.Empty<ILValue>();
			var res = new ILValue[a.Length - 1];
			for (int i = 0; i < res.Length; i++)
				res[i] = a[i + 1];
			return res;
		}
	}
}
