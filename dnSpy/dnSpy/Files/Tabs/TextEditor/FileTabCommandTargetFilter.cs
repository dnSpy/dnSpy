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

namespace dnSpy.Files.Tabs.TextEditor {
	sealed class FileTabCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;

		public FileTabCommandTargetFilter(ITextView textView) {
			this.textView = textView;
		}

		TextEditorControl TryGetInstance() =>
			__textEditorControl ?? (__textEditorControl = TextEditorControl.TryGetInstance(textView));
		TextEditorControl __textEditorControl;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (TryGetInstance() == null)
				return CommandTargetStatus.NotHandled;

			if (group == FileTabCommandConstants.FileTabGroup) {
				switch ((FileTabIds)cmdId) {
				case FileTabIds.MoveToNextReference:
				case FileTabIds.MoveToPreviousReference:
				case FileTabIds.MoveToNextDefinition:
				case FileTabIds.MoveToPreviousDefinition:
				case FileTabIds.FollowReference:
				case FileTabIds.FollowReferenceNewTab:
				case FileTabIds.ClearMarkedReferencesAndToolTip:
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(FileTabIds)} id: {(FileTabIds)cmdId}");
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			var tabControl = TryGetInstance();
			if (tabControl == null)
				return CommandTargetStatus.NotHandled;

			if (group == FileTabCommandConstants.FileTabGroup) {
				switch ((FileTabIds)cmdId) {
				case FileTabIds.MoveToNextReference:
					tabControl.MoveReference(true);
					return CommandTargetStatus.Handled;

				case FileTabIds.MoveToPreviousReference:
					tabControl.MoveReference(false);
					return CommandTargetStatus.Handled;

				case FileTabIds.MoveToNextDefinition:
					tabControl.MoveToNextDefinition(true);
					return CommandTargetStatus.Handled;

				case FileTabIds.MoveToPreviousDefinition:
					tabControl.MoveToNextDefinition(false);
					return CommandTargetStatus.Handled;

				case FileTabIds.FollowReference:
					tabControl.FollowReference();
					return CommandTargetStatus.Handled;

				case FileTabIds.FollowReferenceNewTab:
					tabControl.FollowReferenceNewTab();
					return CommandTargetStatus.Handled;

				case FileTabIds.ClearMarkedReferencesAndToolTip:
					tabControl.ClearMarkedReferencesAndToolTip();
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(FileTabIds)} id: {(FileTabIds)cmdId}");
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
