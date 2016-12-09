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

using System.Collections.Generic;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Operations;

namespace dnSpy.Hex.Operations {
	abstract class HexSearchServiceImpl : HexSearchService {
		protected IEnumerable<HexSpan> GetValidSpans(HexBuffer buffer, HexPosition start, HexPosition upperBounds) {
			var pos = start;
			while (pos < HexPosition.MaxEndPosition) {
				var span = buffer.GetNextValidSpan(pos, upperBounds);
				if (span == null)
					break;

				var newStart = HexPosition.Max(pos, span.Value.Start);
				var newEnd = HexPosition.Min(upperBounds, span.Value.End);
				if (newStart < newEnd)
					yield return HexSpan.FromBounds(newStart, newEnd);

				pos = span.Value.End;
			}
		}

		protected IEnumerable<HexSpan> GetValidSpansReverse(HexBuffer buffer, HexPosition start, HexPosition lowerBounds) {
			var pos = start;
			for (;;) {
				var span = buffer.GetPreviousValidSpan(pos, lowerBounds);
				if (span == null)
					break;

				var newStart = HexPosition.Max(lowerBounds, span.Value.Start);
				var newEnd = HexPosition.Min(pos + 1, span.Value.End);
				if (newStart < newEnd)
					yield return HexSpan.FromBounds(newStart, newEnd);

				if (span.Value.Start == 0)
					break;
				pos = span.Value.Start - 1;
			}
		}
	}
}
