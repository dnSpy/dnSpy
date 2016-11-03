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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text.Tagging.Xml {
	enum XamlKind {
		Delimiter,
		Class,
		ParameterName,
		ParameterValue,
	}

	struct XamlSpan {
		public SnapshotSpan Span { get; }
		public XamlKind Kind { get; }
		public XamlSpan(SnapshotSpan span, XamlKind kind) {
			Span = span;
			Kind = kind;
		}
	}

	sealed class XamlAttributeValueClassifier {
		// Absolute minimum is "{x}", but most likely it's longer but we can't assume that
		const int MIN_STRING_LENGTH = 3;

		readonly char[] buffer;
		int bufferLen;
		int bufferPos;
		int snapshotPos;
		ITextSnapshot snapshot;
		int spanStart;
		int spanEnd;

		enum TokenKind {
			Name,
			Delimiter,
			Colon,
			EqualsSign,
			OpenCurlyBrace,
			Period,
		}

		struct CharSpan {
			public Span Span { get; }
			public TokenKind Kind { get; }
			public CharSpan(int start, int end, TokenKind kind) {
				Span = Span.FromBounds(start, end);
				Kind = kind;
			}
		}

		// Used for Name or Namespace : Name
		struct CharSpan3 {
			public CharSpan Item1 { get; }
			public CharSpan? Item2 { get; }
			public CharSpan? Item3 { get; }
			public CharSpan3(CharSpan item1, CharSpan? item2 = null, CharSpan? item3 = null) {
				Item1 = item1;
				Item2 = item2;
				Item3 = item3;
			}
		}

		public XamlAttributeValueClassifier() {
			this.buffer = new char[512];
		}

		public bool Initialize(SnapshotSpan span) {
			// Quick check that will filter out many small strings
			if (span.Length < MIN_STRING_LENGTH)
				return false;

			// Don't allow spaces at the beginning of the string. Perhaps that's allowed by the
			// XAML parser, but at least VS' classifier has the same check (or optimization)
			if (span.Start.GetChar() != '{')
				return false;
			// Check for {} sequence which, if present, says that what follows is just normal characters
			if ((span.Start + 1).GetChar() == '}')
				return false;

			bufferLen = 0;
			bufferPos = 0;
			spanStart = span.Start.Position;
			spanEnd = span.End.Position;
			snapshotPos = spanStart + 1;
			snapshot = span.Snapshot;
			return true;
		}

		public IEnumerable<XamlSpan> GetTags() {
			// This is the first { delimiter
			yield return new XamlSpan(new SnapshotSpan(snapshot, spanStart, 1), XamlKind.Delimiter);

			bool readingExtensionClass = true;
			for (;;) {
				var cspan = GetNextSpan();
				if (cspan == null)
					break;
				switch (cspan.Value.Kind) {
				case TokenKind.OpenCurlyBrace:
					readingExtensionClass = true;
					goto case TokenKind.Delimiter;
				case TokenKind.Colon:
				case TokenKind.EqualsSign:
				case TokenKind.Delimiter:
				case TokenKind.Period:
					yield return new XamlSpan(new SnapshotSpan(snapshot, cspan.Value.Span), XamlKind.Delimiter);
					break;

				case TokenKind.Name:
					var name = GetNextName(cspan.Value);
					if (readingExtensionClass) {
						yield return new XamlSpan(new SnapshotSpan(snapshot, name.Item1.Span), XamlKind.Class);
						if (name.Item2 != null)
							yield return new XamlSpan(new SnapshotSpan(snapshot, name.Item2.Value.Span), XamlKind.Delimiter);
						if (name.Item3 != null)
							yield return new XamlSpan(new SnapshotSpan(snapshot, name.Item3.Value.Span), XamlKind.Class);
						readingExtensionClass = false;
					}
					else {
						yield return new XamlSpan(new SnapshotSpan(snapshot, name.Item1.Span), XamlKind.ParameterName);
						if (name.Item2 != null)
							yield return new XamlSpan(new SnapshotSpan(snapshot, name.Item2.Value.Span), XamlKind.Delimiter);
						if (name.Item3 != null)
							yield return new XamlSpan(new SnapshotSpan(snapshot, name.Item3.Value.Span), XamlKind.ParameterName);

						var next = GetNextSpan();
						if (next == null)
							break;
						if (next.Value.Kind != TokenKind.EqualsSign) {
							Undo(next.Value);
							break;
						}
						yield return new XamlSpan(new SnapshotSpan(snapshot, next.Value.Span), XamlKind.Delimiter);

						for (;;) {
							next = GetNextSpan();
							if (next == null)
								break;
							if (next.Value.Kind == TokenKind.Period)
								yield return new XamlSpan(new SnapshotSpan(snapshot, next.Value.Span), XamlKind.Delimiter);
							else if (next.Value.Kind == TokenKind.Name) {
								name = GetNextName(next.Value);

								yield return new XamlSpan(new SnapshotSpan(snapshot, name.Item1.Span), XamlKind.ParameterValue);
								if (name.Item2 != null)
									yield return new XamlSpan(new SnapshotSpan(snapshot, name.Item2.Value.Span), XamlKind.Delimiter);
								if (name.Item3 != null)
									yield return new XamlSpan(new SnapshotSpan(snapshot, name.Item3.Value.Span), XamlKind.ParameterValue);
							}
							else {
								Undo(next.Value);
								break;
							}
						}
					}
					break;

				default:
					throw new InvalidOperationException();
				}
			}

			snapshot = null;
		}

		CharSpan3 GetNextName(CharSpan item1) {
			Debug.Assert(item1.Kind == TokenKind.Name);
			var item2 = GetNextSpan();
			if (item2 == null || item2.Value.Span.Length != 1 || item2.Value.Kind != TokenKind.Colon) {
				if (item2 != null)
					Undo(item2.Value);
				return new CharSpan3(item1);
			}
			var item3 = GetNextSpan();
			if (item3 == null || item3.Value.Kind != TokenKind.Name) {
				if (item3 != null)
					Undo(item3.Value);
				return new CharSpan3(item1, item2);
			}
			return new CharSpan3(item1, item2, item3);
		}

		void Undo(CharSpan charSpan) {
			Debug.Assert(nextCharSpan == null);
			if (nextCharSpan != null)
				throw new InvalidOperationException();
			nextCharSpan = charSpan;
		}
		CharSpan? nextCharSpan;

		CharSpan? GetNextSpan() {
			if (nextCharSpan != null) {
				var res = nextCharSpan;
				nextCharSpan = null;
				return res;
			}

			SkipWhitespace();
			int startPos = snapshotPos;
			int c = NextChar();
			if (c < 0)
				return null;

			if (IsPeriod((char)c))
				return new CharSpan(startPos, snapshotPos, TokenKind.Period);
			if (IsColon((char)c))
				return new CharSpan(startPos, snapshotPos, TokenKind.Colon);
			if (IsEqualsSign((char)c))
				return new CharSpan(startPos, snapshotPos, TokenKind.EqualsSign);
			if (IsOpenCurlyBrace((char)c))
				return new CharSpan(startPos, snapshotPos, TokenKind.OpenCurlyBrace);
			if (IsIdChar((char)c)) {
				SkipId();
				return new CharSpan(startPos, snapshotPos, TokenKind.Name);
			}
			Debug.Assert(IsDelimiter((char)c));
			SkipDelimiter();
			return new CharSpan(startPos, snapshotPos, TokenKind.Delimiter);
		}

		bool IsPeriod(char c) => c == '.';
		bool IsColon(char c) => c == ':';
		bool IsEqualsSign(char c) => c == '=';
		bool IsOpenCurlyBrace(char c) => c == '{';
		bool IsIdChar(char c) => char.IsLetterOrDigit(c) || c == '_';
		bool IsDelimiter(char c) => !char.IsWhiteSpace(c) && !IsIdChar(c) && !IsPeriod(c) && !IsColon(c) && !IsEqualsSign(c) && !IsOpenCurlyBrace(c);

		void SkipDelimiter() {
			for (;;) {
				int c = PeekChar();
				if (c < 0 || !IsDelimiter((char)c))
					break;
				SkipChar();
			}
		}

		void SkipId() {
			for (;;) {
				int c = PeekChar();
				if (c < 0 || !IsIdChar((char)c))
					break;
				SkipChar();
			}
		}

		void SkipWhitespace() {
			for (;;) {
				int c = PeekChar();
				if (c < 0 || !char.IsWhiteSpace((char)c))
					break;
				SkipChar();
			}
		}

		int NextChar() {
			if (bufferPos >= bufferLen) {
				int len = spanEnd - snapshotPos;
				if (len == 0)
					return -1;
				if (len > buffer.Length)
					len = buffer.Length;
				snapshot.CopyTo(snapshotPos, buffer, 0, len);
				bufferLen = len;
				bufferPos = 0;
			}
			snapshotPos++;
			return buffer[bufferPos++];
		}

		int PeekChar() {
			if (bufferPos >= bufferLen) {
				int len = spanEnd - snapshotPos;
				if (len == 0)
					return -1;
				if (len > buffer.Length)
					len = buffer.Length;
				snapshot.CopyTo(snapshotPos, buffer, 0, len);
				bufferLen = len;
				bufferPos = 0;
			}
			return buffer[bufferPos];
		}

		void SkipChar() {
			Debug.Assert(snapshotPos < spanEnd);
			Debug.Assert(bufferPos < bufferLen);
			snapshotPos++;
			bufferPos++;
		}
	}
}
