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
using System.Windows.Input;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Hex;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.ToolBars;

namespace dnSpy.AsmEditor.SaveModule {
	[ExportAutoLoaded]
	sealed class SaveModuleCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand SaveAllCommand = new RoutedCommand("SaveAll", typeof(SaveModuleCommandLoader));
		readonly Lazy<IUndoCommandService> undoCommandService;
		readonly Lazy<IDocumentSaver> documentSaver;

		[ImportingConstructor]
		SaveModuleCommandLoader(IWpfCommandService wpfCommandService, Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver) {
			this.undoCommandService = undoCommandService;
			this.documentSaver = documentSaver;

			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(SaveAllCommand, (s, e) => SaveAll_Execute(), (s, e) => e.CanExecute = SaveAll_CanExecute, ModifierKeys.Control | ModifierKeys.Shift, Key.S);
		}

		object[] GetDirtyDocs() => undoCommandService.Value.GetModifiedDocuments().ToArray();
		bool SaveAll_CanExecute => undoCommandService.Value.CachedHasModifiedDocuments;
		void SaveAll_Execute() => documentSaver.Value.Save(GetDirtyDocs());
	}

	[ExportToolBarButton(Icon = "SaveAll", ToolTip = "res:SaveAllToolBarToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_OPEN, Order = 10)]
	sealed class SaveAllToolbarCommand : ToolBarButtonCommand {
		SaveAllToolbarCommand()
			: base(SaveModuleCommandLoader.SaveAllCommand) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:SaveAllCommand", Icon = "SaveAll", InputGestureText = "res:ShortCutKeyCtrlShiftS", Group = MenuConstants.GROUP_APP_MENU_FILE_SAVE, Order = 30)]
	sealed class SaveAllCommand : MenuItemCommand {
		SaveAllCommand()
			: base(SaveModuleCommandLoader.SaveAllCommand) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Group = MenuConstants.GROUP_APP_MENU_FILE_SAVE, Order = 20)]
	sealed class SaveModuleCommand : FileMenuHandler {
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IUndoCommandService> undoCommandService;
		readonly Lazy<IHexDocumentService> hexDocumentService;
		readonly Lazy<IDocumentSaver> documentSaver;

		[ImportingConstructor]
		SaveModuleCommand(IDocumentTabService documentTabService, Lazy<IUndoCommandService> undoCommandService, Lazy<IHexDocumentService> hexDocumentService, Lazy<IDocumentSaver> documentSaver)
			: base(documentTabService.DocumentTreeView) {
			this.documentTabService = documentTabService;
			this.undoCommandService = undoCommandService;
			this.hexDocumentService = hexDocumentService;
			this.documentSaver = documentSaver;
		}

		HashSet<object> GetDocuments(IDocumentTreeNodeData[] nodes) {
			var hash = new HashSet<object>();

			foreach (var node in nodes) {
				var fileNode = node.GetDocumentNode();
				if (fileNode == null)
					continue;

				// Removed nodes could still be used, don't use them.
				var topNode = fileNode.GetTopNode();
				if (topNode == null || topNode.TreeNode.Parent == null)
					continue;

				bool added = false;

				if (fileNode.Document.ModuleDef != null) {
					var file = fileNode.Document;
					var uo = undoCommandService.Value.GetUndoObject(file);
					if (undoCommandService.Value.IsModified(uo)) {
						hash.Add(file);
						added = true;
					}
				}

				var doc = hexDocumentService.Value.TryGet(fileNode.Document.Filename);
				if (doc != null) {
					var uo = undoCommandService.Value.GetUndoObject(doc);
					if (undoCommandService.Value.IsModified(uo)) {
						hash.Add(doc);
						added = true;
					}
				}

				// If nothing was modified, just include the selected module
				if (!added && fileNode.Document.ModuleDef != null)
					hash.Add(fileNode.Document);
			}
			return new HashSet<object>(undoCommandService.Value.GetUniqueDocuments(hash));
		}

		public override bool IsVisible(AsmEditorContext context) => true;
		public override bool IsEnabled(AsmEditorContext context) => GetDocuments(context.Nodes).Count > 0;

		public override void Execute(AsmEditorContext context) {
			var asmNodes = GetDocuments(context.Nodes);
			documentSaver.Value.Save(asmNodes);
		}

		public override string GetHeader(AsmEditorContext context) => GetDocuments(context.Nodes).Count <= 1 ? dnSpy_AsmEditor_Resources.SaveModuleCommand : dnSpy_AsmEditor_Resources.SaveModulesCommand;
	}
}
