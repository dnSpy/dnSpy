// Copyright (c) 2010 Daniel Grunwald
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
	public sealed class ControlFlowGraphBuilder
	{
		public static ControlFlowGraph Build(MethodBody methodBody)
		{
			return new ControlFlowGraphBuilder(methodBody).Build();
		}
		
		MethodBody methodBody;
		int[] offsets; // array index = instruction index; value = IL offset
		bool[] hasIncomingJumps; // array index = instruction index
		List<ControlFlowNode> nodes = new List<ControlFlowNode>();
		ControlFlowNode entryPoint;
		ControlFlowNode regularExit ;
		ControlFlowNode exceptionalExit;
		//Stack<> activeExceptionHandlers = new Stack<ExceptionHandler>();
		
		private ControlFlowGraphBuilder(MethodBody methodBody)
		{
			this.methodBody = methodBody;
			offsets = methodBody.Instructions.Select(i => i.Offset).ToArray();
			hasIncomingJumps = new bool[methodBody.Instructions.Count];
			
			entryPoint = new ControlFlowNode(0, ControlFlowNodeType.EntryPoint);
			nodes.Add(entryPoint);
			regularExit = new ControlFlowNode(1, ControlFlowNodeType.RegularExit);
			nodes.Add(regularExit);
			exceptionalExit = new ControlFlowNode(2, ControlFlowNodeType.ExceptionalExit);
			nodes.Add(exceptionalExit);
			Debug.Assert(nodes.Count == 3);
		}
		
		int GetInstructionIndex(Instruction inst)
		{
			int index = Array.BinarySearch(offsets, inst.Offset);
			Debug.Assert(index >= 0);
			return index;
		}
		
		public ControlFlowGraph Build()
		{
			CalculateHasIncomingJumps();
			CreateNodes();
			CreateRegularControlFlow();
			CreateExceptionalControlFlow();
			CopyFinallyBlocksIntoLeaveEdges();
			return new ControlFlowGraph(nodes.ToArray());
		}
		
		void CalculateHasIncomingJumps()
		{
			foreach (Instruction inst in methodBody.Instructions) {
				if (inst.OpCode.OperandType == OperandType.InlineBrTarget) {
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
		
		void CreateNodes()
		{
			for (int i = 0; i < methodBody.Instructions.Count; i++) {
				Instruction blockStart = methodBody.Instructions[i];
				// try and see how big we can make that block:
				for (; i + 1 < methodBody.Instructions.Count; i++) {
					Instruction inst = methodBody.Instructions[i];
					if (IsBranch(inst.OpCode) || CanThrowException(inst.OpCode))
						break;
					if (hasIncomingJumps[i + 1])
						break;
				}
				
				nodes.Add(new ControlFlowNode(nodes.Count, blockStart, methodBody.Instructions[i]));
			}
			foreach (ExceptionHandler handler in methodBody.ExceptionHandlers) {
				if (handler.HandlerType == ExceptionHandlerType.Filter)
					throw new NotSupportedException();
				ControlFlowNode endFinallyOrFaultNode = null;
				if (handler.HandlerType == ExceptionHandlerType.Finally || handler.HandlerType == ExceptionHandlerType.Fault) {
					endFinallyOrFaultNode = new ControlFlowNode(nodes.Count, ControlFlowNodeType.EndFinallyOrFault);
					nodes.Add(endFinallyOrFaultNode);
				}
				nodes.Add(new ControlFlowNode(nodes.Count, handler, endFinallyOrFaultNode));
			}
		}
		
		void CreateRegularControlFlow()
		{
			CreateEdge(entryPoint, methodBody.Instructions[0], JumpType.Normal);
			foreach (ControlFlowNode node in nodes) {
				if (node.End != null) {
					// create normal edges from one instruction to the next
					if (!IsUnconditionalBranch(node.End.OpCode))
						CreateEdge(node, node.End.Next, JumpType.Normal);
					
					// create edges for branch instructions
					if (node.End.OpCode.OperandType == OperandType.InlineBrTarget) {
						if (node.End.OpCode == OpCodes.Leave) {
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
		
		void CreateExceptionalControlFlow()
		{
			foreach (ControlFlowNode node in nodes) {
				if (node.End != null && CanThrowException(node.End.OpCode)) {
					CreateEdge(node, FindInnermostExceptionHandler(node.End.Offset), JumpType.JumpToExceptionHandler);
				}
				if (node.ExceptionHandler != null) {
					if (node.EndFinallyOrFaultNode != null) {
						// For Fault and Finally blocks, create edge from "EndFinally" to next exception handler.
						// This represents the exception bubbling up after finally block was executed.
						CreateEdge(node.EndFinallyOrFaultNode, FindInnermostExceptionHandler(node.ExceptionHandler.HandlerEnd.Offset), JumpType.JumpToExceptionHandler);
					} else {
						// For Catch blocks, create edge from "CatchHandler" block (at beginning) to next exception handler.
						// This represents the exception bubbling up because it did not match the type of the catch block.
						CreateEdge(node, FindInnermostExceptionHandler(node.ExceptionHandler.HandlerStart.Offset), JumpType.JumpToExceptionHandler);
					}
					CreateEdge(node, node.ExceptionHandler.HandlerStart, JumpType.Normal);
				}
			}
			
			// now create edges between catch handlers that mutually protect
			for (int i = 0; i < methodBody.ExceptionHandlers.Count; i++) {
				ExceptionHandler first = methodBody.ExceptionHandlers[i];
				if (first.HandlerType == ExceptionHandlerType.Catch) {
					for (int j = i + 1; j < methodBody.ExceptionHandlers.Count; j++) {
						ExceptionHandler second = methodBody.ExceptionHandlers[j];
						if (second.HandlerType == ExceptionHandlerType.Catch && second.TryStart == first.TryStart && second.TryEnd == first.TryEnd) {
							CreateEdge(nodes.Single(n => n.ExceptionHandler == first), nodes.Single(n => n.ExceptionHandler == second), JumpType.MutualProtection);
						}
					}
				}
			}
		}
		
		ControlFlowNode FindInnermostExceptionHandler(int instructionOffsetInTryBlock)
		{
			foreach (ExceptionHandler h in methodBody.ExceptionHandlers) {
				if (h.TryStart.Offset <= instructionOffsetInTryBlock && instructionOffsetInTryBlock < h.TryEnd.Offset) {
					return nodes.Single(n => n.ExceptionHandler == h && n.CopyFrom == null);
				}
			}
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
		
		void CopyFinallyBlocksIntoLeaveEdges()
		{
			// We need to process try-finally blocks inside-out.
			// We'll do that by going through all instructions in reverse order
			for (int i = nodes.Count - 1; i >= 0; i--) {
				ControlFlowNode node = nodes[i];
				if (node.End != null && node.Outgoing.Count == 1 && node.Outgoing[0].Type == JumpType.LeaveTry) {
					Debug.Assert(node.End.OpCode == OpCodes.Leave);
					
					ControlFlowNode target = node.Outgoing[0].Target;
					// remove the edge
					target.Incoming.Remove(node.Outgoing[0]);
					node.Outgoing.Clear();
					
					ControlFlowNode handler = FindInnermostExceptionHandler(node.End.Offset);
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
		
		static bool IsUnconditionalBranch(OpCode opcode)
		{
			if (opcode.OpCodeType == OpCodeType.Prefix)
				return false;
			switch (opcode.FlowControl) {
				case FlowControl.Branch:
				case FlowControl.Throw:
				case FlowControl.Return:
					return true;
				case FlowControl.Next:
				case FlowControl.Call:
				case FlowControl.Cond_Branch:
					return false;
				default:
					throw new NotSupportedException(opcode.FlowControl.ToString());
			}
		}
		#endregion
	}
}
