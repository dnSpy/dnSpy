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
using System.Threading;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// Detects the structure of the control flow (exception blocks and loops).
	/// </summary>
	public class ControlStructureDetector
	{
		public static ControlStructure DetectStructure(ControlFlowGraph g, IEnumerable<ExceptionHandler> exceptionHandlers, CancellationToken cancellationToken)
		{
			ControlStructure root = new ControlStructure(new HashSet<ControlFlowNode>(g.Nodes), g.EntryPoint, ControlStructureType.Root);
			// First build a structure tree out of the exception table
			DetectExceptionHandling(root, g, exceptionHandlers);
			// Then run the loop detection.
			DetectLoops(g, root, cancellationToken);
			return root;
		}
		
		#region Exception Handling
		static void DetectExceptionHandling(ControlStructure current, ControlFlowGraph g, IEnumerable<ExceptionHandler> exceptionHandlers)
		{
			// We rely on the fact that the exception handlers are sorted so that the innermost come first.
			// For each exception handler, we determine the nodes and substructures inside that handler, and move them into a new substructure.
			// This is always possible because exception handlers are guaranteed (by the CLR spec) to be properly nested and non-overlapping;
			// so they directly form the tree that we need.
			foreach (ExceptionHandler eh in exceptionHandlers) {
				var tryNodes = FindNodes(current, eh.TryStart, eh.TryEnd);
				current.Nodes.ExceptWith(tryNodes);
				ControlStructure tryBlock = new ControlStructure(
					tryNodes,
					g.Nodes.Single(n => n.Start == eh.TryStart),
					ControlStructureType.Try);
				tryBlock.ExceptionHandler = eh;
				MoveControlStructures(current, tryBlock, eh.TryStart, eh.TryEnd);
				current.Children.Add(tryBlock);
				
				if (eh.FilterStart != null) {
					throw new NotSupportedException();
				}
				
				var handlerNodes = FindNodes(current, eh.HandlerStart, eh.HandlerEnd);
				var handlerNode = current.Nodes.Single(n => n.ExceptionHandler == eh);
				handlerNodes.Add(handlerNode);
				if (handlerNode.EndFinallyOrFaultNode != null)
					handlerNodes.Add(handlerNode.EndFinallyOrFaultNode);
				current.Nodes.ExceptWith(handlerNodes);
				ControlStructure handlerBlock = new ControlStructure(
					handlerNodes, handlerNode, ControlStructureType.Handler);
				handlerBlock.ExceptionHandler = eh;
				MoveControlStructures(current, handlerBlock, eh.HandlerStart, eh.HandlerEnd);
				current.Children.Add(handlerBlock);
			}
		}
		
		/// <summary>
		/// Removes all nodes from start to end (exclusive) from this ControlStructure and moves them to the target structure.
		/// </summary>
		static HashSet<ControlFlowNode> FindNodes(ControlStructure current, Instruction startInst, Instruction endInst)
		{
			HashSet<ControlFlowNode> result = new HashSet<ControlFlowNode>();
			int start = startInst.Offset;
			int end = endInst.Offset;
			foreach (var node in current.Nodes.ToArray()) {
				if (node.Start != null && start <= node.Start.Offset && node.Start.Offset < end) {
					result.Add(node);
				}
			}
			return result;
		}
		
		static void MoveControlStructures(ControlStructure current, ControlStructure target, Instruction startInst, Instruction endInst)
		{
			for (int i = 0; i < current.Children.Count; i++) {
				var child = current.Children[i];
				if (startInst.Offset <= child.EntryPoint.Offset && child.EntryPoint.Offset < endInst.Offset) {
					current.Children.RemoveAt(i--);
					target.Children.Add(child);
					target.AllNodes.UnionWith(child.AllNodes);
				}
			}
		}
		#endregion
		
		#region Loop Detection
		// Loop detection works like this:
		// We find a top-level loop by looking for its entry point, which is characterized by a node dominating its own predecessor.
		// Then we determine all other nodes that belong to such a loop (all nodes which lead to the entry point, and are dominated by it).
		// Finally, we check whether our result conforms with potential existing exception structures, and create the substructure for the loop if successful.
		
		// This algorithm is applied recursively for any substructures (both detected loops and exception blocks)
		
		// But maybe we should get rid of this complex stuff and instead treat every backward jump as a loop?
		// That should still work with the IL produced by compilers, and has the advantage that the detected loop bodies are consecutive IL regions.
		
		static void DetectLoops(ControlFlowGraph g, ControlStructure current, CancellationToken cancellationToken)
		{
			if (!current.EntryPoint.IsReachable)
				return;
			g.ResetVisited();
			cancellationToken.ThrowIfCancellationRequested();
			FindLoops(current, current.EntryPoint);
			foreach (ControlStructure loop in current.Children)
				DetectLoops(g, loop, cancellationToken);
		}
		
		static void FindLoops(ControlStructure current, ControlFlowNode node)
		{
			if (node.Visited)
				return;
			node.Visited = true;
			if (current.Nodes.Contains(node)
			    && node.DominanceFrontier.Contains(node)
			    && !(node == current.EntryPoint && current.Type == ControlStructureType.Loop))
			{
				HashSet<ControlFlowNode> loopContents = new HashSet<ControlFlowNode>();
				FindLoopContents(current, loopContents, node, node);
				List<ControlStructure> containedChildStructures = new List<ControlStructure>();
				bool invalidNesting = false;
				foreach (ControlStructure childStructure in current.Children) {
					if (childStructure.AllNodes.IsSubsetOf(loopContents)) {
						containedChildStructures.Add(childStructure);
					} else if (childStructure.AllNodes.Intersect(loopContents).Any()) {
						invalidNesting = true;
					}
				}
				if (!invalidNesting) {
					current.Nodes.ExceptWith(loopContents);
					ControlStructure ctl = new ControlStructure(loopContents, node, ControlStructureType.Loop);
					foreach (ControlStructure childStructure in containedChildStructures) {
						ctl.Children.Add(childStructure);
						current.Children.Remove(childStructure);
						ctl.Nodes.ExceptWith(childStructure.AllNodes);
					}
					current.Children.Add(ctl);
				}
			}
			foreach (var edge in node.Outgoing) {
				FindLoops(current, edge.Target);
			}
		}
		
		static void FindLoopContents(ControlStructure current, HashSet<ControlFlowNode> loopContents, ControlFlowNode loopHead, ControlFlowNode node)
		{
			if (current.AllNodes.Contains(node) && loopHead.Dominates(node) && loopContents.Add(node)) {
				foreach (var edge in node.Incoming) {
					FindLoopContents(current, loopContents, loopHead, edge.Source);
				}
			}
		}
		#endregion
	}
	
	public enum ControlStructureType
	{
		/// <summary>
		/// The root block of the method
		/// </summary>
		Root,
		/// <summary>
		/// A nested control structure representing a loop.
		/// </summary>
		Loop,
		/// <summary>
		/// A nested control structure representing a try block.
		/// </summary>
		Try,
		/// <summary>
		/// A nested control structure representing a catch, finally, or fault block.
		/// </summary>
		Handler,
		/// <summary>
		/// A nested control structure representing an exception filter block.
		/// </summary>
		Filter
	}
	
	/// <summary>
	/// Represents the structure detected by the <see cref="ControlStructureDetector"/>.
	/// 
	/// This is a tree of ControlStructure nodes. Each node contains a set of CFG nodes, and every CFG node is contained in exactly one ControlStructure node.
	/// </summary>
	public class ControlStructure
	{
		public readonly ControlStructureType Type;
		public readonly List<ControlStructure> Children = new List<ControlStructure>();
		
		/// <summary>
		/// The nodes in this control structure.
		/// </summary>
		public readonly HashSet<ControlFlowNode> Nodes;
		
		/// <summary>
		/// The nodes in this control structure and in all child control structures.
		/// </summary>
		public readonly HashSet<ControlFlowNode> AllNodes;
		
		/// <summary>
		/// The entry point of this control structure.
		/// </summary>
		public readonly ControlFlowNode EntryPoint;
		
		/// <summary>
		/// The exception handler associated with this Try,Handler or Finally structure.
		/// </summary>
		public ExceptionHandler ExceptionHandler;
		
		public ControlStructure(HashSet<ControlFlowNode> nodes, ControlFlowNode entryPoint, ControlStructureType type)
		{
			if (nodes == null)
				throw new ArgumentNullException("nodes");
			this.Nodes = nodes;
			this.EntryPoint = entryPoint;
			this.Type = type;
			this.AllNodes = new HashSet<ControlFlowNode>(nodes);
		}
	}
}
