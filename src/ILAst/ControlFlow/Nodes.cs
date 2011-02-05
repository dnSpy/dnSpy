using System;
using System.Collections.Generic;
using Decompiler.Mono.Cecil.Rocks;
using Mono.Cecil.Cil;

namespace Decompiler.ControlFlow
{
	public class BasicBlock: Node
	{
		List<ILExpression> body = new List<ILExpression>();
		List<BasicBlock> basicBlockPredecessors = new List<BasicBlock>();
		BasicBlock fallThroughBasicBlock;
		BasicBlock branchBasicBlock;
		
		public List<ILExpression> Body {
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
		
		public MethodBodyGraph(List<ILExpression> exprs)
		{
			if (exprs.Count == 0) throw new ArgumentException("Count == 0", "exprs");
			
			BasicBlock basicBlock = null;
			for(int i = 0; i < exprs.Count; i++) {
				if (i == 0 ||
				    exprs[i - 1].OpCode.IsBranch() ||
				    exprs[i].IsBranchTarget ||
				    exprs[i].OpCode.IsBranch())
				{
					basicBlock = new BasicBlock();
					this.Childs.Add(basicBlock);
				}
				basicBlock.Body.Add(exprs[i]);
				exprs[i].SetBasicBlock(basicBlock);
			}
			
			// Add fall-through links to BasicBlocks
			for(int i = 0; i < exprs.Count - 1; i++) {
				BasicBlock node = exprs[i].BasicBlock;
				BasicBlock target = exprs[i + 1].BasicBlock;
				
				if (target != node && exprs[i].OpCode.CanFallThough()) {
					node.FallThroughBasicBlock = target;
					target.BasicBlockPredecessors.Add(node);
				}
			}
			
			// Add branch links to BasicBlocks
			for(int i = 0; i < exprs.Count; i++) {
				if (exprs[i].OpCode.IsBranch()) {
					BasicBlock node = exprs[i].BasicBlock;
					BasicBlock target = ((ILExpression)exprs[i].Operand).BasicBlock;
					
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
