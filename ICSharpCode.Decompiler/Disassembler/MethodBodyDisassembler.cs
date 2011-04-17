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
using System.Linq;
using System.Threading;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.FlowAnalysis;
using ICSharpCode.Decompiler.ILAst;
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
		
		public void Disassemble(MethodBody body, MemberMapping methodMapping)
		{
			// start writing IL code
			MethodDefinition method = body.Method;
			output.WriteLine("// Method begins at RVA 0x{0:x4}", method.RVA);
			if (method.HasOverrides)
				foreach (var methodOverride in method.Overrides)
					output.WriteLine(".override {0}::{1}", methodOverride.DeclaringType.FullName, methodOverride.Name);
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
			
			if (detectControlStructure && body.Instructions.Count > 0) {
				Instruction inst = body.Instructions[0];
				WriteStructureBody(new ILStructure(body), ref inst, methodMapping, method.Body.CodeSize);
			} else {
				foreach (var inst in method.Body.Instructions) {
					inst.WriteTo(output);
					
					// add IL code mappings - used in debugger
					methodMapping.MemberCodeMappings.Add(
						new SourceCodeMapping() {
							SourceCodeLine = output.CurrentLine,
							ILInstructionOffset = new ILRange { From = inst.Offset, To = inst.Next == null ? method.Body.CodeSize : inst.Next.Offset },
							MemberMapping = methodMapping
						});
					
					output.WriteLine();
				}
				output.WriteLine();
				foreach (var eh in method.Body.ExceptionHandlers) {
					eh.WriteTo(output);
					output.WriteLine();
				}
			}
		}
		
		void WriteStructureHeader(ILStructure s)
		{
			switch (s.Type) {
				case ILStructureType.Loop:
					output.Write("// loop start");
					if (s.LoopEntryPoint != null) {
						output.Write(" (head: ");
						DisassemblerHelpers.WriteOffsetReference(output, s.LoopEntryPoint);
						output.Write(')');
					}
					output.WriteLine();
					break;
				case ILStructureType.Try:
					output.WriteLine(".try {");
					break;
				case ILStructureType.Handler:
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
				case ILStructureType.Filter:
					output.WriteLine("filter {");
					break;
				default:
					throw new NotSupportedException();
			}
			output.Indent();
		}
		
		void WriteStructureBody(ILStructure s, ref Instruction inst, MemberMapping currentMethodMapping, int codeSize)
		{
			int childIndex = 0;
			while (inst != null && inst.Offset < s.EndOffset) {
				int offset = inst.Offset;
				if (childIndex < s.Children.Count && s.Children[childIndex].StartOffset <= offset && offset < s.Children[childIndex].EndOffset) {
					ILStructure child = s.Children[childIndex++];
					WriteStructureHeader(child);
					WriteStructureBody(child, ref inst, currentMethodMapping, codeSize);
					WriteStructureFooter(child);
				} else {
					inst.WriteTo(output);
					
					// add IL code mappings - used in debugger
					if (currentMethodMapping != null) {
						currentMethodMapping.MemberCodeMappings.Add(
							new SourceCodeMapping() {
								SourceCodeLine = output.CurrentLine,
								ILInstructionOffset = new ILRange { From = inst.Offset, To = inst.Next == null ? codeSize : inst.Next.Offset },
								MemberMapping = currentMethodMapping
							});
					}
					
					output.WriteLine();
					inst = inst.Next;
				}
			}
		}
		
		void WriteStructureFooter(ILStructure s)
		{
			output.Unindent();
			switch (s.Type) {
				case ILStructureType.Loop:
					output.WriteLine("// end loop");
					break;
				case ILStructureType.Try:
					output.WriteLine("} // end .try");
					break;
				case ILStructureType.Handler:
					output.WriteLine("} // end handler");
					break;
				case ILStructureType.Filter:
					output.WriteLine("} // end filter");
					break;
				default:
					throw new NotSupportedException();
			}
		}
	}
}
