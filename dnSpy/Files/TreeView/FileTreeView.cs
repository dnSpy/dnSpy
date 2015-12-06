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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.TreeView;
using ICSharpCode.Decompiler;

namespace dnSpy.Files.TreeView {
	[Export, Export(typeof(IFileTreeView)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class FileTreeView : IFileTreeView, ITreeViewListener {
		readonly FileTreeNodeDataContext context;
		readonly IDnSpyFileNodeCreator[] dnSpyFileNodeCreators;
		readonly Lazy<IFileTreeNodeDataFinder, IFileTreeNodeDataFinderMetadata>[] nodeFinders;

		public IFileManager FileManager {
			get { return fileManager; }
		}
		readonly IFileManager fileManager;

		public ITreeView TreeView {
			get { return treeView; }
		}
		readonly ITreeView treeView;

		public IFileTreeNodeGroups FileTreeNodeGroups {
			get { return fileTreeNodeGroups; }
		}
		readonly FileTreeNodeGroups fileTreeNodeGroups;

		IEnumerable<IDnSpyFileNode> TopNodes {
			get { return treeView.Root.Children.Select(a => (IDnSpyFileNode)a.Data); }
		}

		public IDotNetImageManager DotNetImageManager {
			get { return dotNetImageManager; }
		}
		readonly IDotNetImageManager dotNetImageManager;

		public IWpfCommands WpfCommands {
			get { return wpfCommands; }
		}
		readonly IWpfCommands wpfCommands;

		public event EventHandler<NotifyFileTreeViewCollectionChangedEventArgs> CollectionChanged;

		void CallCollectionChanged(NotifyFileTreeViewCollectionChangedEventArgs eventArgs) {
			var c = CollectionChanged;
			if (c != null)
				c(this, eventArgs);
		}

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly ITreeView treeView;

			public GuidObjectsCreator(ITreeView treeView) {
				this.treeView = treeView;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_TREEVIEW_NODES_ARRAY_GUID, treeView.TopLevelSelection);
			}
		}

		[ImportingConstructor]
		FileTreeView(IThemeManager themeManager, ITreeViewManager treeViewManager, ILanguageManager languageManager, IFileManager fileManager, IFileTreeViewSettings fileTreeViewSettings, IMenuManager menuManager, IDotNetImageManager dotNetImageManager, IWpfCommandManager wpfCommandManager, DecompilerSettings decompilerSettings, IResourceNodeFactory resourceNodeFactory, IAppSettings appSettings, [ImportMany] IDnSpyFileNodeCreator[] dnSpyFileNodeCreators, [ImportMany] IEnumerable<Lazy<IFileTreeNodeDataFinder, IFileTreeNodeDataFinderMetadata>> mefFinders) {
			var options = new TreeViewOptions {
				AllowDrop = true,
				IsVirtualizing = true,
				VirtualizationMode = VirtualizationMode.Recycling,
				TreeViewListener = this,
			};
			this.fileTreeNodeGroups = new FileTreeNodeGroups();
			this.dnSpyFileNodeCreators = dnSpyFileNodeCreators.OrderBy(a => a.Order).ToArray();
			this.treeView = treeViewManager.Create(new Guid(TVConstants.FILE_TREEVIEW_GUID), options);
			menuManager.InitializeContextMenu((FrameworkElement)this.treeView.UIObject, new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID), new GuidObjectsCreator(this.treeView));
			this.fileManager = fileManager;
			this.dotNetImageManager = dotNetImageManager;
			var dispatcher = Dispatcher.CurrentDispatcher;
			this.fileManager.SetDispatcher(a => {
				if (!dispatcher.HasShutdownFinished && !dispatcher.HasShutdownStarted) {
					bool callInvoke;
					lock (actionsToCall) {
						actionsToCall.Add(a);
						callInvoke = actionsToCall.Count == 1;
					}
					if (callInvoke) {
						// Always notify with a delay because adding stuff to the tree view could
						// cause some problems with the tree view or the list box it derives from.
						dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(CallActions));
					}
				}
			});
			this.fileManager.CollectionChanged += FileManager_CollectionChanged;
			this.context = new FileTreeNodeDataContext(this, resourceNodeFactory);
			this.context.SyntaxHighlight = fileTreeViewSettings.SyntaxHighlight;
			this.context.SingleClickExpandsChildren = fileTreeViewSettings.SingleClickExpandsTreeViewChildren;
			this.context.ShowAssemblyVersion = fileTreeViewSettings.ShowAssemblyVersion;
			this.context.ShowAssemblyPublicKeyToken = fileTreeViewSettings.ShowAssemblyPublicKeyToken;
			this.context.ShowToken = fileTreeViewSettings.ShowToken;
			this.context.Language = languageManager.SelectedLanguage;
			this.context.UseNewRenderer = appSettings.UseNewRenderer_FileTreeView;
			this.context.DeserializeResources = fileTreeViewSettings.DeserializeResources;
			languageManager.LanguageChanged += LanguageManager_LanguageChanged;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			fileTreeViewSettings.PropertyChanged += FileTreeViewSettings_PropertyChanged;
			appSettings.PropertyChanged += AppSettings_PropertyChanged;

