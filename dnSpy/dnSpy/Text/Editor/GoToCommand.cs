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
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Properties;

namespace dnSpy.Text.Editor {
	[ExportCommandTargetFilterCreator(CommandConstants.CMDTARGETFILTER_ORDER_TEXT_EDITOR - 1)]
	sealed class GoToCommandTargetFilterCreator : ICommandTargetFilterCreator {
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		GoToCommandTargetFilterCreator(IMessageBoxManager messageBoxManager) {
			this.messageBoxManager = messageBoxManager;
		}

		public ICommandTargetFilter Create(object target) {
			var textView = target as ITextView;
			if (textView == null)
				return null;

			return new GoToCommandTargetFilter(textView, messageBoxManager);
		}
	}

	sealed class GoToCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;
		readonly IMessageBoxManager messageBoxManager;

		public GoToCommandTargetFilter(ITextView textView, IMessageBoxManager messageBoxManager) {
			this.textView = textView;
			this.messageBoxManager = messageBoxManager;
		}

		public CommandTargetStatus CanExecute(Guid group, int cmdId) =>
			group == CommandConstants.TextEditorGroup && (TextEditorIds)cmdId == TextEditorIds.GOTOLINE ? CommandTargetStatus.Handled : CommandTargetStatus.NotHandled;

		public CommandTargetStatus Execute(Guid group, int cmdId, object args = null) {
			object result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			if (group == CommandConstants.TextEditorGroup && (TextEditorIds)cmdId == TextEditorIds.GOTOLINE) {
				int lineNumber, columnNumber;
				if (args is int) {
					lineNumber = (int)args;
					columnNumber = 0;
				}
				else {
					if (!GetLineColumn(out lineNumber, out columnNumber))
						return CommandTargetStatus.Handled;
				}
				if ((uint)lineNumber >= textView.TextSnapshot.LineCount)
					lineNumber = textView.TextSnapshot.LineCount - 1;
				var line = textView.TextSnapshot.GetLineFromLineNumber(lineNumber);
				if ((uint)columnNumber > line.Length)
					columnNumber = line.Length;
				textView.Caret.MoveTo(line.Start + columnNumber);
				textView.Caret.EnsureVisible();
				return CommandTargetStatus.Handled;
			}
			return CommandTargetStatus.NotHandled;
		}

		bool GetLineColumn(out int chosenLine, out int chosenColumn) {
			var viewLine = textView.Caret.ContainingTextViewLine;
			var snapshotLine = viewLine.Start.GetContainingLine();

			var res = messageBoxManager.Ask(dnSpy_Resources.GoToLine_Label, null, dnSpy_Resources.GoToLine_Title, s => {
				int? line, column;
				TryGetRowCol(s, snapshotLine.LineNumber, out line, out column);
				return Tuple.Create(line.Value, column.Value);
			}, s => {
				int? line, column;
				return TryGetRowCol(s, snapshotLine.LineNumber, out line, out column);
			});
			if (res == null) {
				chosenLine = 0;
				chosenColumn = 0;
				return false;
			}

			chosenLine = res.Item1;
			chosenColumn = res.Item2;
			return true;
		}

		string TryGetRowCol(string s, int currentLine, out int? line, out int? column) {
			line = null;
			column = null;
			Match match;
			if ((match = goToLineRegex1.Match(s)) != null && match.Groups.Count == 4) {
				line = TryParseOneBasedToZeroBased(match.Groups[1].Value);
				column = match.Groups[3].Value != string.Empty ? TryParseOneBasedToZeroBased(match.Groups[3].Value) : 0;
			}
			else if ((match = goToLineRegex2.Match(s)) != null && match.Groups.Count == 2) {
				line = currentLine;
				column = TryParseOneBasedToZeroBased(match.Groups[1].Value);
			}
			if (line == null || column == null) {
				if (string.IsNullOrWhiteSpace(s))
					return dnSpy_Resources.GoToLine_EnterLineNum;
				return string.Format(dnSpy_Resources.GoToLine_InvalidLine, s);
			}
			return string.Empty;
		}
		static readonly Regex goToLineRegex1 = new Regex(@"^\s*(\d+)\s*(,\s*(\d+))?\s*$");
		static readonly Regex goToLineRegex2 = new Regex(@"^\s*,\s*(\d+)\s*$");

		static int? TryParseOneBasedToZeroBased(string valText) {
			int val;
			return int.TryParse(valText, out val) && val > 0 ? (int?)(val - 1) : null;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
