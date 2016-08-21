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
using System.Diagnostics;
using System.Windows.Input;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.AsmEditor.Commands {
	sealed class CodeContextMenuHandlerCommandProxy : ICommand {
		readonly CodeContextMenuHandler command;
		readonly IFileTabManager fileTabManager;

		public CodeContextMenuHandlerCommandProxy(CodeContextMenuHandler command, IFileTabManager fileTabManager) {
			this.command = command;
			this.fileTabManager = fileTabManager;
		}

		CodeContext CreateContext() {
			var documentViewer = fileTabManager.ActiveTab.TryGetDocumentViewer();
			if (documentViewer == null)
				return null;
			if (!documentViewer.UIObject.IsKeyboardFocusWithin)
				return null;

			var refInfo = documentViewer.SelectedReference;
			if (refInfo == null)
				return null;

			var node = fileTabManager.FileTreeView.FindNode(refInfo.Value.Data.Reference);
			var nodes = node == null ? Array.Empty<IFileTreeNodeData>() : new IFileTreeNodeData[] { node };
			return new CodeContext(nodes, refInfo.Value.Data.IsDefinition, null);
		}

		event EventHandler ICommand.CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		bool ICommand.CanExecute(object parameter) {
			var ctx = CreateContext();
			return ctx != null && command.IsVisible(ctx) && command.IsEnabled(ctx);
		}

		void ICommand.Execute(object parameter) {
			var ctx = CreateContext();
			Debug.Assert(ctx != null);
			if (ctx != null)
				command.Execute(ctx);
		}
	}
}
