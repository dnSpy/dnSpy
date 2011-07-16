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
	internal class AnalyzedFieldTreeNode : AnalyzerEntityTreeNode
	{
		private readonly FieldDefinition analyzedField;

		public AnalyzedFieldTreeNode(FieldDefinition analyzedField)
		{
			if (analyzedField == null)
				throw new ArgumentNullException("analyzedField");
			this.analyzedField = analyzedField;
			this.LazyLoading = true;
		}

		public override object Icon
		{
			get { return FieldTreeNode.GetIcon(analyzedField); }
		}

		public override object Text
		{
			get
			{
				return Language.TypeToString(analyzedField.DeclaringType, true) +
					"." + analyzedField.Name + " : " + this.Language.TypeToString(analyzedField.FieldType, false, analyzedField);
			}
		}

		protected override void LoadChildren()
		{
			this.Children.Add(new AnalyzedFieldAccessTreeNode(analyzedField, false));
			if (!analyzedField.IsLiteral)
				this.Children.Add(new AnalyzedFieldAccessTreeNode(analyzedField, true));
		}

		public override MemberReference Member {
			get { return analyzedField; }
		}
	}
}
