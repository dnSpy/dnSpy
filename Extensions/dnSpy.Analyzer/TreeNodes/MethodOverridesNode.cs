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
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	/// <summary>
	/// Searches for overrides of the analyzed method.
	/// </summary>
	sealed class MethodOverridesNode : SearchNode {
		readonly MethodDef analyzedMethod;

		public MethodOverridesNode(MethodDef analyzedMethod) {
			if (analyzedMethod == null)
				throw new ArgumentNullException(nameof(analyzedMethod));

			this.analyzedMethod = analyzedMethod;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.OverriddenByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedMethod, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			AnalyzerTreeNodeData newNode = null;
			try {
				if (!TypesHierarchyHelpers.IsBaseType(analyzedMethod.DeclaringType, type, resolveTypeArguments: false))
					yield break;

				foreach (MethodDef method in type.Methods) {
					if (TypesHierarchyHelpers.IsBaseMethod(analyzedMethod, method)) {
						bool hidesParent = !method.IsVirtual ^ method.IsNewSlot;
						newNode = new MethodNode(method, hidesParent) { Context = Context };
					}
				}
			}
			catch (ResolveException) {
				// ignore this type definition. maybe add a notification about such cases.
			}

			if (newNode != null)
				yield return newNode;
		}

		public static bool CanShow(MethodDef method) =>
			method.IsVirtual &&
			!method.IsFinal &&
			!method.DeclaringType.IsSealed &&
			!method.DeclaringType.IsInterface;  // interface methods are definitions not implementations - cannot be overridden
	}
}
