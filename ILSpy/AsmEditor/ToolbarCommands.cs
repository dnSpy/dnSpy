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

using System.Windows;
using System.Windows.Input;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	[ExportToolbarCommand(ToolTip = "Undo (Ctrl+Z)",
						  ToolbarIcon = "Images/Undo.png",
						  ToolbarCategory = "AsmEdit",
						  ToolbarOrder = 5000)]
	sealed class UndoAsmEdCommand : CommandWrapper
	{
		public UndoAsmEdCommand()
			: base(ApplicationCommands.Undo)
		{
		}
	}

	[ExportToolbarCommand(ToolTip = "Redo (Ctrl+Y)",
						  ToolbarIcon = "Images/Redo.png",
						  ToolbarCategory = "AsmEdit",
						  ToolbarOrder = 5010)]
	sealed class RedoAsmEdCommand : CommandWrapper
	{
		public RedoAsmEdCommand()
			: base(ApplicationCommands.Redo)
		{
		}
	}

	[ExportToolbarCommand(ToolTip = "Clear Undo/Redo History",
						  ToolbarIcon = "Images/DeleteHistory.png",
						  ToolbarCategory = "AsmEdit",
						  ToolbarOrder = 5020)]
	sealed class DeleteHistoryAsmEdCommand : TreeNodeCommand
	{
		public DeleteHistoryAsmEdCommand()
			: base(true)
		{
		}

		protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
		{
			return UndoCommandManager.Instance.CanUndo ||
				UndoCommandManager.Instance.CanRedo;
		}

		protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
		{
			var res = MainWindow.Instance.ShowIgnorableMessageBox("undo: clear history", "Do you want to clear the undo/redo history?", MessageBoxButton.YesNo);
			if (res == null || res == MsgBoxButton.OK)
				UndoCommandManager.Instance.Clear();
		}
	}
}
