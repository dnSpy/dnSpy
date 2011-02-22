using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Cecil = Mono.Cecil;

namespace Decompiler
{
	public class ILAstBuilder
	{
		class StackSlot
		{
			public List<ByteCode> PushedBy;  // One of those
			public ILVariable     LoadFrom;  // Where can we get the value from in AST
			
			public StackSlot()
			{
			}
			
			public StackSlot(ByteCode pushedBy)
			{
				this.PushedBy = new List<ByteCode>(1);
				this.PushedBy.Add(pushedBy);
			}
			
			public static List<StackSlot> CloneStack(List<StackSlot> stack, int? popCount)
			{
				List<StackSlot> clone = new List<StackSlot>();
				if (popCount.HasValue) {
					if (popCount.Value > stack.Count) {
						throw new Exception("Can not pop - the stack is empty");
					}
					for(int i = 0; i < stack.Count - popCount.Value; i++) {
						clone.Add(new StackSlot() { PushedBy = new List<ByteCode>(stack[i].PushedBy) });
					}
				}
				return clone;
			}
		}
		
		class VariableSlot
		{
			public static List<ByteCode> Empty = new List<ByteCode>();
			
			public List<ByteCode> StoredBy = Empty;  // One of those
			public bool           StoredByAll;       // Overestimate which is useful for exceptional control flow.
			
			public static VariableSlot[] CloneVariableState(VariableSlot[] state)
			{
				VariableSlot[] clone = new ILAstBuilder.VariableSlot[state.Length];
				if (VariableSlot.Empty.Count > 0)
					throw new Exception("Constant data corrupted");
				for (int i = 0; i < clone.Length; i++) {
					VariableSlot varSlot = state[i];
					clone[i] = new VariableSlot() {
						StoredBy = varSlot.StoredBy.Count == 0 ? VariableSlot.Empty : new List<ByteCode>(varSlot.StoredBy),
						StoredByAll  = varSlot.StoredByAll
					};
				}
				return clone;
			}
			
			public static VariableSlot[] MakeEmptyState(int varCount)
			{
				VariableSlot[] emptyVariableState = new VariableSlot[varCount];
				for (int i = 0; i < emptyVariableState.Length; i++) {
					emptyVariableState[i] = new VariableSlot();
				}
				return emptyVariableState;
			}
			
			public static VariableSlot[] MakeFullState(int varCount)
			{
				VariableSlot[] unknownVariableState = new VariableSlot[varCount];
				for (int i = 0; i < unknownVariableState.Length; i++) {
					unknownVariableState[i] = new VariableSlot() { StoredByAll = true };
				}
				return unknownVariableState;
			}
		}
		
		class ByteCode
		{
			public ILLabel  Label;      // Non-null only if needed
			public int      Offset;
			public int      EndOffset;
			public ILCode   Code;
			public object   Operand;
			public int?     PopCount;   // Null means pop all
			public int      PushCount;
			public string   Name { get { return "IL_" + this.Offset.ToString("X2"); } }
			public ByteCode Next;
			public Instruction[]    Prefixes;        // Non-null only if needed
			public List<StackSlot>  StackBefore;
			public List<ILVariable> StoreTo;         // Store result of instruction to those AST variables
			public VariableSlot[]   VariablesBefore;
			
			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				
				// Label
				sb.Append(this.Name);
				sb.Append(':');
				if (this.Label != null)
					sb.Append('*');
				
				// Name
				sb.Append(' ');
				if (this.Prefixes != null) {
					foreach (var prefix in this.Prefixes) {
						sb.Append(prefix.OpCode.Name);
						sb.Append(' ');
					}
				}
				sb.Append(this.Code.GetName());
				
				if (this.Operand != null) {
					sb.Append(' ');
					if (this.Operand is Instruction) {
						sb.Append("IL_" + ((Instruction)this.Operand).Offset.ToString("X2"));
					} else if (this.Operand is Instruction[]) {
						foreach(Instruction inst in (Instruction[])this.Operand) {
							sb.Append("IL_" + inst.Offset.ToString("X2"));
							sb.Append(" ");
						}
					} else if (this.Operand is ILLabel) {
						sb.Append(((ILLabel)this.Operand).Name);
					} else if (this.Operand is ILLabel[]) {
						foreach(ILLabel label in (ILLabel[])this.Operand) {
							sb.Append(label.Name);
							sb.Append(" ");
						}
					} else {
						sb.Append(this.Operand.ToString());
					}
				}
				
