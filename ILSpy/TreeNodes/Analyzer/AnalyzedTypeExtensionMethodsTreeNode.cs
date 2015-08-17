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
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer {
	internal class AnalyzedTypeExtensionMethodsTreeNode : AnalyzerSearchTreeNode
	{
		private readonly TypeDef analyzedType;

		public AnalyzedTypeExtensionMethodsTreeNode(TypeDef analyzedType)
		{
			if (analyzedType == null)
				throw new ArgumentNullException("analyzedType");

			this.analyzedType = analyzedType;
		}

		protected override void Write(ITextOutput output, Language language)
		{
			output.Write("Extension Methods", TextTokenType.Text);
		}

		protected override IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct)
		{
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNode>(analyzedType, FindReferencesInType);
			return analyzer.PerformAnalysis(ct).OrderBy(n => n.ToString(Language));
		}

		private IEnumerable<AnalyzerTreeNode> FindReferencesInType(TypeDef type)
		{
			if (!HasExtensionAttribute(type))
				yield break;
			foreach (MethodDef method in type.Methods) {
				if (method.IsStatic && HasExtensionAttribute(method)) {
					int skip = Decompiler.DnlibExtensions.GetParametersSkip(method.Parameters);
					if (method.Parameters.Count > skip && new SigComparer().Equals(analyzedType, method.Parameters[skip].Type)) {
						var node = new AnalyzedMethodTreeNode(method);
						node.Language = this.Language;
						yield return node;
					}
				}
			}
		}
		
		bool HasExtensionAttribute(IHasCustomAttribute p)
		{
			return p.CustomAttributes.Find("System.Runtime.CompilerServices.ExtensionAttribute") != null;
		}
		

		public static bool CanShow(TypeDef type)
		{
			// show on all types except static classes
			return !(type.IsAbstract && type.IsSealed);
		}
	}
}
