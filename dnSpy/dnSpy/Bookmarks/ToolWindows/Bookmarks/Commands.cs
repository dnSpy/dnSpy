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
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Properties;

namespace dnSpy.Bookmarks.ToolWindows.Bookmarks {
	[ExportAutoLoaded]
	sealed class BookmarksCommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		BookmarksCommandsLoader(IWpfCommandService wpfCommandService, Lazy<IBookmarksContent> bookmarksContent) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_BOOKMARKS_LISTVIEW);
			cmds.Add(new RelayCommand(a => bookmarksContent.Value.Operations.Copy(), a => bookmarksContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => bookmarksContent.Value.Operations.Copy(), a => bookmarksContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.Insert);
			cmds.Add(new RelayCommand(a => bookmarksContent.Value.Operations.RemoveBookmarks(), a => bookmarksContent.Value.Operations.CanRemoveBookmarks), ModifierKeys.None, Key.Delete);
			cmds.Add(new RelayCommand(a => bookmarksContent.Value.Operations.GoToLocation(false), a => bookmarksContent.Value.Operations.CanGoToLocation), ModifierKeys.None, Key.Enter);
			cmds.Add(new RelayCommand(a => bookmarksContent.Value.Operations.EditName(), a => bookmarksContent.Value.Operations.CanEditName), ModifierKeys.None, Key.F2);
			cmds.Add(new RelayCommand(a => bookmarksContent.Value.Operations.EditName(), a => bookmarksContent.Value.Operations.CanEditName), ModifierKeys.Control, Key.D1);
			cmds.Add(new RelayCommand(a => bookmarksContent.Value.Operations.EditLabels(), a => bookmarksContent.Value.Operations.CanEditLabels), ModifierKeys.Control, Key.D2);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_BOOKMARKS_CONTROL);
			cmds.Add(new RelayCommand(a => bookmarksContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.F);
			cmds.Add(new RelayCommand(a => bookmarksContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.E);
		}
	}

	sealed class BookmarksCtxMenuContext {
		public BookmarksOperations Operations { get; }
		public BookmarksCtxMenuContext(BookmarksOperations operations) => Operations = operations;
	}

	abstract class BookmarksCtxMenuCommand : MenuItemBase<BookmarksCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<IBookmarksContent> bookmarksContent;

		protected BookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarksContent) => this.bookmarksContent = bookmarksContent;

		protected sealed override BookmarksCtxMenuContext? CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != bookmarksContent.Value.ListView)
				return null;
			return Create();
		}

		BookmarksCtxMenuContext Create() => new BookmarksCtxMenuContext(bookmarksContent.Value.Operations);
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_BOOKMARKS_COPY, Order = 0)]
	sealed class CopyBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		CopyBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarksContent)
			: base(bookmarksContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.Copy();
		public override bool IsEnabled(BookmarksCtxMenuContext context) => context.Operations.CanCopy;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_BOOKMARKS_COPY, Order = 10)]
	sealed class SelectAllBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		SelectAllBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarksContent)
			: base(bookmarksContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.SelectAll();
		public override bool IsEnabled(BookmarksCtxMenuContext context) => context.Operations.CanSelectAll;
	}

	[ExportMenuItem(Header = "res:GoToBookmarkCommand", Icon = DsImagesAttribute.GoToSourceCode, InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_BOOKMARKS_CODE, Order = 0)]
	sealed class GoToSourceBookmarkCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		GoToSourceBookmarkCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.GoToLocation(false);
		public override bool IsEnabled(BookmarksCtxMenuContext context) => context.Operations.CanGoToLocation;
	}

	[ExportMenuItem(Header = "res:RenameCommand", Icon = DsImagesAttribute.Edit, InputGestureText = "res:ShortCutKeyF2", Group = MenuConstants.GROUP_CTX_BOOKMARKS_SETTINGS, Order = 0)]
	sealed class EditNameBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		EditNameBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.EditName();
		public override bool IsEnabled(BookmarksCtxMenuContext context) => context.Operations.CanEditName;
	}

	[ExportMenuItem(Header = "res:EditLabelsCommand", Group = MenuConstants.GROUP_CTX_BOOKMARKS_SETTINGS, Order = 10)]
	sealed class EditLabelsBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		EditLabelsBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.EditLabels();
		public override bool IsEnabled(BookmarksCtxMenuContext context) => context.Operations.CanEditLabels;
		public override string? GetInputGestureText(BookmarksCtxMenuContext context) => string.Format(dnSpy_Resources.ShortCutKeyCtrl_DIGIT, "2");
	}

	[ExportMenuItem(Header = "res:RemoveBookmarkCommand", InputGestureText = "res:ShortCutKeyDelete", Icon = DsImagesAttribute.Cancel, Group = MenuConstants.GROUP_CTX_BOOKMARKS_CMDS1, Order = 0)]
	sealed class RemoveBookmarkBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		RemoveBookmarkBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.RemoveBookmarks();
		public override bool IsEnabled(BookmarksCtxMenuContext context) => context.Operations.CanRemoveBookmarks;
	}

	[ExportMenuItem(Header = "res:RemoveAllBookmarksCommand", Icon = DsImagesAttribute.ClearBookmark, Group = MenuConstants.GROUP_CTX_BOOKMARKS_CMDS1, Order = 10)]
	sealed class RemoveAllBookmarksBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		RemoveAllBookmarksBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.RemoveMatchingBookmarks();
		public override bool IsEnabled(BookmarksCtxMenuContext context) => context.Operations.CanRemoveMatchingBookmarks;
	}

	[ExportMenuItem(Header = "res:EnableBookmarkCommand3", Icon = DsImagesAttribute.BookmarkDisabled, Group = MenuConstants.GROUP_CTX_BOOKMARKS_CMDS1, Order = 20)]
	sealed class EnableBookmarkBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		EnableBookmarkBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.EnableBookmarks();
		public override bool IsVisible(BookmarksCtxMenuContext context) => context.Operations.CanEnableBookmarks;
	}

	[ExportMenuItem(Header = "res:DisableBookmarkCommand3", Icon = DsImagesAttribute.BookmarkDisabled, Group = MenuConstants.GROUP_CTX_BOOKMARKS_CMDS1, Order = 30)]
	sealed class DisableBookmarkBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		DisableBookmarkBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.DisableBookmarks();
		public override bool IsVisible(BookmarksCtxMenuContext context) => context.Operations.CanDisableBookmarks;
	}

	[ExportMenuItem(Header = "res:ExportSelectedCommand", Icon = DsImagesAttribute.Open, Group = MenuConstants.GROUP_CTX_BOOKMARKS_EXPORT, Order = 0)]
	sealed class ExportSelectedBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		ExportSelectedBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.ExportSelectedBookmarks();
		public override bool IsEnabled(BookmarksCtxMenuContext context) => context.Operations.CanExportSelectedBookmarks;
	}

	[ExportMenuItem(Header = "res:ShowTokensCommand", Group = MenuConstants.GROUP_CTX_BOOKMARKS_OPTS, Order = 0)]
	sealed class ShowTokensBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		ShowTokensBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.ShowTokens = !context.Operations.ShowTokens;
		public override bool IsChecked(BookmarksCtxMenuContext context) => context.Operations.ShowTokens;
	}

	[ExportMenuItem(Header = "res:ShowModuleNamesCommand", Group = MenuConstants.GROUP_CTX_BOOKMARKS_OPTS, Order = 10)]
	sealed class ShowModuleNamesBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		ShowModuleNamesBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.ShowModuleNames = !context.Operations.ShowModuleNames;
		public override bool IsChecked(BookmarksCtxMenuContext context) => context.Operations.ShowModuleNames;
	}

	[ExportMenuItem(Header = "res:ShowParameterTypesCommand2", Group = MenuConstants.GROUP_CTX_BOOKMARKS_OPTS, Order = 20)]
	sealed class ShowParameterTypesBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		ShowParameterTypesBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.ShowParameterTypes = !context.Operations.ShowParameterTypes;
		public override bool IsChecked(BookmarksCtxMenuContext context) => context.Operations.ShowParameterTypes;
	}

	[ExportMenuItem(Header = "res:ShowParameterNamesCommand", Group = MenuConstants.GROUP_CTX_BOOKMARKS_OPTS, Order = 30)]
	sealed class ShowParameterNamesBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		ShowParameterNamesBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.ShowParameterNames = !context.Operations.ShowParameterNames;
		public override bool IsChecked(BookmarksCtxMenuContext context) => context.Operations.ShowParameterNames;
	}

	[ExportMenuItem(Header = "res:ShowDeclaringTypesCommand", Group = MenuConstants.GROUP_CTX_BOOKMARKS_OPTS, Order = 40)]
	sealed class ShowDeclaringTypesBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		ShowDeclaringTypesBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.ShowDeclaringTypes = !context.Operations.ShowDeclaringTypes;
		public override bool IsChecked(BookmarksCtxMenuContext context) => context.Operations.ShowDeclaringTypes;
	}

	[ExportMenuItem(Header = "res:ShowNamespacesCommand", Group = MenuConstants.GROUP_CTX_BOOKMARKS_OPTS, Order = 50)]
	sealed class ShowNamespacesBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		ShowNamespacesBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.ShowNamespaces = !context.Operations.ShowNamespaces;
		public override bool IsChecked(BookmarksCtxMenuContext context) => context.Operations.ShowNamespaces;
	}

	[ExportMenuItem(Header = "res:ShowReturnTypesCommand", Group = MenuConstants.GROUP_CTX_BOOKMARKS_OPTS, Order = 60)]
	sealed class ShowReturnTypesBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		ShowReturnTypesBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.ShowReturnTypes = !context.Operations.ShowReturnTypes;
		public override bool IsChecked(BookmarksCtxMenuContext context) => context.Operations.ShowReturnTypes;
	}

	[ExportMenuItem(Header = "res:ShowIntrinsicTypeKeywordsCommand", Group = MenuConstants.GROUP_CTX_BOOKMARKS_OPTS, Order = 70)]
	sealed class ShowTypeKeywordsBookmarksCtxMenuCommand : BookmarksCtxMenuCommand {
		[ImportingConstructor]
		ShowTypeKeywordsBookmarksCtxMenuCommand(Lazy<IBookmarksContent> bookmarkesContent)
			: base(bookmarkesContent) {
		}

		public override void Execute(BookmarksCtxMenuContext context) => context.Operations.ShowIntrinsicTypeKeywords = !context.Operations.ShowIntrinsicTypeKeywords;
		public override bool IsChecked(BookmarksCtxMenuContext context) => context.Operations.ShowIntrinsicTypeKeywords;
	}
}
