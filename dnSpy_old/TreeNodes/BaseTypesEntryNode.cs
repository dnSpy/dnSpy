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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Images;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes {
	public sealed class BaseTypesEntryNode : ILSpyTreeNode, IMemberTreeNode {
		private readonly ITypeDefOrRef tr;
		private TypeDef def;
		private readonly bool isInterface;

		public BaseTypesEntryNode(ITypeDefOrRef tr, bool isInterface) {
			if (tr == null)
				throw new ArgumentNullException("tr");
			this.tr = tr;
			this.def = tr.ResolveTypeDef();
			this.isInterface = isInterface;
			this.LazyLoading = true;
		}

		public override bool ShowExpander {
			get { return def != null && (def.BaseType != null || def.HasInterfaces); }
		}

		protected override void Write(ITextOutput output, Language language) {
			Write(output, tr, language);
		}

		public static ITextOutput Write(ITextOutput output, ITypeDefOrRef tr, Language language) {
			language.TypeToString(output, tr, true);
			tr.MDToken.WriteSuffixString(output);
			return output;
		}

		public override object Icon {
			get {
				if (def != null)
					return TypeTreeNode.GetIcon(def, BackgroundType.TreeNode);
				else
					return isInterface ?
						ImageCache.Instance.GetImage("Interface", BackgroundType.TreeNode) :
						ImageCache.Instance.GetImage("Interface", BackgroundType.TreeNode);
			}
		}

		public override FilterResult Filter(FilterSettings settings) {
			var res = settings.Filter.GetFilterResult(this);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}

		protected override void LoadChildren() {
			if (def != null)
				BaseTypesTreeNode.AddBaseTypes(this.Children, def);
		}

		protected override void ActivateItemInternal(System.Windows.RoutedEventArgs e) {
			// on item activation, try to resolve once again (maybe the user loaded the assembly in the meantime)
			if (def == null) {
				def = tr.ResolveTypeDef();
				if (def != null)
					this.LazyLoading = true;
				// re-load children
			}
			if (ActivateItem(this, def))
				e.Handled = true;
		}

		internal static bool ActivateItem(SharpTreeNode node, TypeDef def) {
			if (def != null) {
				var assemblyListNode = node.Ancestors().OfType<DnSpyFileListTreeNode>().FirstOrDefault();
				if (assemblyListNode != null) {
					assemblyListNode.Select(assemblyListNode.FindTypeNode(def));
					return true;
				}
			}
			return false;
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			language.WriteCommentLine(output, language.TypeToString(tr, true));
		}

		IMemberRef IMemberTreeNode.Member {
			get { return tr; }
		}

		IMDTokenProvider ITokenTreeNode.MDTokenProvider {
			get { return tr; }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("bte", tr.FullName); }
		}
	}
}
