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

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Description of DominanceLoopDetector.
	/// </summary>
	public class DominanceLoopDetector
	{
		public static ControlStructure DetectLoops(ControlFlowGraph g)
		{
			ControlStructure root = new ControlStructure(
				new HashSet<ControlFlowNode>(g.Nodes),
				g.EntryPoint
			);
			DetectLoops(g, root);
			g.ResetVisited();
			return root;
		}
		
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
			    && node != current.EntryPoint)
			{
				HashSet<ControlFlowNode> loopContents = new HashSet<ControlFlowNode>();
				FindLoopContents(current, loopContents, node, node);
				current.Nodes.ExceptWith(loopContents);
				current.Children.Add(new ControlStructure(loopContents, node));
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
	}
	
	public class ControlStructure
	{
		public List<ControlStructure> Children = new List<ControlStructure>();
		public HashSet<ControlFlowNode> Nodes;
		public ControlFlowNode EntryPoint;
		
		public ControlStructure(HashSet<ControlFlowNode> nodes, ControlFlowNode entryPoint)
		{
			this.Nodes = nodes;
			this.EntryPoint = entryPoint;
		}
	}
}
