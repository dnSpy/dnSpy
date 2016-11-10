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
using dnSpy.Contracts.Hex.Formatting;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// WPF hex view
	/// </summary>
	public abstract class WpfHexView : HexView {
		/// <summary>
		/// Gets the UI element
		/// </summary>
		public abstract FrameworkElement VisualElement { get; }

		/// <summary>
		/// Gets/sets the background brush
		/// </summary>
		public abstract Brush Background { get; set; }

		/// <summary>
		/// Raised when the background property has changed
		/// </summary>
		public abstract event EventHandler<BackgroundBrushChangedEventArgs> BackgroundBrushChanged;

		/// <summary>
		/// Gets/sets the zoom level between 20% to 400%
		/// </summary>
		public abstract double ZoomLevel { get; set; }

		/// <summary>
		/// Raised when the zoom level has changed
		/// </summary>
		public abstract event EventHandler<ZoomLevelChangedEventArgs> ZoomLevelChanged;

		/// <summary>
		/// Gets the formatted line source
		/// </summary>
		public abstract HexFormattedLineSource FormattedLineSource { get; }

		/// <summary>
		/// Gets the line transform source
		/// </summary>
		public abstract HexLineTransformSource LineTransformSource { get; }

		/// <summary>
		/// Gets the WPF hex view lines
		/// </summary>
		public abstract WpfHexViewLineCollection WpfHexViewLines { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		protected WpfHexView() { }

		/// <summary>
		/// Gets an adornment layer
		/// </summary>
		/// <param name="name">Name of adornment layer</param>
		/// <returns></returns>
		public abstract HexAdornmentLayer GetAdornmentLayer(string name);

		/// <summary>
		/// Gets the space reservation manager
		/// </summary>
		/// <param name="name">Name of space reservation manager</param>
		/// <returns></returns>
		public abstract HexSpaceReservationManager GetSpaceReservationManager(string name);

		/// <summary>
		/// Gets the line that contains the position
		/// </summary>
		/// <param name="bufferPosition">Position</param>
		/// <returns></returns>
		public abstract WpfHexViewLine GetWpfHexViewLineContainingBufferPosition(HexBufferPoint bufferPosition);
	}
}
