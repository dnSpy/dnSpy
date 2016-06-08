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
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// WPF text view
	/// </summary>
	public interface IWpfTextView : ITextView, IUIObjectProvider2 {
		/// <summary>
		/// Gets WPF text view's element
		/// </summary>
		FrameworkElement VisualElement { get; }

		/// <summary>
		/// Gets/sets the background color
		/// </summary>
		Brush Background { get; set; }

		/// <summary>
		/// Zoom level, between 20% and 400% (20.0 and 400.0)
		/// </summary>
		double ZoomLevel { get; set; }

		/// <summary>
		/// Gets the formatter
		/// </summary>
		IFormattedLineSource FormattedLineSource { get; }

		/// <summary>
		/// Gets the text view lines
		/// </summary>
		new IWpfTextViewLineCollection TextViewLines { get; }

		/// <summary>
		/// Gets the IWpfTextViewLine that contains the specified text buffer position
		/// </summary>
		/// <param name="bufferPosition">Position</param>
		/// <returns></returns>
		new IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition);

		/// <summary>
		/// Raised when <see cref="Background"/> has changed
		/// </summary>
		event EventHandler<BackgroundBrushChangedEventArgs> BackgroundBrushChanged;

		/// <summary>
		/// Raised when <see cref="ZoomLevel"/> has changed
		/// </summary>
		event EventHandler<ZoomLevelChangedEventArgs> ZoomLevelChanged;
	}
}
