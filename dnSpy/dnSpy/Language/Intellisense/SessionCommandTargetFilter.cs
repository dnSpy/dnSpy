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
using System.Windows;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	sealed class SessionCommandTargetFilter : ICommandTargetFilter {
		readonly ICompletionSessionImpl completionSession;
		readonly IDnSpyWpfTextView dnSpyWpfTextView;
		readonly Window window;
		readonly int minimumCaretPosition;

		public SessionCommandTargetFilter(ICompletionSessionImpl completionSession) {
			if (completionSession == null)
				throw new ArgumentNullException(nameof(completionSession));
			this.completionSession = completionSession;
			this.dnSpyWpfTextView = completionSession.TextView as IDnSpyWpfTextView;
			Debug.Assert(dnSpyWpfTextView != null);
			this.window = dnSpyWpfTextView == null ? null : Window.GetWindow(dnSpyWpfTextView.VisualElement);

			dnSpyWpfTextView?.CommandTarget.AddFilter(this, CommandConstants.CMDTARGETFILTER_ORDER_DEFAULT_STATEMENTCOMPLETION);
			completionSession.TextView.Caret.PositionChanged += Caret_PositionChanged;
			completionSession.TextView.TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			if (window != null)
				window.LocationChanged += Window_LocationChanged;

			// Make sure that pressing backspace at start pos dismisses the session
			var span = completionSession.SelectedCompletionCollection.ApplicableTo.GetSpan(completionSession.TextView.TextSnapshot);
			minimumCaretPosition = span.Start.Position;
		}

		void TextBuffer_ChangedLowPriority(object sender, TextContentChangedEventArgs e) {
			if (!completionSession.IsDismissed) {
				completionSession.Filter();
				completionSession.Match();
			}
		}

		void Window_LocationChanged(object sender, EventArgs e) => completionSession.Dismiss();

		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) {
			if (e.NewPosition.VirtualSpaces > 0)
				completionSession.Dismiss();
			else {
				var pos = e.NewPosition.BufferPosition;
				var span = completionSession.SelectedCompletionCollection.ApplicableTo.GetSpan(pos.Snapshot);
				if (pos < minimumCaretPosition || pos < span.Start || pos > span.End)
					completionSession.Dismiss();
			}
		}

		public void Close() {
			dnSpyWpfTextView?.CommandTarget.RemoveFilter(this);
			completionSession.TextView.Caret.PositionChanged -= Caret_PositionChanged;
			completionSession.TextView.TextBuffer.ChangedLowPriority -= TextBuffer_ChangedLowPriority;
			if (window != null)
				window.LocationChanged -= Window_LocationChanged;
		}

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.UP:
				case TextEditorIds.DOWN:
				case TextEditorIds.PAGEUP:
				case TextEditorIds.PAGEDN:
				case TextEditorIds.BOL:
				case TextEditorIds.EOL:
				case TextEditorIds.TOPLINE:
				case TextEditorIds.BOTTOMLINE:
				case TextEditorIds.CANCEL:
				case TextEditorIds.RETURN:
				case TextEditorIds.DECREASEFILTER:
				case TextEditorIds.INCREASEFILTER:
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
				var presenterCmd = TryGetPresenterCommand((TextEditorIds)cmdId);
				if (presenterCmd != null) {
					if (completionSession.Presenter.HandleCommand(presenterCmd.Value))
						return CommandTargetStatus.Handled;
					if (presenterCmd.Value == PresenterCommandTargetCommand.Escape) {
						completionSession.Dismiss();
						return CommandTargetStatus.Handled;
					}
					if (presenterCmd.Value == PresenterCommandTargetCommand.Enter) {
						completionSession.Commit();
						return CommandTargetStatus.Handled;
					}
				}
				else {
					switch ((TextEditorIds)cmdId) {
					case TextEditorIds.TAB:
						if (completionSession.SelectedCompletionCollection.CurrentCompletion.IsSelected) {
							completionSession.Commit();
							// Don't include the tab character
							return CommandTargetStatus.Handled;
						}
						else
							completionSession.Dismiss();
						break;
					}
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		static PresenterCommandTargetCommand? TryGetPresenterCommand(TextEditorIds cmdId) {
			switch (cmdId) {
			case TextEditorIds.UP:				return PresenterCommandTargetCommand.Up;
			case TextEditorIds.DOWN:			return PresenterCommandTargetCommand.Down;
			case TextEditorIds.PAGEUP:			return PresenterCommandTargetCommand.PageUp;
			case TextEditorIds.PAGEDN:			return PresenterCommandTargetCommand.PageDown;
			case TextEditorIds.BOL:				return PresenterCommandTargetCommand.Home;
			case TextEditorIds.EOL:				return PresenterCommandTargetCommand.End;
			case TextEditorIds.TOPLINE:			return PresenterCommandTargetCommand.TopLine;
			case TextEditorIds.BOTTOMLINE:		return PresenterCommandTargetCommand.BottomLine;
			case TextEditorIds.CANCEL:			return PresenterCommandTargetCommand.Escape;
			case TextEditorIds.RETURN:			return PresenterCommandTargetCommand.Enter;
			case TextEditorIds.DECREASEFILTER:	return PresenterCommandTargetCommand.DecreaseFilterLevel;
			case TextEditorIds.INCREASEFILTER:	return PresenterCommandTargetCommand.IncreaseFilterLevel;
			default: return null;
			}
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		void IDisposable.Dispose() { }
	}
}
