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
using System.Diagnostics;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Hex.Editor;
using VSLI = Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Hex.Intellisense {
	sealed partial class HexIntellisenseSessionStackImpl {
		sealed class CommandTargetFilter : ICommandTargetFilter {
			readonly HexIntellisenseSessionStackImpl owner;
			readonly WpfHexView wpfHexView;
			bool hasHookedKeyboard;

			public CommandTargetFilter(HexIntellisenseSessionStackImpl owner) {
				this.owner = owner;
				wpfHexView = owner.wpfHexView;
			}

			public void HookKeyboard() {
				Debug.Assert(!hasHookedKeyboard);
				if (hasHookedKeyboard)
					return;
				if (wpfHexView is null)
					return;
				wpfHexView.CommandTarget.AddFilter(this, CommandTargetFilterOrder.HexIntellisenseSessionStack);
				hasHookedKeyboard = true;
			}

			public void UnhookKeyboard() {
				if (!hasHookedKeyboard)
					return;
				if (wpfHexView is null)
					return;
				wpfHexView.CommandTarget.RemoveFilter(this);
				hasHookedKeyboard = false;
			}

			public CommandTargetStatus CanExecute(Guid group, int cmdId) {
				if (group == CommandConstants.HexEditorGroup) {
					if (TryGetIntellisenseKeyboardCommand((HexEditorIds)cmdId) is not null)
						return CommandTargetStatus.Handled;
				}
				return CommandTargetStatus.NotHandled;
			}

			public CommandTargetStatus Execute(Guid group, int cmdId, object? args) {
				object? result = null;
				return Execute(group, cmdId, args, ref result);
			}

			public CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) {
				if (group == CommandConstants.HexEditorGroup) {
					var command = TryGetIntellisenseKeyboardCommand((HexEditorIds)cmdId);
					if (command is not null && owner.ExecuteKeyboardCommand(command.Value))
						return CommandTargetStatus.Handled;
				}
				return CommandTargetStatus.NotHandled;
			}

			static VSLI.IntellisenseKeyboardCommand? TryGetIntellisenseKeyboardCommand(HexEditorIds cmdId) {
				switch (cmdId) {
				case HexEditorIds.UP:				return VSLI.IntellisenseKeyboardCommand.Up;
				case HexEditorIds.DOWN:				return VSLI.IntellisenseKeyboardCommand.Down;
				case HexEditorIds.PAGEUP:			return VSLI.IntellisenseKeyboardCommand.PageUp;
				case HexEditorIds.PAGEDN:			return VSLI.IntellisenseKeyboardCommand.PageDown;
				case HexEditorIds.BOL:				return VSLI.IntellisenseKeyboardCommand.Home;
				case HexEditorIds.EOL:				return VSLI.IntellisenseKeyboardCommand.End;
				case HexEditorIds.TOPLINE:			return VSLI.IntellisenseKeyboardCommand.TopLine;
				case HexEditorIds.BOTTOMLINE:		return VSLI.IntellisenseKeyboardCommand.BottomLine;
				case HexEditorIds.CANCEL:			return VSLI.IntellisenseKeyboardCommand.Escape;
				case HexEditorIds.RETURN:			return VSLI.IntellisenseKeyboardCommand.Enter;
				case HexEditorIds.DECREASEFILTER:	return VSLI.IntellisenseKeyboardCommand.DecreaseFilterLevel;
				case HexEditorIds.INCREASEFILTER:	return VSLI.IntellisenseKeyboardCommand.IncreaseFilterLevel;
				default: return null;
				}
			}

			public void SetNextCommandTarget(ICommandTarget commandTarget) { }
			void IDisposable.Dispose() { }

			public void Destroy() => UnhookKeyboard();
		}
	}
}
