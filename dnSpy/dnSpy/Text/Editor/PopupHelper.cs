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

using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Controls;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	static class PopupHelper {
		const double maxHeightMultiplier = 0.8;
		const double maxWidthMultiplier = 0.8;

		public static Size GetMaxSize(IWpfTextView wpfTextView) {
			var screen = new Screen(wpfTextView.VisualElement);
			var screenRect = screen.IsValid ? screen.DisplayRect : SystemParameters.WorkArea;
			var size = TransformFromDevice(wpfTextView, screenRect.Size);
			return new Size(size.Width * maxWidthMultiplier, size.Height * maxHeightMultiplier);
		}

		public static void SetScaleTransform(IWpfTextView wpfTextView, FrameworkElement popupElement) {
			if (wpfTextView == null)
				return;
			var metroWindow = Window.GetWindow(wpfTextView.VisualElement) as MetroWindow;
			if (metroWindow == null)
				return;
			metroWindow.SetScaleTransform(popupElement, wpfTextView.ZoomLevel / 100);

			var maxSize = GetMaxSize(wpfTextView);
			popupElement.MaxWidth = maxSize.Width;
			popupElement.MaxHeight = maxSize.Height;
		}

		public static Size TransformFromDevice(IWpfTextView wpfTextView, Size size) {
			var zoomMultiplier = wpfTextView.ZoomLevel == 0 ? 1 : 100 / wpfTextView.ZoomLevel;
			var source = PresentationSource.FromVisual(wpfTextView.VisualElement);
			var transformFromDevice = source?.CompositionTarget.TransformFromDevice ?? Matrix.Identity;
			var wpfRect = transformFromDevice.Transform(new Point(size.Width, size.Height));
			var width = wpfRect.X * zoomMultiplier;
			var height = wpfRect.Y * zoomMultiplier;
			return new Size(width, height);
		}

		public static Size TransformToDevice(IWpfTextView wpfTextView, Size size) {
			var zoomMultiplier = wpfTextView.ZoomLevel == 0 ? 1 : wpfTextView.ZoomLevel / 100;
			var source = PresentationSource.FromVisual(wpfTextView.VisualElement);
			var transformToDevice = source?.CompositionTarget.TransformToDevice ?? Matrix.Identity;
			var wpfRect = transformToDevice.Transform(new Point(size.Width, size.Height));
			var width = wpfRect.X * zoomMultiplier;
			var height = wpfRect.Y * zoomMultiplier;
			return new Size(width, height);
		}

		public static Rect TransformFromDevice(IWpfTextView wpfTextView, Rect rect) {
			var zoomMultiplier = wpfTextView.ZoomLevel == 0 ? 1 : 100 / wpfTextView.ZoomLevel;
			var source = PresentationSource.FromVisual(wpfTextView.VisualElement);
			var transformFromDevice = source?.CompositionTarget.TransformFromDevice ?? Matrix.Identity;
			var viewPoint = wpfTextView.VisualElement.PointToScreen(new Point(0, 0));
			var fixedRect = new Rect((rect.Left - viewPoint.X) * zoomMultiplier, (rect.Top - viewPoint.Y) * zoomMultiplier, rect.Width * zoomMultiplier, rect.Height * zoomMultiplier);
			var topLeft = transformFromDevice.Transform(fixedRect.TopLeft);
			var bottomRight = transformFromDevice.Transform(fixedRect.BottomRight);
			return new Rect(topLeft, bottomRight);
		}
	}
}
