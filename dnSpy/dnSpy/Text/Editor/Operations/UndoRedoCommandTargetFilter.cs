/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor.Operations;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Text.Editor.Operations {
	sealed class UndoRedoCommandTargetFilter : ICommandTargetFilter {
		// Always reference the ITextUndoHistory instance from the TextBufferUndoHistory
		// property since the property can return a new instance if the history was unregistered
		// (eg. to clear undo/redo history)
		readonly ITextViewUndoManager textViewUndoManager;

		bool IsReadOnly => textViewUndoManager.TextView.Options.DoesViewProhibitUserInput();

		public UndoRedoCommandTargetFilter(ITextViewUndoManager textViewUndoManager) => this.textViewUndoManager = textViewUndoManager ?? throw new ArgumentNullException(nameof(textViewUndoManager));

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (group == CommandConstants.StandardGroup) {
				switch ((StandardIds)cmdId) {
				case StandardIds.Undo:
					if (!IsReadOnly && textViewUndoManager.TextViewUndoHistory.CanUndo)
						return CommandTargetStatus.Handled;
					break;

				case StandardIds.Redo:
					if (!IsReadOnly && textViewUndoManager.TextViewUndoHistory.CanRedo)
						return CommandTargetStatus.Handled;
					break;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args = null) {
			object result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			if (group == CommandConstants.StandardGroup) {
				switch ((StandardIds)cmdId) {
				case StandardIds.Undo:
					if (!IsReadOnly && textViewUndoManager.TextViewUndoHistory.CanUndo) {
						textViewUndoManager.TextViewUndoHistory.Undo(1);
						return CommandTargetStatus.Handled;
					}
					break;

				case StandardIds.Redo:
					if (!IsReadOnly && textViewUndoManager.TextViewUndoHistory.CanRedo) {
						textViewUndoManager.TextViewUndoHistory.Redo(1);
						return CommandTargetStatus.Handled;
					}
					break;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
