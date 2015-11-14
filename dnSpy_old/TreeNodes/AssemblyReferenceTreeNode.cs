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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Images;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Highlighting;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes {
	/// <summary>
	/// Node within assembly reference list.
	/// </summary>
	public sealed class AssemblyReferenceTreeNode : ILSpyTreeNode, ITokenTreeNode {
		readonly AssemblyRef r;
		readonly DnSpyFileListTreeNode dnSpyFileListTreeNode;
		readonly AssemblyTreeNode parentAssembly;

		internal AssemblyReferenceTreeNode(AssemblyRef r, AssemblyTreeNode parentAssembly, DnSpyFileListTreeNode dnSpyFileListTreeNode) {
			if (parentAssembly == null)
				throw new ArgumentNullException("parentAssembly");
			if (dnSpyFileListTreeNode == null)
				throw new ArgumentNullException("dnSpyFileListTreeNode");
			if (r == null)
				throw new ArgumentNullException("r");
			this.r = r;
			this.dnSpyFileListTreeNode = dnSpyFileListTreeNode;
			this.parentAssembly = parentAssembly;
			this.LazyLoading = true;
		}

		public AssemblyRef AssemblyNameReference {
			get { return r; }
		}

		public IMDTokenProvider MDTokenProvider {
			get { return r; }
		}

		protected override void Write(ITextOutput output, Language language) {
			Write(output, r, language);
		}

		public static ITextOutput Write(ITextOutput output, AssemblyRef r, Language language) {
			output.Write(NameUtils.CleanIdentifier(r.Name), TextTokenType.Text);
			r.MDToken.WriteSuffixString(output);
			return output;
		}

		public override object Icon {
			get { return DnSpy.App.ImageManager.GetImage(GetType().Assembly, "AssemblyReference", BackgroundType.TreeNode); }
		}

		public override FilterResult Filter(FilterSettings settings) {
			var res = settings.Filter.GetFilterResult(this.r);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}

		static AssemblyDef LookupReferencedAssembly(IDnSpyFile file, IAssembly asm) {
			var mod = file.ModuleDef;
			return mod == null ? null : mod.Context.AssemblyResolver.Resolve(asm, mod);
		}

		protected override void ActivateItemInternal(System.Windows.RoutedEventArgs e) {
			var assemblyListNode = dnSpyFileListTreeNode;
			Debug.Assert(assemblyListNode != null);
			if (assemblyListNode != null) {
				assemblyListNode.Select(assemblyListNode.FindAssemblyNode(LookupReferencedAssembly(parentAssembly.DnSpyFile, r)));
				e.Handled = true;
			}
		}

		protected override void LoadChildren() {
			var assemblyListNode = dnSpyFileListTreeNode;
			Debug.Assert(assemblyListNode != null);
			if (assemblyListNode != null) {
				var refNode = assemblyListNode.FindAssemblyNode(LookupReferencedAssembly(parentAssembly.DnSpyFile, r));
				if (refNode != null) {
					foreach (var childRef in refNode.DnSpyFile.ModuleDef.GetAssemblyRefs())
						this.Children.Add(new AssemblyReferenceTreeNode(childRef, refNode, dnSpyFileListTreeNode));
				}
			}
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			if (r.IsContentTypeWindowsRuntime) {
				language.WriteCommentLine(output, r.Name + " [WinRT]");
			}
			else {
				language.WriteCommentLine(output, r.FullName);
			}
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("asmref", r.FullName); }
		}
	}
}
