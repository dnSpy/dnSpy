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
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class TypeInstantiationsNode : SearchNode {
		readonly TypeDef analyzedType;
		readonly bool isSystemObject;

		public TypeInstantiationsNode(TypeDef analyzedType) {
			if (analyzedType == null)
				throw new ArgumentNullException(nameof(analyzedType));

			this.analyzedType = analyzedType;
			this.isSystemObject = analyzedType.DefinitionAssembly.IsCorLib() && analyzedType.FullName == "System.Object";
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.InstantiatedByTreeNode);

		protected override IEnumerable<IAnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			var analyzer = new ScopedWhereUsedAnalyzer<IAnalyzerTreeNodeData>(Context.DocumentService, analyzedType, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<IAnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			foreach (MethodDef method in type.Methods) {
				if (!method.HasBody)
					continue;

				// ignore chained constructors
				// (since object is the root of everything, we can short circuit the test in this case)
				if (method.Name == ".ctor" &&
					(isSystemObject || analyzedType == type || TypesHierarchyHelpers.IsBaseType(analyzedType, type, false)))
					continue;

				Instruction foundInstr = null;
				foreach (Instruction instr in method.Body.Instructions) {
					IMethod mr = instr.Operand as IMethod;
					if (mr != null && !mr.IsField && mr.Name == ".ctor") {
						if (Helpers.IsReferencedBy(analyzedType, mr.DeclaringType)) {
							foundInstr = instr;
							break;
						}
					}
				}

				if (foundInstr != null)
					yield return new MethodNode(method) { Context = Context, SourceRef = new SourceRef(method, foundInstr.Offset, foundInstr.Operand as IMDTokenProvider) };
			}
		}

		public static bool CanShow(TypeDef type) => (type.IsClass && !(type.IsAbstract && type.IsSealed) && !type.IsEnum);
	}
}
