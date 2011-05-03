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
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.TreeView;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedTypeInstantiationsTreeNode : AnalyzerTreeNode
	{
		private readonly TypeDefinition analyzedType;
		private readonly ThreadingSupport threading;
		private readonly bool isSystemObject;

		public AnalyzedTypeInstantiationsTreeNode(TypeDefinition analyzedType)
		{
			if (analyzedType == null)
				throw new ArgumentNullException("analyzedType");

			this.analyzedType = analyzedType;
			this.threading = new ThreadingSupport();
			this.LazyLoading = true;

			this.isSystemObject = (analyzedType.FullName == "System.Object");
		}

		public override object Text
		{
			get { return "Instantiated By"; }
		}

		public override object Icon
		{
			get { return Images.Search; }
		}

		protected override void LoadChildren()
		{
			threading.LoadChildren(this, FetchChildren);
		}

		protected override void OnCollapsing()
		{
			if (threading.IsRunning) {
				this.LazyLoading = true;
				threading.Cancel();
				this.Children.Clear();
			}
		}

		private IEnumerable<SharpTreeNode> FetchChildren(CancellationToken ct)
		{
			ScopedWhereUsedScopeAnalyzer<SharpTreeNode> analyzer;

			analyzer = new ScopedWhereUsedScopeAnalyzer<SharpTreeNode>(analyzedType, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		private IEnumerable<SharpTreeNode> FindReferencesInType(TypeDefinition type)
		{
			foreach (MethodDefinition method in type.Methods) {
				bool found = false;
				if (!method.HasBody)
					continue;

				// ignore chained constructors
				// (since object is the root of everything, we can short circuit the test in this case)
				if (method.Name == ".ctor" &&
					(isSystemObject || analyzedType == type || TypesHierarchyHelpers.IsBaseType(analyzedType, type, false)))
					continue;

				foreach (Instruction instr in method.Body.Instructions) {
					MethodReference mr = instr.Operand as MethodReference;
					if (mr != null && mr.Name == ".ctor") {
						if (Helpers.IsReferencedBy(analyzedType, mr.DeclaringType)) {
							found = true;
							break;
						}
					}
				}

				method.Body = null;

				if (found)
					yield return new AnalyzedMethodTreeNode(method);
			}
		}

		public static bool CanShow(TypeDefinition type)
		{
			if (type.IsClass && !type.IsEnum) {
				return type.Methods.Where(m => m.Name == ".ctor").Any(m => !m.IsPrivate);
			}
			return false;
		}
	}
}