				if (this.StackBefore != null) {
					sb.Append(" StackBefore={");
					bool first = true;
					foreach (StackSlot slot in this.StackBefore) {
						if (!first) sb.Append(",");
						bool first2 = true;
						foreach(ByteCode pushedBy in slot.PushedBy) {
							if (!first2) sb.Append("|");
							sb.AppendFormat("IL_{0:X2}", pushedBy.Offset);
							first2 = false;
						}
						first = false;
					}
					sb.Append("}");
				}
				
				if (this.StoreTo != null && this.StoreTo.Count > 0) {
					sb.Append(" StoreTo={");
					bool first = true;
					foreach (ILVariable stackVar in this.StoreTo) {
						if (!first) sb.Append(",");
						sb.Append(stackVar.Name);
						first = false;
					}
					sb.Append("}");
				}
				
				if (this.VariablesBefore != null) {
					sb.Append(" VarsBefore={");
					bool first = true;
					foreach (VariableSlot varSlot in this.VariablesBefore) {
						if (!first) sb.Append(",");
						if (varSlot.StoredByAll) {
							sb.Append("*");
						} else if (varSlot.StoredBy.Count == 0) {
							sb.Append("_");
						} else {
							bool first2 = true;
							foreach (ByteCode storedBy in varSlot.StoredBy) {
								if (!first2) sb.Append("|");
								sb.AppendFormat("IL_{0:X2}", storedBy.Offset);
								first2 = false;
							}
						}
						first = false;
					}
					sb.Append("}");
				}
				
				return sb.ToString();
			}
		}
		
		MethodDefinition methodDef;
		bool optimize;
		
		Dictionary<Instruction, ByteCode> instrToByteCode = new Dictionary<Instruction, ByteCode>();
		Dictionary<ILVariable, bool> allowInline = new Dictionary<ILVariable, bool>();
		
		// Virtual instructions to load exception on stack
		Dictionary<ExceptionHandler, ByteCode> ldexceptions = new Dictionary<ExceptionHandler, ILAstBuilder.ByteCode>();
		
		public List<ILVariable> Variables;
		
		public List<ILNode> Build(MethodDefinition methodDef, bool optimize)
		{
			this.methodDef = methodDef;
			this.optimize = optimize;
			
			if (methodDef.Body.Instructions.Count == 0) return new List<ILNode>();
			
			List<ByteCode> body = StackAnalysis(methodDef);
			
			List<ILNode> ast = ConvertToAst(body, new HashSet<ExceptionHandler>(methodDef.Body.ExceptionHandlers));
			
			return ast;
		}
		
