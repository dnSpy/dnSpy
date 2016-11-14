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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Editor {
	sealed class WpfHexViewLineCollectionImpl : WpfHexViewLineCollection {
		readonly WpfHexView hexView;
		readonly ReadOnlyCollection<WpfHexViewLine> lines;
		readonly HexBuffer buffer;

		public WpfHexViewLineCollectionImpl(WpfHexView hexView, HexBuffer buffer, IList<WpfHexViewLine> lines) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (lines == null)
				throw new ArgumentNullException(nameof(lines));
			this.hexView = hexView;
			this.buffer = buffer;
			this.lines = new ReadOnlyCollection<WpfHexViewLine>(lines);
			isValid = true;
			if (lines.Count == 0)
				formattedSpan = new HexBufferSpan(buffer, new HexSpan(HexPosition.Zero, 0));
			else
				formattedSpan = new HexBufferSpan(lines[0].BufferLine.VisibleBytesSpan.Start, lines[lines.Count - 1].BufferLine.VisibleBytesSpan.End);
			Debug.Assert(this.lines.Count > 0);
		}

		public override WpfHexViewLine GetWpfHexViewLine(int index) => lines[index];
		public override int Count => lines.Count;
		public override IEnumerator<HexViewLine> GetEnumerator() => lines.GetEnumerator();

		public override ReadOnlyCollection<WpfHexViewLine> WpfHexViewLines {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfHexViewLineCollectionImpl));
				return lines;
			}
		}

		public override WpfHexViewLine FirstVisibleWpfLine {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfHexViewLineCollectionImpl));
				foreach (var l in lines) {
					if (l.IsVisible())
						return l;
				}
				return lines[0];
			}
		}

		public override WpfHexViewLine LastVisibleWpfLine {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfHexViewLineCollectionImpl));
				for (int i = lines.Count - 1; i >= 0; i--) {
					var l = lines[i];
					if (l.IsVisible())
						return l;
				}
				return lines[lines.Count - 1];
			}
		}

		public override HexBufferSpan FormattedSpan {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfHexViewLineCollectionImpl));
				return formattedSpan;
			}
		}
		readonly HexBufferSpan formattedSpan;

		public override bool IsValid => isValid;
		bool isValid;

		public override bool ContainsBufferPosition(HexBufferPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfHexViewLineCollectionImpl));
			if (bufferPosition.Buffer != buffer)
				throw new ArgumentException();
			if (FormattedSpan.Contains(bufferPosition))
				return true;
			if (lines.Count > 0 && lines[lines.Count - 1].ContainsBufferPosition(bufferPosition))
				return true;
			return false;
		}

		public override bool IntersectsBufferSpan(HexBufferSpan bufferSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfHexViewLineCollectionImpl));
			if (bufferSpan.Buffer != buffer)
				throw new ArgumentException();
			if (FormattedSpan.IntersectsWith(bufferSpan))
				return true;
			if (lines.Count > 0 && lines[lines.Count - 1].IntersectsBufferSpan(bufferSpan))
				return true;
			return false;
		}

		public override Geometry GetLineMarkerGeometry(HexBufferSpan bufferSpan, HexMarkerFlags flags) =>
			GetMarkerGeometry(bufferSpan, flags, HexMarkerHelper.LineMarkerPadding, true);
		public override Geometry GetLineMarkerGeometry(HexBufferSpan bufferSpan, HexMarkerFlags flags, Thickness padding) =>
			GetMarkerGeometry(bufferSpan, flags, padding, true);

		public override Geometry GetTextMarkerGeometry(HexBufferSpan bufferSpan, HexMarkerFlags flags) =>
			GetMarkerGeometry(bufferSpan, flags, HexMarkerHelper.TextMarkerPadding, false);
		public override Geometry GetTextMarkerGeometry(HexBufferSpan bufferSpan, HexMarkerFlags flags, Thickness padding) =>
			GetMarkerGeometry(bufferSpan, flags, padding, false);

		Geometry GetMarkerGeometry(HexBufferSpan bufferSpan, HexMarkerFlags flags, Thickness padding, bool isLineGeometry) {
			if (bufferSpan.Buffer != buffer)
				throw new ArgumentException();

			bool createOutlinedPath = false;
			PathGeometry geo = null;
			var textBounds = GetNormalizedTextBounds(bufferSpan, GetTextBoundsFlags(flags));
			bool clipToViewport = (flags & HexMarkerFlags.ClipToViewport) != 0;
			HexMarkerHelper.AddGeometries(hexView, textBounds, isLineGeometry, clipToViewport, padding, 0, ref geo, ref createOutlinedPath);
			if (createOutlinedPath)
				geo = geo.GetOutlinedPathGeometry();
			if (geo != null && geo.CanFreeze)
				geo.Freeze();
			return geo;
		}

		static TextBoundsFlags GetTextBoundsFlags(HexMarkerFlags flags) {
			var res = TextBoundsFlags.None;
			if ((flags & HexMarkerFlags.Offset) != 0)
				res |= TextBoundsFlags.Offset;
			if ((flags & HexMarkerFlags.Values) != 0)
				res |= TextBoundsFlags.Values;
			if ((flags & HexMarkerFlags.Ascii) != 0)
				res |= TextBoundsFlags.Ascii;
			if ((flags & HexMarkerFlags.Cell) != 0)
				res |= TextBoundsFlags.Cell;
			if ((flags & HexMarkerFlags.Separator) != 0)
				res |= TextBoundsFlags.Separator;
			return res;
		}

		public override Geometry GetMarkerGeometry(HexBufferSpan bufferSpan, HexMarkerFlags flags) {
			if (bufferSpan.Buffer != buffer)
				throw new ArgumentException();
			if (HexMarkerHelper.IsMultiLineSpan(hexView, bufferSpan))
				return GetLineMarkerGeometry(bufferSpan, flags);
			return GetTextMarkerGeometry(bufferSpan, flags);
		}

		public override Geometry GetMarkerGeometry(HexBufferSpan bufferSpan, HexMarkerFlags flags, Thickness padding) {
			if (bufferSpan.Buffer != buffer)
				throw new ArgumentException();
			if (HexMarkerHelper.IsMultiLineSpan(hexView, bufferSpan))
				return GetLineMarkerGeometry(bufferSpan, flags, padding);
			return GetTextMarkerGeometry(bufferSpan, flags, padding);
		}

		public override Collection<TextBounds> GetNormalizedTextBounds(HexBufferSpan bufferSpan, TextBoundsFlags flags) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfHexViewLineCollectionImpl));
			if (bufferSpan.Buffer != buffer)
				throw new ArgumentException();
			var span = FormattedSpan.Overlap(bufferSpan);
			var list = new List<TextBounds>();
			if (span == null)
				return new Collection<TextBounds>(list);

			bool found = false;
			for (int i = 0; i < lines.Count; i++) {
				var line = lines[i];
				if (line.IntersectsBufferSpan(span.Value)) {
					found = true;
					list.AddRange(line.GetNormalizedTextBounds(span.Value, flags));
				}
				else if (found)
					break;
			}

			return new Collection<TextBounds>(list);
		}

		public override HexViewLine GetHexViewLineContainingBufferPosition(HexBufferPoint bufferPosition) =>
			GetWpfHexViewLineContainingBufferPosition(bufferPosition);
		public override WpfHexViewLine GetWpfHexViewLineContainingBufferPosition(HexBufferPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfHexViewLineCollectionImpl));
			if (bufferPosition.Buffer != buffer)
				throw new ArgumentException();
			foreach (var line in lines) {
				if (line.ContainsBufferPosition(bufferPosition))
					return line;
			}
			return null;
		}

		public override HexViewLine GetHexViewLineContainingYCoordinate(double y) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfHexViewLineCollectionImpl));
			if (double.IsNaN(y))
				throw new ArgumentOutOfRangeException(nameof(y));
			foreach (var line in lines) {
				if (line.Top <= y && y < line.Bottom)
					return line;
			}
			return null;
		}

		public override Collection<HexViewLine> GetHexViewLinesIntersectingSpan(HexBufferSpan bufferSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfHexViewLineCollectionImpl));
			if (bufferSpan.Buffer != buffer)
				throw new ArgumentException();
			var coll = new Collection<HexViewLine>();
			for (int i = 0; i < lines.Count; i++) {
				var line = lines[i];
				if (line.IntersectsBufferSpan(bufferSpan))
					coll.Add(line);
				else if (coll.Count > 0)
					break;
			}
			return coll;
		}

		public void Invalidate() {
			isValid = false;
		}
	}
}
