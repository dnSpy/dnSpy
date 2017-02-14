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
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Analyzer {
	abstract class OpenReferenceCtxMenuCommandBase : MenuItemBase {
		readonly Lazy<IAnalyzerService> analyzerService;
		readonly bool newTab;
		readonly bool useCodeRef;

		protected OpenReferenceCtxMenuCommandBase(Lazy<IAnalyzerService> analyzerService, bool newTab, bool useCodeRef) {
			this.analyzerService = analyzerService;
			this.newTab = newTab;
			this.useCodeRef = useCodeRef;
		}

		public override void Execute(IMenuItemContext context) {
			var @ref = GetReference(context);
			if (@ref == null)
				return;
			analyzerService.Value.FollowNode(@ref, newTab, useCodeRef);
		}

		public override bool IsVisible(IMenuItemContext context) => GetReference(context) != null;

		TreeNodeData GetReference(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID))
				return null;

			var nodes = context.Find<TreeNodeData[]>();
			if (nodes == null || nodes.Length != 1)
				return null;

			if (nodes[0] is IMDTokenNode tokenNode && tokenNode.Reference != null) {
				if (!analyzerService.Value.CanFollowNode(nodes[0], useCodeRef))
					return null;
				return nodes[0];
			}

			return null;
		}
	}

	[ExportMenuItem(Header = "res:GoToReferenceInCodeCommand", InputGestureText = "res:DoubleClick", Group = MenuConstants.GROUP_CTX_ANALYZER_TABS, Order = 0)]
	sealed class OpenReferenceInCodeCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceInCodeCtxMenuCommand(Lazy<IAnalyzerService> analyzerService)
			: base(analyzerService, false, true) {
		}
	}

	[ExportMenuItem(Header = "res:GoToReferenceInCodeNewTabCommand", InputGestureText = "res:ShiftDoubleClick", Group = MenuConstants.GROUP_CTX_ANALYZER_TABS, Order = 10)]
	sealed class OpenReferenceInCodeNewTabCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceInCodeNewTabCtxMenuCommand(Lazy<IAnalyzerService> analyzerService)
			: base(analyzerService, true, true) {
		}
	}

	[ExportMenuItem(Header = "res:GoToReferenceCommand", Group = MenuConstants.GROUP_CTX_ANALYZER_TABS, Order = 20)]
	sealed class OpenReferenceCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceCtxMenuCommand(Lazy<IAnalyzerService> analyzerService)
			: base(analyzerService, false, false) {
		}
	}

	[ExportMenuItem(Header = "res:GoToReferenceNewTabCommand", Group = MenuConstants.GROUP_CTX_ANALYZER_TABS, Order = 30)]
	sealed class OpenReferenceNewTabCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceNewTabCtxMenuCommand(Lazy<IAnalyzerService> analyzerService)
			: base(analyzerService, true, false) {
		}
	}

	[ExportAutoLoaded]
	sealed class BreakpointsContentCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		BreakpointsContentCommandLoader(IWpfCommandService wpfCommandService, Lazy<IAnalyzerService> analyzerService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_ANALYZER_TREEVIEW);
			cmds.Add(ApplicationCommands.Copy,
				(s, e) => CopyCtxMenuCommand.ExecuteInternal(analyzerService),
				(s, e) => e.CanExecute = CopyCtxMenuCommand.CanExecuteInternal(analyzerService));
		}
	}

	[ExportMenuItem(Header = "res:CopyCommand", InputGestureText = "res:ShortCutKeyCtrlC", Icon = DsImagesAttribute.Copy, Group = MenuConstants.GROUP_CTX_ANALYZER_TOKENS, Order = -1)]
	sealed class CopyCtxMenuCommand : MenuItemBase {
		readonly Lazy<IAnalyzerService> analyzerService;

		[ImportingConstructor]
		CopyCtxMenuCommand(Lazy<IAnalyzerService> analyzerService) => this.analyzerService = analyzerService;

		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID);
		public override bool IsEnabled(IMenuItemContext context) => CanExecuteInternal(analyzerService);
		public override void Execute(IMenuItemContext context) => ExecuteInternal(analyzerService);
		public static bool CanExecuteInternal(Lazy<IAnalyzerService> analyzerService) => analyzerService.Value.TreeView.SelectedItems.Length > 0;

		public static void ExecuteInternal(Lazy<IAnalyzerService> analyzerService) {
			var items = analyzerService.Value.TreeView.SelectedItems;
			var sb = new StringBuilder();
			int count = 0;
			foreach (var t in GetNodes(analyzerService.Value.TreeView, items)) {
				if (count > 0)
					sb.Append(Environment.NewLine);
				sb.Append(new string('\t', t.Item1));
				sb.Append(t.Item2.ToString());
				count++;
			}
			if (count > 1)
				sb.Append(Environment.NewLine);
			if (sb.Length > 0) {
				try {
					Clipboard.SetText(sb.ToString());
				}
				catch (ExternalException) { }
			}
		}

		sealed class State {
			public readonly int Level;
			public int Index;
			public readonly IList<ITreeNode> Nodes;
			public State(ITreeNode node, int level) {
				Level = level;
				Nodes = node.Children;
			}
		}

		static IEnumerable<Tuple<int, TreeNodeData>> GetNodes(ITreeView treeView, IEnumerable<TreeNodeData> nodes) {
			var hash = new HashSet<TreeNodeData>(nodes);
			var stack = new Stack<State>();
			stack.Push(new State(treeView.Root, 0));
			while (stack.Count > 0) {
				var state = stack.Pop();
				if (state.Index >= state.Nodes.Count)
					continue;
				var child = state.Nodes[state.Index++];
				if (hash.Contains(child.Data))
					yield return Tuple.Create(state.Level, child.Data);
				stack.Push(state);
				stack.Push(new State(child, state.Level + 1));
			}
		}
	}

	[ExportMenuItem(Header = "res:ShowMetadataTokensCommand", Group = MenuConstants.GROUP_CTX_ANALYZER_OPTIONS, Order = 0)]
	sealed class ShowTokensCtxMenuCommand : MenuItemBase {
		readonly AnalyzerSettingsImpl analyzerSettings;

		[ImportingConstructor]
		ShowTokensCtxMenuCommand(AnalyzerSettingsImpl analyzerSettings) => this.analyzerSettings = analyzerSettings;

		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID);
		public override bool IsChecked(IMenuItemContext context) => analyzerSettings.ShowToken;
		public override void Execute(IMenuItemContext context) => analyzerSettings.ShowToken = !analyzerSettings.ShowToken;
	}

	[ExportMenuItem(Header = "res:SyntaxHighlightCommand", Group = MenuConstants.GROUP_CTX_ANALYZER_OPTIONS, Order = 10)]
	sealed class SyntaxHighlightCtxMenuCommand : MenuItemBase {
		readonly AnalyzerSettingsImpl analyzerSettings;

		[ImportingConstructor]
		SyntaxHighlightCtxMenuCommand(AnalyzerSettingsImpl analyzerSettings) => this.analyzerSettings = analyzerSettings;

		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID);
		public override bool IsChecked(IMenuItemContext context) => analyzerSettings.SyntaxHighlight;
		public override void Execute(IMenuItemContext context) => analyzerSettings.SyntaxHighlight = !analyzerSettings.SyntaxHighlight;
	}
}
