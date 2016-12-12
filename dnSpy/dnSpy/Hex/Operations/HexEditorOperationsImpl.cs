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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Operations;
using dnSpy.Controls;
using dnSpy.Hex.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Operations {
	sealed class HexEditorOperationsImpl : HexEditorOperations {
		const ulong htmlMaxTotalBytesToCopy = 1 * 1024 * 1024;
		const ulong textMaxTotalBytesToCopy = 10 * 1024 * 1024;
		const ulong bytesMaxTotalBytesToCopy = 10 * 1024 * 1024;

		public override HexView HexView { get; }
		public override HexBufferSpan? ProvisionalCompositionSpan { get; set; }

		HexSelection Selection => HexView.Selection;
		HexCaret Caret => HexView.Caret;
		HexBuffer Buffer => HexView.Buffer;
		HexBufferLineFormatter BufferLines => HexView.BufferLines;
		VSTE.ITextViewRoleSet Roles => HexView.Roles;
		HexViewScroller ViewScroller => HexView.ViewScroller;
		HexBufferPoint ActiveCaretBufferPosition => Caret.Position.Position.ActivePosition.BufferPosition;

		readonly HexHtmlBuilderService htmlBuilderService;

		public HexEditorOperationsImpl(HexView hexView, HexHtmlBuilderService htmlBuilderService) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			if (htmlBuilderService == null)
				throw new ArgumentNullException(nameof(htmlBuilderService));
			HexView = hexView;
			HexView.Closed += HexView_Closed;
			this.htmlBuilderService = htmlBuilderService;
		}

		void HexView_Closed(object sender, EventArgs e) {
			HexView.Closed -= HexView_Closed;
			HexEditorOperationsFactoryServiceImpl.RemoveFromProperties(this);
		}

		HexBufferPoint GetAnchorPositionOrCaretIfNoSelection() {
			HexBufferPoint anchorPoint, activePoint;
			GetSelectionOrCaretIfNoSelection(out anchorPoint, out activePoint);
			return anchorPoint;
		}

		void GetSelectionOrCaretIfNoSelection(out HexBufferPoint anchorPoint, out HexBufferPoint activePoint) {
			if (!Selection.IsEmpty) {
				anchorPoint = Selection.AnchorPoint;
				activePoint = Selection.ActivePoint;
				if (anchorPoint > activePoint)
					anchorPoint = anchorPoint - 1;
			}
			else {
				anchorPoint = ActiveCaretBufferPosition;
				activePoint = anchorPoint;
			}
		}

		struct SelectionInfo {
			public HexBufferPoint AnchorPoint { get; }
			public HexBufferPoint ActivePoint { get; }
			public HexBufferPoint CaretPosition { get; }

			public SelectionInfo(HexBufferPoint anchorPoint, HexBufferPoint activePoint, HexBufferPoint caretPosition) {
				AnchorPoint = anchorPoint;
				ActivePoint = activePoint;
				CaretPosition = caretPosition;
			}
		}

		SelectionInfo GetSelectionInfoToCaret(HexBufferPoint anchorPoint, HexBufferPoint caretPosition) {
			if (caretPosition < anchorPoint)
				return new SelectionInfo(TryInc(anchorPoint), caretPosition, caretPosition);
			return new SelectionInfo(anchorPoint, TryInc(caretPosition), caretPosition);
		}

		HexBufferPoint TryInc(HexBufferPoint anchorPoint) {
			if (anchorPoint == BufferLines.BufferEnd)
				return anchorPoint;
			return anchorPoint + 1;
		}

		void SelectToCaret(HexBufferPoint anchorPoint) {
			var info = GetSelectionInfoToCaret(anchorPoint, ActiveCaretBufferPosition);
			Selection.Select(info.AnchorPoint, info.ActivePoint, alignPoints: true);
		}

		void MoveCaretToSelection(HexBufferPoint anchorPoint, HexBufferPoint activePoint) {
			if (activePoint <= anchorPoint)
				Caret.MoveTo(activePoint);
			else
				Caret.MoveTo(activePoint - 1);
		}

		public override void SelectAndMoveCaret(HexColumnType column, HexBufferPoint anchorPoint, HexBufferPoint activePoint, bool alignPoints, VSTE.EnsureSpanVisibleOptions? scrollOptions) {
			if (anchorPoint.IsDefault)
				throw new ArgumentException();
			if (activePoint.IsDefault)
				throw new ArgumentException();
			if (anchorPoint == activePoint)
				Selection.Clear();
			else
				Selection.Select(anchorPoint, activePoint, alignPoints);
			MoveCaretToSelection(anchorPoint, activePoint);
			if (scrollOptions == null)
				return;
			var options = Selection.IsReversed ? VSTE.EnsureSpanVisibleOptions.ShowStart | VSTE.EnsureSpanVisibleOptions.MinimumScroll : VSTE.EnsureSpanVisibleOptions.ShowStart;
			var flags = Caret.Position.Position.ActiveColumn == HexColumnType.Values ? HexSpanSelectionFlags.Values : HexSpanSelectionFlags.Ascii;
			flags |= HexSpanSelectionFlags.Cell;
			if (activePoint > anchorPoint)
				ViewScroller.EnsureSpanVisible(new HexBufferSpan(anchorPoint, activePoint), flags, scrollOptions.Value & ~VSTE.EnsureSpanVisibleOptions.ShowStart);
			else
				ViewScroller.EnsureSpanVisible(new HexBufferSpan(activePoint, anchorPoint), flags, scrollOptions.Value | VSTE.EnsureSpanVisibleOptions.ShowStart);
		}

		public override void MoveToNextCharacter(bool extendSelection) {
			if (!extendSelection && !Selection.IsEmpty) {
				var newEndPos = Selection.End - 1;
				if (ActiveCaretBufferPosition != newEndPos)
					Caret.MoveTo(newEndPos);
				Caret.EnsureVisible();
				Selection.Clear();
				return;
			}

			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveToNextCaretPosition();
			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void MoveToPreviousCharacter(bool extendSelection) {
			if (!extendSelection && !Selection.IsEmpty) {
				if (ActiveCaretBufferPosition != Selection.Start)
					Caret.MoveTo(Selection.Start);
				Caret.EnsureVisible();
				Selection.Clear();
				return;
			}

			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveToPreviousCaretPosition();
			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void MoveToNextWord(bool extendSelection) {
			switch (Caret.Position.Position.ActiveColumn) {
			case HexColumnType.Values:
				var line = Caret.ContainingHexViewLine.BufferLine;
				var position = BufferLines.FilterAndVerify(ActiveCaretBufferPosition);
				var cell = line.ValueCells.GetCell(position);
				if (cell == null)
					return;

				var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
				if (cell.BufferEnd >= BufferLines.BufferEnd)
					break;
				Caret.MoveTo(cell.BufferEnd);
				Caret.EnsureVisible();
				if (extendSelection)
					SelectToCaret(anchorPoint);
				else
					Selection.Clear();
				break;

			case HexColumnType.Ascii:
				MoveToNextCharacter(extendSelection);
				break;

			case HexColumnType.Offset:
			default:
				throw new InvalidOperationException();
			}
		}

		public override void MoveToPreviousWord(bool extendSelection) {
			switch (Caret.Position.Position.ActiveColumn) {
			case HexColumnType.Values:
				var line = Caret.ContainingHexViewLine.BufferLine;

				var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
				if (Caret.Position.Position.ActivePosition.CellPosition != 0)
					Caret.MoveTo(ActiveCaretBufferPosition);
				else {
					var position = BufferLines.FilterAndVerify(ActiveCaretBufferPosition);
					var cell = line.ValueCells.GetCell(position);
					if (cell == null)
						return;

					if (cell.BufferStart <= BufferLines.BufferStart)
						break;
					Caret.MoveTo(cell.BufferStart - 1);
				}
				Caret.EnsureVisible();
				if (extendSelection)
					SelectToCaret(anchorPoint);
				else
					Selection.Clear();
				break;

			case HexColumnType.Ascii:
				MoveToPreviousCharacter(extendSelection);
				break;

			case HexColumnType.Offset:
			default:
				throw new InvalidOperationException();
			}
		}

		public override void MoveLineUp(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			HexViewLine line;
			if (extendSelection || Selection.IsEmpty)
				line = Caret.ContainingHexViewLine;
			else
				line = HexView.GetHexViewLineContainingBufferPosition(Selection.Start);
			if (line.BufferStart <= BufferLines.BufferStart)
				Caret.MoveTo(line);
			else {
				var prevLine = HexView.GetHexViewLineContainingBufferPosition(line.BufferStart - 1);
				Caret.MoveTo(prevLine);
			}

			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void MoveLineDown(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			HexViewLine line;
			if (extendSelection || Selection.IsEmpty)
				line = Caret.ContainingHexViewLine;
			else
				line = HexView.GetHexViewLineContainingBufferPosition(Selection.End);
			if (line.IsLastDocumentLine())
				Caret.MoveTo(line);
			else {
				var nextLine = HexView.GetHexViewLineContainingBufferPosition(line.BufferEnd);
				Caret.MoveTo(nextLine);
			}

			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void PageUp(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			var caretLine = Caret.ContainingHexViewLine;
			bool caretLineIsVisible = caretLine.IsVisible();
			var line = HexView.HexViewLines.FirstVisibleLine;

			bool firstViewLineIsFullyVisible = line.VisibilityState == VSTF.VisibilityState.FullyVisible && line.IsFirstDocumentLine();
			if (!firstViewLineIsFullyVisible) {
				ViewScroller.ScrollViewportVerticallyByPage(VSTE.ScrollDirection.Up);
				var newFirstLine = HexView.GetFirstFullyVisibleLine();
				if (newFirstLine.IsFirstDocumentLine() && Caret.ContainingHexViewLine.IsVisible() == caretLineIsVisible)
					Caret.MoveTo(newFirstLine);
				else
					Caret.MoveToPreferredCoordinates();
			}
			else
				Caret.MoveTo(HexView.GetFirstFullyVisibleLine());

			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void PageDown(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			var line = HexView.HexViewLines.LastVisibleLine;

			bool lastViewLineIsFullyVisible = line.VisibilityState == VSTF.VisibilityState.FullyVisible && line.IsLastDocumentLine();
			if (!lastViewLineIsFullyVisible) {
				ViewScroller.ScrollViewportVerticallyByPage(VSTE.ScrollDirection.Down);
				Caret.MoveToPreferredCoordinates();
			}
			else
				Caret.MoveTo(HexView.GetLastFullyVisibleLine());

			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void MoveToEndOfLine(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			var lineSpan = Caret.ContainingHexViewLine.BufferSpan;
			if (lineSpan.Length != 0)
				Caret.MoveTo(lineSpan.End - 1);
			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void MoveToStartOfLine(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(Caret.ContainingHexViewLine.BufferStart);
			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void MoveToStartOfDocument(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			var newPoint = BufferLines.BufferStart;
			HexView.DisplayHexLineContainingBufferPosition(newPoint, 0, VSTE.ViewRelativePosition.Top);
			Caret.MoveTo(newPoint);
			Caret.EnsureVisible();

			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void MoveToEndOfDocument(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			var newPoint = BufferLines.BufferEnd;
			var line = HexView.GetHexViewLineContainingBufferPosition(newPoint);
			switch (line.VisibilityState) {
			case VSTF.VisibilityState.FullyVisible:
				break;

			case VSTF.VisibilityState.PartiallyVisible:
			case VSTF.VisibilityState.Hidden:
				HexView.DisplayHexLineContainingBufferPosition(newPoint, 0,
					line.Top - 0.01 >= HexView.ViewportTop || line.Height + 0.01 >= HexView.ViewportHeight ?
					VSTE.ViewRelativePosition.Bottom : VSTE.ViewRelativePosition.Top);
				break;

			case VSTF.VisibilityState.Unattached:
			default:
				HexView.DisplayHexLineContainingBufferPosition(newPoint, 0, VSTE.ViewRelativePosition.Bottom);
				break;
			}

			Caret.MoveTo(newPoint);
			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void MoveCurrentLineToTop() =>
			HexView.DisplayHexLineContainingBufferPosition(ActiveCaretBufferPosition, 0, VSTE.ViewRelativePosition.Top);

		public override void MoveCurrentLineToBottom() =>
			HexView.DisplayHexLineContainingBufferPosition(ActiveCaretBufferPosition, 0, VSTE.ViewRelativePosition.Bottom);

		HexViewLine GetBottomFullyVisibleLine() =>
			HexView.HexViewLines.LastOrDefault(a => a.VisibilityState == VSTF.VisibilityState.FullyVisible) ??
			HexView.HexViewLines.LastOrDefault(a => a.VisibilityState == VSTF.VisibilityState.PartiallyVisible) ??
			HexView.HexViewLines.Last();
		HexViewLine GetTopFullyVisibleLine() =>
			HexView.HexViewLines.FirstOrDefault(a => a.VisibilityState == VSTF.VisibilityState.FullyVisible) ??
			HexView.HexViewLines.FirstOrDefault(a => a.VisibilityState == VSTF.VisibilityState.PartiallyVisible) ??
			HexView.HexViewLines.First();

		public override void MoveToTopOfView(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(GetTopFullyVisibleLine());
			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void MoveToBottomOfView(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(GetBottomFullyVisibleLine());
			Caret.EnsureVisible();
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void SwapCaretAndAnchor() {
			Selection.Select(anchorPoint: Selection.ActivePoint, activePoint: Selection.AnchorPoint, alignPoints: false);
			MoveCaretToSelection(Selection.AnchorPoint, Selection.ActivePoint);
			Caret.EnsureVisible();
		}

		public override bool InsertText(string text) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));

			switch (Caret.Position.Position.ActiveColumn) {
			case HexColumnType.Values:
				return InsertTextValues(Caret.Position.Position.ValuePosition, text);

			case HexColumnType.Ascii:
				return InsertTextAscii(Caret.Position.Position.AsciiPosition, text);

			case HexColumnType.Offset:
			default:
				throw new InvalidOperationException();
			}
		}

		bool InsertTextValues(HexCellPosition cellPosition, string text) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (text.Length == 0)
				return true;
			if (text.Length != 1)
				return false;

			var bufferLines = BufferLines;
			if (!bufferLines.CanEditValueCell)
				return false;
			var line = bufferLines.GetLineFromPosition(cellPosition.BufferPosition);
			var cell = line.ValueCells.GetCell(cellPosition.BufferPosition);
			if (cell == null)
				return false;
			if ((uint)cellPosition.CellPosition >= (uint)cell.CellSpan.Length)
				return false;
			var newValue = bufferLines.EditValueCell(cell, cellPosition.CellPosition, text[0]);
			if (newValue == null)
				return false;

			using (var ed = HexView.Buffer.CreateEdit()) {
				if (!ed.Replace(newValue.Value.Position, newValue.Value.Data))
					return false;
				ed.Apply();
			}

			MoveToNextCharacter(false);
			return true;
		}

		bool InsertTextAscii(HexCellPosition cellPosition, string text) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (text.Length == 0)
				return true;

			var encoding = Options.TryGetEncoding();
			Debug.Assert(encoding != null);
			if (encoding == null)
				return false;

			var bytes = encoding.GetBytes(text);
			using (var ed = HexView.Buffer.CreateEdit()) {
				if (!ed.Replace(cellPosition.BufferPosition, bytes))
					return false;
				ed.Apply();
			}

			var newPos = cellPosition.BufferPosition.Position + bytes.LongLength;
			if (newPos > BufferLines.BufferEnd.Position)
				newPos = BufferLines.BufferEnd.Position;
			Caret.MoveTo(new HexBufferPoint(cellPosition.BufferPosition.Buffer, newPos));
			Caret.EnsureVisible();
			Selection.Clear();
			return true;
		}

		public override void SelectLine(HexViewLine viewLine, bool extendSelection) {
			if (viewLine == null)
				throw new ArgumentNullException(nameof(viewLine));

			HexBufferPoint anchorPoint, activePoint;
			var lineStart = viewLine.BufferStart;
			var lineEnd = viewLine.BufferEnd;

			if (Selection.IsEmpty || !extendSelection) {
				anchorPoint = lineStart;
				activePoint = lineEnd;
			}
			else {
				var anchorSpan = SelectionUtilities.GetLineAnchorSpan(Selection);
				if (anchorSpan.Start <= viewLine.BufferStart) {
					anchorPoint = anchorSpan.Start;
					activePoint = lineEnd;
				}
				else {
					anchorPoint = anchorSpan.End;
					activePoint = lineStart;
				}
			}
			Selection.Select(anchorPoint, activePoint, alignPoints: true);
			// This moves the caret outside the selection but it matches the text editor when
			// full lines are selected.
			Caret.MoveTo(activePoint);
			Caret.EnsureVisible();
		}

		public override void SelectCurrentWord() {
			if (BufferLines.BufferSpan.Length == 0)
				return;

			var position = BufferLines.FilterAndVerify(ActiveCaretBufferPosition);
			switch (Caret.Position.Position.ActiveColumn) {
			case HexColumnType.Values:
				var line = Caret.ContainingHexViewLine.BufferLine;
				var cell = line.ValueCells.GetCell(position);
				if (cell == null)
					return;
				SelectAndMove(cell.BufferSpan);
				break;

			case HexColumnType.Ascii:
				SelectAndMove(new HexBufferSpan(position, 1));
				break;

			case HexColumnType.Offset:
			default:
				throw new InvalidOperationException();
			}
		}

		public override void SelectAll() =>
			SelectAndMove(BufferLines.BufferSpan);

		void SelectAndMove(HexBufferSpan span) {
			Selection.Select(span, false, true);
			MoveCaretToSelection(span.Start, span.End);
			Caret.EnsureVisible();
		}

		public override void ExtendSelection(HexBufferPoint newEnd) {
			if (newEnd.IsDefault)
				throw new ArgumentException();
			if (!BufferLines.IsValidPosition(newEnd))
				throw new ArgumentOutOfRangeException(nameof(newEnd));
			Selection.Select(Selection.AnchorPoint, newEnd, alignPoints: true);
			MoveCaretToSelection(Selection.AnchorPoint, Selection.ActivePoint);
			var options = Selection.IsReversed ? VSTE.EnsureSpanVisibleOptions.ShowStart | VSTE.EnsureSpanVisibleOptions.MinimumScroll : VSTE.EnsureSpanVisibleOptions.ShowStart;
			var flags = Caret.Position.Position.ActiveColumn == HexColumnType.Values ? HexSpanSelectionFlags.Values : HexSpanSelectionFlags.Ascii;
			flags |= HexSpanSelectionFlags.Cell;
			ViewScroller.EnsureSpanVisible(Selection.StreamSelectionSpan, flags, options);
		}

		public override void MoveCaret(HexViewLine hexLine, double horizontalOffset, bool extendSelection, HexMoveToFlags flags) {
			if (hexLine == null)
				throw new ArgumentNullException(nameof(hexLine));

			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(hexLine, horizontalOffset, flags);
			if (extendSelection)
				SelectToCaret(anchorPoint);
			else
				Selection.Clear();
		}

		public override void ResetSelection() => Selection.Clear();

		const string VS_COPY_FULL_LINE_DATA_FORMAT = "VisualStudioEditorOperationsLineCutCopyClipboardTag";
		const string VS_COPY_BOX_DATA_FORMAT = "MSDEVColumnSelect";
		bool CopyToClipboard(string text, string htmlText, bool isFullLineData, bool isBoxData) {
			try {
				var dataObj = new DataObject();
				dataObj.SetText(text);
				if (isFullLineData)
					dataObj.SetData(VS_COPY_FULL_LINE_DATA_FORMAT, true);
				if (isBoxData)
					dataObj.SetData(VS_COPY_BOX_DATA_FORMAT, true);
				if (htmlText != null)
					dataObj.SetData(DataFormats.Html, htmlText);
				Clipboard.SetDataObject(dataObj);
				return true;
			}
			catch (ExternalException) {
				return false;
			}
		}

		string TryCreateHtmlText(HexBufferSpan span) => TryCreateHtmlText(new NormalizedHexBufferSpanCollection(span));
		string TryCreateHtmlText(NormalizedHexBufferSpanCollection spans) {
			if (spans.Count == 0)
				return null;

			// There's no way for us to cancel it so don't classify too much text
			var totalBytes = GetTotalBytes(spans);
			if (totalBytes > htmlMaxTotalBytesToCopy)
				return null;
			var cancellationToken = CancellationToken.None;

			return htmlBuilderService.GenerateHtmlFragment(spans, HexView, cancellationToken);
		}

		static HexPosition GetTotalBytes(NormalizedHexBufferSpanCollection spans) {
			var totalBytes = HexPosition.Zero;
			foreach (var span in spans)
				totalBytes += span.Length;
			return totalBytes;
		}

		bool CopyTooMuchDataError() => CopyToClipboard("Too much data selected", null, isFullLineData: false, isBoxData: false);

		public override bool CanCopy => !Selection.IsEmpty;
		public override bool CopySelectionBytes() {
			bool upper = !BufferLines.ValuesLowerCaseHex;
			var span = Selection.StreamSelectionSpan;
			var text = span.Length > bytesMaxTotalBytesToCopy ? null : ClipboardUtils.ToHexString(span, upper);
			if (text == null)
				return CopyTooMuchDataError();
			return CopyToClipboard(text, null, isFullLineData: false, isBoxData: false);
		}

		public override bool CopySelectionText() {
			string htmlText;
			if (Selection.IsEmpty) {
				var line = Caret.ContainingHexViewLine;
				var lineExtentSpan = line.BufferSpan;
				string lineText = line.Text;
				htmlText = TryCreateHtmlText(lineExtentSpan);
				return CopyToClipboard(lineText, htmlText, isFullLineData: true, isBoxData: false);
			}
			var spans = Selection.SelectedSpans;
			var totalBytes = GetTotalBytes(spans);
			if (totalBytes > textMaxTotalBytesToCopy)
				return CopyTooMuchDataError();
			var text = Selection.GetText();
			htmlText = TryCreateHtmlText(spans);
			return CopyToClipboard(text, htmlText, isFullLineData: false, isBoxData: false);
		}

		public override bool CopySpecial(HexCopySpecialKind copyKind) {
			switch (copyKind) {
			case HexCopySpecialKind.Utf8String:			return CopyString(Encoding.UTF8);
			case HexCopySpecialKind.UnicodeString:		return CopyString(Encoding.Unicode);
			case HexCopySpecialKind.CSharpArray:		return CopyCSharpArray();
			case HexCopySpecialKind.VisualBasicArray:	return CopyVisualBasicArray();
			case HexCopySpecialKind.Offset:				return CopyOffset();
			default:
				throw new ArgumentOutOfRangeException(nameof(copyKind));
			}
		}

		bool CopyString(Encoding encoding) {
			var span = Selection.StreamSelectionSpan;
			if (span.Length > bytesMaxTotalBytesToCopy)
				return CopyTooMuchDataError();
			var data = span.GetData();
			return CopyToClipboard(encoding.GetString(data), null, isFullLineData: false, isBoxData: false);
		}

		bool CopyCSharpArray() => CopyArray(Selection.StreamSelectionSpan, "0x", string.Empty, "new byte[] {", "};", string.Empty);
		bool CopyVisualBasicArray() => CopyArray(Selection.StreamSelectionSpan, "&H", string.Empty, "New Byte() {", "}", " _");

		bool CopyArray(HexBufferSpan span, string numberPrefix, string numberSuffix, string allocStringStart, string allocStringEnd, string eol) {
			if (span.Length > bytesMaxTotalBytesToCopy)
				return CopyTooMuchDataError();

			bool upper = !BufferLines.ValuesLowerCaseHex;
			const int BYTES_PER_LINE = 16;
			var sb = new StringBuilder();

			sb.Append(allocStringStart);
			sb.Append(eol);
			sb.AppendLine();
			var pos = span.Start;
			for (int i = 0; pos < span.End; i++) {
				if (i >= BYTES_PER_LINE) {
					i = 0;
					sb.Append(eol);
					sb.AppendLine();
				}
				if (i == 0)
					sb.Append('\t');
				else
					sb.Append(' ');

				sb.Append(numberPrefix);
				int b = pos.TryGetByte();
				if (b < 0)
					sb.Append("??");
				else {
					sb.Append(ClipboardUtils.NibbleToHex(b >> 4, upper));
					sb.Append(ClipboardUtils.NibbleToHex(b & 0x0F, upper));
				}
				sb.Append(numberSuffix);

				pos = pos + 1;
				if (pos >= span.End)
					break;
				sb.Append(',');
			}
			sb.Append(eol);
			sb.AppendLine();
			sb.Append(allocStringEnd);
			sb.AppendLine();

			return CopyToClipboard(sb.ToString(), null, isFullLineData: false, isBoxData: false);
		}

		bool CopyOffset() {
			var pos = BufferLines.ToLogicalPosition(Caret.Position.Position.ActivePosition.BufferPosition);
			var s = BufferLines.GetFormattedOffset(pos);
			return CopyToClipboard(s, null, isFullLineData: false, isBoxData: false);
		}

		public override bool CanPaste {
			get {
				switch (Caret.Position.Position.ActiveColumn) {
				case HexColumnType.Values:
					return ClipboardUtils.GetData(canBeEmpty: false) != null;

				case HexColumnType.Ascii:
					return ClipboardUtils.GetText(canBeEmpty: false) != null;

				case HexColumnType.Offset:
				default:
					throw new InvalidOperationException();
				}
			}
		}

		public override bool Paste() {
			switch (Caret.Position.Position.ActiveColumn) {
			case HexColumnType.Values:
				return PasteValues(Caret.Position.Position.ValuePosition);

			case HexColumnType.Ascii:
				return PasteAscii(Caret.Position.Position.AsciiPosition);

			case HexColumnType.Offset:
			default:
				throw new InvalidOperationException();
			}
		}

		bool PasteValues(HexCellPosition cellPosition) {
			var data = ClipboardUtils.GetData(canBeEmpty: false);
			if (data == null)
				return false;
			return PasteData(cellPosition, data);
		}

		bool PasteData(HexCellPosition cellPosition, byte[] data) {
			var line = BufferLines.GetLineFromPosition(cellPosition.BufferPosition);
			var cells = cellPosition.Column == HexColumnType.Values ? line.ValueCells : line.AsciiCells;
			var cell = cells.GetCell(cellPosition.BufferPosition);
			if (cell == null)
				return false;

			var pos = cell.BufferStart;
			using (var ed = HexView.Buffer.CreateEdit()) {
				if (!ed.Replace(pos, data))
					return false;
				ed.Apply();
			}

			var newPos = pos.Position + data.LongLength;
			if (newPos > BufferLines.BufferEnd.Position)
				newPos = BufferLines.BufferEnd.Position;
			Caret.MoveTo(new HexBufferPoint(pos.Buffer, newPos));
			Caret.EnsureVisible();
			Selection.Clear();
			return true;
		}

		bool PasteAscii(HexCellPosition cellPosition) {
			var text = ClipboardUtils.GetText(canBeEmpty: false);
			if (text == null)
				return false;
			return InsertTextAscii(cellPosition, text);
		}

		public override bool PasteSpecial(HexPasteSpecialKind pasteKind) {
			switch (pasteKind) {
			case HexPasteSpecialKind.Utf8String:	return PasteString(Encoding.UTF8);
			case HexPasteSpecialKind.UnicodeString:	return PasteString(Encoding.Unicode);
			case HexPasteSpecialKind.Blob:			return PasteBlob();
			default:
				throw new ArgumentOutOfRangeException(nameof(pasteKind));
			}
		}

		bool PasteString(Encoding encoding) {
			var text = ClipboardUtils.GetText(canBeEmpty: false);
			if (text == null)
				return false;
			return PasteData(encoding.GetBytes(text));
		}

		bool PasteBlob() {
			var data = ClipboardUtils.GetData(canBeEmpty: true);
			if (data == null)
				return false;
			return PasteData(GetBlobData(data));
		}

		static byte[] GetBlobData(byte[] data) {
			if (data == null)
				return null;
			uint len = (uint)data.Length;
			int extraLen = MDUtils.GetCompressedUInt32Length(len);
			if (extraLen < 0)
				return null;
			var d = new byte[data.Length + extraLen];
			MDUtils.WriteCompressedUInt32(d, 0, len);
			Array.Copy(data, 0, d, extraLen, data.Length);
			return d;
		}

		bool PasteData(byte[] data) {
			if (data.Length == 0)
				return true;
			return PasteData(Caret.Position.Position.ActivePosition, data);
		}

		public override void ScrollUpAndMoveCaretIfNecessary() => ScrollAndMoveCaretIfNecessary(VSTE.ScrollDirection.Up);
		public override void ScrollDownAndMoveCaretIfNecessary() => ScrollAndMoveCaretIfNecessary(VSTE.ScrollDirection.Down);
		void ScrollAndMoveCaretIfNecessary(VSTE.ScrollDirection scrollDirection) {
			var origCaretContainingHexViewLinePosition = Caret.ContainingHexViewLine.BufferStart;
			bool firstDocLineWasVisible = HexView.HexViewLines.FirstVisibleLine.IsFirstDocumentLine();
			ViewScroller.ScrollViewportVerticallyByLine(scrollDirection);

			var pos = ActiveCaretBufferPosition;
			var line = Caret.ContainingHexViewLine;
			var firstVisLine = HexView.HexViewLines.FirstVisibleLine;
			var lastVisLine = HexView.HexViewLines.LastVisibleLine;
			if (scrollDirection == VSTE.ScrollDirection.Up && firstDocLineWasVisible)
				lastVisLine = HexView.GetLastFullyVisibleLine();
			if (line.VisibilityState == VSTF.VisibilityState.Unattached)
				Caret.MoveTo(line.BufferStart <= firstVisLine.BufferStart ? firstVisLine : lastVisLine);
			else if (line.VisibilityState != VSTF.VisibilityState.FullyVisible) {
				if (scrollDirection == VSTE.ScrollDirection.Up) {
					var newLine = lastVisLine;
					if (newLine.BufferStart == origCaretContainingHexViewLinePosition) {
						if (newLine.BufferStart > BufferLines.BufferStart)
							newLine = HexView.HexViewLines.GetHexViewLineContainingBufferPosition(newLine.BufferStart - 1) ?? newLine;
					}
					Caret.MoveTo(newLine);
				}
				else {
					var newLine = firstVisLine;
					if (newLine.BufferStart == origCaretContainingHexViewLinePosition && !newLine.IsLastDocumentLine())
						newLine = HexView.HexViewLines.GetHexViewLineContainingBufferPosition(newLine.BufferEnd) ?? newLine;
					Caret.MoveTo(newLine);
				}
			}
			Caret.EnsureVisible();

			var newPos = ActiveCaretBufferPosition;
			if (newPos != pos)
				Selection.Clear();
		}

		public override void ScrollPageUp() => HexView.ViewScroller.ScrollViewportVerticallyByPage(VSTE.ScrollDirection.Up);
		public override void ScrollPageDown() => HexView.ViewScroller.ScrollViewportVerticallyByPage(VSTE.ScrollDirection.Down);

		public override void ScrollColumnLeft() {
			var wpfHexView = HexView as WpfHexView;
			Debug.Assert(wpfHexView != null);
			if (wpfHexView != null)
				wpfHexView.ViewScroller.ScrollViewportHorizontallyByPixels(-wpfHexView.FormattedLineSource.ColumnWidth);
		}

		public override void ScrollColumnRight() {
			var wpfHexView = HexView as WpfHexView;
			Debug.Assert(wpfHexView != null);
			if (wpfHexView != null)
				wpfHexView.ViewScroller.ScrollViewportHorizontallyByPixels(wpfHexView.FormattedLineSource.ColumnWidth);
		}

		public override void ScrollLineBottom() =>
			HexView.DisplayHexLineContainingBufferPosition(Caret.ContainingHexViewLine.BufferStart, 0, VSTE.ViewRelativePosition.Bottom);

		public override void ScrollLineTop() =>
			HexView.DisplayHexLineContainingBufferPosition(Caret.ContainingHexViewLine.BufferStart, 0, VSTE.ViewRelativePosition.Top);

		public override void ScrollLineCenter() {
			// line.Height depends on the line transform and it's set when the line is visible
			Caret.EnsureVisible();
			var line = Caret.ContainingHexViewLine;
			HexView.DisplayHexLineContainingBufferPosition(line.BufferStart, Math.Max(0, (HexView.ViewportHeight - line.Height) / 2), VSTE.ViewRelativePosition.Top);
		}

		WpfHexView GetZoomableView() {
			if (!Roles.Contains(PredefinedHexViewRoles.Zoomable))
				return null;
			var wpfHexView = HexView as WpfHexView;
			Debug.Assert(wpfHexView != null);
			return wpfHexView;
		}

		static bool UseGlobalZoomLevelOption(HexView hexView) => !hexView.Options.IsOptionDefined(DefaultWpfHexViewOptions.ZoomLevelId, true);

		void SetZoom(WpfHexView wpfHexView, double newZoom) {
			if (newZoom < VSTE.ZoomConstants.MinZoom || newZoom > VSTE.ZoomConstants.MaxZoom)
				return;
			// VS writes to the global options, instead of the text view's options
			var options = UseGlobalZoomLevelOption(wpfHexView) ? wpfHexView.Options.GlobalOptions : wpfHexView.Options;
			options.SetOptionValue(DefaultWpfHexViewOptions.ZoomLevelId, newZoom);
		}

		public override void ZoomIn() {
			var wpfHexView = GetZoomableView();
			if (wpfHexView == null)
				return;
			SetZoom(wpfHexView, ZoomSelector.ZoomIn(wpfHexView.ZoomLevel));
		}

		public override void ZoomOut() {
			var wpfHexView = GetZoomableView();
			if (wpfHexView == null)
				return;
			SetZoom(wpfHexView, ZoomSelector.ZoomOut(wpfHexView.ZoomLevel));
		}

		public override void ZoomTo(double zoomLevel) {
			var wpfHexView = GetZoomableView();
			if (wpfHexView == null)
				return;
			SetZoom(wpfHexView, zoomLevel);
		}

		public override void ToggleColumn() {
			Caret.ToggleActiveColumn();
			Caret.EnsureVisible();
		}

		HexBufferSpan? GetSelectedSpanOrCell() {
			if (!Selection.IsEmpty)
				return Selection.StreamSelectionSpan;

			var line = BufferLines.GetLineFromPosition(Caret.Position.Position.ActivePosition.BufferPosition);
			var cells = Caret.Position.Position.ActivePosition.Column == HexColumnType.Values ? line.ValueCells : line.AsciiCells;
			var cell = cells.GetCell(Caret.Position.Position.ActivePosition.BufferPosition);
			return cell?.BufferSpan;
		}

		public override bool ClearData() {
			var span = GetSelectedSpanOrCell();
			if (span == null)
				return false;
			if (span.Value.Length > bytesMaxTotalBytesToCopy)
				return false;
			var newData = new byte[span.Value.Length.ToUInt64()];

			var pos = span.Value.Start;
			using (var ed = HexView.Buffer.CreateEdit()) {
				if (!ed.Replace(pos, newData))
					return false;
				ed.Apply();
			}

			if (Selection.IsEmpty) {
				var newPos = pos.Position + newData.LongLength;
				if (newPos > BufferLines.BufferEnd.Position)
					newPos = BufferLines.BufferEnd.Position;
				Caret.MoveTo(new HexBufferPoint(pos.Buffer, newPos));
			}
			Caret.EnsureVisible();
			Selection.Clear();
			return true;
		}

		public override void MoveToStartOfNextValidSpan() {
			var startPos = ActiveCaretBufferPosition;
			var span = Buffer.GetNextValidSpan(startPos, BufferLines.EndPosition, fullSpan: false);
			if (span != null && span.Value.Contains(startPos) && span.Value.End < BufferLines.EndPosition)
				span = Buffer.GetNextValidSpan(span.Value.End, BufferLines.EndPosition, fullSpan: false);
			if (span != null && !span.Value.Contains(startPos))
				MoveToValidSpan(span);
		}

		public override void MoveToStartOfPreviousValidSpan() {
			var startPos = ActiveCaretBufferPosition;
			var span = Buffer.GetPreviousValidSpan(startPos, BufferLines.StartPosition, fullSpan: false);
			if (span != null && span.Value.Contains(startPos) && span.Value.Start > BufferLines.StartPosition)
				span = Buffer.GetPreviousValidSpan(span.Value.Start - 1, BufferLines.StartPosition, fullSpan: false);
			if (span != null && !span.Value.Contains(startPos))
				MoveToValidSpan(span);
		}

		void MoveToValidSpan(HexSpan? span) {
			if (span == null)
				return;
			var start = span.Value.Start;
			if (start < BufferLines.StartPosition)
				start = BufferLines.StartPosition;
			if (start > BufferLines.EndPosition)
				start = BufferLines.EndPosition;
			Caret.MoveTo(new HexBufferPoint(Buffer, start));
			HexView.DisplayHexLineContainingBufferPosition(ActiveCaretBufferPosition, 0, VSTE.ViewRelativePosition.Top);
			Selection.Clear();
		}

		public override void ShowAllBytes() => SetNewVisibleSpan(new HexBufferSpan(Buffer, Buffer.Span));
		public override void ShowOnlySelectedBytes() {
			if (Selection.IsEmpty)
				return;
			SetNewVisibleSpan(Selection.StreamSelectionSpan);
		}

		void SetNewVisibleSpan(HexBufferSpan span) {
			if (BufferLines.BufferSpan == span)
				return;
			Options.SetOptionValue(DefaultHexViewOptions.StartPositionId, span.Start);
			Options.SetOptionValue(DefaultHexViewOptions.EndPositionId, span.End);
			RedisplayHexLines();
		}

		void RedisplayHexLines() {
			var line = HexView.HexViewLines.FirstVisibleLine;
			var verticalDistance = line.Top - HexView.ViewportTop;
			var bufferPosition = line.BufferStart;
			HexView.DisplayHexLineContainingBufferPosition(bufferPosition, verticalDistance, VSTE.ViewRelativePosition.Top, null, null, DisplayHexLineOptions.CanRecreateBufferLines);
		}

		public override void Refresh() => HexView.Refresh();
	}
}
