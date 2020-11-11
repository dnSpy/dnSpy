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
using System.Linq;
using dnSpy.AsmEditor.Assembly;
using dnSpy.AsmEditor.SaveModule;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Menus;

namespace dnSpy.AsmEditor.Commands {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:CloseAllMissingFilesCommand", Group = MenuConstants.GROUP_APP_MENU_FILE_OPEN, Order = 60)]
	sealed class CloseAllMissingFilesCommand : MenuItemBase {
		readonly IDocumentTreeView documentTreeView;
		readonly Lazy<IUndoCommandService> undoCommandService;
		readonly Lazy<IDocumentSaver> documentSaver;
		readonly IAppService appService;

		[ImportingConstructor]
		CloseAllMissingFilesCommand(IDocumentTreeView documentTreeView, Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver, IAppService appService) {
			this.documentTreeView = documentTreeView;
			this.undoCommandService = undoCommandService;
			this.documentSaver = documentSaver;
			this.appService = appService;
		}

		UnknownDocumentNode[]? GetNodes() {
			var nodes = documentTreeView.TreeView.Root.DataChildren.OfType<UnknownDocumentNode>().ToArray();
			return nodes.Length == 0 ? null : nodes;
		}

		public override bool IsEnabled(IMenuItemContext context) => GetNodes() is not null;
		public override void Execute(IMenuItemContext context) {
			var nodes = GetNodes();
			if (nodes is not null)
				RemoveAssemblyCommand.Execute(undoCommandService, documentSaver, appService, nodes);
		}
	}
}
