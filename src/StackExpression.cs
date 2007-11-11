using System;
using System.Collections.Generic;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class StackExpression
	{
		ByteCode expressionByteCode;
		List<StackExpression> lastArguments = new List<StackExpression>();
		
		public ByteCode ExpressionByteCode {
			get { return expressionByteCode; }
		}
		
		public List<StackExpression> LastArguments {
			get { return lastArguments; }
		}
		
		public CilStack StackBefore {
			get {
				return this.FirstByteCode.StackBefore;
			}
		}
		
		public CilStack StackAfter {
			get {
				return this.ExpressionByteCode.StackAfter;
			}
		}
		
		public int PopCount {
			get {
				int popCount;
				int pushCount;
				SimulateStackSize(out popCount, out pushCount);
				return popCount;
			}
		}
		
		public int PushCount {
			get {
				int popCount;
				int pushCount;
				SimulateStackSize(out popCount, out pushCount);
				return pushCount;
			}
		}
		
		void SimulateStackSize(out int popCount, out int pushCount)
		{
			int stackSize = 0;
			int minStackSize = 0;
			foreach(StackExpression expr in lastArguments) {
				stackSize -= expr.PopCount;
				minStackSize = Math.Min(minStackSize, stackSize);
				stackSize += expr.PushCount;
			}
			{
				stackSize -= expressionByteCode.PopCount;
				minStackSize = Math.Min(minStackSize, stackSize);
				stackSize += expressionByteCode.PushCount;
			}
			popCount = -minStackSize;
			pushCount = stackSize - minStackSize;
		}
		
		public ByteCode FirstByteCode {
			get {
				if (lastArguments.Count > 0) {
					return lastArguments[0].FirstByteCode;
				} else {
					return this.ExpressionByteCode;
				}
			}
		}
		
		public StackExpression(ByteCode expressionByteCode)
		{
			this.expressionByteCode = expressionByteCode;
		}
	}
}
