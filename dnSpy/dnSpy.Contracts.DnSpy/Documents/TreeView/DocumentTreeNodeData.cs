/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Document treenode data base class
	/// </summary>
	public abstract class DocumentTreeNodeData : TreeNodeData {
		/// <summary>
		/// true if single clicking on a node expands all its children
		/// </summary>
		public override bool SingleClickExpandsChildren => Context.SingleClickExpandsChildren;

		/// <summary>
		/// Gets the context. Should only be set by the owner <see cref="IDocumentTreeView"/>
		/// </summary>
		public IDocumentTreeNodeDataContext Context { get; set; }

		/// <summary>
		/// Gets the node path name
		/// </summary>
		public abstract NodePathName NodePathName { get; }

		/// <summary>
		/// Gets the icon
		/// </summary>
		/// <param name="dnImgMgr">Image service</param>
		/// <returns></returns>
		protected abstract ImageReference GetIcon(IDotNetImageService dnImgMgr);

		/// <summary>
		/// Gets the icon shown when the node has been expanded
		/// </summary>
		/// <param name="dnImgMgr">Image service</param>
		/// <returns></returns>
		protected virtual ImageReference? GetExpandedIcon(IDotNetImageService dnImgMgr) => null;

		/// <summary>
		/// Icon
		/// </summary>
		public sealed override ImageReference Icon => GetIcon(Context.DocumentTreeView.DotNetImageService);

		/// <summary>
		/// Expanded icon or null to use <see cref="Icon"/>
		/// </summary>
		public sealed override ImageReference? ExpandedIcon => GetExpandedIcon(Context.DocumentTreeView.DotNetImageService);

		static class Cache {
			static readonly TextClassifierTextColorWriter writer = new TextClassifierTextColorWriter();
			public static TextClassifierTextColorWriter GetWriter() => writer;
			public static void FreeWriter(TextClassifierTextColorWriter writer) => writer.Clear();
		}

		/// <summary>
		/// Gets the data shown in the UI
		/// </summary>
		public sealed override object? Text {
			get {
				if (cachedText?.Target is object cached)
					return cached;

				var writer = Cache.GetWriter();
				try {
					WriteCore(writer, Context.Decompiler, DocumentNodeWriteOptions.None);
					var classifierContext = new TreeViewNodeClassifierContext(writer.Text, Context.DocumentTreeView.TreeView, this, isToolTip: false, colorize: Context.SyntaxHighlight, colors: writer.Colors);
					var elem = Context.TreeViewNodeTextElementProvider.CreateTextElement(classifierContext, TreeViewContentTypes.TreeViewNodeAssemblyExplorer, TextElementFlags.FilterOutNewLines);
					cachedText = new WeakReference(elem);
					return elem;
				}
				finally {
					Cache.FreeWriter(writer);
				}
			}
		}
		WeakReference? cachedText;

		/// <summary>
		/// Writes the contents
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="options">Options</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) =>
			WriteCore(output, decompiler, options);

		/// <summary>
		/// Writes the contents
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="options">Options</param>
		protected abstract void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options);

		/// <summary>
		/// Returns true if <see cref="WriteCore(ITextColorWriter, IDecompiler, DocumentNodeWriteOptions)"/> should show tokens
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		protected bool GetShowToken(DocumentNodeWriteOptions options) =>
			(options & DocumentNodeWriteOptions.ToolTip) != 0 || (options & DocumentNodeWriteOptions.Title) == 0 ? Context.ShowToken : false;

		/// <summary>
		/// Gets the data shown in a tooltip
		/// </summary>
		public sealed override object? ToolTip {
			get {
				var writer = Cache.GetWriter();
				WriteCore(writer, Context.Decompiler, DocumentNodeWriteOptions.ToolTip);
				var classifierContext = new TreeViewNodeClassifierContext(writer.Text, Context.DocumentTreeView.TreeView, this, isToolTip: true, colorize: Context.SyntaxHighlight, colors: writer.Colors);
				var elem = Context.TreeViewNodeTextElementProvider.CreateTextElement(classifierContext, TreeViewContentTypes.TreeViewNodeAssemblyExplorer, TextElementFlags.None);
				Cache.FreeWriter(writer);
				return elem;
			}
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public sealed override string ToString() => ToString(Context.Decompiler);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public string ToString(DocumentNodeWriteOptions options = DocumentNodeWriteOptions.None) =>
			ToString(Context.Decompiler, options);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public string ToString(IDecompiler decompiler, DocumentNodeWriteOptions options = DocumentNodeWriteOptions.None) {
			var output = new StringBuilderTextColorOutput();
			WriteCore(output, decompiler, options);
			return output.ToString();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		protected DocumentTreeNodeData() {
			Context = null!;
		}

		/// <summary>
		/// Called by <see cref="ITreeNode.RefreshUI()"/> before it invalidates all UI properties
		/// </summary>
		public sealed override void OnRefreshUI() => cachedText = null;

		/// <summary>
		/// Called when the item gets activated, eg. double clicked. Returns true if it was handled,
		/// false otherwise.
		/// </summary>
		/// <returns></returns>
		public override bool Activate() => Context.DocumentTreeView.RaiseNodeActivated(this);

		/// <summary>
		/// Gets the <see cref="FilterType"/> to filter this instance
		/// </summary>
		/// <param name="filter">Filter to call</param>
		/// <returns></returns>
		public virtual FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResultOther(this).FilterType;

		/// <summary>
		/// Called by <see cref="ITreeNode.EnsureChildrenLoaded()"/>
		/// </summary>
		public sealed override void OnEnsureChildrenLoaded() {
			if (refilter) {
				refilter = false;
				foreach (var node in TreeNode.DataChildren.OfType<DocumentTreeNodeData>())
					Filter(node);
			}
		}
		bool refilter = false;

		/// <summary>
		/// The class (<see cref="DocumentTreeNodeData"/>) should call <see cref="Refilter()"/> when updating
		/// this value.
		/// </summary>
		public int FilterVersion {
			get => filterVersion;
			set {
				if (filterVersion != value) {
					filterVersion = value;
					Refilter();
				}
			}
		}
		int filterVersion;

		static void Filter(DocumentTreeNodeData? node) {
			if (node is null)
				return;
			var res = node.GetFilterType(node.Context.Filter);
			switch (res) {
			case FilterType.Default:
			case FilterType.Visible:
				node.FilterVersion = node.Context.FilterVersion;
				node.TreeNode.IsHidden = false;
				var fnode = node as DocumentTreeNodeData;
				if (!(fnode is null) && fnode.refilter && node.TreeNode.Children.Count > 0)
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

		/// <summary>
		/// Called when the children has changed
		/// </summary>
		/// <param name="added">Added nodes</param>
		/// <param name="removed">Removed nodes</param>
		public sealed override void OnChildrenChanged(TreeNodeData[] added, TreeNodeData[] removed) {
			if (TreeNode.Parent is null)
				refilter = true;
			else {
				if (added.Length > 0) {
					if (TreeNode.IsHidden)
						Filter(this);
					if (TreeNode.IsVisible) {
						foreach (var node in added)
							Filter(node as DocumentTreeNodeData);
					}
					else
						refilter = true;
				}

				if (TreeNode.IsVisible && TreeNode.Children.Count == 0)
					Filter(this);
			}
		}

		/// <summary>
		/// Called when <see cref="ITreeNode.IsVisible"/> has changed
		/// </summary>
		public sealed override void OnIsVisibleChanged() {
			if (refilter && TreeNode.Children.Count > 0 && TreeNode.IsVisible)
				OnEnsureChildrenLoaded();
		}

		/// <summary>
		/// Called when <see cref="IDocumentTreeNodeDataContext.Filter"/> has changed
		/// </summary>
		public void Refilter() {
			if (!TreeNode.IsVisible) {
				refilter = true;
				return;
			}

			foreach (var node in TreeNode.DataChildren)
				Filter(node as DocumentTreeNodeData);
		}

		/// <summary>
		/// Returns true if the nodes can be dragged
		/// </summary>
		/// <param name="nodes">Nodes</param>
		/// <returns></returns>
		public sealed override bool CanDrag(TreeNodeData[] nodes) =>
			Context.CanDragAndDrop && nodes.Length != 0 &&
			nodes.All(a => a is DocumentTreeNodeData &&
			((DocumentTreeNodeData)a).TreeNode.Parent == Context.DocumentTreeView.TreeView.Root);

		/// <summary>
		/// Starts the drag and drop operation
		/// </summary>
		/// <param name="dragSource">Drag source</param>
		/// <param name="nodes">Nodes</param>
		public sealed override void StartDrag(DependencyObject dragSource, TreeNodeData[] nodes) {
			bool b = CanDrag(nodes);
			Debug.Assert(b);
			if (!b)
				return;
			try {
				DragDrop.DoDragDrop(dragSource, Copy(nodes), DragDropEffects.All);
			}
			catch (COMException) {
			}
		}

		/// <summary>
		/// Copies nodes
		/// </summary>
		/// <param name="nodes">Nodes</param>
		/// <returns></returns>
		public sealed override IDataObject Copy(TreeNodeData[] nodes) {
			var dataObject = new DataObject();
			if (!Context.CanDragAndDrop)
				return dataObject;
			var rootNode = Context.DocumentTreeView.TreeView.Root;
			var dict = new Dictionary<TreeNodeData, int>();
			for (int i = 0; i < rootNode.Children.Count; i++)
				dict.Add(rootNode.Children[i].Data, i);
			var data = nodes.Where(a => dict.ContainsKey(a)).Select(a => dict[a]).ToArray();
			if (data.Length != 0)
				dataObject.SetData(DocumentTreeViewConstants.DATAFORMAT_COPIED_ROOT_NODES, data);
			return dataObject;
		}

		/// <summary>
		/// Writes the filename of the module
		/// </summary>
		/// <param name="output">Output</param>
		protected void WriteFilename(ITextColorWriter output) => output.WriteFilename(this.GetModule()?.Location ?? "???");

		/// <summary>
		/// Writes a module/assembly
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="scope">Scope</param>
		protected void WriteScope(ITextColorWriter output, IScope? scope) {
			if (scope is AssemblyRef asmRef)
				output.Write(asmRef);
			else if (scope is ModuleRef modRef)
				output.WriteModule(modRef.Name);
			else if (scope is ModuleDef modDef)
				output.WriteModule(modDef.Name);
			else
				output.Write(BoxedTextColor.Error, "???");
		}

		/// <summary>
		/// Writes the member
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="member">Member</param>
		protected void WriteMemberRef(ITextColorWriter output, IDecompiler decompiler, IMemberRef member) {
			decompiler.WriteToolTip(output, member, member as IHasCustomAttribute);
			if (member.DeclaringType is ITypeDefOrRef declType) {
				output.WriteLine();
				decompiler.WriteToolTip(output, declType, declType);
			}
		}

		/// <summary>
		/// Gets data added by <see cref="AddData{T}(T)"/>
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="data">Updated with the data if successful</param>
		/// <returns></returns>
		public bool TryGetData<T>([NotNullWhen(true)] out T? data) where T : class {
			if (!(dataList is null)) {
				foreach (var obj in dataList) {
					if (obj is T t) {
						data = t;
						return true;
					}
				}
			}

			data = null;
			return false;
		}

		/// <summary>
		/// Adds data
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="data">Data</param>
		public void AddData<T>(T data) where T : class {
			if (data is null)
				throw new ArgumentNullException(nameof(data));
			if (dataList is null)
				dataList = new List<object>();
			dataList.Add(data);
		}
		List<object>? dataList;
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class DocumentTreeNodeDataExtensionMethods {
		/// <summary>
		/// Gets the <see cref="AssemblyDocumentNode"/> owner or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static AssemblyDocumentNode? GetAssemblyNode(this TreeNodeData? self) => self.GetAncestorOrSelf<AssemblyDocumentNode>();

		/// <summary>
		/// Gets the <see cref="ModuleDocumentNode"/> owner or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static ModuleDocumentNode? GetModuleNode(this TreeNodeData? self) => self.GetAncestorOrSelf<ModuleDocumentNode>();

		/// <summary>
		/// Gets the first <see cref="DsDocumentNode"/> owner or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static DsDocumentNode? GetDocumentNode(this TreeNodeData? self) => self.GetAncestorOrSelf<DsDocumentNode>();

		/// <summary>
		/// Gets the <see cref="DsDocumentNode"/> top node or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static DsDocumentNode? GetTopNode(this TreeNodeData? self) {
			var root = self is null ? null : self.TreeNode.TreeView.Root;
			while (!(self is null)) {
				if (self is DsDocumentNode found) {
					var p = found.TreeNode.Parent;
					if (p is null || p == root)
						return found;
				}
				var parent = self.TreeNode.Parent;
				if (parent is null)
					break;
				self = parent.Data;
			}
			return null;
		}

		/// <summary>
		/// Gets the <see cref="ModuleDef"/> instance or null
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static ModuleDef? GetModule(this TreeNodeData? self) {
			var node = self.GetDocumentNode();
			return node?.Document.ModuleDef;
		}

		/// <summary>
		/// Gets the <see cref="ModuleDef"/> instance or null
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static ModuleDef? GetParentModule(this TreeNodeData? self) {
			var node = self?.TreeNode.Parent?.Data.GetDocumentNode();
			return node?.Document.ModuleDef;
		}
	}
}
