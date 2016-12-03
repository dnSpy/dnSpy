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

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Intellisense;
using dnSpy.Hex.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Intellisense {
	sealed class HexQuickInfoPresenter : HexQuickInfoPresenterBase, IHexCustomIntellisensePresenter {
		readonly Popup popup;
		readonly WpfHexView wpfHexView;

		public HexQuickInfoPresenter(HexQuickInfoSession session)
			: base(session) {
			wpfHexView = session.HexView as WpfHexView;
			Debug.Assert(wpfHexView != null);
			popup = new Popup {
				PlacementTarget = wpfHexView?.VisualElement,
				Placement = PlacementMode.Relative,
				Visibility = Visibility.Collapsed,
				IsOpen = false,
				AllowsTransparency = true,
				UseLayoutRounding = true,
				SnapsToDevicePixels = true,
			};
		}

		bool renderCalled;
		public void Render() {
			Debug.Assert(!renderCalled);
			if (renderCalled)
				return;
			renderCalled = true;
			if (!RenderCore())
				session.Dismiss();
		}

		bool RenderCore() {
			if (session.IsDismissed || session.HexView.IsClosed)
				return false;
			if (wpfHexView == null)
				return false;

			var point = session.TriggerPoint;
			Debug.Assert(!point.IsDefault);
			if (point.IsDefault)
				return false;

			var line = session.HexView.HexViewLines.GetHexViewLineContainingBufferPosition(point.BufferPosition);
			Debug.Assert(line != null && line.VisibilityState != VSTF.VisibilityState.Unattached);
			if (line == null || line.VisibilityState == VSTF.VisibilityState.Unattached)
				return false;

			var linePosition = line.BufferLine.GetLinePosition(point) ?? 0;
			var bounds = line.GetExtendedCharacterBounds(linePosition);
			popup.HorizontalOffset = bounds.Left - wpfHexView.ViewportLeft;
			popup.VerticalOffset = bounds.TextBottom - session.HexView.ViewportTop;

			HexPopupHelper.SetScaleTransform(wpfHexView, popup);
			popup.Child = control;
			popup.Visibility = Visibility.Visible;
			popup.IsOpen = true;

			wpfHexView.VisualElement.MouseLeave += VisualElement_MouseLeave;
			wpfHexView.VisualElement.MouseMove += VisualElement_MouseMove;
			wpfHexView.Caret.PositionChanged += Caret_PositionChanged;
			wpfHexView.LayoutChanged += TextView_LayoutChanged;
			return true;
		}

		void VisualElement_MouseLeave(object sender, MouseEventArgs e) => session.Dismiss();
		void TextView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) => session.Dismiss();
		void Caret_PositionChanged(object sender, HexCaretPositionChangedEventArgs e) => session.Dismiss();

		void VisualElement_MouseMove(object sender, MouseEventArgs e) {
			if (session.IsDismissed)
				return;
			var mousePos = GetMousePoint(e.MouseDevice);
			if (mousePos == null || !IsMouseWithinSpan(mousePos.Value))
				session.Dismiss();
		}

		Point? GetMousePoint(MouseDevice device) {
			if (wpfHexView == null)
				return null;
			var mousePos = Mouse.PrimaryDevice.GetPosition(wpfHexView.VisualElement);
			mousePos.X += wpfHexView.ViewportLeft;
			mousePos.Y += wpfHexView.ViewportTop;
			return mousePos;
		}

		bool IsMouseWithinSpan(Point mousePos) {
			var applicableToSpan = session.ApplicableToSpan;
			if (applicableToSpan.IsDefault)
				return false;
			var lines = session.HexView.HexViewLines.GetHexViewLinesIntersectingSpan(applicableToSpan.BufferSpan);
			foreach (var line in lines) {
				foreach (var bounds in line.GetNormalizedTextBounds(applicableToSpan)) {
					if (bounds.Left <= mousePos.X && mousePos.X < bounds.Right && bounds.TextTop <= mousePos.Y && mousePos.Y < bounds.TextBottom)
						return true;
				}
			}
			return false;
		}

		void ClosePopup() {
			popup.IsOpen = false;
			popup.Visibility = Visibility.Collapsed;
			popup.Child = null;
		}

		protected override void OnSessionDismissed() {
			ClosePopup();
			if (wpfHexView != null) {
				wpfHexView.VisualElement.MouseLeave -= VisualElement_MouseLeave;
				wpfHexView.VisualElement.MouseMove -= VisualElement_MouseMove;
				wpfHexView.Caret.PositionChanged -= Caret_PositionChanged;
				wpfHexView.LayoutChanged -= TextView_LayoutChanged;
			}
		}
	}
}
