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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Operations;

namespace dnSpy.Hex.Operations {
	abstract class StringHexSearchService : HexSearchServiceImpl {
		readonly byte[] lowerBytes;
		readonly byte[] upperBytes;
		readonly byte[] charLengths;
		readonly bool valid;

		public override int ByteCount => lowerBytes.Length;

		protected StringHexSearchService(string pattern) {
			valid = Initialize(pattern, out lowerBytes!, out upperBytes!, out charLengths!);
			Debug.Assert(valid);
		}

		protected abstract bool Initialize(string pattern, [NotNullWhen(true)] out byte[]? lowerBytes, [NotNullWhen(true)] out byte[]? upperBytes, [NotNullWhen(true)] out byte[]? charLengths);

		protected bool Initialize(Encoding encoding, string pattern, [NotNullWhen(true)] out byte[]? lowerBytes, [NotNullWhen(true)] out byte[]? upperBytes, [NotNullWhen(true)] out byte[]? charLengths) {
			lowerBytes = null;
			upperBytes = null;
			charLengths = null;

			var lower = pattern.ToLowerInvariant();
			var upper = pattern.ToUpperInvariant();
			if (lower.Length != upper.Length)
				return false;
			if (!GetCharLengths(encoding, lower, out var lowerCharLengths, out var lowerBytesTmp))
				return false;
			if (!GetCharLengths(encoding, upper, out var upperCharLengths, out var upperBytesTmp))
				return false;

			if (lowerBytesTmp.Length != upperBytesTmp.Length)
				return false;
			if (lowerCharLengths.Length != upperCharLengths.Length)
				return false;
			for (int i = 0; i < lowerCharLengths.Length; i++) {
				if (lowerCharLengths[i] != upperCharLengths[i])
					return false;
			}

			lowerBytes = lowerBytesTmp;
			upperBytes = upperBytesTmp;
			charLengths = lowerCharLengths;
			return true;
		}

		bool GetCharLengths(Encoding encoding, string s, [NotNullWhen(true)] out byte[]? charLengths, [NotNullWhen(true)] out byte[]? encodedBytes) {
			charLengths = null;
			encodedBytes = null;

			var decoder = encoding.GetDecoder();
			var encodedBytesTmp = encoding.GetBytes(s);
			var bytes = new byte[1];
			var chars = new char[2];
			var charLengthsTmp = new byte[s.Length];
			int charLengthsIndex = 0;
			int charStartByteIndex = 0;
			for (int encodedBytesIndex = 0; encodedBytesIndex < encodedBytesTmp.Length; encodedBytesIndex++) {
				bytes[0] = encodedBytesTmp[encodedBytesIndex];
				var isLastByte = encodedBytesIndex + 1 == encodedBytesTmp.Length;
				decoder.Convert(bytes, 0, 1, chars, 0, 2, isLastByte, out int bytesUsed, out int charsUsed, out bool completed);
				if (isLastByte && charsUsed == 0)
					return false;
				if (charsUsed > 0) {
					if (charLengthsIndex >= charLengthsTmp.Length)
						return false;
					int bytesPerChar = encodedBytesIndex - charStartByteIndex + 1;
					if (bytesPerChar > byte.MaxValue)
						return false;
					charLengthsTmp[charLengthsIndex++] = (byte)bytesPerChar;
					charStartByteIndex = encodedBytesIndex + 1;
				}
			}
			if (charLengthsTmp.Length != charLengthsIndex) {
				var old = charLengthsTmp;
				charLengthsTmp = new byte[charLengthsIndex];
				Array.Copy(old, 0, charLengthsTmp, 0, charLengthsTmp.Length);
			}
			charLengths = charLengthsTmp;
			encodedBytes = encodedBytesTmp;
			return true;
		}

		public override IEnumerable<HexBufferSpan> FindAll(HexBufferSpan searchRange, HexBufferPoint startingPosition, HexFindOptions options, CancellationToken cancellationToken) {
			if (searchRange.IsDefault)
				throw new ArgumentException();
			if (searchRange.Buffer != startingPosition.Buffer)
				throw new ArgumentException();
			if (startingPosition < searchRange.Start || startingPosition > searchRange.End)
				throw new ArgumentException();
			if (!valid)
				return Array.Empty<HexBufferSpan>();
			if (searchRange.IsEmpty)
				return Array.Empty<HexBufferSpan>();
			return new FindAllCoreEnumerable(this, searchRange, startingPosition, options, cancellationToken);
		}

