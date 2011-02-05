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
using System.IO;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// A block in a control flow graph; with instructions represented by "SsaInstructions" (instructions use variables, no evaluation stack).
	/// Usually these variables are in SSA form to make analysis easier.
	/// </summary>
	public sealed class SsaBlock
	{
		public readonly List<SsaBlock> Successors = new List<SsaBlock>();
		public readonly List<SsaBlock> Predecessors = new List<SsaBlock>();
		public readonly ControlFlowNodeType NodeType;
		public readonly List<SsaInstruction> Instructions = new List<SsaInstruction>();
		
		/// <summary>
		/// The block index in the control flow graph.
		/// This correspons to the node index in ControlFlowGraph.Nodes, so it can be used to retrieve the original CFG node and look
		/// up additional information (e.g. dominance).
		/// </summary>
		public readonly int BlockIndex;
		
		internal SsaBlock(ControlFlowNode node)
		{
			this.NodeType = node.NodeType;
			this.BlockIndex = node.BlockIndex;
		}
		
		public override string ToString()
		{
			StringWriter writer = new StringWriter();
			writer.Write("Block #{0} ({1})", BlockIndex, NodeType);
			foreach (SsaInstruction inst in Instructions) {
				writer.WriteLine();
				inst.WriteTo(writer);
			}
			return writer.ToString();
		}
	}
}
