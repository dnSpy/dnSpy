using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decompiler.Mono.Cecil.Rocks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Cecil = Mono.Cecil;

namespace Decompiler
{
	public class ILAstBuilder
	{
		class ILStack
		{
			public class Slot
			{
				public Instruction PushedBy;
				public TypeReference Type;
				
				public Slot(Instruction inst, TypeReference type)
				{
					this.PushedBy = inst;
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
			
			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				bool first = true;
				foreach (Slot s in this.Items) {
					if (!first) sb.Append(", ");
					sb.Append(s.PushedBy.Offset.ToString("X"));
					first = false;
				}
				return sb.ToString();
			}
		}
		
		Dictionary<Instruction, ILStack> stackBefore = new Dictionary<Instruction, ILAstBuilder.ILStack>();
		Dictionary<Instruction, ILLabel> labels = new Dictionary<Instruction, ILLabel>();
		
		public List<ILNode> Build(MethodDefinition methodDef)
		{
			// Make editable copy
			List<Instruction> body = new List<Instruction>(methodDef.Body.Instructions);
			
			if (body.Count == 0) return new List<ILNode>();
			
			StackAnalysis(body, methodDef);
			
			// Create branch labels for instructins; use the labels as branch operands
			foreach (Instruction inst in body) {
				if (inst.Operand is Instruction[]) {
					foreach(Instruction target in (Instruction[])inst.Operand) {
						if (!labels.ContainsKey(target)) {
							labels[target] = new ILLabel() { Name = "IL_" + target.Offset.ToString("X2") };
						}
					}
				} else if (inst.Operand is Instruction) {
					Instruction target = (Instruction)inst.Operand;
					if (!labels.ContainsKey(target)) {
						labels[target] = new ILLabel() { Name = "IL_" + target.Offset.ToString("X2") };
					}
				}
			}
			
			List<ILNode> ast = ConvertToAst(body, methodDef.Body.ExceptionHandlers);
			
			return ast;
		}
		
		public void StackAnalysis(List<Instruction> body, MethodDefinition methodDef)
		{
			Queue<Instruction> agenda = new Queue<Instruction>();
			
			// Add known states
			stackBefore[body[0]] = new ILStack();
			agenda.Enqueue(body[0]);
			
			if(methodDef.Body.HasExceptionHandlers) {
				foreach(ExceptionHandler ex in methodDef.Body.ExceptionHandlers) {
					stackBefore[ex.TryStart] = new ILStack();
					agenda.Enqueue(ex.TryStart);
					
					ILStack stack = new ILStack();
					stack.Items.Add(new ILStack.Slot(null, MyRocks.TypeException));
					stackBefore[ex.HandlerStart] = stack;
					agenda.Enqueue(ex.HandlerStart);
				}
			}
			
			// Process agenda
			while(agenda.Count > 0) {
				Instruction inst = agenda.Dequeue();
				
				// What is the effect of the instruction on the stack?
				ILStack newStack = stackBefore[inst].Clone();
				int popCount = inst.GetPopCount();
				if (popCount == int.MaxValue) popCount = stackBefore[inst].Items.Count; // Pop all
				List<TypeReference> typeArgs = new List<TypeReference>();
				for (int i = newStack.Items.Count - popCount; i < newStack.Items.Count; i++) {
					typeArgs.Add(newStack.Items[i].Type);
				}
				TypeReference type;
				try {
					type = inst.GetTypeInternal(methodDef, typeArgs);
				} catch {
					type = MyRocks.TypeObject;
				}
				if (popCount > 0) {
					newStack.Items.RemoveRange(newStack.Items.Count - popCount, popCount);
				}
				int pushCount = inst.GetPushCount();
				for (int i = 0; i < pushCount; i++) {
					newStack.Items.Add(new ILStack.Slot(inst, type));
				}
				
				// Apply the state to any successors
				List<Instruction> branchTargets = new List<Instruction>();
				if (inst.OpCode.CanFallThough()) {
					branchTargets.Add(inst.Next);
				}
				if (inst.OpCode.IsBranch()) {
					if (inst.Operand is Instruction[]) {
						branchTargets.AddRange((Instruction[])inst.Operand);
					} else {
						branchTargets.Add((Instruction)inst.Operand);
					}
				}
				foreach (Instruction branchTarget in branchTargets) {
					ILStack nextStack;
					if (stackBefore.TryGetValue(branchTarget, out nextStack)) {
						// TODO: Compare stacks
					} else {
						stackBefore[branchTarget] = newStack;
						agenda.Enqueue(branchTarget);
					}
				}
			}
		}
		
		public List<ILNode> ConvertToAst(List<Instruction> body, IEnumerable<ExceptionHandler> ehs)
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
					tryCatchBlock.TryBlock = ConvertToAst(body.CutRange(0, tryEndIdx), nestedEHs);
				}
				