		// Needed so we can return the byte[] buffer to the cache
		sealed class FindAllCoreEnumerable : IEnumerable<HexBufferSpan> {
			readonly StringHexSearchService owner;
			/*readonly*/ HexBufferSpan searchRange;
			/*readonly*/ HexBufferPoint startingPosition;
			readonly HexFindOptions options;
			/*readonly*/ CancellationToken cancellationToken;

			public FindAllCoreEnumerable(StringHexSearchService owner, HexBufferSpan searchRange, HexBufferPoint startingPosition, HexFindOptions options, CancellationToken cancellationToken) {
				this.owner = owner;
				this.searchRange = searchRange;
				this.startingPosition = startingPosition;
				this.options = options;
				this.cancellationToken = cancellationToken;
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			public IEnumerator<HexBufferSpan> GetEnumerator() => new Enumerator(this);

			sealed class Enumerator : IEnumerator<HexBufferSpan> {
				readonly IEnumerator<HexBufferSpan> realEnumerator;
				SearchState state;

				public Enumerator(FindAllCoreEnumerable ownerEnumerable) {
					state = new SearchState(ownerEnumerable.searchRange.Buffer, ownerEnumerable.cancellationToken);
					if ((ownerEnumerable.options & HexFindOptions.SearchReverse) != 0)
						realEnumerator = ownerEnumerable.owner.FindAllCoreReverse(state, ownerEnumerable.searchRange, ownerEnumerable.startingPosition, ownerEnumerable.options, ownerEnumerable.cancellationToken).GetEnumerator();
					else
						realEnumerator = ownerEnumerable.owner.FindAllCore(state, ownerEnumerable.searchRange, ownerEnumerable.startingPosition, ownerEnumerable.options, ownerEnumerable.cancellationToken).GetEnumerator();
				}

				object IEnumerator.Current => Current;
				public HexBufferSpan Current => realEnumerator.Current;
				public bool MoveNext() => realEnumerator.MoveNext();
				public void Dispose() {
					state?.Dispose();
					state = null!;
					realEnumerator.Dispose();
				}

				public void Reset() => throw new NotSupportedException();
			}
		}

		sealed class SearchState : SearchStateBase {
			public SearchState(HexBuffer buffer, CancellationToken cancellationToken)
				: base(buffer, cancellationToken) {
			}

			public HexPosition? PositionAfter1(int value1a, int value1b, HexPosition end) {
				if (end < dataPosition)
					return null;
				var dataLocal = Data;
				for (;;) {
					if (dataIndex >= dataLength)
						FillNextData();
					int len = (int)HexPosition.Min(end - (dataPosition + dataIndex), dataLength - dataIndex).ToUInt64();
					int index1a = Array.IndexOf(dataLocal, (byte)value1a, dataIndex, len);
					int index1b = Array.IndexOf(dataLocal, (byte)value1b, dataIndex, len);
					int index = (int)Math.Min((uint)index1a, (uint)index1b);
					if (index >= 0) {
						dataIndex = index + 1;
						return dataPosition + index + 1;
					}
					dataIndex = dataLength;
					var newPos = dataPosition + dataLength;
					if (newPos >= end)
						return null;
				}
			}

