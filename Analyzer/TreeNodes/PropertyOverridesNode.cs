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
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Languages;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler.Ast;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class PropertyOverridesNode : SearchNode {
		readonly PropertyDef analyzedProperty;

		public PropertyOverridesNode(PropertyDef analyzedProperty) {
			if (analyzedProperty == null)
				throw new ArgumentNullException("analyzedProperty");

			this.analyzedProperty = analyzedProperty;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			output.Write("Overridden By", TextTokenType.Text);
		}

		protected override IEnumerable<IAnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			var analyzer = new ScopedWhereUsedAnalyzer<IAnalyzerTreeNodeData>(Context.FileManager, analyzedProperty, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<IAnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			if (!TypesHierarchyHelpers.IsBaseType(analyzedProperty.DeclaringType, type, resolveTypeArguments: false))
				yield break;

			foreach (PropertyDef property in type.Properties) {

				if (TypesHierarchyHelpers.IsBaseProperty(analyzedProperty, property)) {
					MethodDef anyAccessor = property.GetMethod ?? property.SetMethod;
					if (anyAccessor == null)
						continue;
					bool hidesParent = !anyAccessor.IsVirtual ^ anyAccessor.IsNewSlot;
					yield return new PropertyNode(property, hidesParent) { Context = Context };
				}
			}
		}

		public static bool CanShow(PropertyDef property) {
			var accessor = property.GetMethod ?? property.SetMethod;
			return accessor != null && accessor.IsVirtual && !accessor.IsFinal && !accessor.DeclaringType.IsInterface;
		}
	}
}
