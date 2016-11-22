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
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Operations;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Editor {
	sealed class DefaultHexViewMouseProcessor : DefaultHexMouseProcessor {
		readonly WpfHexView wpfHexView;
		readonly HexEditorOperations editorOperations;

		const bool insertionPosition = false;
		const HexMoveToFlags hexMoveToFlags = HexMoveToFlags.None;

		public DefaultHexViewMouseProcessor(WpfHexView wpfHexView, HexEditorOperationsFactoryService editorOperationsFactoryService) {
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			this.wpfHexView = wpfHexView;
			this.editorOperations = editorOperationsFactoryService.GetEditorOperations(wpfHexView);
		}

		HexMouseLocation GetLocation(MouseEventArgs e) => HexMouseLocation.Create(wpfHexView, e, insertionPosition: insertionPosition);

		bool IsInSelection(HexMouseLocation mouseLoc) {
			if (wpfHexView.Selection.IsEmpty)
				return false;
			var info = mouseLoc.HexViewLine.BufferLine.GetLinePositionInfo(mouseLoc.Position);
			var position = mouseLoc.HexViewLine.BufferLine.GetClosestCellPosition(info, true);
			if (position == null)
				return false;
			var point = position.Value.BufferPosition;
			foreach (var span in wpfHexView.Selection.SelectedSpans) {
				if (span.Contains(point))
					return true;
			}
			return false;
		}

		public override void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			e.Handled = true;
			var mouseLoc = GetLocation(e);
			wpfHexView.Caret.MoveTo(mouseLoc.HexViewLine, mouseLoc.Point.X, HexMoveToFlags.CaptureHorizontalPosition);
			if (!IsInSelection(mouseLoc))
				wpfHexView.Selection.Clear();
			wpfHexView.Caret.EnsureVisible();
		}

		void SelectToMousePosition(MouseEventArgs e, bool extendSelection) =>
			SelectToMousePosition(GetLocation(e), extendSelection);
		void SelectToMousePosition(HexMouseLocation mouseLoc, bool extendSelection) =>
			editorOperations.MoveCaret(mouseLoc.HexViewLine, mouseLoc.Point.X, extendSelection, hexMoveToFlags);

		public override void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			e.Handled = true;
			var mouseLoc = GetLocation(e);
			int clickCount = e.ClickCount;
			if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Control)
				clickCount = 2;
			switch (clickCount) {
			default:
			case 1:
				bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
				SelectToMousePosition(mouseLoc, isShift);
				break;

			case 2:
				editorOperations.MoveCaret(mouseLoc.HexViewLine, mouseLoc.Point.X, false, hexMoveToFlags);
				editorOperations.SelectCurrentWord();
				break;

			case 3:
				editorOperations.SelectLine(mouseLoc.HexViewLine, false);
				// Seems to match VS behavior
				var end = mouseLoc.HexViewLine.TextRight;
				end += wpfHexView.FormattedLineSource.ColumnWidth;
				if (mouseLoc.Point.X < end)
					wpfHexView.Caret.MoveTo(mouseLoc.HexViewLine.BufferStart);
				break;
			}
			wpfHexView.Caret.EnsureVisible();
			mouseLeftDownInfo = new MouseLeftDownInfo(GetSelectionOrCaretIfNoSelection(), mouseLoc.Point, clickCount, wpfHexView.BufferLines);
		}
		MouseLeftDownInfo? mouseLeftDownInfo;

		struct MouseLeftDownInfo {
			public HexBufferSpan Span { get; }
			public Point Point { get; }
			public int Clicks { get; }
			HexBufferLineProvider BufferLines { get; set; }
			public MouseLeftDownInfo(HexBufferSpan span, Point point, int clicks, HexBufferLineProvider bufferLines) {
				Span = span;
				Point = point;
				Clicks = clicks;
				BufferLines = bufferLines;
			}

			public bool TryUpdateBufferLines(HexBufferLineProvider bufferLines) => BufferLines == bufferLines;
		}

		public override void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			bool oldMouseCaptured = mouseCaptured;
			CancelMouseLeftButtonSelection();
			if (oldMouseCaptured) {
				// We're always called, so don't mark it as handled
				// e.Handled = true;
				return;
			}
		}

		void CancelMouseLeftButtonSelection() {
			mouseLeftDownInfo = null;
			if (mouseCaptured) {
				StopScrolling();
				wpfHexView.VisualElement.ReleaseMouseCapture();
				mouseCaptured = false;
				return;
			}
		}

		HexBufferSpan GetSelectionOrCaretIfNoSelection() {
			HexBufferPoint start, end;
			GetSelectionOrCaretIfNoSelection(out start, out end);
			return new HexBufferSpan(start, end);
		}

		void GetSelectionOrCaretIfNoSelection(out HexBufferPoint start, out HexBufferPoint end) {
			if (!wpfHexView.Selection.IsEmpty) {
				start = wpfHexView.Selection.Start;
				end = wpfHexView.Selection.End;
			}
			else {
				start = wpfHexView.Caret.Position.Position.ActivePosition.BufferPosition;
				end = start;
			}
		}

		public override void OnMouseMove(object sender, MouseEventArgs e) {
			if (e.LeftButton == MouseButtonState.Pressed) {
				if (mouseLeftDownInfo != null && !mouseLeftDownInfo.Value.TryUpdateBufferLines(wpfHexView.BufferLines)) {
					CancelMouseLeftButtonSelection();
					return;
				}
				if (!mouseCaptured && mouseLeftDownInfo != null) {
					var mouseLoc = GetLocation(e);
					var dist = mouseLeftDownInfo.Value.Point - mouseLoc.Point;
					bool movedEnough = Math.Abs(dist.X) >= SystemParameters.MinimumHorizontalDragDistance ||
									   Math.Abs(dist.Y) >= SystemParameters.MinimumVerticalDragDistance;
					if (movedEnough && wpfHexView.VisualElement.CaptureMouse()) {
						mouseCaptured = true;
						e.Handled = true;
						return;
					}
				}
				else if (mouseCaptured) {
					e.Handled = true;
					Debug.Assert(mouseLeftDownInfo != null);
					if (mouseLeftDownInfo == null)
						StopScrolling();
					else if (mouseLeftDownInfo.Value.Clicks == 2 || mouseLeftDownInfo.Value.Clicks == 3) {
						Debug.Assert(dispatcherTimer == null);
						StopScrolling();

						var mouseLoc = GetLocation(e);
						// Same behavior as in VS: don't scroll if it's word or line selection
						if (!mouseLoc.HexViewLine.IsVisible())
							return;
						wpfHexView.Caret.MoveTo(mouseLoc.HexViewLine, mouseLoc.Point.X, hexMoveToFlags);

						if (mouseLeftDownInfo.Value.Clicks == 2)
							editorOperations.SelectCurrentWord();
						else
							editorOperations.SelectLine(wpfHexView.Caret.ContainingHexViewLine, false);
						HexBufferPoint selStart, selEnd;
						GetSelectionOrCaretIfNoSelection(out selStart, out selEnd);

						HexBufferPoint anchorPoint, activePoint;
						if (selStart < mouseLeftDownInfo.Value.Span.Start) {
							activePoint = selStart;
							anchorPoint = mouseLeftDownInfo.Value.Span.End;
						}
						else {
							activePoint = selEnd;
							anchorPoint = mouseLeftDownInfo.Value.Span.Start;
						}
						wpfHexView.Selection.Select(anchorPoint, activePoint);
						wpfHexView.Caret.MoveTo(activePoint);
						wpfHexView.Caret.EnsureVisible();
					}
					else {
						SelectToMousePosition(e, true);
						UpdateScrolling(e);
					}
					return;
				}
			}
		}
		bool mouseCaptured;
		DispatcherTimer dispatcherTimer;

		void StopScrolling() {
			dispatcherTimer?.Stop();
			dispatcherTimer = null;
		}

		void UpdateScrolling(MouseEventArgs e) {
			var mouseLoc = GetLocation(e);
			TimeSpan interval;
			var scrollDir = GetScrollDirection(mouseLoc, out interval);
			if (scrollDir == null) {
				StopScrolling();
				wpfHexView.Caret.EnsureVisible();
				return;
			}

			if (dispatcherTimer != null) {
				// It resets the timer if we write a new value, even if it's identical to the original value
				if (dispatcherTimer.Interval != interval)
					dispatcherTimer.Interval = interval;
			}
			else {
				dispatcherTimer = new DispatcherTimer(interval, DispatcherPriority.Normal, (s, e2) => OnScroll(scrollDir.Value, mouseLoc.Point.X), wpfHexView.VisualElement.Dispatcher);
				OnScroll(scrollDir.Value, mouseLoc.Point.X);
			}
		}

		ScrollDirection? GetScrollDirection(HexMouseLocation mouseLoc, out TimeSpan interval) {
			// Give prio to scrolling up/down (more common than scrolling left/right)
			if (mouseLoc.Point.Y < wpfHexView.ViewportTop) {
				interval = GetVerticalInterval(mouseLoc.Point.Y - wpfHexView.ViewportTop);
				return ScrollDirection.Up;
			}
			if (mouseLoc.Point.Y >= wpfHexView.ViewportBottom) {
				interval = GetVerticalInterval(mouseLoc.Point.Y - wpfHexView.ViewportBottom);
				return ScrollDirection.Down;
			}
			if (mouseLoc.Point.X < wpfHexView.ViewportLeft) {
				interval = GetHorizontalInterval(mouseLoc.Point.X - wpfHexView.ViewportLeft);
				return ScrollDirection.Left;
			}
			if (mouseLoc.Point.X >= wpfHexView.ViewportRight) {
				interval = GetHorizontalInterval(mouseLoc.Point.X - wpfHexView.ViewportRight);
				return ScrollDirection.Right;
			}
			interval = TimeSpan.Zero;
			return null;
		}

		TimeSpan GetVerticalInterval(double dist) => GetInterval(dist, wpfHexView.LineHeight);
		TimeSpan GetHorizontalInterval(double dist) => GetInterval(dist, DefaultCharacterWidth);
		TimeSpan GetInterval(double dist, double length) {
			const double SCROLL_INTERVAL_MS = 250;
			if (Math.Abs(dist) < 2 * length)
				return TimeSpan.FromMilliseconds(SCROLL_INTERVAL_MS);
			return TimeSpan.FromMilliseconds(SCROLL_INTERVAL_MS / 16);
		}

		double DefaultCharacterWidth => wpfHexView.FormattedLineSource.ColumnWidth;

		enum ScrollDirection {
			Left,
			Right,
			Up,
			Down,
		}

		void OnScroll(ScrollDirection value, double xCoordinate) {
			HexViewLine line;
			HexBufferPoint lineStart;
			switch (value) {
			case ScrollDirection.Left:
				line = wpfHexView.Caret.ContainingHexViewLine;
				if (line.TextLeft >= wpfHexView.ViewportLeft)
					StopScrolling();
				else if (wpfHexView.Caret.Position.Position.ActivePosition.BufferPosition != line.BufferStart)
					editorOperations.MoveToPreviousCharacter(true);
				else {
					wpfHexView.ViewportLeft = line.TextLeft;
					StopScrolling();
				}
				break;

			case ScrollDirection.Right:
				line = wpfHexView.Caret.ContainingHexViewLine;
				if (line.TextRight <= wpfHexView.ViewportRight)
					StopScrolling();
				else if (wpfHexView.Caret.Position.Position.ActivePosition.BufferPosition < line.BufferEnd)
					editorOperations.MoveToNextCharacter(true);
				else {
					wpfHexView.ViewportLeft = Math.Max(0, line.TextRight - wpfHexView.ViewportWidth);
					StopScrolling();
				}
				break;

			case ScrollDirection.Up:
				line = wpfHexView.HexViewLines.FirstVisibleLine;
				if (line.VisibilityState == VSTF.VisibilityState.FullyVisible && !line.IsFirstDocumentLine())
					line = wpfHexView.GetHexViewLineContainingBufferPosition(line.BufferStart - 1);
				lineStart = line.BufferStart;
				if (line.VisibilityState != VSTF.VisibilityState.FullyVisible)
					wpfHexView.DisplayHexLineContainingBufferPosition(line.BufferStart, 0, VSTE.ViewRelativePosition.Top);
				if (!line.IsValid)
					line = wpfHexView.GetHexViewLineContainingBufferPosition(lineStart);
				if (line.IsFirstDocumentLine())
					StopScrolling();
				editorOperations.MoveCaret(line, xCoordinate, true, hexMoveToFlags);
				break;

			case ScrollDirection.Down:
				line = wpfHexView.HexViewLines.LastVisibleLine;
				if (line.VisibilityState == VSTF.VisibilityState.FullyVisible && !line.IsLastDocumentLine())
					line = wpfHexView.GetHexViewLineContainingBufferPosition(line.BufferEnd);
				lineStart = line.BufferStart;
				if (line.VisibilityState != VSTF.VisibilityState.FullyVisible)
					wpfHexView.DisplayHexLineContainingBufferPosition(line.BufferStart, 0, VSTE.ViewRelativePosition.Bottom);
				if (!line.IsValid)
					line = wpfHexView.GetHexViewLineContainingBufferPosition(lineStart);
				if (line.IsLastDocumentLine())
					StopScrolling();
				editorOperations.MoveCaret(line, xCoordinate, true, hexMoveToFlags);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(value));
			}

			wpfHexView.Caret.EnsureVisible();
		}
	}
}
