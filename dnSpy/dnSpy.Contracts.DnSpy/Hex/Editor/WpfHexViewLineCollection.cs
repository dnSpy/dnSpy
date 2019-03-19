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

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Hex.Formatting;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// WPF hex view line collection
	/// </summary>
	public abstract class WpfHexViewLineCollection : HexViewLineCollection {
		/// <summary>
		/// Constructor
		/// </summary>
		protected WpfHexViewLineCollection() { }

		/// <summary>
		/// Gets a line
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public abstract WpfHexViewLine GetWpfHexViewLine(int index);

		/// <summary>
		/// Gets a line
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public override HexViewLine this[int index] => GetWpfHexViewLine(index);

		/// <summary>
		/// Gets the first visible line
		/// </summary>
		public abstract WpfHexViewLine FirstVisibleWpfLine { get; }

		/// <summary>
		/// Gets the last visible line
		/// </summary>
		public abstract WpfHexViewLine LastVisibleWpfLine { get; }

		/// <summary>
		/// Gets the first visible line
		/// </summary>
		public override HexViewLine FirstVisibleLine => FirstVisibleWpfLine;

		/// <summary>
		/// Gets the last visible line
		/// </summary>
		public override HexViewLine LastVisibleLine => LastVisibleWpfLine;

		/// <summary>
		/// Gets all the lines
		/// </summary>
		public abstract ReadOnlyCollection<WpfHexViewLine> WpfHexViewLines { get; }

		/// <summary>
		/// Gets a line marker geometry
		/// </summary>
		/// <param name="line">A line in this collection</param>
		/// <param name="span">Text span</param>
		/// <returns></returns>
		public abstract Geometry GetLineMarkerGeometry(WpfHexViewLine line, VST.Span span);

		/// <summary>
		/// Gets a line marker geometry
		/// </summary>
		/// <param name="line">A line in this collection</param>
		/// <param name="span">Text span</param>
		/// <param name="clipToViewport">true to clip the geometry to the viewport</param>
		/// <param name="padding">Padding to use</param>
		/// <returns></returns>
		public abstract Geometry GetLineMarkerGeometry(WpfHexViewLine line, VST.Span span, bool clipToViewport, Thickness padding);

		/// <summary>
		/// Gets a text marker geometry
		/// </summary>
		/// <param name="line">A line in this collection</param>
		/// <param name="span">Text span</param>
		/// <returns></returns>
		public abstract Geometry GetTextMarkerGeometry(WpfHexViewLine line, VST.Span span);

		/// <summary>
		/// Gets a text marker geometry
		/// </summary>
		/// <param name="line">A line in this collection</param>
		/// <param name="span">Text span</param>
		/// <param name="clipToViewport">true to clip the geometry to the viewport</param>
		/// <param name="padding">Padding to use</param>
		/// <returns></returns>
		public abstract Geometry GetTextMarkerGeometry(WpfHexViewLine line, VST.Span span, bool clipToViewport, Thickness padding);

		/// <summary>
		/// Gets a line marker geometry
		/// </summary>
		/// <param name="bufferSpan">Span</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public abstract Geometry GetLineMarkerGeometry(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags);

		/// <summary>
		/// Gets a line marker geometry
		/// </summary>
		/// <param name="bufferSpan">Span</param>
		/// <param name="flags">Flags</param>
		/// <param name="clipToViewport">true to clip the geometry to the viewport</param>
		/// <param name="padding">Padding to use</param>
		/// <returns></returns>
		public abstract Geometry GetLineMarkerGeometry(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags, bool clipToViewport, Thickness padding);

		/// <summary>
		/// Gets a marker geometry
		/// </summary>
		/// <param name="bufferSpan">Span</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public abstract Geometry GetMarkerGeometry(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags);

		/// <summary>
		/// Gets a marker geometry
		/// </summary>
		/// <param name="bufferSpan">Span</param>
		/// <param name="flags">Flags</param>
		/// <param name="clipToViewport">true to clip the geometry to the viewport</param>
		/// <param name="padding">Padding to use</param>
		/// <returns></returns>
		public abstract Geometry GetMarkerGeometry(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags, bool clipToViewport, Thickness padding);

		/// <summary>
		/// Gets a text marker geometry
		/// </summary>
		/// <param name="bufferSpan">Span</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public abstract Geometry GetTextMarkerGeometry(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags);

		/// <summary>
		/// Gets a text marker geometry
		/// </summary>
		/// <param name="bufferSpan">Span</param>
		/// <param name="flags">Flags</param>
		/// <param name="clipToViewport">true to clip the geometry to the viewport</param>
		/// <param name="padding">Padding to use</param>
		/// <returns></returns>
		public abstract Geometry GetTextMarkerGeometry(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags, bool clipToViewport, Thickness padding);

		/// <summary>
		/// Gets the line containing <paramref name="bufferPosition"/>
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		public abstract WpfHexViewLine GetWpfHexViewLineContainingBufferPosition(HexBufferPoint bufferPosition);
	}
}
