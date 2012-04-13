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
using System.IO;
using System.Linq;

using ICSharpCode.Decompiler.Disassembler;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// Type of the control flow node
	/// </summary>
	public enum ControlFlowNodeType
	{
		/// <summary>
		/// A normal node represents a basic block.
		/// </summary>
		Normal,
		/// <summary>
		/// The entry point of the method.
		/// </summary>
		EntryPoint,
		/// <summary>
		/// The exit point of the method (every ret instruction branches to this node)
		/// </summary>
		RegularExit,
		/// <summary>
		/// This node represents leaving a method irregularly by throwing an exception.
		/// </summary>
		ExceptionalExit,
		/// <summary>
		/// This node is used as a header for exception handler blocks.
		/// </summary>
		CatchHandler,
		/// <summary>
		/// This node is used as a header for finally blocks and fault blocks.
		/// Every leave instruction in the try block leads to the handler of the containing finally block;
		/// and exceptional control flow also leads to this handler.
		/// </summary>
		FinallyOrFaultHandler,
		/// <summary>
		/// This node is used as footer for finally blocks and fault blocks.
		/// Depending on the "copyFinallyBlocks" option used when creating the graph, it is connected with all leave targets using
		/// EndFinally edges (when not copying); or with a specific leave target using a normal edge (when copying).
		/// For fault blocks, an exception edge is used to represent the "re-throwing" of the exception.
		/// </summary>
		EndFinallyOrFault
	}
	
	/// <summary>
	/// Represents a block in the control flow graph.
	/// </summary>
	public sealed class ControlFlowNode
	{
		/// <summary>
		/// Index of this node in the ControlFlowGraph.Nodes collection.
		/// </summary>
		public readonly int BlockIndex;
		
		/// <summary>
		/// Gets the IL offset of this node.
		/// </summary>
		public readonly int Offset;
		
		/// <summary>
		/// Type of the node.
		/// </summary>
		public readonly ControlFlowNodeType NodeType;
		
		/// <summary>
		/// If this node is a FinallyOrFaultHandler node, this field points to the corresponding EndFinallyOrFault node.
		/// Otherwise, this field is null.
		/// </summary>
		public readonly ControlFlowNode EndFinallyOrFaultNode;
		
		/// <summary>
		/// Visited flag, used in various algorithms.
		/// Before using it in your algorithm, reset it to false by calling ControlFlowGraph.ResetVisited();
		/// </summary>
		public bool Visited;
		
		/// <summary>
		/// Gets whether this node is reachable. Requires that dominance is computed!
		/// </summary>
		public bool IsReachable {
			get { return ImmediateDominator != null || NodeType == ControlFlowNodeType.EntryPoint; }
		}
		
		/// <summary>
		/// Signalizes that this node is a copy of another node.
		/// </summary>
		public ControlFlowNode CopyFrom { get; internal set; }
		
		/// <summary>
		/// Gets the immediate dominator (the parent in the dominator tree).
		/// Null if dominance has not been calculated; or if the node is unreachable.
		/// </summary>
		public ControlFlowNode ImmediateDominator { get; internal set; }
		
		/// <summary>
		/// List of children in the dominator tree.
		/// </summary>
		public readonly List<ControlFlowNode> DominatorTreeChildren = new List<ControlFlowNode>();
		
		/// <summary>
		/// The dominance frontier of this node.
		/// This is the set of nodes for which this node dominates a predecessor, but which are not strictly dominated by this node.
		/// </summary>
		/// <remarks>
		/// b.DominanceFrontier = { y in CFG; (exists p in predecessors(y): b dominates p) and not (b strictly dominates y)}
		/// </remarks>
		public HashSet<ControlFlowNode> DominanceFrontier;
		
		/// <summary>
		/// Start of code block represented by this node. Only set for nodetype == Normal.
		/// </summary>
		public readonly Instruction Start;
		
		/// <summary>
		/// End of the code block represented by this node. Only set for nodetype == Normal.
		/// The end is exclusive, the end instruction itself does not belong to this block.
		/// </summary>
		public readonly Instruction End;
		
		/// <summary>
		/// Gets the exception handler associated with this node.
		/// Only set for nodetype == CatchHandler or nodetype == FinallyOrFaultHandler.
		/// </summary>
		public readonly ExceptionHandler ExceptionHandler;
		
		/// <summary>
		/// List of incoming control flow edges.
		/// </summary>
		public readonly List<ControlFlowEdge> Incoming = new List<ControlFlowEdge>();
		
		/// <summary>
		/// List of outgoing control flow edges.
		/// </summary>
		public readonly List<ControlFlowEdge> Outgoing = new List<ControlFlowEdge>();
		
		/// <summary>
		/// Any user data
		/// </summary>
		public object UserData;
		
		internal ControlFlowNode(int blockIndex, int offset, ControlFlowNodeType nodeType)
		{
			this.BlockIndex = blockIndex;
			this.Offset = offset;
			this.NodeType = nodeType;
		}
		
		internal ControlFlowNode(int blockIndex, Instruction start, Instruction end)
		{
			if (start == null)
				throw new ArgumentNullException("start");
			if (end == null)
				throw new ArgumentNullException("end");
			this.BlockIndex = blockIndex;
			this.NodeType = ControlFlowNodeType.Normal;
			this.Start = start;
			this.End = end;
			this.Offset = start.Offset;
		}
		
		internal ControlFlowNode(int blockIndex, ExceptionHandler exceptionHandler, ControlFlowNode endFinallyOrFaultNode)
		{
			this.BlockIndex = blockIndex;
			this.NodeType = endFinallyOrFaultNode != null ? ControlFlowNodeType.FinallyOrFaultHandler : ControlFlowNodeType.CatchHandler;
			this.ExceptionHandler = exceptionHandler;
			this.EndFinallyOrFaultNode = endFinallyOrFaultNode;
			Debug.Assert((exceptionHandler.HandlerType == ExceptionHandlerType.Finally || exceptionHandler.HandlerType == ExceptionHandlerType.Fault) == (endFinallyOrFaultNode != null));
			this.Offset = exceptionHandler.HandlerStart.Offset;
		}
		
		/// <summary>
		/// Gets all predecessors (=sources of incoming edges)
		/// </summary>
		public IEnumerable<ControlFlowNode> Predecessors {
			get {
				return Incoming.Select(e => e.Source);
			}
		}
		
		/// <summary>
		/// Gets all successors (=targets of outgoing edges)
		/// </summary>
		public IEnumerable<ControlFlowNode> Successors {
			get {
				return Outgoing.Select(e => e.Target);
			}
		}
		
		/// <summary>
		/// Gets all instructions in this node.
		/// Returns an empty list for special nodes that don't have any instructions.
		/// </summary>
		public IEnumerable<Instruction> Instructions {
			get {
				Instruction inst = Start;
				if (inst != null) {
					yield return inst;
					while (inst != End) {
						inst = inst.Next;
						yield return inst;
					}
				}
			}
		}
		
		public void TraversePreOrder(Func<ControlFlowNode, IEnumerable<ControlFlowNode>> children, Action<ControlFlowNode> visitAction)
		{
			if (Visited)
				return;
			Visited = true;
			visitAction(this);
			foreach (ControlFlowNode t in children(this))
				t.TraversePreOrder(children, visitAction);
		}
		
		public void TraversePostOrder(Func<ControlFlowNode, IEnumerable<ControlFlowNode>> children, Action<ControlFlowNode> visitAction)
		{
			if (Visited)
				return;
			Visited = true;
			foreach (ControlFlowNode t in children(this))
				t.TraversePostOrder(children, visitAction);
			visitAction(this);
		}
		
		public override string ToString()
		{
			StringWriter writer = new StringWriter();
			switch (NodeType) {
				case ControlFlowNodeType.Normal:
					writer.Write("Block #{0}", BlockIndex);
					if (Start != null)
						writer.Write(": IL_{0:x4}", Start.Offset);
					if (End != null)
						writer.Write(" to IL_{0:x4}", End.GetEndOffset());
					break;
				case ControlFlowNodeType.CatchHandler:
				case ControlFlowNodeType.FinallyOrFaultHandler:
					writer.Write("Block #{0}: {1}: ", BlockIndex, NodeType);
					Disassembler.DisassemblerHelpers.WriteTo(ExceptionHandler, new PlainTextOutput(writer));
					break;
				default:
					writer.Write("Block #{0}: {1}", BlockIndex, NodeType);
					break;
			}
//			if (ImmediateDominator != null) {
//				writer.WriteLine();
//				writer.Write("ImmediateDominator: #{0}", ImmediateDominator.BlockIndex);
//			}
			if (DominanceFrontier != null && DominanceFrontier.Any()) {
				writer.WriteLine();
				writer.Write("DominanceFrontier: " + string.Join(",", DominanceFrontier.OrderBy(d => d.BlockIndex).Select(d => d.BlockIndex.ToString())));
			}
			foreach (Instruction inst in this.Instructions) {
				writer.WriteLine();
				Disassembler.DisassemblerHelpers.WriteTo(inst, new PlainTextOutput(writer));
			}
			if (UserData != null) {
				writer.WriteLine();
				writer.Write(UserData.ToString());
			}
			return writer.ToString();
		}
		
		/// <summary>
		/// Gets whether <c>this</c> dominates <paramref name="node"/>.
		/// </summary>
		public bool Dominates(ControlFlowNode node)
		{
			// TODO: this can be made O(1) by numbering the dominator tree
			ControlFlowNode tmp = node;
			while (tmp != null) {
				if (tmp == this)
					return true;
				tmp = tmp.ImmediateDominator;
			}
			return false;
		}
	}
}
