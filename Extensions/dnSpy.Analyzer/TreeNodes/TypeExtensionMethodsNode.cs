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
	sealed class TypeExtensionMethodsNode : SearchNode {
		readonly TypeDef analyzedType;

		public TypeExtensionMethodsNode(TypeDef analyzedType) => this.analyzedType = analyzedType ?? throw new ArgumentNullException(nameof(analyzedType));

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.ExtensionMethodsTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedType, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			if (!HasExtensionAttribute(type))
				yield break;
			foreach (MethodDef method in type.Methods) {
				if (method.IsStatic && HasExtensionAttribute(method)) {
					int skip = GetParametersSkip(method.Parameters);
					if (method.Parameters.Count > skip && new SigComparer().Equals(analyzedType, method.Parameters[skip].Type)) {
						yield return new MethodNode(method) { Context = Context };
					}
				}
			}
		}

		static int GetParametersSkip(IList<Parameter> parameters) {
			if (parameters == null || parameters.Count == 0)
				return 0;
			if (parameters[0].IsHiddenThisParameter)
				return 1;
			return 0;
		}

		bool HasExtensionAttribute(IHasCustomAttribute p) => p.CustomAttributes.Find("System.Runtime.CompilerServices.ExtensionAttribute") != null;

		// show on all types except static classes
		public static bool CanShow(TypeDef type) => !(type.IsAbstract && type.IsSealed);
	}
}
