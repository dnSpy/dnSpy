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
using dnSpy.Decompiler.Shared;

namespace dnSpy.Shared.Highlighting {
	public sealed class TextTokenInfo {
		public int Length {
			get { return currentOffset; }
		}

		// current write offset
		int currentOffset;
		// current offset of this token. Only gets updated when a new token is saved
		int currentTokenOffset;
		// current length of default text. Can't be incremented when isAppendingDefaultText == true
		int currentDefaultTextLength;
		// current length of token. Valid when isAppendingDefaultText == false
		int currentTokenLength;
		// current token kind. Valid when isAppendingDefaultText == false
		TextTokenKind currentTokenKind;
		bool isAppendingDefaultText = true;

		// Convert offset to an index into the token list
		Dictionary<int, uint> offsetToTokenInfoIndex = new Dictionary<int, uint>();

		static readonly char[] newLineChars = new char[] { '\r', '\n' };

		// Each token is usually surrounded by tokens of default color, and the default color
		// usually is the first token on each line. Merge this info into one struct. 32 bits is
		// enough. If a token or a default text token exceeds these lengths, it will be split up
		// into multiple of the following structs.
		//	[31:22] = token kind
		//	[21:16] = unused
		//	[15:8]  = token length
		//	[7:0]   = Text (default color) token length
		const int TOKEN_KIND_BITS = 10;
		const int TOKEN_KIND_BIT = 22;
		const int TOKEN_KIND_MAX = (1 << TOKEN_KIND_BITS) - 1;
		const int TOKEN_LENGTH_BITS = 8;
		const int TOKEN_LENGTH_BIT = 8;
		const int TOKEN_LENGTH_MAX = (1 << TOKEN_LENGTH_BITS) - 1;
		const int TEXT_TOKEN_LENGTH_BITS = 8;
		const int TEXT_TOKEN_LENGTH_BIT = 0;
		const int TEXT_TOKEN_LENGTH_MAX = (1 << TEXT_TOKEN_LENGTH_BITS) - 1;

		static TextTokenInfo() {
			if ((int)TextTokenKind.Last > (1 << TOKEN_KIND_BITS))
				throw new InvalidProgramException("TOKEN_KIND_BITS is too small");
		}

		public bool Find(int offset, out int defaultTextLength, out TextTokenKind tokenKind, out int tokenLength) {
			uint val;
			if (!offsetToTokenInfoIndex.TryGetValue(offset, out val)) {
				defaultTextLength = 0;
				tokenKind = TextTokenKind.Last;
				tokenLength = 0;
				return false;
			}

			defaultTextLength = (int)(val >> TEXT_TOKEN_LENGTH_BIT) & TEXT_TOKEN_LENGTH_MAX;
			tokenKind = (TextTokenKind)((val >> TOKEN_KIND_BIT) & TOKEN_KIND_MAX);
			tokenLength = (int)(val >> TOKEN_LENGTH_BIT) & TOKEN_LENGTH_MAX;
			return true;
		}

		public void Flush() {
			EndCurrentToken(0);
		}

		public void Finish() {
			Flush();
		}

		public void Append(TextTokenKind tokenKind, string s) {
			if (s == null)
				return;
			Append(tokenKind, s, 0, s.Length);
		}

		public void Append(TextTokenKind tokenKind, string s, int offset, int length) {
			if (s == null)
				return;

			// Newlines could be part of the input string
			int so = offset;
			int end = offset + length;
			while (so < end) {
				int nlOffs = s.IndexOfAny(newLineChars, so, end - so);
				if (nlOffs >= 0) {
					AppendInternal(tokenKind, nlOffs - so);
					so = nlOffs;
					int nlLen = s[so] == '\r' && so + 1 < end && s[so + 1] == '\n' ? 2 : 1;
					currentOffset += nlLen;
					EndCurrentToken(nlLen);
					so += nlLen;
				}
				else {
					AppendInternal(tokenKind, end - so);
					break;
				}
			}
		}

		public void AppendLine() {
			// We must append the same type of new line string as StringBuilder
			Append(TextTokenKind.Text, Environment.NewLine);
		}

		// Gets called to add one token. No newlines are allowed
		void AppendInternal(TextTokenKind tokenKind, int length) {
			Debug.Assert(length >= 0);
			if (length == 0)
				return;

redo:
			if (isAppendingDefaultText) {
				if (tokenKind == TextTokenKind.Text) {
					int newLength = currentDefaultTextLength + length;
					while (newLength > TEXT_TOKEN_LENGTH_MAX) {
						currentDefaultTextLength = Math.Min(newLength, TEXT_TOKEN_LENGTH_MAX);
						EndCurrentToken(0);
						newLength -= TEXT_TOKEN_LENGTH_MAX;
					}
					currentDefaultTextLength = newLength;
					currentOffset += length;
					return;
				}
				isAppendingDefaultText = false;
				currentTokenKind = tokenKind;
			}

			if (currentTokenKind != tokenKind) {
				EndCurrentToken(0);
				goto redo;
			}

			{
				int newLength = currentTokenLength + length;
				while (newLength > TOKEN_LENGTH_MAX) {
					currentTokenLength = Math.Min(newLength, TOKEN_LENGTH_MAX);
					EndCurrentToken(0);
					newLength -= TOKEN_LENGTH_MAX;
					isAppendingDefaultText = false;
					currentTokenKind = tokenKind;
				}
				currentTokenLength = newLength;
				if (currentTokenLength == 0)
					isAppendingDefaultText = true;
				currentOffset += length;
			}
		}

		void EndCurrentToken(int lengthTillNextToken) {
			Debug.Assert(currentTokenLength == 0 || !isAppendingDefaultText);
			int totalLength = currentDefaultTextLength + currentTokenLength;
			if (totalLength != 0) {
				Debug.Assert((int)currentTokenKind <= TOKEN_KIND_MAX);
				Debug.Assert((int)currentTokenLength <= TOKEN_LENGTH_MAX);
				Debug.Assert((int)currentDefaultTextLength <= TEXT_TOKEN_LENGTH_MAX);
				uint compressedValue = (uint)(
							((int)currentTokenKind << TOKEN_KIND_BIT) |
							(currentTokenLength << TOKEN_LENGTH_BIT) |
							(currentDefaultTextLength << TEXT_TOKEN_LENGTH_BIT));
				offsetToTokenInfoIndex.Add(currentTokenOffset, compressedValue);
			}

			currentTokenOffset += totalLength + lengthTillNextToken;
			currentDefaultTextLength = 0;
			currentTokenLength = 0;
			currentTokenKind = TextTokenKind.Last;
			isAppendingDefaultText = true;
		}
	}
}
