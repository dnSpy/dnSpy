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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Tagging;

namespace dnSpy.Hex.Formatting {
	sealed class HexAndAdornmentSequencerImpl : HexAndAdornmentSequencer {
		public override HexBuffer Buffer => hexView.HexBuffer;
		public override event EventHandler<HexAndAdornmentSequenceChangedEventArgs> SequenceChanged;//TODO:

		readonly HexTagAggregator<HexSpaceNegotiatingAdornmentTag> hexTagAggregator;
		readonly HexView hexView;

		public HexAndAdornmentSequencerImpl(HexView hexView, HexTagAggregator<HexSpaceNegotiatingAdornmentTag> hexTagAggregator) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			if (hexTagAggregator == null)
				throw new ArgumentNullException(nameof(hexTagAggregator));
			this.hexView = hexView;
			this.hexTagAggregator = hexTagAggregator;
		}

		public override HexAndAdornmentCollection CreateHexAndAdornmentCollection(HexBufferSpan span) {
			throw new NotImplementedException();//TODO:
		}

		public override HexAndAdornmentCollection CreateHexAndAdornmentCollection(HexBufferLine line) {
			throw new NotImplementedException();//TODO:
		}
	}
}
