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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Editor.Operations {
	sealed class EditorOperations : IEditorOperations2 {
		public bool CanCut {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public bool CanDelete {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public bool CanPaste {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public IEditorOptions Options => TextView.Options;

		public ITrackingSpan ProvisionalCompositionSpan {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public string SelectedText => TextView.Selection.GetText();
		public ITextView TextView { get; }
		ITextSelection Selection => TextView.Selection;
		ITextCaret Caret => TextView.Caret;
		ITextSnapshot Snapshot => TextView.TextSnapshot;
		ITextBuffer TextBuffer => TextView.TextBuffer;
		ITextViewRoleSet Roles => TextView.Roles;
		IViewScroller ViewScroller => TextView.ViewScroller;
		static CultureInfo Culture => CultureInfo.CurrentCulture;
		readonly ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService;

		ITextStructureNavigator TextStructureNavigator {
			get {
				if (textStructureNavigator == null)
					textStructureNavigator = textStructureNavigatorSelectorService.GetTextStructureNavigator(TextView.TextBuffer);
				return textStructureNavigator;
			}
		}
		ITextStructureNavigator textStructureNavigator;

		void OnContentTypeChanged(object sender, ContentTypeChangedEventArgs e) {
			// The TextStructureNavigator prop checks it for null and re-initializes it. The reason that we
			// don't just call GetTextStructureNavigator() now is that the ITextStructureNavigatorSelectorService
			// instance will remove the cached navigator instance from its ContentTypeChanged handler. If this
			// method is called before its ContentTypeChanged handler, we'll get the old cached nav instance.
			// We can't depend on always being called after it so re-initialize this field lazily.
			textStructureNavigator = null;
		}

		public EditorOperations(ITextView textView, ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (textStructureNavigatorSelectorService == null)
				throw new ArgumentNullException(nameof(textStructureNavigatorSelectorService));
			TextView = textView;
			TextView.TextBuffer.ContentTypeChanged += OnContentTypeChanged;
			this.textStructureNavigatorSelectorService = textStructureNavigatorSelectorService;
		}

		struct SavedCaretSelection {
			readonly EditorOperations editorOperations;
			public VirtualSnapshotPoint AnchorPoint { get; }
			public VirtualSnapshotPoint ActivePoint { get; }
			readonly VirtualSnapshotPoint caretPosition;

			public SavedCaretSelection(EditorOperations editorOperations) {
				this.editorOperations = editorOperations;
				AnchorPoint = editorOperations.Selection.AnchorPoint;
				ActivePoint = editorOperations.Selection.ActivePoint;
				this.caretPosition = editorOperations.Caret.Position.VirtualBufferPosition;
			}

			public void UpdatePositions() {
				var newAnchor = AnchorPoint.TranslateTo(editorOperations.Snapshot);
				var newActive = ActivePoint.TranslateTo(editorOperations.Snapshot);
				if (newAnchor != newActive)
					editorOperations.Selection.Select(newAnchor, newActive);
				else
					editorOperations.Selection.Clear();
				editorOperations.Caret.MoveTo(caretPosition.TranslateTo(editorOperations.Snapshot));
			}
		}

		SnapshotPoint GetNextNonVirtualCaretPosition() {
			var caretLine = Caret.ContainingTextViewLine;
			var span = caretLine.GetTextElementSpan(Caret.Position.BufferPosition);
			return new SnapshotPoint(Snapshot, span.End);
		}

		public void AddAfterTextBufferChangePrimitive() {
			throw new NotImplementedException();//TODO:
		}

		public void AddBeforeTextBufferChangePrimitive() {
			throw new NotImplementedException();//TODO:
		}

		public bool Backspace() => DeleteOrBackspace(true);
		public bool Delete() => DeleteOrBackspace(false);
		bool DeleteOrBackspace(bool isBackspace) {
			if (Selection.IsEmpty) {
				var caretPos = Caret.Position.VirtualBufferPosition;
				string whitespaces = GetWhitespaceForVirtualSpace(caretPos);
				if (!string.IsNullOrEmpty(whitespaces)) {
					TextBuffer.Insert(Caret.Position.BufferPosition.Position, whitespaces);
					Caret.MoveTo(new SnapshotPoint(Snapshot, caretPos.Position.Position + whitespaces.Length));
				}

				SnapshotSpan span;
				if (isBackspace) {
					if (Caret.Position.BufferPosition.Position == 0)
						return true;
					span = TextView.GetTextElementSpan(Caret.Position.BufferPosition - 1);
				}
				else {
					if (Caret.Position.BufferPosition.Position == Snapshot.Length)
						return true;
					span = TextView.GetTextElementSpan(Caret.Position.BufferPosition);
				}
				TextBuffer.Delete(span);
				Caret.MoveTo(new SnapshotPoint(Snapshot, span.Start.Position));
				Caret.EnsureVisible();
				return true;
			}
			else {
				var spans = Selection.SelectedSpans;
				var selSpan = Selection.IsReversed ? Selection.VirtualSelectedSpans[0] : Selection.VirtualSelectedSpans[Selection.VirtualSelectedSpans.Count - 1];
				Selection.Clear();
				using (var ed = TextBuffer.CreateEdit()) {
					foreach (var span in spans)
						ed.Delete(span);
					ed.Apply();
				}
				var translatedPos = selSpan.Start.TranslateTo(Snapshot, PointTrackingMode.Negative);
				Caret.MoveTo(translatedPos);
				var whitespace = GetWhitespaceForVirtualSpace(Caret.Position.VirtualBufferPosition);
				if (!string.IsNullOrEmpty(whitespace))
					TextBuffer.Insert(Caret.Position.BufferPosition.Position, whitespace);
				return true;
			}
		}

		public bool Capitalize() {
			if (Selection.IsEmpty) {
				if (Caret.Position.BufferPosition.Position == Snapshot.Length)
					return true;
				var caretPos = Caret.Position.BufferPosition;
				var info = TextStructureNavigator.GetExtentOfWord(caretPos);
				if (info.Span.Length == 0)
					return true;
				var nextCaretPos = GetNextNonVirtualCaretPosition();
				var s = info.Span.Snapshot.GetText(caretPos, 1);
				if (Caret.Position.BufferPosition == info.Span.Start)
					s = s.ToUpper(Culture);
				else
					s = s.ToLower(Culture);
				TextBuffer.Replace(new Span(caretPos, s.Length), s);
				Caret.MoveTo(new SnapshotPoint(Snapshot, nextCaretPos.Position));
				return true;
			}
			else {
				var lastWord = default(TextExtent);
				using (var ed = TextBuffer.CreateEdit()) {
					foreach (var span in Selection.SelectedSpans) {
						foreach (var info in GetWords(span)) {
							lastWord = info;
							if (!info.IsSignificant)
								continue;
							var newSpan = span.Overlap(info.Span);
							Debug.Assert(newSpan != null);
							if (newSpan == null)
								continue;
							var s = info.Span.GetText();
							int start = newSpan.Value.Start.Position;
							int end = newSpan.Value.End.Position;
							Debug.Assert(start < end);
							if (start >= end)
								continue;
							int startIndex = start - info.Span.Start.Position;
							int endIndex = end - info.Span.Start.Position;
							s = s.Substring(0, 1).ToUpper(Culture) + s.Substring(1).ToLower(Culture);
							s = s.Substring(startIndex, endIndex - startIndex);
							Debug.Assert(s.Length == newSpan.Value.Length);
							ed.Replace(Span.FromBounds(start, end), s);
						}
					}
					ed.Apply();
				}
				Debug.Assert(lastWord.Span.Snapshot != null);
				if (lastWord.IsSignificant)
					Caret.MoveTo(new SnapshotPoint(Snapshot, lastWord.Span.End.Position));

				return true;
			}
		}

		IEnumerable<TextExtent> GetWords(SnapshotSpan span) {
			if (span.Snapshot != Snapshot)
				throw new ArgumentException();
			var pos = span.Start;
			while (pos < span.End) {
				var info = TextStructureNavigator.GetExtentOfWord(pos);
				yield return info;
				pos = info.Span.End;
			}
		}

		public bool ConvertSpacesToTabs() {
			throw new NotImplementedException();//TODO:
		}

		public bool ConvertTabsToSpaces() {
			throw new NotImplementedException();//TODO:
		}

		const string VS_COPY_FULL_LINE_DATA_FORMAT = "VisualStudioEditorOperationsLineCutCopyClipboardTag";
		const string VS_COPY_BOX_DATA_FORMAT = "MSDEVColumnSelect";
		bool CopyToClipboard(string text, bool isFullLineData, bool isBoxData) {
			try {
				var dataObj = new DataObject();
				dataObj.SetText(text);
				if (isFullLineData)
					dataObj.SetData(VS_COPY_FULL_LINE_DATA_FORMAT, true);
				if (isBoxData)
					dataObj.SetData(VS_COPY_BOX_DATA_FORMAT, true);
				Clipboard.SetDataObject(dataObj);
				return true;
			}
			catch (ExternalException) {
				return false;
			}
		}

		public bool CopySelection() => CutOrCopySelection(false);
		public bool CutSelection() => CutOrCopySelection(true);
		bool CutOrCopySelection(bool cut) {
			if (Selection.IsEmpty) {
				var line = Caret.Position.BufferPosition.GetContainingLine();
				bool copyEmptyLines = Options.GetOptionValue(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId);
				if (!copyEmptyLines && string.IsNullOrWhiteSpace(line.GetText()))
					return true;
				if (cut)
					TextBuffer.Delete(line.ExtentIncludingLineBreak);
				return CopyToClipboard(line.GetTextIncludingLineBreak(), true, false);
			}
			var text = Selection.GetText();
			bool isBox = Selection.Mode == TextSelectionMode.Box;
			if (cut) {
				var spans = Selection.SelectedSpans;
				Selection.Clear();
				using (var ed = TextBuffer.CreateEdit()) {
					foreach (var span in spans)
						ed.Delete(span);
					ed.Apply();
				}
			}
			return CopyToClipboard(text, false, isBox);
		}

		VirtualSnapshotSpan GetSelectionOrCaretIfNoSelection() {
			if (!Selection.IsEmpty)
				return Selection.StreamSelectionSpan;
			return new VirtualSnapshotSpan(Caret.Position.VirtualBufferPosition, Caret.Position.VirtualBufferPosition);
		}

		public bool CutFullLine() => CutDeleteFullLine(true);
		public bool DeleteFullLine() => CutDeleteFullLine(false);
		bool CutDeleteFullLine(bool cut) {
			var caretPos = Caret.Position;
			var span = GetSelectionOrCaretIfNoSelection();
			var startLine = span.Start.Position.GetContainingLine();
			var endLine = span.End.Position.GetContainingLine();
			var cutSpan = new SnapshotSpan(startLine.Start, endLine.EndIncludingLineBreak);
			var text = Snapshot.GetText(cutSpan);
			Selection.Clear();
			TextBuffer.Delete(cutSpan);
			var newPos = caretPos.BufferPosition.TranslateTo(Snapshot, PointTrackingMode.Negative);
			Caret.MoveTo(newPos);
			Caret.EnsureVisible();
			if (cut)
				return CopyToClipboard(text, true, false);
			return true;
		}

		public bool DecreaseLineIndent() {
			throw new NotImplementedException();//TODO:
		}

		public bool DeleteBlankLines() {
			var caretLeft = Caret.Left;
			var span = GetSelectionOrCaretIfNoSelection();
			var startLineNumber = span.Start.Position.GetContainingLine().LineNumber;
			var endLineNumber = span.End.Position.GetContainingLine().LineNumber;

			using (var ed = TextBuffer.CreateEdit()) {
				for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++) {
					var line = Snapshot.GetLineFromLineNumber(lineNumber);
					if (IsLineEmpty(line))
						ed.Delete(line.ExtentIncludingLineBreak.Span);
				}
				ed.Apply();
			}

			Caret.EnsureVisible();
			Caret.MoveTo(Caret.ContainingTextViewLine, caretLeft, true);
			return true;
		}

		bool IsLineEmpty(ITextSnapshotLine line) {
			if (line.Length == 0)
				return true;
			// Check the end first, it's rarely whitespace if there's non-whitespace on the line
			if (!char.IsWhiteSpace((line.End - 1).GetChar()))
				return false;

			// Don't check the end, we already checked it above
			int end = line.End.Position - 1;
			for (int offset = line.Start.Position; offset < end; offset++) {
				if (!char.IsWhiteSpace(line.Snapshot[offset]))
					return false;
			}
			return true;
		}

		public bool DeleteHorizontalWhiteSpace() {
			var oldSelection = new SavedCaretSelection(this);
			var span = Selection.IsEmpty ? GetDefaultHorizontalWhitespaceSpan(Caret.Position.BufferPosition) : Selection.StreamSelectionSpan.SnapshotSpan.Span;

			using (var ed = TextBuffer.CreateEdit()) {
				foreach (var deleteSpan in GetHorizontalWhiteSpaceSpans(Snapshot, span)) {
					Debug.Assert(deleteSpan.Span.Length > 0);
					ed.Delete(deleteSpan.Span);
					if (deleteSpan.AddSpace) {
						// VS always inserts a space character, even if the deleted whitespace chars were all tabs
						ed.Insert(deleteSpan.Span.Start, " ");
					}
				}
				ed.Apply();
			}

			oldSelection.UpdatePositions();
			return true;
		}

		Span GetDefaultHorizontalWhitespaceSpan(SnapshotPoint point) {
			var snapshot = point.Snapshot;
			var line = point.GetContainingLine();
			int lineStartOffset = line.Start.Position;
			int lineEndOffset = line.End.Position;

			int c = point.Position;
			while (c < lineEndOffset && IsWhiteSpace(snapshot[c]))
				c++;
			int end = c;

			c = point.Position;
			while (c > lineStartOffset && IsWhiteSpace(snapshot[c - 1]))
				c--;
			int start = c;

			return Span.FromBounds(start, end);
		}

		struct DeleteHorizontalWhitespaceInfo {
			public Span Span { get; }
			public bool AddSpace { get; }
			public DeleteHorizontalWhitespaceInfo(Span span, bool addSpace) {
				Span = span;
				AddSpace = addSpace;
			}
		}

		IEnumerable<DeleteHorizontalWhitespaceInfo> GetHorizontalWhiteSpaceSpans(ITextSnapshot snapshot, Span span) {
			// Make sure no newline character is considered whitespace
			Debug.Assert(!IsWhiteSpace('\r'));
			Debug.Assert(!IsWhiteSpace('\n'));
			Debug.Assert(!IsWhiteSpace('\u0085'));
			Debug.Assert(!IsWhiteSpace('\u2028'));
			Debug.Assert(!IsWhiteSpace('\u2029'));

			int spanEnd = span.End;
			if (spanEnd > snapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(span));
			int pos = span.Start;
			ITextSnapshotLine line = null;
			while (pos < spanEnd) {
				while (pos < spanEnd && !IsWhiteSpace(snapshot[pos]))
					pos++;
				int start = pos;

				while (pos < spanEnd && IsWhiteSpace(snapshot[pos]))
					pos++;
				int end = pos;

				if (start == end) {
					Debug.Assert(end == spanEnd);
					break;
				}

				if (line == null || start >= line.EndIncludingLineBreak.Position)
					line = snapshot.GetLineFromPosition(start);
				Debug.Assert(start >= line.Start.Position && end <= line.End);

				bool addSpace;
				if (start == line.Start.Position || end == line.End.Position)
					addSpace = false;
				else if (IsWhiteSpace(snapshot[start - 1]))
					addSpace = false;
				else if (IsWhiteSpace(snapshot[end]))
					addSpace = false;
				else {
					//TODO: sometimes all the spaces are removed "//    xxx = int i	123;"
					//		Select "xxx = int i	123;", and all spaces are removed ("xxx=inti123;")
					//		Select the full string "//    xxx = int i	123;" and whitespaces
					//		are kept ("// xxx = int i 123;"). Execute it again, and all spaces
					//		are removed ("//xxx=inti123;")
					addSpace = true;
				}
				yield return new DeleteHorizontalWhitespaceInfo(Span.FromBounds(start, end), addSpace);
			}
			Debug.Assert(pos == spanEnd);
		}

		// Same as VS, \u200B == zero-width space
		static bool IsWhiteSpace(char c) => c == '\t' || c == '\u200B' || char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator;

		public bool DeleteToBeginningOfLine() {
			var pos = Caret.Position.VirtualBufferPosition;
			if (!Selection.IsEmpty) {
				TextBuffer.Delete(Selection.StreamSelectionSpan.SnapshotSpan.Span);
				Selection.Clear();
			}
			else {
				var line = Snapshot.GetLineFromPosition(Caret.Position.BufferPosition.Position);
				int column = Caret.Position.BufferPosition.Position - line.Start.Position;
				TextBuffer.Delete(new Span(line.Start.Position, column));
			}
			Caret.MoveTo(pos.TranslateTo(Snapshot));
			return true;
		}

		public bool DeleteToEndOfLine() {
			if (!Selection.IsEmpty) {
				var oldSelection = new SavedCaretSelection(this);

				Span deleteSpan;
				if (oldSelection.ActivePoint < oldSelection.AnchorPoint)
					deleteSpan = new Span(oldSelection.ActivePoint.Position, oldSelection.AnchorPoint.Position - oldSelection.ActivePoint.Position);
				else {
					var line = Snapshot.GetLineFromPosition(oldSelection.ActivePoint.Position);
					deleteSpan = new Span(oldSelection.AnchorPoint.Position, line.End.Position - oldSelection.AnchorPoint.Position);
				}

				TextBuffer.Delete(deleteSpan);

				oldSelection.UpdatePositions();
			}
			else {
				var line = Snapshot.GetLineFromPosition(Caret.Position.BufferPosition.Position);
				TextBuffer.Delete(new Span(Caret.Position.BufferPosition.Position, line.End.Position - Caret.Position.BufferPosition.Position));
			}
			return true;
		}

		public bool DeleteWordToLeft() {
			throw new NotImplementedException();//TODO:
		}

		public bool DeleteWordToRight() {
			throw new NotImplementedException();//TODO:
		}

		public void ExtendSelection(int newEnd) {
			if ((uint)newEnd > (uint)Snapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(newEnd));
			Selection.Select(Selection.AnchorPoint, new VirtualSnapshotPoint(new SnapshotPoint(Snapshot, newEnd)));
			Caret.MoveTo(Selection.ActivePoint);
			var options = Selection.IsReversed ? EnsureSpanVisibleOptions.ShowStart | EnsureSpanVisibleOptions.MinimumScroll : EnsureSpanVisibleOptions.ShowStart;
			ViewScroller.EnsureSpanVisible(Selection.StreamSelectionSpan, options);
		}

		public string GetWhitespaceForVirtualSpace(VirtualSnapshotPoint point) {
			if (point.Position.Snapshot != Snapshot)
				throw new ArgumentException();
			if (point.VirtualSpaces == 0)
				return string.Empty;

			if (Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId))
				return new string(' ', point.VirtualSpaces);
			int tabSize = Options.GetOptionValue(DefaultOptions.TabSizeOptionId);

			var line = point.Position.GetContainingLine();
			int column = point.Position - line.Start;
			Debug.Assert(column == line.Length);
			if (column != line.Length)
				return string.Empty;

			int lineLengthNoTabs = ConvertTabsToSpaces(line.GetText()).Length;
			int newEndColumn = lineLengthNoTabs + point.VirtualSpaces;

			var firstTabCol = (lineLengthNoTabs + tabSize - 1) / tabSize * tabSize;
			if (firstTabCol > newEndColumn)
				return new string(' ', newEndColumn - lineLengthNoTabs);

			int tabs = (newEndColumn - (lineLengthNoTabs - lineLengthNoTabs % tabSize)) / tabSize;
			int spaces = newEndColumn % tabSize;
			var chars = new char[tabs + spaces];
			for (int i = 0; i < tabs; i++)
				chars[i] = '\t';
			for (int i = 0; i < spaces; i++)
				chars[tabs + i] = ' ';
			return new string(chars);
		}

		string ConvertTabsToSpaces(string line) {
			if (line.IndexOf('\t') < 0)
				return line;

			int tabSize = Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
			var sb = new StringBuilder();
			for (int i = 0; i < line.Length; i++) {
				var c = line[i];
				if (c != '\t')
					sb.Append(c);
				else {
					int spaces = tabSize - (sb.Length % tabSize);
					for (int j = 0; j < spaces; j++)
						sb.Append(' ');
					Debug.Assert((sb.Length % tabSize) == 0);
				}
			}
			return sb.ToString();
		}

		public void GotoLine(int lineNumber) => GotoLine(lineNumber, 0);
		public void GotoLine(int lineNumber, int column) {
			if ((uint)lineNumber >= (uint)Snapshot.LineCount)
				throw new ArgumentOutOfRangeException(nameof(lineNumber));
			var line = Snapshot.GetLineFromLineNumber(lineNumber);
			if ((uint)column > (uint)line.Length)
				throw new ArgumentOutOfRangeException(nameof(column));
			var point = line.Start + column;
			var span = TextView.GetTextElementSpan(point);
			Selection.Clear();
			Caret.MoveTo(span.Start);
			ViewScroller.EnsureSpanVisible(span);
		}

		public bool IncreaseLineIndent() {
			throw new NotImplementedException();//TODO:
		}

		public bool Indent() {
			throw new NotImplementedException();//TODO:
		}

		public bool InsertFile(string filePath) {
			throw new NotImplementedException();//TODO:
		}

		public bool InsertNewLine() {
			throw new NotImplementedException();//TODO:
		}

		public bool InsertProvisionalText(string text) {
			throw new NotImplementedException();//TODO:
		}

		public bool InsertText(string text) {
			throw new NotImplementedException();//TODO:
		}

		public bool InsertTextAsBox(string text, out VirtualSnapshotPoint boxStart, out VirtualSnapshotPoint boxEnd) {
			throw new NotImplementedException();//TODO:
		}

		public bool MakeLowercase() => UpperLower(false);
		public bool MakeUppercase() => UpperLower(true);
		bool UpperLower(bool upper) {
			if (Selection.IsEmpty) {
				if (Caret.Position.BufferPosition.Position >= Snapshot.Length)
					return true;
				var caretLine = Caret.ContainingTextViewLine;
				var span = caretLine.GetTextElementSpan(Caret.Position.BufferPosition);
				var text = Snapshot.GetText(span);
				if (text.Length == 1)
					text = upper ? text.ToUpper(Culture) : text.ToLower(Culture);
				TextBuffer.Replace(span, text);
				Caret.MoveTo(new SnapshotPoint(Snapshot, span.End));
				return true;
			}
			else {
				var snapshot = Snapshot;
				using (var ed = TextBuffer.CreateEdit()) {
					foreach (var span in Selection.SelectedSpans) {
						var text = snapshot.GetText(span);
						text = upper ? text.ToUpper(Culture) : text.ToLower(Culture);
						ed.Replace(span, text);
					}
					ed.Apply();
				}
				return true;
			}
		}

		public void MoveCaret(ITextViewLine textLine, double horizontalOffset, bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveCurrentLineToBottom() =>
			TextView.DisplayTextLineContainingBufferPosition(Caret.Position.BufferPosition, 0, ViewRelativePosition.Bottom);

		public void MoveCurrentLineToTop() =>
			TextView.DisplayTextLineContainingBufferPosition(Caret.Position.BufferPosition, 0, ViewRelativePosition.Top);

		public void MoveLineDown(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveLineUp(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public bool MoveSelectedLinesDown() {
			throw new NotImplementedException();//TODO:
		}

		public bool MoveSelectedLinesUp() {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToBottomOfView(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToEndOfDocument(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToEndOfLine(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToHome(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToLastNonWhiteSpaceCharacter(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToNextCharacter(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToNextWord(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToPreviousCharacter(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToPreviousWord(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToStartOfDocument(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToStartOfLine(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToStartOfLineAfterWhiteSpace(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToStartOfNextLineAfterWhiteSpace(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToStartOfPreviousLineAfterWhiteSpace(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToTopOfView(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public bool NormalizeLineEndings(string replacement) {
			throw new NotImplementedException();//TODO:
		}

		public bool OpenLineAbove() {
			throw new NotImplementedException();//TODO:
		}

		public bool OpenLineBelow() {
			throw new NotImplementedException();//TODO:
		}

		public void PageDown(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void PageUp(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public bool Paste() {
			throw new NotImplementedException();//TODO:
		}

		public int ReplaceAllMatches(string searchText, string replaceText, bool matchCase, bool matchWholeWord, bool useRegularExpressions) {
			throw new NotImplementedException();//TODO:
		}

		public bool ReplaceSelection(string text) {
			throw new NotImplementedException();//TODO:
		}

		public bool ReplaceText(Span replaceSpan, string text) {
			throw new NotImplementedException();//TODO:
		}

		public void ResetSelection() => Selection.Clear();

		public void ScrollColumnLeft() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollColumnRight() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollDownAndMoveCaretIfNecessary() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollLineBottom() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollLineCenter() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollLineTop() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollPageDown() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollPageUp() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollUpAndMoveCaretIfNecessary() {
			throw new NotImplementedException();//TODO:
		}

		public void SelectAll() =>
			SelectAndMove(new SnapshotSpan(new SnapshotPoint(Snapshot, 0), Snapshot.Length));

		void SelectAndMove(SnapshotSpan span) {
			Selection.Select(span, false);
			Caret.MoveTo(span.End);
			Caret.EnsureVisible();
		}

		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint) {
			throw new NotImplementedException();//TODO:
		}

		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode) {
			throw new NotImplementedException();//TODO:
		}

		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode, EnsureSpanVisibleOptions? scrollOptions) {
			throw new NotImplementedException();//TODO:
		}

		bool IsSelected(SnapshotSpan span) {
			var spans = Selection.SelectedSpans;
			if (spans.Count != 1)
				return false;
			return spans[0] == span;
		}

		public void SelectCurrentWord() {
			var info = TextStructureNavigator.GetExtentOfWord(Caret.Position.BufferPosition);
			var prevInfo = info;

			var line = Caret.Position.BufferPosition.GetContainingLine();
			int column = Caret.Position.BufferPosition - line.Start;
			if (column != 0) {
				var prevPos = Caret.Position.BufferPosition - 1;
				prevInfo = TextStructureNavigator.GetExtentOfWord(prevPos);
			}

			// If the word is already selected, select it, not the next one since the caret
			// is placed after the selected word.
			if (IsSelected(prevInfo.Span))
				info = prevInfo;
			else if (!info.IsSignificant && prevInfo.IsSignificant)
				info = prevInfo;

			SelectAndMove(info.Span);
		}

		public void SelectEnclosing() {
			throw new NotImplementedException();//TODO:
		}

		public void SelectFirstChild() {
			throw new NotImplementedException();//TODO:
		}

		public void SelectLine(ITextViewLine viewLine, bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void SelectNextSibling(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void SelectPreviousSibling(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void SwapCaretAndAnchor() {
			Selection.Select(anchorPoint: Selection.ActivePoint, activePoint: Selection.AnchorPoint);
			Caret.MoveTo(Selection.ActivePoint);
			Caret.EnsureVisible();
		}

		public bool Tabify() {
			throw new NotImplementedException();//TODO:
		}

		public bool ToggleCase() {
			throw new NotImplementedException();//TODO:
		}

		public bool TransposeCharacter() {
			throw new NotImplementedException();//TODO:
		}

		public bool TransposeLine() {
			throw new NotImplementedException();//TODO:
		}

		public bool TransposeWord() {
			throw new NotImplementedException();//TODO:
		}

		public bool Unindent() {
			throw new NotImplementedException();//TODO:
		}

		public bool Untabify() {
			throw new NotImplementedException();//TODO:
		}

		IWpfTextView GetZoomableView() {
			if (!Roles.Contains(PredefinedTextViewRoles.Zoomable))
				return null;
			var wpfView = TextView as IWpfTextView;
			Debug.Assert(wpfView != null);
			return wpfView;
		}

		void SetZoom(IWpfTextView wpfView, double newZoom) {
			if (newZoom < 20 || newZoom > 400)
				return;
			// VS writes to the global options, instead of the text view's options
			wpfView.Options.GlobalOptions.SetOptionValue(DefaultWpfViewOptions.ZoomLevelId, newZoom);
		}

		public void ZoomIn() {
			var wpfView = GetZoomableView();
			if (wpfView == null)
				return;
			SetZoom(wpfView, wpfView.ZoomLevel * 1.1);
		}

		public void ZoomOut() {
			var wpfView = GetZoomableView();
			if (wpfView == null)
				return;
			SetZoom(wpfView, wpfView.ZoomLevel / 1.1);
		}

		public void ZoomTo(double zoomLevel) {
			var wpfView = GetZoomableView();
			if (wpfView == null)
				return;
			SetZoom(wpfView, zoomLevel);
		}
	}
}
