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
			Dictionary<ByteCode, ByteCodeExpression> exprForByteCode = new Dictionary<ByteCode, ByteCodeExpression>();
			
			foreach(ByteCode byteCode in byteCodeCol) {
				ByteCodeExpression newExpr = new ByteCodeExpression(byteCode);
				
				// If the bytecode pushes anything encapsulate it with stloc
				if (byteCode.PushCount > 0) {
					string name = string.Format("expr{0:X2}", byteCode.Offset);
					ByteCodeExpression stExpr = ByteCodeExpression.Stloc(name);
					stExpr.Arguments.Add(newExpr);
					stExpr.IsSSASR = byteCode.PushCount == 1;
					newExpr = stExpr;
				}
				
				exprForByteCode[byteCode] = newExpr;
				this.Add(newExpr);
			}
			
			// Branching links
			foreach(ByteCodeExpression expr in this) {
				if (expr.Operand is ByteCode) {
					expr.BranchTarget = exprForByteCode[(ByteCode)expr.Operand];
					expr.BranchTarget.BranchesHere.Add(expr);
				}
			}
		}
		
		Dictionary<ByteCodeExpression, object> alreadyDuplicated = new Dictionary<ByteCodeExpression, object>();
		
		public void Optimize()
		{
			for(int i = 0; i < this.Count - 1; i++) {
				if (i < 0) continue;
				
				ByteCodeExpression expr = this[i];
				ByteCodeExpression nextExpr = this[i + 1];
				
				// Duplicate 'dup' expressions
				
				if (expr.OpCode.Code == Code.Stloc &&
				    expr.Arguments[0].OpCode.Code == Code.Dup &&
				    expr.Arguments[0].Arguments[0].IsConstant() &&
				    !alreadyDuplicated.ContainsKey(expr))
				{
					Options.NotifyCollapsingExpression();
					this.Insert(i + 1, expr.Clone());
					this[i].IsSSASR = true;
					this[i + 1].IsSSASR = true;
					alreadyDuplicated.Add(this[i], null);
					alreadyDuplicated.Add(this[i + 1], null);
					continue;
				}
				
				// Try to in-line stloc into following expression
				
				if (expr.OpCode.Code == Code.Stloc &&
				    expr.IsSSASR &&
				    !nextExpr.IsBranchTarget) {
					
					// If the next expression is stloc, look inside 
					if (nextExpr.OpCode.Code == Code.Stloc &&
					    nextExpr.Arguments[0].OpCode.Code != Code.Ldloc) {
						nextExpr = nextExpr.Arguments[0];
					}
					
					// Find the use of the 'expr'
					for(int j = 0; j < nextExpr.Arguments.Count; j++) {
						ByteCodeExpression arg = nextExpr.Arguments[j];
						
						if (arg.OpCode.Code == Code.Ldloc &&
						    ((VariableDefinition)arg.Operand).Name == ((VariableDefinition)expr.Operand).Name) {
							// Found
							Options.NotifyCollapsingExpression();
							this.RemoveAt(i); i--; // Remove the stloc
							nextExpr.Arguments[j] = expr.Arguments[0]; // Inline the stloc body
							// Move branch links
							foreach(ByteCodeExpression predExpr in expr.BranchesHere) {
								predExpr.BranchTarget = this[i + 1];
								predExpr.BranchTarget.BranchesHere.Add(predExpr);
							}
							
							i--; // Try the same index again
							break;
						}
						if (arg.OpCode.Code != Code.Ldloc) {
							// This argument might have side effects so we can not
							// move the 'expr' after it.  Terminate
							break;
						}
					}
				}
			}
		}
	}
}
