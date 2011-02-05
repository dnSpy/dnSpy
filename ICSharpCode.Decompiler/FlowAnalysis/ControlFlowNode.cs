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
using System.Text;

using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	public enum ControlFlowNodeType
	{
		Normal,
		EntryPoint,
		RegularExit,
		ExceptionalExit,
		CatchHandler,
		FinallyOrFaultHandler,
		EndFinallyOrFault
	}
	
	public sealed class ControlFlowNode
	{
		public readonly int BlockIndex;
		public readonly ControlFlowNodeType NodeType;
		public readonly ControlFlowNode EndFinallyOrFaultNode;
		
		/// <summary>
		/// Visited flag that's used in various algorithms.
		/// </summary>
		internal bool Visited;
		
		/// <summary>
		/// Signalizes that this node is a copy of another node.
		/// </summary>
		public ControlFlowNode CopyFrom { get; internal set; }
		
		/// <summary>
		/// Gets the immediate dominator.
		/// </summary>
		public ControlFlowNode ImmediateDominator { get; internal set; }
		
		public readonly List<ControlFlowNode> DominatorTreeChildren = new List<ControlFlowNode>();
		
		public HashSet<ControlFlowNode> DominanceFrontier;
		
		/// <summary>
		/// Start of code block represented by this node.  Only set for nodetype == Normal.
		/// </summary>
		public readonly Instruction Start;
		
		/// <summary>
		/// End of the code block represented by this node. Only set for nodetype == Normal.
		/// </summary>
		public readonly Instruction End;
		
		/// <summary>
		/// Gets the exception handler associated with this node.
		/// Only set for nodetype == CatchHandler or nodetype == FinallyOrFaultHandler.
		/// </summary>
		public readonly ExceptionHandler ExceptionHandler;
		
		public readonly List<ControlFlowEdge> Incoming = new List<ControlFlowEdge>();
		public readonly List<ControlFlowEdge> Outgoing = new List<ControlFlowEdge>();
		
		internal ControlFlowNode(int blockIndex, ControlFlowNodeType nodeType)
		{
			this.BlockIndex = blockIndex;
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
		}
		
		internal ControlFlowNode(int blockIndex, ExceptionHandler exceptionHandler, ControlFlowNode endFinallyOrFaultNode)
		{
			this.BlockIndex = blockIndex;
			this.NodeType = endFinallyOrFaultNode != null ? ControlFlowNodeType.FinallyOrFaultHandler : ControlFlowNodeType.CatchHandler;
			this.ExceptionHandler = exceptionHandler;
			this.EndFinallyOrFaultNode = endFinallyOrFaultNode;
			Debug.Assert((exceptionHandler.HandlerType == ExceptionHandlerType.Finally || exceptionHandler.HandlerType == ExceptionHandlerType.Fault) == (endFinallyOrFaultNode != null));
		}
		
		public IEnumerable<ControlFlowNode> Predecessors {
			get {
				return Incoming.Select(e => e.Source);
			}
		}
		
		public IEnumerable<ControlFlowNode> Successors {
			get {
				return Outgoing.Select(e => e.Target);
			}
		}
		
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
					int endOffset = End.Next != null ? End.Next.Offset : End.Offset + End.GetSize();
					writer.Write("Block #{0}: IL_{1:x4} to IL_{2:x4}", BlockIndex, Start.Offset, endOffset);
					break;
				case ControlFlowNodeType.CatchHandler:
				case ControlFlowNodeType.FinallyOrFaultHandler:
					writer.Write("Block #{0}: {1}: ", BlockIndex, NodeType);
					ExceptionHandler.WriteTo(writer);
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
				inst.WriteTo(writer);
			}
			return writer.ToString();
		}
		
		public bool Dominates(ControlFlowNode node)
		{
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