			public HexPosition? PositionAfter2(int value1a, int value1b, int value2a, int value2b, HexPosition end) {
				if (end < dataPosition)
					return null;
				HexPosition newPos;
				var dataLocal = Data;
				for (;;) {
					if (dataIndex >= dataLength)
						FillNextData();
					int len = (int)HexPosition.Min(end - (dataPosition + dataIndex), dataLength - dataIndex).ToUInt64();
					int index1a = Array.IndexOf(dataLocal, (byte)value1a, dataIndex, len);
					int index1b = Array.IndexOf(dataLocal, (byte)value1b, dataIndex, len);
					int index = (int)Math.Min((uint)index1a, (uint)index1b);
					if (index >= 0) {
						index++;
						dataIndex = index;
						if (index >= dataLength) {
							newPos = dataPosition + dataLength;
							if (newPos >= end)
								return null;
							FillNextData();
							index = dataIndex;
						}
						bool match = dataLocal[index] == value2a || dataLocal[index] == value2b;
						if (match) {
							dataIndex = index + 1;
							return dataPosition + index + 1;
						}
					}
					else {
						dataIndex = dataLength;
						newPos = dataPosition + dataLength;
						if (newPos >= end)
							return null;
					}
				}
			}

			public HexPosition? PositionBefore1(int value1a, int value1b, HexPosition lowerBounds) {
				if (dataPosition == HexPosition.Zero && dataIndex < 0)
					return null;
				var dataLocal = Data;
				for (;;) {
					var currPos = dataPosition + dataIndex;
					if (lowerBounds > currPos)
						return null;
					if (dataIndex < 0)
						FillPreviousData();
					int len = (int)HexPosition.Min(currPos - lowerBounds + 1, dataIndex + 1).ToUInt64();
					int index1a = Array.LastIndexOf(dataLocal, (byte)value1a, dataIndex, len);
					int index1b = Array.LastIndexOf(dataLocal, (byte)value1b, dataIndex, len);
					int index = Math.Max(index1a, index1b);
					if (index >= 0) {
						dataIndex = index - 1;
						return dataPosition + (index - 1);
					}
					if (dataPosition == HexPosition.Zero)
						return null;
					dataIndex = -1;
				}
			}

			public HexPosition? PositionBefore2(int value1a, int value1b, int value2a, int value2b, HexPosition lowerBounds) {
				if (dataPosition == HexPosition.Zero && dataIndex < 0)
					return null;
				var dataLocal = Data;
				for (;;) {
					var currPos = dataPosition + dataIndex;
					if (lowerBounds > currPos)
						return null;
					if (dataIndex < 0)
						FillPreviousData();
					int len = (int)HexPosition.Min(currPos - lowerBounds + 1, dataIndex + 1).ToUInt64();
					int index1a = Array.LastIndexOf(dataLocal, (byte)value1a, dataIndex, len);
					int index1b = Array.LastIndexOf(dataLocal, (byte)value1b, dataIndex, len);
					int index = Math.Max(index1a, index1b);
					if (index >= 0) {
						index--;
						dataIndex = index;
						if (dataIndex < 0) {
							if (dataPosition <= lowerBounds)
								return null;
							FillPreviousData();
							index = dataIndex;
						}
						bool match = dataLocal[index] == value2a || dataLocal[index] == value2b;
						if (match) {
							dataIndex = index - 1;
							return dataPosition + (index - 1);
						}
					}
					else {
						if (dataPosition == HexPosition.Zero)
							return null;
						dataIndex = -1;
					}
				}
			}
		}

		IEnumerable<HexBufferSpan> FindAllCore(SearchState state, HexBufferSpan searchRange, HexBufferPoint startingPosition, HexFindOptions options, CancellationToken cancellationToken) {
			HexBufferSpan? firstBlockResult = null;
			foreach (var span in GetValidSpans(startingPosition.Buffer, startingPosition, searchRange.End)) {
				cancellationToken.ThrowIfCancellationRequested();
				foreach (var span2 in FindAllCore(state, span, options)) {
					if (firstBlockResult is null)
						firstBlockResult = span2;
					yield return span2;
				}
			}

			if ((options & HexFindOptions.Wrap) != 0) {
				var upperBounds = HexPosition.Min(searchRange.Span.End, startingPosition.Position + lowerBytes.LongLength - 1);
				if ((options & HexFindOptions.NoOverlaps) != 0 && !(firstBlockResult is null) && upperBounds > firstBlockResult.Value.Start)
					upperBounds = firstBlockResult.Value.Start;
				foreach (var span in GetValidSpans(startingPosition.Buffer, searchRange.Start, upperBounds)) {
					cancellationToken.ThrowIfCancellationRequested();
					foreach (var span2 in FindAllCore(state, span, options))
						yield return span2;
				}
			}
		}

