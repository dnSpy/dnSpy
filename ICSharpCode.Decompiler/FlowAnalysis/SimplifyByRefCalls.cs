// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// This is a transformation working on SSA form.
	/// It removes ldloca instructions and replaces them with SpecialOpCode.PrepareByOutCall or SpecialOpCode.PrepareByRefCall.
	/// This then allows the variable that had its address taken to also be transformed into SSA.
	/// </summary>
	sealed class SimplifyByRefCalls
	{
		public static bool MakeByRefCallsSimple(SsaForm ssaForm)
		{
			SimplifyByRefCalls instance = new SimplifyByRefCalls(ssaForm);
			foreach (SsaBlock block in ssaForm.Blocks) {
				for (int i = 0; i < block.Instructions.Count; i++) {
					SsaInstruction inst = block.Instructions[i];
					if (inst.Instruction != null) {
						switch (inst.Instruction.OpCode.Code) {
							case Code.Call:
							case Code.Callvirt:
								instance.MakeByRefCallSimple(block, ref i, (IMethodSignature)inst.Instruction.Operand);
								break;
							case Code.Initobj:
								instance.MakeInitObjCallSimple(block, ref i);
								break;
							case Code.Ldfld:
								instance.MakeLoadFieldCallSimple(block, ref i);
								break;
						}
					}
				}
			}
			instance.RemoveRedundantInstructions();
			if (instance.couldSimplifySomething)
				ssaForm.ComputeVariableUsage();
			return instance.couldSimplifySomething;
		}
		
		readonly SsaForm ssaForm;
		
		bool couldSimplifySomething;
		
		// the list of ldloca instructions we will remove
		readonly List<SsaInstruction> redundantLoadAddressInstructions = new List<SsaInstruction>();
		
		private SimplifyByRefCalls(SsaForm ssaForm)
		{
			this.ssaForm = ssaForm;
		}
		
		void MakeByRefCallSimple(SsaBlock block, ref int instructionIndexInBlock, IMethodSignature targetMethod)
		{
			SsaInstruction inst = block.Instructions[instructionIndexInBlock];
			for (int i = 0; i < inst.Operands.Length; i++) {
				SsaVariable operand = inst.Operands[i];
				if (operand.IsSingleAssignment && operand.Usage.Count == 1 && IsLoadAddress(operand.Definition)) {
					// address is used for this method call only
					
					Instruction loadAddressInstruction = operand.Definition.Instruction;
					
					// find target parameter type:
					bool isOut;
					if (i == 0 && targetMethod.HasThis) {
						isOut = false;
					} else {
						ParameterDefinition parameter = targetMethod.Parameters[i - (targetMethod.HasThis ? 1 : 0)];
						isOut = parameter.IsOut;
					}
					
					SsaVariable addressTakenOf = GetVariableFromLoadAddressInstruction(loadAddressInstruction);
					
					// insert "Prepare" instruction on front
					SpecialOpCode loadOpCode = isOut ? SpecialOpCode.PrepareByOutCall : SpecialOpCode.PrepareByRefCall;
					block.Instructions.Insert(instructionIndexInBlock++, new SsaInstruction(
						block, null, operand, new SsaVariable[] { addressTakenOf }, specialOpCode: loadOpCode));
					
					// insert "WriteAfterByRefOrOutCall" instruction after call
					block.Instructions.Insert(instructionIndexInBlock + 1, new SsaInstruction(
						block, null, addressTakenOf, new SsaVariable[] { operand }, specialOpCode: SpecialOpCode.WriteAfterByRefOrOutCall));
					
					couldSimplifySomething = true;
					
					// remove the loadAddressInstruction later
					// (later because it might be defined in the current block and we don't want instructionIndex to become invalid)
					redundantLoadAddressInstructions.Add(operand.Definition);
				}
			}
		}

		SsaVariable GetVariableFromLoadAddressInstruction(Instruction loadAddressInstruction)
		{
			if (loadAddressInstruction.OpCode == OpCodes.Ldloca) {
				return ssaForm.GetOriginalVariable((VariableReference)loadAddressInstruction.Operand);
			} else {
				Debug.Assert(loadAddressInstruction.OpCode == OpCodes.Ldarga);
				return ssaForm.GetOriginalVariable((ParameterReference)loadAddressInstruction.Operand);
			}
		}
		
		static bool IsLoadAddress(SsaInstruction inst)
		{
			return inst.Instruction != null && (inst.Instruction.OpCode == OpCodes.Ldloca || inst.Instruction.OpCode == OpCodes.Ldarga);
		}
		
		void MakeInitObjCallSimple(SsaBlock block, ref int instructionIndexInBlock)
		{
			SsaInstruction inst = block.Instructions[instructionIndexInBlock];
			Debug.Assert(inst.Operands.Length == 1);
			SsaVariable operand = inst.Operands[0];
			if (operand.IsSingleAssignment && operand.Usage.Count == 1 && IsLoadAddress(operand.Definition)) {
				// replace instruction with special "InitObj" instruction
				block.Instructions[instructionIndexInBlock] = new SsaInstruction(
					inst.ParentBlock, null, GetVariableFromLoadAddressInstruction(operand.Definition.Instruction), null,
					specialOpCode: SpecialOpCode.InitObj,
					typeOperand: (TypeReference)inst.Instruction.Operand);
				
				couldSimplifySomething = true;
				
				// remove the loadAddressInstruction later
				redundantLoadAddressInstructions.Add(operand.Definition);
			}
		}
		
		void MakeLoadFieldCallSimple(SsaBlock block, ref int instructionIndexInBlock)
		{
			SsaInstruction inst = block.Instructions[instructionIndexInBlock];
			Debug.Assert(inst.Operands.Length == 1);
			SsaVariable operand = inst.Operands[0];
			if (operand.IsSingleAssignment && operand.Usage.Count == 1 && IsLoadAddress(operand.Definition)) {
				// insert special "PrepareForFieldAccess" instruction in front
				block.Instructions.Insert(instructionIndexInBlock++, new SsaInstruction(
					inst.ParentBlock, null, operand, 
					new SsaVariable[] { GetVariableFromLoadAddressInstruction(operand.Definition.Instruction) },
					specialOpCode: SpecialOpCode.PrepareForFieldAccess));
				
				couldSimplifySomething = true;
				
				// remove the loadAddressInstruction later
				redundantLoadAddressInstructions.Add(operand.Definition);
			}
		}
		
		void RemoveRedundantInstructions()
		{
			foreach (SsaInstruction inst in redundantLoadAddressInstructions) {
				inst.ParentBlock.Instructions.Remove(inst);
			}
		}
	}
}
