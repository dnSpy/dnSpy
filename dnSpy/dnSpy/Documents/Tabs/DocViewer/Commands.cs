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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Documents.Tabs.DocViewer {
	[ExportAutoLoaded]
	sealed class WordWrapInit : IAutoLoaded {
		readonly IEditorOptions editorOptions;
		readonly IAppSettings appSettings;
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		WordWrapInit(IAppWindow appWindow, IEditorOptionsFactoryService editorOptionsFactoryService, IAppSettings appSettings, IMessageBoxService messageBoxService) {
			this.editorOptions = editorOptionsFactoryService.GlobalOptions;
			this.appSettings = appSettings;
			this.messageBoxService = messageBoxService;
			appWindow.MainWindow.KeyDown += MainWindow_KeyDown;
		}

		void MainWindow_KeyDown(object sender, KeyEventArgs e) {
			if (!waitingForSecondKey && e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.E) {
				waitingForSecondKey = true;
				e.Handled = true;
				return;
			}
			if (waitingForSecondKey && (e.KeyboardDevice.Modifiers == ModifierKeys.Control || e.KeyboardDevice.Modifiers == ModifierKeys.None) && e.Key == Key.W) {
				waitingForSecondKey = false;
				e.Handled = true;
				ToggleWordWrap();
				return;
			}

			waitingForSecondKey = false;
		}
		bool waitingForSecondKey;

		void ToggleWordWrap() {
			editorOptions.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, editorOptions.WordWrapStyle() ^ WordWrapStyles.WordWrap);
			if ((editorOptions.WordWrapStyle() & WordWrapStyles.WordWrap) != 0 && appSettings.UseNewRenderer_TextEditor)
				messageBoxService.ShowIgnorableMessage(new Guid("AA6167DA-827C-49C6-8EF3-0797FE8FC5E6"), dnSpy_Resources.TextEditorNewFormatterWarningMsg);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:WordWrapHeader", Icon = "WordWrap", InputGestureText = "res:ShortCutKeyCtrlECtrlW", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 0)]
	sealed class WordWrapCommand : MenuItemBase {
		readonly IEditorOptions editorOptions;

		[ImportingConstructor]
		WordWrapCommand(IEditorOptionsFactoryService editorOptionsFactoryService) {
			this.editorOptions = editorOptionsFactoryService.GlobalOptions;
		}

		public override void Execute(IMenuItemContext context) => editorOptions.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, editorOptions.WordWrapStyle() ^ WordWrapStyles.WordWrap);
		public override bool IsChecked(IMenuItemContext context) => (editorOptions.WordWrapStyle() & WordWrapStyles.WordWrap) != 0;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:HighlightLine", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 10)]
	sealed class HighlightCurrentLineCommand : MenuItemBase {
		readonly IEditorOptions editorOptions;

		[ImportingConstructor]
		HighlightCurrentLineCommand(IEditorOptionsFactoryService editorOptionsFactoryService) {
			this.editorOptions = editorOptionsFactoryService.GlobalOptions;
		}

		public override bool IsChecked(IMenuItemContext context) => editorOptions.GetOptionValue(DefaultWpfViewOptions.EnableHighlightCurrentLineId);
		public override void Execute(IMenuItemContext context) => editorOptions.SetOptionValue(DefaultWpfViewOptions.EnableHighlightCurrentLineId, !editorOptions.GetOptionValue(DefaultWpfViewOptions.EnableHighlightCurrentLineId));
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = "Copy", InputGestureText = "res:CopyKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_EDITOR, Order = 0)]
	internal sealed class CopyCodeCtxMenuCommand : MenuItemCommand {
		public CopyCodeCtxMenuCommand()
			: base(ApplicationCommands.Copy) {
		}

		public override bool IsVisible(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return false;
			var uiContext = context.Find<IDocumentViewer>();
			return uiContext != null && !uiContext.Selection.IsEmpty;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:FindCommand", Icon = "Find", InputGestureText = "res:FindKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_FIND, Order = 0)]
	sealed class FindInCodeCommand : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			var elem = GetInputElement();
			if (elem != null)
				ApplicationCommands.Find.Execute(null, elem);
		}

		public override bool IsEnabled(IMenuItemContext context) => GetInputElement() != null;

		IInputElement GetInputElement() {
			var elem = Keyboard.FocusedElement;
			return elem != null && ApplicationCommands.Find.CanExecute(null, elem) ? elem : null;
		}
	}

	abstract class DocumentViewerCommandTargetMenuItemBase : CommandTargetMenuItemBase {
		protected DocumentViewerCommandTargetMenuItemBase(StandardIds cmdId)
			: base(cmdId) {
		}

		protected override ICommandTarget GetCommandTarget(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return null;
			return context.Find<IDocumentViewer>()?.TextView.CommandTarget;
		}
	}

	[ExportMenuItem(Header = "res:FindCommand2", Icon = "Find", InputGestureText = "res:FindKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_EDITOR, Order = 10)]
	sealed class FindInCodeContexMenuEntry : DocumentViewerCommandTargetMenuItemBase {
		FindInCodeContexMenuEntry()
			: base(StandardIds.Find) {
		}
	}

	[ExportMenuItem(Header = "res:IncrementalSearchCommand", Icon = "Find", InputGestureText = "res:ShortCutKeyCtrlI", Group = MenuConstants.GROUP_CTX_DOCVIEWER_EDITOR, Order = 20)]
	sealed class IncrementalSearchForwardContexMenuEntry : DocumentViewerCommandTargetMenuItemBase {
		IncrementalSearchForwardContexMenuEntry()
			: base(StandardIds.IncrementalSearchForward) {
		}
	}
}
