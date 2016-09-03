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
using System.Windows;
using System.Windows.Controls.Primitives;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Language.Intellisense {
	sealed class TextViewPopup {
		readonly IWpfTextView wpfTextView;
		readonly ITrackingPoint trackingPoint;
		readonly Popup popup;
		readonly IPopupContent popupContent;
		double oldZoomLevel;
		bool disposed;

		public TextViewPopup(ITextView textView, ITrackingPoint trackingPoint, IPopupContent popupContent) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (trackingPoint == null)
				throw new ArgumentNullException(nameof(trackingPoint));
			if (popupContent == null)
				throw new ArgumentNullException(nameof(popupContent));
			var wpfTextView = textView as IWpfTextView;
			if (wpfTextView == null)
				throw new ArgumentException($"{nameof(textView)} must be a {nameof(IWpfTextView)}", nameof(textView));
			this.wpfTextView = wpfTextView;
			this.trackingPoint = trackingPoint;
			this.popupContent = popupContent;
			this.popup = new Popup {
				Placement = PlacementMode.Relative,
				PlacementTarget = wpfTextView.VisualElement,
				AllowsTransparency = true,
			};
		}

		public void Show() {
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
			popup.Child = popupContent.UIElement;
			PositionPopup();
			popup.Visibility = Visibility.Visible;
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (disposed)
				return;
			if (MustRepositionPopup(e))
				PositionPopup();
		}

		bool MustRepositionPopup(TextViewLayoutChangedEventArgs e) {
			if (wpfTextView.ZoomLevel != oldZoomLevel)
				return true;
			if (e.OldViewState.ViewportLeft != e.NewViewState.ViewportLeft)
				return true;
			if (e.OldViewState.ViewportTop != e.NewViewState.ViewportTop)
				return true;
			if (e.OldViewState.ViewportWidth != e.NewViewState.ViewportWidth)
				return true;
			if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				return true;
			var t = GetTriggerLine();
			if (t != null && t.Item1.Change != TextViewLineChange.None)
				return true;
			return false;
		}

		Tuple<IWpfTextViewLine, SnapshotPoint> GetTriggerLine() {
			var point = trackingPoint.GetPoint(wpfTextView.TextSnapshot);
			var line = wpfTextView.TextViewLines.GetTextViewLineContainingBufferPosition(point);
			return line == null ? null : Tuple.Create(line, point);
		}

		void PositionPopup() {
			if (disposed)
				return;
			var newZoomLevel = wpfTextView.ZoomLevel;
			bool updateZoom = newZoomLevel != oldZoomLevel;
			if (updateZoom) {
				oldZoomLevel = newZoomLevel;
				// Make sure it can use the new scale transform
				popup.IsOpen = false;
			}

			var t = GetTriggerLine();
			if (t != null) {
				var extent = t.Item1.GetExtendedCharacterBounds(t.Item2);
				popup.HorizontalOffset = extent.Left - wpfTextView.ViewportLeft;
				popup.VerticalOffset = extent.Bottom - wpfTextView.ViewportTop;
			}

			if (updateZoom)
				ToolTipHelper.SetScaleTransform(wpfTextView, popup);

			popup.IsOpen = true;
		}

		public void Dispose() {
			if (disposed)
				return;
			disposed = true;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			popup.IsOpen = false;
			popup.Visibility = Visibility.Collapsed;
			popup.Child = null;
			popup.PlacementTarget = null;
		}
	}
}
