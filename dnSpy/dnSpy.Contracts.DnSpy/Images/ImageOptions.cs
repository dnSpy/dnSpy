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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Image options
	/// </summary>
	public sealed class ImageOptions {
		/// <summary>
		/// Background color
		/// </summary>
		public Color? BackgroundColor { get; set; }

		/// <summary>
		/// Background brush or null. Only <see cref="SolidColorBrush"/> and <see cref="GradientBrush"/> brushes are supported.
		/// </summary>
		public Brush? BackgroundBrush { get; set; }

		/// <summary>
		/// Image size in logical pixels. 16x16 is used if this is 0x0
		/// </summary>
		public Size LogicalSize { get; set; }

		/// <summary>
		/// Total zoom applied to the element containing the image or (0,0) if the property shouldn't be used.
		/// 1.0 == 100%
		/// </summary>
		public Size Zoom { get; set; }

		/// <summary>
		/// If initialized, the DPI of its containing window will be used and <see cref="Dpi"/> doesn't have to be initialized.
		/// </summary>
		public DependencyObject? DpiObject { get; set; }

		/// <summary>
		/// DPI or (0,0) to use the default DPI (DPI of main window)
		/// </summary>
		public Size Dpi { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ImageOptions() { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="textView">Text view with which to initialize <see cref="Zoom"/> and <see cref="DpiObject"/></param>
		public ImageOptions(ITextView textView) {
			if (textView is null)
				throw new ArgumentNullException(nameof(textView));
			var wpfTextView = textView as IWpfTextView;
			Debug2.Assert(wpfTextView is not null);
			if (wpfTextView is not null) {
				Zoom = new Size(wpfTextView.ZoomLevel / 100, wpfTextView.ZoomLevel / 100);
				DpiObject = wpfTextView.VisualElement;
			}
		}
	}
}
