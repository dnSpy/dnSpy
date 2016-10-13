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
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Analyzer.TreeNodes;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.TreeView;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Analyzer {
	interface IAnalyzerService {
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

	[Export(typeof(IAnalyzerService))]
	sealed class AnalyzerService : IAnalyzerService, ITreeViewListener {
		static readonly Guid ANALYZER_TREEVIEW_GUID = new Guid("8981898A-1384-4B67-9577-3CB096195146");

		public ITreeView TreeView { get; }

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly ITreeView treeView;

			public GuidObjectsProvider(ITreeView treeView) {
				this.treeView = treeView;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_TREEVIEW_NODES_ARRAY_GUID, treeView.TopLevelSelection);
			}
		}

		readonly AnalyzerTreeNodeDataContext context;
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		AnalyzerService(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, ITreeViewService treeViewService, IMenuService menuService, IThemeService themeService, IAnalyzerSettings analyzerSettings, IDotNetImageService dotNetImageService, IDecompilerService decompilerService) {
			this.documentTabService = documentTabService;

			this.context = new AnalyzerTreeNodeDataContext {
				DotNetImageService = dotNetImageService,
				Decompiler = decompilerService.Decompiler,
				DocumentService = documentTabService.DocumentTreeView.DocumentService,
				ShowToken = analyzerSettings.ShowToken,
				SingleClickExpandsChildren = analyzerSettings.SingleClickExpandsChildren,
				SyntaxHighlight = analyzerSettings.SyntaxHighlight,
				UseNewRenderer = analyzerSettings.UseNewRenderer,
				AnalyzerService = this,
			};

			var options = new TreeViewOptions {
				CanDragAndDrop = false,
				TreeViewListener = this,
			};
			this.TreeView = treeViewService.Create(ANALYZER_TREEVIEW_GUID, options);

			documentTabService.DocumentTreeView.DocumentService.CollectionChanged += DocumentService_CollectionChanged;
			documentTabService.DocumentModified += DocumentTabService_FileModified;
			decompilerService.DecompilerChanged += DecompilerService_DecompilerChanged;
			themeService.ThemeChanged += ThemeService_ThemeChanged;
			analyzerSettings.PropertyChanged += AnalyzerSettings_PropertyChanged;

			menuService.InitializeContextMenu(this.TreeView.UIObject, new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID), new GuidObjectsProvider(this.TreeView));
			wpfCommandService.Add(ControlConstants.GUID_ANALYZER_TREEVIEW, this.TreeView.UIObject);
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_ANALYZER_TREEVIEW);
			var command = new RelayCommand(a => ActivateNode());
			cmds.Add(command, ModifierKeys.Control, Key.Enter);
			cmds.Add(command, ModifierKeys.Shift, Key.Enter);
		}

		void DocumentTabService_FileModified(object sender, DocumentModifiedEventArgs e) {
			AnalyzerTreeNodeData.HandleModelUpdated(TreeView.Root, e.Documents);
			RefreshNodes();
		}

		void ActivateNode() {
			var nodes = TreeView.TopLevelSelection;
			var node = nodes.Length == 0 ? null : nodes[0] as ITreeNodeData;
			if (node != null)
				node.Activate();
		}

		void DocumentService_CollectionChanged(object sender, NotifyDocumentCollectionChangedEventArgs e) {
			switch (e.Type) {
			case NotifyDocumentCollectionType.Clear:
				ClearAll();
				break;

			case NotifyDocumentCollectionType.Add:
				AnalyzerTreeNodeData.HandleAssemblyListChanged(TreeView.Root, Array.Empty<IDsDocument>(), e.Documents);
				break;

			case NotifyDocumentCollectionType.Remove:
				AnalyzerTreeNodeData.HandleAssemblyListChanged(TreeView.Root, e.Documents, Array.Empty<IDsDocument>());
				break;

			default:
				break;
			}
		}

		void DecompilerService_DecompilerChanged(object sender, EventArgs e) {
			this.context.Decompiler = ((IDecompilerService)sender).Decompiler;
			RefreshNodes();
		}

		void AnalyzerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var analyzerSettings = (IAnalyzerSettings)sender;
			switch (e.PropertyName) {
			case nameof(analyzerSettings.ShowToken):
				context.ShowToken = analyzerSettings.ShowToken;
				RefreshNodes();
				break;

			case nameof(analyzerSettings.SyntaxHighlight):
				context.SyntaxHighlight = analyzerSettings.SyntaxHighlight;
				RefreshNodes();
				break;

			case nameof(analyzerSettings.UseNewRenderer):
				context.UseNewRenderer = analyzerSettings.UseNewRenderer;
				RefreshNodes();
				break;
			}
		}

		void ThemeService_ThemeChanged(object sender, ThemeChangedEventArgs e) => RefreshNodes();
		void RefreshNodes() => this.TreeView.RefreshAllNodes();

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

		void Cancel() => AnalyzerTreeNodeData.CancelSelfAndChildren(TreeView.Root.Data);
		public void OnClose() => ClearAll();

		void ClearAll() {
			Cancel();
			this.TreeView.Root.Children.Clear();
		}

		public void Add(IAnalyzerTreeNodeData node) {
			if (node is EntityNode) {
				var an = node as EntityNode;
				var found = this.TreeView.Root.DataChildren.OfType<EntityNode>().FirstOrDefault(n => n.Member == an.Member);
				if (found != null) {
					found.TreeNode.IsExpanded = true;
					this.TreeView.SelectItems(new ITreeNodeData[] { found });
					this.TreeView.Focus();
					return;
				}
			}
			this.TreeView.Root.Children.Add(this.TreeView.Create(node));
			node.TreeNode.IsExpanded = true;
			this.TreeView.SelectItems(new ITreeNodeData[] { node });
			this.TreeView.Focus();
		}

		public void OnActivated(IAnalyzerTreeNodeData node) {
			if (node == null)
				throw new ArgumentNullException(nameof(node));
			bool newTab = Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift;
			FollowNode(node, newTab, null);
		}

		public void FollowNode(ITreeNodeData node, bool newTab, bool? useCodeRef) {
			var tokNode = node as IMDTokenNode;
			var @ref = tokNode?.Reference;

			var entityNode = node as EntityNode;
			var srcRef = entityNode?.SourceRef;

			bool code = useCodeRef ?? srcRef != null;
			if (code) {
				if (srcRef == null)
					return;
				documentTabService.FollowReference(srcRef.Value.Method, newTab, true, a => {
					if (!a.HasMovedCaret && a.Success && srcRef != null)
						a.HasMovedCaret = GoTo(a.Tab, srcRef.Value.Method, srcRef.Value.ILOffset, srcRef.Value.Reference);
				});
			}
			else {
				if (@ref == null)
					return;
				documentTabService.FollowReference(@ref, newTab);
			}
		}

		public bool CanFollowNode(ITreeNodeData node, bool useCodeRef) {
			var tokNode = node as IMDTokenNode;
			var @ref = tokNode?.Reference;

			var entityNode = node as EntityNode;
			var srcRef = entityNode?.SourceRef;

			if (useCodeRef)
				return srcRef != null;
			return @ref != null;
		}

		bool GoTo(IDocumentTab tab, MethodDef method, uint? ilOffset, object @ref) {
			if (method == null || ilOffset == null)
				return false;
			var documentViewer = tab.TryGetDocumentViewer();
			if (documentViewer == null)
				return false;
			var methodDebugService = documentViewer.GetMethodDebugService();
			var methodStatement = methodDebugService.FindByCodeOffset(method, ilOffset.Value);
			if (methodStatement == null)
				return false;

			var textSpan = methodStatement.Value.Statement.TextSpan;
			var loc = FindLocation(documentViewer.Content.ReferenceCollection.FindFrom(textSpan.Start), documentViewer.TextView.TextSnapshot, methodStatement.Value.Statement.TextSpan.End, @ref);
			if (loc == null)
				loc = textSpan.Start;

			documentViewer.MoveCaretToPosition(loc.Value);
			return true;
		}

		static int GetLineNumber(ITextSnapshot snapshot, int position) {
			Debug.Assert((uint)position <= (uint)snapshot.Length);
			if ((uint)position > (uint)snapshot.Length)
				return int.MaxValue;
			return snapshot.GetLineFromPosition(position).LineNumber;
		}

		int? FindLocation(IEnumerable<SpanData<ReferenceInfo>> refs, ITextSnapshot snapshot, int endPos, object @ref) {
			int lb = GetLineNumber(snapshot, endPos);
			foreach (var info in refs) {
				var la = GetLineNumber(snapshot, info.Span.Start);
				int c = Compare(la, info.Span.Start, lb, endPos);
				if (c > 0)
					break;
				if (RefEquals(@ref, info.Data.Reference))
					return info.Span.Start;
			}
			return null;
		}

		static int Compare(int la, int ca, int lb, int cb) {
			if (la > lb)
				return 1;
			if (la == lb)
				return ca - cb;
			return -1;
		}

		static bool RefEquals(object a, object b) {
			if (Equals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
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
