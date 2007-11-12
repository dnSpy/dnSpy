using System;
using System.Collections.Generic;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class StackExpressionCollection: List<StackExpression>
	{
		public StackExpressionCollection(ByteCodeCollection byteCodeCol)
		{
			foreach(ByteCode bc in byteCodeCol) {
				this.Add(new StackExpression(bc));
			}
		}
		
		public void Optimize()
		{
			for(int i = 1; i < this.Count; i++) {
				StackExpression prevExpr = this[i - 1];
				StackExpression expr = this[i];
				
				if (expr.PopCount > 0 && // This expr needs some more arguments
				    !expr.IsBranchTarget &&
				    prevExpr.IsClosed)
				{
					this.RemoveAt(i - 1); i--;
					expr.LastArguments.Insert(0, prevExpr);
					i--;
				}
			}
		}
	}
}
