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

using System.Windows.Input;

namespace dnSpy.AsmEditor.UndoRedo {
	static class UndoRoutedCommands {
		public static readonly RoutedUICommand Undo;
		public static readonly RoutedUICommand Redo;

		static UndoRoutedCommands() {
			// Create our own Undo/Redo commands because if a text box has the focus (eg. search
			// pane), it will send undo/redo events and the undo/redo toolbar buttons will be
			// enabled/disabled based on the text box's undo/redo state, not the asm editor's
			// undo/redo state.
			Undo = new RoutedUICommand("Undo", "Undo", typeof(UndoRoutedCommands), new InputGestureCollection(ApplicationCommands.Undo.InputGestures));
			Redo = new RoutedUICommand("Redo", "Redo", typeof(UndoRoutedCommands), new InputGestureCollection(ApplicationCommands.Redo.InputGestures));
		}
	}
}
