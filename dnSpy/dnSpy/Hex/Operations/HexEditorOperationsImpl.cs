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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Operations;
using dnSpy.Controls;
using dnSpy.Hex.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Operations {
	sealed class HexEditorOperationsImpl : HexEditorOperations {
		public override HexView HexView { get; }
		public override HexBufferSpan? ProvisionalCompositionSpan { get; set; }
		public override string SelectedText { get; }//TODO:
		public override bool CanCopy { get; }//TODO:
		public override bool CanPaste { get; }//TODO:

		HexSelection Selection => HexView.Selection;
		HexCaret Caret => HexView.Caret;
		HexBuffer Buffer => HexView.Buffer;
		VSTE.ITextViewRoleSet Roles => HexView.Roles;
		HexViewScroller ViewScroller => HexView.ViewScroller;

		public HexEditorOperationsImpl(HexView hexView) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			HexView = hexView;
			HexView.Closed += HexView_Closed;
		}

		void HexView_Closed(object sender, EventArgs e) {
			HexView.Closed -= HexView_Closed;
			HexEditorOperationsFactoryServiceImpl.RemoveFromProperties(this);
		}

		// Filters input only if it's equal to the last position. The text editor API allows it
		// but not the hex editor API.
		HexBufferPoint Filter(HexBufferPoint position) {
			var span = HexView.BufferLines.BufferSpan;
			if (span.Contains(position))
				return position;
			if (span.End == position)
				return span.Length == 0 ? position : position - 1;
			return position;
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
			}
			else {
				anchorPoint = Caret.Position.Position.ActivePosition.BufferPosition;
				activePoint = anchorPoint;
			}
		}

		public override void SelectAndMoveCaret(HexColumnType column, HexBufferPoint anchorPoint, HexBufferPoint activePoint, VSTE.EnsureSpanVisibleOptions? scrollOptions) {
			if (anchorPoint.IsDefault)
				throw new ArgumentException();
			if (activePoint.IsDefault)
				throw new ArgumentException();
			if (anchorPoint == activePoint)
				Selection.Clear();
			else
				Selection.Select(anchorPoint, activePoint);
			Caret.MoveTo(activePoint);
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
				if (Caret.Position.Position.ActivePosition.BufferPosition != Selection.End)
					Caret.MoveTo(Filter(Selection.End));
				Caret.EnsureVisible();
				Selection.Clear();
				return;
			}

			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveToNextCaretPosition();
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
			else
				Selection.Clear();
		}

		public override void MoveToPreviousCharacter(bool extendSelection) {
			if (!extendSelection && !Selection.IsEmpty) {
				if (Caret.Position.Position.ActivePosition.BufferPosition != Selection.Start)
					Caret.MoveTo(Filter(Selection.Start));
				Caret.EnsureVisible();
				Selection.Clear();
				return;
			}

			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveToPreviousCaretPosition();
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
			else
				Selection.Clear();
		}

		public override void MoveToNextWord(bool extendSelection) {
			//TODO:
		}

		public override void MoveToPreviousWord(bool extendSelection) {
			//TODO:
		}

		public override void MoveLineUp(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			HexViewLine line;
			if (extendSelection || Selection.IsEmpty)
				line = Caret.ContainingHexViewLine;
			else
				line = HexView.GetHexViewLineContainingBufferPosition(Selection.Start);
			if (line.BufferSpan.Start <= HexView.BufferLines.BufferStart)
				Caret.MoveTo(line);
			else {
				var prevLine = HexView.GetHexViewLineContainingBufferPosition(line.BufferSpan.Start - 1);
				Caret.MoveTo(prevLine);
			}

			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
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
				var nextLine = HexView.GetHexViewLineContainingBufferPosition(line.BufferSpan.End);
				Caret.MoveTo(nextLine);
			}

			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
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
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
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
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
			else
				Selection.Clear();
		}

		public override void MoveToEndOfLine(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			if (Caret.ContainingHexViewLine.BufferSpan.Length != 0)
				Caret.MoveTo(Caret.ContainingHexViewLine.BufferSpan.End - 1);
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
			else
				Selection.Clear();
		}

		public override void MoveToStartOfLine(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(Caret.ContainingHexViewLine.BufferSpan.Start);
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
			else
				Selection.Clear();
		}

		public override void MoveToStartOfDocument(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			var newPoint = HexView.BufferLines.BufferStart;
			HexView.DisplayHexLineContainingBufferPosition(newPoint, 0, VSTE.ViewRelativePosition.Top);
			Caret.MoveTo(newPoint);
			Caret.EnsureVisible();

			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
			else
				Selection.Clear();
		}

		public override void MoveToEndOfDocument(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();

			var fullSpan = HexView.BufferLines.BufferSpan;
			var newPoint = fullSpan.Length == 0 ? fullSpan.Start : fullSpan.End - 1;
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
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
			else
				Selection.Clear();
		}

		public override void MoveCurrentLineToTop() =>
			HexView.DisplayHexLineContainingBufferPosition(Caret.Position.Position.ActivePosition.BufferPosition, 0, VSTE.ViewRelativePosition.Top);

		public override void MoveCurrentLineToBottom() =>
			HexView.DisplayHexLineContainingBufferPosition(Caret.Position.Position.ActivePosition.BufferPosition, 0, VSTE.ViewRelativePosition.Bottom);

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
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
			else
				Selection.Clear();
		}

		public override void MoveToBottomOfView(bool extendSelection) {
			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(GetBottomFullyVisibleLine());
			Caret.EnsureVisible();
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
			else
				Selection.Clear();
		}

		public override void SwapCaretAndAnchor() {
			Selection.Select(anchorPoint: Selection.ActivePoint, activePoint: Selection.AnchorPoint);
			Caret.MoveTo(Selection.ActivePoint);
			Caret.EnsureVisible();
		}

		public override bool InsertText(string text) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			return false;//TODO:
		}

		public override void SelectLine(HexViewLine viewLine, bool extendSelection) {
			if (viewLine == null)
				throw new ArgumentNullException(nameof(viewLine));

			HexBufferPoint anchorPoint, activePoint;
			var lineStart = viewLine.BufferSpan.Start;
			var lineEnd = viewLine.BufferSpan.End;

			if (Selection.IsEmpty || !extendSelection) {
				anchorPoint = lineStart;
				activePoint = lineEnd;
			}
			else {
				var anchorSpan = SelectionUtilities.GetLineAnchorSpan(Selection);
				if (anchorSpan.Start <= viewLine.BufferSpan.Start) {
					anchorPoint = anchorSpan.Start;
					activePoint = lineEnd;
				}
				else {
					anchorPoint = anchorSpan.End;
					activePoint = lineStart;
				}
			}
			Selection.Select(anchorPoint, activePoint);
			Caret.MoveTo(Filter(activePoint));
			Caret.EnsureVisible();
		}

		public override void SelectCurrentWord() {
			//TODO:
		}

		public override void SelectAll() =>
			SelectAndMove(HexView.BufferLines.BufferSpan);

		void SelectAndMove(HexBufferSpan span) {
			Selection.Select(span, false);
			Caret.MoveTo(Filter(span.End));
			Caret.EnsureVisible();
		}

		public override void ExtendSelection(HexBufferPoint newEnd) {
			if (newEnd.IsDefault)
				throw new ArgumentException();
			if (!HexView.BufferLines.IsValidPosition(newEnd))
				throw new ArgumentOutOfRangeException(nameof(newEnd));
			Selection.Select(Selection.AnchorPoint, newEnd);
			Caret.MoveTo(Selection.ActivePoint);
			var options = Selection.IsReversed ? VSTE.EnsureSpanVisibleOptions.ShowStart | VSTE.EnsureSpanVisibleOptions.MinimumScroll : VSTE.EnsureSpanVisibleOptions.ShowStart;
			var flags = Caret.Position.Position.ActiveColumn == HexColumnType.Values ? HexSpanSelectionFlags.Values : HexSpanSelectionFlags.Ascii;
			flags |= HexSpanSelectionFlags.Cell;
			ViewScroller.EnsureSpanVisible(Selection.StreamSelectionSpan, flags, options);
		}

		public override void MoveCaret(HexViewLine hexLine, double horizontalOffset, bool extendSelection) {
			if (hexLine == null)
				throw new ArgumentNullException(nameof(hexLine));

			var anchorPoint = GetAnchorPositionOrCaretIfNoSelection();
			Caret.MoveTo(hexLine, horizontalOffset);
			if (extendSelection)
				Selection.Select(anchorPoint, Caret.Position.Position.ActivePosition.BufferPosition);
			else
				Selection.Clear();
		}

		public override void ResetSelection() => Selection.Clear();

		public override bool CopySelection() {
			return false;//TODO:
		}

		public override bool Paste() {
			return false;//TODO:
		}

		public override void ScrollUpAndMoveCaretIfNecessary() => ScrollAndMoveCaretIfNecessary(VSTE.ScrollDirection.Up);
		public override void ScrollDownAndMoveCaretIfNecessary() => ScrollAndMoveCaretIfNecessary(VSTE.ScrollDirection.Down);
		void ScrollAndMoveCaretIfNecessary(VSTE.ScrollDirection scrollDirection) {
			var origCaretContainingTextViewLinePosition = Caret.ContainingHexViewLine.BufferSpan.Start;
			bool firstDocLineWasVisible = HexView.HexViewLines.FirstVisibleLine.IsFirstDocumentLine();
			ViewScroller.ScrollViewportVerticallyByLine(scrollDirection);

			var pos = Caret.Position.Position.ActivePosition.BufferPosition;
			var line = Caret.ContainingHexViewLine;
			var firstVisLine = HexView.HexViewLines.FirstVisibleLine;
			var lastVisLine = HexView.HexViewLines.LastVisibleLine;
			if (scrollDirection == VSTE.ScrollDirection.Up && firstDocLineWasVisible)
				lastVisLine = HexView.GetLastFullyVisibleLine();
			if (line.VisibilityState == VSTF.VisibilityState.Unattached)
				Caret.MoveTo(line.BufferSpan.Start <= firstVisLine.BufferSpan.Start ? firstVisLine : lastVisLine);
			else if (line.VisibilityState != VSTF.VisibilityState.FullyVisible) {
				if (scrollDirection == VSTE.ScrollDirection.Up) {
					var newLine = lastVisLine;
					if (newLine.BufferSpan.Start.Position == origCaretContainingTextViewLinePosition) {
						if (newLine.BufferSpan.Start.Position > HexView.BufferLines.BufferStart)
							newLine = HexView.HexViewLines.GetHexViewLineContainingBufferPosition(newLine.BufferSpan.Start - 1) ?? newLine;
					}
					Caret.MoveTo(newLine);
				}
				else {
					var newLine = firstVisLine;
					if (newLine.BufferSpan.Start.Position == origCaretContainingTextViewLinePosition && !newLine.IsLastDocumentLine())
						newLine = HexView.HexViewLines.GetHexViewLineContainingBufferPosition(newLine.BufferSpan.End) ?? newLine;
					Caret.MoveTo(newLine);
				}
			}
			Caret.EnsureVisible();

			var newPos = Caret.Position.Position.ActivePosition.BufferPosition;
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
			HexView.DisplayHexLineContainingBufferPosition(Caret.ContainingHexViewLine.BufferSpan.Start, 0, VSTE.ViewRelativePosition.Bottom);

		public override void ScrollLineTop() =>
			HexView.DisplayHexLineContainingBufferPosition(Caret.ContainingHexViewLine.BufferSpan.Start, 0, VSTE.ViewRelativePosition.Top);

		public override void ScrollLineCenter() {
			// line.Height depends on the line transform and it's set when the line is visible
			Caret.EnsureVisible();
			var line = Caret.ContainingHexViewLine;
			HexView.DisplayHexLineContainingBufferPosition(line.BufferSpan.Start, Math.Max(0, (HexView.ViewportHeight - line.Height) / 2), VSTE.ViewRelativePosition.Top);
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

		public override void ToggleColumn() => HexView.Caret.ToggleActiveColumn();
	}
}
