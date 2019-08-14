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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using dnSpy.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class PopupSpaceReservationAgent : ISpaceReservationAgent {
		bool IsVisible => !(popup.Child is null);
		public bool HasFocus => IsVisible && popup.IsKeyboardFocusWithin;
		public bool IsMouseOver => IsVisible && popup.IsMouseOver;
		public event EventHandler? GotFocus;
		public event EventHandler? LostFocus;

		readonly ISpaceReservationManager spaceReservationManager;
		readonly IWpfTextView wpfTextView;
		readonly UIElement content;
		readonly Popup popup;
		PopupStyles style;
		ITrackingSpan visualSpan;
		double popupZoomLevel = double.NaN;

		public PopupSpaceReservationAgent(ISpaceReservationManager spaceReservationManager, IWpfTextView wpfTextView, ITrackingSpan visualSpan, PopupStyles style, UIElement content) {
			if ((style & (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent)) == (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent))
				throw new ArgumentOutOfRangeException(nameof(style));
			this.spaceReservationManager = spaceReservationManager ?? throw new ArgumentNullException(nameof(spaceReservationManager));
			this.wpfTextView = wpfTextView;
			this.visualSpan = visualSpan ?? throw new ArgumentNullException(nameof(visualSpan));
			this.style = style;
			this.content = content ?? throw new ArgumentNullException(nameof(content));
			popup = new Popup {
				PlacementTarget = wpfTextView.VisualElement,
				Placement = PlacementMode.Relative,
				Visibility = Visibility.Collapsed,
				IsOpen = false,
				AllowsTransparency = true,
				UseLayoutRounding = true,
				SnapsToDevicePixels = true,
			};
		}

		void Content_GotFocus(object? sender, RoutedEventArgs e) => GotFocus?.Invoke(this, EventArgs.Empty);
		void Content_LostFocus(object? sender, RoutedEventArgs e) => LostFocus?.Invoke(this, EventArgs.Empty);

		internal void Update(ITrackingSpan visualSpan, PopupStyles style) {
			if ((style & (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent)) == (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent))
				throw new ArgumentOutOfRangeException(nameof(style));
			this.visualSpan = visualSpan ?? throw new ArgumentNullException(nameof(visualSpan));
			this.style = style;
			wpfTextView.QueueSpaceReservationStackRefresh();
		}

		Rect? GetVisualSpanBounds() {
			var span = visualSpan.GetSpan(visualSpan.TextBuffer.CurrentSnapshot);
			var lines = wpfTextView.TextViewLines.GetTextViewLinesIntersectingSpan(span);
			if (lines.Count == 0)
				return null;
			double left = double.PositiveInfinity, right = double.NegativeInfinity;
			double top = double.PositiveInfinity, bottom = double.NegativeInfinity;
			foreach (var line in lines) {
				if (!line.IsVisible())
					continue;
				foreach (var textBounds in GetTextBounds(line, span)) {
					left = Math.Min(left, textBounds.Left);
					right = Math.Max(right, textBounds.Right);
					top = Math.Min(top, textBounds.TextTop);
					bottom = Math.Max(bottom, textBounds.TextBottom);
				}
			}
			bool b = left != double.PositiveInfinity && right != double.NegativeInfinity &&
				top != double.PositiveInfinity && bottom != double.NegativeInfinity;
			if (!b)
				return null;
			if (left > right)
				right = left;
			if (top > bottom)
				bottom = top;
			return new Rect(left, top, right - left, bottom - top);
		}

		IList<TextBounds> GetTextBounds(ITextViewLine line, SnapshotSpan fullSpan) {
			if (fullSpan.Length == 0) {
				if (line.ExtentIncludingLineBreak.Contains(fullSpan)) {
					var bounds = line.GetCharacterBounds(fullSpan.Start);
					// It's just a point, so use zero width
					bounds = new TextBounds(bounds.Leading, bounds.Top, 0, bounds.Height, bounds.TextTop, bounds.TextHeight);
					return new TextBounds[] { bounds };
				}
				return Array.Empty<TextBounds>();
			}
			else
				return line.GetNormalizedTextBounds(fullSpan);
		}

		Size PopupSize {
			get {
				var maxSize = PopupHelper.GetMaxSize(wpfTextView);
				content.Measure(maxSize);
				return new Size(Math.Min(content.DesiredSize.Width, maxSize.Width), Math.Min(content.DesiredSize.Height, maxSize.Height));
			}
		}

		Size ToScreenSize(Size size) => PopupHelper.TransformToDevice(wpfTextView, size);

		Rect WpfTextViewRectToScreenRect(Rect wpfTextViewRect) {
			wpfTextViewRect.X -= wpfTextView.ViewportLeft;
			wpfTextViewRect.Y -= wpfTextView.ViewportTop;
			return ToScreenRect(wpfTextViewRect);
		}

		Rect ToScreenRect(Rect wpfRect) => new Rect(ToScreenPoint(wpfRect.TopLeft), ToScreenPoint(wpfRect.BottomRight));
		Point ToScreenPoint(Point point) => wpfTextView.VisualElement.PointToScreen(point);

		public Geometry? PositionAndDisplay(Geometry reservedSpace) {
			var spanBoundsTmp = GetVisualSpanBounds();
			if (spanBoundsTmp is null || spanBoundsTmp.Value.IsEmpty)
				return null;
			var spanBounds = WpfTextViewRectToScreenRect(spanBoundsTmp.Value);
			var desiredSize = ToScreenSize(PopupSize);

			var screen = new Screen(wpfTextView.VisualElement);
			var screenRect = screen.IsValid ? screen.DisplayRect : SystemParameters.WorkArea;

			Rect? popupRect = null;
			if ((style & PopupStyles.PositionClosest) != 0) {
				foreach (var pos in GetValidPositions(screenRect, reservedSpace, desiredSize, spanBounds, style))
					popupRect = GetClosest(spanBounds, popupRect, pos, style);
			}
			else {
				foreach (var pos in GetValidPositions(screenRect, reservedSpace, desiredSize, spanBounds, style)) {
					popupRect = pos;
					break;
				}
			}

			if (popupRect is null)
				return null;
			var viewRelativeRect = PopupHelper.TransformFromDevice(wpfTextView, popupRect.Value);

			bool isOpen = popup.IsOpen;
			if (!isOpen)
				AddEvents();

			if (popupZoomLevel != wpfTextView.ZoomLevel) {
				// Must set IsOpen to false when setting a new scale transform
				popup.IsOpen = false;
				PopupHelper.SetScaleTransform(wpfTextView, popup);
				popupZoomLevel = wpfTextView.ZoomLevel;
			}
			if (!isOpen) {
				popup.Child = content;
				popup.Visibility = Visibility.Visible;
			}
			popup.VerticalOffset = viewRelativeRect.Top;
			popup.HorizontalOffset = viewRelativeRect.Left;
			popup.IsOpen = true;

			return new RectangleGeometry(popupRect.Value);
		}

		Rect GetClosest(Rect spanBounds, Rect? rect, Rect candidate, PopupStyles style) {
			if (rect is null)
				return candidate;
			double rectDist, candidateDist;
			if ((style & PopupStyles.PositionLeftOrRight) != 0) {
				rectDist = GetHorizontalDistance(spanBounds, rect.Value);
				candidateDist = GetHorizontalDistance(spanBounds, candidate);
			}
			else {
				rectDist = GetVerticalDistance(spanBounds, rect.Value);
				candidateDist = GetVerticalDistance(spanBounds, candidate);
			}
			return rectDist <= candidateDist ? rect.Value : candidate;
		}

		double GetHorizontalDistance(Rect spanBounds, Rect rect) {
			double a = Math.Min(Math.Abs(rect.Left - spanBounds.Left), Math.Abs(rect.Right - spanBounds.Left));
			double b = Math.Min(Math.Abs(rect.Left - spanBounds.Right), Math.Abs(rect.Right - spanBounds.Right));
			return Math.Min(a, b);
		}

		double GetVerticalDistance(Rect spanBounds, Rect rect) {
			double a = Math.Min(Math.Abs(rect.Top - spanBounds.Top), Math.Abs(rect.Bottom - spanBounds.Top));
			double b = Math.Min(Math.Abs(rect.Top - spanBounds.Bottom), Math.Abs(rect.Bottom - spanBounds.Bottom));
			return Math.Min(a, b);
		}

		bool OverlapsReservedSpace(Geometry reservedSpace, Rect rect) {
			var rs = reservedSpace.Bounds;
			return rs.Left < rect.Right && rs.Right > rect.Left && rs.Top < rect.Bottom && rs.Bottom > rect.Top;
		}

		IEnumerable<Rect> GetValidPositions(Rect screenRect, Geometry reservedSpace, Size desiredSize, Rect spanBounds, PopupStyles style) {
			var possiblePositions = new List<Rect>();
			possiblePositions.AddRange(GetPositions(screenRect, reservedSpace, desiredSize, spanBounds, style));
			possiblePositions.AddRange(GetPositions(screenRect, reservedSpace, desiredSize, spanBounds, style ^ PopupStyles.PreferLeftOrTopPosition));
			bool isLeftRight = (style & PopupStyles.PositionLeftOrRight) != 0;
			foreach (var pos in possiblePositions) {
				if (isLeftRight) {
					if (pos.Left >= screenRect.Left && pos.Right <= screenRect.Right)
						yield return pos;
				}
				else {
					if (pos.Top >= screenRect.Top && pos.Bottom <= screenRect.Bottom)
						yield return pos;
				}
			}
		}

		IEnumerable<Rect> GetPositions(Rect screenRect, Geometry reservedSpace, Size desiredSize, Rect spanBounds, PopupStyles style) {
			var rect1 = GetPosition(screenRect, desiredSize, spanBounds, spanBounds, style);
			if (!OverlapsReservedSpace(reservedSpace, rect1))
				yield return rect1;

			var unionBounds = Rect.Union(reservedSpace.Bounds, spanBounds);
			if ((style & PopupStyles.PositionLeftOrRight) != 0) {
				bool preferLeft = (style & PopupStyles.PreferLeftOrTopPosition) != 0;
				var rect2 = preferLeft ?
						new Rect(unionBounds.Left - desiredSize.Width, spanBounds.Y, desiredSize.Width, desiredSize.Height) :
						new Rect(unionBounds.Right, spanBounds.Y, desiredSize.Width, desiredSize.Height);
				if (!OverlapsReservedSpace(reservedSpace, rect2))
					yield return rect2;
			}
			else {
				bool preferTop = (style & PopupStyles.PreferLeftOrTopPosition) != 0;
				var rect2 = preferTop ?
						new Rect(spanBounds.X, unionBounds.Top - desiredSize.Height, desiredSize.Width, desiredSize.Height) :
						new Rect(spanBounds.X, unionBounds.Bottom, desiredSize.Width, desiredSize.Height);
				if (!OverlapsReservedSpace(reservedSpace, rect2))
					yield return rect2;
			}

			var rect3 = GetPosition(screenRect, desiredSize, spanBounds, unionBounds, style);
			if (!OverlapsReservedSpace(reservedSpace, rect3))
				yield return rect3;
		}

		Rect GetPosition(Rect screenRect, Size desiredSize, Rect spanBounds, Rect reservedBounds, PopupStyles style) {
			Debug.Assert(Rect.Union(reservedBounds, spanBounds) == reservedBounds);
			double left, top;
			if ((style & PopupStyles.PositionLeftOrRight) != 0) {
				bool preferLeft = (style & PopupStyles.PreferLeftOrTopPosition) != 0;
				bool bottomJustify = (style & PopupStyles.RightOrBottomJustify) != 0;
				top = bottomJustify ? spanBounds.Bottom - desiredSize.Height : reservedBounds.Top;
				left = preferLeft ? reservedBounds.Left - desiredSize.Width : reservedBounds.Right;
				if (top < screenRect.Top)
					top = screenRect.Top;
				else if (top + desiredSize.Height > screenRect.Bottom)
					top = screenRect.Bottom - desiredSize.Height;
			}
			else {
				bool preferTop = (style & PopupStyles.PreferLeftOrTopPosition) != 0;
				bool rightJustify = (style & PopupStyles.RightOrBottomJustify) != 0;
				top = preferTop ? reservedBounds.Top - desiredSize.Height : reservedBounds.Bottom;
				left = rightJustify ? spanBounds.Right - desiredSize.Width : reservedBounds.Left;
				if (left < screenRect.Left)
					left = screenRect.Left;
				else if (left + desiredSize.Width > screenRect.Right)
					left = screenRect.Right - desiredSize.Width;
			}
			return new Rect(left, top, desiredSize.Width, desiredSize.Height);
		}

		bool IsMouseOverSpan(MouseEventArgs e) {
			var rect = GetVisualSpanBounds();
			if (rect is null)
				return false;
			var point = e.MouseDevice.GetPosition(wpfTextView.VisualElement);
			point.X += wpfTextView.ViewportLeft;
			point.Y += wpfTextView.ViewportTop;
			return rect.Value.Contains(point);
		}

		bool IsMouseOverValidLocation(MouseEventArgs e) {
			if (IsMouseOverSpan(e))
				return true;
			if ((style & PopupStyles.DismissOnMouseLeaveTextOrContent) != 0)
				return content.IsMouseOver;
			return false;
		}

		void Content_MouseLeave(object? sender, MouseEventArgs e) {
			if (!popup.IsOpen)
				return;
			if (!IsMouseOverValidLocation(e))
				spaceReservationManager.RemoveAgent(this);
		}

		void VisualElement_PreviewMouseMove(object? sender, MouseEventArgs e) {
			if (!popup.IsOpen)
				return;
			if (!IsMouseOverValidLocation(e))
				spaceReservationManager.RemoveAgent(this);
		}

		void WpfTextView_LostAggregateFocus(object? sender, EventArgs e) => spaceReservationManager.RemoveAgent(this);
		void Window_LocationChanged(object? sender, EventArgs e) => spaceReservationManager.RemoveAgent(this);
		void Content_SizeChanged(object? sender, SizeChangedEventArgs e) => wpfTextView.QueueSpaceReservationStackRefresh();

		void AddEvents() {
			wpfTextView.LostAggregateFocus += WpfTextView_LostAggregateFocus;
			if (content is FrameworkElement fwElem)
				fwElem.SizeChanged += Content_SizeChanged;
			var window = Window.GetWindow(wpfTextView.VisualElement);
			if (!(window is null))
				window.LocationChanged += Window_LocationChanged;
			content.GotFocus += Content_GotFocus;
			content.LostFocus += Content_LostFocus;
			if ((style & (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent)) != 0) {
				wpfTextView.VisualElement.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(VisualElement_PreviewMouseMove), true);
				if ((style & PopupStyles.DismissOnMouseLeaveTextOrContent) != 0)
					content.AddHandler(UIElement.MouseLeaveEvent, new MouseEventHandler(Content_MouseLeave), true);
			}
		}

		void RemoveEvents() {
			wpfTextView.LostAggregateFocus -= WpfTextView_LostAggregateFocus;
			if (content is FrameworkElement fwElem)
				fwElem.SizeChanged -= Content_SizeChanged;
			var window = Window.GetWindow(wpfTextView.VisualElement);
			if (!(window is null))
				window.LocationChanged -= Window_LocationChanged;
			content.GotFocus -= Content_GotFocus;
			content.LostFocus -= Content_LostFocus;
			wpfTextView.VisualElement.PreviewMouseMove -= VisualElement_PreviewMouseMove;
			content.MouseLeave -= Content_MouseLeave;
		}

		public void Hide() {
			popup.Child = null;
			popup.Visibility = Visibility.Collapsed;
			popup.IsOpen = false;
			RemoveEvents();
		}
	}
}
