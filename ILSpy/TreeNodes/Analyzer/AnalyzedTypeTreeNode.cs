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
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal class AnalyzedTypeTreeNode : AnalyzerEntityTreeNode
	{
		private readonly TypeDefinition analyzedType;

		public AnalyzedTypeTreeNode(TypeDefinition analyzedType)
		{
			if (analyzedType == null)
				throw new ArgumentNullException("analyzedType");
			this.analyzedType = analyzedType;
			this.LazyLoading = true;
		}

		public override object Icon
		{
			get { return TypeTreeNode.GetIcon(analyzedType); }
		}

		public override object Text
		{
			get
			{
				return Language.TypeToString(analyzedType, true);
			}
		}

		protected override void LoadChildren()
		{
			if (AnalyzedAttributeAppliedToTreeNode.CanShow(analyzedType))
				this.Children.Add(new AnalyzedAttributeAppliedToTreeNode(analyzedType));

			if (AnalyzedTypeInstantiationsTreeNode.CanShow(analyzedType))
				this.Children.Add(new AnalyzedTypeInstantiationsTreeNode(analyzedType));

			if (AnalyzedTypeUsedByTreeNode.CanShow(analyzedType))
				this.Children.Add(new AnalyzedTypeUsedByTreeNode(analyzedType));

			if (AnalyzedTypeExposedByTreeNode.CanShow(analyzedType))
				this.Children.Add(new AnalyzedTypeExposedByTreeNode(analyzedType));

			if (AnalyzedTypeExtensionMethodsTreeNode.CanShow(analyzedType))
				this.Children.Add(new AnalyzedTypeExtensionMethodsTreeNode(analyzedType));
		}

		public override MemberReference Member {
			get { return analyzedType; }
		}
	}
}
