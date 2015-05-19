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
using ICSharpCode.Decompiler;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Module reference in ReferenceFolderTreeNode.
	/// </summary>
	sealed class ModuleReferenceTreeNode : ILSpyTreeNode, ITokenTreeNode
	{
		readonly ModuleRef r;

		public ModuleRef ModuleReference {
			get { return r; }
		}

		public IMDTokenProvider MDTokenProvider {
			get { return r; }
		}
		
		public ModuleReferenceTreeNode(ModuleRef r)
		{
			if (r == null)
				throw new ArgumentNullException("r");
			this.r = r;
		}
		
		public override object Text {
			get { return ToString(Language); }
		}

		public override string ToString(Language language)
		{
			return CleanUpName(r.Name) + r.MDToken.ToSuffixString();
		}
		
		public override object Icon {
			get { return ImageCache.Instance.GetImage("ModuleReference", BackgroundType.TreeNode); }
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			var res = settings.Filter.GetFilterResult(this.r);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, r.Name);
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("modref", r.FullName); }
		}
	}
}