		IEnumerable<HexBufferSpan> FindAllCore(SearchState state, HexSpan span, HexFindOptions options) {
			var pos = span.Start;
			if (pos + lowerBytes.LongLength > span.End)
				yield break;
			var endPos = span.End - lowerBytes.LongLength + 1;
			while (pos < endPos) {
				state.CancellationToken.ThrowIfCancellationRequested();
				var result = FindCore(state, pos, span.End);
				if (result is null)
					break;
				yield return new HexBufferSpan(state.Buffer, new HexSpan(result.Value, (ulong)lowerBytes.LongLength));
				if ((options & HexFindOptions.NoOverlaps) != 0)
					pos = result.Value + (ulong)lowerBytes.LongLength;
				else {
					// We must return all possible matches. If we search for aa and data is
					// aaaa, we must return positions 0, 1, 2, and not 0, 2.
					pos = result.Value + 1;
				}
			}
		}

		HexPosition? FindCore(SearchState state, HexPosition start, HexPosition upperBounds) {
			var charLengthsLocal = charLengths;
			var lowerBytesLocal = lowerBytes;
			var upperBytesLocal = upperBytes;
			var pos = start;
			if (pos + lowerBytesLocal.LongLength > upperBounds)
				return null;
			var endPos = upperBounds - lowerBytesLocal.LongLength + 1;
			state.SetPosition(pos);
			var lowerBytesLocal0 = lowerBytesLocal[0];
			var upperBytesLocal0 = upperBytesLocal[0];
			var lowerBytesLocal1 = lowerBytesLocal.Length <= 1 ? 0 : lowerBytesLocal[1];
			var upperBytesLocal1 = upperBytesLocal.Length <= 1 ? 0 : upperBytesLocal[1];
			var lowerBytesLocalLengthIsAtLeast2 = lowerBytesLocal.Length >= 2;
loop:
			// This loop doesn't check the cancellation token because SearchState does that
			// every time it reads new memory from the buffer.
			if (pos >= endPos)
				return null;
			int skip;
			HexPosition? afterPos;
			if (lowerBytesLocalLengthIsAtLeast2) {
				skip = 2;
				afterPos = state.PositionAfter2(lowerBytesLocal0, upperBytesLocal0, lowerBytesLocal1, upperBytesLocal1, endPos);
			}
			else {
				skip = 1;
				afterPos = state.PositionAfter1(lowerBytesLocal0, upperBytesLocal0, endPos);
			}
			if (afterPos is null)
				return null;
			pos = afterPos.Value - skip;
			state.SetPosition(pos);

			for (int i = 0, bi = 0; i < charLengthsLocal.Length; i++) {
				int charByteLen = charLengthsLocal[i];
				bool upperMatch = true;
				bool lowerMatch = true;
				for (int j = 0; j < charByteLen; j++, bi++) {
					var b = state.GetNextByte();
					upperMatch &= b == upperBytesLocal[bi];
					lowerMatch &= b == lowerBytesLocal[bi];
				}
				if (!upperMatch && !lowerMatch) {
					pos = pos + 1;
					state.SetPosition(pos);
					goto loop;
				}
			}
			return pos;
		}

		IEnumerable<HexBufferSpan> FindAllCoreReverse(SearchState state, HexBufferSpan searchRange, HexBufferPoint startingPosition, HexFindOptions options, CancellationToken cancellationToken) {
			HexBufferSpan? firstBlockResult = null;
			foreach (var span in GetValidSpansReverse(startingPosition.Buffer, startingPosition, searchRange.Start)) {
				cancellationToken.ThrowIfCancellationRequested();
				foreach (var span2 in FindAllCoreReverse(state, span, options)) {
					if (firstBlockResult is null)
						firstBlockResult = span2;
					yield return span2;
				}
			}

			if ((options & HexFindOptions.Wrap) != 0) {
				var lowerBounds = startingPosition.Position >= lowerBytes.LongLength - 1 ?
					startingPosition.Position - (lowerBytes.LongLength - 1) :
					HexPosition.Zero;
				if (lowerBounds < searchRange.Span.Start)
					lowerBounds = searchRange.Span.Start;
				if ((options & HexFindOptions.NoOverlaps) != 0 && !(firstBlockResult is null) && lowerBounds < firstBlockResult.Value.End)
					lowerBounds = firstBlockResult.Value.End;
				foreach (var span in GetValidSpansReverse(startingPosition.Buffer, searchRange.End - 1, lowerBounds)) {
					cancellationToken.ThrowIfCancellationRequested();
					foreach (var span2 in FindAllCoreReverse(state, span, options))
						yield return span2;
				}
			}
		}

