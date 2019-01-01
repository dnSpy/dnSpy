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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Intellisense;

namespace dnSpy.Hex.Intellisense {
	sealed class HexQuickInfoController : HexIntellisenseController {
		readonly HexQuickInfoBroker quickInfoBroker;
		readonly HexView hexView;

		public HexQuickInfoController(HexQuickInfoBroker quickInfoBroker, HexView hexView) {
			this.hexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
			this.quickInfoBroker = quickInfoBroker ?? throw new ArgumentNullException(nameof(quickInfoBroker));
			hexView.MouseHover += HexView_MouseHover;
		}

		void HexView_MouseHover(object sender, HexMouseHoverEventArgs e) {
			var posInfo = e.Line.GetLinePositionInfo(e.TextPosition);
			HexCellPosition triggerPoint;
			if (posInfo.IsAsciiCell && posInfo.Cell.HasData)
				triggerPoint = new HexCellPosition(HexColumnType.Ascii, posInfo.Cell.BufferStart, posInfo.CellPosition);
			else if (posInfo.IsValueCell && posInfo.Cell.HasData)
				triggerPoint = new HexCellPosition(HexColumnType.Values, hexView.BufferLines.GetValueBufferSpan(posInfo.Cell, posInfo.CellPosition).Start, posInfo.CellPosition);
			else if (posInfo.IsValueCellSeparator && posInfo.Cell.HasData)
				triggerPoint = new HexCellPosition(HexColumnType.Values, hexView.BufferLines.GetValueBufferSpan(posInfo.Cell, posInfo.Cell.CellSpan.Length - 1).Start, posInfo.Cell.CellSpan.Length - 1);
			else
				return;

			var sessions = quickInfoBroker.GetSessions(hexView);
			foreach (var session in sessions) {
				if (Intersects(session.ApplicableToSpan, triggerPoint))
					return;
				if (session.HasInteractiveContent) {
					foreach (var o in session.QuickInfoContent) {
						var io = o as IHexInteractiveQuickInfoContent;
						if (io == null)
							continue;
						if (io.KeepQuickInfoOpen || io.IsMouseOverAggregated)
							return;
					}
				}
			}
			foreach (var session in sessions)
				session.Dismiss();
			quickInfoBroker.TriggerQuickInfo(hexView, triggerPoint, trackMouse: true);
		}

		bool Intersects(HexBufferSpanSelection span, HexCellPosition point) {
			if (span.IsDefault)
				return false;
			if (point.BufferPosition.Buffer != span.BufferSpan.Buffer)
				return false;
			return span.BufferSpan.IntersectsWith(new HexSpan(point.BufferPosition, 0));
		}

		public override void Detach(HexView hexView) => hexView.MouseHover -= HexView_MouseHover;
	}
}
