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
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SR = System.Reflection;
using SRE = System.Reflection.Emit;

namespace dnSpy.Roslyn.Debugger.FilterExpressionEvaluator {
	delegate bool EvalDelegate(string machineName, int processId, string processName, ulong threadId, string threadName);

	[Serializable]
	sealed class EvalDelegateCreatorException : Exception { }

	readonly struct EvalDelegateCreator : IDisposable {
		readonly ModuleDefMD module;
		readonly string evalClassName;
		readonly string evalMethodName;

		static EvalDelegateCreator() {
			toReflectionOpCode = new Dictionary<OpCode, SRE.OpCode>(0x200);
			foreach (var info in typeof(SRE.OpCodes).GetFields(SR.BindingFlags.Public | SR.BindingFlags.Static)) {
				var sreOpCode = (SRE.OpCode)info.GetValue(null);
				int value = (ushort)sreOpCode.Value;
				OpCode opCode;
				switch (value >> 8) {
				case 0:
					opCode = OpCodes.OneByteOpCodes[value & 0xFF];
					Debug.Assert(opCode != null);
					if (opCode != null)
						toReflectionOpCode[opCode] = sreOpCode;
					break;

				case 0xFE:
					opCode = OpCodes.TwoByteOpCodes[value & 0xFF];
					Debug.Assert(opCode != null);
					if (opCode != null)
						toReflectionOpCode[opCode] = sreOpCode;
					break;

				default:
					Debug.Fail("!");
					break;
				}
			}
		}
		static readonly Dictionary<OpCode, SRE.OpCode> toReflectionOpCode;

		public EvalDelegateCreator(byte[] assemblyBytes, string evalClassName, string evalMethodName) {
			module = ModuleDefMD.Load(assemblyBytes);
			this.evalClassName = evalClassName;
			this.evalMethodName = evalMethodName;
		}

		static readonly Type[] evalDelegateParamTypes = new Type[] { typeof(string), typeof(int), typeof(string), typeof(ulong), typeof(string) };
		public EvalDelegate CreateDelegate() {
			var type = module.Find(evalClassName, isReflectionName: true);
			Debug.Assert(type != null);
			var method = type?.FindMethod(evalMethodName);
			Debug.Assert(method?.Body != null);
			if (!(method?.Body is CilBody body))
				return null;
			if (method.ReturnType.ElementType != ElementType.Boolean)
				return null;
			if (body.HasExceptionHandlers)
				return null;

			Debug.Assert(method.MethodSig.Params.Count == evalDelegateParamTypes.Length);
			if (method.MethodSig.Params.Count != evalDelegateParamTypes.Length)
				return null;
			var dm = new SRE.DynamicMethod("compiled filter expr", typeof(bool), evalDelegateParamTypes, typeof(FilterExpressionMethods), skipVisibility: false);
			var ilg = dm.GetILGenerator();

			var labelsDict = new Dictionary<Instruction, SRE.Label>();
			var instrs = body.Instructions;
			for (int i = 0; i < instrs.Count; i++) {
				var instr = instrs[i];
				switch (instr.Operand) {
				case Instruction targetInstr:
					if (!labelsDict.ContainsKey(targetInstr))
						labelsDict.Add(targetInstr, ilg.DefineLabel());
					break;

				case IList<Instruction> targetInstrs:
					foreach (var targetInstr in targetInstrs) {
						if (!labelsDict.ContainsKey(targetInstr))
							labelsDict.Add(targetInstr, ilg.DefineLabel());
					}
					break;
				}
			}

			Dictionary<Local, SRE.LocalBuilder> localsDict = null;
			if (body.Variables.Count > 0) {
				localsDict = new Dictionary<Local, SRE.LocalBuilder>(body.Variables.Count);
				foreach (var local in body.Variables) {
					var lb = ilg.DeclareLocal(Import(local.Type), local.Type.IsPinned);
					if (local.Index != lb.LocalIndex)
						return null;
					localsDict.Add(local, lb);
				}
			}

			foreach (var instr in body.Instructions) {
				if (labelsDict.TryGetValue(instr, out var label))
					ilg.MarkLabel(label);
				if (!toReflectionOpCode.TryGetValue(instr.OpCode, out var sreOpCode))
					return null;
				switch (instr.OpCode.OperandType) {
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineBrTarget:
					var targetInstr = (Instruction)instr.Operand;
					ilg.Emit(sreOpCode, labelsDict[targetInstr]);
					break;

				case OperandType.InlineSwitch:
					var targets = (IList<Instruction>)instr.Operand;
					var newTargets = new SRE.Label[targets.Count];
					for (int i = 0; i < newTargets.Length; i++)
						newTargets[i] = labelsDict[targets[i]];
					ilg.Emit(sreOpCode, newTargets);
					break;

				case OperandType.InlineNone:
					ilg.Emit(sreOpCode);
					break;

				case OperandType.InlineI:
					ilg.Emit(sreOpCode, (int)instr.Operand);
					break;

				case OperandType.InlineI8:
					ilg.Emit(sreOpCode, (long)instr.Operand);
					break;

				case OperandType.InlineR:
					ilg.Emit(sreOpCode, (double)instr.Operand);
					break;

				case OperandType.ShortInlineR:
					ilg.Emit(sreOpCode, (float)instr.Operand);
					break;

				case OperandType.ShortInlineI:
					if (instr.OpCode.Code == Code.Ldc_I4_S)
						ilg.Emit(sreOpCode, (sbyte)instr.Operand);
					else
						ilg.Emit(sreOpCode, (byte)instr.Operand);
					break;

				case OperandType.InlineString:
					ilg.Emit(sreOpCode, (string)instr.Operand);
					break;

				case OperandType.InlineVar:
				case OperandType.ShortInlineVar:
					Parameter p;
					switch (instr.OpCode.Code) {
					case Code.Ldarg:
					case Code.Ldarga:
					case Code.Starg:
						p = (Parameter)instr.Operand;
						if (p.Index > ushort.MaxValue)
							return null;
						ilg.Emit(sreOpCode, (short)p.Index);
						break;

					case Code.Ldarg_S:
					case Code.Ldarga_S:
					case Code.Starg_S:
						p = (Parameter)instr.Operand;
						if (p.Index > byte.MaxValue)
							return null;
						ilg.Emit(sreOpCode, (byte)p.Index);
						break;

					case Code.Ldloc:
					case Code.Ldloca:
					case Code.Stloc:
					case Code.Ldloc_S:
					case Code.Ldloca_S:
					case Code.Stloc_S:
						ilg.Emit(sreOpCode, localsDict[(Local)instr.Operand]);
						break;

					default:
						return null;
					}
					break;

				case OperandType.InlineMethod:
					var m = Import((IMethod)instr.Operand);
					if (m is SR.ConstructorInfo ci)
						ilg.Emit(sreOpCode, ci);
					else
						ilg.Emit(sreOpCode, (SR.MethodInfo)m);
					break;

				case OperandType.InlineTok:
				case OperandType.InlineType:
				case OperandType.InlineField:
				case OperandType.InlinePhi:
				case OperandType.InlineSig:
				default:
					return null;
				}
			}

			return (EvalDelegate)dm.CreateDelegate(typeof(EvalDelegate));
		}

		Type Import(TypeSig typeSig) {
			if (typeSig.IsPinned)
				typeSig = typeSig.Next;
			switch (typeSig.ElementType) {
			case ElementType.Boolean: return typeof(bool);
			case ElementType.I4: return typeof(int);
			case ElementType.U4: return typeof(uint);
			case ElementType.U8: return typeof(ulong);
			case ElementType.String: return typeof(string);
			default: throw new EvalDelegateCreatorException();
			}
		}

		SR.MethodBase Import(IMethod method) {
			if (method is MethodDef md) {
				if (md.DeclaringType.ReflectionFullName == "System.String") {
					switch (md.Name) {
					case "op_Equality":
						if (!IsBoolean_StringString(md.MethodSig))
							break;
						return SystemString_op_Equality;
					case "op_Inequality":
						if (!IsBoolean_StringString(md.MethodSig))
							break;
						return SystemString_op_Inequality;
					}
				}
			}
			throw new EvalDelegateCreatorException();
		}
		static readonly SR.MethodInfo SystemString_op_Equality = typeof(FilterExpressionMethods).GetMethod("StringEquals");
		static readonly SR.MethodInfo SystemString_op_Inequality = typeof(FilterExpressionMethods).GetMethod("StringNotEquals");

		static bool IsBoolean_StringString(MethodSig sig) {
			if (sig.RetType.ElementType != ElementType.Boolean)
				return false;
			if (sig.GenParamCount != 0)
				return false;
			if (sig.ParamsAfterSentinel != null)
				return false;
			if (sig.Params.Count != 2)
				return false;
			if (sig.Params[0].ElementType != ElementType.String)
				return false;
			if (sig.Params[1].ElementType != ElementType.String)
				return false;
			return true;
		}

		public void Dispose() => module.Dispose();
	}

	static class FilterExpressionMethods {
		public static bool StringEquals(string a, string b) => StringComparer.OrdinalIgnoreCase.Equals(a, b);
		public static bool StringNotEquals(string a, string b) => !StringComparer.OrdinalIgnoreCase.Equals(a, b);
	}
}
