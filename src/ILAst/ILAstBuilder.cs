using System;
using System.Collections.Generic;
using System.Text;

using Decompiler.Mono.Cecil.Rocks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Cecil = Mono.Cecil;

namespace Decompiler
{
	public class ILAstBuilder
	{
		class ByteCode
		{
			public Instruction Instruction;		
			public ILStack     StackBefore;
			
			public string TmpVarName { get { return "expr" + this.Instruction.Offset; } }
			
			public override string ToString()
			{
				return string.Format("[{0}, [{1}]]", Instruction, StackBefore);
			}
		}
		
		class ILStack
		{
			public class Slot
			{
				public ByteCode PushedBy;
				public TypeReference Type;
				
				public Slot(ByteCode byteCode, TypeReference type)
				{
					this.PushedBy = byteCode;
					this.Type = type;
				}
			}
			
			public List<Slot> Items = new List<Slot>();
			
			public ILStack Clone()
			{
				ILStack clone = new ILStack();
				foreach(Slot s in this.Items) {
					clone.Items.Add(new Slot(s.PushedBy, s.Type));
				}
				return clone;
			}
			
			public void MergeInto(ref ILStack other, out bool changed)
			{
				if (other == null) {
					other = this;
					changed = true;
				} else {
					changed = false;
				}
			}
			
			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				bool first = true;
				foreach (Slot s in this.Items) {
					if (!first) sb.Append(", ");
					sb.Append(s.PushedBy.Instruction.Offset.ToString("X"));
					first = false;
				}
				return sb.ToString();
			}
		}
		
