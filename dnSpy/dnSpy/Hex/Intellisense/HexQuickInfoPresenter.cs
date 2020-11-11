/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
		readonly WpfHexView? wpfHexView;

		public HexQuickInfoPresenter(HexQuickInfoSession session)
			: base(session) {
			wpfHexView = session.HexView as WpfHexView;
			Debug2.Assert(wpfHexView is not null);
			popup = new Popup {
				PlacementTarget = wpfHexView?.VisualElement,
				Placement = PlacementMode.Relative,
				Visibility = Visibility.Collapsed,
				IsOpen = false,
				AllowsTransparency = true,
				UseLayoutRounding = true,
				SnapsToDevicePixels = true,
			};

			// It's possible that quick info gets triggered inside the space between two values but
			// the full span doesn't include that space. In that case, dismiss the session.
			var mousePos = GetMousePoint(Mouse.PrimaryDevice);
			if (mousePos is null || !IsMouseWithinSpan(mousePos.Value))
				session.Dismiss();
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
			if (wpfHexView is null)
				return false;

			var point = session.TriggerPoint;
			Debug.Assert(!point.IsDefault);
			if (point.IsDefault)
				return false;

			var line = session.HexView.HexViewLines.GetHexViewLineContainingBufferPosition(point.BufferPosition);
			Debug2.Assert(line is not null && line.VisibilityState != VSTF.VisibilityState.Unattached);
			if (line is null || line.VisibilityState == VSTF.VisibilityState.Unattached)
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
			popup.AddHandler(UIElement.MouseLeaveEvent, new MouseEventHandler(Popup_MouseLeave), handledEventsToo: true);
			wpfHexView.Caret.PositionChanged += Caret_PositionChanged;
			wpfHexView.LayoutChanged += HexView_LayoutChanged;
			return true;
		}

		void Popup_MouseLeave(object? sender, MouseEventArgs e) => DismissIfNeeded(e);
		void VisualElement_MouseLeave(object? sender, MouseEventArgs e) => DismissIfNeeded(e);
		void HexView_LayoutChanged(object? sender, HexViewLayoutChangedEventArgs e) => session.Dismiss();
		void Caret_PositionChanged(object? sender, HexCaretPositionChangedEventArgs e) => session.Dismiss();
		void VisualElement_MouseMove(object? sender, MouseEventArgs e) => DismissIfNeeded(e);

		void DismissIfNeeded(MouseEventArgs e) {
			if (session.IsDismissed)
				return;
			if (ShouldDismiss(e))
				session.Dismiss();
		}

		bool ShouldDismiss(MouseEventArgs e) {
			Debug2.Assert(wpfHexView is not null);
			var mousePos = GetMousePoint(e.MouseDevice);
			if (mousePos is null)
				return true;
			if (IsMouseWithinSpan(mousePos.Value) && wpfHexView.VisualElement.IsMouseOver)
				return false;
			if (popup.IsMouseOver)
				return false;
			// Not over popup or applicable-to-span
			return true;
		}

		Point? GetMousePoint(MouseDevice device) {
			if (wpfHexView is null)
				return null;
			var mousePos = device.GetPosition(wpfHexView.VisualElement);
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
					if (bounds.Left <= mousePos.X && mousePos.X < bounds.Right && bounds.Top <= mousePos.Y && mousePos.Y < bounds.Bottom)
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
			if (wpfHexView is not null) {
				wpfHexView.VisualElement.MouseLeave -= VisualElement_MouseLeave;
				wpfHexView.VisualElement.MouseMove -= VisualElement_MouseMove;
				popup.RemoveHandler(UIElement.MouseLeaveEvent, new MouseEventHandler(Popup_MouseLeave));
				wpfHexView.Caret.PositionChanged -= Caret_PositionChanged;
				wpfHexView.LayoutChanged -= HexView_LayoutChanged;
			}
		}
	}
}
