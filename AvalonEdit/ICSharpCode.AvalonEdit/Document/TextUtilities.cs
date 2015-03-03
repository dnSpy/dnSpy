// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Globalization;
using System.Windows.Documents;
#if NREFACTORY
using ICSharpCode.NRefactory.Editor;
#endif

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Specifies the mode for getting the next caret position.
	/// </summary>
	public enum CaretPositioningMode
	{
		/// <summary>
		/// Normal positioning (stop after every grapheme)
		/// </summary>
		Normal,
		/// <summary>
		/// Stop only on word borders.
		/// </summary>
		WordBorder,
		/// <summary>
		/// Stop only at the beginning of words. This is used for Ctrl+Left/Ctrl+Right.
		/// </summary>
		WordStart,
		/// <summary>
		/// Stop only at the beginning of words, and anywhere in the middle of symbols.
		/// </summary>
		WordStartOrSymbol,
		/// <summary>
		/// Stop only on word borders, and anywhere in the middle of symbols.
		/// </summary>
		WordBorderOrSymbol,
		/// <summary>
		/// Stop between every Unicode codepoint, even within the same grapheme.
		/// This is used to implement deleting the previous grapheme when Backspace is pressed.
		/// </summary>
		EveryCodepoint
	}
	
	/// <summary>
	/// Static helper methods for working with text.
	/// </summary>
	public static partial class TextUtilities
	{
		#region GetControlCharacterName
		// the names of the first 32 ASCII characters = Unicode C0 block
		static readonly string[] c0Table = {
			"NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL", "BS", "HT",
			"LF", "VT", "FF", "CR", "SO", "SI", "DLE", "DC1", "DC2", "DC3",
			"DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB", "ESC", "FS", "GS",
			"RS", "US"
		};
		
		// DEL (ASCII 127) and
		// the names of the control characters in the C1 block (Unicode 128 to 159)
		static readonly string[] delAndC1Table = {
			"DEL",
			"PAD", "HOP", "BPH", "NBH", "IND", "NEL", "SSA", "ESA", "HTS", "HTJ",
			"VTS", "PLD", "PLU", "RI", "SS2", "SS3", "DCS", "PU1", "PU2", "STS",
			"CCH", "MW", "SPA", "EPA", "SOS", "SGCI", "SCI", "CSI", "ST", "OSC",
			"PM", "APC"
		};
		
		/// <summary>
		/// Gets the name of the control character.
		/// For unknown characters, the unicode codepoint is returned as 4-digit hexadecimal value.
		/// </summary>
		public static string GetControlCharacterName(char controlCharacter)
		{
			int num = (int)controlCharacter;
			if (num < c0Table.Length)
				return c0Table[num];
			else if (num >= 127 && num <= 159)
				return delAndC1Table[num - 127];
			else
				return num.ToString("x4", CultureInfo.InvariantCulture);
		}
		#endregion
		
		#region GetWhitespace
		/// <summary>
		/// Gets all whitespace (' ' and '\t', but no newlines) after offset.
		/// </summary>
		/// <param name="textSource">The text source.</param>
		/// <param name="offset">The offset where the whitespace starts.</param>
		/// <returns>The segment containing the whitespace.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace",
		                                                 Justification = "WPF uses 'Whitespace'")]
		public static ISegment GetWhitespaceAfter(ITextSource textSource, int offset)
		{
			if (textSource == null)
				throw new ArgumentNullException("textSource");
			int pos;
			for (pos = offset; pos < textSource.TextLength; pos++) {
				char c = textSource.GetCharAt(pos);
				if (c != ' ' && c != '\t')
					break;
			}
			return new SimpleSegment(offset, pos - offset);
		}
		
		/// <summary>
		/// Gets all whitespace (' ' and '\t', but no newlines) before offset.
		/// </summary>
		/// <param name="textSource">The text source.</param>
		/// <param name="offset">The offset where the whitespace ends.</param>
		/// <returns>The segment containing the whitespace.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace",
		                                                 Justification = "WPF uses 'Whitespace'")]
		public static ISegment GetWhitespaceBefore(ITextSource textSource, int offset)
		{
			if (textSource == null)
				throw new ArgumentNullException("textSource");
			int pos;
			for (pos = offset - 1; pos >= 0; pos--) {
				char c = textSource.GetCharAt(pos);
				if (c != ' ' && c != '\t')
					break;
			}
			pos++; // go back the one character that isn't whitespace
			return new SimpleSegment(pos, offset - pos);
		}
		
		/// <summary>
		/// Gets the leading whitespace segment on the document line.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace",
		                                                 Justification = "WPF uses 'Whitespace'")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
		                                                 Justification = "Parameter cannot be ITextSource because it must belong to the DocumentLine")]
		public static ISegment GetLeadingWhitespace(TextDocument document, DocumentLine documentLine)
		{
			if (documentLine == null)
				throw new ArgumentNullException("documentLine");
			return GetWhitespaceAfter(document, documentLine.Offset);
		}
		
		/// <summary>
		/// Gets the trailing whitespace segment on the document line.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace",
		                                                 Justification = "WPF uses 'Whitespace'")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
		                                                 Justification = "Parameter cannot be ITextSource because it must belong to the DocumentLine")]
		public static ISegment GetTrailingWhitespace(TextDocument document, DocumentLine documentLine)
		{
			if (documentLine == null)
				throw new ArgumentNullException("documentLine");
			ISegment segment = GetWhitespaceBefore(document, documentLine.EndOffset);
			// If the whole line consists of whitespace, we consider all of it as leading whitespace,
			// so return an empty segment as trailing whitespace.
			if (segment.Offset == documentLine.Offset)
				return new SimpleSegment(documentLine.EndOffset, 0);
			else
				return segment;
		}
		#endregion
		
		#region GetSingleIndentationSegment
		/// <summary>
		/// Gets a single indentation segment starting at <paramref name="offset"/> - at most one tab
		/// or <paramref name="indentationSize"/> spaces.
		/// </summary>
		/// <param name="textSource">The text source.</param>
		/// <param name="offset">The offset where the indentation segment starts.</param>
		/// <param name="indentationSize">The size of an indentation unit. See <see cref="TextEditorOptions.IndentationSize"/>.</param>
		/// <returns>The indentation segment.
		/// If there is no indentation character at the specified <paramref name="offset"/>,
		/// an empty segment is returned.</returns>
		public static ISegment GetSingleIndentationSegment(ITextSource textSource, int offset, int indentationSize)
		{
			if (textSource == null)
				throw new ArgumentNullException("textSource");
			int pos = offset;
			while (pos < textSource.TextLength) {
				char c = textSource.GetCharAt(pos);
				if (c == '\t') {
					if (pos == offset)
						return new SimpleSegment(offset, 1);
					else
						break;
				} else if (c == ' ') {
					if (pos - offset >= indentationSize)
						break;
				} else {
					break;
				}
				// continue only if c==' ' and (pos-offset)<tabSize
				pos++;
			}
			return new SimpleSegment(offset, pos - offset);
		}
		#endregion
		
		#region GetCharacterClass
		/// <summary>
		/// Gets whether the character is whitespace, part of an identifier, or line terminator.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "c")]
		public static CharacterClass GetCharacterClass(char c)
		{
			if (c == '\r' || c == '\n')
				return CharacterClass.LineTerminator;
			if (c == '_')
				return CharacterClass.IdentifierPart;
			return GetCharacterClass(char.GetUnicodeCategory(c));
		}
		
		static CharacterClass GetCharacterClass(char highSurrogate, char lowSurrogate)
		{
			if (char.IsSurrogatePair(highSurrogate, lowSurrogate)) {
				return GetCharacterClass(char.GetUnicodeCategory(highSurrogate.ToString() + lowSurrogate.ToString(), 0));
			} else {
				// malformed surrogate pair
				return CharacterClass.Other;
			}
		}
		
		static CharacterClass GetCharacterClass(UnicodeCategory c)
		{
			switch (c) {
				case UnicodeCategory.SpaceSeparator:
				case UnicodeCategory.LineSeparator:
				case UnicodeCategory.ParagraphSeparator:
				case UnicodeCategory.Control:
					return CharacterClass.Whitespace;
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.OtherLetter:
				case UnicodeCategory.DecimalDigitNumber:
					return CharacterClass.IdentifierPart;
				case UnicodeCategory.NonSpacingMark:
				case UnicodeCategory.SpacingCombiningMark:
				case UnicodeCategory.EnclosingMark:
					return CharacterClass.CombiningMark;
				default:
					return CharacterClass.Other;
			}
		}
		#endregion
		
		#region GetNextCaretPosition
		/// <summary>
		/// Gets the next caret position.
		/// </summary>
		/// <param name="textSource">The text source.</param>
		/// <param name="offset">The start offset inside the text source.</param>
		/// <param name="direction">The search direction (forwards or backwards).</param>
		/// <param name="mode">The mode for caret positioning.</param>
		/// <returns>The offset of the next caret position, or -1 if there is no further caret position
		/// in the text source.</returns>
		/// <remarks>
		/// This method is NOT equivalent to the actual caret movement when using VisualLine.GetNextCaretPosition.
		/// In real caret movement, there are additional caret stops at line starts and ends. This method
		/// treats linefeeds as simple whitespace.
		/// </remarks>
		public static int GetNextCaretPosition(ITextSource textSource, int offset, LogicalDirection direction, CaretPositioningMode mode)
		{
			if (textSource == null)
				throw new ArgumentNullException("textSource");
			switch (mode) {
				case CaretPositioningMode.Normal:
				case CaretPositioningMode.EveryCodepoint:
				case CaretPositioningMode.WordBorder:
				case CaretPositioningMode.WordBorderOrSymbol:
				case CaretPositioningMode.WordStart:
				case CaretPositioningMode.WordStartOrSymbol:
					break; // OK
				default:
					throw new ArgumentException("Unsupported CaretPositioningMode: " + mode, "mode");
			}
			if (direction != LogicalDirection.Backward
			    && direction != LogicalDirection.Forward)
			{
				throw new ArgumentException("Invalid LogicalDirection: " + direction, "direction");
			}
			int textLength = textSource.TextLength;
			if (textLength <= 0) {
				// empty document? has a normal caret position at 0, though no word borders
				if (IsNormal(mode)) {
					if (offset > 0 && direction == LogicalDirection.Backward) return 0;
					if (offset < 0 && direction == LogicalDirection.Forward) return 0;
				}
				return -1;
			}
			while (true) {
				int nextPos = (direction == LogicalDirection.Backward) ? offset - 1 : offset + 1;
				
				// return -1 if there is no further caret position in the text source
				// we also need this to handle offset values outside the valid range
				if (nextPos < 0 || nextPos > textLength)
					return -1;
				
				// check if we've run against the textSource borders.
				// a 'textSource' usually isn't the whole document, but a single VisualLineElement.
				if (nextPos == 0) {
					// at the document start, there's only a word border
					// if the first character is not whitespace
					if (IsNormal(mode) || !char.IsWhiteSpace(textSource.GetCharAt(0)))
						return nextPos;
				} else if (nextPos == textLength) {
					// at the document end, there's never a word start
					if (mode != CaretPositioningMode.WordStart && mode != CaretPositioningMode.WordStartOrSymbol) {
						// at the document end, there's only a word border
						// if the last character is not whitespace
						if (IsNormal(mode) || !char.IsWhiteSpace(textSource.GetCharAt(textLength - 1)))
							return nextPos;
					}
				} else {
					char charBefore = textSource.GetCharAt(nextPos - 1);
					char charAfter = textSource.GetCharAt(nextPos);
					// Don't stop in the middle of a surrogate pair
					if (!char.IsSurrogatePair(charBefore, charAfter)) {
						CharacterClass classBefore = GetCharacterClass(charBefore);
						CharacterClass classAfter = GetCharacterClass(charAfter);
						// get correct class for characters outside BMP:
						if (char.IsLowSurrogate(charBefore) && nextPos >= 2) {
							classBefore = GetCharacterClass(textSource.GetCharAt(nextPos - 2), charBefore);
						}
						if (char.IsHighSurrogate(charAfter) && nextPos + 1 < textLength) {
							classAfter = GetCharacterClass(charAfter, textSource.GetCharAt(nextPos + 1));
						}
						if (StopBetweenCharacters(mode, classBefore, classAfter)) {
							return nextPos;
						}
					}
				}
				// we'll have to continue searching...
				offset = nextPos;
			}
		}
		
		static bool IsNormal(CaretPositioningMode mode)
		{
			return mode == CaretPositioningMode.Normal || mode == CaretPositioningMode.EveryCodepoint;
		}
		
		static bool StopBetweenCharacters(CaretPositioningMode mode, CharacterClass charBefore, CharacterClass charAfter)
		{
			if (mode == CaretPositioningMode.EveryCodepoint)
				return true;
			// Don't stop in the middle of a grapheme
			if (charAfter == CharacterClass.CombiningMark)
				return false;
			// Stop after every grapheme in normal mode
			if (mode == CaretPositioningMode.Normal)
				return true;
			if (charBefore == charAfter) {
				if (charBefore == CharacterClass.Other &&
				    (mode == CaretPositioningMode.WordBorderOrSymbol || mode == CaretPositioningMode.WordStartOrSymbol))
				{
					// With the "OrSymbol" modes, there's a word border and start between any two unknown characters
					return true;
				}
			} else {
				// this looks like a possible border
				
				// if we're looking for word starts, check that this is a word start (and not a word end)
				// if we're just checking for word borders, accept unconditionally
				if (!((mode == CaretPositioningMode.WordStart || mode == CaretPositioningMode.WordStartOrSymbol)
				      && (charAfter == CharacterClass.Whitespace || charAfter == CharacterClass.LineTerminator)))
				{
					return true;
				}
			}
			return false;
		}
		#endregion
	}
	
	/// <summary>
	/// Classifies a character as whitespace, line terminator, part of an identifier, or other.
	/// </summary>
	public enum CharacterClass
	{
		/// <summary>
		/// The character is not whitespace, line terminator or part of an identifier.
		/// </summary>
		Other,
		/// <summary>
		/// The character is whitespace (but not line terminator).
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace",
		                                                 Justification = "WPF uses 'Whitespace'")]
		Whitespace,
		/// <summary>
		/// The character can be part of an identifier (Letter, digit or underscore).
		/// </summary>
		IdentifierPart,
		/// <summary>
		/// The character is line terminator (\r or \n).
		/// </summary>
		LineTerminator,
		/// <summary>
		/// The character is a unicode combining mark that modifies the previous character.
		/// Corresponds to the Unicode designations "Mn", "Mc" and "Me".
		/// </summary>
		CombiningMark
	}
}
