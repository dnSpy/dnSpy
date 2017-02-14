/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;
using dnSpy.Properties;

namespace dnSpy.Documents.TreeView {
	[Export, Export(typeof(IDocumentTreeView))]
	sealed class DocumentTreeView : IDocumentTreeView, ITreeViewListener {
		readonly DocumentTreeNodeDataContext context;
		readonly Lazy<IDsDocumentNodeProvider, IDsDocumentNodeProviderMetadata>[] dsDocumentNodeProvider;
		readonly Lazy<IDocumentTreeNodeDataFinder, IDocumentTreeNodeDataFinderMetadata>[] nodeFinders;

		public IDocumentTreeNodeGroups DocumentTreeNodeGroups => documentTreeNodeGroups;
		readonly DocumentTreeNodeGroups documentTreeNodeGroups;

		public IDsDocumentService DocumentService { get; }
		public ITreeView TreeView { get; }
		IEnumerable<DsDocumentNode> TopNodes => TreeView.Root.Children.Select(a => (DsDocumentNode)a.Data);
		public IDotNetImageService DotNetImageService { get; }
		public IWpfCommands WpfCommands { get; }
		public event EventHandler<NotifyDocumentTreeViewCollectionChangedEventArgs> CollectionChanged;

		void CallCollectionChanged(NotifyDocumentTreeViewCollectionChangedEventArgs eventArgs) =>
			CollectionChanged?.Invoke(this, eventArgs);
		public event EventHandler<DocumentTreeNodeActivatedEventArgs> NodeActivated;

		public bool RaiseNodeActivated(DocumentTreeNodeData node) {
			if (node == null)
				throw new ArgumentNullException(nameof(node));
			if (NodeActivated == null)
				return false;
			var e = new DocumentTreeNodeActivatedEventArgs(node);
			NodeActivated(this, e);
			return e.Handled;
		}

		public event EventHandler<TreeViewSelectionChangedEventArgs> SelectionChanged;
		bool disable_SelectionChanged = false;

		void TreeView_SelectionChanged(object sender, TreeViewSelectionChangedEventArgs e) {
			if (disable_SelectionChanged)
				return;
			SelectionChanged?.Invoke(this, e);
		}

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly ITreeView treeView;

