// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ICSharpCode.NRefactory.Parser.CSharp
{
	internal sealed class Lexer : AbstractLexer
	{
		bool isAtLineBegin = true;
		
		public Lexer(TextReader reader) : base(reader)
		{
		}
		
		protected override Token Next()
		{
			char ch;
			bool hadLineEnd = false;
			if (Line == 1 && Col == 1) {
				isAtLineBegin = true;
				hadLineEnd = true; // beginning of document
			}
			
			while (true) {
				Location startLocation = new Location(Col, Line);
				int nextChar = ReaderRead();
				if (nextChar == -1)
					break;
				
				Token token;
				
				switch (nextChar) {
					case ' ':
					case '\t':
						continue;
					case '\r':
					case '\n':
						HandleLineEnd((char)nextChar);
						if (hadLineEnd) {
							// second line end before getting to a token
							// -> here was a blank line
							specialTracker.AddEndOfLine(startLocation);
						}
						hadLineEnd = true;
						isAtLineBegin = true;
						continue;
					case '/':
						int peek = ReaderPeek();
						if (peek == '/' || peek == '*') {
							ReadComment();
							continue;
						} else {
							isAtLineBegin = false;
							token = ReadOperator('/');
						}
						break;
					case '#':
						ReadPreProcessingDirective();
						isAtLineBegin = false;
						continue;
					case '"':
						token = ReadString();
						isAtLineBegin = false;
						break;
					case '\'':
						token = ReadChar();
						isAtLineBegin = false;
						break;
					case '@':
						isAtLineBegin = false;
						int next = ReaderRead();
						if (next == -1) {
							errors.Error(Line, Col, String.Format("EOF after @"));
							continue;
						} else {
							int x = Col - 1;
							int y = Line;
							ch = (char)next;
							if (ch == '"') {
								token = ReadVerbatimString();
							} else if (Char.IsLetterOrDigit(ch) || ch == '_') {
								bool canBeKeyword;
								token = new Token(Tokens.Identifier, x - 1, y, ReadIdent(ch, out canBeKeyword));
							} else {
								HandleLineEnd(ch);
								errors.Error(y, x, String.Format("Unexpected char in Lexer.Next() : {0}", ch));
								continue;
							}
						}
						break;
					default:
						isAtLineBegin = false; // non-ws chars are handled here
						ch = (char)nextChar;
						if (Char.IsLetter(ch) || ch == '_' || ch == '\\') {
							int x = Col - 1; // Col was incremented above, but we want the start of the identifier
							int y = Line;
							bool canBeKeyword;
							string s = ReadIdent(ch, out canBeKeyword);
							if (canBeKeyword) {
								int keyWordToken = Keywords.GetToken(s);
								if (keyWordToken >= 0) {
									return new Token(keyWordToken, x, y, s);
								}
							}
							return new Token(Tokens.Identifier, x, y, s);
						} else if (Char.IsDigit(ch)) {
							token = ReadDigit(ch, Col - 1);
						} else {
							token = ReadOperator(ch);
						}
						break;
				}
				
				// try error recovery (token = null -> continue with next char)
				if (token != null) {
					return token;
				}
			}
			
			return new Token(Tokens.EOF, Col, Line, String.Empty);
		}
		
		// The C# compiler has a fixed size length therefore we'll use a fixed size char array for identifiers
		// it's also faster than using a string builder.
		const int MAX_IDENTIFIER_LENGTH = 512;
		char[] identBuffer = new char[MAX_IDENTIFIER_LENGTH];
		
		string ReadIdent(char ch, out bool canBeKeyword)
		{
			int peek;
			int curPos     = 0;
			canBeKeyword = true;
			while (true) {
				if (ch == '\\') {
					peek = ReaderPeek();
					if (peek != 'u' && peek != 'U') {
						errors.Error(Line, Col, "Identifiers can only contain unicode escape sequences");
					}
					canBeKeyword = false;
					string surrogatePair;
					ReadEscapeSequence(out ch, out surrogatePair);
					if (surrogatePair != null) {
						if (!char.IsLetterOrDigit(surrogatePair, 0)) {
							errors.Error(Line, Col, "Unicode escape sequences in identifiers cannot be used to represent characters that are invalid in identifiers");
						}
						for (int i = 0; i < surrogatePair.Length - 1; i++) {
							if (curPos < MAX_IDENTIFIER_LENGTH) {
								identBuffer[curPos++] = surrogatePair[i];
							}
						}
						ch = surrogatePair[surrogatePair.Length - 1];
					} else {
						if (!IsIdentifierPart(ch)) {
							errors.Error(Line, Col, "Unicode escape sequences in identifiers cannot be used to represent characters that are invalid in identifiers");
						}
					}
				}
				
				if (curPos < MAX_IDENTIFIER_LENGTH) {
					if (ch != '\0') // only add character, if it is valid
									 // prevents \ from being added
						identBuffer[curPos++] = ch;
				} else {
					errors.Error(Line, Col, String.Format("Identifier too long"));
					while (IsIdentifierPart(ReaderPeek())) {
						ReaderRead();
					}
					break;
				}
				peek = ReaderPeek();
				if (IsIdentifierPart(peek) || peek == '\\') {
					ch = (char)ReaderRead();
				} else {
					break;
				}
			}
			return new String(identBuffer, 0, curPos);
		}
		
		Token ReadDigit(char ch, int x)
		{
			unchecked { // prevent exception when ReaderPeek() = -1 is cast to char
				int y = Line;
				sb.Length = 0;
				sb.Append(ch);
				string prefix = null;
				string suffix = null;
				
				bool ishex      = false;
				bool isunsigned = false;
				bool islong     = false;
				bool isfloat    = false;
				bool isdouble   = false;
				bool isdecimal  = false;
				
				char peek = (char)ReaderPeek();
				
				if (ch == '.')  {
					isdouble = true;
					
					while (Char.IsDigit((char)ReaderPeek())) { // read decimal digits beyond the dot
						sb.Append((char)ReaderRead());
					}
					peek = (char)ReaderPeek();
				} else if (ch == '0' && (peek == 'x' || peek == 'X')) {
					ReaderRead(); // skip 'x'
					sb.Length = 0; // Remove '0' from 0x prefix from the stringvalue
					while (IsHex((char)ReaderPeek())) {
						sb.Append((char)ReaderRead());
					}
					if (sb.Length == 0) {
						sb.Append('0'); // dummy value to prevent exception
						errors.Error(y, x, "Invalid hexadecimal integer literal");
					}
					ishex = true;
					prefix = "0x";
					peek = (char)ReaderPeek();
				} else {
					while (Char.IsDigit((char)ReaderPeek())) {
						sb.Append((char)ReaderRead());
					}
					peek = (char)ReaderPeek();
				}
				
				Token nextToken = null; // if we accidently read a 'dot'
				if (peek == '.') { // read floating point number
					ReaderRead();
					peek = (char)ReaderPeek();
					if (!Char.IsDigit(peek)) {
						nextToken = new Token(Tokens.Dot, Col - 1, Line);
						peek = '.';
					} else {
						isdouble = true; // double is default
						if (ishex) {
							errors.Error(y, x, String.Format("No hexadecimal floating point values allowed"));
						}
						sb.Append('.');
						
						while (Char.IsDigit((char)ReaderPeek())) { // read decimal digits beyond the dot
							sb.Append((char)ReaderRead());
						}
						peek = (char)ReaderPeek();
					}
				}
				
				if (peek == 'e' || peek == 'E') { // read exponent
					isdouble = true;
					sb.Append((char)ReaderRead());
					peek = (char)ReaderPeek();
					if (peek == '-' || peek == '+') {
						sb.Append((char)ReaderRead());
					}
					while (Char.IsDigit((char)ReaderPeek())) { // read exponent value
						sb.Append((char)ReaderRead());
					}
					isunsigned = true;
					peek = (char)ReaderPeek();
				}
				
				if (peek == 'f' || peek == 'F') { // float value
					ReaderRead();
					suffix = "f";
					isfloat = true;
				} else if (peek == 'd' || peek == 'D') { // double type suffix (obsolete, double is default)
					ReaderRead();
					suffix = "d";
					isdouble = true;
				} else if (peek == 'm' || peek == 'M') { // decimal value
					ReaderRead();
					suffix = "m";
					isdecimal = true;
				} else if (!isdouble) {
					if (peek == 'u' || peek == 'U') {
						ReaderRead();
						suffix = "u";
						isunsigned = true;
						peek = (char)ReaderPeek();
					}
					
					if (peek == 'l' || peek == 'L') {
						ReaderRead();
						peek = (char)ReaderPeek();
						islong = true;
						if (!isunsigned && (peek == 'u' || peek == 'U')) {
							ReaderRead();
							suffix = "Lu";
							isunsigned = true;
						} else {
							suffix = isunsigned ? "uL" : "L";
						}
					}
				}
				
				string digit       = sb.ToString();
				string stringValue = prefix + digit + suffix;
				
				if (isfloat) {
					float num;
					if (float.TryParse(digit, NumberStyles.Any, CultureInfo.InvariantCulture, out num)) {
						return new Token(Tokens.Literal, x, y, stringValue, num, LiteralFormat.DecimalNumber);
					} else {
						errors.Error(y, x, String.Format("Can't parse float {0}", digit));
						return new Token(Tokens.Literal, x, y, stringValue, 0f, LiteralFormat.DecimalNumber);
					}
				}
				if (isdecimal) {
					decimal num;
					if (decimal.TryParse(digit, NumberStyles.Any, CultureInfo.InvariantCulture, out num)) {
						return new Token(Tokens.Literal, x, y, stringValue, num, LiteralFormat.DecimalNumber);
					} else {
						errors.Error(y, x, String.Format("Can't parse decimal {0}", digit));
						return new Token(Tokens.Literal, x, y, stringValue, 0m, LiteralFormat.DecimalNumber);
					}
				}
				if (isdouble) {
					double num;
					if (double.TryParse(digit, NumberStyles.Any, CultureInfo.InvariantCulture, out num)) {
						return new Token(Tokens.Literal, x, y, stringValue, num, LiteralFormat.DecimalNumber);
					} else {
						errors.Error(y, x, String.Format("Can't parse double {0}", digit));
						return new Token(Tokens.Literal, x, y, stringValue, 0d, LiteralFormat.DecimalNumber);
					}
				}
				
				// Try to determine a parsable value using ranges.
				ulong result;
				if (ishex) {
					if (!ulong.TryParse(digit, NumberStyles.HexNumber, null, out result)) {
						errors.Error(y, x, String.Format("Can't parse hexadecimal constant {0}", digit));
						return new Token(Tokens.Literal, x, y, stringValue.ToString(), 0, LiteralFormat.HexadecimalNumber);
					}
				} else {
					if (!ulong.TryParse(digit, NumberStyles.Integer, null, out result)) {
						errors.Error(y, x, String.Format("Can't parse integral constant {0}", digit));
						return new Token(Tokens.Literal, x, y, stringValue.ToString(), 0, LiteralFormat.DecimalNumber);
					}
				}
				
				if (result > long.MaxValue) {
					islong     = true;
					isunsigned = true;
				} else if (result > uint.MaxValue) {
					islong = true;
				} else if (islong == false && result > int.MaxValue) {
					isunsigned = true;
				}
				
				Token token;
				
				LiteralFormat literalFormat = ishex ? LiteralFormat.HexadecimalNumber : LiteralFormat.DecimalNumber;
				if (islong) {
					if (isunsigned) {
						ulong num;
						if (ulong.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num)) {
							token = new Token(Tokens.Literal, x, y, stringValue, num, literalFormat);
						} else {
							errors.Error(y, x, String.Format("Can't parse unsigned long {0}", digit));
							token = new Token(Tokens.Literal, x, y, stringValue, 0UL, literalFormat);
						}
					} else {
						long num;
						if (long.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num)) {
							token = new Token(Tokens.Literal, x, y, stringValue, num, literalFormat);
						} else {
							errors.Error(y, x, String.Format("Can't parse long {0}", digit));
							token = new Token(Tokens.Literal, x, y, stringValue, 0L, literalFormat);
						}
					}
				} else {
					if (isunsigned) {
						uint num;
						if (uint.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num)) {
							token = new Token(Tokens.Literal, x, y, stringValue, num, literalFormat);
						} else {
							errors.Error(y, x, String.Format("Can't parse unsigned int {0}", digit));
							token = new Token(Tokens.Literal, x, y, stringValue, (uint)0, literalFormat);
						}
					} else {
						int num;
						if (int.TryParse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number, CultureInfo.InvariantCulture, out num)) {
							token = new Token(Tokens.Literal, x, y, stringValue, num, literalFormat);
						} else {
							errors.Error(y, x, String.Format("Can't parse int {0}", digit));
							token = new Token(Tokens.Literal, x, y, stringValue, 0, literalFormat);
						}
					}
				}
				token.next = nextToken;
				return token;
			}
		}
		
		Token ReadString()
		{
			int x = Col - 1;
			int y = Line;
			
			sb.Length = 0;
			originalValue.Length = 0;
			originalValue.Append('"');
			bool doneNormally = false;
			int nextChar;
			while ((nextChar = ReaderRead()) != -1) {
				char ch = (char)nextChar;
				
				if (ch == '"') {
					doneNormally = true;
					originalValue.Append('"');
					break;
				}
				
				if (ch == '\\') {
					originalValue.Append('\\');
					string surrogatePair;
					originalValue.Append(ReadEscapeSequence(out ch, out surrogatePair));
					if (surrogatePair != null) {
						sb.Append(surrogatePair);
					} else {
						sb.Append(ch);
					}
				} else if (HandleLineEnd(ch)) {
					// call HandleLineEnd to ensure line numbers are still correct after the error
					errors.Error(y, x, String.Format("No new line is allowed inside a string literal"));
					break;
				} else {
					originalValue.Append(ch);
					sb.Append(ch);
				}
			}
			
			if (!doneNormally) {
				errors.Error(y, x, String.Format("End of file reached inside string literal"));
			}
			
			return new Token(Tokens.Literal, x, y, originalValue.ToString(), sb.ToString(), LiteralFormat.StringLiteral);
		}
		
		Token ReadVerbatimString()
		{
			sb.Length            = 0;
			originalValue.Length = 0;
			originalValue.Append("@\"");
			Location startLocation = new Location(Col - 2, Line); // @ and " already read
			int nextChar;
			while ((nextChar = ReaderRead()) != -1) {
				char ch = (char)nextChar;
				
				if (ch == '"') {
					if (ReaderPeek() != '"') {
						originalValue.Append('"');
						break;
					}
					originalValue.Append("\"\"");
					sb.Append('"');
					ReaderRead();
				} else if (HandleLineEnd(ch)) {
					sb.Append("\r\n");
					originalValue.Append("\r\n");
				} else {
					sb.Append(ch);
					originalValue.Append(ch);
				}
			}
			
			if (nextChar == -1) {
				errors.Error(startLocation.Line, startLocation.Column, String.Format("End of file reached inside verbatim string literal"));
			}
			
			return new Token(Tokens.Literal, startLocation, new Location(Col, Line), originalValue.ToString(), sb.ToString(), LiteralFormat.VerbatimStringLiteral);
		}
		
		readonly char[] escapeSequenceBuffer = new char[12];
		
		/// <summary>
		/// reads an escape sequence
		/// </summary>
		/// <param name="ch">The character represented by the escape sequence,
		/// or '\0' if there was an error or the escape sequence represents a character that
		/// can be represented only be a suggorate pair</param>
		/// <param name="surrogatePair">Null, except when the character represented
		/// by the escape sequence can only be represented by a surrogate pair (then the string
		/// contains the surrogate pair)</param>
		/// <returns>The escape sequence</returns>
		string ReadEscapeSequence(out char ch, out string surrogatePair)
		{
			surrogatePair = null;
			
			int nextChar = ReaderRead();
			if (nextChar == -1) {
				errors.Error(Line, Col, String.Format("End of file reached inside escape sequence"));
				ch = '\0';
				return String.Empty;
			}
			int number;
			char c = (char)nextChar;
			int curPos              = 1;
			escapeSequenceBuffer[0] = c;
			switch (c)  {
				case '\'':
					ch = '\'';
					break;
				case '\"':
					ch = '\"';
					break;
				case '\\':
					ch = '\\';
					break;
				case '0':
					ch = '\0';
					break;
				case 'a':
					ch = '\a';
					break;
				case 'b':
					ch = '\b';
					break;
				case 'f':
					ch = '\f';
					break;
				case 'n':
					ch = '\n';
					break;
				case 'r':
					ch = '\r';
					break;
				case 't':
					ch = '\t';
					break;
				case 'v':
					ch = '\v';
					break;
				case 'u':
				case 'x':
					// 16 bit unicode character
					c = (char)ReaderRead();
					number = GetHexNumber(c);
					escapeSequenceBuffer[curPos++] = c;
					
					if (number < 0) {
						errors.Error(Line, Col - 1, String.Format("Invalid char in literal : {0}", c));
					}
					for (int i = 0; i < 3; ++i) {
						if (IsHex((char)ReaderPeek())) {
							c = (char)ReaderRead();
							int idx = GetHexNumber(c);
							escapeSequenceBuffer[curPos++] = c;
							number = 16 * number + idx;
						} else {
							break;
						}
					}
					ch = (char)number;
					break;
				case 'U':
					// 32 bit unicode character
					number = 0;
					for (int i = 0; i < 8; ++i) {
						if (IsHex((char)ReaderPeek())) {
							c = (char)ReaderRead();
							int idx = GetHexNumber(c);
							escapeSequenceBuffer[curPos++] = c;
							number = 16 * number + idx;
						} else {
							errors.Error(Line, Col - 1, String.Format("Invalid char in literal : {0}", (char)ReaderPeek()));
							break;
						}
					}
					if (number > 0xffff) {
						ch = '\0';
						surrogatePair = char.ConvertFromUtf32(number);
					} else {
						ch = (char)number;
					}
					break;
				default:
					errors.Error(Line, Col, String.Format("Unexpected escape sequence : {0}", c));
					ch = '\0';
					break;
			}
			return new String(escapeSequenceBuffer, 0, curPos);
		}
		
		Token ReadChar()
		{
			int x = Col - 1;
			int y = Line;
			int nextChar = ReaderRead();
			if (nextChar == -1 || HandleLineEnd((char)nextChar)) {
				errors.Error(y, x, String.Format("End of line reached inside character literal"));
				return null;
			}
			char ch = (char)nextChar;
			char chValue = ch;
			string escapeSequence = String.Empty;
			if (ch == '\\') {
				string surrogatePair;
				escapeSequence = ReadEscapeSequence(out chValue, out surrogatePair);
				if (surrogatePair != null) {
					errors.Error(y, x, String.Format("The unicode character must be represented by a surrogate pair and does not fit into a System.Char"));
				}
			}
			
			unchecked {
				if ((char)ReaderRead() != '\'') {
					errors.Error(y, x, String.Format("Char not terminated"));
				}
			}
			return new Token(Tokens.Literal, x, y, "'" + ch + escapeSequence + "'", chValue, LiteralFormat.CharLiteral);
		}
		
		Token ReadOperator(char ch)
		{
			int x = Col - 1;
			int y = Line;
			switch (ch) {
				case '+':
					switch (ReaderPeek()) {
						case '+':
							ReaderRead();
							return new Token(Tokens.Increment, x, y);
						case '=':
							ReaderRead();
							return new Token(Tokens.PlusAssign, x, y);
					}
					return new Token(Tokens.Plus, x, y);
				case '-':
					switch (ReaderPeek()) {
						case '-':
							ReaderRead();
							return new Token(Tokens.Decrement, x, y);
						case '=':
							ReaderRead();
							return new Token(Tokens.MinusAssign, x, y);
						case '>':
							ReaderRead();
							return new Token(Tokens.Pointer, x, y);
					}
					return new Token(Tokens.Minus, x, y);
				case '*':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.TimesAssign, x, y);
						default:
							break;
					}
					return new Token(Tokens.Times, x, y);
				case '/':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.DivAssign, x, y);
					}
					return new Token(Tokens.Div, x, y);
				case '%':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.ModAssign, x, y);
					}
					return new Token(Tokens.Mod, x, y);
				case '&':
					switch (ReaderPeek()) {
						case '&':
							ReaderRead();
							return new Token(Tokens.LogicalAnd, x, y);
						case '=':
							ReaderRead();
							return new Token(Tokens.BitwiseAndAssign, x, y);
					}
					return new Token(Tokens.BitwiseAnd, x, y);
				case '|':
					switch (ReaderPeek()) {
						case '|':
							ReaderRead();
							return new Token(Tokens.LogicalOr, x, y);
						case '=':
							ReaderRead();
							return new Token(Tokens.BitwiseOrAssign, x, y);
					}
					return new Token(Tokens.BitwiseOr, x, y);
				case '^':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.XorAssign, x, y);
						default:
							break;
					}
					return new Token(Tokens.Xor, x, y);
				case '!':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.NotEqual, x, y);
					}
					return new Token(Tokens.Not, x, y);
				case '~':
					return new Token(Tokens.BitwiseComplement, x, y);
				case '=':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.Equal, x, y);
						case '>':
							ReaderRead();
							return new Token(Tokens.LambdaArrow, x, y);
					}
					return new Token(Tokens.Assign, x, y);
				case '<':
					switch (ReaderPeek()) {
						case '<':
							ReaderRead();
							switch (ReaderPeek()) {
								case '=':
									ReaderRead();
									return new Token(Tokens.ShiftLeftAssign, x, y);
								default:
									break;
							}
							return new Token(Tokens.ShiftLeft, x, y);
						case '=':
							ReaderRead();
							return new Token(Tokens.LessEqual, x, y);
					}
					return new Token(Tokens.LessThan, x, y);
				case '>':
					switch (ReaderPeek()) {
							// Removed because of generics:
//						case '>':
//							ReaderRead();
//							if (ReaderPeek() != -1) {
//								switch ((char)ReaderPeek()) {
//									case '=':
//										ReaderRead();
//										return new Token(Tokens.ShiftRightAssign, x, y);
//									default:
//										break;
//								}
//							}
//							return new Token(Tokens.ShiftRight, x, y);
						case '=':
							ReaderRead();
							return new Token(Tokens.GreaterEqual, x, y);
					}
					return new Token(Tokens.GreaterThan, x, y);
				case '?':
					if (ReaderPeek() == '?') {
						ReaderRead();
						return new Token(Tokens.DoubleQuestion, x, y);
					}
					return new Token(Tokens.Question, x, y);
				case ';':
					return new Token(Tokens.Semicolon, x, y);
				case ':':
					if (ReaderPeek() == ':') {
						ReaderRead();
						return new Token(Tokens.DoubleColon, x, y);
					}
					return new Token(Tokens.Colon, x, y);
				case ',':
					return new Token(Tokens.Comma, x, y);
				case '.':
					// Prevent OverflowException when ReaderPeek returns -1
					int tmp = ReaderPeek();
					if (tmp > 0 && Char.IsDigit((char)tmp)) {
						return ReadDigit('.', Col - 1);
					}
					return new Token(Tokens.Dot, x, y);
				case ')':
					return new Token(Tokens.CloseParenthesis, x, y);
				case '(':
					return new Token(Tokens.OpenParenthesis, x, y);
				case ']':
					return new Token(Tokens.CloseSquareBracket, x, y);
				case '[':
					return new Token(Tokens.OpenSquareBracket, x, y);
				case '}':
					return new Token(Tokens.CloseCurlyBrace, x, y);
				case '{':
					return new Token(Tokens.OpenCurlyBrace, x, y);
				default:
					return null;
			}
		}
		
		void ReadComment()
		{
			switch (ReaderRead()) {
				case '*':
					ReadMultiLineComment();
					isAtLineBegin = false;
					break;
				case '/':
					if (ReaderPeek() == '/') {
						ReaderRead();
						ReadSingleLineComment(CommentType.Documentation);
					} else {
						ReadSingleLineComment(CommentType.SingleLine);
					}
					isAtLineBegin = true;
					break;
				default:
					errors.Error(Line, Col, String.Format("Error while reading comment"));
					break;
			}
		}
		
		string ReadCommentToEOL()
		{
			if (specialCommentHash == null) {
				return ReadToEndOfLine();
			}
			sb.Length = 0;
			StringBuilder curWord = new StringBuilder();
			
			int nextChar;
			while ((nextChar = ReaderRead()) != -1) {
				char ch = (char)nextChar;
				
				if (HandleLineEnd(ch)) {
					break;
				}
				
				sb.Append(ch);
				if (IsIdentifierPart(nextChar)) {
					curWord.Append(ch);
				} else {
					string tag = curWord.ToString();
					curWord.Length = 0;
					if (specialCommentHash.ContainsKey(tag)) {
						Location p = new Location(Col, Line);
						string comment = ch + ReadToEndOfLine();
						this.TagComments.Add(new TagComment(tag, comment, isAtLineBegin, p, new Location(Col, Line)));
						sb.Append(comment);
						break;
					}
				}
			}
			return sb.ToString();
		}
		
		void ReadSingleLineComment(CommentType commentType)
		{
			if (this.SkipAllComments) {
				SkipToEndOfLine();
			} else {
				specialTracker.StartComment(commentType, isAtLineBegin, new Location(Col, Line));
				specialTracker.AddString(ReadCommentToEOL());
				specialTracker.FinishComment(new Location(Col, Line));
			}
		}
		
		void ReadMultiLineComment()
		{
			int nextChar;
			if (this.SkipAllComments) {
				while ((nextChar = ReaderRead()) != -1) {
					char ch = (char)nextChar;
					if (ch == '*' && ReaderPeek() == '/') {
						ReaderRead();
						return;
					} else {
						HandleLineEnd(ch);
					}
				}
			} else {
				specialTracker.StartComment(CommentType.Block, isAtLineBegin, new Location(Col, Line));
				
				// sc* = special comment handling (TO DO markers)
				string scTag = null; // is set to non-null value when we are inside a comment marker
				StringBuilder scCurWord = new StringBuilder(); // current word, (scTag == null) or comment (when scTag != null)
				Location scStartLocation = Location.Empty;
				
				while ((nextChar = ReaderRead()) != -1) {
					char ch = (char)nextChar;
					
					if (HandleLineEnd(ch)) {
						if (scTag != null) {
							this.TagComments.Add(new TagComment(scTag, scCurWord.ToString(), isAtLineBegin, scStartLocation, new Location(Col, Line)));
							scTag = null;
						}
						scCurWord.Length = 0;
						specialTracker.AddString(Environment.NewLine);
						continue;
					}
					
					// End of multiline comment reached ?
					if (ch == '*' && ReaderPeek() == '/') {
						if (scTag != null) {
							this.TagComments.Add(new TagComment(scTag, scCurWord.ToString(), isAtLineBegin, scStartLocation, new Location(Col, Line)));
						}
						ReaderRead();
						specialTracker.FinishComment(new Location(Col, Line));
						return;
					}
					specialTracker.AddChar(ch);
					if (scTag != null || IsIdentifierPart(ch)) {
						scCurWord.Append(ch);
					} else {
						if (specialCommentHash != null && specialCommentHash.ContainsKey(scCurWord.ToString())) {
							scTag = scCurWord.ToString();
							scStartLocation = new Location(Col, Line);
						}
						scCurWord.Length = 0;
					}
				}
				specialTracker.FinishComment(new Location(Col, Line));
			}
			// Reached EOF before end of multiline comment.
			errors.Error(Line, Col, String.Format("Reached EOF before the end of a multiline comment"));
		}
		
		/// <summary>
		/// Skips to the end of the current code block.
		/// For this, the lexer must have read the next token AFTER the token opening the
		/// block (so that Lexer.Token is the block-opening token, not Lexer.LookAhead).
		/// After the call, Lexer.LookAhead will be the block-closing token.
		/// </summary>
		public override void SkipCurrentBlock(int targetToken)
		{
			int braceCount = 0;
			while (curToken != null) {
				if (curToken.kind == Tokens.OpenCurlyBrace) {
					++braceCount;
				} else if (curToken.kind == Tokens.CloseCurlyBrace) {
					if (--braceCount < 0)
						return;
				}
				lastToken = curToken;
				curToken = curToken.next;
			}
			isAtLineBegin = true;
			int nextChar;
			while ((nextChar = ReaderRead()) != -1) {
				switch (nextChar) {
					case '{':
						isAtLineBegin = false;
						braceCount++;
						break;
					case '}':
						isAtLineBegin = false;
						if (--braceCount < 0) {
							curToken = new Token(Tokens.CloseCurlyBrace, Col - 1, Line);
							return;
						}
						break;
					case '/':
						int peek = ReaderPeek();
						if (peek == '/' || peek == '*') {
							ReadComment();
						}
						isAtLineBegin = false;
						break;
					case '#':
						ReadPreProcessingDirective();
						isAtLineBegin = false;
						break;
					case '"':
						ReadString();
						isAtLineBegin = false;
						break;
					case '\'':
						ReadChar();
						isAtLineBegin = false;
						break;
					case '\r':
					case '\n':
						HandleLineEnd((char)nextChar);
						isAtLineBegin = true;
						break;
					case '@':
						int next = ReaderRead();
						if (next == -1) {
							errors.Error(Line, Col, String.Format("EOF after @"));
						} else if (next == '"') {
							ReadVerbatimString();
						}
						isAtLineBegin = false;
						break;
				}
			}
			curToken = new Token(Tokens.EOF, Col, Line);
		}
		
		public override IDictionary<string, object> ConditionalCompilationSymbols {
			get { return conditionalCompilation.Symbols; }
		}
		
		public override void SetConditionalCompilationSymbols (string symbols)
		{
			foreach (string symbol in GetSymbols (symbols)) {
				conditionalCompilation.Define (symbol);
			}
		}
		
		
		ConditionalCompilation conditionalCompilation = new ConditionalCompilation();
		
		void ReadPreProcessingDirective()
		{
			PreprocessingDirective d = ReadPreProcessingDirectiveInternal(true, true);
			this.specialTracker.AddPreprocessingDirective(d);
			
			if (EvaluateConditionalCompilation) {
				switch (d.Cmd) {
					case "#define":
						conditionalCompilation.Define(d.Arg);
						break;
					case "#undef":
						conditionalCompilation.Undefine(d.Arg);
						break;
					case "#if":
						if (!conditionalCompilation.Evaluate(d.Expression)) {
							// skip to valid #elif or #else or #endif
							int level = 1;
							while (true) {
								d = SkipToPreProcessingDirective(false, level == 1);
								if (d == null)
									break;
								if (d.Cmd == "#if") {
									level++;
								} else if (d.Cmd == "#endif") {
									level--;
									if (level == 0)
										break;
								} else if (level == 1 &&  (d.Cmd == "#else"
								                           || d.Cmd == "#elif" && conditionalCompilation.Evaluate(d.Expression)))
								{
									break;
								}
							}
							if (d != null)
								this.specialTracker.AddPreprocessingDirective(d);
						}
						break;
					case "#elif":
					case "#else":
						// we already visited the #if part or a previous #elif part, so skip until #endif
						{
							int level = 1;
							while (true) {
								d = SkipToPreProcessingDirective(false, false);
								if (d == null)
									break;
								if (d.Cmd == "#if") {
									level++;
								} else if (d.Cmd == "#endif") {
									level--;
									if (level == 0)
										break;
								}
							}
							if (d != null)
								this.specialTracker.AddPreprocessingDirective(d);
						}
						break;
				}
			}
		}
		
		PreprocessingDirective SkipToPreProcessingDirective(bool parseIfExpression, bool parseElifExpression)
		{
			int c;
			while (true) {
				PPWhitespace();
				c = ReaderRead();
				if (c == -1) {
					errors.Error(Line, Col, String.Format("Reached EOF but expected #endif"));
					return null;
				} else if (c == '#') {
					break;
				} else {
					SkipToEndOfLine();
				}
			}
			return ReadPreProcessingDirectiveInternal(parseIfExpression, parseElifExpression);
		}
		
		PreprocessingDirective ReadPreProcessingDirectiveInternal(bool parseIfExpression, bool parseElifExpression)
		{
			Location start = new Location(Col - 1, Line);
			
			// skip spaces between # and the directive
			PPWhitespace();
			
			bool canBeKeyword;
			string directive = ReadIdent('#', out canBeKeyword);
			
			PPWhitespace();
			if (parseIfExpression && directive == "#if" || parseElifExpression && directive == "#elif") {
				recordedText.Length = 0;
				recordRead = true;
				Ast.Expression expr = PPExpression();
				string arg = recordedText.ToString ();
				recordRead = false;
				
				Location endLocation = new Location(Col, Line);
				int c = ReaderRead();
				if (c >= 0 && !HandleLineEnd((char)c)) {
					if (c == '/' && ReaderRead() == '/') {
						// comment to end of line
					} else {
						errors.Error(Col, Line, "Expected end of line");
					}
					SkipToEndOfLine(); // skip comment
				}
				return new PreprocessingDirective(directive, arg, start, endLocation) { Expression = expr, LastLineEnd = lastLineEnd };
			} else {
				Location endLocation = new Location(Col, Line);
				string arg = ReadToEndOfLine();
				endLocation.Column += arg.Length;
				int pos = arg.IndexOf("//");
				if (pos >= 0)
					arg = arg.Substring(0, pos);
				arg = arg.Trim();
				return new PreprocessingDirective(directive, arg, start, endLocation) { LastLineEnd = lastLineEnd };
			}
		}
		
		void PPWhitespace()
		{
			while (ReaderPeek() == ' ' || ReaderPeek() == '\t')
				ReaderRead();
		}
		
		Ast.Expression PPExpression()
		{
			Ast.Expression expr = PPAndExpression();
			while (ReaderPeek() == '|') {
				Token token = ReadOperator((char)ReaderRead());
				if (token == null || token.kind != Tokens.LogicalOr) {
					return expr;
				}
				Ast.Expression expr2 = PPAndExpression();
				expr = new Ast.BinaryOperatorExpression(expr, Ast.BinaryOperatorType.LogicalOr, expr2);
			}
			return expr;
		}
		
		Ast.Expression PPAndExpression()
		{
			Ast.Expression expr = PPEqualityExpression();
			while (ReaderPeek() == '&') {
				Token token = ReadOperator((char)ReaderRead());
				if (token == null || token.kind != Tokens.LogicalAnd) {
					break;
				}
				Ast.Expression expr2 = PPEqualityExpression();
				expr = new Ast.BinaryOperatorExpression(expr, Ast.BinaryOperatorType.LogicalAnd, expr2);
			}
			return expr;
		}
		
		Ast.Expression PPEqualityExpression()
		{
			Ast.Expression expr = PPUnaryExpression();
			while (ReaderPeek() == '=' || ReaderPeek() == '!') {
				Token token = ReadOperator((char)ReaderRead());
				if (token == null || token.kind != Tokens.Equals && token.kind != Tokens.NotEqual) {
					break;
				}
				Ast.Expression expr2 = PPUnaryExpression();
				expr = new Ast.BinaryOperatorExpression(expr, token.kind == Tokens.Equals ? Ast.BinaryOperatorType.Equality : Ast.BinaryOperatorType.InEquality, expr2);
			}
			return expr;
		}
		
		Ast.Expression PPUnaryExpression()
		{
			PPWhitespace();
			if (ReaderPeek() == '!') {
				ReaderRead();
				PPWhitespace();
				return new Ast.UnaryOperatorExpression(PPUnaryExpression(), Ast.UnaryOperatorType.Not);
			} else {
				return PPPrimaryExpression();
			}
		}
		
		Ast.Expression PPPrimaryExpression()
		{
			int c = ReaderRead();
			if (c < 0)
				return Ast.Expression.Null;
			if (c == '(') {
				Ast.Expression expr = new Ast.ParenthesizedExpression(PPExpression());
				PPWhitespace();
				if (ReaderRead() != ')')
					errors.Error(Col, Line, "Expected ')'");
				PPWhitespace();
				return expr;
			} else {
				if (c != '_' && !char.IsLetterOrDigit((char)c) && c != '\\')
					errors.Error(Col, Line, "Expected conditional symbol");
				bool canBeKeyword;
				string symbol = ReadIdent((char)c, out canBeKeyword);
				PPWhitespace();
				if (canBeKeyword && symbol == "true")
					return new Ast.PrimitiveExpression(true, "true");
				else if (canBeKeyword && symbol == "false")
					return new Ast.PrimitiveExpression(false, "false");
				else
					return new Ast.IdentifierExpression(symbol);
			}
		}
	}
}
