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
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.FlowAnalysis;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace ICSharpCode.ILSpy.Disassembler
{
	public class ILLanguage : Language
	{
		public override string Name {
			get { return "IL"; }
		}
		
		bool detectControlStructure = true;
		
		public override void Decompile(MethodDefinition method, ITextOutput output)
		{
			output.WriteComment("// Method begins at RVA 0x{0:x4}", method.RVA);
			output.WriteLine();
			output.WriteComment("// Code size {0} (0x{0:x})", method.Body.CodeSize);
			output.WriteLine();
			output.WriteLine(".maxstack {0}", method.Body.MaxStackSize);
			if (method.DeclaringType.Module.Assembly.EntryPoint == method)
				output.WriteLine (".entrypoint");
			
			if (method.Body.HasVariables) {
				output.Write(".locals ");
				if (method.Body.InitLocals)
					output.Write("init ");
				output.WriteLine("(");
				output.Indent();
				foreach (var v in method.Body.Variables) {
					v.VariableType.WriteTo(output);
					output.Write(' ');
					output.WriteDefinition(string.IsNullOrEmpty(v.Name) ? v.Index.ToString() : v.Name, v);
					output.WriteLine();
				}
				output.Unindent();
				output.WriteLine(")");
			}
			
			if (detectControlStructure) {
				method.Body.SimplifyMacros();
				var cfg = ControlFlowGraphBuilder.Build(method.Body);
				cfg.ComputeDominance();
				cfg.ComputeDominanceFrontier();
				var s = DominanceLoopDetector.DetectLoops(cfg);
				WriteStructure(output, s);
			} else {
				foreach (var inst in method.Body.Instructions) {
					inst.WriteTo(output);
					output.WriteLine();
				}
				output.WriteLine();
				foreach (var eh in method.Body.ExceptionHandlers) {
					eh.WriteTo(output);
					output.WriteLine();
				}
			}
		}
		
		void WriteStructure(ITextOutput output, ControlStructure s)
		{
			output.WriteComment("// loop start");
			output.WriteLine();
			output.Indent();
			foreach (var node in s.Nodes.Concat(s.Children.Select(c => c.EntryPoint)).OrderBy(n => n.BlockIndex)) {
				if (s.Nodes.Contains(node)) {
					foreach (var inst in node.Instructions) {
						inst.WriteTo(output);
						output.WriteLine();
					}
				} else {
					WriteStructure(output, s.Children.Single(c => c.EntryPoint == node));
				}
			}
			output.Unindent();
			output.WriteComment("// loop end");
			output.WriteLine();
		}
	}
}
