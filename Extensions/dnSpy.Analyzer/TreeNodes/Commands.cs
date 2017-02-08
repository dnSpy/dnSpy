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
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Analyzer.TreeNodes {
	[ExportAutoLoaded]
	sealed class AnalyzeCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand AnalyzeRoutedCommand = new RoutedCommand("AnalyzeRoutedCommand", typeof(AnalyzeCommandLoader));
		readonly IDsToolWindowService toolWindowService;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IAnalyzerService> analyzerService;
		readonly IDecompilerService decompilerService;

		[ImportingConstructor]
		AnalyzeCommandLoader(IDsToolWindowService toolWindowService, IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, Lazy<IAnalyzerService> analyzerService, IDecompilerService decompilerService) {
			this.toolWindowService = toolWindowService;
			this.documentTabService = documentTabService;
			this.analyzerService = analyzerService;
			this.decompilerService = decompilerService;

			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENTVIEWER_UICONTEXT);
			cmds.Add(AnalyzeRoutedCommand, TextEditor_Executed, TextEditor_CanExecute, ModifierKeys.Control | ModifierKeys.Shift, Key.R);
			cmds.Add(AnalyzeRoutedCommand, ShowAnalyzerExecuted, ShowAnalyzerCanExecute, ModifierKeys.Control | ModifierKeys.Shift, Key.R);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENT_TREEVIEW);
			cmds.Add(AnalyzeRoutedCommand, DocumentTreeView_Executed, DocumentTreeView_CanExecute, ModifierKeys.Control | ModifierKeys.Shift, Key.R);
			cmds.Add(AnalyzeRoutedCommand, ShowAnalyzerExecuted, ShowAnalyzerCanExecute, ModifierKeys.Control | ModifierKeys.Shift, Key.R);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_ANALYZER_TREEVIEW);
			cmds.Add(AnalyzeRoutedCommand, AnalyzerTreeView_Executed, AnalyzerTreeView_CanExecute, ModifierKeys.Control | ModifierKeys.Shift, Key.R);
			cmds.Add(AnalyzeRoutedCommand, ShowAnalyzerExecuted, ShowAnalyzerCanExecute, ModifierKeys.Control | ModifierKeys.Shift, Key.R);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_SEARCH_LISTBOX);
			cmds.Add(AnalyzeRoutedCommand, SearchListBox_Executed, SearchListBox_CanExecute, ModifierKeys.Control | ModifierKeys.Shift, Key.R);
			cmds.Add(AnalyzeRoutedCommand, ShowAnalyzerExecuted, ShowAnalyzerCanExecute, ModifierKeys.Control | ModifierKeys.Shift, Key.R);
		}

		void ShowAnalyzerCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = toolWindowService.IsShown(AnalyzerToolWindowContent.THE_GUID);
		void ShowAnalyzerExecuted(object sender, ExecutedRoutedEventArgs e) =>
			toolWindowService.Show(AnalyzerToolWindowContent.THE_GUID);
		void TextEditor_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = AnalyzeCommand.CanAnalyze(TextEditor_GetMemberRef(), decompilerService.Decompiler);
		void TextEditor_Executed(object sender, ExecutedRoutedEventArgs e) =>
			AnalyzeCommand.Analyze(toolWindowService, analyzerService, decompilerService.Decompiler, TextEditor_GetMemberRef());
		IMemberRef TextEditor_GetMemberRef() =>
			(documentTabService.ActiveTab?.UIContext as IDocumentViewer)?.SelectedReference?.Data.Reference as IMemberRef;
		void DocumentTreeView_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = AnalyzeCommand.CanAnalyze(DocumentTreeView_GetMemberRef(), decompilerService.Decompiler);
		void DocumentTreeView_Executed(object sender, ExecutedRoutedEventArgs e) =>
			AnalyzeCommand.Analyze(toolWindowService, analyzerService, decompilerService.Decompiler, DocumentTreeView_GetMemberRef());

		IMemberRef DocumentTreeView_GetMemberRef() {
			var nodes = documentTabService.DocumentTreeView.TreeView.TopLevelSelection;
			var node = nodes.Length == 0 ? null : nodes[0] as IMDTokenNode;
			return node?.Reference as IMemberRef;
		}

		void AnalyzerTreeView_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = AnalyzeCommand.CanAnalyze(AnalyzerTreeView_GetMemberRef(), decompilerService.Decompiler);
		void AnalyzerTreeView_Executed(object sender, ExecutedRoutedEventArgs e) =>
			AnalyzeCommand.Analyze(toolWindowService, analyzerService, decompilerService.Decompiler, AnalyzerTreeView_GetMemberRef());

		IMemberRef AnalyzerTreeView_GetMemberRef() {
			var nodes = analyzerService.Value.TreeView.TopLevelSelection;
			var node = nodes.Length == 0 ? null : nodes[0] as IMDTokenNode;
			return node?.Reference as IMemberRef;
		}

		void SearchListBox_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = AnalyzeCommand.CanAnalyze(SearchListBox_GetMemberRef(e.Source as ListBox), decompilerService.Decompiler);
		void SearchListBox_Executed(object sender, ExecutedRoutedEventArgs e) =>
			AnalyzeCommand.Analyze(toolWindowService, analyzerService, decompilerService.Decompiler, SearchListBox_GetMemberRef(e.Source as ListBox));
		IMemberRef SearchListBox_GetMemberRef(ListBox listBox) =>
			(listBox?.SelectedItem as ISearchResultReferenceProvider)?.Reference as IMemberRef;
	}

	static class AnalyzeCommand {
		[ExportMenuItem(Header = "res:AnalyzeCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:ShortCutKeyCtrlShiftR", Group = MenuConstants.GROUP_CTX_DOCUMENTS_OTHER, Order = 0)]
		sealed class FilesCommand : MenuItemBase {
			readonly IDsToolWindowService toolWindowService;
			readonly IDecompilerService decompilerService;
			readonly Lazy<IAnalyzerService> analyzerService;

			[ImportingConstructor]
			FilesCommand(IDsToolWindowService toolWindowService, IDecompilerService decompilerService, Lazy<IAnalyzerService> analyzerService) {
				this.toolWindowService = toolWindowService;
				this.decompilerService = decompilerService;
				this.analyzerService = analyzerService;
			}

			public override bool IsVisible(IMenuItemContext context) => GetMemberRefs(context).Any();
			IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) =>
				GetMemberRefs(context, MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID, false, decompilerService);

			internal static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context, string guid, bool checkRoot, IDecompilerService decompilerService) {
				if (context.CreatorObject.Guid != new Guid(guid))
					yield break;
				var nodes = context.Find<TreeNodeData[]>();
				if (nodes == null)
					yield break;

				if (checkRoot && nodes.All(a => a.TreeNode.Parent != null && a.TreeNode.Parent.Parent == null))
					yield break;

				foreach (var node in nodes) {
					var mr = node as IMDTokenNode;
					if (mr != null && CanAnalyze(mr.Reference as IMemberRef, decompilerService.Decompiler))
						yield return mr.Reference as IMemberRef;
				}
			}

			public override void Execute(IMenuItemContext context) =>
				Analyze(toolWindowService, analyzerService, decompilerService.Decompiler, GetMemberRefs(context));
		}

		[ExportMenuItem(Header = "res:AnalyzeCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:ShortCutKeyCtrlShiftR", Group = MenuConstants.GROUP_CTX_ANALYZER_OTHER, Order = 0)]
		sealed class AnalyzerCommand : MenuItemBase {
			readonly IDsToolWindowService toolWindowService;
			readonly IDecompilerService decompilerService;
			readonly Lazy<IAnalyzerService> analyzerService;

			[ImportingConstructor]
			AnalyzerCommand(IDsToolWindowService toolWindowService, IDecompilerService decompilerService, Lazy<IAnalyzerService> analyzerService) {
				this.toolWindowService = toolWindowService;
				this.decompilerService = decompilerService;
				this.analyzerService = analyzerService;
			}

			public override bool IsVisible(IMenuItemContext context) => GetMemberRefs(context).Any();
			IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) =>
				FilesCommand.GetMemberRefs(context, MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID, true, decompilerService);
			public override void Execute(IMenuItemContext context) =>
				Analyze(toolWindowService, analyzerService, decompilerService.Decompiler, GetMemberRefs(context));
		}

		[ExportMenuItem(Header = "res:AnalyzeCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:ShortCutKeyCtrlShiftR", Group = MenuConstants.GROUP_CTX_DOCVIEWER_OTHER, Order = 0)]
		sealed class CodeCommand : MenuItemBase {
			readonly IDsToolWindowService toolWindowService;
			readonly IDecompilerService decompilerService;
			readonly Lazy<IAnalyzerService> analyzerService;

			[ImportingConstructor]
			CodeCommand(IDsToolWindowService toolWindowService, IDecompilerService decompilerService, Lazy<IAnalyzerService> analyzerService) {
				this.toolWindowService = toolWindowService;
				this.decompilerService = decompilerService;
				this.analyzerService = analyzerService;
			}

			public override bool IsVisible(IMenuItemContext context) => GetMemberRefs(context).Any();
			static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) =>
				GetMemberRefs(context, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID);

			internal static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					yield break;

				var @ref = context.Find<TextReference>();
				if (@ref != null) {
					var mr = @ref.Reference as IMemberRef;
					if (mr != null)
						yield return mr;
				}
			}

			public override void Execute(IMenuItemContext context) =>
				Analyze(toolWindowService, analyzerService, decompilerService.Decompiler, GetMemberRefs(context));
		}

		[ExportMenuItem(Header = "res:AnalyzeCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:ShortCutKeyCtrlShiftR", Group = MenuConstants.GROUP_CTX_SEARCH_OTHER, Order = 0)]
		sealed class SearchCommand : MenuItemBase {
			readonly IDsToolWindowService toolWindowService;
			readonly IDecompilerService decompilerService;
			readonly Lazy<IAnalyzerService> analyzerService;

			[ImportingConstructor]
			SearchCommand(IDsToolWindowService toolWindowService, IDecompilerService decompilerService, Lazy<IAnalyzerService> analyzerService) {
				this.toolWindowService = toolWindowService;
				this.decompilerService = decompilerService;
				this.analyzerService = analyzerService;
			}

			static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) =>
				CodeCommand.GetMemberRefs(context, MenuConstants.GUIDOBJ_SEARCH_GUID);
			public override bool IsVisible(IMenuItemContext context) => GetMemberRefs(context).Any();
			public override void Execute(IMenuItemContext context) =>
				Analyze(toolWindowService, analyzerService, decompilerService.Decompiler, GetMemberRefs(context));
		}

		public static bool CanAnalyze(IMemberRef member, IDecompiler decompiler) {
			member = ResolveReference(member);
			return member is TypeDef ||
					member is FieldDef ||
					member is MethodDef ||
					PropertyNode.CanShow(member, decompiler) ||
					EventNode.CanShow(member, decompiler);
		}

		static void Analyze(IDsToolWindowService toolWindowService, Lazy<IAnalyzerService> analyzerService, IDecompiler decompiler, IEnumerable<IMemberRef> mrs) {
			foreach (var mr in mrs)
				Analyze(toolWindowService, analyzerService, decompiler, mr);
		}

		public static void Analyze(IDsToolWindowService toolWindowService, Lazy<IAnalyzerService> analyzerService, IDecompiler decompiler, IMemberRef member) {
			var memberDef = ResolveReference(member);

			var type = memberDef as TypeDef;
			if (type != null) {
				toolWindowService.Show(AnalyzerToolWindowContent.THE_GUID);
				analyzerService.Value.Add(new TypeNode(type));
			}

			var field = memberDef as FieldDef;
			if (field != null) {
				toolWindowService.Show(AnalyzerToolWindowContent.THE_GUID);
				analyzerService.Value.Add(new FieldNode(field));
			}

			var method = memberDef as MethodDef;
			if (method != null) {
				toolWindowService.Show(AnalyzerToolWindowContent.THE_GUID);
				analyzerService.Value.Add(new MethodNode(method));
			}

			var propertyAnalyzer = PropertyNode.TryCreateAnalyzer(member, decompiler);
			if (propertyAnalyzer != null) {
				toolWindowService.Show(AnalyzerToolWindowContent.THE_GUID);
				analyzerService.Value.Add(propertyAnalyzer);
			}

			var eventAnalyzer = EventNode.TryCreateAnalyzer(member, decompiler);
			if (eventAnalyzer != null) {
				toolWindowService.Show(AnalyzerToolWindowContent.THE_GUID);
				analyzerService.Value.Add(eventAnalyzer);
			}
		}

		static IMemberDef ResolveReference(object reference) {
			if (reference is ITypeDefOrRef)
				return ((ITypeDefOrRef)reference).ResolveTypeDef();
			else if (reference is IMethod && ((IMethod)reference).MethodSig != null)
				return ((IMethod)reference).ResolveMethodDef();
			else if (reference is IField)
				return ((IField)reference).ResolveFieldDef();
			else if (reference is PropertyDef)
				return (PropertyDef)reference;
			else if (reference is EventDef)
				return (EventDef)reference;
			return null;
		}
	}

	[ExportAutoLoaded]
	sealed class RemoveAnalyzeCommand : IAutoLoaded {
		readonly Lazy<IAnalyzerService> analyzerService;

		[ImportingConstructor]
		RemoveAnalyzeCommand(IWpfCommandService wpfCommandService, Lazy<IAnalyzerService> analyzerService) {
			this.analyzerService = analyzerService;
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_ANALYZER_TREEVIEW);
			cmds.Add(ApplicationCommands.Delete, (s, e) => DeleteNodes(), (s, e) => e.CanExecute = CanDeleteNodes, ModifierKeys.None, Key.Delete);
		}

		bool CanDeleteNodes => GetNodes() != null;
		void DeleteNodes() => DeleteNodes(GetNodes());
		TreeNodeData[] GetNodes() => GetNodes(analyzerService.Value.TreeView.TopLevelSelection);

		internal static TreeNodeData[] GetNodes(TreeNodeData[] nodes) {
			if (nodes == null)
				return null;
			if (nodes.Length == 0 || !nodes.All(a => a.TreeNode.Parent != null && a.TreeNode.Parent.Parent == null))
				return null;
			return nodes;
		}

		internal static void DeleteNodes(TreeNodeData[] nodes) {
			if (nodes != null) {
				foreach (var node in nodes) {
					AnalyzerTreeNodeData.CancelSelfAndChildren(node);
					node.TreeNode.Parent.Children.Remove(node.TreeNode);
				}
			}
		}
	}

	[ExportMenuItem(Header = "res:RemoveCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:ShortCutKeyDelete", Group = MenuConstants.GROUP_CTX_ANALYZER_OTHER, Order = 10)]
	sealed class RemoveAnalyzeCtxMenuCommand : MenuItemBase {
		public override bool IsVisible(IMenuItemContext context) => GetNodes(context) != null;

		static TreeNodeData[] GetNodes(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID))
				return null;
			return RemoveAnalyzeCommand.GetNodes(context.Find<TreeNodeData[]>());
		}

		public override void Execute(IMenuItemContext context) => RemoveAnalyzeCommand.DeleteNodes(GetNodes(context));
	}
}
