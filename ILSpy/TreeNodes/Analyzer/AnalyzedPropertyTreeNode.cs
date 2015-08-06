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
using dnlib.DotNet;
using dnSpy.Images;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer {
	internal sealed class AnalyzedPropertyTreeNode : AnalyzerEntityTreeNode
	{
		private readonly PropertyDef analyzedProperty;
		private readonly bool isIndexer;
		private readonly bool hidesParent;

		public AnalyzedPropertyTreeNode(PropertyDef analyzedProperty, bool hidesParent = false)
		{
			if (analyzedProperty == null)
				throw new ArgumentNullException("analyzedProperty");
			this.isIndexer = analyzedProperty.IsIndexer();
			this.analyzedProperty = analyzedProperty;
			this.hidesParent = hidesParent;
			this.LazyLoading = true;
		}

		public override object Icon
		{
			get { return PropertyTreeNode.GetIcon(analyzedProperty, BackgroundType.TreeNode); }
		}

		protected override void Write(ITextOutput output, Language language)
		{
			if (hidesParent) {
				output.Write('(', TextTokenType.Operator);
				output.Write("hides", TextTokenType.Text);
				output.Write(')', TextTokenType.Operator);
				output.WriteSpace();
			}
			Language.TypeToString(output, analyzedProperty.DeclaringType, true);
			output.Write('.', TextTokenType.Operator);
			PropertyTreeNode.Write(output, analyzedProperty, Language, isIndexer);
		}

		protected override void LoadChildren()
		{
			if (analyzedProperty.GetMethod != null)
				this.Children.Add(new AnalyzedPropertyAccessorTreeNode(analyzedProperty.GetMethod, "get"));
			if (analyzedProperty.SetMethod != null)
				this.Children.Add(new AnalyzedPropertyAccessorTreeNode(analyzedProperty.SetMethod, "set"));
			foreach (var accessor in analyzedProperty.OtherMethods)
				this.Children.Add(new AnalyzedPropertyAccessorTreeNode(accessor, null));

			if (AnalyzedPropertyOverridesTreeNode.CanShow(analyzedProperty))
				this.Children.Add(new AnalyzedPropertyOverridesTreeNode(analyzedProperty));
			if (AnalyzedInterfacePropertyImplementedByTreeNode.CanShow(analyzedProperty))
				this.Children.Add(new AnalyzedInterfacePropertyImplementedByTreeNode(analyzedProperty));
		}

		public static AnalyzerTreeNode TryCreateAnalyzer(IMemberRef member)
		{
			if (CanShow(member))
				return new AnalyzedPropertyTreeNode(member as PropertyDef);
			else
				return null;
		}

		public static bool CanShow(IMemberRef member)
		{
			var property = member as PropertyDef;
			if (property == null)
				return false;

			return !MainWindow.Instance.CurrentLanguage.ShowMember(property.GetMethod ?? property.SetMethod)
			    || AnalyzedPropertyOverridesTreeNode.CanShow(property);
		}
		
		public override IMemberRef Member {
			get { return analyzedProperty; }
		}

		public override IMDTokenProvider MDTokenProvider {
			get { return analyzedProperty; }
		}
	}
}
