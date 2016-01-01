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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Menus;

namespace dnSpy.Analyzer {
	abstract class OpenReferenceCtxMenuCommandBase : MenuItemBase {
		readonly IFileTabManager fileTabManager;
		readonly bool newTab;

		protected OpenReferenceCtxMenuCommandBase(IFileTabManager fileTabManager, bool newTab) {
			this.fileTabManager = fileTabManager;
			this.newTab = newTab;
		}

		public override void Execute(IMenuItemContext context) {
			var @ref = GetReference(context);
			if (@ref == null)
				return;
			fileTabManager.FollowReference(@ref, newTab);
		}

		public override bool IsVisible(IMenuItemContext context) {
			return GetReference(context) != null;
		}

		object GetReference(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID))
				return null;

			var nodes = context.Find<ITreeNodeData[]>();
			if (nodes == null || nodes.Length != 1)
				return null;

			var tokenNode = nodes[0] as IMDTokenNode;
			if (tokenNode != null && tokenNode.Reference != null)
				return tokenNode.Reference;

			return null;
		}
	}

	[ExportMenuItem(Header = "res:GoToReferenceCommand", InputGestureText = "res:GoToReferenceKey", Group = MenuConstants.GROUP_CTX_ANALYZER_TABS, Order = 0)]
	sealed class OpenReferenceCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager, false) {
		}
	}

	[ExportMenuItem(Header = "res:OpenInNewTabCommand", InputGestureText = "res:OpenInNewTabKey", Group = MenuConstants.GROUP_CTX_ANALYZER_TABS, Order = 10)]
	sealed class OpenReferenceNewTabCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceNewTabCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager, true) {
		}
	}

	[ExportMenuItem(Header = "res:ShowMetadataTokensCommand", Group = MenuConstants.GROUP_CTX_ANALYZER_OPTIONS, Order = 0)]
	sealed class ShowTokensCtxMenuCommand : MenuItemBase {
		readonly AnalyzerSettingsImpl analyzerSettings;

		[ImportingConstructor]
		ShowTokensCtxMenuCommand(AnalyzerSettingsImpl analyzerSettings) {
			this.analyzerSettings = analyzerSettings;
		}

		public override bool IsVisible(IMenuItemContext context) {
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID);
		}

		public override bool IsChecked(IMenuItemContext context) {
			return analyzerSettings.ShowToken;
		}

		public override void Execute(IMenuItemContext context) {
			analyzerSettings.ShowToken = !analyzerSettings.ShowToken;
		}
	}

	[ExportMenuItem(Header = "res:SyntaxHighlightCommand", Group = MenuConstants.GROUP_CTX_ANALYZER_OPTIONS, Order = 10)]
	sealed class SyntaxHighlightCtxMenuCommand : MenuItemBase {
		readonly AnalyzerSettingsImpl analyzerSettings;

		[ImportingConstructor]
		SyntaxHighlightCtxMenuCommand(AnalyzerSettingsImpl analyzerSettings) {
			this.analyzerSettings = analyzerSettings;
		}

		public override bool IsVisible(IMenuItemContext context) {
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID);
		}

		public override bool IsChecked(IMenuItemContext context) {
			return analyzerSettings.SyntaxHighlight;
		}

		public override void Execute(IMenuItemContext context) {
			analyzerSettings.SyntaxHighlight = !analyzerSettings.SyntaxHighlight;
		}
	}
}