		IEnumerable<HexBufferSpan> FindAllCoreReverse(SearchState state, HexSpan span, HexFindOptions options) {
			if (span.Length < lowerBytes.LongLength)
				yield break;
			var lowerBounds = span.Start + lowerBytes.LongLength - 1;
			var pos = span.End - 1;
			while (pos >= lowerBounds) {
				state.CancellationToken.ThrowIfCancellationRequested();
				var result = FindCoreReverse(state, pos, lowerBounds);
				if (result is null)
					break;
				yield return new HexBufferSpan(state.Buffer, new HexSpan(result.Value, (ulong)lowerBytes.LongLength));
				if ((options & HexFindOptions.NoOverlaps) != 0) {
					if (result.Value == HexPosition.Zero)
						break;
					pos = result.Value - 1;
				}
				else {
					// We must return all possible matches. If we search for aa and data is
					// aaaa, we must return positions 3, 2, 1, and not 3, 1
					pos = result.Value + (lowerBytes.LongLength - 1);
					if (pos == HexPosition.Zero)
						break;
					pos = pos - 1;
				}
			}
		}

		HexPosition? FindCoreReverse(SearchState state, HexPosition start, HexPosition lowerBounds) {
			var charLengthsLocal = charLengths;
			var lowerBytesLocal = lowerBytes;
			var upperBytesLocal = upperBytes;
			var pos = start;
			state.SetPreviousPosition(pos);
			var lowerBytesLocal0 = lowerBytesLocal[lowerBytesLocal.Length - 1];
			var upperBytesLocal0 = upperBytesLocal[upperBytesLocal.Length - 1];
			var lowerBytesLocal1 = lowerBytesLocal.Length <= 1 ? 0 : lowerBytesLocal[lowerBytesLocal.Length - 2];
			var upperBytesLocal1 = upperBytesLocal.Length <= 1 ? 0 : upperBytesLocal[upperBytesLocal.Length - 2];
			var lowerBytesLocalLengthIsAtLeast2 = lowerBytesLocal.Length >= 2;
loop:
			// This loop doesn't check the cancellation token because SearchState does that
			// every time it reads new memory from the buffer.
			if (pos < lowerBounds)
				return null;
			int skip;
			HexPosition? beforePos;
			if (lowerBytesLocalLengthIsAtLeast2) {
				skip = 2;
				beforePos = state.PositionBefore2(lowerBytesLocal0, upperBytesLocal0, lowerBytesLocal1, upperBytesLocal1, lowerBounds);
			}
			else {
				skip = 1;
				beforePos = state.PositionBefore1(lowerBytesLocal0, upperBytesLocal0, lowerBounds);
			}
			if (beforePos is null)
				return null;
			pos = beforePos.Value + skip;
			state.SetPreviousPosition(pos);

			for (int i = charLengthsLocal.Length - 1, bi = lowerBytesLocal.Length - 1; i >= 0; i--) {
				int charByteLen = charLengthsLocal[i];
				bool upperMatch = true;
				bool lowerMatch = true;
				for (int j = 0; j < charByteLen; j++, bi--) {
					var b = state.GetPreviousByte();
					upperMatch &= b == upperBytesLocal[bi];
					lowerMatch &= b == lowerBytesLocal[bi];
				}
				if (!upperMatch && !lowerMatch) {
					// pos can't be 0 here since skip != 0, see above
					pos = pos - 1;
					state.SetPreviousPosition(pos);
					goto loop;
				}
			}
			return pos - (lowerBytesLocal.LongLength - 1);
		}
	}
}
