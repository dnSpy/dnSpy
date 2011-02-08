using System;
using System.Collections.Generic;
using System.Text;

using Decompiler.ControlFlow;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Cecil = Mono.Cecil;

namespace Decompiler
{
	public class ILNode
	{
	}
	
	public class ILLabel: ILNode
	{
		public string Name;

		public override string ToString()
		{
			return Name + ":";
		}
	}
	
	public class ILTryCatchBlock: ILNode
	{
		public class CatchBlock
		{
			public TypeReference ExceptionType;
			public List<ILNode>  Body;
		}
		
		public List<ILNode>     TryBlock;
		public List<CatchBlock> CatchBlocks;
		public List<ILNode>     FinallyBlock;
		
		public override string ToString()
		{
			return "Try-Catch{}";
		}
	}
	
	public class ILStackVariable
	{
		public string Name;
		public int RefCount;
		
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
}
