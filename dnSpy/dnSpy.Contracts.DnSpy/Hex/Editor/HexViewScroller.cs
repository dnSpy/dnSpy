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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// View scroller
	/// </summary>
	public abstract class HexViewScroller {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexViewScroller() { }

		/// <summary>
		/// Scrolls a span into view
		/// </summary>
		/// <param name="lineSpan">Line span</param>
		public void EnsureSpanVisible(HexLineSpan lineSpan) => EnsureSpanVisible(lineSpan, EnsureSpanVisibleOptions.None);

		/// <summary>
		/// Scrolls a span into view
		/// </summary>
		/// <param name="lineSpan">Line span</param>
		/// <param name="options">Options</param>
		public abstract void EnsureSpanVisible(HexLineSpan lineSpan, EnsureSpanVisibleOptions options);

		/// <summary>
		/// Scrolls a span into view
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="flags">Flags</param>
		public void EnsureSpanVisible(HexBufferSpan span, HexSpanSelectionFlags flags) =>
			EnsureSpanVisible(span, flags, EnsureSpanVisibleOptions.None);

		/// <summary>
		/// Scrolls a span into view
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="flags">Flags</param>
		/// <param name="options">Options</param>
		public abstract void EnsureSpanVisible(HexBufferSpan span, HexSpanSelectionFlags flags, EnsureSpanVisibleOptions options);

		/// <summary>
		/// Scrolls a line into view
		/// </summary>
		/// <param name="line">Line</param>
		/// <param name="span">Line span</param>
		public void EnsureSpanVisible(HexBufferLine line, Span span) =>
			EnsureSpanVisible(line, span, EnsureSpanVisibleOptions.None);

		/// <summary>
		/// Scrolls a line into view
		/// </summary>
		/// <param name="line">Line</param>
		/// <param name="span">Line span</param>
		/// <param name="options">Options</param>
		public abstract void EnsureSpanVisible(HexBufferLine line, Span span, EnsureSpanVisibleOptions options);

		/// <summary>
		/// Scrolls the viewport horizontally
		/// </summary>
		/// <param name="distanceToScroll">Distance to scroll</param>
		public abstract void ScrollViewportHorizontallyByPixels(double distanceToScroll);

		/// <summary>
		/// Scrolls the viewport vertically
		/// </summary>
		/// <param name="distanceToScroll">Distance to scroll</param>
		public abstract void ScrollViewportVerticallyByPixels(double distanceToScroll);

		/// <summary>
		/// Scrolls the viewport one line up or down
		/// </summary>
		/// <param name="direction">Direction</param>
		public void ScrollViewportVerticallyByLine(ScrollDirection direction) =>
			ScrollViewportVerticallyByLines(direction, 1);

		/// <summary>
		/// Scrolls the viewport by lines
		/// </summary>
		/// <param name="direction">Direction</param>
		/// <param name="count">Number of lines to scroll</param>
		public abstract void ScrollViewportVerticallyByLines(ScrollDirection direction, int count);

		/// <summary>
		/// Scrolls the viewport one page up or down
		/// </summary>
		/// <param name="direction">Direction</param>
		/// <returns></returns>
		public abstract bool ScrollViewportVerticallyByPage(ScrollDirection direction);
	}
}
