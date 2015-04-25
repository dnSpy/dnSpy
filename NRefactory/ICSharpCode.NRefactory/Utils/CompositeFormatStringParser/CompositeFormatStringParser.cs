//
// CompositeFormatStringParser.cs
//
// Authors:
//   Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.Utils
{
	/// <summary>
	/// Composite format string parser.
	/// </summary>
	/// <remarks>
	/// Implements a complete parser for valid strings as well as
	/// error reporting and best-effort parsing for invalid strings.
	/// </remarks>		
	public class CompositeFormatStringParser
	{

		public CompositeFormatStringParser ()
		{
			errors = new List<IFormatStringError> ();
		}

		/// <summary>
		/// Parse the specified format string.
		/// </summary>
		/// <param name='format'>
		/// The format string.
		/// </param>
		public FormatStringParseResult Parse (string format)
		{
			if (format == null)
				throw new ArgumentNullException ("format");

			var result = new FormatStringParseResult();

			// Format string syntax: http://msdn.microsoft.com/en-us/library/txafckwd.aspx
			int textStart = 0;
			var length = format.Length;
			for (int i = 0; i < length; i++) {
				// Get fixed text
				GetText (format, ref i);

				if (i < format.Length && format [i] == '{') {
					int formatItemStart = i;
					int index;
					int? alignment;
					string argumentFormat;
					var textSegmentErrors = new List<IFormatStringError>(GetErrors());

					// Try to parse the parts of the format item
					++i;
					index = ParseIndex (format, ref i);
					CheckForMissingEndBrace (format, i, length);

					alignment = ParseAlignment (format, ref i, length);
					CheckForMissingEndBrace (format, i, length);

					argumentFormat = ParseSubFormatString (format, ref i, length);
					CheckForMissingEndBrace (format, i, length);

					// Check what we parsed
					if (i == formatItemStart + 1 && (i == length || (i < length && format[i] != '}'))) {
						// There were no format item after all, this was just an
						// unescaped left brace, or the initial brace of an escape sequence
						SetErrors(textSegmentErrors);
						if (i >= length || format[i] != '{') {
							AddError (new DefaultFormatStringError {
								Message = "Unescaped '{'",
								StartLocation = formatItemStart,
								EndLocation = formatItemStart + 1,
								OriginalText = "{",
								SuggestedReplacementText = "{{"
							});
						}
						continue;
					}

					if (formatItemStart - textStart > 0) {
						// We have parsed a format item, end the text segment
						var textSegment = new TextSegment (UnEscape (format.Substring (textStart, formatItemStart - textStart)));
						textSegment.Errors = textSegmentErrors;
						result.Segments.Add (textSegment);
					}
					
					// Unclosed format items in fixed text advances i one step too far
					if (i < length && format [i] != '}')
						--i;

					// i may actually point outside of format if there is a syntactical error
					// if that happens, we want the last position
					var endLocation = Math.Min (length, i + 1);
					result.Segments.Add (new FormatItem (index, alignment, argumentFormat) {
						StartLocation = formatItemStart,
						EndLocation = endLocation,
						Errors = GetErrors ()
					});
					ClearErrors ();

					// The next potential text segment starts after this format item
					textStart = i + 1;
				}
			}
			// Handle remaining text
			if (textStart < length) {
				var textSegment = new TextSegment (UnEscape (format.Substring (textStart)), textStart);
				textSegment.Errors = GetErrors();
				result.Segments.Add (textSegment);

			}
			return result;
		}

		int ParseIndex (string format, ref int i)
		{
			int parsedCharacters;
			int? maybeIndex = GetAndCheckNumber (format, ",:}", ref i, i, out parsedCharacters);
			if (parsedCharacters == 0) {
				AddError (new DefaultFormatStringError {
					StartLocation = i,
					EndLocation = i,
					Message = "Missing index",
					OriginalText = "",
					SuggestedReplacementText = "0"
				});
			}
			return maybeIndex ?? 0;
		}

		int? ParseAlignment(string format, ref int i, int length)
		{
			if (i < length && format [i] == ',') {
				int alignmentBegin = i;
				++i;
				while (i < length && char.IsWhiteSpace(format [i]))
					++i;

				int parsedCharacters;
				var number = GetAndCheckNumber (format, ",:}", ref i, alignmentBegin + 1, out parsedCharacters);
				if (parsedCharacters == 0) {
					AddError (new DefaultFormatStringError {
						StartLocation = i,
						EndLocation = i,
						Message = "Missing alignment",
						OriginalText = "",
						SuggestedReplacementText = "0"
					});
				}
				return number ?? 0;
			}
			return null;
		}

		string ParseSubFormatString(string format, ref int i, int length)
		{
			if (i < length && format [i] == ':') {
				++i;
				int begin = i;
				GetText(format, ref i, "", true);
				var escaped = format.Substring (begin, i - begin);
				return UnEscape (escaped);
			}
			return null;
		}

		void CheckForMissingEndBrace (string format, int i, int length)
		{
			if (i == length) {
				int j;
				for (j = i - 1; format[j] == '}'; j--);
				var oddEndBraceCount = (i - j) % 2 == 1;
				if (oddEndBraceCount) {
					AddMissingEndBraceError(i, i, "Missing '}'", "");
				}
				return;
			}
			return;
		}
		
		void GetText (string format, ref int index, string delimiters = "", bool allowEscape = false)
		{
			while (index < format.Length) {
				if (format [index] == '{' || format[index] == '}') {
					if (index + 1 < format.Length && format [index + 1] == format[index] && allowEscape)
						++index;
					else
						break;
				} else if (delimiters.Contains(format[index].ToString())) {
					break;
				}
				++index;
			};
		}
		
		int? GetNumber (string format, ref int index)
		{
			if (format.Length == 0) {
				return null;
			}
			int sum = 0;
			int i = index;
			bool positive = format [i] != '-';
			if (!positive)
				++i;
			int numberStartIndex = i;
			while (i < format.Length && format[i] >= '0' && format[i] <= '9') {
				sum = 10 * sum + format [i] - '0';
				++i;
			}
			if (i == numberStartIndex)
				return null;

			index = i;
			return positive ? sum : -sum;
		}

		int? GetAndCheckNumber (string format, string delimiters, ref int index, int numberFieldStart, out int parsedCharacters)
		{
			int fieldIndex = index;
			GetText (format, ref fieldIndex, delimiters);
			int fieldEnd = fieldIndex;
			var numberText = format.Substring(index, fieldEnd - index);
			parsedCharacters = numberText.Length;
			int numberLength = 0;
			int? number = GetNumber (numberText, ref numberLength);
			if (numberLength != parsedCharacters && fieldEnd < format.Length && delimiters.Contains (format [fieldEnd])) {
				// Not the entire number field could be parsed
				// The field actually ended as intended, so set the index to the end of the field
				index = fieldEnd;
				var suggestedNumber = (number ?? 0).ToString ();
				AddInvalidNumberFormatError (numberFieldStart, format.Substring (numberFieldStart, index - numberFieldStart), suggestedNumber);
			} else {
				var endingChar = index + numberLength;
				if (numberLength != parsedCharacters) {
					// Not the entire number field could be parsed
					// The field didn't end, it was cut off so we are missing an ending brace
					index = endingChar;
					AddMissingEndBraceError (index, index, "Missing ending '}'", "");
				} else {
					index = endingChar;
				}
			}
			return number;
		}

		public static string UnEscape (string unEscaped)
		{
			return unEscaped.Replace ("{{", "{").Replace ("}}", "}");
		}

		IList<IFormatStringError> errors;
		
		bool hasMissingEndBrace = false;

		void AddError (IFormatStringError error)
		{
			errors.Add (error);
		}

		void AddMissingEndBraceError(int start, int end, string message, string originalText)
		{
			// Only add a single missing end brace per format item
			if (hasMissingEndBrace)
				return;
			AddError (new DefaultFormatStringError {
				StartLocation = start,
				EndLocation = end,
				Message = message,
				OriginalText = originalText,
				SuggestedReplacementText = "}"
			});
			hasMissingEndBrace = true;
		}

		void AddInvalidNumberFormatError (int i, string number, string replacementText)
		{
			AddError (new DefaultFormatStringError {
				StartLocation = i,
				EndLocation = i + number.Length,
				Message = string.Format ("Invalid number '{0}'", number),
				OriginalText = number,
				SuggestedReplacementText = replacementText
			});
		}

		IList<IFormatStringError> GetErrors ()
		{
			return errors;
		}
		
		void SetErrors (IList<IFormatStringError> errors)
		{
			this.errors = errors;
		}

		void ClearErrors ()
		{
			hasMissingEndBrace = false;
			errors = new List<IFormatStringError> ();
		}
	}

	public class FormatStringParseResult
	{
		public FormatStringParseResult()
		{
			Segments = new List<IFormatStringSegment>();
		}

		public IList<IFormatStringSegment> Segments { get; private set; }

		public bool HasErrors
		{
			get {
				return Segments.SelectMany(segment => segment.Errors).Any();
			}
		}
	}
}

