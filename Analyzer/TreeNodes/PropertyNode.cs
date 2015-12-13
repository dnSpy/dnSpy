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
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Files.TreeView;
using dnSpy.Shared.UI.Highlighting;
using ICSharpCode.Decompiler;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class PropertyNode : EntityNode {
		readonly PropertyDef analyzedProperty;
		readonly bool isIndexer;
		readonly bool hidesParent;

		public PropertyNode(PropertyDef analyzedProperty, bool hidesParent = false) {
			if (analyzedProperty == null)
				throw new ArgumentNullException("analyzedProperty");
			this.isIndexer = analyzedProperty.IsIndexer();
			this.analyzedProperty = analyzedProperty;
			this.hidesParent = hidesParent;
		}

		public override void Initialize() {
			this.TreeNode.LazyLoading = true;
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReference(analyzedProperty);
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			if (hidesParent) {
				output.Write("(", TextTokenType.Operator);
				output.Write("hides", TextTokenType.Text);
				output.Write(")", TextTokenType.Operator);
				output.WriteSpace();
			}
			language.WriteType(output, analyzedProperty.DeclaringType, true);
			output.Write(".", TextTokenType.Operator);
			new NodePrinter().Write(output, language, analyzedProperty, Context.ShowToken, null);
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			if (analyzedProperty.GetMethod != null)
				yield return new PropertyAccessorNode(analyzedProperty.GetMethod, "get");
			if (analyzedProperty.SetMethod != null)
				yield return new PropertyAccessorNode(analyzedProperty.SetMethod, "set");
			foreach (var accessor in analyzedProperty.OtherMethods)
				yield return new PropertyAccessorNode(accessor, null);

			if (PropertyOverridesNode.CanShow(analyzedProperty))
				yield return new PropertyOverridesNode(analyzedProperty);
			if (InterfacePropertyImplementedByNode.CanShow(analyzedProperty))
				yield return new InterfacePropertyImplementedByNode(analyzedProperty);
		}

		public static IAnalyzerTreeNodeData TryCreateAnalyzer(IMemberRef member, ILanguage language, DecompilerSettings decompilerSettings) {
			if (CanShow(member, language, decompilerSettings))
				return new PropertyNode(member as PropertyDef);
			else
				return null;
		}

		public static bool CanShow(IMemberRef member, ILanguage language, DecompilerSettings decompilerSettings) {
			var property = member as PropertyDef;
			if (property == null)
				return false;

			return !language.ShowMember(property.GetMethod ?? property.SetMethod, decompilerSettings.Clone())
				|| PropertyOverridesNode.CanShow(property);
		}

		public override IMemberRef Member {
			get { return analyzedProperty; }
		}

		public override IMDTokenProvider Reference {
			get { return analyzedProperty; }
		}
	}
}
