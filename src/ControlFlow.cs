using System;
using System.Collections;
using System.Collections.Generic;

namespace Decompiler
{
	public class Set<T>: List<T>
	{
		
	}
	
	
	public class BasicBlock
	{
		int id;
		BasicBlockSet owner;
		Set<BasicBlock> predecessors;
		public Set<BasicBlock> successors;
		BasicBlock fallThroughSuccessor;
		BasicBlock branchSuccessor;
		List<StackExpression> body = new List<StackExpression>();
		
		#region Peoperties
		
		public int Id {
			get { return id; }
			set { id = value; }
		}
		
		public BasicBlockSet Owner {
			get { return owner; }
			set { owner = value; }
		}
		
		public Set<BasicBlock> Predecessors {
			get { return predecessors; }
			set { predecessors = value; }
		}
		
		public Set<BasicBlock> Successors {
			get { return successors; }
			set { successors = value; }
		}
		
		public BasicBlock FallThroughSuccessor {
			get { return fallThroughSuccessor; }
			set { fallThroughSuccessor = value; }
		}
		
		public BasicBlock BranchSuccessor {
			get { return branchSuccessor; }
			set { branchSuccessor = value; }
		}
		
		public List<StackExpression> Body {
			get { return body; }
			set { body = value; }
		}
		
		#endregion
		
		public override string ToString()
		{
			//return string.Format("BackBlock {0} ({1} expressions)", id, body.Count);
			return string.Format("BackBlock {0}", id, body.Count);
		}
	}
	
	public enum BasicBlockSetType {
		MethodBody,
		Acyclic,
		Loop,
	}
	
	public class BasicBlockSet
	{
		BasicBlockSet owner;
		BasicBlockSetType type;
		
		object head;
		Set<object> elements = new Set<object>();
		
		Set<BasicBlock> BasicBlockSuccessors;
		
		BasicBlock headBasicBlock {
			get {
				return null;
			}
		}
		
		public BasicBlockSet Owner {
			get { return owner; }
		}
		
		public BasicBlockSetType Type {
			get { return type; }
		}
		
		public object Head {
			get { return head; }
		}
		
		public Set<object> Elements {
			get { return elements; }
		}
		
		
		BasicBlockSet()
		{
			
		}
		
		public BasicBlockSet(object head, object tail)
		{
			if (head == null) throw new ArgumentNullException("head");
			if (tail == null) throw new ArgumentNullException("tail");
			
			BasicBlockSet headAsSet = head as BasicBlockSet;
			BasicBlockSet tailAsSet = tail as BasicBlockSet;
			
			// Add head
			if (head is BasicBlock) {
				this.head = head;
				this.elements.Add(head);
			} else if (headAsSet != null && headAsSet.type == BasicBlockSetType.Acyclic) {
				this.head = headAsSet.head;
				this.elements.AddRange(headAsSet.elements);
			} else if (headAsSet != null && headAsSet.type == BasicBlockSetType.Loop) {
				this.head = headAsSet;
				this.elements.Add(headAsSet);
			} else {
				throw new Exception("Invalid head");
			}
			
			// Add tail
			if (tail is BasicBlock) {
				this.elements.Add(tail);
			} else if (tailAsSet != null && tailAsSet.type == BasicBlockSetType.Acyclic) {
				this.elements.AddRange(tailAsSet.elements);
			} else if (tailAsSet != null && tailAsSet.type == BasicBlockSetType.Loop) {
				this.elements.Add(tailAsSet);
			} else {
				throw new Exception("Invalid tail");
			}
			
			// Get type
			if (tail is BasicBlock) {
				if (((BasicBlock)tail).successors.Contains(this.headBasicBlock)) {
					this.type = BasicBlockSetType.Loop;
				} else {
					this.type = BasicBlockSetType.Acyclic;
				}
			} else if (tailAsSet != null) {
				if (tailAsSet.BasicBlockSuccessors.Contains(this.headBasicBlock)) {
					
				}
			} else {
				throw new Exception("Invalid tail");
			}
		}
		
		public BasicBlockSet(StackExpressionCollection exprs)
		{
			if (exprs.Count == 0) throw new ArgumentException("Count == 0", "exprs");
			
			this.owner = null;
			this.type = BasicBlockSetType.MethodBody;
			
			BasicBlock basicBlock = null;
			int basicBlockId = 1;
			for(int i = 0; i < exprs.Count; i++) {
				// Start new basic block if
				//  - this is first expression
				//  - last expression was branch
				//  - this expression is branch target
				if (i == 0 || exprs[i - 1].BranchTarget != null || exprs[i].BranchesHere.Count > 0){
					basicBlock = new BasicBlock();
					this.elements.Add(basicBlock);
					basicBlock.Id = basicBlockId++;
				}
				basicBlock.Body.Add(exprs[i]);
			}
			
			this.head = this.elements[0];
		}
	}
}
