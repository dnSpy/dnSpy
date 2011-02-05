using System;
using System.Collections.Generic;
using System.Text;

using Decompiler.ControlFlow;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Cecil = Mono.Cecil;

namespace Decompiler
{
	public class ILExpression
	{		
		public OpCode OpCode { get; set; }
		public object Operand { get; set; }
		public List<ILExpression> Arguments { get; set; }
		public bool IsTempStloc { get; set; }
		public bool IsTempLdloc { get; set; }
		public int  RefCount { get; set; }
		public bool IsBranchTarget { get; set; }
		public BasicBlock BasicBlock { get; set; }
		
		// HACK: Do preoperly
		public ILExpression Partent { get; set; }
		
		public ILExpression(OpCode opCode, object operand, params ILExpression[] args)
		{
			this.OpCode = opCode;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
		}
		
		public void SetBasicBlock(BasicBlock bb)
		{
			this.BasicBlock = bb;
			foreach(ILExpression arg in Arguments) {
				arg.SetBasicBlock(bb);
			}
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
