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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// Convers a method to static single assignment form.
	/// </summary>
	sealed class TransformToSsa
	{
		public static void Transform(ControlFlowGraph cfg, SsaForm ssa, bool optimize = true)
		{
			TransformToSsa transform = new TransformToSsa(cfg, ssa);
			transform.ConvertVariablesToSsa();
			SsaOptimization.RemoveDeadAssignments(ssa); // required so that 'MakeByRefCallsSimple' can detect more cases
			if (SimplifyByRefCalls.MakeByRefCallsSimple(ssa)) {
				transform.ConvertVariablesToSsa();
			}
			if (optimize)
				SsaOptimization.Optimize(ssa);
		}
		
		readonly ControlFlowGraph cfg;
		readonly SsaForm ssaForm;
		readonly List<SsaInstruction>[] writeToOriginalVariables; // array index -> SsaVariable OriginalVariableIndex
		readonly bool[] addressTaken; // array index -> SsaVariable OriginalVariableIndex; value = whether ldloca instruction was used with variable
		
		private TransformToSsa(ControlFlowGraph cfg, SsaForm ssaForm)
		{
			this.cfg = cfg;
			this.ssaForm = ssaForm;
			this.writeToOriginalVariables = new List<SsaInstruction>[ssaForm.OriginalVariables.Count];
			this.addressTaken = new bool[ssaForm.OriginalVariables.Count];
		}
		
		#region CollectInformationAboutOriginalVariableUse
		void CollectInformationAboutOriginalVariableUse()
		{
			Debug.Assert(addressTaken.Length == writeToOriginalVariables.Length);
			for (int i = 0; i < writeToOriginalVariables.Length; i++) {
				Debug.Assert(ssaForm.OriginalVariables[i].OriginalVariableIndex == i);
				
				addressTaken[i] = false;
				// writeToOriginalVariables is only used when placing phi functions
				// we don't need to do that anymore for variables that are already in SSA form
				if (ssaForm.OriginalVariables[i].IsSingleAssignment)
					writeToOriginalVariables[i] = null;
				else
					writeToOriginalVariables[i] = new List<SsaInstruction>();
			}
			foreach (SsaBlock block in ssaForm.Blocks) {
				foreach (SsaInstruction inst in block.Instructions) {
					if (inst.Target != null ) {
						var list = writeToOriginalVariables[inst.Target.OriginalVariableIndex];
						if (list != null)
							list.Add(inst);
					}
					if (inst.Instruction != null) {
						if (inst.Instruction.OpCode == OpCodes.Ldloca) {
							addressTaken[ssaForm.GetOriginalVariable((VariableDefinition)inst.Instruction.Operand).OriginalVariableIndex] = true;
						} else if (inst.Instruction.OpCode == OpCodes.Ldarga) {
							addressTaken[ssaForm.GetOriginalVariable((ParameterDefinition)inst.Instruction.Operand).OriginalVariableIndex] = true;
						}
					}
				}
			}
		}
		#endregion
		
		#region ConvertToSsa
		void ConvertVariablesToSsa()
		{
			CollectInformationAboutOriginalVariableUse();
			bool[] processVariable = new bool[ssaForm.OriginalVariables.Count];
			foreach (SsaVariable variable in ssaForm.OriginalVariables) {
				if (!variable.IsSingleAssignment && !addressTaken[variable.OriginalVariableIndex]) {
					PlacePhiFunctions(variable);
					processVariable[variable.OriginalVariableIndex] = true;
				}
			}
			RenameVariables(processVariable);
			foreach (SsaVariable variable in ssaForm.OriginalVariables) {
				if (!addressTaken[variable.OriginalVariableIndex]) {
					Debug.Assert(variable.IsSingleAssignment && variable.Definition != null);
				}
			}
			ssaForm.ComputeVariableUsage();
		}
		#endregion
		
		#region PlacePhiFunctions
		void PlacePhiFunctions(SsaVariable variable)
		{
			cfg.ResetVisited();
			HashSet<SsaBlock> blocksWithPhi = new HashSet<SsaBlock>();
			Queue<ControlFlowNode> worklist = new Queue<ControlFlowNode>();
			foreach (SsaInstruction writeInstruction in writeToOriginalVariables[variable.OriginalVariableIndex]) {
				ControlFlowNode cfgNode = cfg.Nodes[writeInstruction.ParentBlock.BlockIndex];
				if (!cfgNode.Visited) {
					cfgNode.Visited = true;
					worklist.Enqueue(cfgNode);
				}
			}
			while (worklist.Count > 0) {
				ControlFlowNode cfgNode = worklist.Dequeue();
				foreach (ControlFlowNode dfNode in cfgNode.DominanceFrontier) {
					// we don't need phi functions in the exit node
					if (dfNode.NodeType == ControlFlowNodeType.RegularExit || dfNode.NodeType == ControlFlowNodeType.ExceptionalExit)
						continue;
					SsaBlock y = ssaForm.Blocks[dfNode.BlockIndex];
					if (blocksWithPhi.Add(y)) {
						// add a phi instruction in y
						SsaVariable[] operands = Enumerable.Repeat(variable, dfNode.Incoming.Count).ToArray();
						y.Instructions.Insert(0, new SsaInstruction(y, null, variable, operands, specialOpCode: SpecialOpCode.Phi));
						if (!dfNode.Visited) {
							dfNode.Visited = true;
							worklist.Enqueue(dfNode);
						}
					}
				}
			}
		}
		#endregion
		
		#region RenameVariable
		int tempVariableCounter = 1;
		
		void RenameVariables(bool[] processVariable)
		{
			VariableRenamer r = new VariableRenamer(this, processVariable);
			r.Visit(ssaForm.EntryPoint);
		}
		
		sealed class VariableRenamer
		{
			readonly TransformToSsa transform;
			readonly ReadOnlyCollection<SsaVariable> inputVariables;
			internal readonly Stack<SsaVariable>[] versionStacks;
			int[] versionCounters; // specifies for each input variable the next version number
			
			// processVariable = specifies for each input variable whether we should rename it
			public VariableRenamer(TransformToSsa transform, bool[] processVariable)
			{
				this.transform = transform;
				this.inputVariables = transform.ssaForm.OriginalVariables;
				Debug.Assert(inputVariables.Count == processVariable.Length);
				this.versionCounters = new int[inputVariables.Count];
				this.versionStacks = new Stack<SsaVariable>[inputVariables.Count];
				for (int i = 0; i < versionStacks.Length; i++) {
					if (processVariable[i]) {
						Debug.Assert(inputVariables[i].IsSingleAssignment == false);
						// only create version stacks for the variables that we need to process and that weren't already processed earlier
						versionStacks[i] = new Stack<SsaVariable>();
						versionStacks[i].Push(inputVariables[i]);
					}
				}
			}
			
			SsaVariable MakeNewVersion(int variableIndex)
			{
				int versionCounter = ++versionCounters[variableIndex];
				SsaVariable x = inputVariables[variableIndex];
				if (versionCounter == 1) {
					return x;
				} else {
					if (x.IsStackLocation) {
						return new SsaVariable(x, "temp" + (transform.tempVariableCounter++));
					} else {
						return new SsaVariable(x, x.Name + "_" + versionCounter);
					}
				}
			}
			
			internal void Visit(SsaBlock block)
			{
				// duplicate top of all stacks
				foreach (var stack in versionStacks) {
					if (stack != null)
						stack.Push(stack.Peek());
				}
				
				foreach (SsaInstruction s in block.Instructions) {
					// replace all uses of variables being processed with their current version.
					if (s.SpecialOpCode != SpecialOpCode.Phi) {
						for (int i = 0; i < s.Operands.Length; i++) {
							var stack = versionStacks[s.Operands[i].OriginalVariableIndex];
							if (stack != null)
								s.Operands[i] = stack.Peek();
						}
					}
					// if we're writing to a variable we should process:
					if (s.Target != null) {
						int targetIndex = s.Target.OriginalVariableIndex;
						if (versionStacks[targetIndex] != null) {
							s.Target = MakeNewVersion(targetIndex);
							s.Target.IsSingleAssignment = true;
							s.Target.Definition = s;
							
							// we already pushed our entry for this SsaBlock at the beginning (where we duplicated all stacks),
							// so now replace the top element
							versionStacks[targetIndex].Pop();
							versionStacks[targetIndex].Push(s.Target);
						}
					}
				}
				
				foreach (SsaBlock succ in block.Successors) {
					int j = succ.Predecessors.IndexOf(block);
					Debug.Assert(j >= 0);
					foreach (SsaInstruction f in succ.Instructions) {
						if (f.SpecialOpCode == SpecialOpCode.Phi) {
							var stack = versionStacks[f.Target.OriginalVariableIndex];
							if (stack != null) {
								f.Operands[j] = stack.Peek();
							}
						}
					}
				}
				foreach (ControlFlowNode child in transform.cfg.Nodes[block.BlockIndex].DominatorTreeChildren)
					Visit(transform.ssaForm.Blocks[child.BlockIndex]);
				// restore stacks:
				foreach (var stack in versionStacks) {
					if (stack != null)
						stack.Pop();
				}
			}
		}
		#endregion
	}
}
