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
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Operations;

namespace dnSpy.Hex.Operations {
	sealed class ByteHexSearchService : HexSearchServiceImpl {
		readonly byte[] pattern;
		readonly byte[] mask;

		public override int ByteCount => pattern.Length;

		public ByteHexSearchService(byte[] pattern, byte[] mask) {
			if (pattern == null)
				throw new ArgumentNullException(nameof(pattern));
			if (mask == null)
				throw new ArgumentNullException(nameof(mask));
			if (pattern.Length != mask.Length)
				throw new ArgumentOutOfRangeException(nameof(mask));
			if (pattern.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(pattern));
			this.pattern = pattern;
			this.mask = mask;
		}

		public override IEnumerable<HexBufferSpan> FindAll(HexBufferSpan searchRange, HexBufferPoint startingPosition, HexFindOptions options, CancellationToken cancellationToken) {
			if (searchRange.IsDefault)
				throw new ArgumentException();
			if (searchRange.Buffer != startingPosition.Buffer)
				throw new ArgumentException();
			if (startingPosition < searchRange.Start || startingPosition > searchRange.End)
				throw new ArgumentException();
			if (searchRange.IsEmpty)
				return Array.Empty<HexBufferSpan>();
			if ((options & HexFindOptions.SearchReverse) != 0)
				return FindAllCoreReverse(searchRange, startingPosition, options, cancellationToken);
			return FindAllCore(searchRange, startingPosition, options, cancellationToken);
		}

		sealed class SearchState : SearchStateBase {
			public SearchState(HexBuffer buffer, CancellationToken cancellationToken)
				: base(buffer, cancellationToken) {
			}

