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
using System.Threading;

using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	class AnalyzedMethodUsedByTreeNode : ILSpyTreeNode
	{
		MethodDefinition analyzedMethod;
		ThreadingSupport threading;
		
		public AnalyzedMethodUsedByTreeNode(MethodDefinition analyzedMethod)
		{
			if (analyzedMethod == null)
				throw new ArgumentNullException("analyzedMethod");
			
			this.analyzedMethod = analyzedMethod;
			this.threading = new ThreadingSupport();
			this.LazyLoading = true;
		}
		
		public override object Text {
			get { return "Used By"; }
		}
		
		public override object Icon {
			get { return Images.Search; }
		}
		
		protected override void LoadChildren()
		{
			threading.LoadChildren(this, FetchChildren);
		}
		
		IEnumerable<ILSpyTreeNode> FetchChildren(CancellationToken ct)
		{
			return FindReferences(MainWindow.Instance.AssemblyList.GetAssemblies(), ct);
		}
		
		IEnumerable<ILSpyTreeNode> FindReferences(LoadedAssembly[] assemblies, CancellationToken ct)
		{
			foreach (LoadedAssembly asm in assemblies) {
				ct.ThrowIfCancellationRequested();
				foreach (TypeDefinition type in TreeTraversal.PreOrder(asm.AssemblyDefinition.MainModule.Types, t => t.NestedTypes)) {
					ct.ThrowIfCancellationRequested();
					foreach (MethodDefinition method in type.Methods) {
						ct.ThrowIfCancellationRequested();
						bool found = false;
						if (!method.HasBody)
							continue;
						foreach (Instruction instr in method.Body.Instructions) {
							if (instr.Operand is MethodReference
							    && ((MethodReference)instr.Operand).Resolve() == analyzedMethod) {
								found = true;
								break;
							}
						}
						if (found)
							yield return new MethodTreeNode(method);
					}
				}
			}
		}
		
		public override void Decompile(Language language, ICSharpCode.Decompiler.ITextOutput output, DecompilationOptions options)
		{
			throw new NotImplementedException();
		}
	}
}
