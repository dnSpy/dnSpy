using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Cecil = Mono.Cecil;
using Decompiler.Rocks;

namespace Decompiler
{
	public class ILAstBuilder
	{
		class StackSlot
		{
			public List<ByteCode> PushedBy;  // Pushed by one of these;  null element means exception pushed by CLR
			public ILVariable LoadFrom;
			
			public StackSlot()
			{
			}
			
			public StackSlot(ByteCode pushedBy)
			{
				this.PushedBy = new List<ByteCode>(1);
				this.PushedBy.Add(pushedBy);
			}
		}
		
		class ByteCode
		{
			public ILLabel  Label;    // Non-null only if needed
			public int      Offset;
			public int      EndOffset;
			public OpCode   OpCode;
			public object   Operand;
			public int?     PopCount; // Null means pop all
			public int      PushCount;
			public string   Name { get { return "IL_" + this.Offset.ToString("X2"); } }
			public ByteCode Next;
			public List<StackSlot> StackBefore;
			public List<ILVariable> StoreTo;
			
			public List<StackSlot> CloneStack(int? popCount)
			{
				List<StackSlot> clone = new List<StackSlot>();
				if (popCount.HasValue) {
					if (popCount.Value > this.StackBefore.Count) {
						throw new Exception("Can not pop - the stack is empty");
					}
					for(int i = 0; i < this.StackBefore.Count - popCount.Value; i++) {
						clone.Add(new StackSlot() { PushedBy = new List<ByteCode>(this.StackBefore[i].PushedBy) });
					}
				}
				return clone;
			}
			
			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("{0}:{1} {2} {3}", this.Name, this.Label != null ? " *" : "", this.OpCode, this.Operand);
				if (this.StackBefore != null) {
					sb.Append(" StackBefore = {");
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
					sb.Append(" StoreTo = {");
					bool first = true;
					foreach (ILVariable stackVar in this.StoreTo) {
						if (!first) sb.Append(",");
						sb.Append(stackVar.Name);
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
		
		public List<ILNode> Build(MethodDefinition methodDef, bool optimize)
		{
			this.methodDef = methodDef;
			this.optimize = optimize;
			
			if (methodDef.Body.Instructions.Count == 0) return new List<ILNode>();
			
			List<ByteCode> body = StackAnalysis(methodDef);
			
			List<ILNode> ast = ConvertToAst(body, methodDef.Body.ExceptionHandlers);
			
			return ast;
		}
		
		List<ByteCode> StackAnalysis(MethodDefinition methodDef)
		{
			// Create temporary structure for the stack analysis
			List<ByteCode> body = new List<ByteCode>(methodDef.Body.Instructions.Count);
			foreach(Instruction inst in methodDef.Body.Instructions) {
				OpCode opCode  = inst.OpCode;
				object operand = inst.Operand;
				MethodBodyRocks.ExpandMacro(ref opCode, ref operand, methodDef.Body);
				ByteCode byteCode = new ByteCode() {
					Offset      = inst.Offset,
					EndOffset   = inst.Next != null ? inst.Next.Offset : methodDef.Body.CodeSize,
					OpCode      = opCode,
					Operand     = operand,
					PopCount    = inst.GetPopCount(),
					PushCount   = inst.GetPushCount()
				};
				instrToByteCode[inst] = byteCode;
				body.Add(byteCode);
			}
			for (int i = 0; i < body.Count - 1; i++) {
				body[i].Next = body[i + 1];
			}
			
			Queue<ByteCode> agenda = new Queue<ByteCode>();
			
			// Add known states
			body[0].StackBefore = new List<StackSlot>();
			agenda.Enqueue(body[0]);
			
			if(methodDef.Body.HasExceptionHandlers) {
				foreach(ExceptionHandler ex in methodDef.Body.ExceptionHandlers) {
					ByteCode tryStart = instrToByteCode[ex.TryStart];
					tryStart.StackBefore = new List<StackSlot>();
					agenda.Enqueue(tryStart);
					
					ByteCode handlerStart = instrToByteCode[ex.HandlerType == ExceptionHandlerType.Filter ? ex.FilterStart : ex.HandlerStart];
					handlerStart.StackBefore = new List<StackSlot>();
					if (ex.HandlerType == ExceptionHandlerType.Catch || ex.HandlerType == ExceptionHandlerType.Filter) {
						handlerStart.StackBefore.Add(new StackSlot(null));
					}
					agenda.Enqueue(handlerStart);
					
					// Control flow is not required to reach endfilter
					if (ex.HandlerType == ExceptionHandlerType.Filter) {
						ByteCode endFilter = instrToByteCode[ex.FilterEnd.Previous];
						endFilter.StackBefore = new List<StackSlot>();
					}
				}
			}
			
			// Process agenda
			while(agenda.Count > 0) {
				ByteCode byteCode = agenda.Dequeue();
				
				// Calculate new stack
				List<StackSlot> newStack = byteCode.CloneStack(byteCode.PopCount);
				for (int i = 0; i < byteCode.PushCount; i++) {
					newStack.Add(new StackSlot(byteCode));
				}
				
				// Apply the state to any successors
				List<ByteCode> branchTargets = new List<ByteCode>();
				if (byteCode.OpCode.CanFallThough()) {
					branchTargets.Add(byteCode.Next);
				}
				if (byteCode.OpCode.IsBranch()) {
					if (byteCode.Operand is Instruction[]) {
						foreach(Instruction inst in (Instruction[])byteCode.Operand) {
							ByteCode target = instrToByteCode[inst];
							branchTargets.Add(target);
							// The target of a branch must have label
							if (target.Label == null) {
								target.Label = new ILLabel() { Name = target.Name };
							}
						}
					} else {
						ByteCode target = instrToByteCode[(Instruction)byteCode.Operand];
						branchTargets.Add(target);
						// The target of a branch must have label
						if (target.Label == null) {
							target.Label = new ILLabel() { Name = target.Name };
						}
					}
				}
				foreach (ByteCode branchTarget in branchTargets) {
					if (branchTarget.StackBefore == null) {
						branchTarget.StackBefore = newStack;
						// Do not share one stack for several bytecodes
						if (branchTargets.Count > 1) {
							branchTarget.StackBefore = branchTarget.CloneStack(0);
						}
						agenda.Enqueue(branchTarget);
					} else {
						if (branchTarget.StackBefore.Count != newStack.Count) {
							throw new Exception("Inconsistent stack size at " + byteCode.Name);
						}
						
						// Merge stacks
						bool modified = false;
						for (int i = 0; i < newStack.Count; i++) {
							List<ByteCode> oldPushedBy = branchTarget.StackBefore[i].PushedBy;
							List<ByteCode> newPushedBy = oldPushedBy.Union(newStack[i].PushedBy).ToList();
							if (newPushedBy.Count > oldPushedBy.Count) {
								branchTarget.StackBefore[i].PushedBy = newPushedBy;
								modified = true;
							}
						}
						
						if (modified) {
							agenda.Enqueue(branchTarget);
						}
					}
				}
			}
			
			// Genertate temporary variables to replace stack
			foreach(ByteCode byteCode in body) {
				int argIdx = 0;
				int popCount = byteCode.PopCount ?? byteCode.StackBefore.Count;
				for (int i = byteCode.StackBefore.Count - popCount; i < byteCode.StackBefore.Count; i++) {
					StackSlot arg = byteCode.StackBefore[i];
					ILVariable tmpVar = new ILVariable() { Name = string.Format("arg_{0:X2}_{1}", byteCode.Offset, argIdx), IsGenerated = true };
					arg.LoadFrom = tmpVar;
					foreach(ByteCode pushedBy in arg.PushedBy) {
						// TODO: Handle exception variables
						if (pushedBy != null) {
							if (pushedBy.StoreTo == null) {
								pushedBy.StoreTo = new List<ILVariable>(1);
							}
							pushedBy.StoreTo.Add(tmpVar);
						}
					}
					if (arg.PushedBy.Count == 1) {
						allowInline[tmpVar] = true;
					}
					argIdx++;
				}
			}
			
			// Convert local varibles
			List<ILVariable> vars = methodDef.Body.Variables.Select(v => new ILVariable() { Name = string.IsNullOrEmpty(v.Name) ?  "var_" + v.Index : v.Name }).ToList();
			int[] numReads  = new int[vars.Count];
			int[] numWrites = new int[vars.Count];
			foreach(ByteCode byteCode in body) {
				if (byteCode.OpCode == OpCodes.Ldloc) {
					int index = ((VariableDefinition)byteCode.Operand).Index;
					byteCode.Operand = vars[index];
					numReads[index]++;
				}
				if (byteCode.OpCode == OpCodes.Stloc) {
					int index = ((VariableDefinition)byteCode.Operand).Index;
					byteCode.Operand = vars[index];
					numWrites[index]++;
				}
			}
			
			// Find which variables we can inline
			if (this.optimize) {
				for (int i = 0; i < vars.Count; i++) {
					if (numReads[i] == 1 && numWrites[i] == 1) {
						allowInline[vars[i]] = true;
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
		
		List<ILNode> ConvertToAst(List<ByteCode> body, IEnumerable<ExceptionHandler> ehs)
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
					List<ExceptionHandler> nestedEHs = ehs.Where(eh => (tryStart <= eh.TryStart.Offset && eh.TryEnd.Offset < tryEnd) || (tryStart < eh.TryStart.Offset && eh.TryEnd.Offset <= tryEnd)).ToList();
					int tryEndIdx;
					for (tryEndIdx = 0; tryEndIdx < body.Count && body[tryEndIdx].Offset != tryEnd; tryEndIdx++);
					tryCatchBlock.TryBlock = new ILBlock(ConvertToAst(body.CutRange(0, tryEndIdx), nestedEHs));
				}
				
				// Cut all handlers
				tryCatchBlock.CatchBlocks = new List<ILTryCatchBlock.CatchBlock>();
				foreach(ExceptionHandler eh in handlers) {
					int start;
					for (start = 0; body[start].Offset != eh.HandlerStart.Offset; start++);
					int end;
					for (end = 0; body[end].Offset != eh.HandlerEnd.Offset; end++);
					int count = end - start;
					List<ExceptionHandler> nestedEHs = ehs.Where(e => (start <= e.TryStart.Offset && e.TryEnd.Offset < end) || (start < e.TryStart.Offset && e.TryEnd.Offset <= end)).ToList();
					List<ILNode> handlerAst = ConvertToAst(body.CutRange(start, count), nestedEHs);
					if (eh.HandlerType == ExceptionHandlerType.Catch) {
						tryCatchBlock.CatchBlocks.Add(new ILTryCatchBlock.CatchBlock() {
							ExceptionType = eh.CatchType,
							Body = handlerAst
						});
					} else if (eh.HandlerType == ExceptionHandlerType.Finally) {
						tryCatchBlock.FinallyBlock = new ILBlock(handlerAst);
					} else {
						// TODO
					}
				}
				
				ehs = ehs.Where(eh => eh.TryStart.Offset > tryEnd).ToList();
				
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
				OpCode opCode  = byteCode.OpCode;
				object operand = byteCode.Operand;
				
				MethodBodyRocks.ExpandMacro(ref opCode, ref operand, methodDef.Body);
				
				ILExpression expr = new ILExpression(opCode, operand);
				expr.ILRanges.Add(new ILRange() { From = byteCode.Offset, To = byteCode.EndOffset });
				
				// Label for this instruction
				if (byteCode.Label != null) {
					ast.Add(byteCode.Label);
				}
				
				// Reference arguments using temporary variables
				int popCount = byteCode.PopCount ?? byteCode.StackBefore.Count;
				for (int i = byteCode.StackBefore.Count - popCount; i < byteCode.StackBefore.Count; i++) {
					StackSlot slot = byteCode.StackBefore[i];
					if (slot.PushedBy != null) {
						ILExpression ldExpr = new ILExpression(OpCodes.Ldloc, slot.LoadFrom);
						expr.Arguments.Add(ldExpr);
					} else {
						ILExpression ldExpr = new ILExpression(OpCodes.Ldloc, new ILVariable() { Name = "ex", IsGenerated = true });
						expr.Arguments.Add(ldExpr);
					}
				}
			
				// Store the result to temporary variable(s) if needed
				if (byteCode.StoreTo == null || byteCode.StoreTo.Count == 0) {
					ast.Add(expr);
				} else if (byteCode.StoreTo.Count == 1) {
					ast.Add(new ILExpression(OpCodes.Stloc, byteCode.StoreTo[0], expr));
				} else {
					ILVariable tmpVar = new ILVariable() { Name = "expr_" + byteCode.Offset.ToString("X2"), IsGenerated = true };
					ast.Add(new ILExpression(OpCodes.Stloc, tmpVar, expr));
					foreach(ILVariable storeTo in byteCode.StoreTo) {
						ast.Add(new ILExpression(OpCodes.Stloc, storeTo, new ILExpression(OpCodes.Ldloc, tmpVar)));
					}
				}
			}
			
			// Try to in-line stloc / ldloc pairs
			for(int i = 0; i < ast.Count - 1; i++) {
				if (i < 0) continue;
				
				ILExpression currExpr = ast[i] as ILExpression;
				ILExpression nextExpr = ast[i + 1] as ILExpression;
				
				if (currExpr != null && nextExpr != null && currExpr.OpCode.Code == Code.Stloc) {
					
					// If the next expression is generated stloc, look inside 
					if (nextExpr.OpCode.Code == Code.Stloc && ((ILVariable)nextExpr.Operand).IsGenerated) {
						nextExpr = nextExpr.Arguments[0];
					}
					
					// Find the use of the 'expr'
					for(int j = 0; j < nextExpr.Arguments.Count; j++) {
						ILExpression arg = nextExpr.Arguments[j];
						
						// We are moving the expression evaluation past the other aguments.
						// It is ok to pass ldloc because the expression can not contain stloc and thus the ldcoc will still return the same value
						if (arg.OpCode.Code == Code.Ldloc) {
							bool canInline;
							allowInline.TryGetValue((ILVariable)arg.Operand, out canInline);
							if (arg.Operand == currExpr.Operand && canInline) {
								// Assigne the ranges for optimized away instrustions somewhere
								currExpr.Arguments[0].ILRanges.AddRange(currExpr.ILRanges);
								currExpr.Arguments[0].ILRanges.AddRange(nextExpr.Arguments[j].ILRanges);
								ast.RemoveAt(i);
								nextExpr.Arguments[j] = currExpr.Arguments[0]; // Inline the stloc body
								i -= 2; // Try the same index again
								break;  // Found
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
}
