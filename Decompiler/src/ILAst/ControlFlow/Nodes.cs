using System;
using System.Collections.Generic;
using System.Linq;
using Decompiler.Mono.Cecil.Rocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler.ControlFlow
{
	public class BasicBlock: Node
	{
		List<ILNode> body = new List<ILNode>();
		List<BasicBlock> basicBlockPredecessors = new List<BasicBlock>();
		BasicBlock fallThroughBasicBlock;
		BasicBlock branchBasicBlock;
		
		public List<ILNode> Body {
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
		
		Dictionary<ILLabel, BasicBlock> labelToBasicBlock = new Dictionary<ILLabel, BasicBlock>();
		
		public MethodBodyGraph(List<ILNode> ast)
		{
			if (ast.Count == 0) throw new ArgumentException("Count == 0", "ast");
			this.methodEntry = new BasicBlock();
			this.Childs.Add(this.methodEntry);
			this.Childs.AddRange(SplitToBasicBlocks(ast));
			
			// Add branch links to BasicBlocks
			foreach(BasicBlock basicBlock in this.BasicBlocks) {
				foreach(ILNode node in basicBlock.Body) {
					if (node is ILExpression) {
						ILExpression expr = (ILExpression)node;
						if (expr.Operand is ILLabel) {
							BasicBlock target = labelToBasicBlock[(ILLabel)expr.Operand];
							basicBlock.BranchBasicBlock = target;
							target.BasicBlockPredecessors.Add(basicBlock);
						}
						// TODO: Switch
					}
				}
			}
		}
		
		public List<Node> SplitToBasicBlocks(List<ILNode> ast)
		{
			if (ast.Count == 0) return new List<Node>();
			
			List<Node> nodes = new List<Node>();
			
			BasicBlock basicBlock = null;
			
			for(int i = 0; i < ast.Count; i++) {
				if (i == 0 ||
					ast[i] is ILLabel ||
					ast[i - 1] is ILTryCatchBlock ||
					ast[i] is ILTryCatchBlock ||
				    (ast[i - 1] is ILExpression) && ((ILExpression)ast[i - 1]).OpCode.IsBranch() ||
				    (ast[i] is ILExpression)     && ((ILExpression)ast[i]).OpCode.IsBranch())
				{
					BasicBlock oldBB = basicBlock;
					basicBlock = new BasicBlock();
					nodes.Add(basicBlock);
					// Links
					if (oldBB != null && ast[i - 1] is ILExpression && ((ILExpression)ast[i - 1]).OpCode.CanFallThough()) {
						oldBB.FallThroughBasicBlock = basicBlock;
						basicBlock.BasicBlockPredecessors.Add(oldBB);
					}
				}
				if (ast[i] is ILTryCatchBlock) {
					nodes.Add(ConvertTryCatch((ILTryCatchBlock)ast[i]));
				} else {
					basicBlock.Body.Add(ast[i]);
				}
				if (ast[i] is ILLabel) {
					labelToBasicBlock[(ILLabel)ast[i]] = basicBlock;
				}
			}
			
			return nodes;
		}
		
		public TryCatchNode ConvertTryCatch(ILTryCatchBlock ilTryCatch)
		{
			TryCatchNode tryCatch = new TryCatchNode();
			
			Block tryBlock = new Block();
			tryBlock.Childs.AddRange(SplitToBasicBlocks(ilTryCatch.TryBlock));
			tryBlock.MoveTo(tryCatch);
			
			Block finallyBlock = new Block();
			if (ilTryCatch.FinallyBlock != null) {
				finallyBlock.Childs.AddRange(SplitToBasicBlocks(ilTryCatch.FinallyBlock));
			}
			finallyBlock.MoveTo(tryCatch);
			
			foreach(ILTryCatchBlock.CatchBlock cb in ilTryCatch.CatchBlocks) {
				tryCatch.Types.Add(cb.ExceptionType);
				Block catchBlock = new Block();
				catchBlock.Childs.AddRange(SplitToBasicBlocks(cb.Body));
				catchBlock.MoveTo(tryCatch);
			}
			
			return tryCatch;
		}
		
	}
	
	public class TryCatchNode: Node
	{
		public List<TypeReference> Types = new List<TypeReference>();
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
