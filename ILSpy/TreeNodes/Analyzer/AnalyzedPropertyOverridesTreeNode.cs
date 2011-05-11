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
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedPropertyOverridesTreeNode : AnalyzerTreeNode
	{
		private readonly PropertyDefinition analyzedProperty;
		private readonly ThreadingSupport threading;

		public AnalyzedPropertyOverridesTreeNode(PropertyDefinition analyzedProperty)
		{
			if (analyzedProperty == null)
				throw new ArgumentNullException("analyzedProperty");

			this.analyzedProperty = analyzedProperty;
			this.threading = new ThreadingSupport();
			this.LazyLoading = true;
		}

		public override object Text
		{
			get { return "Overridden By"; }
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
			ScopedWhereUsedAnalyzer<SharpTreeNode> analyzer;

			analyzer = new ScopedWhereUsedAnalyzer<SharpTreeNode>(analyzedProperty, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		private IEnumerable<SharpTreeNode> FindReferencesInType(TypeDefinition type)
		{
			string name = analyzedProperty.Name;
			string declTypeName = analyzedProperty.DeclaringType.FullName;

			if (!TypesHierarchyHelpers.IsBaseType(analyzedProperty.DeclaringType, type, resolveTypeArguments: false))
				yield break;

			foreach (PropertyDefinition property in type.Properties) {

				if (TypesHierarchyHelpers.IsBaseProperty(analyzedProperty, property)) {
					MethodDefinition anyAccessor = property.GetMethod ?? property.SetMethod;
					bool hidesParent = !anyAccessor.IsVirtual ^ anyAccessor.IsNewSlot;
					yield return new AnalyzedPropertyTreeNode(property, hidesParent ? "(hides) " : "");
				}
			}
		}

		public static bool CanShow(PropertyDefinition property)
		{
			var accessor = property.GetMethod ?? property.SetMethod;
			return accessor.IsVirtual && !accessor.IsFinal && !accessor.DeclaringType.IsInterface;
		}
	}
}
