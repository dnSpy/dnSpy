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
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.Menus;

namespace dnSpy.Analyzer.TreeNodes {
	[ExportAutoLoaded]
	sealed class AnalyzeCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand AnalyzeRoutedCommand = new RoutedCommand("AnalyzeRoutedCommand", typeof(AnalyzeCommandLoader));
		readonly IMainToolWindowManager mainToolWindowManager;
		readonly IFileTabManager fileTabManager;
		readonly Lazy<IAnalyzerManager> analyzerManager;
		readonly ILanguageManager languageManager;

		[ImportingConstructor]
		AnalyzeCommandLoader(IMainToolWindowManager mainToolWindowManager, IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, Lazy<IAnalyzerManager> analyzerManager, ILanguageManager languageManager) {
			this.mainToolWindowManager = mainToolWindowManager;
			this.fileTabManager = fileTabManager;
			this.analyzerManager = analyzerManager;
			this.languageManager = languageManager;

			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_TEXTEDITOR_UICONTEXT);
			cmds.Add(AnalyzeRoutedCommand, TextEditor_Executed, TextEditor_CanExecute, ModifierKeys.Control, Key.R);
			cmds.Add(AnalyzeRoutedCommand, ShowAnalyzerExecuted, ShowAnalyzerCanExecute, ModifierKeys.Control, Key.R);

			cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_FILE_TREEVIEW);
			cmds.Add(AnalyzeRoutedCommand, FileTreeView_Executed, FileTreeView_CanExecute, ModifierKeys.Control, Key.R);
			cmds.Add(AnalyzeRoutedCommand, ShowAnalyzerExecuted, ShowAnalyzerCanExecute, ModifierKeys.Control, Key.R);

			cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_ANALYZER_TREEVIEW);
			cmds.Add(AnalyzeRoutedCommand, AnalyzerTreeView_Executed, AnalyzerTreeView_CanExecute, ModifierKeys.Control, Key.R);
			cmds.Add(AnalyzeRoutedCommand, ShowAnalyzerExecuted, ShowAnalyzerCanExecute, ModifierKeys.Control, Key.R);

			cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_SEARCH_LISTBOX);
			cmds.Add(AnalyzeRoutedCommand, SearchListBox_Executed, SearchListBox_CanExecute, ModifierKeys.Control, Key.R);
			cmds.Add(AnalyzeRoutedCommand, ShowAnalyzerExecuted, ShowAnalyzerCanExecute, ModifierKeys.Control, Key.R);
		}

		void ShowAnalyzerCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = mainToolWindowManager.IsShown(AnalyzerToolWindowContent.THE_GUID);
		void ShowAnalyzerExecuted(object sender, ExecutedRoutedEventArgs e) =>
			mainToolWindowManager.Show(AnalyzerToolWindowContent.THE_GUID);
		void TextEditor_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = AnalyzeCommand.CanAnalyze(TextEditor_GetMemberRef(), languageManager.Language);
		void TextEditor_Executed(object sender, ExecutedRoutedEventArgs e) =>
			AnalyzeCommand.Analyze(mainToolWindowManager, analyzerManager, languageManager.Language, TextEditor_GetMemberRef());
		IMemberRef TextEditor_GetMemberRef() =>
			(fileTabManager.ActiveTab?.UIContext as ITextEditorUIContext)?.SelectedReference as IMemberRef;
		void FileTreeView_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = AnalyzeCommand.CanAnalyze(FileTreeView_GetMemberRef(), languageManager.Language);
		void FileTreeView_Executed(object sender, ExecutedRoutedEventArgs e) =>
			AnalyzeCommand.Analyze(mainToolWindowManager, analyzerManager, languageManager.Language, FileTreeView_GetMemberRef());

		IMemberRef FileTreeView_GetMemberRef() {
			var nodes = fileTabManager.FileTreeView.TreeView.TopLevelSelection;
			var node = nodes.Length == 0 ? null : nodes[0] as IMDTokenNode;
			return node?.Reference as IMemberRef;
		}

		void AnalyzerTreeView_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = AnalyzeCommand.CanAnalyze(AnalyzerTreeView_GetMemberRef(), languageManager.Language);
		void AnalyzerTreeView_Executed(object sender, ExecutedRoutedEventArgs e) =>
			AnalyzeCommand.Analyze(mainToolWindowManager, analyzerManager, languageManager.Language, AnalyzerTreeView_GetMemberRef());

		IMemberRef AnalyzerTreeView_GetMemberRef() {
			var nodes = analyzerManager.Value.TreeView.TopLevelSelection;
			var node = nodes.Length == 0 ? null : nodes[0] as IMDTokenNode;
			return node?.Reference as IMemberRef;
		}

		void SearchListBox_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = AnalyzeCommand.CanAnalyze(SearchListBox_GetMemberRef(e.Source as ListBox), languageManager.Language);
		void SearchListBox_Executed(object sender, ExecutedRoutedEventArgs e) =>
			AnalyzeCommand.Analyze(mainToolWindowManager, analyzerManager, languageManager.Language, SearchListBox_GetMemberRef(e.Source as ListBox));
		IMemberRef SearchListBox_GetMemberRef(ListBox listBox) =>
			(listBox?.SelectedItem as ISearchResult)?.Reference as IMemberRef;
	}

	static class AnalyzeCommand {
		[ExportMenuItem(Header = "res:AnalyzeCommand", Icon = "Search", InputGestureText = "res:ShortCutKeyCtrlR", Group = MenuConstants.GROUP_CTX_FILES_OTHER, Order = 0)]
		sealed class FilesCommand : MenuItemBase {
			readonly IMainToolWindowManager mainToolWindowManager;
			readonly ILanguageManager languageManager;
			readonly Lazy<IAnalyzerManager> analyzerManager;

			[ImportingConstructor]
			FilesCommand(IMainToolWindowManager mainToolWindowManager, ILanguageManager languageManager, Lazy<IAnalyzerManager> analyzerManager) {
				this.mainToolWindowManager = mainToolWindowManager;
				this.languageManager = languageManager;
				this.analyzerManager = analyzerManager;
			}

			public override bool IsVisible(IMenuItemContext context) => GetMemberRefs(context).Any();
			IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) =>
				GetMemberRefs(context, MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID, false, languageManager);

			internal static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context, string guid, bool checkRoot, ILanguageManager languageManager) {
				if (context.CreatorObject.Guid != new Guid(guid))
					yield break;
				var nodes = context.Find<ITreeNodeData[]>();
				if (nodes == null)
					yield break;

				if (checkRoot && nodes.All(a => a.TreeNode.Parent != null && a.TreeNode.Parent.Parent == null))
					yield break;

				foreach (var node in nodes) {
					var mr = node as IMDTokenNode;
					if (mr != null && CanAnalyze(mr.Reference as IMemberRef, languageManager.Language))
						yield return mr.Reference as IMemberRef;
				}
			}

			public override void Execute(IMenuItemContext context) =>
				Analyze(mainToolWindowManager, analyzerManager, languageManager.Language, GetMemberRefs(context));
		}

		[ExportMenuItem(Header = "res:AnalyzeCommand", Icon = "Search", InputGestureText = "res:ShortCutKeyCtrlR", Group = MenuConstants.GROUP_CTX_ANALYZER_OTHER, Order = 0)]
		sealed class AnalyzerCommand : MenuItemBase {
			readonly IMainToolWindowManager mainToolWindowManager;
			readonly ILanguageManager languageManager;
			readonly Lazy<IAnalyzerManager> analyzerManager;

			[ImportingConstructor]
			AnalyzerCommand(IMainToolWindowManager mainToolWindowManager, ILanguageManager languageManager, Lazy<IAnalyzerManager> analyzerManager) {
				this.mainToolWindowManager = mainToolWindowManager;
				this.languageManager = languageManager;
				this.analyzerManager = analyzerManager;
			}

			public override bool IsVisible(IMenuItemContext context) => GetMemberRefs(context).Any();
			IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) =>
				FilesCommand.GetMemberRefs(context, MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID, true, languageManager);
			public override void Execute(IMenuItemContext context) =>
				Analyze(mainToolWindowManager, analyzerManager, languageManager.Language, GetMemberRefs(context));
		}

		[ExportMenuItem(Header = "res:AnalyzeCommand", Icon = "Search", InputGestureText = "res:ShortCutKeyCtrlR", Group = MenuConstants.GROUP_CTX_CODE_OTHER, Order = 0)]
		sealed class CodeCommand : MenuItemBase {
			readonly IMainToolWindowManager mainToolWindowManager;
			readonly ILanguageManager languageManager;
			readonly Lazy<IAnalyzerManager> analyzerManager;

			[ImportingConstructor]
			CodeCommand(IMainToolWindowManager mainToolWindowManager, ILanguageManager languageManager, Lazy<IAnalyzerManager> analyzerManager) {
				this.mainToolWindowManager = mainToolWindowManager;
				this.languageManager = languageManager;
				this.analyzerManager = analyzerManager;
			}

			public override bool IsVisible(IMenuItemContext context) => GetMemberRefs(context).Any();
			static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) =>
				GetMemberRefs(context, MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID);

			internal static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					yield break;

				var @ref = context.Find<CodeReference>();
				if (@ref != null) {
					var mr = @ref.Reference as IMemberRef;
					if (mr != null)
						yield return mr;
				}
			}

			public override void Execute(IMenuItemContext context) =>
				Analyze(mainToolWindowManager, analyzerManager, languageManager.Language, GetMemberRefs(context));
		}

		[ExportMenuItem(Header = "res:AnalyzeCommand", Icon = "Search", InputGestureText = "res:ShortCutKeyCtrlR", Group = MenuConstants.GROUP_CTX_SEARCH_OTHER, Order = 0)]
		sealed class SearchCommand : MenuItemBase {
			readonly IMainToolWindowManager mainToolWindowManager;
			readonly ILanguageManager languageManager;
			readonly Lazy<IAnalyzerManager> analyzerManager;

			[ImportingConstructor]
			SearchCommand(IMainToolWindowManager mainToolWindowManager, ILanguageManager languageManager, Lazy<IAnalyzerManager> analyzerManager) {
				this.mainToolWindowManager = mainToolWindowManager;
				this.languageManager = languageManager;
				this.analyzerManager = analyzerManager;
			}

			static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) =>
				CodeCommand.GetMemberRefs(context, MenuConstants.GUIDOBJ_SEARCH_GUID);
			public override bool IsVisible(IMenuItemContext context) => GetMemberRefs(context).Any();
			public override void Execute(IMenuItemContext context) =>
				Analyze(mainToolWindowManager, analyzerManager, languageManager.Language, GetMemberRefs(context));
		}

		public static bool CanAnalyze(IMemberRef member, ILanguage language) {
			member = ResolveReference(member);
			return member is TypeDef ||
					member is FieldDef ||
					member is MethodDef ||
					PropertyNode.CanShow(member, language) ||
					EventNode.CanShow(member, language);
		}

		static void Analyze(IMainToolWindowManager mainToolWindowManager, Lazy<IAnalyzerManager> analyzerManager, ILanguage language, IEnumerable<IMemberRef> mrs) {
			foreach (var mr in mrs)
				Analyze(mainToolWindowManager, analyzerManager, language, mr);
		}

		public static void Analyze(IMainToolWindowManager mainToolWindowManager, Lazy<IAnalyzerManager> analyzerManager, ILanguage language, IMemberRef member) {
			var memberDef = ResolveReference(member);

			var type = memberDef as TypeDef;
			if (type != null) {
				mainToolWindowManager.Show(AnalyzerToolWindowContent.THE_GUID);
				analyzerManager.Value.Add(new TypeNode(type));
			}

			var field = memberDef as FieldDef;
			if (field != null) {
				mainToolWindowManager.Show(AnalyzerToolWindowContent.THE_GUID);
				analyzerManager.Value.Add(new FieldNode(field));
			}

			var method = memberDef as MethodDef;
			if (method != null) {
				mainToolWindowManager.Show(AnalyzerToolWindowContent.THE_GUID);
				analyzerManager.Value.Add(new MethodNode(method));
			}

			var propertyAnalyzer = PropertyNode.TryCreateAnalyzer(member, language);
			if (propertyAnalyzer != null) {
				mainToolWindowManager.Show(AnalyzerToolWindowContent.THE_GUID);
				analyzerManager.Value.Add(propertyAnalyzer);
			}

			var eventAnalyzer = EventNode.TryCreateAnalyzer(member, language);
			if (eventAnalyzer != null) {
				mainToolWindowManager.Show(AnalyzerToolWindowContent.THE_GUID);
				analyzerManager.Value.Add(eventAnalyzer);
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
		readonly Lazy<IAnalyzerManager> analyzerManager;

		[ImportingConstructor]
		RemoveAnalyzeCommand(IWpfCommandManager wpfCommandManager, Lazy<IAnalyzerManager> analyzerManager) {
			this.analyzerManager = analyzerManager;
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_ANALYZER_TREEVIEW);
			cmds.Add(ApplicationCommands.Delete, (s, e) => DeleteNodes(), (s, e) => e.CanExecute = CanDeleteNodes, ModifierKeys.None, Key.Delete);
		}

		bool CanDeleteNodes => GetNodes() != null;
		void DeleteNodes() => DeleteNodes(GetNodes());
		ITreeNodeData[] GetNodes() => GetNodes(analyzerManager.Value.TreeView.TopLevelSelection);

		internal static ITreeNodeData[] GetNodes(ITreeNodeData[] nodes) {
			if (nodes == null)
				return null;
			if (nodes.Length == 0 || !nodes.All(a => a.TreeNode.Parent != null && a.TreeNode.Parent.Parent == null))
				return null;
			return nodes;
		}

		internal static void DeleteNodes(ITreeNodeData[] nodes) {
			if (nodes != null) {
				foreach (var node in nodes) {
					AnalyzerTreeNodeData.CancelSelfAndChildren(node);
					node.TreeNode.Parent.Children.Remove(node.TreeNode);
				}
			}
		}
	}

	[ExportMenuItem(Header = "res:RemoveCommand", Icon = "Delete", InputGestureText = "res:ShortCutKeyDelete", Group = MenuConstants.GROUP_CTX_ANALYZER_OTHER, Order = 10)]
	sealed class RemoveAnalyzeCtxMenuCommand : MenuItemBase {
		public override bool IsVisible(IMenuItemContext context) => GetNodes(context) != null;

		static ITreeNodeData[] GetNodes(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID))
				return null;
			return RemoveAnalyzeCommand.GetNodes(context.Find<ITreeNodeData[]>());
		}

		public override void Execute(IMenuItemContext context) => RemoveAnalyzeCommand.DeleteNodes(GetNodes(context));
	}
}