		public static List<ILExpression> Build(MethodDefinition methodDef)
		{
			List<ByteCode> body = new List<ByteCode>();
			
			Dictionary<Instruction, ByteCode> instToByteCode = new Dictionary<Instruction, ByteCode>();
			
			// Create the IL body for analisys
			foreach(Instruction inst in methodDef.Body.Instructions) {
				ByteCode byteCode = new ByteCode();
				byteCode.Instruction = inst;
				body.Add(byteCode);
				instToByteCode.Add(inst, byteCode);
			}
			
			// Stack analisys
			if (body.Count > 0) {
				Queue<ByteCode> agenda = new Queue<ByteCode>();
				
				// Add known states 
				body[0].StackBefore = new ILStack();
				agenda.Enqueue(body[0]);
				
				if(methodDef.Body.HasExceptionHandlers) {
					foreach(ExceptionHandler ex in methodDef.Body.ExceptionHandlers) {
						ByteCode tryStart = instToByteCode[ex.TryStart];
						tryStart.StackBefore = new ILStack();
						agenda.Enqueue(tryStart);
						
						ByteCode handlerStart = instToByteCode[ex.HandlerStart];
						handlerStart.StackBefore = new ILStack();
						handlerStart.StackBefore.Items.Add(new ILStack.Slot(null, MyRocks.TypeException));
						agenda.Enqueue(handlerStart);
					}
				}
				
				// Process agenda
				while(agenda.Count > 0) {
					ByteCode byteCode = agenda.Dequeue();
					
					// What is the effect of the instruction on the stack?
					ILStack newStack = byteCode.StackBefore.Clone();
					int popCount = byteCode.Instruction.GetPopCount(methodDef, byteCode.StackBefore.Items.Count);
					List<TypeReference> typeArgs = new List<TypeReference>();
					for (int i = newStack.Items.Count - popCount; i < newStack.Items.Count; i++) {
						typeArgs.Add(newStack.Items[i].Type);
					}
					TypeReference type;
					try {
						type = byteCode.Instruction.GetTypeInternal(methodDef, typeArgs);
					} catch {
						type = MyRocks.TypeObject;
					}
					if (popCount > 0) {
						newStack.Items.RemoveRange(newStack.Items.Count - popCount, popCount);
					}
					int pushCount = byteCode.Instruction.GetPushCount();
					for (int i = 0; i < pushCount; i++) {
						newStack.Items.Add(new ILStack.Slot(byteCode, type));
					}
					
					// Apply the state to any successors
					if (byteCode.Instruction.OpCode.CanFallThough()) {
						ByteCode next = instToByteCode[byteCode.Instruction.Next];
						bool changed;
						newStack.MergeInto(ref next.StackBefore, out changed);
						if (changed) agenda.Enqueue(next);
					}
					if (byteCode.Instruction.OpCode.IsBranch()) {
						object operand = byteCode.Instruction.Operand;
						if (operand is Instruction) {
							ByteCode next = instToByteCode[(Instruction)operand];
							bool changed;
							newStack.MergeInto(ref next.StackBefore, out changed);
							if (changed) agenda.Enqueue(next);
						} else {
							foreach(Instruction inst in (Instruction[])operand) {
								ByteCode next = instToByteCode[inst];
								bool changed;
								newStack.MergeInto(ref next.StackBefore, out changed);
								if (changed) agenda.Enqueue(next);
							}
						}
					}
				}
			}
			
			List<ILExpression> ast = new List<ILExpression>();
			Dictionary<ByteCode, ILExpression> byteCodeToExpr = new Dictionary<ByteCode, ILExpression>();
			
			// Convert stack-based IL code to ILAst tree
			foreach(ByteCode byteCode in body) {
				ILExpression expr = new ILExpression(byteCode.Instruction.OpCode, byteCode.Instruction.Operand);
				
				byteCodeToExpr[byteCode] = expr;
				
				// Reference arguments using temporary variables
				ILStack stack = byteCode.StackBefore;
				for (int i = stack.Items.Count - byteCode.Instruction.GetPopCount(methodDef, byteCode.StackBefore.Items.Count); i < stack.Items.Count; i++) {
					ILExpression ldExpr = new ILExpression(OpCodes.Ldloc, new VariableDefinition(stack.Items[i].PushedBy.TmpVarName, null));
					ldExpr.IsTempLdloc = true;
					expr.Arguments.Add(ldExpr);
				}
			
				// If the bytecode pushes anything store the result in temporary variable
				int pushCount = byteCode.Instruction.GetPushCount();
				if (pushCount > 0) {
					ILExpression stExpr = new ILExpression(OpCodes.Stloc, new VariableDefinition(byteCode.TmpVarName, null));
					stExpr.Arguments.Add(expr);
					stExpr.IsTempStloc = true;
					stExpr.RefCount = pushCount;
					expr.Partent = stExpr;
					expr = stExpr;
				}
				
				ast.Add(expr);
			}
			
			// Convert branch operands
			foreach(ILExpression expr in ast) {
				if (expr.Operand is Instruction) {
					ILExpression brTarget = byteCodeToExpr[instToByteCode[(Instruction)expr.Operand]];
					expr.Operand = brTarget;
					brTarget.IsBranchTarget = true;
					if (brTarget.Partent != null) {
						brTarget.Partent.IsBranchTarget = true;
					}
				}
			}
			
			// Try to in-line stloc / ldloc pairs
			for(int i = 0; i < ast.Count - 1; i++) {
				ILExpression expr = ast[i];
				ILExpression nextExpr = ast[i + 1];
				
				if (expr.IsTempStloc && !nextExpr.IsBranchTarget) {
					
					// If the next expression is stloc, look inside 
					if (nextExpr.IsTempStloc) {
						nextExpr = nextExpr.Arguments[0];
					}
					
					// Find the use of the 'expr'
					for(int j = 0; j < nextExpr.Arguments.Count; j++) {
						ILExpression arg = nextExpr.Arguments[j];
						
						if (!arg.IsTempLdloc) {
							break; // This argument might have side effects so we can not move the 'expr' after it.
						} else {
							if (((VariableDefinition)arg.Operand).Name == ((VariableDefinition)expr.Operand).Name) {
								expr.RefCount--;
								if (expr.RefCount <= 0) {
									ast.RemoveAt(i);
								}
								nextExpr.Arguments[j] = expr.Arguments[0]; // Inline the stloc body
								if (expr.IsBranchTarget) {
									nextExpr.IsBranchTarget = true;
									if (nextExpr.Partent != null) {
										nextExpr.Partent.IsBranchTarget = true;
									}
								}
								i = Math.Max(0, i - 2); // Try the same index again
								break;  // Found
							}
						}
					}
				}
			}
			
			return ast;
		}
	}
}
