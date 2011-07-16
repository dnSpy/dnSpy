// Copyright (c) 2010 Daniel Grunwald
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
	/// Constructs "SsaForm" graph for a CFG.
	/// This class transforms the method from stack-based IL to a register-based IL language.
	/// Then it calls into TransformToSsa to convert the resulting graph to static single assignment form.
	/// </summary>
	public sealed class SsaFormBuilder
	{
		public static SsaForm Build(MethodDefinition method)
		{
			if (method == null)
				throw new ArgumentNullException("method");
			var cfg = ControlFlowGraphBuilder.Build(method.Body);
			cfg.ComputeDominance();
			cfg.ComputeDominanceFrontier();
			var ssa = BuildRegisterIL(method, cfg);
			TransformToSsa.Transform(cfg, ssa);
			return ssa;
		}
		
		public static SsaForm BuildRegisterIL(MethodDefinition method, ControlFlowGraph cfg)
		{
			if (method == null)
				throw new ArgumentNullException("method");
			if (cfg == null)
				throw new ArgumentNullException("cfg");
			return new SsaFormBuilder(method, cfg).Build();
		}
		
		readonly MethodDefinition method;
		readonly ControlFlowGraph cfg;
		
		readonly SsaBlock[] blocks; // array index = block index
		readonly int[] stackSizeAtBlockStart; // array index = block index
		
		readonly SsaVariable[] parameters; // array index = parameter number
		readonly SsaVariable[] locals; // array index = local number
		readonly SsaVariable[] stackLocations; // array index = position on the IL evaluation stack
		SsaForm ssaForm;
		
		private SsaFormBuilder(MethodDefinition method, ControlFlowGraph cfg)
		{
			this.method = method;
			this.cfg = cfg;
			
			this.blocks = new SsaBlock[cfg.Nodes.Count];
			this.stackSizeAtBlockStart = new int[cfg.Nodes.Count];
			for (int i = 0; i < stackSizeAtBlockStart.Length; i++) {
				stackSizeAtBlockStart[i] = -1;
			}
			stackSizeAtBlockStart[cfg.EntryPoint.BlockIndex] = 0;
			
			this.parameters = new SsaVariable[method.Parameters.Count + (method.HasThis ? 1 : 0)];
			if (method.HasThis)
				parameters[0] = new SsaVariable(method.Body.ThisParameter);
			for (int i = 0; i < method.Parameters.Count; i++)
				parameters[i + (method.HasThis ? 1 : 0)] = new SsaVariable(method.Parameters[i]);
			
			this.locals = new SsaVariable[method.Body.Variables.Count];
			for (int i = 0; i < locals.Length; i++)
				locals[i] = new SsaVariable(method.Body.Variables[i]);
			
			this.stackLocations = new SsaVariable[method.Body.MaxStackSize];
			for (int i = 0; i < stackLocations.Length; i++) {
				stackLocations[i] = new SsaVariable(i);
			}
		}
		
		internal SsaForm Build()
		{
			CreateGraphStructure();
			this.ssaForm = new SsaForm(blocks, parameters, locals, stackLocations, method.HasThis);
			CreateInstructions(cfg.EntryPoint.BlockIndex);
			CreateSpecialInstructions();
			return ssaForm;
		}
		
		void CreateGraphStructure()
		{
			for (int i = 0; i < blocks.Length; i++) {
				blocks[i] = new SsaBlock(cfg.Nodes[i]);
			}
			for (int i = 0; i < blocks.Length; i++) {
				foreach (ControlFlowNode node in cfg.Nodes[i].Successors) {
					blocks[i].Successors.Add(blocks[node.BlockIndex]);
					blocks[node.BlockIndex].Predecessors.Add(blocks[i]);
				}
			}
		}
		
		void CreateInstructions(int blockIndex)
		{
			ControlFlowNode cfgNode = cfg.Nodes[blockIndex];
			SsaBlock block = blocks[blockIndex];
			
			int stackSize = stackSizeAtBlockStart[blockIndex];
			Debug.Assert(stackSize >= 0);
			
			List<Instruction> prefixes = new List<Instruction>();
			foreach (Instruction inst in cfgNode.Instructions) {
				if (inst.OpCode.OpCodeType == OpCodeType.Prefix) {
					prefixes.Add(inst);
					continue;
				}
				
				int popCount = inst.GetPopDelta(method) ?? stackSize;
				stackSize -= popCount;
				if (stackSize < 0)
					throw new InvalidProgramException("IL stack underflow");
				
				int pushCount = inst.GetPushDelta();
				if (stackSize + pushCount > stackLocations.Length)
					throw new InvalidProgramException("IL stack overflow");
				
				SsaVariable target;
				SsaVariable[] operands;
				DetermineOperands(stackSize, inst, popCount, pushCount, out target, out operands);
				
				Instruction[] prefixArray = prefixes.Count > 0 ? prefixes.ToArray() : null;
				prefixes.Clear();
				
				// ignore NOP instructions
				if (!(inst.OpCode == OpCodes.Nop || inst.OpCode == OpCodes.Pop)) {
					block.Instructions.Add(new SsaInstruction(block, inst, target, operands, prefixArray));
				}
				stackSize += pushCount;
			}
			
			foreach (ControlFlowEdge edge in cfgNode.Outgoing) {
				int newStackSize;
				switch (edge.Type) {
					case JumpType.Normal:
						newStackSize = stackSize;
						break;
					case JumpType.EndFinally:
						if (stackSize != 0)
							throw new NotSupportedException("stacksize must be 0 in endfinally edge");
						newStackSize = 0;
						break;
					case JumpType.JumpToExceptionHandler:
						switch (edge.Target.NodeType) {
							case ControlFlowNodeType.FinallyOrFaultHandler:
								newStackSize = 0;
								break;
							case ControlFlowNodeType.ExceptionalExit:
							case ControlFlowNodeType.CatchHandler:
								newStackSize = 1;
								break;
							default:
								throw new NotSupportedException("unsupported target node type: " + edge.Target.NodeType);
						}
						break;
					default:
						throw new NotSupportedException("unsupported jump type: " + edge.Type);
				}
				
				int nextStackSize = stackSizeAtBlockStart[edge.Target.BlockIndex];
				if (nextStackSize == -1) {
					stackSizeAtBlockStart[edge.Target.BlockIndex] = newStackSize;
					CreateInstructions(edge.Target.BlockIndex);
				} else if (nextStackSize != newStackSize) {
					throw new InvalidProgramException("Stack size doesn't match");
				}
			}
		}

		void DetermineOperands(int stackSize, Instruction inst, int popCount, int pushCount, out SsaVariable target, out SsaVariable[] operands)
		{
			switch (inst.OpCode.Code) {
				case Code.Ldarg:
					operands = new SsaVariable[] { ssaForm.GetOriginalVariable((ParameterReference)inst.Operand) };
					target = stackLocations[stackSize];
					break;
				case Code.Starg:
					operands = new SsaVariable[] { stackLocations[stackSize] };
					target = ssaForm.GetOriginalVariable((ParameterReference)inst.Operand);
					break;
				case Code.Ldloc:
					operands = new SsaVariable[] { ssaForm.GetOriginalVariable((VariableReference)inst.Operand) };
					target = stackLocations[stackSize];
					break;
				case Code.Stloc:
					operands = new SsaVariable[] { stackLocations[stackSize] };
					target = ssaForm.GetOriginalVariable((VariableReference)inst.Operand);
					break;
				case Code.Dup:
					operands = new SsaVariable[] { stackLocations[stackSize] };
					target = stackLocations[stackSize + 1];
					break;
				default:
					operands = new SsaVariable[popCount];
					for (int i = 0; i < popCount; i++) {
						operands[i] = stackLocations[stackSize + i];
					}

					switch (pushCount) {
						case 0:
							target = null;
							break;
						case 1:
							target = stackLocations[stackSize];
							break;
						default:
							throw new NotSupportedException("unsupported pushCount=" + pushCount);
					}
					break;
			}
		}
		
		void CreateSpecialInstructions()
		{
			// Everything needs an initial write for the SSA transformation to work correctly.
			foreach (SsaVariable v in parameters) {
				ssaForm.EntryPoint.Instructions.Add(new SsaInstruction(ssaForm.EntryPoint, null, v, null, specialOpCode: SpecialOpCode.Parameter));
			}
			foreach (SsaVariable v in locals) {
				ssaForm.EntryPoint.Instructions.Add(new SsaInstruction(ssaForm.EntryPoint, null, v, null, specialOpCode: SpecialOpCode.Uninitialized));
			}
			foreach (SsaVariable v in stackLocations) {
				ssaForm.EntryPoint.Instructions.Add(new SsaInstruction(ssaForm.EntryPoint, null, v, null, specialOpCode: SpecialOpCode.Uninitialized));
			}
			foreach (SsaBlock b in blocks) {
				if (b.NodeType == ControlFlowNodeType.CatchHandler) {
					b.Instructions.Add(new SsaInstruction(b, null, stackLocations[0], null,
					                                      specialOpCode: SpecialOpCode.Exception,
					                                      typeOperand: cfg.Nodes[b.BlockIndex].ExceptionHandler.CatchType));
				}
			}
		}
	}
}
