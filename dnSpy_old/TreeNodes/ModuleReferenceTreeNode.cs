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
using dnSpy;
using dnSpy.Contracts;
using dnSpy.Contracts.Images;
using dnSpy.NRefactory;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes {
	/// <summary>
	/// Module reference in ReferenceFolderTreeNode.
	/// </summary>
	public sealed class ModuleReferenceTreeNode : ILSpyTreeNode, ITokenTreeNode {
		readonly ModuleRef r;

		public ModuleRef ModuleReference {
			get { return r; }
		}

		public IMDTokenProvider MDTokenProvider {
			get { return r; }
		}

		public ModuleReferenceTreeNode(ModuleRef r) {
			if (r == null)
				throw new ArgumentNullException("r");
			this.r = r;
		}

		protected override void Write(ITextOutput output, Language language) {
			Write(output, r, language);
		}

		public static ITextOutput Write(ITextOutput output, ModuleRef r, Language language) {
			output.Write(UIUtils.CleanUpIdentifier(r.Name), TextTokenType.Text);
			r.MDToken.WriteSuffixString(output);
			return output;
		}

		public override object Icon {
			get { return Globals.App.ImageManager.GetImage(GetType().Assembly, "ModuleReference", BackgroundType.TreeNode); }
		}

		public override FilterResult Filter(FilterSettings settings) {
			var res = settings.Filter.GetFilterResult(this.r);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			language.WriteCommentLine(output, IdentifierEscaper.Escape(r.Name));
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("modref", r.FullName); }
		}
	}
}
