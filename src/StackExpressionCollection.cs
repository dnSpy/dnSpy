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
			
		}
	}
}
