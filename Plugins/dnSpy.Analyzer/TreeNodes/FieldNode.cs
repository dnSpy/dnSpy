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
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Files.TreeView;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class FieldNode : EntityNode {
		readonly FieldDef analyzedField;

		public FieldNode(FieldDef analyzedField) {
			if (analyzedField == null)
				throw new ArgumentNullException("analyzedField");
			this.analyzedField = analyzedField;
		}

		public override void Initialize() {
			this.TreeNode.LazyLoading = true;
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReference(analyzedField);
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			language.WriteType(output, analyzedField.DeclaringType, true);
			output.Write(".", BoxedTextTokenKind.Operator);
			new NodePrinter().Write(output, language, analyzedField, Context.ShowToken);
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			yield return new FieldAccessNode(analyzedField, false);
			if (!analyzedField.IsLiteral)
				yield return new FieldAccessNode(analyzedField, true);
		}

		public override IMemberRef Member {
			get { return analyzedField; }
		}

		public override IMDTokenProvider Reference {
			get { return analyzedField; }
		}
	}
}
