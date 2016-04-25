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
using dnlib.DotNet;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class InterfaceMethodImplementedByNode : SearchNode {
		readonly MethodDef analyzedMethod;

		public InterfaceMethodImplementedByNode(MethodDef analyzedMethod) {
			if (analyzedMethod == null)
				throw new ArgumentNullException("analyzedMethod");

			this.analyzedMethod = analyzedMethod;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			output.Write(dnSpy_Analyzer_Resources.ImplementedByTreeNode, TextTokenKind.Text);
		}

		protected override IEnumerable<IAnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			var analyzer = new ScopedWhereUsedAnalyzer<IAnalyzerTreeNodeData>(Context.FileManager, analyzedMethod, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<IAnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			if (!type.HasInterfaces)
				yield break;
			var iff = type.Interfaces.FirstOrDefault(i => new SigComparer().Equals(i.Interface, analyzedMethod.DeclaringType));
			ITypeDefOrRef implementedInterfaceRef = iff == null ? null : iff.Interface;
			if (implementedInterfaceRef == null)
				yield break;

			//TODO: Can we compare method sigs too?
			foreach (MethodDef method in type.Methods.Where(m => m.Name == analyzedMethod.Name)) {
				if (TypesHierarchyHelpers.MatchInterfaceMethod(method, analyzedMethod, implementedInterfaceRef)) {
					yield return new MethodNode(method) { Context = Context };
				}
			}

			foreach (MethodDef method in type.Methods) {
				if (method.HasOverrides && method.Overrides.Any(m => m.MethodDeclaration.ResolveMethodDef() == analyzedMethod)) {
					yield return new MethodNode(method) { Context = Context };
				}
			}
		}

		public static bool CanShow(MethodDef method) {
			return method.DeclaringType.IsInterface;
		}
	}
}
