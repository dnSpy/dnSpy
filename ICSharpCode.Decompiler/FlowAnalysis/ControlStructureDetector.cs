// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler.FlowAnalysis;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// Description of DominanceLoopDetector.
	/// </summary>
	public class ControlStructureDetector
	{
		public static ControlStructure DetectStructure(ControlFlowGraph g, IEnumerable<ExceptionHandler> exceptionHandlers)
		{
			ControlStructure root = new ControlStructure(new HashSet<ControlFlowNode>(g.Nodes), g.EntryPoint, ControlStructureType.Root);
			DetectExceptionHandling(root, g, exceptionHandlers);
			DetectLoops(g, root);
			g.ResetVisited();
			return root;
		}
		
		#region Exception Handling
		static void DetectExceptionHandling(ControlStructure current, ControlFlowGraph g, IEnumerable<ExceptionHandler> exceptionHandlers)
		{
			foreach (ExceptionHandler eh in exceptionHandlers) {
				ControlStructure tryBlock = new ControlStructure(
					FindAndRemoveNodes(current, eh.TryStart, eh.TryEnd),
					g.Nodes.Single(n => n.Start == eh.TryStart),
					ControlStructureType.Try);
				tryBlock.ExceptionHandler = eh;
				MoveControlStructures(current, tryBlock, eh.TryStart, eh.TryEnd);
				current.Children.Add(tryBlock);
				
				if (eh.FilterStart != null) {
					ControlStructure filterBlock = new ControlStructure(
						FindAndRemoveNodes(current, eh.HandlerStart, eh.HandlerEnd),
						g.Nodes.Single(n => n.Start == eh.HandlerStart),
						ControlStructureType.Filter);
					filterBlock.ExceptionHandler = eh;
					MoveControlStructures(current, filterBlock, eh.FilterStart, eh.FilterEnd);
					current.Children.Add(filterBlock);
				}
				
				ControlStructure handlerBlock = new ControlStructure(
					FindAndRemoveNodes(current, eh.HandlerStart, eh.HandlerEnd),
					g.Nodes.Single(n => n.Start == eh.HandlerStart),
					ControlStructureType.Handler);
				handlerBlock.ExceptionHandler = eh;
				MoveControlStructures(current, handlerBlock, eh.HandlerStart, eh.HandlerEnd);
				current.Children.Add(handlerBlock);
			}
		}
		
		/// <summary>
		/// Removes all nodes from start to end (exclusive) from this ControlStructure and moves them to the target structure.
		/// </summary>
		static HashSet<ControlFlowNode> FindAndRemoveNodes(ControlStructure current, Instruction startInst, Instruction endInst)
		{
			HashSet<ControlFlowNode> result = new HashSet<ControlFlowNode>();
			int start = startInst.Offset;
			int end = endInst.Offset;
			foreach (var node in current.Nodes.ToArray()) {
				if (node.Start != null && node.Start.Offset >= start && node.Start.Offset < end) {
					current.Nodes.Remove(node);
					result.Add(node);
				}
			}
			return result;
		}
		
		static void MoveControlStructures(ControlStructure current, ControlStructure target, Instruction startInst, Instruction endInst)
		{
			for (int i = 0; i < current.Children.Count; i++) {
				var child = current.Children[i];
				if (child.EntryPoint.Start.Offset >= startInst.Offset && child.EntryPoint.Start.Offset <= endInst.Offset) {
					current.Children.RemoveAt(i--);
					target.Children.Add(child);
				}
			}
		}
		#endregion
		
		#region Loop Detection
		static void DetectLoops(ControlFlowGraph g, ControlStructure current)
		{
			g.ResetVisited();
			FindLoops(current, current.EntryPoint);
			foreach (ControlStructure loop in current.Children)
				DetectLoops(g, loop);
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
				current.Nodes.ExceptWith(loopContents);
				current.Children.Add(new ControlStructure(loopContents, node, ControlStructureType.Loop));
			}
			foreach (var edge in node.Outgoing) {
				FindLoops(current, edge.Target);
			}
		}
		
		static void FindLoopContents(ControlStructure current, HashSet<ControlFlowNode> loopContents, ControlFlowNode loopHead, ControlFlowNode node)
		{
			if (current.Nodes.Contains(node) && loopHead.Dominates(node) && loopContents.Add(node)) {
				foreach (var edge in node.Incoming) {
					FindLoopContents(current, loopContents, loopHead, edge.Source);
				}
			}
		}
		#endregion
	}
	
	public enum ControlStructureType
	{
		Root,
		Loop,
		Try,
		Handler,
		Filter
	}
	
	public class ControlStructure
	{
		public readonly ControlStructureType Type;
		public readonly List<ControlStructure> Children = new List<ControlStructure>();
		public readonly HashSet<ControlFlowNode> Nodes;
		public readonly ControlFlowNode EntryPoint;
		public ExceptionHandler ExceptionHandler;
		
		public ControlStructure(HashSet<ControlFlowNode> nodes, ControlFlowNode entryPoint, ControlStructureType type)
		{
			this.Nodes = nodes;
			this.EntryPoint = entryPoint;
			this.Type = type;
		}
	}
}
