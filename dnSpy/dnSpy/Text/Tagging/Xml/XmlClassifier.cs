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

//TODO: CDataSection, Keyword, ProcessingInstruction

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text.Tagging.Xml {
	enum XmlKind {
		/// <summary>
		/// Eg. &lt; or &amp;
		/// </summary>
		EntityReference,

		/// <summary>
		/// Text inside of elements
		/// </summary>
		Text,

		/// <summary>
		/// Text inside of elements that is pure whitespace
		/// </summary>
		TextWhitespace,

		/// <summary>
		/// Delimiter, eg. >
		/// </summary>
		Delimiter,

		/// <summary>
		/// Comment, eg. <!-- hello -->
		/// </summary>
		Comment,

		/// <summary>
		/// Whitespace inside of an element which separates attributes, attribute values, etc.
		/// </summary>
		ElementWhitespace,

		/// <summary>
		/// Name of element
		/// </summary>
		ElementName,

		/// <summary>
		/// Name of attribute
		/// </summary>
		AttributeName,

		/// <summary>
		/// Attribute value quote
		/// </summary>
		AttributeQuote,

		/// <summary>
		/// Attribute value (inside quotes)
		/// </summary>
		AttributeValue,
	}

	struct XmlSpanKind {
		public Span Span { get; }
		public XmlKind Kind { get; }

		public XmlSpanKind(Span span, XmlKind kind) {
			Span = span;
			Kind = kind;
		}
	}

	sealed class XmlClassifier {
		readonly ITextSnapshot snapshot;
		readonly int snapshotLength;
		readonly char[] buffer;
		int bufferLen;
		int bufferPos;
		int snapshotPos;
		State state;
		int spanStart;
		const int BUFFER_SIZE = 4096;

		enum State {
			// Initial state, look for elements, text
			Element,
			// Read element name
			ElementName,
			// Read attributes
			Attribute,
			// Read =
			AttributeEquals,
			// Read attribute quote
			AttributeQuoteStart,
			// Read attribute value
			AttributeValue,
			// Read attribute quote
			AttributeQuoteEnd,
		}

		public XmlClassifier(ITextSnapshot snapshot) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			this.snapshot = snapshot;
			this.snapshotLength = snapshot.Length;
			this.buffer = new char[Math.Min(BUFFER_SIZE, snapshot.Length)];
			this.state = State.Element;
		}

		public XmlSpanKind? GetNext() {
			for (;;) {
				var kind = GetNextCore();
				if (kind == null)
					break;
				Debug.Assert(spanStart != snapshotPos);
				if (spanStart == snapshotPos)
					break;
				return new XmlSpanKind(Span.FromBounds(spanStart, snapshotPos), kind.Value);
			}
			return null;
		}

		XmlKind? GetNextCore() {
			spanStart = snapshotPos;
			int c, pos;
			switch (state) {
			case State.Element:
				c = NextChar();
				if (c < 0)
					return null;
				switch ((char)c) {
				case '<':
					c = PeekChar();
					if (c < 0)
						return XmlKind.Delimiter;
					if (c == '/' || c == '?') {
						// </tag> or <?xml ... ?>
						SkipChar();
						state = State.ElementName;
						return XmlKind.Delimiter;
					}
					if (c != '!') {
						// <tag>
						state = State.ElementName;
						return XmlKind.Delimiter;
					}
					SkipChar();
					if (PeekChar() != '-') {
						// Error
						state = State.ElementName;
						return XmlKind.Delimiter;
					}
					SkipChar();
					if (PeekChar() != '-') {
						// Error
						state = State.ElementName;
						return XmlKind.Delimiter;
					}
					SkipChar();
					ReadComment();
					return XmlKind.Comment;

				case '&':
					ReadEntityReference();
					return XmlKind.EntityReference;

				default:
					return ReadWhitespaceOrText(c);
				}

			case State.ElementName:
				c = PeekChar();
				if (c < 0)
					return null;
				if (char.IsWhiteSpace((char)c)) {
					ReadElementWhitespace();
					return XmlKind.ElementWhitespace;
				}
				if (c == ':') {
					NextChar();
					return XmlKind.Delimiter;
				}
				if (c == '<' || c == '>') {
					// Error
					state = State.Element;
					goto case State.Element;
				}
				pos = snapshotPos;
				ReadName();
				if (pos == snapshotPos) {
					NextChar();
					return XmlKind.Delimiter;
				}
				if (PeekChar() != ':')
					state = State.Attribute;
				return XmlKind.ElementName;

			case State.Attribute:
				c = PeekChar();
				if (c < 0)
					return null;
				if (char.IsWhiteSpace((char)c)) {
					ReadElementWhitespace();
					return XmlKind.ElementWhitespace;
				}
				if (c == ':') {
					NextChar();
					return XmlKind.Delimiter;
				}
				if (c == '/') {
					SkipChar();
					if (PeekChar() == '>') {
						SkipChar();
						state = State.Element;
						return XmlKind.Delimiter;
					}
					return XmlKind.Delimiter;
				}
				if (c == '>') {
					SkipChar();
					state = State.Element;
					return XmlKind.Delimiter;
				}
				if (c == '<') {
					// Error
					state = State.Element;
					goto case State.Element;
				}
				pos = snapshotPos;
				ReadName();
				if (pos == snapshotPos) {
					NextChar();
					return XmlKind.Delimiter;
				}
				if (PeekChar() != ':')
					state = State.AttributeEquals;
				return XmlKind.AttributeName;

			case State.AttributeEquals:
				c = PeekChar();
				if (c < 0)
					return null;
				if (char.IsWhiteSpace((char)c)) {
					ReadElementWhitespace();
					return XmlKind.ElementWhitespace;
				}
				if (c != '=') {
					// Error
					state = State.Attribute;
					goto case State.Attribute;
				}
				SkipChar();
				state = State.AttributeQuoteStart;
				return XmlKind.Delimiter;

			case State.AttributeQuoteStart:
				c = PeekChar();
				if (c < 0)
					return null;
				if (char.IsWhiteSpace((char)c)) {
					ReadElementWhitespace();
					return XmlKind.ElementWhitespace;
				}
				if (c != '\'' && c != '"') {
					// Error
					state = State.Attribute;
					goto case State.Attribute;
				}
				isDoubleQuote = c == '"';
				SkipChar();
				state = State.AttributeValue;
				return XmlKind.AttributeQuote;

			case State.AttributeValue:
				c = PeekChar();
				if (c == (isDoubleQuote ? '"' : '\'')) {
					state = State.AttributeQuoteEnd;
					goto case State.AttributeQuoteEnd;
				}
				ReadString(isDoubleQuote);
				state = State.AttributeQuoteEnd;
				return XmlKind.AttributeValue;

			case State.AttributeQuoteEnd:
				c = NextChar();
				if (c < 0)
					return null;
				Debug.Assert(c == (isDoubleQuote ? '"' : '\''));
				state = State.Attribute;
				return XmlKind.AttributeQuote;

			default:
				throw new InvalidOperationException();
			}
		}
		bool isDoubleQuote;

		void ReadString(bool isDoubleQuote) {
			var quoteChar = isDoubleQuote ? '"' : '\'';
			for (;;) {
				int c = PeekChar();
				if (c < 0 || c == quoteChar)
					break;
				NextChar();
			}
		}

		void ReadName() {
			for (;;) {
				int c = PeekChar();
				if (c < 0)
					break;
				if (!IsNameChar((char)c))
					break;
				NextChar();
			}
		}

		bool IsNameChar(char c) =>
			char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.';

		void ReadElementWhitespace() {
			for (;;) {
				int c = PeekChar();
				if (c < 0)
					break;
				if (!char.IsWhiteSpace((char)c))
					break;
				NextChar();
			}
		}

		void ReadComment() {
			// We've already read <!--
			for (;;) {
				int c = NextChar();
				if (c < 0)
					break;
				if (c != '-')
					continue;

				c = NextChar();
				if (c < 0)
					break;
				if (c != '-')
					continue;

				c = NextChar();
				if (c < 0)
					break;
				if (c != '>')
					continue;

				break;
			}
		}

		XmlKind ReadWhitespaceOrText(int c) {
			bool isText = !char.IsWhiteSpace((char)c);
			while ((c = PeekChar()) >= 0) {
				if (!char.IsWhiteSpace((char)c)) {
					if (c == '&' || c == '<')
						break;
					isText = true;
				}
				SkipChar();
			}
			return isText ? XmlKind.Text : XmlKind.TextWhitespace;
		}

		void ReadEntityReference() {
			// We've already read &
			for (;;) {
				int c = PeekChar();
				if (c < 0)
					break;
				if (c == ';') {
					SkipChar();
					break;
				}
				if (!char.IsLetterOrDigit((char)c))
					break;
				SkipChar();
			}
		}

		int NextChar() {
			if (bufferPos >= bufferLen) {
				int len = snapshotLength - snapshotPos;
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
				int len = snapshotLength - snapshotPos;
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
			Debug.Assert(snapshotPos < snapshotLength);
			Debug.Assert(bufferPos < bufferLen);
			snapshotPos++;
			bufferPos++;
		}
	}
}
