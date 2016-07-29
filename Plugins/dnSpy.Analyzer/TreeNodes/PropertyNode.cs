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
using dnlib.DotNet;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class PropertyNode : EntityNode {
		readonly PropertyDef analyzedProperty;
		readonly bool isIndexer;
		readonly bool hidesParent;

		public PropertyNode(PropertyDef analyzedProperty, bool hidesParent = false) {
			if (analyzedProperty == null)
				throw new ArgumentNullException(nameof(analyzedProperty));
			this.isIndexer = analyzedProperty.IsIndexer();
			this.analyzedProperty = analyzedProperty;
			this.hidesParent = hidesParent;
		}

		public override void Initialize() => this.TreeNode.LazyLoading = true;
		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) => dnImgMgr.GetImageReference(analyzedProperty);

		protected override void Write(ITextColorWriter output, ILanguage language) {
			if (hidesParent) {
				output.Write(BoxedTextColor.Punctuation, "(");
				output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.HidesParent);
				output.Write(BoxedTextColor.Punctuation, ")");
				output.WriteSpace();
			}
			language.WriteType(output, analyzedProperty.DeclaringType, true);
			output.Write(BoxedTextColor.Operator, ".");
			new NodePrinter().Write(output, language, analyzedProperty, Context.ShowToken, null);
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			if (analyzedProperty.GetMethod != null)
				yield return new PropertyAccessorNode(analyzedProperty.GetMethod, dnSpy_Analyzer_Resources.PropertyGetterTreeNode);
			if (analyzedProperty.SetMethod != null)
				yield return new PropertyAccessorNode(analyzedProperty.SetMethod, dnSpy_Analyzer_Resources.PropertySetterTreeNode);
			foreach (var accessor in analyzedProperty.OtherMethods)
				yield return new PropertyAccessorNode(accessor, null);

			if (PropertyOverridesNode.CanShow(analyzedProperty))
				yield return new PropertyOverridesNode(analyzedProperty);
			if (InterfacePropertyImplementedByNode.CanShow(analyzedProperty))
				yield return new InterfacePropertyImplementedByNode(analyzedProperty);
		}

		public static IAnalyzerTreeNodeData TryCreateAnalyzer(IMemberRef member, ILanguage language) {
			if (CanShow(member, language))
				return new PropertyNode(member as PropertyDef);
			else
				return null;
		}

		public static bool CanShow(IMemberRef member, ILanguage language) {
			var property = member as PropertyDef;
			if (property == null)
				return false;

			return !language.ShowMember(property.GetMethod ?? property.SetMethod)
				|| PropertyOverridesNode.CanShow(property);
		}

		public override IMemberRef Member => analyzedProperty;
		public override IMDTokenProvider Reference => analyzedProperty;
	}
}
