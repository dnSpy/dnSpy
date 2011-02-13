using System;
using System.Collections.Generic;
using System.Text;

using Decompiler.ControlFlow;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Cecil = Mono.Cecil;

namespace Decompiler
{
	public abstract class ILNode
	{
		public IEnumerable<T> GetChildrenRecursive<T>() where T: ILNode
		{
			Stack<IEnumerator<ILNode>> stack = new Stack<IEnumerator<ILNode>>();
			try {
				stack.Push(GetChildren().GetEnumerator());
				while (stack.Count > 0) {
					while (stack.Peek().MoveNext()) {
						ILNode element = stack.Peek().Current;
						if (element is T)
							yield return (T)element;
						IEnumerable<ILNode> children = element.GetChildren();
						if (children != null) {
							stack.Push(children.GetEnumerator());
						}
					}
					stack.Pop().Dispose();
				}
			} finally {
				while (stack.Count > 0) {
					stack.Pop().Dispose();
				}
			}
		}
		
		public virtual IEnumerable<ILNode> GetChildren()
		{
			return null;
		}
	}
	
	public class ILLabel: ILNode
	{
		public string Name;

		public override string ToString()
		{
			return Name + ":";
		}
	}
	
	public class ILBlock: ILNode
	{
		public List<ILNode> Body;
		
		public ILBlock(params ILNode[] body)
		{
			this.Body = new List<ILNode>(body);
		}
		
		public ILBlock(List<ILNode> body)
		{
			this.Body = body;
		}
		
		public override IEnumerable<ILNode> GetChildren()
		{
			return this.Body;
		}
	}
	
	public class ILTryCatchBlock: ILNode
	{
		public class CatchBlock: ILBlock
		{
			public TypeReference ExceptionType;
		}
		
		public ILBlock          TryBlock;
		public List<CatchBlock> CatchBlocks;
		public ILBlock          FinallyBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			yield return this.TryBlock;
			foreach (var catchBlock in this.CatchBlocks) {
				yield return catchBlock;
			}
			yield return this.FinallyBlock;
		}
		
		public override string ToString()
		{
			return "Try-Catch{}";
		}
	}
	
	public class ILVariable
	{
		public string Name;
		public bool   IsGenerated;
		
		public override string ToString()
		{
			return Name;
		}
	}
	
	public class ILExpression: ILNode
	{
		public OpCode OpCode { get; set; }
		public object Operand { get; set; }
		public List<ILExpression> Arguments { get; set; }
		
		public ILExpression(OpCode opCode, object operand, params ILExpression[] args)
		{
			this.OpCode = opCode;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
		}
		
		public IEnumerable<ILLabel> GetBranchTargets()
		{
			if (this.Operand is ILLabel) {
				return new ILLabel[] { (ILLabel)this.Operand };
			} else if (this.Operand is ILLabel[]) {
				return (ILLabel[])this.Operand;
			} else {
				return null;
			}
		}
		
		public override IEnumerable<ILNode> GetChildren()
		{
			return Arguments;
		}
		
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(OpCode.Name);
			sb.Append('(');
			bool first = true;
			if (Operand != null) {
				sb.Append(Operand.ToString());
				first = false;
			}
			foreach (ILExpression arg in this.Arguments) {
				if (!first) sb.Append(",");
				sb.Append(arg.ToString());
				first = false;
			}
			sb.Append(')');
			return sb.ToString();
		}
	}
	
	public class ILLoop: ILNode
	{
		public ILBlock ContentBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			yield return ContentBlock;
		}
	}
	
	public class ILCondition: ILNode
	{
		public ILBlock ConditionBlock;
		public ILBlock Block1;
		public ILBlock Block2;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			yield return ConditionBlock;
			yield return Block1;
			yield return Block2;
		}
	}
}