		List<ByteCode> StackAnalysis(MethodDefinition methodDef)
		{
			// Create temporary structure for the stack analysis
			List<ByteCode> body = new List<ByteCode>(methodDef.Body.Instructions.Count);
			List<Instruction> prefixes = null;
			foreach(Instruction inst in methodDef.Body.Instructions) {
				if (inst.OpCode.OpCodeType == OpCodeType.Prefix) {
					if (prefixes == null)
						prefixes = new List<Instruction>(1);
					prefixes.Add(inst);
					continue;
				}
				ILCode code  = (ILCode)inst.OpCode.Code;
				object operand = inst.Operand;
				ILCodeUtil.ExpandMacro(ref code, ref operand, methodDef.Body);
				ByteCode byteCode = new ByteCode() {
					Offset      = inst.Offset,
					EndOffset   = inst.Next != null ? inst.Next.Offset : methodDef.Body.CodeSize,
					Code        = code,
					Operand     = operand,
					PopCount    = inst.GetPopCount(),
					PushCount   = inst.GetPushCount()
				};
				if (prefixes != null) {
					instrToByteCode[prefixes[0]] = byteCode;
					byteCode.Offset = prefixes[0].Offset;
					byteCode.Prefixes = prefixes.ToArray();
					prefixes = null;
				} else {
					instrToByteCode[inst] = byteCode;
				}
				body.Add(byteCode);
			}
			for (int i = 0; i < body.Count - 1; i++) {
				body[i].Next = body[i + 1];
			}
			
			Stack<ByteCode> agenda = new Stack<ByteCode>();
			
			int varCount = methodDef.Body.Variables.Count;
			
			// Add known states
			if(methodDef.Body.HasExceptionHandlers) {
				foreach(ExceptionHandler ex in methodDef.Body.ExceptionHandlers) {
					ByteCode handlerStart = instrToByteCode[ex.HandlerType == ExceptionHandlerType.Filter ? ex.FilterStart : ex.HandlerStart];
					handlerStart.StackBefore = new List<StackSlot>();
					if (ex.HandlerType == ExceptionHandlerType.Catch || ex.HandlerType == ExceptionHandlerType.Filter) {
						ByteCode ldexception = new ByteCode() {
							Code = ILCode.Ldexception,
							Operand = ex.CatchType,
							PopCount = 0,
							PushCount = 1
						};
						ldexceptions[ex] = ldexception;
						handlerStart.StackBefore.Add(new StackSlot(ldexception));
					}
					handlerStart.VariablesBefore = VariableSlot.MakeFullState(varCount);
					agenda.Push(handlerStart);
				}
			}
			
			body[0].StackBefore = new List<StackSlot>();
			body[0].VariablesBefore = VariableSlot.MakeEmptyState(varCount);
			agenda.Push(body[0]);
			
			// Process agenda
			while(agenda.Count > 0) {
				ByteCode byteCode = agenda.Pop();
				
				// Calculate new stack
				List<StackSlot> newStack = StackSlot.CloneStack(byteCode.StackBefore, byteCode.PopCount);
				for (int i = 0; i < byteCode.PushCount; i++) {
					newStack.Add(new StackSlot(byteCode));
				}
				
				// Calculate new variable state
				VariableSlot[] newVariableState = VariableSlot.CloneVariableState(byteCode.VariablesBefore);
				if (byteCode.Code == ILCode.Stloc) {
					int varIndex = ((VariableReference)byteCode.Operand).Index;
					newVariableState[varIndex].StoredBy = new List<ByteCode>(1) { byteCode };
					newVariableState[varIndex].StoredByAll = false;
				}
				
				// After the leave, finally block might have touched the variables
				if (byteCode.Code == ILCode.Leave) {
					newVariableState = VariableSlot.MakeFullState(varCount);
				}
				
				// Find all successors
				List<ByteCode> branchTargets = new List<ByteCode>();
				if (byteCode.Code.CanFallThough()) {
					branchTargets.Add(byteCode.Next);
				}
				if (byteCode.Operand is Instruction[]) {
					foreach(Instruction inst in (Instruction[])byteCode.Operand) {
						ByteCode target = instrToByteCode[inst];
						branchTargets.Add(target);
						// The target of a branch must have label
						if (target.Label == null) {
							target.Label = new ILLabel() { Name = target.Name };
						}
					}
				} else if (byteCode.Operand is Instruction) {
					ByteCode target = instrToByteCode[(Instruction)byteCode.Operand];
					branchTargets.Add(target);
					// The target of a branch must have label
					if (target.Label == null) {
						target.Label = new ILLabel() { Name = target.Name };
					}
				}
				
				// Apply the state to successors
				foreach (ByteCode branchTarget in branchTargets) {
					if (branchTarget.StackBefore == null && branchTarget.VariablesBefore == null) {
						if (branchTargets.Count == 1) {
							branchTarget.StackBefore = newStack;
							branchTarget.VariablesBefore = newVariableState;
						} else {
							// Do not share data for several bytecodes
							branchTarget.StackBefore = StackSlot.CloneStack(newStack, 0);
							branchTarget.VariablesBefore = VariableSlot.CloneVariableState(newVariableState);
						}
						agenda.Push(branchTarget);
					} else {
						if (branchTarget.StackBefore.Count != newStack.Count) {
							throw new Exception("Inconsistent stack size at " + byteCode.Name);
						}
						
						// Be careful not to change our new data - it might be reused for several branch targets.
						// In general, be careful that two bytecodes never share data structures.
						
						bool modified = false;
						
						// Merge stacks - modify the target
						for (int i = 0; i < newStack.Count; i++) {
							List<ByteCode> oldPushedBy = branchTarget.StackBefore[i].PushedBy;
							List<ByteCode> newPushedBy = oldPushedBy.Union(newStack[i].PushedBy).ToList();
							if (newPushedBy.Count > oldPushedBy.Count) {
								branchTarget.StackBefore[i].PushedBy = newPushedBy;
								modified = true;
							}
						}
						
						// Merge variables - modify the target
						for (int i = 0; i < newVariableState.Length; i++) {
							VariableSlot oldSlot = branchTarget.VariablesBefore[i];
							VariableSlot newSlot = newVariableState[i];
							// All can not be unioned further
							if (!oldSlot.StoredByAll) {
								if (newSlot.StoredByAll) {
									oldSlot.StoredByAll = true;
									modified = true;
								} else {
									List<ByteCode> oldStoredBy = oldSlot.StoredBy;
									List<ByteCode> newStoredBy = oldStoredBy.Union(newSlot.StoredBy).ToList();
									if (newStoredBy.Count > oldStoredBy.Count) {
										oldSlot.StoredBy = newStoredBy;
										modified = true;
									}
								}
							}
						}
						
						if (modified) {
							agenda.Push(branchTarget);
						}
					}
				}
			}
			
			// Genertate temporary variables to replace stack
			foreach(ByteCode byteCode in body) {
				if (byteCode.StackBefore == null)
					continue;
				
				int argIdx = 0;
				int popCount = byteCode.PopCount ?? byteCode.StackBefore.Count;
				for (int i = byteCode.StackBefore.Count - popCount; i < byteCode.StackBefore.Count; i++) {
					StackSlot arg = byteCode.StackBefore[i];
					ILVariable tmpVar = new ILVariable() { Name = string.Format("arg_{0:X2}_{1}", byteCode.Offset, argIdx), IsGenerated = true };
					arg.LoadFrom = tmpVar;
					foreach(ByteCode pushedBy in arg.PushedBy) {
						if (pushedBy.StoreTo == null) {
							pushedBy.StoreTo = new List<ILVariable>(1);
						}
						pushedBy.StoreTo.Add(tmpVar);
					}
					if (arg.PushedBy.Count == 1) {
						allowInline[tmpVar] = true;
					}
					argIdx++;
				}
			}
			
			// Convert local varibles
			Variables = methodDef.Body.Variables.Select(v => new ILVariable() { Name = string.IsNullOrEmpty(v.Name) ?  "var_" + v.Index : v.Name, Type = v.VariableType }).ToList();
			int[] numReads  = new int[Variables.Count];
			int[] numWrites = new int[Variables.Count];
			foreach(ByteCode byteCode in body) {
				if (byteCode.Code == ILCode.Ldloc) {
					int index = ((VariableDefinition)byteCode.Operand).Index;
					byteCode.Operand = Variables[index];
					numReads[index]++;
				} else if (byteCode.Code == ILCode.Stloc) {
					int index = ((VariableDefinition)byteCode.Operand).Index;
					byteCode.Operand = Variables[index];
					numWrites[index]++;
				} else if (byteCode.Code == ILCode.Ldloca) {
					int index = ((VariableDefinition)byteCode.Operand).Index;
					byteCode.Operand = Variables[index];
					// ldloca leads to an unknown numbers of reads/writes, so ensure we don't inline the variable
					numReads[index] += 2;
					numWrites[index] += 2;
				}
			}
			
			// Find which variables we can inline
			if (this.optimize) {
				for (int i = 0; i < Variables.Count; i++) {
					if (numReads[i] == 1 && numWrites[i] == 1) {
						allowInline[Variables[i]] = true;
					}
				}
			}
			
			// Convert branch targets to labels
			foreach(ByteCode byteCode in body) {
				if (byteCode.Operand is Instruction[]) {
					List<ILLabel> newOperand = new List<ILLabel>();
					foreach(Instruction target in (Instruction[])byteCode.Operand) {
						newOperand.Add(instrToByteCode[target].Label);
					}
					byteCode.Operand = newOperand.ToArray();
				} else if (byteCode.Operand is Instruction) {
					byteCode.Operand = instrToByteCode[(Instruction)byteCode.Operand].Label;
				}
			}
			
			return body;
		}
		
