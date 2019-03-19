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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text {
	static class TextImageUtils {
		const uint OFFSET_MASK = 0x3FFFFFFF;
		const int LINEBREAK_SHIFT = 30;

		public static int GetLineNumberFromPosition(uint[] lineOffsets, int position, int length) {
			if (position == length)
				return lineOffsets.Length - 1;

			int lo = 0, hi = lineOffsets.Length - 1;
			while (lo <= hi) {
				int lineNo = (lo + hi) / 2;

				int start = (int)(lineOffsets[lineNo] & TextImageUtils.OFFSET_MASK);
				int end = lineNo + 1 < lineOffsets.Length ? (int)(lineOffsets[lineNo + 1] & TextImageUtils.OFFSET_MASK) : length;

				if (position < start)
					hi = lineNo - 1;
				else if (position >= end)
					lo = lineNo + 1;
				else
					return lineNo;
			}

			throw new ArgumentOutOfRangeException(nameof(position));
		}

		public static void GetLineInfo(uint[] lineOffsets, int lineNumber, int length, out int start, out int end, out int lineBreakLength) {
			if ((uint)lineNumber >= (uint)lineOffsets.Length)
				throw new ArgumentOutOfRangeException(nameof(lineNumber));
			start = (int)(lineOffsets[lineNumber] & TextImageUtils.OFFSET_MASK);
			lineBreakLength = (int)(lineOffsets[lineNumber] >> TextImageUtils.LINEBREAK_SHIFT);
			end = (lineNumber + 1 < lineOffsets.Length ? (int)(lineOffsets[lineNumber + 1] & TextImageUtils.OFFSET_MASK) : length) - lineBreakLength;
		}

		public static uint[] CreateLineOffsets(ITextImage textImage) {
			var buffer = Cache.GetReadBuffer();
			var builder = Cache.GetOffsetBuilder();
			int pos = 0;
			int endPos = textImage.Length;
			bool lastCharWasCR = false;
			int linePos = pos;
			int lineLen = 0;
			while (pos < endPos) {
				int bufLen = buffer.Length;
				if (bufLen > endPos - pos)
					bufLen = endPos - pos;
				textImage.CopyTo(pos, buffer, 0, bufLen);
				pos += bufLen;
				int bufPos = 0;

				if (lastCharWasCR) {
					var c = buffer[0];
					if (c == '\n') {
						builder.Add((uint)((2 << LINEBREAK_SHIFT) | linePos));
						linePos += lineLen + 2;
						bufPos++;
					}
					else {
						builder.Add((uint)((1 << LINEBREAK_SHIFT) | linePos));
						linePos += lineLen + 1;
					}
					lineLen = 0;
					lastCharWasCR = false;
				}

				for (; bufPos < bufLen;) {
					int lineBreakSize;
					char c = buffer[bufPos++];
					if (c != '\r' && c != '\n' && c != '\u0085' && c != '\u2028' && c != '\u2029') {
						lineLen++;
						continue;
					}
					if (c == '\r') {
						if (bufPos == bufLen) {
							lastCharWasCR = true;
							break;
						}
						if (buffer[bufPos] == '\n') {
							lineBreakSize = 2;
							bufPos++;
						}
						else
							lineBreakSize = 1;
					}
					else
						lineBreakSize = 1;
					builder.Add((uint)((lineBreakSize << LINEBREAK_SHIFT) | linePos));
					linePos += lineLen + lineBreakSize;
					lineLen = 0;
					lastCharWasCR = false;
				}
			}
			Debug.Assert(pos == endPos);
			if (lastCharWasCR) {
				const int lineBreakSize = 1;
				builder.Add((uint)((lineBreakSize << LINEBREAK_SHIFT) | linePos));
				linePos += lineLen + lineBreakSize;
				lineLen = 0;
			}
			{
				const int lineBreakSize = 0;
				builder.Add((uint)((lineBreakSize << LINEBREAK_SHIFT) | linePos));
				linePos += lineLen + lineBreakSize;
			}
			Debug.Assert(linePos == endPos);

			Cache.FreeReadBuffer(buffer);
			Debug.Assert(builder.Count > 0);
			return Cache.FreeOffsetBuilder(builder);
		}

		static class Cache {
			public static void FreeReadBuffer(char[] buffer) => Interlocked.Exchange(ref __readBuffer, buffer);
			public static char[] GetReadBuffer() => Interlocked.Exchange(ref __readBuffer, null) ?? new char[BUF_LENGTH];
			static char[] __readBuffer;
			const int BUF_LENGTH = 4096;

			public static List<uint> GetOffsetBuilder() {
				var weakRef = Interlocked.Exchange(ref __offsetBuilderWeakRef, null);
				return weakRef?.Target as List<uint> ?? new List<uint>();
			}
			public static uint[] FreeOffsetBuilder(List<uint> list) {
				var res = list.ToArray();
				list.Clear();
				Interlocked.Exchange(ref __offsetBuilderWeakRef, new WeakReference(list));
				return res;
			}
			static WeakReference __offsetBuilderWeakRef;
		}
	}
}
