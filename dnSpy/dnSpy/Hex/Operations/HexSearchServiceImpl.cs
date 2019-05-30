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
using System.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Operations;

namespace dnSpy.Hex.Operations {
	abstract class HexSearchServiceImpl : HexSearchService {
		protected IEnumerable<HexSpan> GetValidSpans(HexBuffer buffer, HexPosition start, HexPosition upperBounds) {
			var pos = start;
			bool fullSpan = true;
			while (pos < HexPosition.MaxEndPosition) {
				var span = buffer.GetNextValidSpan(pos, upperBounds, fullSpan);
				if (span is null)
					break;

				var newStart = HexPosition.Max(pos, span.Value.Start);
				var newEnd = HexPosition.Min(upperBounds, span.Value.End);
				if (newStart < newEnd)
					yield return HexSpan.FromBounds(newStart, newEnd);

				pos = span.Value.End;
				fullSpan = false;
			}
		}

		protected IEnumerable<HexSpan> GetValidSpansReverse(HexBuffer buffer, HexPosition start, HexPosition lowerBounds) {
			var pos = start;
			bool fullSpan = true;
			for (;;) {
				var span = buffer.GetPreviousValidSpan(pos, lowerBounds, fullSpan);
				if (span is null)
					break;

				var newStart = HexPosition.Max(lowerBounds, span.Value.Start);
				var newEnd = HexPosition.Min(pos + 1, span.Value.End);
				if (newStart < newEnd)
					yield return HexSpan.FromBounds(newStart, newEnd);

				if (span.Value.Start == 0)
					break;
				pos = span.Value.Start - 1;
				fullSpan = false;
			}
		}

		protected abstract class SearchStateBase : IDisposable {
			public readonly HexBuffer Buffer;
			protected byte[] Data;
			public /*readonly*/ CancellationToken CancellationToken;
			protected int dataIndex;
			protected int dataLength;
			protected HexPosition dataPosition;

			static class Cache {
				const int BUFFER_LENGTH = 0x1000;
				static WeakReference? weakBuffer;
				public static byte[] GetBuffer() => Interlocked.Exchange(ref weakBuffer, null)?.Target as byte[] ?? new byte[BUFFER_LENGTH];
				public static void ReturnBuffer(ref byte[]? buffer) {
					var tmp = buffer;
					if (!(tmp is null)) {
						buffer = null;
						weakBuffer = new WeakReference(tmp);
					}
				}
			}

			protected SearchStateBase(HexBuffer buffer, CancellationToken cancellationToken) {
				Buffer = buffer;
				Data = Cache.GetBuffer();
				CancellationToken = cancellationToken;
			}

			public void SetPosition(HexPosition position) {
				if (dataPosition <= position && position <= dataPosition + dataLength)
					dataIndex = (int)(position - dataPosition).ToUInt64();
				else {
					dataPosition = position;
					dataIndex = 0;
					dataLength = 0;
				}
			}

			public void SetPreviousPosition(HexPosition position) {
				if (dataPosition <= position && position < dataPosition + dataLength)
					dataIndex = (int)(position - dataPosition).ToUInt64();
				else {
					dataPosition = position + 1;
					dataIndex = -1;
					dataLength = 0;
				}
			}

			public byte GetNextByte() {
				Debug.Assert(dataPosition < HexPosition.MaxEndPosition);
				if (dataIndex >= dataLength)
					FillNextData();
				return Data[dataIndex++];
			}

			public byte GetPreviousByte() {
				Debug.Assert(dataPosition < HexPosition.MaxEndPosition);
				if (dataIndex < 0)
					FillPreviousData();
				return Data[dataIndex--];
			}

			protected void FillPreviousData() {
				Debug.Assert(dataPosition > 0);
				Debug.Assert(dataIndex == -1);
				CancellationToken.ThrowIfCancellationRequested();
				dataLength = (int)HexPosition.Min(dataPosition, Data.Length).ToUInt64();
				dataPosition -= dataLength;
				dataIndex = dataLength - 1;
				Buffer.ReadBytes(dataPosition, Data, 0, dataLength);
			}

			protected void FillNextData() {
				Debug.Assert(dataIndex == dataLength);
				CancellationToken.ThrowIfCancellationRequested();
				dataPosition += dataLength;
				dataLength = (int)HexPosition.Min(HexPosition.MaxEndPosition - dataPosition, Data.Length).ToUInt64();
				dataIndex = 0;
				Buffer.ReadBytes(dataPosition, Data, 0, dataLength);
			}

			public void Dispose() => Cache.ReturnBuffer(ref Data!);
		}
	}
}