			public HexPosition? PositionAfter1(int value, HexPosition end) {
				if (end < dataPosition)
					return null;
				var dataLocal = Data;
				for (;;) {
					if (dataIndex >= dataLength)
						FillNextData();
					int len = (int)HexPosition.Min(end - (dataPosition + dataIndex), dataLength - dataIndex).ToUInt64();
					int index = Array.IndexOf(dataLocal, (byte)value, dataIndex, len);
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

			public HexPosition? PositionAfter2(int value1, int value2, int mask2, HexPosition end) {
				if (end < dataPosition)
					return null;
				HexPosition newPos;
				var dataLocal = Data;
				for (;;) {
					if (dataIndex >= dataLength)
						FillNextData();
					int len = (int)HexPosition.Min(end - (dataPosition + dataIndex), dataLength - dataIndex).ToUInt64();
					int index = Array.IndexOf(dataLocal, (byte)value1, dataIndex, len);
					if (index >= 0) {
						index++;
						dataIndex = index;
						if (index >= dataLength) {
							newPos = dataPosition + dataLength;
							if (newPos >= end)
								return null;
							FillNextData();
							index = 0;
						}
						if ((dataLocal[index] & mask2) == value2) {
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

			// Code is identical to PositionAfter1() with an inlined IndexOf() that supports masks
			public HexPosition? PositionAfterWithMask1(int value, int mask, HexPosition end) {
				Debug.Assert(mask != 0xFF, "Use the other method instead");
				if (end < dataPosition)
					return null;
				var dataLocal = Data;
				for (;;) {
					if (dataIndex >= dataLength)
						FillNextData();
					int len = (int)HexPosition.Min(end - (dataPosition + dataIndex), dataLength - dataIndex).ToUInt64();
					// Our Array.IndexOf():
					int index = -1;
					int dataEnd = dataIndex + len;
					for (int i = dataIndex; i < dataEnd; i++) {
						if ((dataLocal[i] & mask) == value) {
							index = i;
							break;
						}
					}
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

			// Code is identical to PositionAfter2() with an inlined IndexOf() that supports masks
			public HexPosition? PositionAfterWithMask2(int value1, int value2, int mask1, int mask2, HexPosition end) {
				Debug.Assert(mask1 != 0xFF || mask2 != 0xFF, "Use the other method instead");
				if (end < dataPosition)
					return null;
				HexPosition newPos;
				var dataLocal = Data;
				for (;;) {
					if (dataIndex >= dataLength)
						FillNextData();
					int len = (int)HexPosition.Min(end - (dataPosition + dataIndex), dataLength - dataIndex).ToUInt64();
					// Our Array.IndexOf():
					int index = -1;
					int dataEnd = dataIndex + len;
					for (int i = dataIndex; i < dataEnd; i++) {
						if ((dataLocal[i] & mask1) == value1) {
							index = i;
							break;
						}
					}
					if (index >= 0) {
						index++;
						dataIndex = index;
						if (index >= dataLength) {
							newPos = dataPosition + dataLength;
							if (newPos >= end)
								return null;
							FillNextData();
							index = 0;
						}
						if ((dataLocal[index] & mask2) == value2) {
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

			public HexPosition? PositionBefore1(int value, HexPosition lowerBounds) {
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
					int index = Array.LastIndexOf(dataLocal, (byte)value, dataIndex, len);
					if (index >= 0) {
						dataIndex = index - 1;
						return dataPosition + (index - 1);
					}
					if (dataPosition == HexPosition.Zero)
						return null;
					dataIndex = -1;
				}
			}

			public HexPosition? PositionBefore2(int value1, int value2, int mask2, HexPosition lowerBounds) {
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
					int index = Array.LastIndexOf(dataLocal, (byte)value1, dataIndex, len);
					if (index >= 0) {
						index--;
						dataIndex = index;
						if (dataIndex < 0) {
							if (dataPosition <= lowerBounds)
								return null;
							FillPreviousData();
							index = dataIndex;
						}
						if ((dataLocal[index] & mask2) == value2) {
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

			// Code is identical to PositionBefore1() with an inlined LastIndexOf() that supports masks
			public HexPosition? PositionBeforeWithMask1(int value, int mask, HexPosition lowerBounds) {
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
					// Our Array.LastIndexOf():
					int index = -1;
					int dataEnd = dataIndex + 1 - len;
					for (int i = dataIndex; i >= dataEnd; i--) {
						if ((dataLocal[i] & mask) == value) {
							index = i;
							break;
						}
					}
					if (index >= 0) {
						dataIndex = index - 1;
						return dataPosition + (index - 1);
					}
					if (dataPosition == HexPosition.Zero)
						return null;
					dataIndex = -1;
				}
			}

			// Code is identical to PositionBefore2() with an inlined LastIndexOf() that supports masks
			public HexPosition? PositionBeforeWithMask2(int value1, int value2, int mask1, int mask2, HexPosition lowerBounds) {
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
					// Our Array.LastIndexOf():
					int index = -1;
					int dataEnd = dataIndex + 1 - len;
					for (int i = dataIndex; i >= dataEnd; i--) {
						if ((dataLocal[i] & mask1) == value1) {
							index = i;
							break;
						}
					}
					if (index >= 0) {
						index--;
						dataIndex = index;
						if (dataIndex < 0) {
							if (dataPosition <= lowerBounds)
								return null;
							FillPreviousData();
							index = dataIndex;
						}
						if ((dataLocal[index] & mask2) == value2) {
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

		IEnumerable<HexBufferSpan> FindAllCore(HexBufferSpan searchRange, HexBufferPoint startingPosition, HexFindOptions options, CancellationToken cancellationToken) {
			var state = new SearchState(searchRange.Buffer, cancellationToken);
			foreach (var span in GetValidSpans(startingPosition.Buffer, startingPosition, searchRange.End)) {
				cancellationToken.ThrowIfCancellationRequested();
				foreach (var span2 in FindAllCore(state, span))
					yield return span2;
			}

			if ((options & HexFindOptions.Wrap) != 0) {
				var upperBounds = HexPosition.Min(searchRange.Span.End, startingPosition.Position + pattern.LongLength - 1);
				foreach (var span in GetValidSpans(startingPosition.Buffer, searchRange.Start, upperBounds)) {
					cancellationToken.ThrowIfCancellationRequested();
					foreach (var span2 in FindAllCore(state, span))
						yield return span2;
				}
			}
		}

		IEnumerable<HexBufferSpan> FindAllCore(SearchState state, HexSpan span) {
			var pos = span.Start;
			if (pos + pattern.LongLength > span.End)
				yield break;
			var endPos = span.End - pattern.LongLength + 1;
			while (pos < endPos) {
				state.CancellationToken.ThrowIfCancellationRequested();
				var result = FindCore(state, pos, span.End);
				if (result == null)
					break;
				yield return new HexBufferSpan(state.Buffer, new HexSpan(result.Value, (ulong)pattern.LongLength));
				// We must return all possible matches. If we search for 1111 and data is
				// 11111111, we must return positions 0, 1, 2, and not 0, 2.
				pos = result.Value + 1;
			}
		}

		HexPosition? FindCore(SearchState state, HexPosition start, HexPosition upperBounds) {
			var patternLocal = pattern;
			var maskLocal = mask;
			var pos = start;
			if (pos + patternLocal.LongLength > upperBounds)
				return null;
			var endPos = upperBounds - patternLocal.LongLength + 1;
			state.SetPosition(pos);
			var patternLocal0 = patternLocal[0];
			var maskLocal0 = maskLocal[0];
			var patternLocal1 = patternLocal.Length <= 1 ? 0 : patternLocal[1];
			var maskLocal1 = maskLocal.Length <= 1 ? 0 : maskLocal[1];
			var maskLocalLengthIsAtLeast2 = maskLocal.Length >= 2;
loop:
			// This loop doesn't check the cancellation token because SearchState does that
			// every time it reads new memory from the buffer.
			if (pos >= endPos)
				return null;
			int skip;
			HexPosition? afterPos;
			if (maskLocalLengthIsAtLeast2) {
				skip = 2;
				afterPos = maskLocal0 == 0xFF ?
						state.PositionAfter2(patternLocal0, patternLocal1, maskLocal1, endPos) :
						state.PositionAfterWithMask2(patternLocal0, patternLocal1, maskLocal0, maskLocal1, endPos);
			}
			else if (maskLocal0 == 0xFF) {
				skip = 1;
				afterPos = state.PositionAfter1(patternLocal0, endPos);
			}
			else {
				skip = 1;
				afterPos = state.PositionAfterWithMask1(patternLocal0, maskLocal0, endPos);
			}
			if (afterPos == null)
				return null;
			pos = afterPos.Value;

			for (int i = skip; i < patternLocal.Length; i++) {
				var b = state.GetNextByte();
				var m = maskLocal[i];
				if ((b & m) != patternLocal[i]) {
					pos = pos - (skip - 1);
					state.SetPosition(pos);
					goto loop;
				}
			}
			return pos - skip;
		}

		IEnumerable<HexBufferSpan> FindAllCoreReverse(HexBufferSpan searchRange, HexBufferPoint startingPosition, HexFindOptions options, CancellationToken cancellationToken) {
			var state = new SearchState(searchRange.Buffer, cancellationToken);
			foreach (var span in GetValidSpansReverse(startingPosition.Buffer, startingPosition, searchRange.Start)) {
				cancellationToken.ThrowIfCancellationRequested();
				foreach (var span2 in FindAllCoreReverse(state, span))
					yield return span2;
			}

			if ((options & HexFindOptions.Wrap) != 0) {
				var lowerBounds = startingPosition.Position >= pattern.LongLength - 1 ?
					startingPosition.Position - (pattern.LongLength - 1) :
					HexPosition.Zero;
				if (lowerBounds < searchRange.Span.Start)
					lowerBounds = searchRange.Span.Start;
				foreach (var span in GetValidSpansReverse(startingPosition.Buffer, searchRange.End - 1, lowerBounds)) {
					cancellationToken.ThrowIfCancellationRequested();
					foreach (var span2 in FindAllCoreReverse(state, span))
						yield return span2;
				}
			}
		}

		IEnumerable<HexBufferSpan> FindAllCoreReverse(SearchState state, HexSpan span) {
			if (span.Length < pattern.LongLength)
				yield break;
			var lowerBounds = span.Start + pattern.LongLength - 1;
			var pos = span.End - 1;
			while (pos >= lowerBounds) {
				state.CancellationToken.ThrowIfCancellationRequested();
				var result = FindCoreReverse(state, pos, lowerBounds);
				if (result == null)
					break;
				yield return new HexBufferSpan(state.Buffer, new HexSpan(result.Value, (ulong)pattern.LongLength));
				// We must return all possible matches. If we search for 1111 and data is
				// 11111111, we must return positions 3, 2, 1, and not 3, 1
				pos = result.Value + (pattern.LongLength - 1);
			}
		}

		HexPosition? FindCoreReverse(SearchState state, HexPosition start, HexPosition lowerBounds) {
			var patternLocal = pattern;
			var maskLocal = mask;
			var pos = start;
			state.SetPreviousPosition(pos);
			var patternLocal0 = patternLocal[patternLocal.Length - 1];
			var maskLocal0 = maskLocal[maskLocal.Length - 1];
			var patternLocal1 = patternLocal.Length <= 1 ? 0 : patternLocal[patternLocal.Length - 2];
			var maskLocal1 = maskLocal.Length <= 1 ? 0 : maskLocal[maskLocal.Length - 2];
			var maskLocalLengthIsAtLeast2 = maskLocal.Length >= 2;
loop:
			// This loop doesn't check the cancellation token because SearchState does that
			// every time it reads new memory from the buffer.
			if (pos < lowerBounds)
				return null;
			int skip;
			HexPosition? beforePos;
			if (maskLocalLengthIsAtLeast2) {
				skip = 2;
				beforePos = maskLocal0 == 0xFF ?
						state.PositionBefore2(patternLocal0, patternLocal1, maskLocal1, lowerBounds) :
						state.PositionBeforeWithMask2(patternLocal0, patternLocal1, maskLocal0, maskLocal1, lowerBounds);
			}
			else if (maskLocal0 == 0xFF) {
				skip = 1;
				beforePos = state.PositionBefore1(patternLocal0, lowerBounds);
			}
			else {
				skip = 1;
				beforePos = state.PositionBeforeWithMask1(patternLocal0, maskLocal0, lowerBounds);
			}
			if (beforePos == null)
				return null;
			pos = beforePos.Value;

			for (int i = patternLocal.Length - 1 - skip; i >= 0; i--) {
				var b = state.GetPreviousByte();
				var m = maskLocal[i];
				if ((b & m) != patternLocal[i]) {
					pos = pos + (skip - 1);
					state.SetPreviousPosition(pos);
					goto loop;
				}
			}
			return pos - (patternLocal.LongLength - skip - 1);
		}
	}
}
