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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Caches text and data
	/// </summary>
	public sealed class CachedTextTokenColors {
		static CachedTextTokenColors() {
			var empty = new CachedTextTokenColors();
			empty.Freeze();
			Empty = empty;
		}

		/// <summary>
		/// Gets an empty frozen instance
		/// </summary>
		public static readonly CachedTextTokenColors Empty;

		/// <summary>
		/// Gets the length of the text
		/// </summary>
		public int Length => currentOffset;

		static object noColor = new object();

		// current write offset
		int currentOffset;
		// current offset of this token. Only gets updated when a new token is saved
		int currentTokenOffset;
		// current length of default text. Can't be incremented when isAppendingDefaultText == true
		int currentDefaultTextLength;
		// current length of token. Valid when isAppendingDefaultText == false
		int currentTokenLength;
		// current token data (color). Valid when isAppendingDefaultText == false
		object currentTokenData = noColor;
		bool isAppendingDefaultText = true;
		Dictionary<int, TokenInfo> offsetToTokenInfo = new Dictionary<int, TokenInfo>();
		bool isFrozen;

		static readonly char[] newLineChars = new char[] { '\r', '\n', '\u0085', '\u2028', '\u2029' };

		const int TEXT_TOKEN_LENGTH_MAX = ushort.MaxValue;
		const int TOKEN_LENGTH_MAX = ushort.MaxValue;
		struct TokenInfo {
			public readonly ushort TokenLength;
			public readonly ushort TextLength;
			public readonly object Data;
			public TokenInfo(int tokenLength, int textLength, object data) {
				Debug.Assert(tokenLength <= TOKEN_LENGTH_MAX);
				Debug.Assert(textLength <= TEXT_TOKEN_LENGTH_MAX);
				TokenLength = (ushort)tokenLength;
				TextLength = (ushort)textLength;
				Data = data;
			}
		}

		/// <summary>
		/// Finds data
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="defaultTextLength">Updated with length of default text with no data</param>
		/// <param name="data">Data</param>
		/// <param name="tokenLength">Updated with length of text</param>
		/// <returns></returns>
		public bool Find(int offset, out int defaultTextLength, out object data, out int tokenLength) {
			TokenInfo info;
			if (!offsetToTokenInfo.TryGetValue(offset, out info)) {
				defaultTextLength = 0;
				data = BoxedOutputColor.Text;
				tokenLength = 0;
				return false;
			}

			defaultTextLength = info.TextLength;
			data = info.Data;
			tokenLength = info.TokenLength;
			return true;
		}

		/// <summary>
		/// Ends the current token
		/// </summary>
		public void Flush() => EndCurrentToken(0);

		/// <summary>
		/// Call this method when there's nothing more to write
		/// </summary>
		public void Finish() => Flush();

		/// <summary>
		/// Freezes the instance so no new text can be added
		/// </summary>
		public void Freeze() {
			if (isFrozen)
				return;
			Finish();
			isFrozen = true;
		}

		/// <summary>
		/// Appends data and text
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="s">Text</param>
		public void Append(object data, string s) {
			if (s == null)
				return;
			Append(data, s, 0, s.Length);
		}

		/// <summary>
		/// Appends data and text
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="s">Text</param>
		/// <param name="offset">Start offset in <paramref name="s"/></param>
		/// <param name="length">Length</param>
		public void Append(object data, string s, int offset, int length) {
			if (s == null)
				return;

			// Newlines could be part of the input string
			int so = offset;
			int end = offset + length;
			while (so < end) {
				int nlOffs = s.IndexOfAny(newLineChars, so, end - so);
				if (nlOffs >= 0) {
					AppendInternal(data, nlOffs - so);
					so = nlOffs;
					int nlLen = s[so] == '\r' && so + 1 < end && s[so + 1] == '\n' ? 2 : 1;
					currentOffset += nlLen;
					EndCurrentToken(nlLen);
					so += nlLen;
				}
				else {
					AppendInternal(data, end - so);
					break;
				}
			}
		}

		/// <summary>
		/// Appends a newline
		/// </summary>
		public void AppendLine() {
			// We must append the same type of new line string as StringBuilder
			Append(BoxedOutputColor.Text, Environment.NewLine);
		}

		// Gets called to add one token. No newlines are allowed
		void AppendInternal(object data, int length) {
			if (isFrozen)
				throw new InvalidOperationException("Can't append more data after the instance has been frozen");
			Debug.Assert(length >= 0);
			if (length == 0)
				return;
			Debug.Assert(data != null);
			if (data == null)
				data = noColor;

redo:
			if (isAppendingDefaultText) {
				if (data.Equals(BoxedTextTokenKind.Text) || data.Equals(BoxedOutputColor.Text)) {
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
				currentTokenData = data;
			}

			if (!currentTokenData.Equals(data)) {
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
					currentTokenData = data;
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
				Debug.Assert(currentTokenLength <= TOKEN_LENGTH_MAX);
				Debug.Assert(currentDefaultTextLength <= TEXT_TOKEN_LENGTH_MAX);
				offsetToTokenInfo.Add(currentTokenOffset, new TokenInfo(currentTokenLength, currentDefaultTextLength, currentTokenData));
			}

			currentTokenOffset += totalLength + lengthTillNextToken;
			currentDefaultTextLength = 0;
			currentTokenLength = 0;
			currentTokenData = noColor;
			isAppendingDefaultText = true;
		}
	}
}
