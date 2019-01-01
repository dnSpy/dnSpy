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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using VST = Microsoft.VisualStudio.Text;
using VSTC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Editor {
	sealed class HexSelectionImpl : HexSelection {
		internal const HexSpanSelectionFlags SelectionFlags = HexSpanSelectionFlags.Selection;
		public override HexView HexView { get; }
		public override HexBufferPoint AnchorPoint => anchorPoint;
		public override HexBufferPoint ActivePoint => activePoint;
		public override event EventHandler SelectionChanged;

		public override bool IsActive {
			get => hexSelectionLayer.IsActive;
			set => hexSelectionLayer.IsActive = value;
		}

		public override bool ActivationTracksFocus {
			get => activationTracksFocus;
			set {
				if (activationTracksFocus == value)
					return;
				activationTracksFocus = value;
				if (value)
					IsActive = HexView.HasAggregateFocus;
			}
		}
		bool activationTracksFocus;

		public override NormalizedHexBufferSpanCollection SelectedSpans => new NormalizedHexBufferSpanCollection(StreamSelectionSpan);

		readonly HexSelectionLayer hexSelectionLayer;
		HexBufferPoint anchorPoint, activePoint;

		public HexSelectionImpl(WpfHexView hexView, HexAdornmentLayer selectionLayer, VSTC.IEditorFormatMap editorFormatMap) {
			if (selectionLayer == null)
				throw new ArgumentNullException(nameof(selectionLayer));
			if (editorFormatMap == null)
				throw new ArgumentNullException(nameof(editorFormatMap));
			HexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
			HexView.GotAggregateFocus += HexView_GotAggregateFocus;
			HexView.LostAggregateFocus += HexView_LostAggregateFocus;
			hexSelectionLayer = new HexSelectionLayer(this, selectionLayer, editorFormatMap);
			ActivationTracksFocus = true;
		}

		internal void Initialize() {
			activePoint = anchorPoint = HexView.BufferLines.BufferStart;
			HexView.BufferLinesChanged += HexView_BufferLinesChanged;
		}

		void HexView_BufferLinesChanged(object sender, BufferLinesChangedEventArgs e) {
			var newActivePoint = Filter(activePoint);
			var newAnchorPoint = Filter(anchorPoint);
			if (newActivePoint != activePoint || newAnchorPoint != anchorPoint)
				Select(newAnchorPoint, newActivePoint, alignPoints: true);
		}

		HexBufferPoint Filter(HexBufferPoint position) {
			if (position < HexView.BufferLines.BufferStart)
				return HexView.BufferLines.BufferStart;
			if (position > HexView.BufferLines.BufferEnd)
				return HexView.BufferLines.BufferEnd;
			return position;
		}

		void HexView_GotAggregateFocus(object sender, EventArgs e) {
			if (ActivationTracksFocus)
				IsActive = true;
		}

		void HexView_LostAggregateFocus(object sender, EventArgs e) {
			if (ActivationTracksFocus)
				IsActive = false;
		}

		public override void Clear() {
			bool isEmpty = IsEmpty;
			ActivationTracksFocus = true;
			anchorPoint = activePoint;
			if (!isEmpty)
				SelectionChanged?.Invoke(this, EventArgs.Empty);
		}

		public override IEnumerable<VST.Span> GetSelectionOnHexViewLine(HexViewLine line) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			if (line.BufferLine.LineProvider != HexView.BufferLines)
				throw new ArgumentException();

			foreach (var info in line.BufferLine.GetSpans(StreamSelectionSpan, SelectionFlags)) {
				if (info.TextSpan.Length != 0)
					yield return info.TextSpan;
			}
		}

		public override void Select(HexBufferSpan selectionSpan, bool isReversed, bool alignPoints) {
			if (isReversed)
				Select(selectionSpan.End, selectionSpan.Start, alignPoints);
			else
				Select(selectionSpan.Start, selectionSpan.End, alignPoints);
		}

		public override void Select(HexBufferPoint anchorPoint, HexBufferPoint activePoint, bool alignPoints) {
			if (anchorPoint.Buffer != activePoint.Buffer)
				throw new ArgumentException();
			if (anchorPoint.Buffer != HexView.Buffer)
				throw new ArgumentException();
			if (anchorPoint == activePoint) {
				Clear();
				return;
			}
			ActivationTracksFocus = true;

			if (alignPoints && HexView.Caret.Position.Position.ActiveColumn == HexColumnType.Values) {
				var bufferLines = HexView.BufferLines;
				if (bufferLines.BytesPerValue != 1) {
					Debug.Assert(anchorPoint != activePoint);
					if (anchorPoint < activePoint) {
						var anchorCell = GetCell(bufferLines, anchorPoint);
						var activeCell = GetCell(bufferLines, activePoint - 1);
						if (anchorCell != null && activeCell != null) {
							anchorPoint = anchorCell.BufferStart;
							activePoint = activeCell.BufferEnd;
						}
					}
					else {
						var activeCell = GetCell(bufferLines, activePoint);
						var anchorCell = GetCell(bufferLines, anchorPoint - 1);
						if (anchorCell != null && activeCell != null) {
							activePoint = activeCell.BufferStart;
							anchorPoint = anchorCell.BufferEnd;
						}
					}
				}
			}

			bool sameSelection = this.anchorPoint == anchorPoint && this.activePoint == activePoint;
			if (!sameSelection) {
				this.anchorPoint = anchorPoint;
				this.activePoint = activePoint;
				SelectionChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		static HexCell GetCell(HexBufferLineFormatter bufferLines, HexBufferPoint position) {
			var line = bufferLines.GetLineFromPosition(position);
			var cell = line.ValueCells.GetCell(position);
			if (cell == null && position == line.BufferEnd && position > line.BufferStart)
				cell = line.ValueCells.GetCell(position - 1);
			return cell;
		}

		internal void Dispose() {
			HexView.GotAggregateFocus -= HexView_GotAggregateFocus;
			HexView.LostAggregateFocus -= HexView_LostAggregateFocus;
			HexView.BufferLinesChanged -= HexView_BufferLinesChanged;
			hexSelectionLayer.Dispose();
		}
	}
}
