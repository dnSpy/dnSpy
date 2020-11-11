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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class InterfacePropertyImplementedByNode : SearchNode {
		readonly PropertyDef analyzedProperty;
		readonly MethodDef? analyzedMethod;
		readonly bool isGetter;

		public InterfacePropertyImplementedByNode(PropertyDef analyzedProperty) {
			this.analyzedProperty = analyzedProperty ?? throw new ArgumentNullException(nameof(analyzedProperty));
			if (this.analyzedProperty.GetMethod is not null) {
				analyzedMethod = this.analyzedProperty.GetMethod;
				isGetter = true;
			}
			else {
				analyzedMethod = this.analyzedProperty.SetMethod;
				isGetter = false;
			}
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.ImplementedByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			if (analyzedMethod is null)
				return new List<AnalyzerTreeNodeData>();
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedMethod, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			if (analyzedMethod is null)
				yield break;
			if (type.IsInterface)
				yield break;
			var implementedInterfaceRef = InterfaceMethodImplementedByNode.GetInterface(type, analyzedMethod.DeclaringType);
			if (implementedInterfaceRef is null)
				yield break;

			foreach (PropertyDef property in type.Properties.Where(e => e.Name.EndsWith(analyzedProperty.Name))) {
				MethodDef accessor = isGetter ? property.GetMethod : property.SetMethod;
				// Don't include abstract accessors, they don't implement anything
				if (accessor is null || !accessor.IsVirtual || accessor.IsAbstract)
					continue;
				if (accessor.HasOverrides && accessor.Overrides.Any(m => CheckEquals(m.MethodDeclaration.ResolveMethodDef(), analyzedMethod))) {
					yield return new PropertyNode(property) { Context = Context };
					yield break;
				}
			}

			foreach (PropertyDef property in type.Properties.Where(e => e.Name == analyzedProperty.Name)) {
				MethodDef accessor = isGetter ? property.GetMethod : property.SetMethod;
				// Don't include abstract accessors, they don't implement anything
				if (accessor is null || !accessor.IsVirtual || accessor.IsAbstract)
					continue;
				if (TypesHierarchyHelpers.MatchInterfaceMethod(accessor, analyzedMethod, implementedInterfaceRef)) {
					yield return new PropertyNode(property) { Context = Context };
					yield break;
				}
			}
		}

		public static bool CanShow(PropertyDef property) => property.DeclaringType.IsInterface && (property.GetMethod ?? property.SetMethod) is MethodDef accessor && (accessor.IsVirtual || accessor.IsAbstract);
	}
}
