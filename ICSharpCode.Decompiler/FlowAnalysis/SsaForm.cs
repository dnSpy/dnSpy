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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// Represents a graph of SsaBlocks.
	/// </summary>
	public sealed class SsaForm
	{
		readonly SsaVariable[] parameters;
		readonly SsaVariable[] locals;
		public readonly ReadOnlyCollection<SsaVariable> OriginalVariables;
		public readonly ReadOnlyCollection<SsaBlock> Blocks;
		readonly bool methodHasThis;
		
		public SsaBlock EntryPoint {
			get { return this.Blocks[0]; }
		}
		
		public SsaBlock RegularExit {
			get { return this.Blocks[1]; }
		}
		
		public SsaBlock ExceptionalExit {
			get { return this.Blocks[2]; }
		}
		
		internal SsaForm(SsaBlock[] blocks, SsaVariable[] parameters, SsaVariable[] locals, SsaVariable[] stackLocations, bool methodHasThis)
		{
			this.parameters = parameters;
			this.locals = locals;
			this.Blocks = new ReadOnlyCollection<SsaBlock>(blocks);
			this.OriginalVariables = new ReadOnlyCollection<SsaVariable>(parameters.Concat(locals).Concat(stackLocations).ToList());
			this.methodHasThis = methodHasThis;
			
			Debug.Assert(EntryPoint.NodeType == ControlFlowNodeType.EntryPoint);
			Debug.Assert(RegularExit.NodeType == ControlFlowNodeType.RegularExit);
			Debug.Assert(ExceptionalExit.NodeType == ControlFlowNodeType.ExceptionalExit);
			for (int i = 0; i < this.OriginalVariables.Count; i++) {
				this.OriginalVariables[i].OriginalVariableIndex = i;
			}
		}
		
		public GraphVizGraph ExportBlockGraph(Func<SsaBlock, string> labelProvider = null)
		{
			if (labelProvider == null)
				labelProvider = b => b.ToString();
			GraphVizGraph graph = new GraphVizGraph();
			foreach (SsaBlock block in this.Blocks) {
				graph.AddNode(new GraphVizNode(block.BlockIndex) { label = labelProvider(block), shape = "box" });
			}
			foreach (SsaBlock block in this.Blocks) {
				foreach (SsaBlock s in block.Successors) {
					graph.AddEdge(new GraphVizEdge(block.BlockIndex, s.BlockIndex));
				}
			}
			return graph;
		}
		
		public GraphVizGraph ExportVariableGraph(Func<SsaVariable, string> labelProvider = null)
		{
			if (labelProvider == null)
				labelProvider = v => v.ToString();
			GraphVizGraph graph = new GraphVizGraph();
			foreach (SsaVariable v in this.AllVariables) {
				graph.AddNode(new GraphVizNode(v.Name) { label = labelProvider(v) });
			}
			int instructionIndex = 0;
			foreach (SsaBlock block in this.Blocks) {
				foreach (SsaInstruction inst in block.Instructions) {
					if (inst.Operands.Length == 0 && inst.Target == null)
						continue;
					string id = "instruction" + (++instructionIndex);
					graph.AddNode(new GraphVizNode(id) { label = inst.ToString(), shape = "box" });
					foreach (SsaVariable op in inst.Operands)
						graph.AddEdge(new GraphVizEdge(op.Name, id));
					if (inst.Target != null)
						graph.AddEdge(new GraphVizEdge(id, inst.Target.Name));
				}
			}
			return graph;
		}
		
		public SsaVariable GetOriginalVariable(ParameterReference parameter)
		{
			if (methodHasThis)
				return parameters[parameter.Index + 1];
			else
				return parameters[parameter.Index];
		}
		
		public SsaVariable GetOriginalVariable(VariableReference variable)
		{
			return locals[variable.Index];
		}
		
		#region ComputeVariableUsage
		public void ComputeVariableUsage()
		{
			// clear data from previous runs
			foreach (SsaBlock block in this.Blocks) {
				foreach (SsaInstruction inst in block.Instructions) {
					foreach (SsaVariable v in inst.Operands) {
						if (v.Usage != null)
							v.Usage.Clear();
					}
					if (inst.Target != null && inst.Target.Usage != null)
						inst.Target.Usage.Clear();
				}
			}
			foreach (SsaBlock block in this.Blocks) {
				foreach (SsaInstruction inst in block.Instructions) {
					foreach (SsaVariable v in inst.Operands) {
						if (v.Usage == null)
							v.Usage = new List<SsaInstruction>();
						v.Usage.Add(inst);
					}
					if (inst.Target != null && inst.Target.Usage == null)
						inst.Target.Usage = new List<SsaInstruction>();
				}
			}
		}
		#endregion
		
		public IEnumerable<SsaVariable> AllVariables {
			get {
				return (
					from block in this.Blocks
					from instruction in block.Instructions
					where instruction.Target != null
					select instruction.Target
				).Distinct();
			}
		}
	}
}
