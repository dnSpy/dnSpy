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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using VST = Microsoft.VisualStudio.Text;
using VSTC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Editor {
	sealed class HexSelectionImpl : HexSelection {
		public override HexView HexView { get; }//TODO:
		public override NormalizedHexBufferSpanCollection SelectedSpans => NormalizedHexBufferSpanCollection.Empty;//TODO:
		public override HexBufferSpan StreamSelectionSpan { get; }//TODO:
		public override bool IsEmpty => true;//TODO:
		public override bool IsActive { get; set; }//TODO:
		public override bool ActivationTracksFocus { get; set; }//TODO:
		public override event EventHandler SelectionChanged;//TODO:
		public override HexBufferPoint ActivePoint { get; }//TODO:
		public override HexBufferPoint AnchorPoint { get; }//TODO:

		public HexSelectionImpl(WpfHexView hexView, HexAdornmentLayer selectionLayer, VSTC.IEditorFormatMap editorFormatMap) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			if (selectionLayer == null)
				throw new ArgumentNullException(nameof(selectionLayer));
			if (editorFormatMap == null)
				throw new ArgumentNullException(nameof(editorFormatMap));
			//TODO:
			HexView = hexView;
			ActivationTracksFocus = true;
		}

		public override void Select(HexBufferSpan selectionSpan, bool isReversed) {
			//TODO:
		}

		public override void Select(HexBufferPoint anchorPoint, HexBufferPoint activePoint) {
			//TODO:
		}

		public override IEnumerable<VST.Span> GetSelectionOnTextViewLine(HexViewLine line) {
			throw new NotImplementedException();//TODO:
		}

		public override void Clear() {
			//TODO:
		}

		internal void Dispose() {
			//TODO:
		}
	}
}
