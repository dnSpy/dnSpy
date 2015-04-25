//
// UnicodeNewline.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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

namespace ICSharpCode.NRefactory
{
	public enum UnicodeNewline {
		Unknown,

		/// <summary>
		/// Line Feed, U+000A
		/// </summary>
		LF = 0x0A,


		CRLF = 0x0D0A,

		/// <summary>
		/// Carriage Return, U+000D
		/// </summary>
		CR = 0x0D,

		/// <summary>
		/// Next Line, U+0085
		/// </summary>
		NEL = 0x85,

		/// <summary>
		/// Vertical Tab, U+000B
		/// </summary>
		VT = 0x0B,

		/// <summary>
		/// Form Feed, U+000C
		/// </summary>
		FF = 0x0C,

		/// <summary>
		/// Line Separator, U+2028
		/// </summary>
		LS = 0x2028,

		/// <summary>
		/// Paragraph Separator, U+2029
		/// </summary>
		PS = 0x2029
	}


	/// <summary>
	/// Defines unicode new lines according to  Unicode Technical Report #13
	/// http://www.unicode.org/standard/reports/tr13/tr13-5.html
	/// </summary>
	public static class NewLine
	{
		/// <summary>
		/// Carriage Return, U+000D
		/// </summary>
		public const char CR = (char)0x0D;

		/// <summary>
		/// Line Feed, U+000A
		/// </summary>
		public const char LF = (char)0x0A;

		/// <summary>
		/// Next Line, U+0085
		/// </summary>
		public const char NEL = (char)0x85;

		/// <summary>
		/// Vertical Tab, U+000B
		/// </summary>
		public const char VT  = (char)0x0B;

		/// <summary>
		/// Form Feed, U+000C
		/// </summary>
		public const char FF  = (char)0x0C;

		/// <summary>
		/// Line Separator, U+2028
		/// </summary>
		public const char LS  = (char)0x2028;

		/// <summary>
		/// Paragraph Separator, U+2029
		/// </summary>
		public const char PS  = (char)0x2029;

		/// <summary>
		/// Determines if a char is a new line delimiter.
		/// </summary>
		/// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
		/// <param name="curChar">The current character.</param>
		/// <param name="nextChar">A callback getting the next character (may be null).</param>
		public static int GetDelimiterLength (char curChar, Func<char> nextChar = null)
		{
			if (curChar == CR) {
				if (nextChar != null && nextChar () == LF)
					return 2;
				return 1;
			}

			if (curChar == LF || curChar == NEL || curChar == VT || curChar == FF || curChar == LS || curChar == PS)
				return 1;
			return 0;
		}

		/// <summary>
		/// Determines if a char is a new line delimiter.
		/// </summary>
		/// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
		/// <param name="curChar">The current character.</param>
		/// <param name="nextChar">The next character (if != LF then length will always be 0 or 1).</param>
		public static int GetDelimiterLength (char curChar, char nextChar)
		{
			if (curChar == CR) {
				if (nextChar == LF)
					return 2;
				return 1;
			}

			if (curChar == LF || curChar == NEL || curChar == VT || curChar == FF || curChar == LS || curChar == PS)
				return 1;
			return 0;
		}


		/// <summary>
		/// Determines if a char is a new line delimiter.
		/// </summary>
		/// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
		/// <param name="curChar">The current character.</param>
		/// <param name = "length">The length of the delimiter</param>
		/// <param name = "type">The type of the delimiter</param>
		/// <param name="nextChar">A callback getting the next character (may be null).</param>
		public static bool TryGetDelimiterLengthAndType (char curChar, out int length, out UnicodeNewline type, Func<char> nextChar = null)
		{
			if (curChar == CR) {
				if (nextChar != null && nextChar () == LF) {
					length = 2;
					type = UnicodeNewline.CRLF;
				} else {
					length = 1;
					type = UnicodeNewline.CR;

				}
				return true;
			}

			switch (curChar) {
			case LF:
				type = UnicodeNewline.LF;
				length = 1;
				return true;
			case NEL:
				type = UnicodeNewline.NEL;
				length = 1;
				return true;
			case VT:
				type = UnicodeNewline.VT;
				length = 1;
				return true;
			case FF:
				type = UnicodeNewline.FF;
				length = 1;
				return true;
			case LS:
				type = UnicodeNewline.LS;
				length = 1;
				return true;
			case PS:
				type = UnicodeNewline.PS;
				length = 1;
				return true;
			}
			length = -1;
			type = UnicodeNewline.Unknown;
			return false;
		}

