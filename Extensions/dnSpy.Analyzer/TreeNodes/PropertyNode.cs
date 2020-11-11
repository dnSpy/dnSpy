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
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class PropertyNode : EntityNode {
		readonly PropertyDef analyzedProperty;
		readonly bool isIndexer;
		readonly bool hidesParent;

		public PropertyNode(PropertyDef analyzedProperty, bool hidesParent = false) {
			if (analyzedProperty is null)
				throw new ArgumentNullException(nameof(analyzedProperty));
			isIndexer = analyzedProperty.IsIndexer();
			this.analyzedProperty = analyzedProperty;
			this.hidesParent = hidesParent;
		}

		public override void Initialize() => TreeNode.LazyLoading = true;
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => dnImgMgr.GetImageReference(analyzedProperty);

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) {
			if (hidesParent) {
				output.Write(BoxedTextColor.Punctuation, "(");
				output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.HidesParent);
				output.Write(BoxedTextColor.Punctuation, ")");
				output.WriteSpace();
			}
			decompiler.WriteType(output, analyzedProperty.DeclaringType, true);
			output.Write(BoxedTextColor.Operator, ".");
			new NodeFormatter().Write(output, decompiler, analyzedProperty, Context.ShowToken, null);
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			if (analyzedProperty.GetMethod is not null)
				yield return new PropertyAccessorNode(analyzedProperty.GetMethod, "get", isSetter: false);

			if (analyzedProperty.SetMethod is not null)
				yield return new PropertyAccessorNode(analyzedProperty.SetMethod, "set", isSetter: true);

			foreach (var accessor in analyzedProperty.OtherMethods)
				yield return new PropertyAccessorNode(accessor, null, isSetter: false);

			if (PropertyOverriddenNode.CanShow(analyzedProperty))
				yield return new PropertyOverriddenNode(analyzedProperty);

			if (PropertyOverridesNode.CanShow(analyzedProperty))
				yield return new PropertyOverridesNode(analyzedProperty);

			if (InterfacePropertyImplementedByNode.CanShow(analyzedProperty))
				yield return new InterfacePropertyImplementedByNode(analyzedProperty);
		}

		public static AnalyzerTreeNodeData? TryCreateAnalyzer(IMemberRef? member, IDecompiler decompiler) {
			if (CanShow(member, decompiler))
				return new PropertyNode((PropertyDef)member!);
			else
				return null;
		}

		public static bool CanShow(IMemberRef? member, IDecompiler decompiler) => member is PropertyDef;
		public override IMemberRef? Member => analyzedProperty;
		public override IMDTokenProvider? Reference => analyzedProperty;
	}
}