				// Cut all handlers
				tryCatchBlock.CatchBlocks = new List<ILTryCatchBlock.CatchBlock>();
				foreach(ExceptionHandler eh in handlers) {
					int start;
					for (start = 0; body[start] != eh.HandlerStart; start++);
					int end;
					for (end = 0; body[end] != eh.HandlerEnd; end++);
					int count = end - start;
					List<ExceptionHandler> nestedEHs = ehs.Where(e => (start <= e.TryStart.Offset && e.TryEnd.Offset < end) || (start < e.TryStart.Offset && e.TryEnd.Offset <= end)).ToList();
					List<ILNode> handlerAst = ConvertToAst(body.CutRange(start, count), nestedEHs);
					if (eh.HandlerType == ExceptionHandlerType.Catch) {
						tryCatchBlock.CatchBlocks.Add(new ILTryCatchBlock.CatchBlock() {
							ExceptionType = eh.CatchType,
							Body = handlerAst
						});
					} else if (eh.HandlerType == ExceptionHandlerType.Finally) {
						tryCatchBlock.FinallyBlock = handlerAst;
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
		
		public List<ILNode> ConvertToAst(List<Instruction> body)
		{
			List<ILNode> ast = new List<ILNode>();
			
			// Convert stack-based IL code to ILAst tree
			foreach(Instruction inst in body) {
				ILExpression expr = new ILExpression(inst.OpCode, inst.Operand);
				
				// Label for this instruction
				ILLabel label;
				if (labels.TryGetValue(inst, out label)) {
					ast.Add(label);
				}
				
				// Branch using labels
				if (inst.Operand is Instruction[]) {
					List<ILLabel> newOperand = new List<ILLabel>();
					foreach(Instruction target in (Instruction[])inst.Operand) {
						newOperand.Add(labels[target]);
					}
					expr.Operand = newOperand.ToArray();
				} else if (inst.Operand is Instruction) {
					expr.Operand = labels[(Instruction)inst.Operand];
				}
				
				// Reference arguments using temporary variables
				ILStack stack = stackBefore[inst];
				int popCount = inst.GetPopCount();
				if (popCount == int.MaxValue) popCount = stackBefore[inst].Items.Count; // Pop all
				for (int i = stack.Items.Count - popCount; i < stack.Items.Count; i++) {
					Instruction pushedBy = stack.Items[i].PushedBy;
					if (pushedBy != null) {
						ILExpression ldExpr = new ILExpression(OpCodes.Ldloc, new ILStackVariable() { Name = "expr" + pushedBy.Offset.ToString("X2") });
						expr.Arguments.Add(ldExpr);
					} else {
						ILExpression ldExpr = new ILExpression(OpCodes.Ldloc, new ILStackVariable() { Name = "exception" });
						expr.Arguments.Add(ldExpr);
					}
				}
			
				// If the bytecode pushes anything store the result in temporary variable
				int pushCount = inst.GetPushCount();
				if (pushCount > 0) {
					ILExpression stExpr = new ILExpression(OpCodes.Stloc, new ILStackVariable() { Name = "expr" + inst.Offset.ToString("X2"), RefCount = pushCount });
					stExpr.Arguments.Add(expr);
					expr = stExpr;
				}
				
				ast.Add(expr);
			}
			
			// Try to in-line stloc / ldloc pairs
			for(int i = 0; i < ast.Count - 1; i++) {
				ILExpression expr = ast[i] as ILExpression;
				ILExpression nextExpr = ast[i + 1] as ILExpression;
				
				if (expr != null && nextExpr != null && expr.OpCode.Code == Code.Stloc && expr.Operand is ILStackVariable) {
					
					// If the next expression is stloc, look inside 
					if (nextExpr.OpCode.Code == Code.Stloc && nextExpr.Operand is ILStackVariable) {
						nextExpr = nextExpr.Arguments[0];
					}
					
					// Find the use of the 'expr'
					for(int j = 0; j < nextExpr.Arguments.Count; j++) {
						ILExpression arg = nextExpr.Arguments[j];
						
						// TODO: Check if duplicating the dup opcode has side-effects
						
						if (arg.OpCode.Code == Code.Ldloc && arg.Operand is ILStackVariable) {
							ILStackVariable stVar = (ILStackVariable)expr.Operand;
							ILStackVariable ldVar = (ILStackVariable)arg.Operand;
							if (stVar.Name == ldVar.Name) {
								stVar.RefCount--;
								if (stVar.RefCount <= 0) {
									ast.RemoveAt(i);
								}
								nextExpr.Arguments[j] = expr.Arguments[0]; // Inline the stloc body
								i = Math.Max(0, i - 2); // Try the same index again
								break;  // Found
							}
						} else {
							break; // This argument might have side effects so we can not move the 'expr' after it.
						}
					}
				}
			}
			
			return ast;
		}
	}
}