		List<ILNode> ConvertToAst(List<ByteCode> body, HashSet<ExceptionHandler> ehs)
		{
			List<ILNode> ast = new List<ILNode>();
			
			while (ehs.Any()) {
				ILTryCatchBlock tryCatchBlock = new ILTryCatchBlock();
				
				// Find the first and widest scope
				int tryStart = ehs.Min(eh => eh.TryStart.Offset);
				int tryEnd   = ehs.Where(eh => eh.TryStart.Offset == tryStart).Max(eh => eh.TryEnd.Offset);
				var handlers = ehs.Where(eh => eh.TryStart.Offset == tryStart && eh.TryEnd.Offset == tryEnd).ToList();
				
				// Cut all instructions up to the try block
				{
					int tryStartIdx;
					for (tryStartIdx = 0; body[tryStartIdx].Offset != tryStart; tryStartIdx++);
					ast.AddRange(ConvertToAst(body.CutRange(0, tryStartIdx)));
				}
				
				// Cut the try block
				{
					HashSet<ExceptionHandler> nestedEHs = new HashSet<ExceptionHandler>(ehs.Where(eh => (tryStart <= eh.TryStart.Offset && eh.TryEnd.Offset < tryEnd) || (tryStart < eh.TryStart.Offset && eh.TryEnd.Offset <= tryEnd)));
					ehs.ExceptWith(nestedEHs);
					int tryEndIdx;
					for (tryEndIdx = 0; tryEndIdx < body.Count && body[tryEndIdx].Offset != tryEnd; tryEndIdx++);
					tryCatchBlock.TryBlock = new ILBlock(ConvertToAst(body.CutRange(0, tryEndIdx), nestedEHs));
				}
				
				// Cut all handlers
				tryCatchBlock.CatchBlocks = new List<ILTryCatchBlock.CatchBlock>();
				foreach(ExceptionHandler eh in handlers) {
					int startIndex;
					for (startIndex = 0; body[startIndex].Offset != eh.HandlerStart.Offset; startIndex++);
					int endInclusiveIndex;
					// Note that the end(exclusiove) instruction may not necessarly be in our body
					for (endInclusiveIndex = 0; body[endInclusiveIndex].Next.Offset != eh.HandlerEnd.Offset; endInclusiveIndex++);
					int count = 1 + endInclusiveIndex - startIndex;
					HashSet<ExceptionHandler> nestedEHs = new HashSet<ExceptionHandler>(ehs.Where(e => (eh.HandlerStart.Offset <= e.TryStart.Offset && e.TryEnd.Offset < eh.HandlerEnd.Offset) || (eh.HandlerStart.Offset < e.TryStart.Offset && e.TryEnd.Offset <= eh.HandlerEnd.Offset)));
					ehs.ExceptWith(nestedEHs);
					List<ILNode> handlerAst = ConvertToAst(body.CutRange(startIndex, count), nestedEHs);
					if (eh.HandlerType == ExceptionHandlerType.Catch) {
						ILTryCatchBlock.CatchBlock catchBlock = new ILTryCatchBlock.CatchBlock() {
							ExceptionType = eh.CatchType,
							Body = handlerAst
						};
						// Handle the automatically pushed exception on the stack
						ByteCode ldexception = ldexceptions[eh];
						if (ldexception.StoreTo.Count == 0) {
							throw new Exception("Exception should be consumed by something");
						} else if (ldexception.StoreTo.Count == 1) {
							ILExpression first = catchBlock.Body[0] as ILExpression;
							if (first != null &&
							    first.Code == ILCode.Pop &&
							    first.Arguments[0].Code == ILCode.Ldloc &&
							    first.Arguments[0].Operand == ldexception.StoreTo[0])
							{
								// The exception is just poped - optimize it all away;
								catchBlock.ExceptionVariable = null;
								catchBlock.Body.RemoveAt(0);
							} else {
								catchBlock.ExceptionVariable = ldexception.StoreTo[0];
							}
						} else {
							ILVariable exTemp = new ILVariable() { Name = "ex_" + eh.HandlerStart.Offset.ToString("X2"), IsGenerated = true };
							catchBlock.ExceptionVariable = exTemp;
							foreach(ILVariable storeTo in ldexception.StoreTo) {
								catchBlock.Body.Insert(0, new ILExpression(ILCode.Stloc, storeTo, new ILExpression(ILCode.Ldloc, exTemp)));
							}
						}
						tryCatchBlock.CatchBlocks.Add(catchBlock);
					} else if (eh.HandlerType == ExceptionHandlerType.Finally) {
						tryCatchBlock.FinallyBlock = new ILBlock(handlerAst);
						// TODO: ldexception
					} else {
						// TODO
					}
				}
				
				ehs.ExceptWith(handlers);
				
				ast.Add(tryCatchBlock);
			}
			
			// Add whatever is left
			ast.AddRange(ConvertToAst(body));
			
			return ast;
		}
		
