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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Analyzer.TreeNodes;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Analyzer {
	interface IAnalyzerManager {
		/// <summary>
		/// Gets the <see cref="ITreeView"/> instance
		/// </summary>
		ITreeView TreeView { get; }

		/// <summary>
		/// Called when it's been closed
		/// </summary>
		void OnClose();

		/// <summary>
		/// Adds <paramref name="node"/> if it hasn't been added and gives it focus.
		/// </summary>
		/// <param name="node">Node</param>
		void Add(IAnalyzerTreeNodeData node);

		/// <summary>
		/// Called by <paramref name="node"/> when its <see cref="ITreeNodeData.Activate()"/> method
		/// has been called.
		/// </summary>
		/// <param name="node">Activated node (it's the caller)</param>
		void OnActivated(IAnalyzerTreeNodeData node);

		/// <summary>
		/// Follows the reference
		/// </summary>
		/// <param name="node">Node</param>
		/// <param name="newTab">true to show it in a new tab</param>
		/// <param name="useCodeRef">true to show the reference in a method body</param>
		void FollowNode(ITreeNodeData node, bool newTab, bool? useCodeRef);

		/// <summary>
		/// Returns true if <see cref="FollowNode(ITreeNodeData, bool, bool?)"/> can execute
		/// </summary>
		/// <param name="node">Node</param>
		/// <param name="useCodeRef">true to show the reference in a method body</param>
		/// <returns></returns>
		bool CanFollowNode(ITreeNodeData node, bool useCodeRef);
	}

	[Export, Export(typeof(IAnalyzerManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class AnalyzerManager : IAnalyzerManager, ITreeViewListener {
		static readonly Guid ANALYZER_TREEVIEW_GUID = new Guid("8981898A-1384-4B67-9577-3CB096195146");

		public ITreeView TreeView {
			get { return treeView; }
		}
		readonly ITreeView treeView;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly ITreeView treeView;

			public GuidObjectsCreator(ITreeView treeView) {
				this.treeView = treeView;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_TREEVIEW_NODES_ARRAY_GUID, treeView.TopLevelSelection);
			}
		}

		readonly AnalyzerTreeNodeDataContext context;
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		AnalyzerManager(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, ITreeViewManager treeViewManager, IMenuManager menuManager, IThemeManager themeManager, IAnalyzerSettings analyzerSettings, IDotNetImageManager dotNetImageManager, ILanguageManager languageManager, IFileManager fileManager) {
			this.fileTabManager = fileTabManager;

			this.context = new AnalyzerTreeNodeDataContext {
				DotNetImageManager = dotNetImageManager,
				Language = languageManager.SelectedLanguage,
				FileManager = fileManager,
				ShowToken = analyzerSettings.ShowToken,
				SingleClickExpandsChildren = analyzerSettings.SingleClickExpandsChildren,
				SyntaxHighlight = analyzerSettings.SyntaxHighlight,
				UseNewRenderer = analyzerSettings.UseNewRenderer,
				AnalyzerManager = this,
			};

			var options = new TreeViewOptions {
				CanDragAndDrop = false,
				TreeViewListener = this,
			};
			this.treeView = treeViewManager.Create(ANALYZER_TREEVIEW_GUID, options);

			fileManager.CollectionChanged += FileManager_CollectionChanged;
			fileTabManager.FileModified += FileTabManager_FileModified;
			languageManager.LanguageChanged += LanguageManager_LanguageChanged;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			analyzerSettings.PropertyChanged += AnalyzerSettings_PropertyChanged;

			menuManager.InitializeContextMenu((FrameworkElement)this.treeView.UIObject, new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID), new GuidObjectsCreator(this.treeView));
			wpfCommandManager.Add(CommandConstants.GUID_ANALYZER_TREEVIEW, (UIElement)this.treeView.UIObject);
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_ANALYZER_TREEVIEW);
			var command = new RelayCommand(a => ActivateNode());
			cmds.Add(command, ModifierKeys.Control, Key.Enter);
			cmds.Add(command, ModifierKeys.Shift, Key.Enter);
		}

		void FileTabManager_FileModified(object sender, FileModifiedEventArgs e) {
			AnalyzerTreeNodeData.HandleModelUpdated(treeView.Root, e.Files);
			RefreshNodes();
		}

		void ActivateNode() {
			var nodes = treeView.TopLevelSelection;
			var node = nodes.Length == 0 ? null : nodes[0] as ITreeNodeData;
			if (node != null)
				node.Activate();
		}

		void FileManager_CollectionChanged(object sender, NotifyFileCollectionChangedEventArgs e) {
			switch (e.Type) {
			case NotifyFileCollectionType.Clear:
				ClearAll();
				break;

			case NotifyFileCollectionType.Add:
				AnalyzerTreeNodeData.HandleAssemblyListChanged(treeView.Root, new IDnSpyFile[0], e.Files);
				break;

			case NotifyFileCollectionType.Remove:
				AnalyzerTreeNodeData.HandleAssemblyListChanged(treeView.Root, e.Files, new IDnSpyFile[0]);
				break;

			default:
				break;
			}
		}

		void LanguageManager_LanguageChanged(object sender, EventArgs e) {
			this.context.Language = ((ILanguageManager)sender).SelectedLanguage;
			RefreshNodes();
		}

		void AnalyzerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var analyzerSettings = (IAnalyzerSettings)sender;
			switch (e.PropertyName) {
			case "ShowToken":
				context.ShowToken = analyzerSettings.ShowToken;
				RefreshNodes();
				break;

			case "SyntaxHighlight":
				context.SyntaxHighlight = analyzerSettings.SyntaxHighlight;
				RefreshNodes();
				break;

			case "UseNewRenderer":
				context.UseNewRenderer = analyzerSettings.UseNewRenderer;
				RefreshNodes();
				break;
			}
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			RefreshNodes();
		}

		void RefreshNodes() {
			this.treeView.RefreshAllNodes();
		}

		void ITreeViewListener.OnEvent(ITreeView treeView, TreeViewListenerEventArgs e) {
			if (e.Event == TreeViewListenerEvent.NodeCreated) {
				Debug.Assert(context != null);
				var node = (ITreeNode)e.Argument;
				var d = node.Data as IAnalyzerTreeNodeData;
				if (d != null)
					d.Context = context;
				return;
			}
		}

		void Cancel() {
			AnalyzerTreeNodeData.CancelSelfAndChildren(treeView.Root.Data);
		}

		public void OnClose() {
			ClearAll();
		}

		void ClearAll() {
			Cancel();
			this.treeView.Root.Children.Clear();
		}

		public void Add(IAnalyzerTreeNodeData node) {
			if (node is EntityNode) {
				var an = node as EntityNode;
				var found = this.treeView.Root.DataChildren.OfType<EntityNode>().FirstOrDefault(n => n.Member == an.Member);
				if (found != null) {
					found.TreeNode.IsExpanded = true;
					this.treeView.SelectItems(new ITreeNodeData[] { found });
					this.treeView.Focus();
					return;
				}
			}
			this.treeView.Root.Children.Add(this.treeView.Create(node));
			node.TreeNode.IsExpanded = true;
			this.treeView.SelectItems(new ITreeNodeData[] { node });
			this.treeView.Focus();
		}

		public void OnActivated(IAnalyzerTreeNodeData node) {
			if (node == null)
				throw new ArgumentNullException();
			bool newTab = Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift;
			FollowNode(node, newTab, null);
		}

		public void FollowNode(ITreeNodeData node, bool newTab, bool? useCodeRef) {
			var tokNode = node as IMDTokenNode;
			var @ref = tokNode == null ? null : tokNode.Reference;

			var entityNode = node as EntityNode;
			var srcRef = entityNode == null ? null : entityNode.SourceRef;

			bool code = useCodeRef ?? srcRef != null;
			if (code) {
				if (srcRef == null)
					return;
				fileTabManager.FollowReference(srcRef.Value.Method, newTab, true, a => {
					if (!a.HasMovedCaret && a.Success && srcRef != null)
						a.HasMovedCaret = GoTo(a.Tab, srcRef.Value.Method, srcRef.Value.ILOffset, srcRef.Value.Reference);
				});
			}
			else {
				if (@ref == null)
					return;
				fileTabManager.FollowReference(@ref, newTab);
			}
		}

		public bool CanFollowNode(ITreeNodeData node, bool useCodeRef) {
			var tokNode = node as IMDTokenNode;
			var @ref = tokNode == null ? null : tokNode.Reference;

			var entityNode = node as EntityNode;
			var srcRef = entityNode == null ? null : entityNode.SourceRef;

			if (useCodeRef)
				return srcRef != null;
			return @ref != null;
		}

		bool GoTo(IFileTab tab, MethodDef method, uint? ilOffset, object @ref) {
			if (method == null || ilOffset == null)
				return false;
			var uiContext = tab.TryGetTextEditorUIContext();
			if (uiContext == null)
				return false;
			var cm = uiContext.GetCodeMappings();
			var mapping = cm.Find(method, ilOffset.Value);
			if (mapping == null)
				return false;

			var location = mapping.StartPosition;
			var loc = FindLocation(uiContext.GetCodeReferences(location.Line, location.Column), mapping.EndPosition, @ref);
			if (loc == null)
				loc = new TextEditorLocation(location.Line, location.Column);

			uiContext.ScrollAndMoveCaretTo(loc.Value.Line, loc.Value.Column);
			return true;
		}

		TextEditorLocation? FindLocation(IEnumerable<Tuple<CodeReference, TextEditorLocation>> infos, TextPosition endLoc, object @ref) {
			foreach (var info in infos) {
				int c = Compare(info.Item2, endLoc);
				if (c > 0)
					break;
				if (RefEquals(@ref, info.Item1.Reference))
					return info.Item2;
			}
			return null;
		}

		static int Compare(TextEditorLocation a, TextPosition b) {
			if (a.Line > b.Line)
				return 1;
			if (a.Line == b.Line)
				return a.Column.CompareTo(b.Column);
			return -1;
		}

		static bool RefEquals(object a, object b) {
			if (Equals(a, b))
				return true;
			if (Equals(a, null) || Equals(b, null))
				return false;

			{
				var pb = b as PropertyDef;
				if (pb != null) {
					var tmp = a;
					a = b;
					b = tmp;
				}
				var eb = b as EventDef;
				if (eb != null) {
					var tmp = a;
					a = b;
					b = tmp;
				}
			}

			const SigComparerOptions flags = SigComparerOptions.CompareDeclaringTypes | SigComparerOptions.PrivateScopeIsComparable;

			var type = a as IType;
			if (type != null)
				return new SigComparer().Equals(type, b as IType);

			var method = a as IMethod;
			if (method != null && method.IsMethod)
				return new SigComparer(flags).Equals(method, b as IMethod);

			var field = a as IField;
			if (field != null && field.IsField)
				return new SigComparer(flags).Equals(field, b as IField);

			var prop = a as PropertyDef;
			if (prop != null) {
				if (new SigComparer(flags).Equals(prop, b as PropertyDef))
					return true;
				var bm = b as IMethod;
				return bm != null &&
					(new SigComparer(flags).Equals(prop.GetMethod, bm) ||
					new SigComparer(flags).Equals(prop.SetMethod, bm));
			}

			var evt = a as EventDef;
			if (evt != null) {
				if (new SigComparer(flags).Equals(evt, b as EventDef))
					return true;
				var bm = b as IMethod;
				return bm != null &&
					(new SigComparer(flags).Equals(evt.AddMethod, bm) ||
					new SigComparer(flags).Equals(evt.InvokeMethod, bm) ||
					new SigComparer(flags).Equals(evt.RemoveMethod, bm));
			}

			Debug.Fail("Shouldn't be here");
			return false;
		}
	}
}
