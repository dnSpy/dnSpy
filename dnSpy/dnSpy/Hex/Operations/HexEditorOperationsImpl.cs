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

		public override void MoveCurrentLineToTop() {
			//TODO:
		}

		public override void MoveCurrentLineToBottom() {
			//TODO:
		}

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

		public override void ScrollUpAndMoveCaretIfNecessary() {
			//TODO:
		}

		public override void ScrollDownAndMoveCaretIfNecessary() {
			//TODO:
		}

		public override void ScrollPageUp() {
			//TODO:
		}

		public override void ScrollPageDown() {
			//TODO:
		}

		public override void ScrollColumnLeft() {
			//TODO:
		}

		public override void ScrollColumnRight() {
			//TODO:
		}

		public override void ScrollLineBottom() {
			//TODO:
		}

		public override void ScrollLineTop() {
			//TODO:
		}

		public override void ScrollLineCenter() {
			//TODO:
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
			//TODO:
		}
	}
}
