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
using System.Diagnostics;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionSessionCommandTargetFilter : ICommandTargetFilter {
		readonly ICompletionSession completionSession;
		readonly IDsWpfTextView dsWpfTextView;
		readonly int minimumCaretPosition;

		public CompletionSessionCommandTargetFilter(ICompletionSession completionSession) {
			this.completionSession = completionSession ?? throw new ArgumentNullException(nameof(completionSession));
			dsWpfTextView = completionSession.TextView as IDsWpfTextView;
			Debug.Assert(dsWpfTextView != null);

			dsWpfTextView?.CommandTarget.AddFilter(this, CommandTargetFilterOrder.IntellisenseDefaultStatmentCompletion);
			completionSession.TextView.Caret.PositionChanged += Caret_PositionChanged;

			// Make sure that pressing backspace at start pos dismisses the session
			var span = completionSession.SelectedCompletionSet?.ApplicableTo.GetSpan(completionSession.TextView.TextSnapshot);
			minimumCaretPosition = span == null ? 0 : span.Value.Start.Position;
		}

		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) {
			if (e.NewPosition.VirtualSpaces > 0)
				completionSession.Dismiss();
			else {
				var pos = e.NewPosition.BufferPosition;
				var span = completionSession.SelectedCompletionSet?.ApplicableTo.GetSpan(pos.Snapshot);
				if (span == null || pos < minimumCaretPosition || pos < span.Value.Start || pos > span.Value.End)
					completionSession.Dismiss();
				else if (pos == span.Value.Start.Position) {
					// This matches what VS does. It prevents you from accidentally committing
					// something when you select the current input text by pressing Shift+Home
					// and then pressing eg. " or some other commit-character.
					var curr = completionSession.SelectedCompletionSet.SelectionStatus;
					completionSession.SelectedCompletionSet.SelectionStatus = new CompletionSelectionStatus(curr.Completion, isSelected: false, isUnique: curr.IsUnique);
				}
			}
		}

		public void Close() {
			dsWpfTextView?.CommandTarget.RemoveFilter(this);
			completionSession.TextView.Caret.PositionChanged -= Caret_PositionChanged;
		}

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.TAB:
					return CommandTargetStatus.Handled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args) {
			object result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			if (group == CommandConstants.StandardGroup) {
				switch ((StandardIds)cmdId) {
				case StandardIds.Paste:
					completionSession.Dismiss();
					return CommandTargetStatus.NotHandled;
				}
			}
			else if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.TAB:
					if (completionSession.SelectedCompletionSet?.SelectionStatus.IsSelected == true) {
						completionSession.Commit();
						// Don't include the tab character
						return CommandTargetStatus.Handled;
					}
					else
						completionSession.Dismiss();
					break;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		void IDisposable.Dispose() { }
	}
}
