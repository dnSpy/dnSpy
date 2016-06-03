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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// View scroller
	/// </summary>
	public interface IViewScroller {
		/// <summary>
		/// Ensures that all the text in the specified span is entirely visible in the view.
		/// </summary>
		/// <param name="span">The span to make visible</param>
		void EnsureSpanVisible(SnapshotSpan span);

		/// <summary>
		/// Ensures that all the text in the specified span is entirely visible in the view.
		/// </summary>
		/// <param name="span">The span to make visible</param>
		/// <param name="options">Options</param>
		void EnsureSpanVisible(SnapshotSpan span, EnsureSpanVisibleOptions options);

		/// <summary>
		/// Ensures that all the text in the specified span is entirely visible in the view.
		/// </summary>
		/// <param name="span">The span to make visible</param>
		/// <param name="options">Options</param>
		void EnsureSpanVisible(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options);

		/// <summary>
		/// Scrolls the viewport horizontally by the specified distance.
		/// </summary>
		/// <param name="distanceToScroll">The distance to scroll the viewport in the text rendering coordinate system. Positive values scroll the viewport to the right, and negative values scroll the viewport to the left.</param>
		void ScrollViewportHorizontallyByPixels(double distanceToScroll);

		/// <summary>
		/// Scrolls the viewport vertically one line up or down.
		/// </summary>
		/// <param name="direction">The direction in which to scroll</param>
		void ScrollViewportVerticallyByLine(ScrollDirection direction);

		/// <summary>
		/// Scrolls the viewport vertically by multiple lines up or down.
		/// </summary>
		/// <param name="direction">The direction in which to scroll</param>
		/// <param name="count">The number of lines to scroll up or down</param>
		void ScrollViewportVerticallyByLines(ScrollDirection direction, int count);

		/// <summary>
		/// Scrolls the viewport vertically one page up or down.
		/// </summary>
		/// <param name="direction">The direction in which to scroll</param>
		/// <returns></returns>
		bool ScrollViewportVerticallyByPage(ScrollDirection direction);

		/// <summary>
		/// Scrolls the viewport vertically by the specified distance.
		/// </summary>
		/// <param name="distanceToScroll">The distance to scroll in the text rendering coordinate system. Positive values scroll the viewport up, and negative values scroll the viewport down.</param>
		void ScrollViewportVerticallyByPixels(double distanceToScroll);
	}
}