		List<ILNode> ConvertToAst(List<ByteCode> body)
		{
			List<ILNode> ast = new List<ILNode>();
			
			// Convert stack-based IL code to ILAst tree
			foreach(ByteCode byteCode in body) {
				ILRange ilRange = new ILRange() { From = byteCode.Offset, To = byteCode.EndOffset };
				
				if (byteCode.StackBefore == null) {
					ast.Add(new ILComment() {
						Text = "Unreachable code: " + byteCode.Code.GetName(),
						ILRanges = new List<ILRange>(new[] { ilRange })
					});
					continue;
				}
				
				ILExpression expr = new ILExpression(byteCode.Code, byteCode.Operand);
				expr.ILRanges.Add(ilRange);
				expr.Prefixes = byteCode.Prefixes;
				
				// Label for this instruction
				if (byteCode.Label != null) {
					ast.Add(byteCode.Label);
				}
				
				// Reference arguments using temporary variables
				int popCount = byteCode.PopCount ?? byteCode.StackBefore.Count;
				for (int i = byteCode.StackBefore.Count - popCount; i < byteCode.StackBefore.Count; i++) {
					StackSlot slot = byteCode.StackBefore[i];
					expr.Arguments.Add(new ILExpression(ILCode.Ldloc, slot.LoadFrom));
				}
			
				// Store the result to temporary variable(s) if needed
				if (byteCode.StoreTo == null || byteCode.StoreTo.Count == 0) {
					ast.Add(expr);
				} else if (byteCode.StoreTo.Count == 1) {
					ast.Add(new ILExpression(ILCode.Stloc, byteCode.StoreTo[0], expr));
				} else {
					ILVariable tmpVar = new ILVariable() { Name = "expr_" + byteCode.Offset.ToString("X2"), IsGenerated = true };
					ast.Add(new ILExpression(ILCode.Stloc, tmpVar, expr));
					foreach(ILVariable storeTo in byteCode.StoreTo) {
						ast.Add(new ILExpression(ILCode.Stloc, storeTo, new ILExpression(ILCode.Ldloc, tmpVar)));
					}
				}
			}
			
			// Try to in-line stloc / ldloc pairs
			for(int i = 0; i < ast.Count - 1; i++) {
				if (i < 0) continue;
				
				ILExpression currExpr = ast[i] as ILExpression;
				ILExpression nextExpr = ast[i + 1] as ILExpression;
				
				if (currExpr != null && nextExpr != null && currExpr.Code == ILCode.Stloc) {
					
					// If the next expression is generated stloc, look inside 
					if (nextExpr.Code == ILCode.Stloc && ((ILVariable)nextExpr.Operand).IsGenerated) {
						nextExpr = nextExpr.Arguments[0];
					}
					
					// Find the use of the 'expr'
					for(int j = 0; j < nextExpr.Arguments.Count; j++) {
						ILExpression arg = nextExpr.Arguments[j];
						
						// We are moving the expression evaluation past the other aguments.
						// It is ok to pass ldloc because the expression can not contain stloc and thus the ldcoc will still return the same value
						if (arg.Code == ILCode.Ldloc) {
							if (arg.Operand == currExpr.Operand) {
								bool canInline;
								allowInline.TryGetValue((ILVariable)arg.Operand, out canInline);
								
								if (canInline) {
									// Assigne the ranges for optimized away instrustions somewhere
									currExpr.Arguments[0].ILRanges.AddRange(currExpr.ILRanges);
									currExpr.Arguments[0].ILRanges.AddRange(nextExpr.Arguments[j].ILRanges);
									
									ast.RemoveAt(i);
									nextExpr.Arguments[j] = currExpr.Arguments[0]; // Inline the stloc body
									i -= 2; // Try the same index again
									break;  // Found
								}
							}
						} else {
							break;  // Side-effects
						}
					}
				}
			}
			
			return ast;
		}
	}
	
	public static class ILAstBuilderExtensionMethods
	{
		public static List<T> CutRange<T>(this List<T> list, int start, int count)
		{
			List<T> ret = new List<T>(count);
			for (int i = 0; i < count; i++) {
				ret.Add(list[start + i]);
			}
			list.RemoveRange(start, count);
			return ret;
		}
	}
}
