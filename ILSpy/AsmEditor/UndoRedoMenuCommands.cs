
using System.Windows.Input;

namespace ICSharpCode.ILSpy.AsmEditor
{
	[ExportMainMenuCommand(Header = "Undo",
						   InputGestureText = "Ctrl+Z",
						   MenuIcon = "Images/Undo.png",
						   Menu = "_Edit",
						   MenuCategory = "UndoRedo",
						   MenuOrder = 2000)]
	sealed class UndoMainMenuEntryCommand : CommandWrapper
	{
		public UndoMainMenuEntryCommand()
			: base(ApplicationCommands.Undo)
		{
		}
	}

	[ExportMainMenuCommand(Header = "Redo",
						   InputGestureText = "Ctrl+Y",
						   MenuIcon = "Images/Redo.png",
						   Menu = "_Edit",
						   MenuCategory = "UndoRedo",
						   MenuOrder = 2010)]
	sealed class RedoMainMenuEntryCommand : CommandWrapper
	{
		public RedoMainMenuEntryCommand()
			: base(ApplicationCommands.Redo)
		{
		}
	}
}
