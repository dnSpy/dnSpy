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
		const int MAX_INTERPRETED_INSTRUCTIONS = 5000;
		DebuggerRuntime debuggerRuntime;
		int totalInstructionCount;
		readonly List<ILValue> ilValueStack;

		public DebuggerILInterpreter() => ilValueStack = new List<ILValue>();

		public ILValue Execute(DebuggerRuntime debuggerRuntime, DmdMethodBase method) {
			try {
				Debug.Assert(ilValueStack.Count == 0);
				this.debuggerRuntime = debuggerRuntime;
				totalInstructionCount = 0;
				return ExecuteLoop(method);
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

		ILValue Pop1() {
			int index = ilValueStack.Count - 1;
			// ArgumentOutOfRangeException gets thrown if it underflows
			var value = ilValueStack[index];
			ilValueStack.RemoveAt(index);
			return value;
		}

		void Pop2(out ILValue a, out ILValue b) {
			int index = ilValueStack.Count - 1;
			// ArgumentOutOfRangeException gets thrown if it underflows
			b = ilValueStack[index];
			ilValueStack.RemoveAt(index--);
			a = ilValueStack[index];
			ilValueStack.RemoveAt(index);
		}

		void ThrowInvalidMethodBodyInterpreterException() => throw new InvalidMethodBodyInterpreterException();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ToUInt16(byte[] a, ref int pos) => a[pos++] | (a[pos++] << 8);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ToInt32(byte[] a, ref int pos) => a[pos++] | (a[pos++] << 8) | (a[pos++] << 16) | (a[pos++] << 24);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		long ToInt64(byte[] a, ref int pos) => (uint)(a[pos++] | (a[pos++] << 8) | (a[pos++] << 16) | (a[pos++] << 24)) | ((long)a[pos++] << 32) | ((long)a[pos++] << 40) | ((long)a[pos++] << 48) | ((long)a[pos++] << 56);

		ILValue ExecuteLoop(DmdMethodBase method) {
			var body = method.GetMethodBody();
			if (body == null)
				throw new InvalidMethodBodyInterpreterException();
			var bodyBytes = body.GetILAsByteArray();
			var exceptionHandlingClauses = body.ExceptionHandlingClauses;
			int methodBodyPos = 0;
			for (;;) {
				if (totalInstructionCount++ >= MAX_INTERPRETED_INSTRUCTIONS)
					throw new TooManyInstructionsInterpreterException();

				int i;
				ILValue v1;
				DmdType type;

				i = bodyBytes[methodBodyPos++];
				switch ((OpCode)i) {
				case OpCode.Prefix1:
					i = bodyBytes[methodBodyPos++];
					switch ((OpCodeFE)i) {
					case OpCodeFE.Ldarg:
						v1 = debuggerRuntime.GetArgument(ToUInt16(bodyBytes, ref methodBodyPos));
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1);
						break;

					case OpCodeFE.Ldarga:
						v1 = debuggerRuntime.GetArgumentAddress(ToUInt16(bodyBytes, ref methodBodyPos));
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1);
						break;

					case OpCodeFE.Ldloc:
						v1 = debuggerRuntime.GetLocal(ToUInt16(bodyBytes, ref methodBodyPos));
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1);
						break;

					case OpCodeFE.Ldloca:
						v1 = debuggerRuntime.GetLocalAddress(ToUInt16(bodyBytes, ref methodBodyPos));
						if (v1 == null)
							ThrowInvalidMethodBodyInterpreterException();
						ilValueStack.Add(v1);
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
						type = method.Module.ResolveType(ToInt32(bodyBytes, ref methodBodyPos), body.GenericTypeArguments, body.GenericMethodArguments, DmdResolveOptions.ThrowOnError);
						if (type.IsValueType)
							goto default;//TODO: Calculate value type size, it's probably PointerSize aligned
						else
							ilValueStack.Add(ILValueConstants.GetInt32Constant(method.AppDomain.Runtime.PointerSize));
						break;

					case OpCodeFE.Arglist:
					case OpCodeFE.Ceq:
					case OpCodeFE.Cgt:
					case OpCodeFE.Cgt_Un:
					case OpCodeFE.Clt:
					case OpCodeFE.Clt_Un:
					case OpCodeFE.Constrained:
					case OpCodeFE.Cpblk:
					case OpCodeFE.Endfilter:
					case OpCodeFE.Initblk:
					case OpCodeFE.Initobj:
					case OpCodeFE.Ldftn:
					case OpCodeFE.Ldvirtftn:
					case OpCodeFE.Localloc:
					case OpCodeFE.No:
					case OpCodeFE.Readonly:
					case OpCodeFE.Refanytype:
					case OpCodeFE.Rethrow:
					case OpCodeFE.Tailcall:
					case OpCodeFE.Unaligned:
					case OpCodeFE.Volatile:
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
					ilValueStack.Add(new ConstantStringILValue(method.Module.ResolveString(ToInt32(bodyBytes, ref methodBodyPos))));
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
					ilValueStack.Add(v1);
					break;

				case OpCode.Ldarg_S:
					v1 = debuggerRuntime.GetArgument(bodyBytes[methodBodyPos++]);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Ldarga_S:
					v1 = debuggerRuntime.GetArgumentAddress(bodyBytes[methodBodyPos++]);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Ldloc_0:
				case OpCode.Ldloc_1:
				case OpCode.Ldloc_2:
				case OpCode.Ldloc_3:
					v1 = debuggerRuntime.GetLocal(i - (int)OpCode.Ldloc_0);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Ldloc_S:
					v1 = debuggerRuntime.GetLocal(bodyBytes[methodBodyPos++]);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
					break;

				case OpCode.Ldloca_S:
					v1 = debuggerRuntime.GetLocalAddress(bodyBytes[methodBodyPos++]);
					if (v1 == null)
						ThrowInvalidMethodBodyInterpreterException();
					ilValueStack.Add(v1);
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

				case OpCode.Add:
				case OpCode.Add_Ovf:
				case OpCode.Add_Ovf_Un:
				case OpCode.And:
				case OpCode.Beq:
				case OpCode.Beq_S:
				case OpCode.Bge:
				case OpCode.Bge_S:
				case OpCode.Bge_Un:
				case OpCode.Bge_Un_S:
				case OpCode.Bgt:
				case OpCode.Bgt_S:
				case OpCode.Bgt_Un:
				case OpCode.Bgt_Un_S:
				case OpCode.Ble:
				case OpCode.Ble_S:
				case OpCode.Ble_Un:
				case OpCode.Ble_Un_S:
				case OpCode.Blt:
				case OpCode.Blt_S:
				case OpCode.Blt_Un:
				case OpCode.Blt_Un_S:
				case OpCode.Bne_Un:
				case OpCode.Bne_Un_S:
				case OpCode.Box:
				case OpCode.Br:
				case OpCode.Br_S:
				case OpCode.Break:
				case OpCode.Brfalse:
				case OpCode.Brfalse_S:
				case OpCode.Brtrue:
				case OpCode.Brtrue_S:
				case OpCode.Call:
				case OpCode.Calli:
				case OpCode.Callvirt:
				case OpCode.Castclass:
				case OpCode.Ckfinite:
				case OpCode.Conv_I:
				case OpCode.Conv_I1:
				case OpCode.Conv_I2:
				case OpCode.Conv_I4:
				case OpCode.Conv_I8:
				case OpCode.Conv_Ovf_I:
				case OpCode.Conv_Ovf_I_Un:
				case OpCode.Conv_Ovf_I1:
				case OpCode.Conv_Ovf_I1_Un:
				case OpCode.Conv_Ovf_I2:
				case OpCode.Conv_Ovf_I2_Un:
				case OpCode.Conv_Ovf_I4:
				case OpCode.Conv_Ovf_I4_Un:
				case OpCode.Conv_Ovf_I8:
				case OpCode.Conv_Ovf_I8_Un:
				case OpCode.Conv_Ovf_U:
				case OpCode.Conv_Ovf_U_Un:
				case OpCode.Conv_Ovf_U1:
				case OpCode.Conv_Ovf_U1_Un:
				case OpCode.Conv_Ovf_U2:
				case OpCode.Conv_Ovf_U2_Un:
				case OpCode.Conv_Ovf_U4:
				case OpCode.Conv_Ovf_U4_Un:
				case OpCode.Conv_Ovf_U8:
				case OpCode.Conv_Ovf_U8_Un:
				case OpCode.Conv_R_Un:
				case OpCode.Conv_R4:
				case OpCode.Conv_R8:
				case OpCode.Conv_U:
				case OpCode.Conv_U1:
				case OpCode.Conv_U2:
				case OpCode.Conv_U4:
				case OpCode.Conv_U8:
				case OpCode.Cpobj:
				case OpCode.Div:
				case OpCode.Div_Un:
				case OpCode.Dup:
				case OpCode.Endfinally:
				case OpCode.Isinst:
				case OpCode.Jmp:
				case OpCode.Ldelem:
				case OpCode.Ldelem_I:
				case OpCode.Ldelem_I1:
				case OpCode.Ldelem_I2:
				case OpCode.Ldelem_I4:
				case OpCode.Ldelem_I8:
				case OpCode.Ldelem_R4:
				case OpCode.Ldelem_R8:
				case OpCode.Ldelem_Ref:
				case OpCode.Ldelem_U1:
				case OpCode.Ldelem_U2:
				case OpCode.Ldelem_U4:
				case OpCode.Ldelema:
				case OpCode.Ldfld:
				case OpCode.Ldflda:
				case OpCode.Ldind_I:
				case OpCode.Ldind_I1:
				case OpCode.Ldind_I2:
				case OpCode.Ldind_I4:
				case OpCode.Ldind_I8:
				case OpCode.Ldind_R4:
				case OpCode.Ldind_R8:
				case OpCode.Ldind_Ref:
				case OpCode.Ldind_U1:
				case OpCode.Ldind_U2:
				case OpCode.Ldind_U4:
				case OpCode.Ldlen:
				case OpCode.Ldobj:
				case OpCode.Ldsfld:
				case OpCode.Ldsflda:
				case OpCode.Ldtoken:
				case OpCode.Leave:
				case OpCode.Leave_S:
				case OpCode.Mkrefany:
				case OpCode.Mul:
				case OpCode.Mul_Ovf:
				case OpCode.Mul_Ovf_Un:
				case OpCode.Neg:
				case OpCode.Newarr:
				case OpCode.Newobj:
				case OpCode.Nop:
				case OpCode.Not:
				case OpCode.Or:
				case OpCode.Pop:
				case OpCode.Prefix2:
				case OpCode.Prefix3:
				case OpCode.Prefix4:
				case OpCode.Prefix5:
				case OpCode.Prefix6:
				case OpCode.Prefix7:
				case OpCode.Prefixref:
				case OpCode.Refanyval:
				case OpCode.Rem:
				case OpCode.Rem_Un:
				case OpCode.Ret:
				case OpCode.Shl:
				case OpCode.Shr:
				case OpCode.Shr_Un:
				case OpCode.Stelem:
				case OpCode.Stelem_I:
				case OpCode.Stelem_I1:
				case OpCode.Stelem_I2:
				case OpCode.Stelem_I4:
				case OpCode.Stelem_I8:
				case OpCode.Stelem_R4:
				case OpCode.Stelem_R8:
				case OpCode.Stelem_Ref:
				case OpCode.Stfld:
				case OpCode.Stind_I:
				case OpCode.Stind_I1:
				case OpCode.Stind_I2:
				case OpCode.Stind_I4:
				case OpCode.Stind_I8:
				case OpCode.Stind_R4:
				case OpCode.Stind_R8:
				case OpCode.Stind_Ref:
				case OpCode.Stobj:
				case OpCode.Stsfld:
				case OpCode.Sub:
				case OpCode.Sub_Ovf:
				case OpCode.Sub_Ovf_Un:
				case OpCode.Switch:
				case OpCode.Throw:
				case OpCode.Unbox:
				case OpCode.Unbox_Any:
				case OpCode.Xor:
				default:
					throw new InstructionNotSupportedInterpreterException("Unsupported IL opcode 0x" + i.ToString("X2"));
				}
			}
		}
	}
}
