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
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor {
	sealed class HexScrollMapImpl : HexScrollMap, IDisposable {
		public override event EventHandler MappingChanged;
		public override HexView HexView { get; }
		public override double Start => 0;
		public override double End => end;
		public override double ThumbSize => thumbSize;

		double end;
		double thumbSize;

		public HexScrollMapImpl(HexView hexView) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			HexView = hexView;
			HexView.BufferLinesChanged += HexView_BufferLinesChanged;
			HexView.LayoutChanged += HexView_LayoutChanged;
			HexView.Closed += HexView_Closed;
		}

		void HexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) {
			if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				UpdateCachedState();
		}

		void HexView_Closed(object sender, EventArgs e) => Dispose();
		void HexView_BufferLinesChanged(object sender, BufferLinesChangedEventArgs e) => UpdateCachedState();

		void UpdateCachedState() {
			var lineCount = HexView.BufferLines.LineCount;
			end = Start + (lineCount == HexPosition.MaxEndPosition ? ulong.MaxValue : lineCount.ToUInt64());
			thumbSize = HexView.ViewportHeight / HexView.LineHeight;
			MappingChanged?.Invoke(this, EventArgs.Empty);
		}

		public override HexBufferPoint GetBufferPositionAtCoordinate(double coordinate) {
			var bufferLines = HexView.BufferLines;
			if (coordinate >= End)
				return bufferLines.GetBufferPositionFromLineNumber(bufferLines.LineCount - 1);
			coordinate -= Start;
			if (coordinate < 0)
				coordinate = 0;
			HexPosition lineNumber = coordinate >= ulong.MaxValue ? HexPosition.MaxEndPosition : (ulong)coordinate;
			if (lineNumber >= bufferLines.LineCount)
				lineNumber = bufferLines.LineCount - 1;
			return bufferLines.GetBufferPositionFromLineNumber(lineNumber);
		}

		public override double GetCoordinateAtBufferPosition(HexBufferPoint bufferPosition) {
			if (bufferPosition.IsDefault)
				throw new ArgumentException();
			if (bufferPosition.Buffer != HexView.Buffer)
				throw new ArgumentException();
			if (!HexView.BufferLines.IsValidPosition(bufferPosition))
				throw new ArgumentOutOfRangeException(nameof(bufferPosition));
			return Start + HexView.BufferLines.GetLineNumberFromPosition(bufferPosition).ToUInt64();
		}

		public override HexBufferPoint GetBufferPositionAtFraction(double fraction) {
			if (fraction < 0 || fraction > 1)
				throw new ArgumentOutOfRangeException(nameof(fraction));
			double length = End - Start;
			var coord = Start + fraction * length;
			return GetBufferPositionAtCoordinate(coord);
		}

		public override double GetFractionAtBufferPosition(HexBufferPoint bufferPosition) {
			if (bufferPosition.IsDefault)
				throw new ArgumentException();
			if (bufferPosition.Buffer != HexView.Buffer)
				throw new ArgumentException();
			if (!HexView.BufferLines.IsValidPosition(bufferPosition))
				throw new ArgumentOutOfRangeException(nameof(bufferPosition));
			double length = End - Start;
			if (length == 0)
				return 0;
			if (HexView.BufferLines.GetLineNumberFromPosition(bufferPosition) + 1 == HexView.BufferLines.LineCount)
				return 1;
			var coord = GetCoordinateAtBufferPosition(bufferPosition);
			Debug.Assert(Start <= coord && coord <= End);
			return Math.Min(Math.Max(0, (coord - Start) / length), 1);
		}

		public void Dispose() {
			HexView.BufferLinesChanged -= HexView_BufferLinesChanged;
			HexView.LayoutChanged -= HexView_LayoutChanged;
			HexView.Closed -= HexView_Closed;
		}
	}
}
