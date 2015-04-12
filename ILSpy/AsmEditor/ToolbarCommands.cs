
using System;
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
