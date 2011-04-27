// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Cecil = Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	public class ILAstBuilder
	{
		static ByteCode[] EmptyByteCodeArray = new ByteCode[] {};
		
		/// <summary> Immutable </summary>
		class StackSlot
		{
			public readonly ByteCode[] PushedBy;  // One of those
			public readonly ILVariable LoadFrom;  // Where can we get the value from in AST
			
			public StackSlot(ByteCode[] pushedBy, ILVariable loadFrom)
			{
				this.PushedBy = pushedBy;
				this.LoadFrom = loadFrom;
			}
			
			public StackSlot(ByteCode pushedBy)
			{
				this.PushedBy = new[] { pushedBy };
				this.LoadFrom = null;
			}
			
			public static List<StackSlot> CloneStack(List<StackSlot> stack, int? popCount)
			{
				if (popCount.HasValue) {
					return stack.GetRange(0, stack.Count - popCount.Value);
				} else {
					return new List<StackSlot>(0);
				}
			}
		}
		
		/// <summary> Immutable </summary>
		class VariableSlot
		{			
			public readonly ByteCode[] StoredBy;    // One of those
			public readonly bool       StoredByAll; // Overestimate which is useful for exceptional control flow.
			
			public VariableSlot(ByteCode[] storedBy, bool storedByAll)
			{
				this.StoredBy = storedBy;
				this.StoredByAll = storedByAll;
			}
			
			public VariableSlot(ByteCode storedBy)
			{
				this.StoredBy = new[] { storedBy };
				this.StoredByAll = false;
			}
			
			public static VariableSlot[] CloneVariableState(VariableSlot[] state)
			{
				VariableSlot[] clone = new VariableSlot[state.Length];
				for (int i = 0; i < clone.Length; i++) {
					clone[i] = state[i];
				}
				return clone;
			}
			
			public static VariableSlot[] MakeEmptyState(int varCount)
			{
				VariableSlot[] emptyVariableState = new VariableSlot[varCount];
				for (int i = 0; i < emptyVariableState.Length; i++) {
					emptyVariableState[i] = new VariableSlot(EmptyByteCodeArray, false);
				}
				return emptyVariableState;
			}
			
			public static VariableSlot[] MakeFullState(int varCount)
			{
				VariableSlot[] unknownVariableState = new VariableSlot[varCount];
				for (int i = 0; i < unknownVariableState.Length; i++) {
					unknownVariableState[i] = new VariableSlot(EmptyByteCodeArray, true);
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
			public List<StackSlot>  StackBefore;     // Unique per bytecode; not shared
			public List<ILVariable> StoreTo;         // Store result of instruction to those AST variables
			public VariableSlot[]   VariablesBefore; // Unique per bytecode; not shared
			
			public VariableDefinition OperandAsVariable { get { return (VariableDefinition)this.Operand; } }
			
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
						} else if (varSlot.StoredBy.Length == 0) {
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
		
		// Virtual instructions to load exception on stack
		Dictionary<ExceptionHandler, ByteCode> ldexceptions = new Dictionary<ExceptionHandler, ILAstBuilder.ByteCode>();
		
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
			Dictionary<Instruction, ByteCode> instrToByteCode = new Dictionary<Instruction, ByteCode>();
			
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
					PopCount    = inst.GetPopDelta(),
					PushCount   = inst.GetPushDelta()
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
					ByteCode handlerStart = instrToByteCode[ex.HandlerStart];
					handlerStart.StackBefore = new List<StackSlot>();
					handlerStart.VariablesBefore = VariableSlot.MakeFullState(varCount);
					if (ex.HandlerType == ExceptionHandlerType.Catch || ex.HandlerType == ExceptionHandlerType.Filter) {
						// Catch and Filter handlers start with the exeption on the stack
						ByteCode ldexception = new ByteCode() {
							Code = ILCode.Ldexception,
							Operand = ex.CatchType,
							PopCount = 0,
							PushCount = 1
						};
						ldexceptions[ex] = ldexception;
						handlerStart.StackBefore.Add(new StackSlot(ldexception));
					}
					agenda.Push(handlerStart);
					
					if (ex.HandlerType == ExceptionHandlerType.Filter)
					{
						ByteCode filterStart = instrToByteCode[ex.FilterStart];
						filterStart.StackBefore = new List<StackSlot>();
						filterStart.VariablesBefore = VariableSlot.MakeFullState(varCount);
						ByteCode ldexception = new ByteCode() {
							Code = ILCode.Ldexception,
							Operand = ex.CatchType,
							PopCount = 0,
							PushCount = 1
						};
						// TODO: ldexceptions[ex] = ldexception;
						filterStart.StackBefore.Add(new StackSlot(ldexception));
						agenda.Push(filterStart);
					}
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
					newVariableState[varIndex] = new VariableSlot(byteCode);
				}
				
				// After the leave, finally block might have touched the variables
				if (byteCode.Code == ILCode.Leave) {
					newVariableState = VariableSlot.MakeFullState(varCount);
				}
				
				// Find all successors
				List<ByteCode> branchTargets = new List<ByteCode>();
				if (!byteCode.Code.IsUnconditionalControlFlow()) {
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
							ByteCode[] oldPushedBy = branchTarget.StackBefore[i].PushedBy;
							ByteCode[] newPushedBy = oldPushedBy.Union(newStack[i].PushedBy);
							if (newPushedBy.Length > oldPushedBy.Length) {
								branchTarget.StackBefore[i] = new StackSlot(newPushedBy, null);
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
									branchTarget.VariablesBefore[i] = newSlot;
									modified = true;
								} else {
									ByteCode[] oldStoredBy = oldSlot.StoredBy;
									ByteCode[] newStoredBy = oldStoredBy.Union(newSlot.StoredBy);
									if (newStoredBy.Length > oldStoredBy.Length) {
										branchTarget.VariablesBefore[i] = new VariableSlot(newStoredBy, false);
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
			
			// Occasionally the compiler generates unreachable code - it can be usually just ignored
			var reachableBody   = body.Where(b => b.StackBefore != null);
			var unreachableBody = body.Where(b => b.StackBefore == null);
			
			// Genertate temporary variables to replace stack
			// Unrachable code does not need temporary variables - the values are never pushed on the stack for consuption
			foreach(ByteCode byteCode in reachableBody) {
				int argIdx = 0;
				int popCount = byteCode.PopCount ?? byteCode.StackBefore.Count;
				for (int i = byteCode.StackBefore.Count - popCount; i < byteCode.StackBefore.Count; i++) {
					ILVariable tmpVar = new ILVariable() { Name = string.Format("arg_{0:X2}_{1}", byteCode.Offset, argIdx), IsGenerated = true };
					byteCode.StackBefore[i] = new StackSlot(byteCode.StackBefore[i].PushedBy, tmpVar);
					foreach(ByteCode pushedBy in byteCode.StackBefore[i].PushedBy) {
						if (pushedBy.StoreTo == null) {
							pushedBy.StoreTo = new List<ILVariable>(1);
						}
						pushedBy.StoreTo.Add(tmpVar);
					}
					argIdx++;
				}
			}
			
			// Try to use single temporary variable insted of several if possilbe (especially useful for dup)
			// This has to be done after all temporary variables are assigned so we know about all loads
			// Unrachable code will not have any StoreTo
			foreach(ByteCode byteCode in reachableBody) {
				if (byteCode.StoreTo != null && byteCode.StoreTo.Count > 1) {
					var locVars = byteCode.StoreTo;
					// For each of the variables, find the location where it is loaded - there should be preciesly one
					var loadedBy = locVars.Select(locVar => reachableBody.SelectMany(bc => bc.StackBefore).Where(s => s.LoadFrom == locVar).Single()).ToList();
					// We now know that all the variables have a single load,
					// Let's make sure that they have also a single store - us
					if (loadedBy.All(slot => slot.PushedBy.Length == 1 && slot.PushedBy[0] == byteCode)) {
						// Great - we can reduce everything into single variable
						ILVariable tmpVar = new ILVariable() { Name = string.Format("expr_{0:X2}", byteCode.Offset), IsGenerated = true };
						byteCode.StoreTo = new List<ILVariable>() { tmpVar };
						foreach(ByteCode bc in reachableBody) {
							for (int i = 0; i < bc.StackBefore.Count; i++) {
								// Is it one of the variable to be merged?
								if (locVars.Contains(bc.StackBefore[i].LoadFrom)) {
									// Replace with the new temp variable
									bc.StackBefore[i] = new StackSlot(bc.StackBefore[i].PushedBy, tmpVar);
								}
							}
						}
					}
				}
			}
			
			// Split and convert the normal local variables
			ConvertLocalVariables(body);
			
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
			
			// Convert parameters to ILVariables
			ConvertParameters(body);
			
			return body;
		}
		
		class VariableInfo
		{
			public ILVariable Variable;
			public List<ByteCode> Stores;
			public List<ByteCode> Loads;
		}
		
		/// <summary>
		/// If possible, separates local variables into several independent variables.
		/// It should undo any compilers merging.
		/// </summary>
		void ConvertLocalVariables(List<ByteCode> body)
		{
			if (optimize) {
				int varCount = methodDef.Body.Variables.Count;
				
				for(int variableIndex = 0; variableIndex < varCount; variableIndex++) {
					// Find all stores and loads for this variable
					List<ByteCode> stores = body.Where(b => b.Code == ILCode.Stloc && b.Operand is VariableDefinition && b.OperandAsVariable.Index == variableIndex).ToList();
					List<ByteCode> loads  = body.Where(b => (b.Code == ILCode.Ldloc || b.Code == ILCode.Ldloca) && b.Operand is VariableDefinition && b.OperandAsVariable.Index == variableIndex).ToList();
					TypeReference varType = methodDef.Body.Variables[variableIndex].VariableType;
					
					List<VariableInfo> newVars;
					
					bool isPinned = methodDef.Body.Variables[variableIndex].IsPinned;
					// If the variable is pinned, use single variable.
					// If any of the loads is from "all", use single variable
					// If any of the loads is ldloca, fallback to single variable as well
					if (isPinned || loads.Any(b => b.VariablesBefore[variableIndex].StoredByAll || b.Code == ILCode.Ldloca)) {
						newVars = new List<VariableInfo>(1) { new VariableInfo() {
							Variable = new ILVariable() {
								Name = "var_" + variableIndex,
								Type = isPinned ? ((PinnedType)varType).ElementType : varType,
								OriginalVariable = methodDef.Body.Variables[variableIndex]
							},
							Stores = stores,
							Loads  = loads
						}};
					} else {
						// Create a new variable for each store
						newVars = stores.Select(st => new VariableInfo() {
							Variable = new ILVariable() {
						    		Name = "var_" + variableIndex + "_" + st.Offset.ToString("X2"),
						    		Type = varType,
						    		OriginalVariable = methodDef.Body.Variables[variableIndex]
						    },
						    Stores = new List<ByteCode>() {st},
						    Loads  = new List<ByteCode>()
						}).ToList();
						
						// VB.NET uses the 'init' to allow use of uninitialized variables.
						// We do not really care about them too much - if the original variable
						// was uninitialized at that point it means that no store was called and
						// thus all our new variables must be uninitialized as well.
						// So it does not matter which one we load.
						
						// TODO: We should add explicit initialization so that C# code compiles.
						// Remember to handle cases where one path inits the variable, but other does not.
						
						// Add loads to the data structure; merge variables if necessary
						foreach(ByteCode load in loads) {
							ByteCode[] storedBy = load.VariablesBefore[variableIndex].StoredBy;
							if (storedBy.Length == 0) {
								// Load which always loads the default ('uninitialized') value
								// Create a dummy variable just for this load
								newVars.Add(new VariableInfo() {
									Variable = new ILVariable() {
								    		Name = "var_" + variableIndex + "_" + load.Offset.ToString("X2") + "_default",
								    		Type = varType,
								    		OriginalVariable = methodDef.Body.Variables[variableIndex]
								    },
								    Stores = new List<ByteCode>(),
								    Loads  = new List<ByteCode>() { load }
								});
							} else if (storedBy.Length == 1) {
								VariableInfo newVar = newVars.Where(v => v.Stores.Contains(storedBy[0])).Single();
								newVar.Loads.Add(load);
							} else {
								List<VariableInfo> mergeVars = newVars.Where(v => v.Stores.Union(storedBy).Any()).ToList();
								VariableInfo mergedVar = new VariableInfo() {
									Variable = mergeVars[0].Variable,
									Stores = mergeVars.SelectMany(v => v.Stores).ToList(),
									Loads  = mergeVars.SelectMany(v => v.Loads).ToList()
								};
								mergedVar.Loads.Add(load);
								newVars = newVars.Except(mergeVars).ToList();
								newVars.Add(mergedVar);
							}
						}
					}
					
					// Set bytecode operands
					foreach(VariableInfo newVar in newVars) {
						foreach(ByteCode store in newVar.Stores) {
							store.Operand = newVar.Variable;
						}
						foreach(ByteCode load in newVar.Loads) {
							load.Operand = newVar.Variable;
						}
					}
				}
			} else {
				var variables = methodDef.Body.Variables.Select(v => new ILVariable() { Name = string.IsNullOrEmpty(v.Name) ?  "var_" + v.Index : v.Name, Type = v.VariableType, OriginalVariable = v }).ToList();
				foreach(ByteCode byteCode in body) {
					if (byteCode.Code == ILCode.Ldloc || byteCode.Code == ILCode.Stloc || byteCode.Code == ILCode.Ldloca) {
						int index = ((VariableDefinition)byteCode.Operand).Index;
						byteCode.Operand = variables[index];
					}
				}
			}
		}
		
		public List<ILVariable> Parameters = new List<ILVariable>();
		
		void ConvertParameters(List<ByteCode> body)
		{
			ILVariable thisParameter = null;
			if (methodDef.HasThis) {
				TypeReference type = methodDef.DeclaringType;
				thisParameter = new ILVariable();
				thisParameter.Type = type.IsValueType ? new ByReferenceType(type) : type;
				thisParameter.Name = "this";
				thisParameter.OriginalParameter = methodDef.Body.ThisParameter;
			}
			foreach (ParameterDefinition p in methodDef.Parameters) {
				this.Parameters.Add(new ILVariable { Type = p.ParameterType, Name = p.Name, OriginalParameter = p });
			}
			foreach (ByteCode byteCode in body) {
				ParameterDefinition p;
				switch (byteCode.Code) {
					case ILCode.__Ldarg:
						p = (ParameterDefinition)byteCode.Operand;
						byteCode.Code = ILCode.Ldloc;
						byteCode.Operand = p.Index < 0 ? thisParameter : this.Parameters[p.Index];
						break;
					case ILCode.__Starg:
						p = (ParameterDefinition)byteCode.Operand;
						byteCode.Code = ILCode.Stloc;
						byteCode.Operand = p.Index < 0 ? thisParameter : this.Parameters[p.Index];
						break;
					case ILCode.__Ldarga:
						p = (ParameterDefinition)byteCode.Operand;
						byteCode.Code = ILCode.Ldloca;
						byteCode.Operand = p.Index < 0 ? thisParameter : this.Parameters[p.Index];
						break;
				}
			}
			if (thisParameter != null)
				this.Parameters.Add(thisParameter);
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
					if (eh.HandlerEnd == null) endInclusiveIndex = body.Count - 1;
					// Note that the end(exclusive) instruction may not necessarly be in our body
					else for (endInclusiveIndex = 0; body[endInclusiveIndex].Next.Offset != eh.HandlerEnd.Offset; endInclusiveIndex++);
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
					} else if (eh.HandlerType == ExceptionHandlerType.Fault) {
						tryCatchBlock.FaultBlock = new ILBlock(handlerAst);
					} else {
						// TODO: ExceptionHandlerType.Filter
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
					// Unreachable code
					continue;
				}
				
				ILExpression expr = new ILExpression(byteCode.Code, byteCode.Operand);
				expr.ILRanges.Add(ilRange);
				if (byteCode.Prefixes != null && byteCode.Prefixes.Length > 0) {
					ILExpressionPrefix[] prefixes = new ILExpressionPrefix[byteCode.Prefixes.Length];
					for (int i = 0; i < prefixes.Length; i++) {
						prefixes[i] = new ILExpressionPrefix((ILCode)byteCode.Prefixes[i].OpCode.Code, byteCode.Prefixes[i].Operand);
					}
					expr.Prefixes = prefixes;
				}
				
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
					foreach(ILVariable storeTo in byteCode.StoreTo.AsEnumerable().Reverse()) {
						ast.Add(new ILExpression(ILCode.Stloc, storeTo, new ILExpression(ILCode.Ldloc, tmpVar)));
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
		
		public static T[] Union<T>(this T[] a, T[] b)
		{
			if (a.Length == 0)
				return b;
			if (b.Length == 0)
				return a;
			if (a.Length == 1 && b.Length == 1 && a[0].Equals(b[0]))
				return a;
			return Enumerable.Union(a, b).ToArray();
		}
	}
}
