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
using System.Threading;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.FlowAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Disassembles a method body.
	/// </summary>
	public sealed class MethodBodyDisassembler
	{
		readonly ITextOutput output;
		readonly bool detectControlStructure;
		readonly CancellationToken cancellationToken;
		
		public MethodBodyDisassembler(ITextOutput output, bool detectControlStructure, CancellationToken cancellationToken)
		{
			if (output == null)
				throw new ArgumentNullException("output");
			this.output = output;
			this.detectControlStructure = detectControlStructure;
			this.cancellationToken = cancellationToken;
		}
		
		public void Disassemble(MethodBody body)
		{
			MethodDefinition method = body.Method;
			output.WriteLine("// Method begins at RVA 0x{0:x4}", method.RVA);
			output.WriteLine("// Code size {0} (0x{0:x})", body.CodeSize);
			output.WriteLine(".maxstack {0}", body.MaxStackSize);
			if (method.DeclaringType.Module.Assembly.EntryPoint == method)
				output.WriteLine (".entrypoint");
			
			if (method.Body.HasVariables) {
				output.Write(".locals ");
				if (method.Body.InitLocals)
					output.Write("init ");
				output.WriteLine("(");
				output.Indent();
				foreach (var v in method.Body.Variables) {
					output.WriteDefinition("[" + v.Index + "] ", v);
					v.VariableType.WriteTo(output);
					output.Write(' ');
					output.Write(DisassemblerHelpers.Escape(v.Name));
					output.WriteLine();
				}
				output.Unindent();
				output.WriteLine(")");
			}
			output.WriteLine();
			
			if (detectControlStructure) {
				var cfg = ControlFlowGraphBuilder.Build(method.Body);
				cfg.ComputeDominance(cancellationToken);
				cfg.ComputeDominanceFrontier();
				var s = ControlStructureDetector.DetectStructure(cfg, method.Body.ExceptionHandlers, cancellationToken);
				WriteStructure(s);
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
		
		void WriteStructure(ControlStructure s)
		{
			if (s.Type != ControlStructureType.Root) {
				switch (s.Type) {
					case ControlStructureType.Loop:
						output.Write("// loop start");
						if (s.EntryPoint.Start != null) {
							output.Write(" (head: ");
							DisassemblerHelpers.WriteOffsetReference(output, s.EntryPoint.Start);
							output.Write(')');
						}
						output.WriteLine();
						break;
					case ControlStructureType.Try:
						output.WriteLine(".try {");
						break;
					case ControlStructureType.Handler:
						switch (s.ExceptionHandler.HandlerType) {
							case Mono.Cecil.Cil.ExceptionHandlerType.Catch:
							case Mono.Cecil.Cil.ExceptionHandlerType.Filter:
								output.Write("catch");
								if (s.ExceptionHandler.CatchType != null) {
									output.Write(' ');
									s.ExceptionHandler.CatchType.WriteTo(output);
								}
								output.WriteLine(" {");
								break;
							case Mono.Cecil.Cil.ExceptionHandlerType.Finally:
								output.WriteLine("finally {");
								break;
							case Mono.Cecil.Cil.ExceptionHandlerType.Fault:
								output.WriteLine("fault {");
								break;
							default:
								throw new NotSupportedException();
						}
						break;
					case ControlStructureType.Filter:
						output.WriteLine("filter {");
						break;
					default:
						throw new NotSupportedException();
				}
				output.Indent();
			}
			foreach (var node in s.Nodes.Concat(s.Children.Select(c => c.EntryPoint)).OrderBy(n => n.BlockIndex)) {
				if (s.Nodes.Contains(node)) {
					foreach (var inst in node.Instructions) {
						inst.WriteTo(output);
						output.WriteLine();
					}
				} else {
					WriteStructure(s.Children.Single(c => c.EntryPoint == node));
				}
			}
			if (s.Type != ControlStructureType.Root) {
				output.Unindent();
				switch (s.Type) {
					case ControlStructureType.Loop:
						output.WriteLine("// end loop");
						break;
					case ControlStructureType.Try:
						output.WriteLine("} // end .try");
						break;
					case ControlStructureType.Handler:
						output.WriteLine("} // end handler");
						break;
					case ControlStructureType.Filter:
						output.WriteLine("} // end filter");
						break;
					default:
						throw new NotSupportedException();
				}
			}
		}
	}
}
