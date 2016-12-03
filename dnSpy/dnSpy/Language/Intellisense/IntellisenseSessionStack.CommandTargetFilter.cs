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
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Language.Intellisense {
	sealed partial class IntellisenseSessionStack {
		sealed class CommandTargetFilter : ICommandTargetFilter {
			readonly IntellisenseSessionStack owner;
			readonly IDsWpfTextView wpfTextView;
			bool hasHookedKeyboard;

			public CommandTargetFilter(IntellisenseSessionStack owner) {
				this.owner = owner;
				wpfTextView = owner.wpfTextView as IDsWpfTextView;
				Debug.Assert(wpfTextView != null);
			}

			public void HookKeyboard() {
				Debug.Assert(!hasHookedKeyboard);
				if (hasHookedKeyboard)
					return;
				if (wpfTextView == null)
					return;
				wpfTextView.CommandTarget.AddFilter(this, CommandTargetFilterOrder.IntellisenseSessionStack);
				hasHookedKeyboard = true;
			}

			public void UnhookKeyboard() {
				if (!hasHookedKeyboard)
					return;
				if (wpfTextView == null)
					return;
				wpfTextView.CommandTarget.RemoveFilter(this);
				hasHookedKeyboard = false;
			}

			public CommandTargetStatus CanExecute(Guid group, int cmdId) {
				if (group == CommandConstants.TextEditorGroup) {
					if (TryGetIntellisenseKeyboardCommand((TextEditorIds)cmdId) != null)
						return CommandTargetStatus.Handled;
				}
				return CommandTargetStatus.NotHandled;
			}

			public CommandTargetStatus Execute(Guid group, int cmdId, object args) {
				object result = null;
				return Execute(group, cmdId, args, ref result);
			}

			public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
				if (group == CommandConstants.TextEditorGroup) {
					var command = TryGetIntellisenseKeyboardCommand((TextEditorIds)cmdId);
					if (command != null && owner.ExecuteKeyboardCommand(command.Value))
						return CommandTargetStatus.Handled;
				}
				return CommandTargetStatus.NotHandled;
			}

			static IntellisenseKeyboardCommand? TryGetIntellisenseKeyboardCommand(TextEditorIds cmdId) {
				switch (cmdId) {
				case TextEditorIds.UP:				return IntellisenseKeyboardCommand.Up;
				case TextEditorIds.DOWN:			return IntellisenseKeyboardCommand.Down;
				case TextEditorIds.PAGEUP:			return IntellisenseKeyboardCommand.PageUp;
				case TextEditorIds.PAGEDN:			return IntellisenseKeyboardCommand.PageDown;
				case TextEditorIds.BOL:				return IntellisenseKeyboardCommand.Home;
				case TextEditorIds.EOL:				return IntellisenseKeyboardCommand.End;
				case TextEditorIds.TOPLINE:			return IntellisenseKeyboardCommand.TopLine;
				case TextEditorIds.BOTTOMLINE:		return IntellisenseKeyboardCommand.BottomLine;
				case TextEditorIds.CANCEL:			return IntellisenseKeyboardCommand.Escape;
				case TextEditorIds.RETURN:			return IntellisenseKeyboardCommand.Enter;
				case TextEditorIds.DECREASEFILTER:	return IntellisenseKeyboardCommand.DecreaseFilterLevel;
				case TextEditorIds.INCREASEFILTER:	return IntellisenseKeyboardCommand.IncreaseFilterLevel;
				default: return null;
				}
			}

			public void SetNextCommandTarget(ICommandTarget commandTarget) { }
			void IDisposable.Dispose() { }

			public void Destroy() => UnhookKeyboard();
		}
	}
}
