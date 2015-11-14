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
using dnSpy.Contracts.Images;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer {
	internal class AnalyzedMethodTreeNode : AnalyzerEntityTreeNode {
		private readonly MethodDef analyzedMethod;
		private readonly bool hidesParent;

		public AnalyzedMethodTreeNode(MethodDef analyzedMethod, bool hidesParent = false) {
			if (analyzedMethod == null)
				throw new ArgumentNullException("analyzedMethod");
			this.analyzedMethod = analyzedMethod;
			this.hidesParent = hidesParent;
			this.LazyLoading = true;
		}

		public override object Icon {
			get { return MethodTreeNode.GetIcon(analyzedMethod, BackgroundType.TreeNode); }
		}

		protected override void Write(ITextOutput output, Language language) {
			if (hidesParent) {
				output.Write("(", TextTokenType.Operator);
				output.Write("hides", TextTokenType.Text);
				output.Write(")", TextTokenType.Operator);
				output.WriteSpace();
			}
			Language.TypeToString(output, analyzedMethod.DeclaringType, true);
			output.Write(".", TextTokenType.Operator);
			MethodTreeNode.Write(output, analyzedMethod, Language);
		}

		protected override void LoadChildren() {
			if (analyzedMethod.HasBody)
				this.Children.Add(new AnalyzedMethodUsesTreeNode(analyzedMethod));

			if (analyzedMethod.IsVirtual && !(analyzedMethod.IsNewSlot && analyzedMethod.IsFinal))
				this.Children.Add(new AnalyzedVirtualMethodUsedByTreeNode(analyzedMethod));
			else
				this.Children.Add(new AnalyzedMethodUsedByTreeNode(analyzedMethod));

			if (AnalyzedMethodOverridesTreeNode.CanShow(analyzedMethod))
				this.Children.Add(new AnalyzedMethodOverridesTreeNode(analyzedMethod));

			if (AnalyzedInterfaceMethodImplementedByTreeNode.CanShow(analyzedMethod))
				this.Children.Add(new AnalyzedInterfaceMethodImplementedByTreeNode(analyzedMethod));
		}

		public override IMemberRef Member {
			get { return analyzedMethod; }
		}

		public override IMDTokenProvider MDTokenProvider {
			get { return analyzedMethod; }
		}
	}
}
