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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="IWpfTextView"/> extensions
	/// </summary>
	static class WpfTextViewExtensions {
		/// <summary>
		/// Initializes <see cref="IWpfTextView.ZoomLevel"/> to the default value (<see cref="ZoomConstants.DefaultZoom"/>)
		/// to make sure that this <see cref="IWpfTextView"/> instance doesn't use the global zoom level.
		/// </summary>
		/// <param name="wpfTextView">Text view</param>
		public static void InitializeLocalZoomLevel(this IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			// Don't write wpfTextView.ZoomLevel because it's equal to the global zoom level. If
			// this text view was just created, we want it to use the default zoom level, not the
			// current global zoom level.
			wpfTextView.Options.SetOptionValue(DefaultWpfViewOptions.ZoomLevelId, ZoomConstants.DefaultZoom);
		}
	}
}
