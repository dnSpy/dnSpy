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
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Documents.Tabs.DocViewer.Settings;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Documents.Tabs.DocViewer {
	[ExportAutoLoaded]
	sealed class WordWrapInit : IAutoLoaded {
		readonly Lazy<IDocumentViewerOptionsService> documentViewerOptionsService;

		[ImportingConstructor]
		WordWrapInit(IAppWindow appWindow, Lazy<IDocumentViewerOptionsService> documentViewerOptionsService) {
			this.documentViewerOptionsService = documentViewerOptionsService;
			appWindow.MainWindow.KeyDown += MainWindow_KeyDown;
		}

		void MainWindow_KeyDown(object? sender, KeyEventArgs e) {
			if (!waitingForSecondKey && e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.E) {
				waitingForSecondKey = true;
				e.Handled = true;
				return;
			}
			if (waitingForSecondKey && (e.KeyboardDevice.Modifiers == ModifierKeys.Control || e.KeyboardDevice.Modifiers == ModifierKeys.None) && e.Key == Key.W) {
				waitingForSecondKey = false;
				e.Handled = true;
				documentViewerOptionsService.Value.Default.WordWrapStyle ^= WordWrapStyles.WordWrap;
				return;
			}

			waitingForSecondKey = false;
		}
		bool waitingForSecondKey;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:WordWrapHeader", Icon = DsImagesAttribute.WordWrap, InputGestureText = "res:ShortCutKeyCtrlECtrlW", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 0)]
	sealed class WordWrapCommand : MenuItemBase {
		readonly Lazy<IDocumentViewerOptionsService> documentViewerOptionsService;

		[ImportingConstructor]
		WordWrapCommand(Lazy<IDocumentViewerOptionsService> documentViewerOptionsService) => this.documentViewerOptionsService = documentViewerOptionsService;

		public override void Execute(IMenuItemContext context) => documentViewerOptionsService.Value.Default.WordWrapStyle ^= WordWrapStyles.WordWrap;
		public override bool IsChecked(IMenuItemContext context) => (documentViewerOptionsService.Value.Default.WordWrapStyle & WordWrapStyles.WordWrap) != 0;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:HighlightLine", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 10)]
	sealed class HighlightCurrentLineCommand : MenuItemBase {
		readonly Lazy<IDocumentViewerOptionsService> documentViewerOptionsService;

		[ImportingConstructor]
		HighlightCurrentLineCommand(Lazy<IDocumentViewerOptionsService> documentViewerOptionsService) => this.documentViewerOptionsService = documentViewerOptionsService;

		public override void Execute(IMenuItemContext context) => documentViewerOptionsService.Value.Default.EnableHighlightCurrentLine = !documentViewerOptionsService.Value.Default.EnableHighlightCurrentLine;
		public override bool IsChecked(IMenuItemContext context) => documentViewerOptionsService.Value.Default.EnableHighlightCurrentLine;
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:CopyKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_EDITOR, Order = 0)]
	internal sealed class CopyCodeCtxMenuCommand : MenuItemCommand {
		public CopyCodeCtxMenuCommand()
			: base(ApplicationCommands.Copy) {
		}

		public override bool IsVisible(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return false;
			var uiContext = context.Find<IDocumentViewer>();
			return !(uiContext is null) && !uiContext.Selection.IsEmpty;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:FindCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:FindKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_FIND, Order = 0)]
	sealed class FindInCodeCommand : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			var elem = GetInputElement();
			if (!(elem is null))
				ApplicationCommands.Find.Execute(null, elem);
		}

		public override bool IsEnabled(IMenuItemContext context) => !(GetInputElement() is null);

		IInputElement? GetInputElement() {
			var elem = Keyboard.FocusedElement;
			return !(elem is null) && ApplicationCommands.Find.CanExecute(null, elem) ? elem : null;
		}
	}

	abstract class DocumentViewerCommandTargetMenuItemBase : CommandTargetMenuItemBase {
		protected DocumentViewerCommandTargetMenuItemBase(StandardIds cmdId)
			: base(cmdId) {
		}

		protected override ICommandTarget? GetCommandTarget(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return null;
			return context.Find<IDocumentViewer>()?.TextView.CommandTarget;
		}
	}

	[ExportMenuItem(Header = "res:FindCommand2", Icon = DsImagesAttribute.Search, InputGestureText = "res:FindKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_EDITOR, Order = 10)]
	sealed class FindInCodeContextMenuEntry : DocumentViewerCommandTargetMenuItemBase {
		FindInCodeContextMenuEntry()
			: base(StandardIds.Find) {
		}
	}

	[ExportMenuItem(Header = "res:IncrementalSearchCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:ShortCutKeyCtrlI", Group = MenuConstants.GROUP_CTX_DOCVIEWER_EDITOR, Order = 20)]
	sealed class IncrementalSearchForwardContextMenuEntry : DocumentViewerCommandTargetMenuItemBase {
		IncrementalSearchForwardContextMenuEntry()
			: base(StandardIds.IncrementalSearchForward) {
		}
	}
}
