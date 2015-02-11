
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.TextView
{
	sealed class LanguageTokens
	{
		// current write offset
		int currentOffset;
		// current offset of this token. Only gets updated when a new token is saved
		int currentTokenOffset;
		// current length of default text. Can't be incremented when isAppendingDefaultText == true
		int currentDefaultTextLength;
		// current length of token. Valid when isAppendingDefaultText == false
		int currentTokenLength;
		// current token type. Valid when isAppendingDefaultText == false
		TextTokenType currentTokenType;
		bool isAppendingDefaultText = true;

		// Convert offset to an index into the token list
		Dictionary<int, int> offsetToTokenInfoIndex = new Dictionary<int, int>();
		List<ushort> tokenInfosList = new List<ushort>();
		ushort[] tokenInfos;

		static readonly char[] newLineChars = new char[] { '\r', '\n' };

		// Each token is usually surrounded by tokens of default color, and the default color
		// usually is the first token on each line. Merge this info into one struct. 16 bits is
		// enough. If a token or a default text token exceeds these lengths, it will be split up
		// into multiple of the following structs.
		//	[15:10] = token type
		//	[9:4]   = token length
		//	[3:0]   = Text (default color) token length
		const int TOKEN_TYPE_BITS = 6;
		const int TOKEN_TYPE_BIT = 10;
		const int TOKEN_TYPE_MAX = (1 << TOKEN_TYPE_BITS) - 1;
		const int TOKEN_LENGTH_BITS = 6;
		const int TOKEN_LENGTH_BIT = 4;
		const int TOKEN_LENGTH_MAX = (1 << TOKEN_LENGTH_BITS) - 1;
		const int TEXT_TOKEN_LENGTH_BITS = 4;
		const int TEXT_TOKEN_LENGTH_BIT = 0;
		const int TEXT_TOKEN_LENGTH_MAX = (1 << TEXT_TOKEN_LENGTH_BITS) - 1;

		static LanguageTokens()
		{
			if ((int)TextTokenType.Last > (1 << TOKEN_TYPE_BITS))
				throw new InvalidProgramException("TOKEN_TYPE_BITS is too small");
		}

		public bool Find(int offset, out int defaultTextLength, out TextTokenType tokenType, out int tokenLength)
		{
			Debug.Assert(tokenInfos != null, "You must call Finish() before you call this method");
			int index;
			if (!offsetToTokenInfoIndex.TryGetValue(offset, out index)) {
				defaultTextLength = 0;
				tokenType = TextTokenType.Last;
				tokenLength = 0;
				return false;
			}

			ushort val = tokenInfos[index];
			defaultTextLength = (val >> TEXT_TOKEN_LENGTH_BIT) & TEXT_TOKEN_LENGTH_MAX;
			tokenType = (TextTokenType)((val >> TOKEN_TYPE_BIT) & TOKEN_TYPE_MAX);
			tokenLength = (val >> TOKEN_LENGTH_BIT) & TOKEN_LENGTH_MAX;
			return true;
		}

		public void Finish()
		{
			EndCurrentToken(0);
			tokenInfos = tokenInfosList.ToArray();
			tokenInfosList = null;
		}

		public void Append(TextTokenType tokenType, char c)
		{
			Append(tokenType, c.ToString());
		}

		public void Append(TextTokenType tokenType, string s)
		{
			int oldCurrentOffset = currentOffset;

			// Newlines could be part of the input string
			int so = 0;
			while (so < s.Length) {
				int nlOffs = s.IndexOfAny(newLineChars, so);
				if (nlOffs >= 0) {
					AppendInternal(tokenType, nlOffs - so);
					so = nlOffs;
					int nlLen = s[so] == '\r' && so + 1 < s.Length && s[so + 1] == '\n' ? 2 : 1;
					currentOffset += nlLen;
					EndCurrentToken(nlLen);
					so += nlLen;
				}
				else {
					AppendInternal(tokenType, s.Length - so);
					break;
				}
			}

			Debug.Assert(oldCurrentOffset + s.Length == currentOffset);
		}

		public void AppendLine()
		{
			// We must append the same type of new line string as StringBuilder
			Append(TextTokenType.Text, Environment.NewLine);
		}

		// Gets called to add one token. No newlines are allowed
		void AppendInternal(TextTokenType tokenType, int length)
		{
			Debug.Assert(length >= 0);
			if (length == 0)
				return;

redo:
			if (isAppendingDefaultText) {
				if (tokenType == TextTokenType.Text) {
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
				currentTokenType = tokenType;
			}

			if (currentTokenType != tokenType) {
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
					currentTokenType = tokenType;
				}
				currentTokenLength = newLength;
				if (currentTokenLength == 0)
					isAppendingDefaultText = true;
				currentOffset += length;
			}
		}

		void EndCurrentToken(int lengthTillNextToken)
		{
			Debug.Assert(currentTokenLength == 0 || !isAppendingDefaultText);
			int totalLength = currentDefaultTextLength + currentTokenLength;
			if (totalLength != 0) {
				offsetToTokenInfoIndex.Add(currentTokenOffset, tokenInfosList.Count);
				Debug.Assert((int)currentTokenType <= TOKEN_TYPE_MAX);
				Debug.Assert((int)currentTokenLength <= TOKEN_LENGTH_MAX);
				Debug.Assert((int)currentDefaultTextLength <= TEXT_TOKEN_LENGTH_MAX);
				ushort compressedValue = (ushort)(
							((int)currentTokenType << TOKEN_TYPE_BIT) |
							(currentTokenLength << TOKEN_LENGTH_BIT) |
							(currentDefaultTextLength << TEXT_TOKEN_LENGTH_BIT));
				tokenInfosList.Add(compressedValue);
			}

			currentTokenOffset += totalLength + lengthTillNextToken;
			currentDefaultTextLength = 0;
			currentTokenLength = 0;
			currentTokenType = TextTokenType.Last;
			isAppendingDefaultText = true;
		}
	}
}
