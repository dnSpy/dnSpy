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
	class AnalyzedTypeExtensionMethodsTreeNode : AnalyzerTreeNode
	{
		TypeDefinition analyzedType;
		ThreadingSupport threading;
		bool IsSystemObject;

		public AnalyzedTypeExtensionMethodsTreeNode(TypeDefinition analyzedType)
		{
			if (analyzedType == null)
				throw new ArgumentNullException("analyzedType");

			this.analyzedType = analyzedType;
			this.threading = new ThreadingSupport();
			this.LazyLoading = true;

			this.IsSystemObject = (analyzedType.FullName == "System.Object");
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

		IEnumerable<SharpTreeNode> FetchChildren(CancellationToken ct)
		{
			ScopedWhereUsedScopeAnalyzer<SharpTreeNode> analyzer;

			analyzer = new ScopedWhereUsedScopeAnalyzer<SharpTreeNode>(analyzedType, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<SharpTreeNode> FindReferencesInType(TypeDefinition type)
		{
			foreach (MethodDefinition method in type.Methods) {
				if (method.IsStatic && method.HasCustomAttributes) {
					if (method.CustomAttributes.Any(ca => ca.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute")) {
						if (method.HasParameters && method.Parameters[0].ParameterType.Resolve() == analyzedType) {
							yield return new AnalyzedMethodTreeNode(method);
						}
					}
				}
			}
		}

		private bool TypeIsExposedBy(MethodDefinition method)
		{
			if (method.IsPrivate)
				return false;

			// exclude methods with 'semantics'. for example, property getters & setters.
			if (method.SemanticsAttributes != MethodSemanticsAttributes.None)
				return false;

			if (method.ReturnType.Resolve() == analyzedType)
				return true;

			if (method.HasParameters) {
				foreach (var parameter in method.Parameters) {
					if (parameter.ParameterType.Resolve() == analyzedType)
						return true;
				}
			}

			return false;
		}

		public static bool CanShowAnalyzer(TypeDefinition type)
		{
			return !(type.IsEnum);
		}

	}
}