			public GuidObjectsProvider(ITreeView treeView) => this.treeView = treeView;

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_TREEVIEW_NODES_ARRAY_GUID, treeView.TopLevelSelection);
			}
		}

		[ImportingConstructor]
		DocumentTreeView(ITreeViewService treeViewService, IDecompilerService decompilerService, IDsDocumentService documentService, IDocumentTreeViewSettings documentTreeViewSettings, IMenuService menuService, IDotNetImageService dotNetImageService, IWpfCommandService wpfCommandService, IResourceNodeFactory resourceNodeFactory, IAppSettings appSettings, [ImportMany] IEnumerable<Lazy<IDsDocumentNodeProvider, IDsDocumentNodeProviderMetadata>> dsDocumentNodeProviders, [ImportMany] IEnumerable<Lazy<IDocumentTreeNodeDataFinder, IDocumentTreeNodeDataFinderMetadata>> mefFinders, ITreeViewNodeTextElementProvider treeViewNodeTextElementProvider)
			: this(true, null, treeViewService, decompilerService, documentService, documentTreeViewSettings, menuService, dotNetImageService, wpfCommandService, resourceNodeFactory, appSettings, dsDocumentNodeProviders, mefFinders, treeViewNodeTextElementProvider) {
		}

		readonly IDecompilerService decompilerService;
		readonly IDocumentTreeViewSettings documentTreeViewSettings;
		readonly IAppSettings appSettings;

		public DocumentTreeView(bool isGlobal, IDocumentTreeNodeFilter filter, ITreeViewService treeViewService, IDecompilerService decompilerService, IDsDocumentService documentService, IDocumentTreeViewSettings documentTreeViewSettings, IMenuService menuService, IDotNetImageService dotNetImageService, IWpfCommandService wpfCommandService, IResourceNodeFactory resourceNodeFactory, IAppSettings appSettings, [ImportMany] IEnumerable<Lazy<IDsDocumentNodeProvider, IDsDocumentNodeProviderMetadata>> dsDocumentNodeProvider, [ImportMany] IEnumerable<Lazy<IDocumentTreeNodeDataFinder, IDocumentTreeNodeDataFinderMetadata>> mefFinders, ITreeViewNodeTextElementProvider treeViewNodeTextElementProvider) {
			this.decompilerService = decompilerService;
			this.documentTreeViewSettings = documentTreeViewSettings;
			this.appSettings = appSettings;

			context = new DocumentTreeNodeDataContext(this, resourceNodeFactory, filter ?? FilterNothingDocumentTreeNodeFilter.Instance, treeViewNodeTextElementProvider) {
				SyntaxHighlight = documentTreeViewSettings.SyntaxHighlight,
				SingleClickExpandsChildren = documentTreeViewSettings.SingleClickExpandsTreeViewChildren,
				ShowAssemblyVersion = documentTreeViewSettings.ShowAssemblyVersion,
				ShowAssemblyPublicKeyToken = documentTreeViewSettings.ShowAssemblyPublicKeyToken,
				ShowToken = documentTreeViewSettings.ShowToken,
				Decompiler = decompilerService.Decompiler,
				UseNewRenderer = appSettings.UseNewRenderer_DocumentTreeView,
				DeserializeResources = documentTreeViewSettings.DeserializeResources,
				CanDragAndDrop = isGlobal,
			};

			var options = new TreeViewOptions {
				AllowDrop = true,
				IsVirtualizing = true,
				VirtualizationMode = VirtualizationMode.Recycling,
				TreeViewListener = this,
				RootNode = new RootNode {
					DropNodes = OnDropNodes,
					DropFiles = OnDropFiles,
				},
			};
			documentTreeNodeGroups = new DocumentTreeNodeGroups();
			this.dsDocumentNodeProvider = dsDocumentNodeProvider.OrderBy(a => a.Metadata.Order).ToArray();
			TreeView = treeViewService.Create(new Guid(TreeViewConstants.DOCUMENT_TREEVIEW_GUID), options);
			TreeView.SelectionChanged += TreeView_SelectionChanged;
			DocumentService = documentService;
			DotNetImageService = dotNetImageService;
			dispatcher = Dispatcher.CurrentDispatcher;
			DocumentService.SetDispatcher(AddAction);
			documentService.CollectionChanged += DocumentService_CollectionChanged;
			decompilerService.DecompilerChanged += DecompilerService_DecompilerChanged;
			documentTreeViewSettings.PropertyChanged += DocumentTreeViewSettings_PropertyChanged;
			appSettings.PropertyChanged += AppSettings_PropertyChanged;

			WpfCommands = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENT_TREEVIEW);

			if (isGlobal) {
				menuService.InitializeContextMenu(TreeView.UIObject, new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID), new GuidObjectsProvider(TreeView));
				wpfCommandService.Add(ControlConstants.GUID_DOCUMENT_TREEVIEW, TreeView.UIObject);
			}

			nodeFinders = mefFinders.OrderBy(a => a.Metadata.Order).ToArray();
			InitializeDocumentTreeNodeGroups();
		}

		readonly Dispatcher dispatcher;
		internal void AddAction(Action action) {
			if (!dispatcher.HasShutdownFinished && !dispatcher.HasShutdownStarted) {
				bool callInvoke;
				lock (actionsToCall) {
					actionsToCall.Add(action);
					callInvoke = actionsToCall.Count == 1;
				}
				if (callInvoke) {
					// Always notify with a delay because adding stuff to the tree view could
					// cause some problems with the tree view or the list box it derives from.
					dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(CallActions));
				}
			}
		}

		// It's not using IDisposable.Dispose() because MEF will call Dispose() at app exit which
		// will trigger code paths that try to call some MEF funcs which will throw since MEF is
		// closing down.
		void IDocumentTreeView.Dispose() {
			DocumentService.CollectionChanged -= DocumentService_CollectionChanged;
			decompilerService.DecompilerChanged -= DecompilerService_DecompilerChanged;
			documentTreeViewSettings.PropertyChanged -= DocumentTreeViewSettings_PropertyChanged;
			appSettings.PropertyChanged -= AppSettings_PropertyChanged;
			DocumentService.Clear();
			TreeView.Root.Children.Clear();
			TreeView.SelectItems(Array.Empty<TreeNodeData>());
			TreeView.Dispose();
			context.Clear();
		}

		void RefilterNodes() {
			context.FilterVersion++;
			((RootNode)TreeView.Root.Data).Refilter();
		}

		void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var appSettings = (IAppSettings)sender;
			if (e.PropertyName == nameof(appSettings.UseNewRenderer_DocumentTreeView)) {
				context.UseNewRenderer = appSettings.UseNewRenderer_DocumentTreeView;
				RefreshNodes();
			}
		}

		void InitializeDocumentTreeNodeGroups() {
			var orders = new MemberKind[] {
				documentTreeViewSettings.MemberKind0,
				documentTreeViewSettings.MemberKind1,
				documentTreeViewSettings.MemberKind2,
				documentTreeViewSettings.MemberKind3,
				documentTreeViewSettings.MemberKind4,
			};
			documentTreeNodeGroups.SetMemberOrder(orders);
		}

		readonly List<Action> actionsToCall = new List<Action>();

		void CallActions() {
			List<Action> list;
			lock (actionsToCall) {
				list = new List<Action>(actionsToCall);
				actionsToCall.Clear();
			}
			foreach (var a in list)
				a();
		}

		void DocumentTreeViewSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var documentTreeViewSettings = (IDocumentTreeViewSettings)sender;
			switch (e.PropertyName) {
			case nameof(documentTreeViewSettings.SyntaxHighlight):
				context.SyntaxHighlight = documentTreeViewSettings.SyntaxHighlight;
				RefreshNodes();
				break;

			case nameof(documentTreeViewSettings.ShowAssemblyVersion):
				context.ShowAssemblyVersion = documentTreeViewSettings.ShowAssemblyVersion;
				RefreshNodes();
				NotifyNodesTextRefreshed();
				break;

			case nameof(documentTreeViewSettings.ShowAssemblyPublicKeyToken):
				context.ShowAssemblyPublicKeyToken = documentTreeViewSettings.ShowAssemblyPublicKeyToken;
				RefreshNodes();
				NotifyNodesTextRefreshed();
				break;

			case nameof(documentTreeViewSettings.ShowToken):
				context.ShowToken = documentTreeViewSettings.ShowToken;
				RefreshNodes();
				NotifyNodesTextRefreshed();
				break;

			case nameof(documentTreeViewSettings.SingleClickExpandsTreeViewChildren):
				context.SingleClickExpandsChildren = documentTreeViewSettings.SingleClickExpandsTreeViewChildren;
				break;

			case nameof(documentTreeViewSettings.DeserializeResources):
				context.DeserializeResources = documentTreeViewSettings.DeserializeResources;
				break;

			default:
				break;
			}
		}

		public event EventHandler<EventArgs> NodesTextChanged;
		void NotifyNodesTextRefreshed() => NodesTextChanged?.Invoke(this, EventArgs.Empty);
		void DecompilerService_DecompilerChanged(object sender, EventArgs e) => UpdateDecompiler(((IDecompilerService)sender).Decompiler);

		void UpdateDecompiler(IDecompiler newDecompiler) {
			context.Decompiler = newDecompiler;
			RefreshNodes();
			RefilterNodes();
			NotifyNodesTextRefreshed();
		}

		void IDocumentTreeView.SetDecompiler(IDecompiler decompiler) {
			if (decompiler == null)
				return;
			UpdateDecompiler(decompiler);
		}

		public void RefreshNodes(bool showMember, bool memberOrder) {
			if (showMember) {
				RefreshNodes();
				RefilterNodes();
			}
			/*TODO: memberOrder
			Should call InitializeDocumentTreeNodeGroups(). Some stuff that must be fixed:
			The asm editor has some classes that store indexes of nodes, and would need to be
			updated to just use the normal AddChild() method to restore the node.
			Also, when the asm editor reinserts a node, its children (recursively) must be resorted
			if the sort order has changed.
			*/
		}

		void RefreshNodes() => TreeView.RefreshAllNodes();

		void DocumentService_CollectionChanged(object sender, NotifyDocumentCollectionChangedEventArgs e) {
			switch (e.Type) {
			case NotifyDocumentCollectionType.Add:
				DsDocumentNode newNode;

				var addDocumentInfo = e.Data as AddDocumentInfo;
				int index;
				if (addDocumentInfo != null) {
					newNode = addDocumentInfo.DsDocumentNode;
					index = addDocumentInfo.Index;
					if (newNode.TreeNode == null)
						TreeView.Create(newNode);
				}
				else {
					newNode = CreateNode(null, e.Documents[0]);
					TreeView.Create(newNode);
					index = TreeView.Root.Children.Count;
				}

				if ((uint)index >= (uint)TreeView.Root.Children.Count)
					index = TreeView.Root.Children.Count;
				TreeView.Root.Children.Insert(index, newNode.TreeNode);
				CallCollectionChanged(NotifyDocumentTreeViewCollectionChangedEventArgs.CreateAdd(newNode));
				break;

			case NotifyDocumentCollectionType.Remove:
				var dict = new Dictionary<DsDocumentNode, int>();
				var dict2 = new Dictionary<IDsDocument, DsDocumentNode>();
				int i = 0;
				foreach (var n in TopNodes) {
					dict[n] = i++;
					dict2[n.Document] = n;
				}
				var list = new List<(DsDocumentNode docNode, int index)>(e.Documents.Select(a => {
					bool b = dict2.TryGetValue(a, out var node);
					Debug.Assert(b);
					int j = -1;
					b = b && dict.TryGetValue(node, out j);
					Debug.Assert(b);
					return ValueTuple.Create(node, b ? j : -1);
				}));
				list.Sort((a, b) => b.index.CompareTo(a.index));
				var removed = new List<DsDocumentNode>();
				foreach (var t in list) {
					if (t.index < 0)
						continue;
					Debug.Assert((uint)t.index < (uint)TreeView.Root.Children.Count);
					Debug.Assert(TreeView.Root.Children[t.index].Data == t.docNode);
					TreeView.Root.Children.RemoveAt(t.index);
					removed.Add(t.docNode);
				}
				DisableMemoryMappedIO(list.Select(a => a.docNode).ToArray());
				CallCollectionChanged(NotifyDocumentTreeViewCollectionChangedEventArgs.CreateRemove(removed.ToArray()));
				break;

			case NotifyDocumentCollectionType.Clear:
				var oldNodes = TreeView.Root.Children.Select(a => (DsDocumentNode)a.Data).ToArray();
				TreeView.Root.Children.Clear();
				DisableMemoryMappedIO(oldNodes);
				CallCollectionChanged(NotifyDocumentTreeViewCollectionChangedEventArgs.CreateClear(oldNodes));
				break;

			default:
				Debug.Fail(string.Format("Unknown event type: {0}", e.Type));
				break;
			}
		}

		public void Remove(IEnumerable<DsDocumentNode> nodes) => DocumentService.Remove(nodes.Select(a => a.Document));

		void DisableMemoryMappedIO(DsDocumentNode[] nodes) {
			// The nodes will be GC'd eventually, but it's not safe to call Dispose(), so disable
			// mmap'd I/O so the documents can at least be modified (eg. deleted) by the user.
			foreach (var node in nodes) {
				foreach (var f in node.Document.GetAllChildrenAndSelf())
					MemoryMappedIOHelper.DisableMemoryMappedIO(f);
			}
		}

		public DsDocumentNode CreateNode(DsDocumentNode owner, IDsDocument document) {
			foreach (var provider in dsDocumentNodeProvider) {
				var result = provider.Value.Create(this, owner, document);
				if (result != null)
					return result;
			}

			return new UnknownDocumentNodeImpl(document);
		}

		void ITreeViewListener.OnEvent(ITreeView treeView, TreeViewListenerEventArgs e) {
			if (e.Event == TreeViewListenerEvent.NodeCreated) {
				Debug.Assert(context != null);
				var node = (ITreeNode)e.Argument;
				if (node.Data is DocumentTreeNodeData d)
					d.Context = context;
				return;
			}
		}

		public AssemblyDocumentNode CreateAssembly(IDsDotNetDocument asmDocument) =>
			(AssemblyDocumentNode)TreeView.Create(new AssemblyDocumentNodeImpl(asmDocument)).Data;
		public ModuleDocumentNode CreateModule(IDsDotNetDocument modDocument) =>
			(ModuleDocumentNode)TreeView.Create(new ModuleDocumentNodeImpl(modDocument)).Data;
		public AssemblyReferenceNode Create(AssemblyRef asmRef, ModuleDef ownerModule) =>
			(AssemblyReferenceNode)TreeView.Create(new AssemblyReferenceNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.AssemblyRefTreeNodeGroupReferences), ownerModule, asmRef)).Data;
		public ModuleReferenceNode Create(ModuleRef modRef) =>
			(ModuleReferenceNode)TreeView.Create(new ModuleReferenceNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ModuleRefTreeNodeGroupReferences), modRef)).Data;
		public MethodNode CreateEvent(MethodDef method) =>
			(MethodNode)TreeView.Create(new MethodNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodTreeNodeGroupEvent), method)).Data;
		public MethodNode CreateProperty(MethodDef method) =>
			(MethodNode)TreeView.Create(new MethodNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodTreeNodeGroupProperty), method)).Data;
		public NamespaceNode Create(string name) =>
			(NamespaceNode)TreeView.Create(new NamespaceNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.NamespaceTreeNodeGroupModule), name, new List<TypeDef>())).Data;
		public TypeNode Create(TypeDef type) =>
			(TypeNode)TreeView.Create(new TypeNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.TypeTreeNodeGroupNamespace), type)).Data;
		public TypeNode CreateNested(TypeDef type) =>
			(TypeNode)TreeView.Create(new TypeNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.TypeTreeNodeGroupType), type)).Data;
		public MethodNode Create(MethodDef method) =>
			(MethodNode)TreeView.Create(new MethodNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodTreeNodeGroupType), method)).Data;
		public PropertyNode Create(PropertyDef property) =>
			(PropertyNode)TreeView.Create(new PropertyNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.PropertyTreeNodeGroupType), property)).Data;
		public EventNode Create(EventDef @event) =>
			(EventNode)TreeView.Create(new EventNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.EventTreeNodeGroupType), @event)).Data;
		public FieldNode Create(FieldDef field) =>
			(FieldNode)TreeView.Create(new FieldNodeImpl(DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.FieldTreeNodeGroupType), field)).Data;

		public DocumentTreeNodeData FindNode(object @ref) {
			if (@ref == null)
				return null;
			if (@ref is DocumentTreeNodeData)
				return (DocumentTreeNodeData)@ref;
			if (@ref is IDsDocument)
				return FindNode((IDsDocument)@ref);
			if (@ref is AssemblyDef)
				return FindNode((AssemblyDef)@ref);
			if (@ref is ModuleDef)
				return FindNode((ModuleDef)@ref);
			if (@ref is ITypeDefOrRef)
				return FindNode(((ITypeDefOrRef)@ref).ResolveTypeDef());
			if (@ref is IMethod && ((IMethod)@ref).MethodSig != null)
				return FindNode(((IMethod)@ref).ResolveMethodDef());
			if (@ref is IField)
				return FindNode(((IField)@ref).ResolveFieldDef());
			if (@ref is PropertyDef)
				return FindNode((PropertyDef)@ref);
			if (@ref is EventDef)
				return FindNode((EventDef)@ref);
			if (@ref is ParamDef)
				return FindNode(((ParamDef)@ref).DeclaringMethod);
			if (@ref is NamespaceRef nsRef) {
				return FindNamespaceNode(nsRef.Module, nsRef.Namespace);
			}

			foreach (var finder in nodeFinders) {
				var node = finder.Value.FindNode(this, @ref);
				if (node != null)
					return node;
			}

			return null;
		}

		public DsDocumentNode FindNode(IDsDocument document) {
			if (document == null)
				return null;
			return Find(TopNodes, document);
		}

		DsDocumentNode Find(IEnumerable<DsDocumentNode> nodes, IDsDocument document) {
			foreach (var n in nodes) {
				if (n.Document == document)
					return n;
				if (n.Document.Children.Count == 0)
					continue;
				n.TreeNode.EnsureChildrenLoaded();
				var found = Find(n.TreeNode.DataChildren.OfType<DsDocumentNode>(), document);
				if (found != null)
					return found;
			}
			return null;
		}

		public AssemblyDocumentNode FindNode(AssemblyDef asm) {
			if (asm == null)
				return null;

			foreach (var n in TopNodes.OfType<AssemblyDocumentNode>()) {
				if (n.Document.AssemblyDef == asm)
					return n;
			}

			return null;
		}

		public ModuleDocumentNode FindNode(ModuleDef mod) {
			if (mod == null)
				return null;

			foreach (var n in TopNodes.OfType<AssemblyDocumentNode>()) {
				n.TreeNode.EnsureChildrenLoaded();
				foreach (var m in n.TreeNode.DataChildren.OfType<ModuleDocumentNode>()) {
					if (m.Document.ModuleDef == mod)
						return m;
				}
			}

			// Check for netmodules
			foreach (var n in TopNodes.OfType<ModuleDocumentNode>()) {
				if (n.Document.ModuleDef == mod)
					return n;
			}

			return null;
		}

		public TypeNode FindNode(TypeDef td) {
			if (td == null)
				return null;

			var types = new List<TypeDef>();
			for (var t = td; t != null; t = t.DeclaringType)
				types.Add(t);
			types.Reverse();

			var modNode = FindNode(types[0].Module);
			if (modNode == null)
				return null;

			var nsNode = modNode.FindNode(types[0].Namespace);
			if (nsNode == null)
				return null;

			var typeNode = FindNode(nsNode, types[0]);
			if (typeNode == null)
				return null;

			for (int i = 1; i < types.Count; i++) {
				var childNode = FindNode(typeNode, types[i]);
				if (childNode == null)
					return null;
				typeNode = childNode;
			}

			return typeNode;
		}

		TypeNode FindNode(NamespaceNode nsNode, TypeDef type) {
			if (nsNode == null || type == null)
				return null;

			nsNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in nsNode.TreeNode.DataChildren.OfType<TypeNode>()) {
				if (n.TypeDef == type)
					return n;
			}

			return null;
		}

		TypeNode FindNode(TypeNode typeNode, TypeDef type) {
			if (typeNode == null || type == null)
				return null;

			typeNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in typeNode.TreeNode.DataChildren.OfType<TypeNode>()) {
				if (n.TypeDef == type)
					return n;
			}

			return null;
		}

		public NamespaceNode FindNamespaceNode(IDsDocument module, string @namespace) {
			if (FindNode(module) is ModuleDocumentNode modNode)
				return modNode.FindNode(@namespace);
			return null;
		}

		public MethodNode FindNode(MethodDef md) {
			if (md == null)
				return null;

			var typeNode = FindNode(md.DeclaringType);
			if (typeNode == null)
				return null;

			typeNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in typeNode.TreeNode.DataChildren.OfType<MethodNode>()) {
				if (n.MethodDef == md)
					return n;
			}

			foreach (var n in typeNode.TreeNode.DataChildren.OfType<PropertyNode>()) {
				n.TreeNode.EnsureChildrenLoaded();
				foreach (var m in n.TreeNode.DataChildren.OfType<MethodNode>()) {
					if (m.MethodDef == md)
						return m;
				}
			}

			foreach (var n in typeNode.TreeNode.DataChildren.OfType<EventNode>()) {
				n.TreeNode.EnsureChildrenLoaded();
				foreach (var m in n.TreeNode.DataChildren.OfType<MethodNode>()) {
					if (m.MethodDef == md)
						return m;
				}
			}

			return null;
		}

		public FieldNode FindNode(FieldDef fd) {
			if (fd == null)
				return null;

			var typeNode = FindNode(fd.DeclaringType);
			if (typeNode == null)
				return null;

			typeNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in typeNode.TreeNode.DataChildren.OfType<FieldNode>()) {
				if (n.FieldDef == fd)
					return n;
			}

			return null;
		}

		public PropertyNode FindNode(PropertyDef pd) {
			if (pd == null)
				return null;

			var typeNode = FindNode(pd.DeclaringType);
			if (typeNode == null)
				return null;

			typeNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in typeNode.TreeNode.DataChildren.OfType<PropertyNode>()) {
				if (n.PropertyDef == pd)
					return n;
			}

			return null;
		}

		public EventNode FindNode(EventDef ed) {
			if (ed == null)
				return null;

			var typeNode = FindNode(ed.DeclaringType);
			if (typeNode == null)
				return null;

			typeNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in typeNode.TreeNode.DataChildren.OfType<EventNode>()) {
				if (n.EventDef == ed)
					return n;
			}

			return null;
		}

		public IEnumerable<ModuleDocumentNode> GetAllModuleNodes() {
			foreach (var node in TopNodes) {
				if (node is ModuleDocumentNode modNode) {
					yield return modNode;
					continue;
				}

				if (node is AssemblyDocumentNode asmNode) {
					asmNode.TreeNode.EnsureChildrenLoaded();
					foreach (var c in asmNode.TreeNode.DataChildren) {
						modNode = c as ModuleDocumentNode;
						if (modNode != null)
							yield return modNode;
					}
					continue;
				}
			}
		}

		public IEnumerable<DsDocumentNode> GetAllCreatedDocumentNodes() {
			foreach (var n in GetAllCreatedDsDocumentNodes(TopNodes))
				yield return n;
		}

		IEnumerable<DsDocumentNode> GetAllCreatedDsDocumentNodes(IEnumerable<TreeNodeData> nodes) {
			foreach (var n in nodes) {
				if (n is DsDocumentNode fn) {
					yield return fn;
					// Don't call fn.TreeNode.EnsureChildrenLoaded(), only return created nodes
					foreach (var c in GetAllCreatedDsDocumentNodes(fn.TreeNode.DataChildren))
						yield return c;
				}
			}
		}

		public void AddNode(DsDocumentNode documentNode, int index) {
			if (documentNode == null)
				throw new ArgumentNullException(nameof(documentNode));
			Debug.Assert(!TreeView.Root.DataChildren.Contains(documentNode));
			Debug.Assert(documentNode.TreeNode.Parent == null);
			DocumentService.ForceAdd(documentNode.Document, false, new AddDocumentInfo(documentNode, index));
			Debug.Assert(TreeView.Root.DataChildren.Contains(documentNode));
		}

		sealed class AddDocumentInfo {
			public readonly DsDocumentNode DsDocumentNode;
			public readonly int Index;

			public AddDocumentInfo(DsDocumentNode documentNode, int index) {
				DsDocumentNode = documentNode;
				Index = index;
			}
		}

		void OnDropNodes(int index, int[] nodeIndexes) {
			if (!context.CanDragAndDrop)
				return;

			nodeIndexes = nodeIndexes.Distinct().ToArray();
			if (nodeIndexes.Length == 0)
				return;

			var children = TreeView.Root.Children;
			if ((uint)index > children.Count)
				return;

			var insertNode = index == children.Count ? null : children[index];

			var movedNodes = new List<ITreeNode>();
			Array.Sort(nodeIndexes, (a, b) => b.CompareTo(a));
			for (int i = 0; i < nodeIndexes.Length; i++) {
				var j = nodeIndexes[i];
				if ((uint)j >= children.Count)
					continue;
				movedNodes.Add(children[j]);
				children.RemoveAt(j);
			}
			movedNodes.Reverse();
			if (movedNodes.Count == 0)
				return;

			int insertIndex = children.IndexOf(insertNode);
			if (insertIndex < 0)
				insertIndex = children.Count;
			for (int i = 0; i < movedNodes.Count; i++)
				children.Insert(insertIndex + i, movedNodes[i]);

			TreeView.SelectItems(movedNodes.Select(a => a.Data));
		}

		void OnDropFiles(int index, string[] filenames) {
			if (!context.CanDragAndDrop)
				return;

			var origFilenames = filenames;
			var existingFiles = new HashSet<string>(DocumentService.GetDocuments().Select(a => a.Filename ?? string.Empty), StringComparer.OrdinalIgnoreCase);
			filenames = filenames.Where(a => File.Exists(a) && !existingFiles.Contains(a)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => Path.GetFileNameWithoutExtension(a), StringComparer.CurrentCultureIgnoreCase).ToArray();
			TreeNodeData newSelectedNode = null;
			for (int i = 0, j = 0; i < filenames.Length; i++) {
				var document = DocumentService.TryCreateOnly(DsDocumentInfo.CreateDocument(filenames[i]));
				if (document == null)
					continue;

				if (filenames.Length > 1) {
					switch (documentTreeViewSettings.FilterDraggedItems) {
					case DocumentFilterType.All:
						break;

					case DocumentFilterType.DotNetOnly:
						if (!(document is IDsDotNetDocument))
							continue;
						break;

					case DocumentFilterType.AllSupported:
						if (document is DsUnknownDocument)
							continue;
						break;

					default:
						Debug.Fail("Shouldn't be here");
						break;
					}
				}

				var node = CreateNode(null, document);
				DocumentService.ForceAdd(document, false, new AddDocumentInfo(node, index + j++));
				if (newSelectedNode == null)
					newSelectedNode = node;

				existingFiles.Add(document.Filename);
			}

			if (filenames.Any() && !filenames.Any(f => existingFiles.Contains(f)))
				MsgBox.Instance.Show(dnSpy_Resources.AssemblyExplorer_AllFilesFilteredOut);

			if (newSelectedNode == null) {
				var filename = origFilenames.FirstOrDefault(a => File.Exists(a));
				if (filename != null) {
					var key = new FilenameKey(filename);
					var document = DocumentService.GetDocuments().FirstOrDefault(a => key.Equals(a.Key));
					newSelectedNode = FindNode(document);
				}
			}
			if (newSelectedNode != null)
				TreeView.SelectItems(new[] { newSelectedNode });
		}

		DsDocumentNode[] GetNewSortedNodes() {
			var origOrder = TopNodes.ToArray();
			var documents = origOrder.Select((a, i) => (a, i)).ToList();
			documents.Sort(DsDocumentNodeComparer.Instance);
			var sorted = documents.Select(a => a.Item1).ToArray();
			if (Equals(sorted, origOrder))
				return null;
			return sorted;
		}

		public bool CanSortTopNodes => GetNewSortedNodes() != null;

		public void SortTopNodes() {
			var sortedDocuments = GetNewSortedNodes();
			if (sortedDocuments == null)
				return;

			var selectedNodes = TreeView.SelectedItems;
			var old = disable_SelectionChanged;
			try {
				disable_SelectionChanged = true;
				TreeView.Root.Children.Clear();
				foreach (var n in sortedDocuments)
					TreeView.Root.Children.Add(n.TreeNode);
				TreeView.SelectItems(selectedNodes);
			}
			finally {
				disable_SelectionChanged = old;
			}
		}

		static bool Equals(IList<DsDocumentNode> a, IList<DsDocumentNode> b) {
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		sealed class DsDocumentNodeComparer : IComparer<(DsDocumentNode, int)> {
			public static readonly DsDocumentNodeComparer Instance = new DsDocumentNodeComparer();

			public int Compare((DsDocumentNode, int) x, (DsDocumentNode, int) y) {
				if (x.Equals(y))
					return 0;
				int c = GetIsAutoLoadedOrder(x.Item1.Document.IsAutoLoaded).CompareTo(GetIsAutoLoadedOrder(y.Item1.Document.IsAutoLoaded));
				if (c != 0)
					return c;
				c = StringComparer.InvariantCultureIgnoreCase.Compare(x.ToString(), y.ToString());
				if (c != 0)
					return c;
				return x.Item2.CompareTo(y.Item2);
			}

			static int GetIsAutoLoadedOrder(bool b) => b ? 1 : 0;
		}
	}
}
