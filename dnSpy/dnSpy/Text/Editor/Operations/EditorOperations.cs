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
using System.IO;
using System.Linq;
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
				if (!Selection.IsEmpty)
					return true;

				var line = Caret.Position.BufferPosition.GetContainingLine();
				bool cutEmptyLines = Options.GetOptionValue(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId);
				return cutEmptyLines || !string.IsNullOrWhiteSpace(line.GetText());
			}
		}

		public bool CanDelete => Caret.Position.BufferPosition.Position < Snapshot.Length;

		public bool CanPaste {
			get {
				try {
					return Clipboard.ContainsText();
				}
				catch (ExternalException) {
					return false;
				}
			}
		}

		public ITrackingSpan ProvisionalCompositionSpan => null;//TODO:
		public IEditorOptions Options => TextView.Options;
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
		readonly ISmartIndentationService smartIndentationService;

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

		public EditorOperations(ITextView textView, ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService, ISmartIndentationService smartIndentationService) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (textStructureNavigatorSelectorService == null)
				throw new ArgumentNullException(nameof(textStructureNavigatorSelectorService));
			TextView = textView;
			TextView.TextBuffer.ContentTypeChanged += OnContentTypeChanged;
			this.textStructureNavigatorSelectorService = textStructureNavigatorSelectorService;
			this.smartIndentationService = smartIndentationService;
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
			var caretLine = TextView.GetTextViewLineContainingBufferPosition(Caret.Position.BufferPosition);
			var span = caretLine.GetTextElementSpan(Caret.Position.BufferPosition);
			return new SnapshotPoint(Snapshot, span.End);
		}

		public void AddAfterTextBufferChangePrimitive() {
			return;//TODO:
		}

		public void AddBeforeTextBufferChangePrimitive() {
			return;//TODO:
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
			return true;//TODO:
		}

		public bool ConvertTabsToSpaces() {
			return true;//TODO:
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
				bool cutEmptyLines = Options.GetOptionValue(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId);
				if (!cutEmptyLines && string.IsNullOrWhiteSpace(line.GetText()))
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

		VirtualSnapshotPoint GetAnchorPositionOrCaretIfNoSelection() {
			VirtualSnapshotPoint anchorPoint, activePoint;
			GetSelectionOrCaretIfNoSelection(out anchorPoint, out activePoint);
			return anchorPoint;
		}

		void GetSelectionOrCaretIfNoSelection(out VirtualSnapshotPoint anchorPoint, out VirtualSnapshotPoint activePoint) {
			if (!Selection.IsEmpty) {
				anchorPoint = Selection.AnchorPoint;
				activePoint = Selection.ActivePoint;
			}
			else {
				anchorPoint = Caret.Position.VirtualBufferPosition;
				activePoint = Caret.Position.VirtualBufferPosition;
			}
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
			return true;//TODO:
		}

		public bool DeleteBlankLines() {
			var caretLeft = Caret.Left;
			var span = GetSelectionOrCaretIfNoSelection();
			var startLineNumber = span.Start.Position.GetContainingLine().LineNumber;
			var endLineNumber = span.End.Position.GetContainingLine().LineNumber;

			using (var ed = TextBuffer.CreateEdit()) {
				for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++) {
					var line = Snapshot.GetLineFromLineNumber(lineNumber);
					if (line.IsEmptyOrWhitespace())
						ed.Delete(line.ExtentIncludingLineBreak.Span);
				}
				ed.Apply();
			}

			Caret.EnsureVisible();
			Caret.MoveTo(Caret.ContainingTextViewLine, caretLeft, true);
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

		static Span GetDefaultHorizontalWhitespaceSpan(SnapshotPoint point) {
			var snapshot = point.Snapshot;
			var line = point.GetContainingLine();
			int lineStartOffset = line.Start.Position;
			int lineEndOffset = line.End.Position;

			int c = point.Position;
			while (c < lineEndOffset && IsWhitespace(snapshot[c]))
				c++;
			int end = c;

			c = point.Position;
			while (c > lineStartOffset && IsWhitespace(snapshot[c - 1]))
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
			Debug.Assert(!IsWhitespace('\r'));
			Debug.Assert(!IsWhitespace('\n'));
			Debug.Assert(!IsWhitespace('\u0085'));
			Debug.Assert(!IsWhitespace('\u2028'));
			Debug.Assert(!IsWhitespace('\u2029'));

			int spanEnd = span.End;
			if (spanEnd > snapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(span));
			int pos = span.Start;
			ITextSnapshotLine line = null;
			while (pos < spanEnd) {
				while (pos < spanEnd && !IsWhitespace(snapshot[pos]))
					pos++;
				int start = pos;

				while (pos < spanEnd && IsWhitespace(snapshot[pos]))
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
				else if (IsWhitespace(snapshot[start - 1]))
					addSpace = false;
				else if (IsWhitespace(snapshot[end]))
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
		static bool IsWhitespace(char c) => c == '\t' || c == '\u200B' || char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator;

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

		void DeleteSelection() {
			var caret = Caret.Position.VirtualBufferPosition;

			var spans = Selection.SelectedSpans;
			if (Selection.Mode != TextSelectionMode.Box)
				Selection.Clear();
			using (var ed = TextBuffer.CreateEdit()) {
				foreach (var span in spans)
					ed.Delete(span);
				ed.Apply();
			}

			Caret.MoveTo(caret.TranslateTo(Snapshot));
		}

		TextExtent TryGetPreviousSignificantWord(TextExtent info) {
			info = TextStructureNavigator.GetExtentOfWord(info.Span.Start - 1);
			if (info.IsSignificant)
				return info;
			var line = info.Span.Start.GetContainingLine();
			if (info.Span.Start == line.Start)
				return info;
			if (info.Span.Start == line.End) {
				info = TextStructureNavigator.GetExtentOfWord(info.Span.Start - 1);
				line = info.Span.Start.GetContainingLine();
			}
			if (info.IsSignificant)
				return info;
			if (info.Span.Start == line.Start)
				return info;
			return TextStructureNavigator.GetExtentOfWord(info.Span.Start - 1);
		}

		SnapshotSpan GetSpanOfLeftWord(VirtualSnapshotPoint point) {
			var snapshot = point.Position.Snapshot;
			int position = point.Position.Position;
			var line = snapshot.GetLineFromPosition(point.Position.Position);

			var info = TextStructureNavigator.GetExtentOfWord(new SnapshotPoint(snapshot, position));
			bool canGetNewInfo = true;
			if (info.Span.Start.Position == 0)
				return info.Span;
			if (!info.IsSignificant) {
				if (position != line.Start.Position && info.Span.Start == line.Start)
					return info.Span;
				info = TryGetPreviousSignificantWord(info);
				canGetNewInfo = false;
			}

			if (info.Span.Start.Position == 0)
				return info.Span;
			if (info.Span.Start.Position != position)
				return info.Span;
			if (canGetNewInfo)
				info = TryGetPreviousSignificantWord(info);

			return info.Span;
		}

		public bool DeleteWordToLeft() {
			if (Selection.ActivePoint > Selection.AnchorPoint) {
				DeleteSelection();
				return true;
			}

			var selSpan = GetSelectionOrCaretIfNoSelection();
			var wordSpan = GetSpanOfLeftWord(selSpan.Start);

			var oldSelection = new SavedCaretSelection(this);

			var newSelSpan = new VirtualSnapshotSpan(new VirtualSnapshotPoint(wordSpan.Start), selSpan.End);
			if (Selection.IsEmpty || Selection.Mode != TextSelectionMode.Box)
				TextBuffer.Delete(newSelSpan.SnapshotSpan.Span);
			else {
				var line = Snapshot.GetLineFromPosition(wordSpan.Start.Position);
				int column = wordSpan.Start - line.Start;
				using (var ed = TextBuffer.CreateEdit()) {
					foreach (var span in Selection.SelectedSpans) {
						line = Snapshot.GetLineFromPosition(span.Start.Position);
						var start = line.Start + Math.Min(line.Length, column);
						if (start < span.End) {
							var newSpan = new SnapshotSpan(start, span.End);
							ed.Delete(newSpan);
						}
					}
					ed.Apply();
				}
			}

			oldSelection.UpdatePositions();
			return true;
		}

		SnapshotPoint GetPointOfRightWord(VirtualSnapshotPoint point) {
			var info = TextStructureNavigator.GetExtentOfWord(point.Position);
			if (info.Span.End.Position == info.Span.Snapshot.Length)
				return info.Span.End;

			var line = info.Span.End.GetContainingLine();
			if (line.End == info.Span.End) {
				if (point.Position == line.End) {
					info = TextStructureNavigator.GetExtentOfWord(line.EndIncludingLineBreak);
					line = info.Span.Start.GetContainingLine();
					if (info.IsSignificant)
						return info.Span.Start;
					if (info.Span.Length == 0)
						return TextStructureNavigator.GetExtentOfWord(info.Span.End).Span.Start;
					return info.Span.End;
				}
				if (info.IsSignificant)
					return info.Span.End;
				return TextStructureNavigator.GetExtentOfWord(info.Span.End).Span.End;
			}

			if (!info.IsSignificant)
				return TextStructureNavigator.GetExtentOfWord(info.Span.End).Span.Start;

			info = TextStructureNavigator.GetExtentOfWord(info.Span.End);
			if (info.IsSignificant)
				return info.Span.Start;
			line = info.Span.Start.GetContainingLine();
			return info.Span.End;
		}

		public bool DeleteWordToRight() {
			if (Selection.ActivePoint < Selection.AnchorPoint) {
				DeleteSelection();
				return true;
			}

			var selSpan = GetSelectionOrCaretIfNoSelection();
			var wordPoint = GetPointOfRightWord(selSpan.End);

			var oldSelection = new SavedCaretSelection(this);

			var newSelSpan = new VirtualSnapshotSpan(selSpan.Start, new VirtualSnapshotPoint(wordPoint));
			if (Selection.IsEmpty || Selection.Mode != TextSelectionMode.Box)
				TextBuffer.Delete(newSelSpan.SnapshotSpan.Span);
			else {
				var line = Snapshot.GetLineFromPosition(wordPoint.Position);
				int column = wordPoint - line.Start;
				using (var ed = TextBuffer.CreateEdit()) {
					foreach (var span in Selection.SelectedSpans) {
						line = Snapshot.GetLineFromPosition(span.Start.Position);
						var end = line.Start + Math.Min(line.Length, column);
						if (span.Start < end) {
							var newSpan = new SnapshotSpan(span.Start, end);
							ed.Delete(newSpan);
						}
					}
					ed.Apply();
				}
			}

			oldSelection.UpdatePositions();
			return true;
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

			var line = point.Position.GetContainingLine();
			int column = point.Position - line.Start;
			Debug.Assert(column == line.Length);
			if (column != line.Length)
				return string.Empty;

			int lineLengthNoTabs = ConvertTabsToSpaces(line.GetText()).Length;
			return GetWhitespaceForVirtualSpace(lineLengthNoTabs, point.VirtualSpaces);
		}

		string GetWhitespaceForVirtualSpace(int lineLengthNoTabs, int virtualSpaces) {
			if (Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId))
				return new string(' ', virtualSpaces);

			int tabSize = Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
			int newEndColumn = lineLengthNoTabs + virtualSpaces;

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
					int spaces = tabSize - sb.Length % tabSize;
					for (int j = 0; j < spaces; j++)
						sb.Append(' ');
					Debug.Assert(sb.Length % tabSize == 0);
				}
			}
			return sb.ToString();
		}

		public void GotoLine(int lineNumber) => GotoLine(lineNumber, 0);
		public void GotoLine(int lineNumber, int column) {
			if (lineNumber < 0 || column < 0)
				return;
			if (lineNumber >= Snapshot.LineCount)
				lineNumber = Snapshot.LineCount - 1;
			var line = Snapshot.GetLineFromLineNumber(lineNumber);
			if (column > line.Length)
				column = line.Length;
			var point = line.Start + column;
			var span = TextView.GetTextElementSpan(point);
			Selection.Clear();
			Caret.MoveTo(span.Start);
			ViewScroller.EnsureSpanVisible(span);
		}

		public bool IncreaseLineIndent() {
			return true;//TODO:
		}

		public bool Indent() {
			return true;//TODO:
		}

		public bool InsertNewLine() {
			var viewLine = Caret.ContainingTextViewLine;
			var linebreak = GetLineBreak(viewLine.Start);

			var spans = Selection.SelectedSpans;
			Selection.Clear();
			var caretPos = Caret.Position;
			using (var ed = TextBuffer.CreateEdit()) {
				foreach (var span in spans)
					ed.Delete(span);
				ed.Apply();
			}
			var newPos = caretPos.BufferPosition.TranslateTo(Snapshot, PointTrackingMode.Negative);
			TextBuffer.Insert(newPos.Position, linebreak);

			var line = Snapshot.GetLineFromPosition(newPos.Position + linebreak.Length);
			int column = IndentHelper.GetDesiredIndentation(TextView, smartIndentationService, line) ?? 0;
			VirtualSnapshotPoint newPoint;
			if (line.Length == 0)
				newPoint = new VirtualSnapshotPoint(line.Start, column);
			else {
				var indentation = GetWhitespaceForVirtualSpace(0, column);
				TextBuffer.Insert(line.Start, indentation);
				newPoint = new VirtualSnapshotPoint(new SnapshotPoint(Snapshot, line.Start.Position + indentation.Length));
			}
			Caret.MoveTo(newPoint);
			Caret.EnsureVisible();
			return true;
		}

		public bool InsertFile(string filePath) {
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath));
			return InsertText(File.ReadAllText(filePath), false, false);
		}

		public bool InsertProvisionalText(string text) => InsertText(text, true);
		public bool InsertText(string text) => InsertText(text, false);
		bool InsertText(string text, bool isProvisional) {
			bool overwriteMode = Options.GetOptionValue(DefaultTextViewOptions.OverwriteModeId);
			if (!Selection.IsEmpty)
				overwriteMode = false;
			if (Caret.InVirtualSpace)
				overwriteMode = false;
			return InsertText(text, isProvisional, overwriteMode);
		}

		public bool Paste() {
			string text;
			try {
				text = Clipboard.GetText();
			}
			catch (ExternalException) {
				return false;
			}
			if (text == null)
				return false;
			return InsertText(text, false, false);
		}

		bool InsertText(string text, bool isProvisional, bool overwriteMode) {
			var spans = Selection.SelectedSpans;
			Selection.Clear();
			var caretPos = Caret.Position;
			using (var ed = TextBuffer.CreateEdit()) {
				foreach (var span in spans)
					ed.Delete(span);
				ed.Apply();
			}
			var newPos = caretPos.VirtualBufferPosition.TranslateTo(Snapshot, PointTrackingMode.Negative);

			if (!overwriteMode) {
				var spaces = GetWhitespaceForVirtualSpace(newPos);
				TextBuffer.Insert(newPos.Position, spaces + text);
				newPos = newPos.TranslateTo(Snapshot);
			}
			else {
				Debug.Assert(!newPos.IsInVirtualSpace);
				var line = newPos.Position.GetContainingLine();
				int column = newPos.Position - line.Start;
				int columnsLeft = line.Length - column;
				int replaceLength = Math.Min(columnsLeft, text.Length);
				TextBuffer.Replace(new Span(newPos.Position.Position, replaceLength), text);
				newPos = newPos.TranslateTo(Snapshot);
			}

			Caret.MoveTo(newPos);
			Caret.EnsureVisible();
			return true;
		}

		public bool InsertTextAsBox(string text, out VirtualSnapshotPoint boxStart, out VirtualSnapshotPoint boxEnd) {
			boxStart = new VirtualSnapshotPoint(Snapshot, 0);
			boxEnd = new VirtualSnapshotPoint(Snapshot, 0);
			return true;//TODO:
		}

		public bool MakeLowercase() => UpperLower(false);
		public bool MakeUppercase() => UpperLower(true);
		bool UpperLower(bool upper) {
			if (Selection.IsEmpty) {
				if (Caret.Position.BufferPosition.Position >= Snapshot.Length)
					return true;
				var caretLine = TextView.GetTextViewLineContainingBufferPosition(Caret.Position.BufferPosition);
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
			if (textLine == null)
				throw new ArgumentNullException(nameof(textLine));

			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(textLine, horizontalOffset);
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else {
				var oldMode = Selection.Mode;
				Selection.Clear();
				Selection.Mode = oldMode;
			}
		}

		public void MoveCurrentLineToBottom() =>
			TextView.DisplayTextLineContainingBufferPosition(Caret.Position.BufferPosition, 0, ViewRelativePosition.Bottom);

		public void MoveCurrentLineToTop() =>
			TextView.DisplayTextLineContainingBufferPosition(Caret.Position.BufferPosition, 0, ViewRelativePosition.Top);

		public void MoveLineDown(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			ITextViewLine line;
			if (extendSelection || Selection.IsEmpty)
				line = Caret.ContainingTextViewLine;
			else
				line = TextView.GetTextViewLineContainingBufferPosition(Selection.End.Position);
			if (line.IsLastDocumentLine())
				return;
			var nextLine = TextView.GetTextViewLineContainingBufferPosition(line.EndIncludingLineBreak);

			Caret.MoveTo(nextLine);
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void MoveLineUp(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			ITextViewLine line;
			if (extendSelection || Selection.IsEmpty)
				line = Caret.ContainingTextViewLine;
			else
				line = TextView.GetTextViewLineContainingBufferPosition(Selection.Start.Position);
			if (line.Start.Position == 0)
				return;
			var prevLine = TextView.GetTextViewLineContainingBufferPosition(line.Start - 1);

			Caret.MoveTo(prevLine);
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public bool MoveSelectedLinesDown() {
			return true;//TODO:
		}

		public bool MoveSelectedLinesUp() {
			return true;//TODO:
		}

		ITextViewLine GetBottomFullyVisibleLine() =>
			TextView.TextViewLines.LastOrDefault(a => a.VisibilityState == VisibilityState.FullyVisible) ??
			TextView.TextViewLines.LastOrDefault(a => a.VisibilityState == VisibilityState.PartiallyVisible) ??
			TextView.TextViewLines.Last();
		ITextViewLine GetTopFullyVisibleLine() =>
			TextView.TextViewLines.FirstOrDefault(a => a.VisibilityState == VisibilityState.FullyVisible) ??
			TextView.TextViewLines.FirstOrDefault(a => a.VisibilityState == VisibilityState.PartiallyVisible) ??
			TextView.TextViewLines.First();

		public void MoveToBottomOfView(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(GetBottomFullyVisibleLine());
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void MoveToTopOfView(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(GetTopFullyVisibleLine());
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void MoveToEndOfDocument(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			var newPoint = new SnapshotPoint(Snapshot, Snapshot.Length);
			var line = TextView.GetTextViewLineContainingBufferPosition(newPoint);
			switch (line.VisibilityState) {
			case VisibilityState.FullyVisible:
				break;

			case VisibilityState.PartiallyVisible:
				TextView.DisplayTextLineContainingBufferPosition(newPoint, 0,
					line.Top - 0.01 >= TextView.ViewportTop || line.Height + 0.01 >= TextView.ViewportHeight ?
					ViewRelativePosition.Top : ViewRelativePosition.Bottom);
				break;

			case VisibilityState.Hidden:
			case VisibilityState.Unattached:
			default:
				TextView.DisplayTextLineContainingBufferPosition(newPoint, 0, ViewRelativePosition.Bottom);
				break;
			}

			Caret.MoveTo(newPoint);
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void MoveToEndOfLine(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			var textLine = Caret.ContainingTextViewLine;
			if (!textLine.IsLastTextViewLineForSnapshotLine)
				Caret.MoveTo(textLine.End, PositionAffinity.Predecessor, true);
			else {
				VirtualSnapshotPoint newPoint;
				var line = Caret.Position.VirtualBufferPosition.Position.GetContainingLine();
				if (Selection.Mode == TextSelectionMode.Box || line.Length != 0 || Caret.Position.VirtualSpaces > 0 || Caret.Position.BufferPosition > line.Start || !textLine.IsFirstTextViewLineForSnapshotLine)
					newPoint = new VirtualSnapshotPoint(textLine.End);
				else {
					int column = IndentHelper.GetDesiredIndentation(TextView, smartIndentationService, line) ?? 0;
					newPoint = new VirtualSnapshotPoint(textLine.Start, column);
				}

				Caret.MoveTo(newPoint);
			}

			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		static SnapshotPoint SkipWhitespace(ITextViewLine line) {
			int pos = line.Start.Position;
			int end = line.End.Position;
			var snapshot = line.Snapshot;
			while (pos < end && char.IsWhiteSpace(snapshot[pos]))
				pos++;
			return new SnapshotPoint(snapshot, pos);
		}

		static SnapshotPoint SkipWhitespaceEOL(ITextViewLine line) {
			if (line.Start == line.End)
				return line.End;
			int pos = line.End.Position - 1;
			int start = line.Start.Position;
			var snapshot = line.Snapshot;
			while (pos >= start && char.IsWhiteSpace(snapshot[pos]))
				pos--;
			return new SnapshotPoint(snapshot, pos + 1);
		}

		public void MoveToHome(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			var line = Caret.ContainingTextViewLine;
			var newPos = SkipWhitespace(line);
			if (newPos == Caret.Position.BufferPosition)
				newPos = line.Start;

			Caret.MoveTo(newPos);
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void MoveToLastNonWhiteSpaceCharacter(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(SkipWhitespaceEOL(Caret.ContainingTextViewLine));
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void MoveToNextCharacter(bool extendSelection) {
			if (!extendSelection && !Selection.IsEmpty) {
				if (Caret.Position.VirtualBufferPosition != Selection.End)
					Caret.MoveTo(Selection.End);
				Caret.EnsureVisible();
				Selection.Clear();
				return;
			}

			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			if (!Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId))
				Caret.MoveToNextCaretPosition();
			else {
				var line = Snapshot.GetLineFromPosition(Caret.Position.BufferPosition.Position);
				if (Caret.InVirtualSpace || Caret.Position.BufferPosition.Position >= line.End.Position)
					Caret.MoveTo(new VirtualSnapshotPoint(line.End, Caret.Position.VirtualSpaces + 1));
				else
					Caret.MoveToNextCaretPosition();
			}
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void MoveToNextWord(bool extendSelection) {
			var point = GetPointOfRightWord(Caret.Position.VirtualBufferPosition);
			if (!extendSelection)
				Selection.Clear();
			else {
				VirtualSnapshotPoint anchor;
				if (Selection.IsEmpty)
					anchor = Caret.Position.VirtualBufferPosition;
				else
					anchor = Selection.AnchorPoint;
				var active = new VirtualSnapshotPoint(point);
				Selection.Select(anchor, active);
			}
			Caret.MoveTo(point);
			Caret.EnsureVisible();
		}

		public void MoveToPreviousCharacter(bool extendSelection) {
			if (!extendSelection && !Selection.IsEmpty) {
				if (Caret.Position.VirtualBufferPosition != Selection.Start)
					Caret.MoveTo(Selection.Start);
				Caret.EnsureVisible();
				Selection.Clear();
				return;
			}

			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			if (!Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId))
				Caret.MoveToPreviousCaretPosition();
			else {
				if (Caret.InVirtualSpace)
					Caret.MoveTo(new VirtualSnapshotPoint(Caret.Position.BufferPosition, Caret.Position.VirtualSpaces - 1));
				else {
					var line = Snapshot.GetLineFromPosition(Caret.Position.BufferPosition.Position);
					if (line.Start != Caret.Position.BufferPosition)
						Caret.MoveToPreviousCaretPosition();
				}
			}
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void MoveToPreviousWord(bool extendSelection) {
			var span = GetSpanOfLeftWord(Caret.Position.VirtualBufferPosition);
			if (!extendSelection)
				Selection.Clear();
			else {
				VirtualSnapshotPoint anchor;
				if (Selection.IsEmpty)
					anchor = Caret.Position.VirtualBufferPosition;
				else
					anchor = Selection.AnchorPoint;
				var active = new VirtualSnapshotPoint(span.Start);
				Selection.Select(anchor, active);
			}
			Caret.MoveTo(span.Start);
			Caret.EnsureVisible();
		}

		public void MoveToStartOfDocument(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			var newPoint = new SnapshotPoint(Snapshot, 0);
			TextView.DisplayTextLineContainingBufferPosition(newPoint, 0, ViewRelativePosition.Top);
			Caret.MoveTo(newPoint);

			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void MoveToStartOfLine(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(Caret.ContainingTextViewLine.Start);
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void MoveToStartOfLineAfterWhiteSpace(bool extendSelection) =>
			MoveToStartOfLineAfterWhiteSpace(Caret.ContainingTextViewLine, extendSelection);
		public void MoveToStartOfNextLineAfterWhiteSpace(bool extendSelection) =>
			MoveToStartOfLineAfterWhiteSpace(TextView.GetTextViewLineContainingBufferPosition(Caret.ContainingTextViewLine.EndIncludingLineBreak), extendSelection);
		public void MoveToStartOfPreviousLineAfterWhiteSpace(bool extendSelection) =>
			MoveToStartOfLineAfterWhiteSpace(TextView.GetTextViewLineContainingBufferPosition(Caret.ContainingTextViewLine.Start.Position == 0 ? Caret.ContainingTextViewLine.Start : Caret.ContainingTextViewLine.Start - 1), extendSelection);
		void MoveToStartOfLineAfterWhiteSpace(ITextViewLine line, bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(SkipWhitespace(line));
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public bool NormalizeLineEndings(string replacement) {
			return true;//TODO:
		}

		public bool OpenLineAbove() {
			Selection.Clear();
			// VS doesn't use Caret.ContainingTextViewLine, perhaps that's a bug, but we do the same thing.
			// This is only important if the caret is at the end of a line (press END) and the line is
			// wrapped to the next line.
			var viewLine = TextView.GetTextViewLineContainingBufferPosition(Caret.Position.BufferPosition);
			if (viewLine.Start.GetContainingLine().LineNumber == 0)
				return OpenLine(new SnapshotPoint(Snapshot, 0), new SnapshotPoint(Snapshot, 0), false);
			return OpenLine(viewLine.Start, viewLine.Start, !viewLine.IsFirstTextViewLineForSnapshotLine);
		}

		public bool OpenLineBelow() {
			Selection.Clear();
			var viewLine = Caret.ContainingTextViewLine;
			return OpenLine(viewLine.Start, viewLine.End, true);
		}

		bool OpenLine(SnapshotPoint linebreakPos, SnapshotPoint insertPos, bool forward) {
			var linebreak = GetLineBreak(linebreakPos);
			TextBuffer.Insert(insertPos.Position, linebreak);
			var line = Snapshot.GetLineFromPosition(insertPos.Position + (forward ? linebreak.Length : 0));
			VirtualSnapshotPoint newPoint;
			if (Selection.Mode != TextSelectionMode.Box && line.Length == 0) {
				int column = IndentHelper.GetDesiredIndentation(TextView, smartIndentationService, line) ?? 0;
				newPoint = new VirtualSnapshotPoint(line.Start, column);
			}
			else
				newPoint = new VirtualSnapshotPoint(line.Start);
			Caret.MoveTo(newPoint);
			Caret.EnsureVisible();
			return true;
		}

		string GetLineBreak(SnapshotPoint pos) => Options.GetLineBreak(pos);

		public void PageDown(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			var line = TextView.TextViewLines.LastVisibleLine;

			bool lastViewLineIsFullyVisible = line.VisibilityState == VisibilityState.FullyVisible && line.IsLastDocumentLine();
			if (!lastViewLineIsFullyVisible) {
				ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Down);
				Caret.MoveToPreferredCoordinates();
			}
			else
				Caret.MoveTo(TextView.GetLastFullyVisibleLineOrGetNew());

			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public void PageUp(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			var caretLine = Caret.ContainingTextViewLine;
			bool caretLineIsVisible = caretLine.IsVisible();
			var line = TextView.TextViewLines.FirstVisibleLine;

			bool firstViewLineIsFullyVisible = line.VisibilityState == VisibilityState.FullyVisible && line.IsFirstDocumentLine();
			if (!firstViewLineIsFullyVisible) {
				ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Up);
				var newFirstLine = TextView.GetFirstFullyVisibleLineOrGetNew();
				if (newFirstLine.IsFirstDocumentLine() && Caret.ContainingTextViewLine.IsVisible() == caretLineIsVisible)
					Caret.MoveTo(newFirstLine);
				else
					Caret.MoveToPreferredCoordinates();
			}
			else
				Caret.MoveTo(TextView.GetFirstFullyVisibleLineOrGetNew());

			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.VirtualBufferPosition);
			else
				Selection.Clear();
		}

		public int ReplaceAllMatches(string searchText, string replaceText, bool matchCase, bool matchWholeWord, bool useRegularExpressions) {
			return 0;//TODO:
		}

		public bool ReplaceSelection(string text) {
			return true;//TODO:
		}

		public bool ReplaceText(Span replaceSpan, string text) {
			return true;//TODO:
		}

		public void ResetSelection() => Selection.Clear();

		public void ScrollColumnLeft() {
			var wpfView = TextView as IWpfTextView;
			if (wpfView != null)
				wpfView.ViewScroller.ScrollViewportHorizontallyByPixels(-wpfView.FormattedLineSource.ColumnWidth);
		}

		public void ScrollColumnRight() {
			var wpfView = TextView as IWpfTextView;
			if (wpfView != null)
				wpfView.ViewScroller.ScrollViewportHorizontallyByPixels(wpfView.FormattedLineSource.ColumnWidth);
		}

		public void ScrollDownAndMoveCaretIfNecessary() {
			ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Down);
			var line = Caret.ContainingTextViewLine;
			if (line.VisibilityState != VisibilityState.FullyVisible)
				Caret.MoveTo(line.Top < TextView.ViewportTop ? TextView.GetFirstFullyVisibleLineOrGetNew() : TextView.GetLastFullyVisibleLineOrGetNew());
		}

		public void ScrollUpAndMoveCaretIfNecessary() {
			ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Up);
			var line = Caret.ContainingTextViewLine;
			if (line.VisibilityState != VisibilityState.FullyVisible)
				Caret.MoveTo(line.Top < TextView.ViewportTop ? TextView.GetFirstFullyVisibleLineOrGetNew() : TextView.GetLastFullyVisibleLineOrGetNew());
		}

		public void ScrollLineBottom() {
			return;//TODO:
		}

		public void ScrollLineCenter() {
			return;//TODO:
		}

		public void ScrollLineTop() {
			return;//TODO:
		}

		public void ScrollPageDown() {
			return;//TODO:
		}

		public void ScrollPageUp() {
			return;//TODO:
		}

		public void SelectAll() =>
			SelectAndMove(new SnapshotSpan(new SnapshotPoint(Snapshot, 0), Snapshot.Length));

		void SelectAndMove(SnapshotSpan span) {
			Selection.Select(span, false);
			Caret.MoveTo(span.End);
			Caret.EnsureVisible();
		}

		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint) =>
			SelectAndMoveCaret(anchorPoint, activePoint, TextSelectionMode.Stream, EnsureSpanVisibleOptions.MinimumScroll);
		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode) =>
			SelectAndMoveCaret(anchorPoint, activePoint, selectionMode, EnsureSpanVisibleOptions.MinimumScroll);
		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode, EnsureSpanVisibleOptions? scrollOptions) {
			anchorPoint = anchorPoint.TranslateTo(Snapshot);
			activePoint = activePoint.TranslateTo(Snapshot);
			if (anchorPoint == activePoint)
				Selection.Clear();
			else
				Selection.Select(anchorPoint, activePoint);
			Selection.Mode = selectionMode;
			Caret.MoveTo(activePoint);
			if (scrollOptions == null)
				return;
			if (activePoint > anchorPoint)
				ViewScroller.EnsureSpanVisible(new SnapshotSpan(anchorPoint.Position, activePoint.Position), scrollOptions.Value & ~EnsureSpanVisibleOptions.ShowStart);
			else
				ViewScroller.EnsureSpanVisible(new SnapshotSpan(activePoint.Position, anchorPoint.Position), scrollOptions.Value | EnsureSpanVisibleOptions.ShowStart);
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
			return;//TODO:
		}

		public void SelectFirstChild() {
			return;//TODO:
		}

		public void SelectLine(ITextViewLine viewLine, bool extendSelection) {
			if (viewLine == null)
				throw new ArgumentNullException(nameof(viewLine));

			VirtualSnapshotPoint anchorPoint, activePoint;
			var lineStart = new VirtualSnapshotPoint(viewLine.Start);
			var lineEnd = new VirtualSnapshotPoint(viewLine.EndIncludingLineBreak);

			if (Selection.IsEmpty || !extendSelection) {
				anchorPoint = lineStart;
				activePoint = lineEnd;
			}
			else {
				if (Selection.ActivePoint <= Selection.AnchorPoint) {
					if (lineStart <= Selection.ActivePoint) {
						anchorPoint = Selection.AnchorPoint;
						activePoint = lineStart;
					}
					else {
						anchorPoint = Selection.AnchorPoint;
						activePoint = lineEnd;
					}
				}
				else {
					if (Selection.ActivePoint <= lineEnd) {
						anchorPoint = Selection.AnchorPoint;
						activePoint = lineEnd;
					}
					else {
						anchorPoint = Selection.AnchorPoint;
						activePoint = lineStart;
					}
				}
			}
			Selection.Select(anchorPoint, activePoint);
			Selection.Mode = TextSelectionMode.Stream;
			Caret.MoveTo(activePoint);
			Caret.EnsureVisible();
		}

		public void SelectNextSibling(bool extendSelection) {
			return;//TODO:
		}

		public void SelectPreviousSibling(bool extendSelection) {
			return;//TODO:
		}

		public void SwapCaretAndAnchor() {
			Selection.Select(anchorPoint: Selection.ActivePoint, activePoint: Selection.AnchorPoint);
			Caret.MoveTo(Selection.ActivePoint);
			Caret.EnsureVisible();
		}

		public bool Tabify() {
			return true;//TODO:
		}

		public bool ToggleCase() {
			return true;//TODO:
		}

		public bool TransposeCharacter() {
			return true;//TODO:
		}

		public bool TransposeLine() {
			return true;//TODO:
		}

		public bool TransposeWord() {
			return true;//TODO:
		}

		public bool Unindent() {
			return true;//TODO:
		}

		public bool Untabify() {
			return true;//TODO:
		}

		IWpfTextView GetZoomableView() {
			if (!Roles.Contains(PredefinedTextViewRoles.Zoomable))
				return null;
			var wpfView = TextView as IWpfTextView;
			Debug.Assert(wpfView != null);
			return wpfView;
		}

		void SetZoom(IWpfTextView wpfView, double newZoom) {
			if (newZoom < ZoomConstants.MinZoom || newZoom > ZoomConstants.MaxZoom)
				return;
			// VS writes to the global options, instead of the text view's options
			wpfView.Options.GlobalOptions.SetOptionValue(DefaultWpfViewOptions.ZoomLevelId, newZoom);
		}

		public void ZoomIn() {
			var wpfView = GetZoomableView();
			if (wpfView == null)
				return;
			SetZoom(wpfView, wpfView.ZoomLevel * ZoomConstants.ScalingFactor);
		}

		public void ZoomOut() {
			var wpfView = GetZoomableView();
			if (wpfView == null)
				return;
			SetZoom(wpfView, wpfView.ZoomLevel / ZoomConstants.ScalingFactor);
		}

		public void ZoomTo(double zoomLevel) {
			var wpfView = GetZoomableView();
			if (wpfView == null)
				return;
			SetZoom(wpfView, zoomLevel);
		}
	}
}
