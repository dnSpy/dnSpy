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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Operations;
using dnSpy.Controls;
using dnSpy.Hex.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using VSTE = Microsoft.VisualStudio.Text.Editor;

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

		public override void SelectAndMoveCaret(HexColumnType column, HexBufferPoint anchorPoint, HexBufferPoint activePoint, VSTE.EnsureSpanVisibleOptions? scrollOptions) {
			if (anchorPoint.IsDefault)
				throw new ArgumentException();
			if (activePoint.IsDefault)
				throw new ArgumentException();
			//TODO:
		}

		public override void MoveToNextCharacter(bool extendSelection) {
			//TODO:
		}

		public override void MoveToPreviousCharacter(bool extendSelection) {
			//TODO:
		}

		public override void MoveToNextWord(bool extendSelection) {
			//TODO:
		}

		public override void MoveToPreviousWord(bool extendSelection) {
			//TODO:
		}

		public override void MoveToNextCell(bool extendSelection) {
			//TODO:
		}

		public override void MoveToPreviousCell(bool extendSelection) {
			//TODO:
		}

		public override void MoveLineUp(bool extendSelection) {
			//TODO:
		}

		public override void MoveLineDown(bool extendSelection) {
			//TODO:
		}

		public override void PageUp(bool extendSelection) {
			//TODO:
		}

		public override void PageDown(bool extendSelection) {
			//TODO:
		}

		public override void MoveToEndOfLine(bool extendSelection) {
			//TODO:
		}

		public override void MoveToStartOfLine(bool extendSelection) {
			//TODO:
		}

		public override void MoveToStartOfDocument(bool extendSelection) {
			//TODO:
		}

		public override void MoveToEndOfDocument(bool extendSelection) {
			//TODO:
		}

		public override void MoveCurrentLineToTop() =>
			HexView.DisplayHexLineContainingBufferPosition(Caret.Position.Position.ActivePosition.BufferPosition, 0, VSTE.ViewRelativePosition.Top);

		public override void MoveCurrentLineToBottom() =>
			HexView.DisplayHexLineContainingBufferPosition(Caret.Position.Position.ActivePosition.BufferPosition, 0, VSTE.ViewRelativePosition.Bottom);

		public override void MoveToTopOfView(bool extendSelection) {
			//TODO:
		}

		public override void MoveToBottomOfView(bool extendSelection) {
			//TODO:
		}

		public override void SwapCaretAndAnchor() {
			//TODO:
		}

		public override bool InsertText(string text) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			return false;//TODO:
		}

		public override void SelectLine(HexViewLine viewLine, bool extendSelection) {
			if (viewLine == null)
				throw new ArgumentNullException(nameof(viewLine));
			//TODO:
		}

		public override void SelectCurrentWord() {
			//TODO:
		}

		public override void SelectAll() {
			//TODO:
		}

		public override void ExtendSelection(HexBufferPoint newEnd) {
			if (newEnd.IsDefault)
				throw new ArgumentException();
			//TODO:
		}

		public override void MoveCaret(HexViewLine hexLine, double horizontalOffset, bool extendSelection) {
			if (hexLine == null)
				throw new ArgumentNullException(nameof(hexLine));
			//TODO:
		}

		public override void ResetSelection() {
			//TODO:
		}

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
			if (line.VisibilityState == VisibilityState.Unattached)
				Caret.MoveTo(line.BufferSpan.Start <= firstVisLine.BufferSpan.Start ? firstVisLine : lastVisLine);
			else if (line.VisibilityState != VisibilityState.FullyVisible) {
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
