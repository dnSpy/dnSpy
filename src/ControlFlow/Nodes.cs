using System;
using System.Collections.Generic;

using Mono.Cecil.Cil;

namespace Decompiler.ControlFlow
{
	public class BasicBlock: Node
	{
		List<ByteCodeExpression> body = new List<ByteCodeExpression>();
		List<BasicBlock> basicBlockPredecessors = new List<BasicBlock>();
		BasicBlock fallThroughBasicBlock;
		BasicBlock branchBasicBlock;
		
		public List<ByteCodeExpression> Body {
			get { return body; }
		}
		
		public List<BasicBlock> BasicBlockPredecessors {
			get { return basicBlockPredecessors; }
		}
		
		public BasicBlock FallThroughBasicBlock {
			get { return fallThroughBasicBlock; }
			set { fallThroughBasicBlock = value; }
		}
		
		public BasicBlock BranchBasicBlock {
			get { return branchBasicBlock; }
			set { branchBasicBlock = value; }
		}
		
		public IEnumerable<BasicBlock> BasicBlockSuccessors {
			get {
				if (this.FallThroughBasicBlock != null) {
					yield return this.FallThroughBasicBlock;
				}
				if (this.BranchBasicBlock != null) {
					yield return this.BranchBasicBlock;
				}
			}
		}
	}
	
	public enum ShortCircuitOperator {
		LeftAndRight,
		LeftOrRight,
		NotLeftAndRight,
		NotLeftOrRight,
	}
	
	public abstract class Branch: Node
	{
		public abstract BasicBlock FirstBasicBlock { get; }
		public abstract BasicBlock TrueSuccessor { get; }
		public abstract BasicBlock FalseSuccessor { get; }
	}
	
	public class SimpleBranch: Branch
	{
		public override BasicBlock FirstBasicBlock {
			get {
				return this.BasicBlock;
			}
		}
		
		public BasicBlock BasicBlock {
			get { return (BasicBlock)this.Childs[0]; }
		}
		
		public override BasicBlock TrueSuccessor {
			get { return this.BasicBlock.BranchBasicBlock; }
		}
		
		public override BasicBlock FalseSuccessor {
			get { return this.BasicBlock.FallThroughBasicBlock; }
		}
	}
	
	public class ShortCircuitBranch: Branch
	{
		ShortCircuitOperator @operator;
		
		public override BasicBlock FirstBasicBlock {
			get {
				return this.Left.FirstBasicBlock;
			}
		}
		
		public Branch Left {
			get { return (Branch)this.Childs[0];; }
		}
		
		public Branch Right {
			get { return (Branch)this.Childs[1]; }
		}
		
		public ShortCircuitOperator Operator {
			get { return @operator; }
			set { @operator = value; }
		}
		
		public override BasicBlock TrueSuccessor {
			get { return this.Right.TrueSuccessor; }
		}
		
		public override BasicBlock FalseSuccessor {
			get { return this.Right.FalseSuccessor; }
		}
	}
	
	public class MethodBodyGraph: Node
	{
		BasicBlock methodEntry;
		
		public BasicBlock MethodEntry {
			get { return methodEntry; }
		}
		
		public MethodBodyGraph(ByteCodeExpressionCollection exprs)
		{
			if (exprs.Count == 0) throw new ArgumentException("Count == 0", "exprs");
			
			BasicBlock basicBlock = null;
			for(int i = 0; i < exprs.Count; i++) {
				// Start new basic block if
				//  - this is first expression
				//  - last expression was branch
				//  - this expression is branch target
				//  - this expression is a branch
				if (i == 0 ||
				    exprs[i - 1].BranchTarget != null ||
				    exprs[i].BranchesHere.Count > 0 ||
				    exprs[i].BranchTarget != null)
				{
					basicBlock = new BasicBlock();
					this.Childs.Add(basicBlock);
				}
				basicBlock.Body.Add(exprs[i]);
				exprs[i].BasicBlock = basicBlock;
			}
			
			// Add fall-through links to BasicBlocks
			for(int i = 0; i < exprs.Count - 1; i++) {
				BasicBlock node = exprs[i].BasicBlock;
				BasicBlock target = exprs[i + 1].BasicBlock;
				
				// Still same basic block - ignore
				if (node == target) continue;
				
				// Non-conditional branch does not fall-through
				if (exprs[i].LastByteCode.OpCode.Code == Code.Br) continue;
				
				node.FallThroughBasicBlock = target;
				target.BasicBlockPredecessors.Add(node);
			}
			
			// Add branch links to BasicBlocks
			for(int i = 0; i < exprs.Count; i++) {
				if (exprs[i].BranchTarget != null) {
					BasicBlock node = exprs[i].BasicBlock;
					BasicBlock target = exprs[i].BranchTarget.BasicBlock;
					
					node.BranchBasicBlock = target;
					target.BasicBlockPredecessors.Add(node);
				}
			}
			
			this.methodEntry = (BasicBlock)this.HeadChild;
		}
	}
	
	public class AcyclicGraph: Node
	{
	}
	
	public class Loop: Node
	{
	}
	
	public class Block: Node
	{
	}
	
	public class ConditionalNode: Node
	{
		Branch condition;
		Block trueBody = new Block();
		Block falseBody = new Block();
		
		public Branch Condition {
			get { return condition; }
		}
		
		public Block TrueBody {
			get { return trueBody; }
		}
		
		public Block FalseBody {
			get { return falseBody; }
		}
		
		public ConditionalNode(Branch condition)
		{
			this.condition = condition;
			
			condition.MoveTo(this);
			trueBody.MoveTo(this);
			falseBody.MoveTo(this);
		}
	}
}
