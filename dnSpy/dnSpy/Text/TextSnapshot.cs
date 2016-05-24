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
using System.IO;
using System.Threading;
using dnSpy.Contracts.Text;
using ICSharpCode.AvalonEdit.Document;

namespace dnSpy.Text {
	sealed class TextSnapshot : ITextSnapshot {
		public ITextSource TextSource => textSource;
		readonly ITextSource textSource;

		const uint OFFSET_MASK = 0x3FFFFFFF;
		const int LINEBREAK_SHIFT = 30;

		public char this[int position] => textSource.GetCharAt(position);
		public IContentType ContentType { get; }
		public int Length => textSource.TextLength;
		public ITextBuffer TextBuffer { get; }

		public TextSnapshot(ITextSource textSource, IContentType contentType, ITextBuffer textBuffer) {
			this.textSource = textSource;
			ContentType = contentType;
			TextBuffer = textBuffer;
		}

		public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => textSource.CopyTo(sourceIndex, destination, destinationIndex, count);
		public string GetText() => textSource.Text;
		public string GetText(Span span) => textSource.GetText(span.Start, span.Length);
		public string GetText(int startIndex, int length) => textSource.GetText(startIndex, length);
		public char[] ToCharArray(int startIndex, int length) => textSource.ToCharArray(startIndex, length);
		public void Write(TextWriter writer) => textSource.WriteTextTo(writer);
		public void Write(TextWriter writer, Span span) => textSource.WriteTextTo(writer, span.Start, span.Length);

		public ITextChange[] GetTextChangesFrom(TextSnapshot other) {
			var list = new List<ITextChange>();
			Debug.Assert(other.textSource.Version != null);
			Debug.Assert(textSource.Version != null);
			foreach (var tca in other.textSource.Version.GetChangesTo(textSource.Version))
				list.Add(new TextChange(tca.Offset, tca.RemovedText, tca.InsertedText));
			return list.ToArray();
		}

		public int LineCount {
			get {
				if (lineOffsets == null)
					lineOffsets = CreateLineOffsets();
				return lineOffsets.Length;
			}
		}

		public IEnumerable<ITextSnapshotLine> Lines {
			get {
				if (lineOffsets == null)
					lineOffsets = CreateLineOffsets();
				for (int lineNo = 0; lineNo < lineOffsets.Length; lineNo++)
					yield return GetLineFromLineNumber(lineNo);
			}
		}

		public ITextSnapshotLine GetLineFromLineNumber(int lineNumber) {
			if (lineOffsets == null)
				lineOffsets = CreateLineOffsets();
			if ((uint)lineNumber >= (uint)lineOffsets.Length)
				throw new ArgumentOutOfRangeException(nameof(lineNumber));
			int start = (int)(lineOffsets[lineNumber] & OFFSET_MASK);
			int lineBreakLength = (int)(lineOffsets[lineNumber] >> LINEBREAK_SHIFT);
			int end = (lineNumber + 1 < lineOffsets.Length ? (int)(lineOffsets[lineNumber + 1] & OFFSET_MASK) : Length) - lineBreakLength;
			return new TextSnapshotLine(this, lineNumber, start, end - start, lineBreakLength);
		}

		public ITextSnapshotLine GetLineFromPosition(int position) =>
			GetLineFromLineNumber(GetLineNumberFromPosition(position));

		public int GetLineNumberFromPosition(int position) {
			if ((uint)position > (uint)Length)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (lineOffsets == null)
				lineOffsets = CreateLineOffsets();
			if (position == Length)
				return lineOffsets.Length - 1;

			int lo = 0, hi = lineOffsets.Length - 1;
			while (lo <= hi && hi != -1) {
				int lineNo = (lo + hi) / 2;

				int start = (int)(lineOffsets[lineNo] & OFFSET_MASK);
				int end = lineNo + 1 < lineOffsets.Length ? (int)(lineOffsets[lineNo + 1] & OFFSET_MASK) : Length;

				if (position < start)
					hi = lineNo - 1;
				else if (position >= end)
					lo = lineNo + 1;
				else
					return lineNo;
			}

			throw new ArgumentOutOfRangeException(nameof(position));
		}

		uint[] lineOffsets;
		uint[] CreateLineOffsets() {
			var buffer = Cache.GetReadBuffer();
			var builder = Cache.GetOffsetBuilder();
			int pos = 0;
			int endPos = Length;
			bool lastCharWasCR = false;
			int linePos = pos;
			int lineLen = 0;
			while (pos < endPos) {
				int bufLen = buffer.Length;
				if (bufLen > endPos - pos)
					bufLen = endPos - pos;
				CopyTo(pos, buffer, 0, bufLen);
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
			if (lineLen != 0 || lastCharWasCR) {
				int lineBreakSize = lastCharWasCR ? 1 : 0;
				builder.Add((uint)((lineBreakSize << LINEBREAK_SHIFT) | linePos));
				linePos += lineLen + lineBreakSize;
			}
			Debug.Assert(linePos == endPos);
			if (endPos == 0)
				builder.Add(0);

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

		public override string ToString() => GetText();
	}
}
