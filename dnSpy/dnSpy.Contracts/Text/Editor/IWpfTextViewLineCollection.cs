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

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="IWpfTextViewLine"/> collection
	/// </summary>
	public interface IWpfTextViewLineCollection : ITextViewLineCollection {
		/// <summary>
		/// Gets the first visible line
		/// </summary>
		new IWpfTextViewLine FirstVisibleLine { get; }

		/// <summary>
		/// Gets the last visible line
		/// </summary>
		new IWpfTextViewLine LastVisibleLine { get; }

		/// <summary>
		/// Gets a line
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		new IWpfTextViewLine this[int index] { get; }

		/// <summary>
		/// Gets the text lines
		/// </summary>
		ReadOnlyCollection<IWpfTextViewLine> WpfTextViewLines { get; }

		/// <summary>
		/// Gets the text marker geometry for the specified range of text in the buffer by using a polygonal approximation algorithm to calculate the outline path of the text regions
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <returns></returns>
		Geometry GetLineMarkerGeometry(SnapshotSpan bufferSpan);

		/// <summary>
		/// Gets the text marker geometry for the specified range of text in the buffer by using a polygonal approximation algorithm to calculate the outline path of the text regions
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <param name="clipToViewport">If true, the created geometry will be clipped to the viewport</param>
		/// <param name="padding">A padding that's applied to the elements on a per line basis</param>
		/// <returns></returns>
		Geometry GetLineMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding);

		/// <summary>
		/// Creates a marker geometry for the specified snapshot span
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <returns></returns>
		Geometry GetMarkerGeometry(SnapshotSpan bufferSpan);

		/// <summary>
		/// Creates a marker geometry for the specified snapshot span
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <param name="clipToViewport">If true, the created geometry will be clipped to the viewport</param>
		/// <param name="padding">A padding that's applied to the elements on a per line basis</param>
		/// <returns></returns>
		Geometry GetMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding);

		/// <summary>
		/// Gets the text marker geometry for the specified range of text in the buffer by using a polygonal approximation algorithm to calculate the outline path of the text regions
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <returns></returns>
		Geometry GetTextMarkerGeometry(SnapshotSpan bufferSpan);

		/// <summary>
		/// Gets the text marker geometry for the specified range of text in the buffer by using a polygonal approximation algorithm to calculate the outline path of the text regions
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <param name="clipToViewport">If true, the created geometry will be clipped to the viewport</param>
		/// <param name="padding">A padding that's applied to the elements on a per line basis</param>
		/// <returns></returns>
		Geometry GetTextMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding);

		/// <summary>
		/// Gets the <see cref="IWpfTextViewLine"/> that contains the specified text buffer position
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		new IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition);
	}
}
