using System;
using System.Collections.Generic;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class ByteCodeExpressionCollection: List<ByteCodeExpression>
	{
		public ByteCodeExpressionCollection(ByteCodeCollection byteCodeCol)
		{
			foreach(ByteCode bc in byteCodeCol) {
				this.Add(new ByteCodeExpression(this, bc));
			}
		}
		
		public void Optimize()
		{
			for(int i = 1; i < this.Count; i++) {
				if (i == 0) continue;
				ByteCodeExpression prevExpr = this[i - 1];
				ByteCodeExpression expr = this[i];
				
				if (expr.PopCount > 0 && // This expr needs some more arguments
				    !expr.IsBranchTarget &&
				    prevExpr.IsClosed)
				{
					Options.NotifyCollapsingExpression();
					this.RemoveAt(i - 1); i--;
					expr.LastArguments.Insert(0, prevExpr);
					i--;
				}
			}
		}
	}
}
