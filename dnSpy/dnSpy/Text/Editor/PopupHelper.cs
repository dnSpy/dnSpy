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

using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Controls;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	static class PopupHelper {
		const double maxHeightMultiplier = 0.8;
		const double maxWidthMultiplier = 0.8;

		public static void SetScaleTransform(IWpfTextView wpfTextView, FrameworkElement popupElement) {
			var metroWindow = Window.GetWindow(wpfTextView.VisualElement) as MetroWindow;
			if (metroWindow == null)
				return;
			metroWindow.SetScaleTransform(popupElement, wpfTextView.ZoomLevel / 100);

			var screen = new Screen(wpfTextView.VisualElement);
			if (screen.IsValid) {
				var zoomMultiplier = wpfTextView.ZoomLevel == 0 ? 1 : 100 / wpfTextView.ZoomLevel;
				var source = PresentationSource.FromVisual(wpfTextView.VisualElement);
				var transformFromDevice = source?.CompositionTarget.TransformFromDevice ?? Matrix.Identity;
				var wpfRect = transformFromDevice.Transform(new Point(screen.DisplayRect.Width, screen.DisplayRect.Height));
				popupElement.MaxWidth = wpfRect.X * zoomMultiplier * maxWidthMultiplier;
				popupElement.MaxHeight = wpfRect.Y * zoomMultiplier * maxHeightMultiplier;
			}
		}
	}
}
