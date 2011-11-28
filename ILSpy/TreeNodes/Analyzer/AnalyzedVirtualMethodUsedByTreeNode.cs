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
using Mono.Cecil;
using Mono.Cecil.Cil;
using ICSharpCode.Decompiler.Ast;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedVirtualMethodUsedByTreeNode : AnalyzerSearchTreeNode
	{
		private readonly MethodDefinition analyzedMethod;
		private ConcurrentDictionary<MethodDefinition, int> foundMethods;
		private MethodDefinition baseMethod;
		private List<TypeReference> possibleTypes;

		public AnalyzedVirtualMethodUsedByTreeNode(MethodDefinition analyzedMethod)
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
			InitializeAnalyzer();

			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNode>(analyzedMethod, FindReferencesInType);
			foreach (var child in analyzer.PerformAnalysis(ct).OrderBy(n => n.Text)) {
				yield return child;
			}

			ReleaseAnalyzer();
		}

		private void InitializeAnalyzer()
		{
			foundMethods = new ConcurrentDictionary<MethodDefinition, int>();

			var baseMethods = TypesHierarchyHelpers.FindBaseMethods(analyzedMethod).ToArray();
			if (baseMethods.Length > 0) {
				baseMethod = baseMethods[baseMethods.Length - 1];
			} else
				baseMethod = analyzedMethod;

			possibleTypes = new List<TypeReference>();

			TypeReference type = analyzedMethod.DeclaringType.BaseType;
			while (type != null) {
				possibleTypes.Add(type);
				type = type.Resolve().BaseType;
			}
		}

		private void ReleaseAnalyzer()
		{
			foundMethods = null;
			baseMethod = null;
		}

		private IEnumerable<AnalyzerTreeNode> FindReferencesInType(TypeDefinition type)
		{
			string name = analyzedMethod.Name;
			foreach (MethodDefinition method in type.Methods) {
				bool found = false;
				string prefix = string.Empty;
				if (!method.HasBody)
					continue;
				foreach (Instruction instr in method.Body.Instructions) {
					MethodReference mr = instr.Operand as MethodReference;
					if (mr != null && mr.Name == name) {
						// explicit call to the requested method 
						if (instr.OpCode.Code == Code.Call
							&& Helpers.IsReferencedBy(analyzedMethod.DeclaringType, mr.DeclaringType)
							&& mr.Resolve() == analyzedMethod) {
							found = true;
							prefix = "(as base) ";
							break;
						}
						// virtual call to base method
						if (instr.OpCode.Code == Code.Callvirt) {
							MethodDefinition md = mr.Resolve();
							if (md == null) {
								// cannot resolve the operand, so ignore this method
								break;
							}
							if (md == baseMethod) {
								found = true;
								break;
							}
						}
					}
				}

				method.Body = null;

				if (found) {
					MethodDefinition codeLocation = this.Language.GetOriginalCodeLocation(method) as MethodDefinition;
					if (codeLocation != null && !HasAlreadyBeenFound(codeLocation)) {
						var node = new AnalyzedMethodTreeNode(codeLocation);
						node.Language = this.Language;
						yield return node;
					}
				}
			}
		}

		private bool HasAlreadyBeenFound(MethodDefinition method)
		{
			return !foundMethods.TryAdd(method, 0);
		}
	}
}
