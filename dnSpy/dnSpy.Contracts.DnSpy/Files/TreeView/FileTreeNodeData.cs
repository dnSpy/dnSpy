/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Files.TreeView {
	/// <summary>
	/// File treenode data base class
	/// </summary>
	public abstract class FileTreeNodeData : TreeNodeData, IFileTreeNodeData {
		/// <inheritdoc/>
		public override bool SingleClickExpandsChildren => Context.SingleClickExpandsChildren;
		/// <summary>Gets the context</summary>
		public IFileTreeNodeDataContext Context { get; set; }
		/// <inheritdoc/>
		public abstract NodePathName NodePathName { get; }
		/// <inheritdoc/>
		protected abstract ImageReference GetIcon(IDotNetImageManager dnImgMgr);
		/// <inheritdoc/>
		protected virtual ImageReference? GetExpandedIcon(IDotNetImageManager dnImgMgr) => null;
		/// <inheritdoc/>
		public sealed override ImageReference Icon => GetIcon(this.Context.FileTreeView.DotNetImageManager);
		/// <inheritdoc/>
		public sealed override ImageReference? ExpandedIcon => GetExpandedIcon(this.Context.FileTreeView.DotNetImageManager);

		/// <inheritdoc/>
		public sealed override object Text {
			get {
				var gen = ColorizedTextElementCreator.Create(Context.SyntaxHighlight);

				var cached = cachedText != null ? cachedText.Target : null;
				if (cached != null)
					return cached;

				Write(gen.Output, Context.Language);

				var text = gen.CreateResultNewFormatter(Context.UseNewRenderer, filterOutNewLines: true);
				cachedText = new WeakReference(text);
				return text;
			}
		}
		WeakReference cachedText;

		/// <summary>
		/// Writes the contents
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="language">Language</param>
		protected abstract void Write(IOutputColorWriter output, ILanguage language);

		/// <summary>
		/// Writes the tooltip
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="language">Language</param>
		protected virtual void WriteToolTip(IOutputColorWriter output, ILanguage language) => Write(output, language);

		/// <inheritdoc/>
		public sealed override object ToolTip {
			get {
				var gen = ColorizedTextElementCreator.Create(Context.SyntaxHighlight);
				WriteToolTip(gen.Output, Context.Language);
				return gen.CreateResult(filterOutNewLines: false);
			}
		}

		/// <inheritdoc/>
		public sealed override string ToString() => ToString(Context.Language);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="language">Language</param>
		/// <returns></returns>
		public string ToString(ILanguage language) {
			var output = new StringBuilderTextColorOutput();
			Write(output, language);
			return output.ToString();
		}

		/// <inheritdoc/>
		public sealed override void OnRefreshUI() => cachedText = null;
		/// <inheritdoc/>
		public override bool Activate() => Context.FileTreeView.RaiseNodeActivated(this);
		/// <inheritdoc/>
		public virtual FilterType GetFilterType(IFileTreeNodeFilter filter) => filter.GetResult(this).FilterType;

		/// <inheritdoc/>
		public sealed override void OnEnsureChildrenLoaded() {
			if (refilter) {
				refilter = false;
				foreach (var node in this.TreeNode.DataChildren.OfType<IFileTreeNodeData>())
					Filter(node);
			}
		}
		bool refilter = false;

		/// <inheritdoc/>
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
				Debug.Fail($"Invalid type: {res}");
				goto case FilterType.Default;
			}
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public sealed override void OnIsVisibleChanged() {
			if (refilter && TreeNode.Children.Count > 0 && TreeNode.IsVisible)
				OnEnsureChildrenLoaded();
		}

		/// <inheritdoc/>
		public void Refilter() {
			if (!TreeNode.IsVisible) {
				refilter = true;
				return;
			}

			foreach (var node in this.TreeNode.DataChildren)
				Filter(node as IFileTreeNodeData);
		}

		/// <inheritdoc/>
		public sealed override bool CanDrag(ITreeNodeData[] nodes) =>
			Context.CanDragAndDrop && nodes.Length != 0 &&
			nodes.All(a => a is IFileTreeNodeData &&
			((IFileTreeNodeData)a).TreeNode.Parent == Context.FileTreeView.TreeView.Root);

		/// <inheritdoc/>
		public sealed override void StartDrag(DependencyObject dragSource, ITreeNodeData[] nodes) {
			bool b = CanDrag(nodes);
			Debug.Assert(b);
			if (!b)
				return;
			DragDrop.DoDragDrop(dragSource, Copy(nodes), DragDropEffects.All);
		}

		/// <inheritdoc/>
		public sealed override IDataObject Copy(ITreeNodeData[] nodes) {
			var dataObject = new DataObject();
			if (!Context.CanDragAndDrop)
				return dataObject;
			var rootNode = Context.FileTreeView.TreeView.Root;
			var dict = new Dictionary<ITreeNodeData, int>();
			for (int i = 0; i < rootNode.Children.Count; i++)
				dict.Add(rootNode.Children[i].Data, i);
			var data = nodes.Where(a => dict.ContainsKey(a)).Select(a => dict[a]).ToArray();
			if (data.Length != 0)
				dataObject.SetData(FileTVConstants.DATAFORMAT_COPIED_ROOT_NODES, data);
			return dataObject;
		}
	}
}
