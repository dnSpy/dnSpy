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

using System.Windows.Input;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;

namespace dnSpy.Commands {
	[ExportMainMenuCommand(Menu = "_Edit", MenuHeader = "_Find", MenuIcon = "Find", MenuCategory = "Search", MenuInputGestureText = "Ctrl+F", MenuOrder = 2090)]
	sealed class FindInCodeCommand : CommandWrapper {
		public FindInCodeCommand()
			: base(ApplicationCommands.Find) {
		}
	}

	[ExportContextMenuEntry(Header = "Find", Order = 1010, Icon = "Find", Category = "Editor", InputGestureText = "Ctrl+F")]
	sealed class FindInCodeContexMenuEntry : IContextMenuEntry {
		public void Execute(ContextMenuEntryContext context) {
			if (ApplicationCommands.Find.CanExecute(null, MainWindow.Instance))
				ApplicationCommands.Find.Execute(null, MainWindow.Instance);
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			return true;
		}

		public bool IsVisible(ContextMenuEntryContext context) {
			return context.Element is DecompilerTextView;
		}
	}
}
