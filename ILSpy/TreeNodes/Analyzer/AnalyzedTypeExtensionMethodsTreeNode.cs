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
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal class AnalyzedTypeExtensionMethodsTreeNode : AnalyzerTreeNode
	{
		private readonly TypeDefinition analyzedType;
		private readonly ThreadingSupport threading;

		public AnalyzedTypeExtensionMethodsTreeNode(TypeDefinition analyzedType)
		{
			if (analyzedType == null)
				throw new ArgumentNullException("analyzedType");

			this.analyzedType = analyzedType;
			this.threading = new ThreadingSupport();
			this.LazyLoading = true;
		}

		public override object Text
		{
			get { return "Extension Methods"; }
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

			analyzer = new ScopedWhereUsedAnalyzer<SharpTreeNode>(analyzedType, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		private IEnumerable<SharpTreeNode> FindReferencesInType(TypeDefinition type)
		{
			if (!HasExtensionAttribute(type))
				yield break;
			foreach (MethodDefinition method in type.Methods) {
				if (method.IsStatic && HasExtensionAttribute(method)) {
					if (method.HasParameters && method.Parameters[0].ParameterType.Resolve() == analyzedType) {
						yield return new AnalyzedMethodTreeNode(method);
					}
				}
			}
		}
		
		bool HasExtensionAttribute(ICustomAttributeProvider p)
		{
			if (p.HasCustomAttributes) {
				foreach (CustomAttribute ca in p.CustomAttributes) {
					TypeReference t = ca.AttributeType;
					if (t.Name == "ExtensionAttribute" && t.Namespace == "System.Runtime.CompilerServices")
						return true;
				}
			}
			return false;
		}
		

		public static bool CanShow(TypeDefinition type)
		{
			// show on all types except static classes
			return !(type.IsAbstract && type.IsSealed);
		}
	}
}
