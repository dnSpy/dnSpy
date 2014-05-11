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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedMethodUsedByTreeNode : AnalyzerSearchTreeNode
	{
		private readonly MethodDef analyzedMethod;
		private ConcurrentDictionary<MethodDef, int> foundMethods;

		public AnalyzedMethodUsedByTreeNode(MethodDef analyzedMethod)
		{
			if (analyzedMethod == null)
				throw new ArgumentNullException("analyzedMethod");

			this.analyzedMethod = analyzedMethod;
		}

		public override object Text
		{
			get { return "Used By"; }
		}

		protected override IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct)
		{
			foundMethods = new ConcurrentDictionary<MethodDef, int>();

			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNode>(analyzedMethod, FindReferencesInType);
			foreach (var child in analyzer.PerformAnalysis(ct).OrderBy(n => n.Text)) {
				yield return child;
			}

			foundMethods = null;
		}

		private IEnumerable<AnalyzerTreeNode> FindReferencesInType(TypeDef type)
		{
			string name = analyzedMethod.Name;
			foreach (MethodDef method in type.Methods) {
				bool found = false;
				if (!method.HasBody)
					continue;
				foreach (Instruction instr in method.Body.Instructions) {
					IMethod mr = instr.Operand as IMethod;
					if (mr != null && !mr.IsField && mr.Name == name &&
						Helpers.IsReferencedBy(analyzedMethod.DeclaringType, mr.DeclaringType) &&
						Decompiler.DnlibExtensions.Resolve(mr) == analyzedMethod) {
						found = true;
						break;
					}
				}

				method.Body = null;

				if (found) {
					MethodDef codeLocation = this.Language.GetOriginalCodeLocation(method) as MethodDef;
					if (codeLocation != null && !HasAlreadyBeenFound(codeLocation)) {
						var node= new AnalyzedMethodTreeNode(codeLocation);
						node.Language = this.Language;
						yield return node;
					}
				}
			}
		}

		private bool HasAlreadyBeenFound(MethodDef method)
		{
			return !foundMethods.TryAdd(method, 0);
		}
	}
}
