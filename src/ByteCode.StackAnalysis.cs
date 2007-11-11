using System;
using System.Collections.Generic;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public partial class ByteCode
	{
		CilStack stackBefore;
		CilStack stackAfter;
		List<ByteCode> branchesHere = new List<ByteCode>();
		
		public CilStack StackBefore {
			get { return stackBefore; }
		}
		
		public CilStack StackAfter {
			get { return stackAfter; }
		}
		
		public List<ByteCode> BranchesHere {
			get { return branchesHere; }
		}
		
		public void MergeStackBeforeWith(CilStack stack)
		{
			CilStack mergedStack;
			if (CilStack.Merge(this.stackBefore, stack, out mergedStack)) {
				// Stacks are identical
				return;
			}
			this.stackBefore = mergedStack;
			
			stackAfter = SimulateEffectOnStack(this.StackBefore);
			
			switch(this.OpCode.FlowControl) {
				case FlowControl.Branch:
					this.BranchTarget.MergeStackBeforeWith(this.StackAfter);
					break;
				case FlowControl.Cond_Branch:
					this.Next.MergeStackBeforeWith(this.StackAfter);
					this.BranchTarget.MergeStackBeforeWith(this.StackAfter);
					break;
				case FlowControl.Next:
				case FlowControl.Call:
					this.Next.MergeStackBeforeWith(this.StackAfter);
					break;
				case FlowControl.Return:
					if (this.StackAfter.Count > 0) throw new Exception("Non-empty stack at return instruction");
					break;
				default: throw new NotImplementedException();
			}
		}
		
		public CilStack SimulateEffectOnStack(CilStack oldStack)
		{
			CilStack newStack = oldStack.Clone();
			List<Cecil.TypeReference> typeArgs = new List<Cecil.TypeReference>();
			foreach(CilStackSlot slot in newStack.PopCount(this.PopCount)) {
				typeArgs.Add(slot.Type);
			}
			for (int i = 0; i < this.PushCount; i++) {
				newStack.Push(new CilStackSlot(this, this.GetType(typeArgs.ToArray())));
			}
			return newStack;
		}
	}
}
