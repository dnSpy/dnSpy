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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// Constructs the Control Flow Graph from a Cecil method body.
	/// </summary>
	public sealed class ControlFlowGraphBuilder
	{
		public static ControlFlowGraph Build(MethodBody methodBody)
		{
			return new ControlFlowGraphBuilder(methodBody).Build();
		}
		
		// This option controls how finally blocks are handled:
		// false means that the endfinally instruction will jump to any of the leave targets (EndFinally edge type).
		// true means that a copy of the whole finally block is created for each leave target. In this case, each endfinally node will be connected with the leave
		//   target using a normal edge.
		bool copyFinallyBlocks = false;
		
		MethodBody methodBody;
		int[] offsets; // array index = instruction index; value = IL offset
		bool[] hasIncomingJumps; // array index = instruction index
		List<ControlFlowNode> nodes = new List<ControlFlowNode>();
		ControlFlowNode entryPoint;
		ControlFlowNode regularExit;
		ControlFlowNode exceptionalExit;
		
		private ControlFlowGraphBuilder(MethodBody methodBody)
		{
			this.methodBody = methodBody;
			offsets = methodBody.Instructions.Select(i => i.Offset).ToArray();
			hasIncomingJumps = new bool[methodBody.Instructions.Count];
			
			entryPoint = new ControlFlowNode(0, 0, ControlFlowNodeType.EntryPoint);
			nodes.Add(entryPoint);
			regularExit = new ControlFlowNode(1, -1, ControlFlowNodeType.RegularExit);
			nodes.Add(regularExit);
			exceptionalExit = new ControlFlowNode(2, -1, ControlFlowNodeType.ExceptionalExit);
			nodes.Add(exceptionalExit);
			Debug.Assert(nodes.Count == 3);
		}
		
		/// <summary>
		/// Determines the index of the instruction (for use with the hasIncomingJumps array)
		/// </summary>
		int GetInstructionIndex(Instruction inst)
		{
			int index = Array.BinarySearch(offsets, inst.Offset);
			Debug.Assert(index >= 0);
			return index;
		}
		
		/// <summary>
		/// Builds the ControlFlowGraph.
		/// </summary>
		public ControlFlowGraph Build()
		{
			CalculateHasIncomingJumps();
			CreateNodes();
			CreateRegularControlFlow();
			CreateExceptionalControlFlow();
			if (copyFinallyBlocks)
				CopyFinallyBlocksIntoLeaveEdges();
			else
				TransformLeaveEdges();
			return new ControlFlowGraph(nodes.ToArray());
		}
		
		#region Step 1: calculate which instructions are the targets of jump instructions.
		void CalculateHasIncomingJumps()
		{
			foreach (Instruction inst in methodBody.Instructions) {
				if (inst.OpCode.OperandType == OperandType.InlineBrTarget || inst.OpCode.OperandType == OperandType.ShortInlineBrTarget) {
					hasIncomingJumps[GetInstructionIndex((Instruction)inst.Operand)] = true;
				} else if (inst.OpCode.OperandType == OperandType.InlineSwitch) {
					foreach (Instruction i in (Instruction[])inst.Operand)
						hasIncomingJumps[GetInstructionIndex(i)] = true;
				}
			}
			foreach (ExceptionHandler eh in methodBody.ExceptionHandlers) {
				if (eh.FilterStart != null) {
					hasIncomingJumps[GetInstructionIndex(eh.FilterStart)] = true;
				}
				hasIncomingJumps[GetInstructionIndex(eh.HandlerStart)] = true;
			}
		}
		#endregion
		
		#region Step 2: create nodes
		void CreateNodes()
		{
			// Step 2a: find basic blocks and create nodes for them
			for (int i = 0; i < methodBody.Instructions.Count; i++) {
				Instruction blockStart = methodBody.Instructions[i];
				ExceptionHandler blockStartEH = FindInnermostExceptionHandler(blockStart.Offset);
				// try and see how big we can make that block:
				for (; i + 1 < methodBody.Instructions.Count; i++) {
					Instruction inst = methodBody.Instructions[i];
					if (IsBranch(inst.OpCode) || CanThrowException(inst.OpCode))
						break;
					if (hasIncomingJumps[i + 1])
						break;
					if (inst.Next != null) {
						// ensure that blocks never contain instructions from different try blocks
						ExceptionHandler instEH = FindInnermostExceptionHandler(inst.Next.Offset);
						if (instEH != blockStartEH)
							break;
					}
				}
				
				nodes.Add(new ControlFlowNode(nodes.Count, blockStart, methodBody.Instructions[i]));
			}
			// Step 2b: Create special nodes for the exception handling constructs
			foreach (ExceptionHandler handler in methodBody.ExceptionHandlers) {
				if (handler.HandlerType == ExceptionHandlerType.Filter)
					throw new NotSupportedException();
				ControlFlowNode endFinallyOrFaultNode = null;
				if (handler.HandlerType == ExceptionHandlerType.Finally || handler.HandlerType == ExceptionHandlerType.Fault) {
					endFinallyOrFaultNode = new ControlFlowNode(nodes.Count, handler.HandlerEnd.Offset, ControlFlowNodeType.EndFinallyOrFault);
					nodes.Add(endFinallyOrFaultNode);
				}
				nodes.Add(new ControlFlowNode(nodes.Count, handler, endFinallyOrFaultNode));
			}
		}
		#endregion
		
		#region Step 3: create edges for the normal flow of control (assuming no exceptions thrown)
		void CreateRegularControlFlow()
		{
			CreateEdge(entryPoint, methodBody.Instructions[0], JumpType.Normal);
			foreach (ControlFlowNode node in nodes) {
				if (node.End != null) {
					// create normal edges from one instruction to the next
					if (!OpCodeInfo.IsUnconditionalBranch(node.End.OpCode))
						CreateEdge(node, node.End.Next, JumpType.Normal);
					
					// create edges for branch instructions
					if (node.End.OpCode.OperandType == OperandType.InlineBrTarget || node.End.OpCode.OperandType == OperandType.ShortInlineBrTarget) {
						if (node.End.OpCode == OpCodes.Leave || node.End.OpCode == OpCodes.Leave_S) {
							var handlerBlock = FindInnermostHandlerBlock(node.End.Offset);
							if (handlerBlock.NodeType == ControlFlowNodeType.FinallyOrFaultHandler)
								CreateEdge(node, (Instruction)node.End.Operand, JumpType.LeaveTry);
							else
								CreateEdge(node, (Instruction)node.End.Operand, JumpType.Normal);
						} else {
							CreateEdge(node, (Instruction)node.End.Operand, JumpType.Normal);
						}
					} else if (node.End.OpCode.OperandType == OperandType.InlineSwitch) {
						foreach (Instruction i in (Instruction[])node.End.Operand)
							CreateEdge(node, i, JumpType.Normal);
					}
					
					// create edges for return instructions
					if (node.End.OpCode.FlowControl == FlowControl.Return) {
						switch (node.End.OpCode.Code) {
							case Code.Ret:
								CreateEdge(node, regularExit, JumpType.Normal);
								break;
							case Code.Endfinally:
								ControlFlowNode handlerBlock = FindInnermostHandlerBlock(node.End.Offset);
								if (handlerBlock.EndFinallyOrFaultNode == null)
									throw new InvalidProgramException("Found endfinally in block " + handlerBlock);
								CreateEdge(node, handlerBlock.EndFinallyOrFaultNode, JumpType.Normal);
								break;
							default:
								throw new NotSupportedException(node.End.OpCode.ToString());
						}
					}
				}
			}
		}
		#endregion
		
		#region Step 4: create edges for the exceptional control flow (from instructions that might throw, to the innermost containing exception handler)
		void CreateExceptionalControlFlow()
		{
			foreach (ControlFlowNode node in nodes) {
				if (node.End != null && CanThrowException(node.End.OpCode)) {
					CreateEdge(node, FindInnermostExceptionHandlerNode(node.End.Offset), JumpType.JumpToExceptionHandler);
				}
				if (node.ExceptionHandler != null) {
					if (node.EndFinallyOrFaultNode != null) {
						// For Fault and Finally blocks, create edge from "EndFinally" to next exception handler.
						// This represents the exception bubbling up after finally block was executed.
						CreateEdge(node.EndFinallyOrFaultNode, FindParentExceptionHandlerNode(node), JumpType.JumpToExceptionHandler);
					} else {
						// For Catch blocks, create edge from "CatchHandler" block (at beginning) to next exception handler.
						// This represents the exception bubbling up because it did not match the type of the catch block.
						CreateEdge(node, FindParentExceptionHandlerNode(node), JumpType.JumpToExceptionHandler);
					}
					CreateEdge(node, node.ExceptionHandler.HandlerStart, JumpType.Normal);
				}
			}
		}
		
		ExceptionHandler FindInnermostExceptionHandler(int instructionOffsetInTryBlock)
		{
			foreach (ExceptionHandler h in methodBody.ExceptionHandlers) {
				if (h.TryStart.Offset <= instructionOffsetInTryBlock && instructionOffsetInTryBlock < h.TryEnd.Offset) {
					return h;
				}
			}
			return null;
		}
		
		ControlFlowNode FindInnermostExceptionHandlerNode(int instructionOffsetInTryBlock)
		{
			ExceptionHandler h = FindInnermostExceptionHandler(instructionOffsetInTryBlock);
			if (h != null)
				return nodes.Single(n => n.ExceptionHandler == h && n.CopyFrom == null);
			else
				return exceptionalExit;
		}
		
		ControlFlowNode FindInnermostHandlerBlock(int instructionOffset)
		{
			foreach (ExceptionHandler h in methodBody.ExceptionHandlers) {
				if (h.TryStart.Offset <= instructionOffset && instructionOffset < h.TryEnd.Offset
				    || h.HandlerStart.Offset <= instructionOffset && instructionOffset < h.HandlerEnd.Offset)
				{
					return nodes.Single(n => n.ExceptionHandler == h && n.CopyFrom == null);
				}
			}
			return exceptionalExit;
		}
		
		ControlFlowNode FindParentExceptionHandlerNode(ControlFlowNode exceptionHandler)
		{
			Debug.Assert(exceptionHandler.NodeType == ControlFlowNodeType.CatchHandler
			             || exceptionHandler.NodeType == ControlFlowNodeType.FinallyOrFaultHandler);
			int offset = exceptionHandler.ExceptionHandler.TryStart.Offset;
			for (int i = exceptionHandler.BlockIndex + 1; i < nodes.Count; i++) {
				ExceptionHandler h = nodes[i].ExceptionHandler;
				if (h != null && h.TryStart.Offset <= offset && offset < h.TryEnd.Offset)
					return nodes[i];
			}
			return exceptionalExit;
		}
		#endregion
		
		#region Step 5a: replace LeaveTry edges with EndFinally edges
		// this is used only for copyFinallyBlocks==false; see Step 5b otherwise
		void TransformLeaveEdges()
		{
			for (int i = nodes.Count - 1; i >= 0; i--) {
				ControlFlowNode node = nodes[i];
				if (node.End != null && node.Outgoing.Count == 1 && node.Outgoing[0].Type == JumpType.LeaveTry) {
					Debug.Assert(node.End.OpCode == OpCodes.Leave || node.End.OpCode == OpCodes.Leave_S);
					
					ControlFlowNode target = node.Outgoing[0].Target;
					// remove the edge
					target.Incoming.Remove(node.Outgoing[0]);
					node.Outgoing.Clear();
					
					ControlFlowNode handler = FindInnermostExceptionHandlerNode(node.End.Offset);
					Debug.Assert(handler.NodeType == ControlFlowNodeType.FinallyOrFaultHandler);
					
					CreateEdge(node, handler, JumpType.Normal);
					CreateEdge(handler.EndFinallyOrFaultNode, target, JumpType.EndFinally);
				}
			}
		}
		#endregion
		
		#region Step 5b: copy finally blocks into the LeaveTry edges
		void CopyFinallyBlocksIntoLeaveEdges()
		{
			// We need to process try-finally blocks inside-out.
			// We'll do that by going through all instructions in reverse order
			for (int i = nodes.Count - 1; i >= 0; i--) {
				ControlFlowNode node = nodes[i];
				if (node.End != null && node.Outgoing.Count == 1 && node.Outgoing[0].Type == JumpType.LeaveTry) {
					Debug.Assert(node.End.OpCode == OpCodes.Leave || node.End.OpCode == OpCodes.Leave_S);
					
					ControlFlowNode target = node.Outgoing[0].Target;
					// remove the edge
					target.Incoming.Remove(node.Outgoing[0]);
					node.Outgoing.Clear();
					
					ControlFlowNode handler = FindInnermostExceptionHandlerNode(node.End.Offset);
					Debug.Assert(handler.NodeType == ControlFlowNodeType.FinallyOrFaultHandler);
					
					ControlFlowNode copy = CopyFinallySubGraph(handler, handler.EndFinallyOrFaultNode, target);
					CreateEdge(node, copy, JumpType.Normal);
				}
			}
		}
		
		/// <summary>
		/// Creates a copy of all nodes pointing to 'end' and replaces those references with references to 'newEnd'.
		/// Nodes pointing to the copied node are copied recursively to update those references, too.
		/// This recursion stops at 'start'. The modified version of start is returned.
		/// </summary>
		ControlFlowNode CopyFinallySubGraph(ControlFlowNode start, ControlFlowNode end, ControlFlowNode newEnd)
		{
			return new CopyFinallySubGraphLogic(this, start, end, newEnd).CopyFinallySubGraph();
		}
		
		class CopyFinallySubGraphLogic
		{
			readonly ControlFlowGraphBuilder builder;
			readonly Dictionary<ControlFlowNode, ControlFlowNode> oldToNew = new Dictionary<ControlFlowNode, ControlFlowNode>();
			readonly ControlFlowNode start;
			readonly ControlFlowNode end;
			readonly ControlFlowNode newEnd;
			
			public CopyFinallySubGraphLogic(ControlFlowGraphBuilder builder, ControlFlowNode start, ControlFlowNode end, ControlFlowNode newEnd)
			{
				this.builder = builder;
				this.start = start;
				this.end = end;
				this.newEnd = newEnd;
			}
			
			internal ControlFlowNode CopyFinallySubGraph()
			{
				foreach (ControlFlowNode n in end.Predecessors) {
					CollectNodes(n);
				}
				foreach (var pair in oldToNew)
					ReconstructEdges(pair.Key, pair.Value);
				return GetNew(start);
			}
			
			void CollectNodes(ControlFlowNode node)
			{
				if (node == end || node == newEnd)
					throw new InvalidOperationException("unexpected cycle involving finally construct");
				if (!oldToNew.ContainsKey(node)) {
					int newBlockIndex = builder.nodes.Count;
					ControlFlowNode copy;
					switch (node.NodeType) {
						case ControlFlowNodeType.Normal:
							copy = new ControlFlowNode(newBlockIndex, node.Start, node.End);
							break;
						case ControlFlowNodeType.FinallyOrFaultHandler:
							copy = new ControlFlowNode(newBlockIndex, node.ExceptionHandler, node.EndFinallyOrFaultNode);
							break;
						default:
							// other nodes shouldn't occur when copying finally blocks
							throw new NotSupportedException(node.NodeType.ToString());
					}
					copy.CopyFrom = node;
					builder.nodes.Add(copy);
					oldToNew.Add(node, copy);
					
					if (node != start) {
						foreach (ControlFlowNode n in node.Predecessors) {
							CollectNodes(n);
						}
					}
				}
			}
			
			void ReconstructEdges(ControlFlowNode oldNode, ControlFlowNode newNode)
			{
				foreach (ControlFlowEdge oldEdge in oldNode.Outgoing) {
					builder.CreateEdge(newNode, GetNew(oldEdge.Target), oldEdge.Type);
				}
			}
			
			ControlFlowNode GetNew(ControlFlowNode oldNode)
			{
				if (oldNode == end)
					return newEnd;
				ControlFlowNode newNode;
				if (oldToNew.TryGetValue(oldNode, out newNode))
					return newNode;
				return oldNode;
			}
		}
		#endregion
		
		#region CreateEdge methods
		void CreateEdge(ControlFlowNode fromNode, Instruction toInstruction, JumpType type)
		{
			CreateEdge(fromNode, nodes.Single(n => n.Start == toInstruction), type);
		}
		
		void CreateEdge(ControlFlowNode fromNode, ControlFlowNode toNode, JumpType type)
		{
			ControlFlowEdge edge = new ControlFlowEdge(fromNode, toNode, type);
			fromNode.Outgoing.Add(edge);
			toNode.Incoming.Add(edge);
		}
		#endregion
		
		#region OpCode info
		static bool CanThrowException(OpCode opcode)
		{
			if (opcode.OpCodeType == OpCodeType.Prefix)
				return false;
			return OpCodeInfo.Get(opcode).CanThrow;
		}
		
		static bool IsBranch(OpCode opcode)
		{
			if (opcode.OpCodeType == OpCodeType.Prefix)
				return false;
			switch (opcode.FlowControl) {
				case FlowControl.Cond_Branch:
				case FlowControl.Branch:
				case FlowControl.Throw:
				case FlowControl.Return:
					return true;
				case FlowControl.Next:
				case FlowControl.Call:
					return false;
				default:
					throw new NotSupportedException(opcode.FlowControl.ToString());
			}
		}
		#endregion
	}
}
