using System;
using System.Collections.Generic;

namespace Decompiler.ControlFlow
{
	public class MethodBodyGraph: Node
	{
		// TODO: Add links between the generated BasicBlocks
		public MethodBodyGraph(StackExpressionCollection exprs): base(null)
		{
			if (exprs.Count == 0) throw new ArgumentException("Count == 0", "exprs");
			
			BasicBlock basicBlock = null;
			int basicBlockId = 1;
			for(int i = 0; i < exprs.Count; i++) {
				// Start new basic block if
				//  - this is first expression
				//  - last expression was branch
				//  - this expression is branch target
				if (i == 0 || exprs[i - 1].BranchTarget != null || exprs[i].BranchesHere.Count > 0){
					basicBlock = new BasicBlock(this, basicBlockId++);
					this.Childs.Add(basicBlock);
				}
				basicBlock.Body.Add(exprs[i]);
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
		int id;
		List<StackExpression> body = new List<StackExpression>();
		
		public int Id {
			get { return id; }
		}
		
		public List<StackExpression> Body {
			get { return body; }
		}
		
		public BasicBlock(Node parent, int id): base(parent)
		{
			this.id = id;
		}
		
		public override string ToString()
		{
			return string.Format("BasicBlock {0}", id, body.Count);
		}
	}
}
