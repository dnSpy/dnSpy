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

using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// Contains some very simple optimizations that work on the SSA form.
	/// </summary>
	static class SsaOptimization
	{
		public static void Optimize(SsaForm ssaForm)
		{
			DirectlyStoreToVariables(ssaForm);
			SimpleCopyPropagation(ssaForm);
			RemoveDeadAssignments(ssaForm);
		}
		
		/// <summary>
		/// When any instructions stores its result in a stack location that's used only once in a 'stloc' or 'starg' instruction,
		/// we optimize this to directly store in the target location.
		/// As optimization this is redundant (does the same as copy propagation), but it'll make us keep the variables named
		/// after locals instead of keeping the temps as using only the simple copy propagation would do.
		/// </summary>
		public static void DirectlyStoreToVariables(SsaForm ssaForm)
		{
			foreach (SsaBlock block in ssaForm.Blocks) {
				block.Instructions.RemoveAll(
					inst => {
						if (inst.Instruction != null && (inst.Instruction.OpCode == OpCodes.Stloc || inst.Instruction.OpCode == OpCodes.Starg)) {
							SsaVariable target = inst.Target;
							SsaVariable temp = inst.Operands[0];
							if (target.IsSingleAssignment && temp.IsSingleAssignment && temp.Usage.Count == 1 && temp.IsStackLocation) {
								temp.Definition.Target = target;
								return true;
							}
						}
						return false;
					});
			}
			ssaForm.ComputeVariableUsage(); // update usage after we modified stuff
		}
		
		public static void SimpleCopyPropagation(SsaForm ssaForm, bool onlyForStackLocations = true)
		{
			foreach (SsaBlock block in ssaForm.Blocks) {
				foreach (SsaInstruction inst in block.Instructions) {
					if (inst.IsMoveInstruction && inst.Target.IsSingleAssignment && inst.Operands[0].IsSingleAssignment) {
						if (inst.Target.IsStackLocation || !onlyForStackLocations) {
							// replace all uses of 'target' with 'operands[0]'.
							foreach (SsaInstruction useInstruction in inst.Target.Usage) {
								useInstruction.ReplaceVariableInOperands(inst.Target, inst.Operands[0]);
							}
						}
					}
				}
			}
			ssaForm.ComputeVariableUsage(); // update usage after we modified stuff
		}
		
		public static void RemoveDeadAssignments(SsaForm ssaForm)
		{
			HashSet<SsaVariable> liveVariables = new HashSet<SsaVariable>();
			// find variables that are used directly
			foreach (SsaBlock block in ssaForm.Blocks) {
				foreach (SsaInstruction inst in block.Instructions) {
					if (!CanRemoveAsDeadCode(inst)) {
						if (inst.Target != null)
							liveVariables.Add(inst.Target);
						foreach (SsaVariable op in inst.Operands) {
							liveVariables.Add(op);
						}
					}
				}
			}
			Queue<SsaVariable> queue = new Queue<SsaVariable>(liveVariables);
			// find variables that are used indirectly
			while (queue.Count > 0) {
				SsaVariable v = queue.Dequeue();
				if (v.IsSingleAssignment) {
					foreach (SsaVariable op in v.Definition.Operands) {
						if (liveVariables.Add(op))
							queue.Enqueue(op);
					}
				}
			}
			// remove assignments to all unused variables
			foreach (SsaBlock block in ssaForm.Blocks) {
				block.Instructions.RemoveAll(
					inst => {
						if (inst.Target != null && !liveVariables.Contains(inst.Target)) {
							Debug.Assert(inst.Target.IsSingleAssignment);
							return true;
						}
						return false;
					});
			}
			ssaForm.ComputeVariableUsage(); // update usage after we modified stuff
		}
		
		static bool CanRemoveAsDeadCode(SsaInstruction inst)
		{
			if (inst.Target != null && !inst.Target.IsSingleAssignment)
				return false;
			switch (inst.SpecialOpCode) {
				case SpecialOpCode.Phi:
				case SpecialOpCode.Exception:
				case SpecialOpCode.Parameter:
				case SpecialOpCode.Uninitialized:
					return true;
				case SpecialOpCode.None:
					return inst.IsMoveInstruction;
				default:
					return false;
			}
		}
	}
}
