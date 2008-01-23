using System;
using System.Collections.Generic;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class StackExpression
	{
		StackExpressionCollection owner;
		ByteCode lastByteCode;
		List<StackExpression> lastArguments = new List<StackExpression>();
		
		public StackExpressionCollection Owner {
			get { return owner; }
		}
		
		public ByteCode LastByteCode {
			get { return lastByteCode; }
		}
		
		// A list of closed expression for last arguments
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
				return this.LastByteCode.StackAfter;
			}
		}
		
		/// <summary>
		/// Expression is closed if it has no inputs and has exactly one output
		/// </summary>
		public bool IsClosed {
			get {
				return this.PopCount == 0 &&
				       this.PushCount == 1;
			}
		}
		
		public List<StackExpression> BranchesHere {
			get {
				List<StackExpression> branchesHere = new List<StackExpression>();
				foreach(ByteCode byteCode in this.FirstByteCode.BranchesHere) {
					branchesHere.Add(byteCode.Owner);
				}
				return branchesHere;
			}
		}
		
		public StackExpression BranchTarget {
			get {
				if (this.lastByteCode.BranchTarget == null) {
					return null;
				} else {
					return this.lastByteCode.BranchTarget.Owner;
				}
			}
		}
		
		public bool IsBranchTarget {
			get {
				return this.FirstByteCode.BranchesHere.Count > 0;
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
				stackSize -= lastByteCode.PopCount;
				minStackSize = Math.Min(minStackSize, stackSize);
				stackSize += lastByteCode.PushCount;
			}
			popCount = -minStackSize;
			pushCount = stackSize - minStackSize;
		}
		
		public ByteCode FirstByteCode {
			get {
				if (lastArguments.Count > 0) {
					return lastArguments[0].FirstByteCode;
				} else {
					return this.LastByteCode;
				}
			}
		}
		
		public StackExpression(StackExpressionCollection owner, ByteCode lastByteCode)
		{
			this.owner = owner;
			this.lastByteCode = lastByteCode;
			this.lastByteCode.Owner = this;
		}
		
		public bool MustBeParenthesized {
			get {
				switch(this.LastByteCode.OpCode.Code) {
					#region Arithmetic
						case Code.Neg:
						case Code.Not:
					#endregion
					#region Arrays
						case Code.Newarr:
						case Code.Ldlen:      
						case Code.Ldelem_I:   
						case Code.Ldelem_I1:  
						case Code.Ldelem_I2:  
						case Code.Ldelem_I4:  
						case Code.Ldelem_I8:  
						case Code.Ldelem_U1:  
						case Code.Ldelem_U2:  
						case Code.Ldelem_U4:  
						case Code.Ldelem_R4:  
						case Code.Ldelem_R8:  
						case Code.Ldelem_Ref: 
					#endregion
					case Code.Call:
					case Code.Ldarg: 
					case Code.Ldc_I4:
					case Code.Ldc_I8:
					case Code.Ldc_R4:
					case Code.Ldc_R8:
					case Code.Ldloc: 
					case Code.Ldnull:
					case Code.Ldstr: 
						return false;
					default: 
						return true;
				}
			}
		}
	}
}