		/// <summary>
		/// Determines if a char is a new line delimiter.
		/// </summary>
		/// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
		/// <param name="curChar">The current character.</param>
		/// <param name = "length">The length of the delimiter</param>
		/// <param name = "type">The type of the delimiter</param>
		/// <param name="nextChar">The next character (if != LF then length will always be 0 or 1).</param>
		public static bool TryGetDelimiterLengthAndType (char curChar, out int length, out UnicodeNewline type, char nextChar)
		{
			if (curChar == CR) {
				if (nextChar == LF) {
					length = 2;
					type = UnicodeNewline.CRLF;
				} else {
					length = 1;
					type = UnicodeNewline.CR;

				}
				return true;
			}

			switch (curChar) {
			case LF:
				type = UnicodeNewline.LF;
				length = 1;
				return true;
			case NEL:
				type = UnicodeNewline.NEL;
				length = 1;
				return true;
			case VT:
				type = UnicodeNewline.VT;
				length = 1;
				return true;
			case FF:
				type = UnicodeNewline.FF;
				length = 1;
				return true;
			case LS:
				type = UnicodeNewline.LS;
				length = 1;
				return true;
			case PS:
				type = UnicodeNewline.PS;
				length = 1;
				return true;
			}
			length = -1;
			type = UnicodeNewline.Unknown;
			return false;
		}

		/// <summary>
		/// Gets the new line type of a given char/next char.
		/// </summary>
		/// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
		/// <param name="curChar">The current character.</param>
		/// <param name="nextChar">A callback getting the next character (may be null).</param>
		public static UnicodeNewline GetDelimiterType (char curChar, Func<char> nextChar = null)
		{
			switch (curChar) {
				case CR:
				if (nextChar != null && nextChar () == LF)
					return UnicodeNewline.CRLF;
				return UnicodeNewline.CR;
				case LF:
				return UnicodeNewline.LF;
				case NEL:
				return UnicodeNewline.NEL;
				case VT:
				return UnicodeNewline.VT;
				case FF:
				return UnicodeNewline.FF;
				case LS:
				return UnicodeNewline.LS;
				case PS:
				return UnicodeNewline.PS;
			}
			return UnicodeNewline.Unknown;
		}

		/// <summary>
		/// Gets the new line type of a given char/next char.
		/// </summary>
		/// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
		/// <param name="curChar">The current character.</param>
		/// <param name="nextChar">The next character (if != LF then length will always be 0 or 1).</param>
		public static UnicodeNewline GetDelimiterType (char curChar, char nextChar)
		{
			switch (curChar) {
			case CR:
				if (nextChar == LF)
					return UnicodeNewline.CRLF;
				return UnicodeNewline.CR;
			case LF:
				return UnicodeNewline.LF;
			case NEL:
				return UnicodeNewline.NEL;
			case VT:
				return UnicodeNewline.VT;
			case FF:
				return UnicodeNewline.FF;
			case LS:
				return UnicodeNewline.LS;
			case PS:
				return UnicodeNewline.PS;
			}
			return UnicodeNewline.Unknown;
		}

		/// <summary>
		/// Determines if a char is a new line delimiter. 
		/// 
		/// Note that the only 2 char wide new line is CR LF and both chars are new line
		/// chars on their own. For most cases GetDelimiterLength is the better choice.
		/// </summary>
		public static bool IsNewLine(char ch)
		{
			return
				ch == NewLine.CR ||
				ch == NewLine.LF ||
				ch == NewLine.NEL ||
				ch == NewLine.VT ||
				ch == NewLine.FF ||
				ch == NewLine.LS ||
				ch == NewLine.PS;
		}

		/// <summary>
		/// Gets the new line as a string.
		/// </summary>
		public static string GetString (UnicodeNewline newLine)
		{
			switch (newLine) {
			case UnicodeNewline.Unknown:
				return "";
			case UnicodeNewline.LF:
				return "\n";
			case UnicodeNewline.CRLF:
				return "\r\n";
			case UnicodeNewline.CR:
				return "\r";
			case UnicodeNewline.NEL:
				return "\u0085";
			case UnicodeNewline.VT:
				return "\u000B";
			case UnicodeNewline.FF:
				return "\u000C";
			case UnicodeNewline.LS:
				return "\u2028";
			case UnicodeNewline.PS:
				return "\u2029";
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}
	}
}

