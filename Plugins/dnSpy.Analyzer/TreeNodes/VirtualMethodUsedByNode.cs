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
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class VirtualMethodUsedByNode : SearchNode {
		readonly MethodDef analyzedMethod;
		ConcurrentDictionary<MethodDef, int> foundMethods;
		MethodDef baseMethod;
		List<ITypeDefOrRef> possibleTypes;

		public VirtualMethodUsedByNode(MethodDef analyzedMethod) {
			if (analyzedMethod == null)
				throw new ArgumentNullException("analyzedMethod");

			this.analyzedMethod = analyzedMethod;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			output.Write(dnSpy_Analyzer_Resources.UsedByTreeNode, TextTokenKind.Text);
		}

		protected override IEnumerable<IAnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			InitializeAnalyzer();

			var analyzer = new ScopedWhereUsedAnalyzer<IAnalyzerTreeNodeData>(Context.FileManager, analyzedMethod, FindReferencesInType);
			foreach (var child in analyzer.PerformAnalysis(ct)) {
				yield return child;
			}

			ReleaseAnalyzer();
		}

		void InitializeAnalyzer() {
			foundMethods = new ConcurrentDictionary<MethodDef, int>();

			var baseMethods = TypesHierarchyHelpers.FindBaseMethods(analyzedMethod).ToArray();
			if (baseMethods.Length > 0) {
				baseMethod = baseMethods[baseMethods.Length - 1];
			}
			else
				baseMethod = analyzedMethod;

			possibleTypes = new List<ITypeDefOrRef>();

			ITypeDefOrRef type = analyzedMethod.DeclaringType.BaseType;
			while (type != null) {
				possibleTypes.Add(type);
				var resolvedType = type.ResolveTypeDef();
				type = resolvedType == null ? null : resolvedType.BaseType;
			}
		}

		void ReleaseAnalyzer() {
			foundMethods = null;
			baseMethod = null;
		}

		IEnumerable<IAnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			string name = analyzedMethod.Name;
			foreach (MethodDef method in type.Methods) {
				if (!method.HasBody)
					continue;
				Instruction foundInstr = null;
				foreach (Instruction instr in method.Body.Instructions) {
					IMethod mr = instr.Operand as IMethod;
					if (mr != null && !mr.IsField && mr.Name == name) {
						// explicit call to the requested method 
						if (instr.OpCode.Code == Code.Call
							&& Helpers.IsReferencedBy(analyzedMethod.DeclaringType, mr.DeclaringType)
							&& mr.ResolveMethodDef() == analyzedMethod) {
							foundInstr = instr;
							break;
						}
						// virtual call to base method
						if (instr.OpCode.Code == Code.Callvirt) {
							MethodDef md = mr.ResolveMethodDef();
							if (md == null) {
								// cannot resolve the operand, so ignore this method
								break;
							}
							if (md == baseMethod) {
								foundInstr = instr;
								break;
							}
						}
					}
				}

				if (foundInstr != null) {
					MethodDef codeLocation = GetOriginalCodeLocation(method) as MethodDef;
					if (codeLocation != null && !HasAlreadyBeenFound(codeLocation)) {
						var node = new MethodNode(codeLocation) { Context = Context };
						if (codeLocation == method)
							node.SourceRef = new SourceRef(method, foundInstr.Offset, foundInstr.Operand as IMDTokenProvider);
						yield return node;
					}
				}
			}
		}

		bool HasAlreadyBeenFound(MethodDef method) {
			return !foundMethods.TryAdd(method, 0);
		}
	}
}