			wpfCommandManager.Add(CommandConstants.GUID_FILE_TREEVIEW, (UIElement)treeView.UIObject);
			this.wpfCommands = wpfCommandManager.GetCommands(CommandConstants.GUID_FILE_TREEVIEW);

			this.nodeFinders = mefFinders.OrderBy(a => a.Metadata.Order).ToArray();
			InitializeFileTreeNodeGroups(decompilerSettings);
		}

		void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var appSettings = (IAppSettings)sender;
			if (e.PropertyName == "UseNewRenderer_FileTreeView") {
				this.context.UseNewRenderer = appSettings.UseNewRenderer_FileTreeView;
				RefreshNodes();
			}
		}

		void InitializeFileTreeNodeGroups(DecompilerSettings decompilerSettings) {
			MemberType[] orders;
			try {
				orders = new MemberType[] {
					ToMemberType(decompilerSettings.DecompilationObject0),
					ToMemberType(decompilerSettings.DecompilationObject1),
					ToMemberType(decompilerSettings.DecompilationObject2),
					ToMemberType(decompilerSettings.DecompilationObject3),
					ToMemberType(decompilerSettings.DecompilationObject4),
				};
			}
			catch (InvalidOperationException) {
				return;
			}
			fileTreeNodeGroups.SetMemberOrder(orders);
		}

		static MemberType ToMemberType(DecompilationObject o) {
			switch (o) {
			case DecompilationObject.NestedTypes: return MemberType.NestedTypes;
			case DecompilationObject.Fields: return MemberType.Fields;
			case DecompilationObject.Events: return MemberType.Events;
			case DecompilationObject.Properties: return MemberType.Properties;
			case DecompilationObject.Methods: return MemberType.Methods;
			}
			throw new InvalidOperationException();
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

		void FileTreeViewSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var fileTreeViewSettings = (IFileTreeViewSettings)sender;
			switch (e.PropertyName) {
			case "SyntaxHighlight":
				context.SyntaxHighlight = fileTreeViewSettings.SyntaxHighlight;
				RefreshNodes();
				break;

			case "ShowAssemblyVersion":
				context.ShowAssemblyVersion = fileTreeViewSettings.ShowAssemblyVersion;
				RefreshNodes();
				NotifyNodesTextRefreshed();
				break;

			case "ShowAssemblyPublicKeyToken":
				context.ShowAssemblyPublicKeyToken = fileTreeViewSettings.ShowAssemblyPublicKeyToken;
				RefreshNodes();
				NotifyNodesTextRefreshed();
				break;

			case "ShowToken":
				context.ShowToken = fileTreeViewSettings.ShowToken;
				RefreshNodes();
				NotifyNodesTextRefreshed();
				break;

			case "SingleClickExpandsTreeViewChildren":
				context.SingleClickExpandsChildren = fileTreeViewSettings.SingleClickExpandsTreeViewChildren;
				break;

			case "DeserializeResources":
				context.DeserializeResources = fileTreeViewSettings.DeserializeResources;
				break;

			default:
				break;
			}
		}

		public event EventHandler<EventArgs> NodesTextChanged;

		void NotifyNodesTextRefreshed() {
			if (NodesTextChanged != null)
				NodesTextChanged(this, EventArgs.Empty);
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			RefreshNodes();
		}

		void LanguageManager_LanguageChanged(object sender, EventArgs e) {
			this.context.Language = ((ILanguageManager)sender).SelectedLanguage;
			RefreshNodes();
			NotifyNodesTextRefreshed();
		}

		public void RefreshNodes(bool showMember, bool decompilationOrder) {
			if (showMember)
				RefreshNodes();
			/*TODO: decompilationOrder
			Should call InitializeFileTreeNodeGroups(). Some stuff that must be fixed:
			The asm editor has some classes that store indexes of nodes, and would need to be
			updated to just use the normal AddChild() method to restore the node.
			Also, when the asm editor reinserts a node, its children (recursively) must be resorted
			if the sort order has changed.
			*/
		}

		void RefreshNodes() {
			//TODO: Should only call the method if the node is visible
			foreach (var node in this.treeView.Root.Descendants())
				node.RefreshUI();
		}

		void FileManager_CollectionChanged(object sender, NotifyFileCollectionChangedEventArgs e) {
			switch (e.Type) {
			case NotifyFileCollectionType.Add:
				var newNode = CreateNode(null, e.Files[0]);
				treeView.Root.Children.Add(treeView.Create(newNode));
				CallCollectionChanged(NotifyFileTreeViewCollectionChangedEventArgs.CreateAdd(newNode));
				break;

			case NotifyFileCollectionType.Remove:
				int index = -1;
				foreach (var child in treeView.Root.Children) {
					index++;
					if (((IDnSpyFileNode)child.Data).DnSpyFile == e.Files[0])
						break;
				}
				bool b = (uint)index < (uint)treeView.Root.Children.Count;
				Debug.Assert(b);
				if (!b)
					break;
				var node = (IDnSpyFileNode)treeView.Root.Children[index];
				DisableMemoryMappedIO(new[] { node });
				CallCollectionChanged(NotifyFileTreeViewCollectionChangedEventArgs.CreateRemove(node));
				break;

			case NotifyFileCollectionType.Clear:
				var oldNodes = treeView.Root.Children.Select(a => (IDnSpyFileNode)a.Data).ToArray();
				treeView.Root.Children.Clear();
				DisableMemoryMappedIO(oldNodes);
				CallCollectionChanged(NotifyFileTreeViewCollectionChangedEventArgs.CreateClear(oldNodes));
				break;

			default:
				Debug.Fail(string.Format("Unknown event type: {0}", e.Type));
				break;
			}
		}

		void DisableMemoryMappedIO(IDnSpyFileNode[] nodes) {
			// The nodes will be GC'd eventually, but it's not safe to call Dispose(), so disable
			// mmap'd I/O so the files can at least be modified (eg. deleted) by the user.
			foreach (var m in nodes.SelectMany(n => n.DnSpyFile.GetModules<ModuleDefMD>()))
				m.MetaData.PEImage.UnsafeDisableMemoryMappedIO();
		}

		public IDnSpyFileNode CreateNode(IDnSpyFileNode owner, IDnSpyFile file) {
			foreach (var creator in dnSpyFileNodeCreators) {
				var result = creator.Create(this, owner, file);
				if (result != null)
					return result;
			}

			return new UnknownFileNode(file);
		}

		void ITreeViewListener.OnEvent(ITreeView treeView, TreeViewListenerEventArgs e) {
			if (e.Event == TreeViewListenerEvent.NodeCreated) {
				var node = (ITreeNode)e.Argument;
				var d = node.Data as IFileTreeNodeData;
				if (d != null)
					d.Context = context;
				return;
			}
		}

		public IAssemblyReferenceNode Create(AssemblyRef asmRef, ModuleDef ownerModule) {
			return (IAssemblyReferenceNode)TreeView.Create(new AssemblyReferenceNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.AssemblyRefTreeNodeGroupReferences), ownerModule, asmRef)).Data;
		}

		public IModuleReferenceNode Create(ModuleRef modRef) {
			return (IModuleReferenceNode)TreeView.Create(new ModuleReferenceNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.ModuleRefTreeNodeGroupReferences), modRef)).Data;
		}

		public IMethodNode CreateEvent(MethodDef method) {
			return (IMethodNode)TreeView.Create(new MethodNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupEvent), method)).Data;
		}

		public IMethodNode CreateProperty(MethodDef method) {
			return (IMethodNode)TreeView.Create(new MethodNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupProperty), method)).Data;
		}

		public INamespaceNode Create(string name) {
			return (INamespaceNode)TreeView.Create(new NamespaceNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.NamespaceTreeNodeGroupModule), name, new List<TypeDef>())).Data;
		}

		public ITypeNode Create(TypeDef type) {
			return (ITypeNode)TreeView.Create(new TypeNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.TypeTreeNodeGroupNamespace), type)).Data;
		}

		public ITypeNode CreateNested(TypeDef type) {
			return (ITypeNode)TreeView.Create(new TypeNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.TypeTreeNodeGroupType), type)).Data;
		}

		public IMethodNode Create(MethodDef method) {
			return (IMethodNode)TreeView.Create(new MethodNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupType), method)).Data;
		}

		public IPropertyNode Create(PropertyDef property) {
			return (IPropertyNode)TreeView.Create(new PropertyNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.PropertyTreeNodeGroupType), property)).Data;
		}

		public IEventNode Create(EventDef @event) {
			return (IEventNode)TreeView.Create(new EventNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.EventTreeNodeGroupType), @event)).Data;
		}

		public IFieldNode Create(FieldDef field) {
			return (IFieldNode)TreeView.Create(new FieldNode(FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.FieldTreeNodeGroupType), field)).Data;
		}

		public IFileTreeNodeData FindNode(object @ref) {
			if (@ref == null)
				return null;
			if (@ref is IFileTreeNodeData)
				return (IFileTreeNodeData)@ref;
			if (@ref is IDnSpyFile)
				return FindNode((IDnSpyFile)@ref);
			if (@ref is AssemblyDef)
				return FindNode((AssemblyDef)@ref);
			if (@ref is ModuleDef)
				return FindNode((ModuleDef)@ref);
			if (@ref is TypeDef)
				return FindNode((TypeDef)@ref);
			if (@ref is MethodDef)
				return FindNode((MethodDef)@ref);
			if (@ref is FieldDef)
				return FindNode((FieldDef)@ref);
			if (@ref is PropertyDef)
				return FindNode((PropertyDef)@ref);
			if (@ref is EventDef)
				return FindNode((EventDef)@ref);

			foreach (var finder in nodeFinders) {
				var node = finder.Value.FindNode(this, @ref);
				if (node != null)
					return node;
			}

			return null;
		}

		public IDnSpyFileNode FindNode(IDnSpyFile file) {
			if (file == null)
				return null;
			return Find(TopNodes, file);
		}

		IDnSpyFileNode Find(IEnumerable<IDnSpyFileNode> nodes, IDnSpyFile file) {
			foreach (var n in nodes) {
				if (n.DnSpyFile == file)
					return n;
				if (n.DnSpyFile.Children.Count == 0)
					continue;
				n.TreeNode.EnsureChildrenLoaded();
				var found = Find(n.TreeNode.DataChildren.OfType<IDnSpyFileNode>(), file);
				if (found != null)
					return found;
			}
			return null;
		}

		public IAssemblyFileNode FindNode(AssemblyDef asm) {
			if (asm == null)
				return null;

			foreach (var n in TopNodes.OfType<IAssemblyFileNode>()) {
				if (n.DnSpyFile.AssemblyDef == asm)
					return n;
			}

			return null;
		}

		public IModuleFileNode FindNode(ModuleDef mod) {
			if (mod == null)
				return null;

			foreach (var n in TopNodes.OfType<IAssemblyFileNode>()) {
				n.TreeNode.EnsureChildrenLoaded();
				foreach (var m in n.TreeNode.DataChildren.OfType<IModuleFileNode>()) {
					if (m.DnSpyFile.ModuleDef == mod)
						return m;
				}
			}

			// Check for netmodules
			foreach (var n in TopNodes.OfType<IModuleFileNode>()) {
				if (n.DnSpyFile.ModuleDef == mod)
					return n;
			}

			return null;
		}

		public ITypeNode FindNode(TypeDef td) {
			if (td == null)
				return null;

			var types = new List<TypeDef>();
			for (var t = td; t != null; t = t.DeclaringType)
				types.Add(t);
			types.Reverse();

			var modNode = FindNode(types[0].Module);
			if (modNode == null)
				return null;

			var nsNode = FindNamespaceNode(modNode, types[0].Namespace);
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

		ITypeNode FindNode(INamespaceNode nsNode, TypeDef type) {
			if (nsNode == null || type == null)
				return null;

			nsNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in nsNode.TreeNode.DataChildren.OfType<ITypeNode>()) {
				if (n.TypeDef == type)
					return n;
			}

			return null;
		}

		ITypeNode FindNode(ITypeNode typeNode, TypeDef type) {
			if (typeNode == null || type == null)
				return null;

			typeNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in typeNode.TreeNode.DataChildren.OfType<ITypeNode>()) {
				if (n.TypeDef == type)
					return n;
			}

			return null;
		}

		INamespaceNode FindNamespaceNode(IModuleFileNode modNode, string ns) {
			if (ns == null)
				return null;

			modNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in modNode.TreeNode.DataChildren.OfType<INamespaceNode>()) {
				if (n.Name == ns)
					return n;
			}

			return null;
		}

		public IMethodNode FindNode(MethodDef md) {
			if (md == null)
				return null;

			var typeNode = FindNode(md.DeclaringType);
			if (typeNode == null)
				return null;

			typeNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in typeNode.TreeNode.DataChildren.OfType<IMethodNode>()) {
				if (n.MethodDef == md)
					return n;
			}

			foreach (var n in typeNode.TreeNode.DataChildren.OfType<IPropertyNode>()) {
				n.TreeNode.EnsureChildrenLoaded();
				foreach (var m in n.TreeNode.DataChildren.OfType<IMethodNode>()) {
					if (m.MethodDef == md)
						return m;
				}
			}

			foreach (var n in typeNode.TreeNode.DataChildren.OfType<IEventNode>()) {
				n.TreeNode.EnsureChildrenLoaded();
				foreach (var m in n.TreeNode.DataChildren.OfType<IMethodNode>()) {
					if (m.MethodDef == md)
						return m;
				}
			}

			return null;
		}

		public IFieldNode FindNode(FieldDef fd) {
			if (fd == null)
				return null;

			var typeNode = FindNode(fd.DeclaringType);
			if (typeNode == null)
				return null;

			typeNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in typeNode.TreeNode.DataChildren.OfType<IFieldNode>()) {
				if (n.FieldDef == fd)
					return n;
			}

			return null;
		}

		public IPropertyNode FindNode(PropertyDef pd) {
			if (pd == null)
				return null;

			var typeNode = FindNode(pd.DeclaringType);
			if (typeNode == null)
				return null;

			typeNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in typeNode.TreeNode.DataChildren.OfType<IPropertyNode>()) {
				if (n.PropertyDef == pd)
					return n;
			}

			return null;
		}

		public IEventNode FindNode(EventDef ed) {
			if (ed == null)
				return null;

			var typeNode = FindNode(ed.DeclaringType);
			if (typeNode == null)
				return null;

			typeNode.TreeNode.EnsureChildrenLoaded();
			foreach (var n in typeNode.TreeNode.DataChildren.OfType<IEventNode>()) {
				if (n.EventDef == ed)
					return n;
			}

			return null;
		}
	}
}
