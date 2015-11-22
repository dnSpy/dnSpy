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
using System.Windows.Controls;
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.TreeView;
using dnSpy.MainApp;

namespace dnSpy.Files.TreeView {
	[Export, Export(typeof(IFileTreeView)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class FileTreeView : IFileTreeView, ITreeViewListener {
		readonly FileTreeNodeDataContext context;
		readonly IDnSpyFileNodeCreator[] dnSpyFileNodeCreators;

		public IFileManager FileManager {
			get { return fileManager; }
		}
		readonly IFileManager fileManager;

		public ITreeView TreeView {
			get { return treeView; }
		}
		readonly ITreeView treeView;

		public IDotNetImageManager DotNetImageManager {
			get { return dotNetImageManager; }
		}
		readonly IDotNetImageManager dotNetImageManager;

		public event EventHandler<NotifyFileTreeViewCollectionChangedEventArgs> CollectionChanged;

		void CallCollectionChanged(NotifyFileTreeViewCollectionChangedEventArgs eventArgs) {
			var c = CollectionChanged;
			if (c != null)
				c(this, eventArgs);
		}

		[ImportingConstructor]
		FileTreeView(IThemeManager themeManager, ITreeViewManager treeViewManager, ILanguageManager languageManager, IFileManager fileManager, AppSettingsImpl appSettings, IDotNetImageManager dotNetImageManager, [ImportMany] IDnSpyFileNodeCreator[] dnSpyFileNodeCreators) {
			var options = new TreeViewOptions {
				AllowDrop = true,
				IsVirtualizing = true,
				VirtualizationMode = VirtualizationMode.Recycling,
				TreeViewListener = this,
			};
			this.dnSpyFileNodeCreators = dnSpyFileNodeCreators.OrderBy(a => a.Order).ToArray();
			this.treeView = treeViewManager.Create(new Guid(TVConstants.FILE_TREEVIEW_GUID), options);
			this.fileManager = fileManager;
			this.dotNetImageManager = dotNetImageManager;
			var dispatcher = treeView.UIObject.Dispatcher;
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
			this.context = new FileTreeNodeDataContext(this);

			this.context.SyntaxHighlight = appSettings.SyntaxHighlightFileTreeView;
			this.context.SingleClickExpandsChildren = appSettings.SingleClickExpandsTreeViewChildren;
			this.context.ShowAssemblyVersion = appSettings.ShowAssemblyVersion;
			this.context.ShowAssemblyPublicKeyToken = appSettings.ShowAssemblyPublicKeyToken;
			this.context.ShowToken = appSettings.ShowToken;
			this.context.Language = languageManager.SelectedLanguage;
			languageManager.LanguageChanged += LanguageManager_LanguageChanged;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			appSettings.PropertyChanged += AppSettings_PropertyChanged;
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

		internal void OnTextFormatterChanged() {
			RefreshNodes();
		}

		void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var appSettings = (AppSettingsImpl)sender;
			switch (e.PropertyName) {
			case "SyntaxHighlightFileTreeView":
				context.SyntaxHighlight = appSettings.SyntaxHighlightFileTreeView;
				RefreshNodes();
				break;

			case "ShowAssemblyVersion":
				context.ShowAssemblyVersion = appSettings.ShowAssemblyVersion;
				RefreshNodes();
				NotifyNodesTextRefreshed();
				break;

			case "ShowAssemblyPublicKeyToken":
				context.ShowAssemblyPublicKeyToken = appSettings.ShowAssemblyPublicKeyToken;
				RefreshNodes();
				NotifyNodesTextRefreshed();
				break;

			case "ShowToken":
				context.ShowToken = appSettings.ShowToken;
				RefreshNodes();
				NotifyNodesTextRefreshed();
				break;

			case "SingleClickExpandsTreeViewChildren":
				context.SingleClickExpandsChildren = appSettings.SingleClickExpandsTreeViewChildren;
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
				CallCollectionChanged(NotifyFileTreeViewCollectionChangedEventArgs.CreateRemove(node));
				break;

			case NotifyFileCollectionType.Clear:
				var oldNodes = treeView.Root.Children.Select(a => (IDnSpyFileNode)a.Data).ToArray();
				treeView.Root.Children.Clear();
				CallCollectionChanged(NotifyFileTreeViewCollectionChangedEventArgs.CreateClear(oldNodes));
				break;

			default:
				Debug.Fail(string.Format("Unknown event type: {0}", e.Type));
				break;
			}
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
			return (IAssemblyReferenceNode)TreeView.Create(new AssemblyReferenceNode(TreeNodeGroups.AssemblyRefTreeNodeGroupReferences, ownerModule, asmRef)).Data;
		}

		public IModuleReferenceNode Create(ModuleRef modRef) {
			return (IModuleReferenceNode)TreeView.Create(new ModuleReferenceNode(TreeNodeGroups.ModuleRefTreeNodeGroupReferences, modRef)).Data;
		}

		public IMethodNode CreateEvent(MethodDef method) {
			return (IMethodNode)TreeView.Create(new MethodNode(TreeNodeGroups.MethodTreeNodeGroupEvent, method)).Data;
		}

		public IMethodNode CreateProperty(MethodDef method) {
			return (IMethodNode)TreeView.Create(new MethodNode(TreeNodeGroups.MethodTreeNodeGroupProperty, method)).Data;
		}

		public INamespaceNode Create(string name) {
			return (INamespaceNode)TreeView.Create(new NamespaceNode(TreeNodeGroups.NamespaceTreeNodeGroupModule, name, new List<TypeDef>())).Data;
		}

		public ITypeNode Create(TypeDef type) {
			return (ITypeNode)TreeView.Create(new TypeNode(TreeNodeGroups.TypeTreeNodeGroupNamespace, type)).Data;
		}

		public ITypeNode CreateNested(TypeDef type) {
			return (ITypeNode)TreeView.Create(new TypeNode(TreeNodeGroups.TypeTreeNodeGroupType, type)).Data;
		}

		public IMethodNode Create(MethodDef method) {
			return (IMethodNode)TreeView.Create(new MethodNode(TreeNodeGroups.MethodTreeNodeGroupType, method)).Data;
		}

		public IPropertyNode Create(PropertyDef property) {
			return (IPropertyNode)TreeView.Create(new PropertyNode(TreeNodeGroups.PropertyTreeNodeGroupType, property)).Data;
		}

		public IEventNode Create(EventDef @event) {
			return (IEventNode)TreeView.Create(new EventNode(TreeNodeGroups.EventTreeNodeGroupType, @event)).Data;
		}

		public IFieldNode Create(FieldDef field) {
			return (IFieldNode)TreeView.Create(new FieldNode(TreeNodeGroups.FieldTreeNodeGroupType, field)).Data;
		}
	}
}
