/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Highlighting;
using dnSpy.Shared.UI.TreeView;
using ICSharpCode.AvalonEdit.Utils;

namespace dnSpy.Shared.UI.Files.TreeView {
	public abstract class FileTreeNodeData : TreeNodeData, IFileTreeNodeData {
		public override bool SingleClickExpandsChildren {
			get { return Context.SingleClickExpandsChildren; }
		}

		public IFileTreeNodeDataContext Context { get; set; }

		public abstract NodePathName NodePathName { get; }

		protected abstract ImageReference GetIcon(IDotNetImageManager dnImgMgr);
		protected virtual ImageReference? GetExpandedIcon(IDotNetImageManager dnImgMgr) {
			return null;
		}

		public sealed override ImageReference Icon {
			get { return GetIcon(this.Context.FileTreeView.DotNetImageManager); }
		}

		public sealed override ImageReference? ExpandedIcon {
			get { return GetExpandedIcon(this.Context.FileTreeView.DotNetImageManager); }
		}

		public sealed override object Text {
			get {
				var gen = UISyntaxHighlighter.Create(Context.SyntaxHighlight);

				var cached = cachedText != null ? cachedText.Target : null;
				if (cached != null)
					return cached;

				Write(gen.Output, Context.Language);

				var provider = Context.UseNewRenderer ? TextFormatterProvider.GlyphRunFormatter : TextFormatterProvider.BuiltIn;
				var text = gen.CreateResult(provider, filterOutNewLines: true);
				cachedText = new WeakReference(text);
				return text;
			}
		}
		WeakReference cachedText;

		protected abstract void Write(ISyntaxHighlightOutput output, ILanguage language);

		protected virtual void WriteToolTip(ISyntaxHighlightOutput output, ILanguage language) {
			Write(output, language);
		}

		public sealed override object ToolTip {
			get {
				var gen = UISyntaxHighlighter.Create(Context.SyntaxHighlight);
				WriteToolTip(gen.Output, Context.Language);
				return gen.CreateResult(filterOutNewLines: false);
			}
		}

		public sealed override string ToString() {
			return ToString(Context.Language);
		}

		public string ToString(ILanguage language) {
			var output = new NoSyntaxHighlightOutput();
			Write(output, language);
			return output.ToString();
		}

		public sealed override void OnRefreshUI() {
			cachedText = null;
		}

		public override bool Activate() {
			return Context.FileTreeView.RaiseNodeActivated(this);
		}

		public virtual FilterType GetFilterType(IFileTreeNodeFilter filter) {
			return filter.GetResult(this).FilterType;
		}

		public sealed override void OnEnsureChildrenLoaded() {
			if (refilter) {
				refilter = false;
				foreach (var node in this.TreeNode.DataChildren.OfType<IFileTreeNodeData>())
					Filter(node);
			}
		}
		bool refilter = false;

		public int FilterVersion {
			get { return filterVersion; }
			set {
				if (filterVersion != value) {
					filterVersion = value;
					Refilter();
				}
			}
		}
		int filterVersion;

		static void Filter(IFileTreeNodeData node) {
			if (node == null)
				return;
			var res = node.GetFilterType(node.Context.Filter);
			switch (res) {
			case FilterType.Default:
			case FilterType.Visible:
				node.FilterVersion = node.Context.FilterVersion;
				node.TreeNode.IsHidden = false;
				var fnode = node as FileTreeNodeData;
				if (fnode != null && fnode.refilter && node.TreeNode.Children.Count > 0)
					node.OnEnsureChildrenLoaded();
				break;

			case FilterType.Hide:
				node.TreeNode.IsHidden = true;
				break;

			case FilterType.CheckChildren:
				node.FilterVersion = node.Context.FilterVersion;
				node.TreeNode.EnsureChildrenLoaded();
				node.TreeNode.IsHidden = node.TreeNode.Children.All(a => a.IsHidden);
				break;

			default:
				Debug.Fail(string.Format("Invalid type: {0}", res));
				goto case FilterType.Default;
			}
		}

		public sealed override void OnChildrenChanged(ITreeNodeData[] added, ITreeNodeData[] removed) {
			if (TreeNode.Parent == null)
				refilter = true;
			else {
				if (added.Length > 0) {
					if (TreeNode.IsHidden)
						Filter(this);
					if (TreeNode.IsVisible) {
						foreach (var node in added)
							Filter(node as IFileTreeNodeData);
					}
					else
						refilter = true;
				}

				if (TreeNode.IsVisible && TreeNode.Children.Count == 0)
					Filter(this);
			}
		}

		public sealed override void OnIsVisibleChanged() {
			if (refilter && TreeNode.Children.Count > 0 && TreeNode.IsVisible)
				OnEnsureChildrenLoaded();
		}

		public void Refilter() {
			if (!TreeNode.IsVisible) {
				refilter = true;
				return;
			}

			foreach (var node in this.TreeNode.DataChildren)
				Filter(node as IFileTreeNodeData);
		}
	}
}
