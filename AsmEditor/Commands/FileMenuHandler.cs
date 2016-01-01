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
using System.Linq;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Menus;

namespace dnSpy.AsmEditor.Commands {
	abstract class FileMenuHandler : MenuItemBase<AsmEditorContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		readonly IFileTreeView fileTreeView;

		protected FileMenuHandler(IFileTreeView fileTreeView) {
			this.fileTreeView = fileTreeView;
		}

		protected sealed override AsmEditorContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.APP_MENU_FILE_GUID))
				return null;
			return CreateContext();
		}

		public AsmEditorContext CreateContext() {
			return new AsmEditorContext(fileTreeView.TreeView.SelectedItems.OfType<IFileTreeNodeData>().ToArray());
		}
	}
}
