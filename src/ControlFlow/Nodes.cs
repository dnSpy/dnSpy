using System;
using System.Collections.Generic;

using Mono.Cecil.Cil;

namespace Decompiler.ControlFlow
{
	public class MethodBodyGraph: Node
	{
		// TODO: Add links between the generated BasicBlocks
		public MethodBodyGraph(StackExpressionCollection exprs): base(null)
		{
			if (exprs.Count == 0) throw new ArgumentException("Count == 0", "exprs");
			
			BasicBlock basicBlock = null;
			for(int i = 0; i < exprs.Count; i++) {
				// Start new basic block if
				//  - this is first expression
				//  - last expression was branch
				//  - this expression is branch target
				if (i == 0 || exprs[i - 1].BranchTarget != null || exprs[i].BranchesHere.Count > 0){
					basicBlock = new BasicBlock(this);
					this.Childs.Add(basicBlock);
				}
				basicBlock.Body.Add(exprs[i]);
				exprs[i].BasicBlock = basicBlock;
			}
			
			// Add fall-through links
			for(int i = 0; i < exprs.Count - 1; i++) {
				BasicBlock node = exprs[i].BasicBlock;
				BasicBlock target = exprs[i + 1].BasicBlock;
				
				// Still same basic block - ignore
				if (node == target) continue;
				
				// Non-conditional branch does not fall-through
				if (exprs[i].LastByteCode.OpCode.Code == Code.Br) continue;
				
				node.FallThroughBasicBlock = target;
				node.Successors.Add(target);
				target.Predecessors.Add(node);
			}
			
			// Add branch links
			for(int i = 0; i < exprs.Count; i++) {
				if (exprs[i].BranchTarget != null) {
					BasicBlock node = exprs[i].BasicBlock;
					BasicBlock target = exprs[i].BranchTarget.BasicBlock;
					
					node.BranchBasicBlock = target;
					node.Successors.Add(target);
					target.Predecessors.Add(node);
				}
			}
			
			this.HeadChild = this.Childs[0];
		}
	}
	
	public class AcyclicGraph: Node
	{
		public AcyclicGraph(Node parent): base(parent){
			
		}
	}
	
	public class Loop: Node
	{
		public Loop(Node parent): base(parent){
			
		}
	}
	
	public class BasicBlock: Node
	{
		List<StackExpression> body = new List<StackExpression>();
		BasicBlock fallThroughBasicBlock;
		BasicBlock branchBasicBlock;
		
		public List<StackExpression> Body {
			get { return body; }
		}
		
		public BasicBlock FallThroughBasicBlock {
			get { return fallThroughBasicBlock; }
			set { fallThroughBasicBlock = value; }
		}
		
		public BasicBlock BranchBasicBlock {
			get { return branchBasicBlock; }
			set { branchBasicBlock = value; }
		}
		
		public BasicBlock(Node parent): base(parent)
		{
		}
	}
}
