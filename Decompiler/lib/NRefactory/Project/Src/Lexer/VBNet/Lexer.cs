// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ICSharpCode.NRefactory.Parser.VB
{
	internal sealed class Lexer : AbstractLexer
	{
		bool lineEnd = true;
		
		public Lexer(TextReader reader) : base(reader)
		{
		}
		
		public override Token NextToken()
		{
			if (curToken == null) { // first call of NextToken()
				curToken = Next();
				specialTracker.InformToken(curToken.kind);
				//Console.WriteLine("Tok:" + Tokens.GetTokenString(curToken.kind) + " --- " + curToken.val);
				return curToken;
			}
			
			lastToken = curToken;
			
			if (curToken.next == null) {
				curToken.next = Next();
				specialTracker.InformToken(curToken.next.kind);
			}
			
			curToken = curToken.next;
			
			if (curToken.kind == Tokens.EOF && !(lastToken.kind == Tokens.EOL)) { // be sure that before EOF there is an EOL token
				curToken = new Token(Tokens.EOL, curToken.col, curToken.line, "\n");
				specialTracker.InformToken(curToken.kind);
				curToken.next = new Token(Tokens.EOF, curToken.col, curToken.line, "\n");
				specialTracker.InformToken(curToken.next.kind);
			}
			//Console.WriteLine("Tok:" + Tokens.GetTokenString(curToken.kind) + " --- " + curToken.val);
			return curToken;
		}
		
		protected override Token Next()
		{
			unchecked {
				int nextChar;
				while ((nextChar = ReaderRead()) != -1) {
					char ch = (char)nextChar;
					if (Char.IsWhiteSpace(ch)) {
						int x = Col - 1;
						int y = Line;
						if (HandleLineEnd(ch)) {
							if (lineEnd) {
								// second line end before getting to a token
								// -> here was a blank line
								specialTracker.AddEndOfLine(new Location(x, y));
							} else {
								lineEnd = true;
								return new Token(Tokens.EOL, x, y);
							}
						}
						continue;
					}
					if (ch == '_') {
						if (ReaderPeek() == -1) {
							errors.Error(Line, Col, String.Format("No EOF expected after _"));
							return new Token(Tokens.EOF);
						}
						if (!Char.IsWhiteSpace((char)ReaderPeek())) {
							int x = Col - 1;
							int y = Line;
							string s = ReadIdent('_');
							lineEnd = false;
							return new Token(Tokens.Identifier, x, y, s);
						}
						ch = (char)ReaderRead();
						
						bool oldLineEnd = lineEnd;
						lineEnd = false;
						while (Char.IsWhiteSpace(ch)) {
							if (HandleLineEnd(ch)) {
								lineEnd = true;
								break;
							}
							if (ReaderPeek() != -1) {
								ch = (char)ReaderRead();
							} else {
								errors.Error(Line, Col, String.Format("No EOF expected after _"));
								return new Token(Tokens.EOF);
							}
						}
						if (!lineEnd) {
							errors.Error(Line, Col, String.Format("Return expected"));
						}
						lineEnd = oldLineEnd;
						continue;
					}
					
					if (ch == '#') {
						while (Char.IsWhiteSpace((char)ReaderPeek())) {
							ReaderRead();
						}
						if (Char.IsDigit((char)ReaderPeek())) {
							int x = Col - 1;
							int y = Line;
							string s = ReadDate();
							DateTime time = new DateTime(1, 1, 1, 0, 0, 0);
							try {
								time = DateTime.Parse(s, System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault);
							} catch (Exception e) {
								errors.Error(Line, Col, String.Format("Invalid date time {0}", e));
							}
							return new Token(Tokens.LiteralDate, x, y, s, time);
						} else {
							ReadPreprocessorDirective();
							continue;
						}
					}
					
					if (ch == '[') { // Identifier
						lineEnd = false;
						if (ReaderPeek() == -1) {
							errors.Error(Line, Col, String.Format("Identifier expected"));
						}
						ch = (char)ReaderRead();
						if (ch == ']' || Char.IsWhiteSpace(ch)) {
							errors.Error(Line, Col, String.Format("Identifier expected"));
						}
						int x = Col - 1;
						int y = Line;
						string s = ReadIdent(ch);
						if (ReaderPeek() == -1) {
							errors.Error(Line, Col, String.Format("']' expected"));
						}
						ch = (char)ReaderRead();
						if (!(ch == ']')) {
							errors.Error(Line, Col, String.Format("']' expected"));
						}
						return new Token(Tokens.Identifier, x, y, s);
					}
					if (Char.IsLetter(ch)) {
						int x = Col - 1;
						int y = Line;
						string s = ReadIdent(ch);
						int keyWordToken = Keywords.GetToken(s);
						if (keyWordToken >= 0) {
							lineEnd = false;
							return new Token(keyWordToken, x, y, s);
						}
						
						// handle 'REM' comments
						if (s.Equals("REM", StringComparison.InvariantCultureIgnoreCase)) {
							ReadComment();
							if (!lineEnd) {
								lineEnd = true;
								return new Token(Tokens.EOL, Col, Line, "\n");
							}
							continue;
						}
						
						lineEnd = false;
						return new Token(Tokens.Identifier, x, y, s);
						
					}
					if (Char.IsDigit(ch)) {
						lineEnd = false;
						return ReadDigit(ch, Col - 1);
					}
					if (ch == '&') {
						lineEnd = false;
						if (ReaderPeek() == -1) {
							return ReadOperator('&');
						}
						ch = (char)ReaderPeek();
						if (Char.ToUpper(ch, CultureInfo.InvariantCulture) == 'H' || Char.ToUpper(ch, CultureInfo.InvariantCulture) == 'O') {
							return ReadDigit('&', Col - 1);
						}
						return ReadOperator('&');
					}
					if (ch == '\'' || ch == '\u2018' || ch == '\u2019') {
						int x = Col - 1;
						int y = Line;
						ReadComment();
						if (!lineEnd) {
							lineEnd = true;
							return new Token(Tokens.EOL, x, y, "\n");
						}
						continue;
					}
					if (ch == '"') {
						lineEnd = false;
						int x = Col - 1;
						int y = Line;
						string s = ReadString();
						if (ReaderPeek() != -1 && (ReaderPeek() == 'C' || ReaderPeek() == 'c')) {
							ReaderRead();
							if (s.Length != 1) {
								errors.Error(Line, Col, String.Format("Chars can only have Length 1 "));
							}
							if (s.Length == 0) {
								s = "\0";
							}
							return new Token(Tokens.LiteralCharacter, x, y, '"' + s  + "\"C", s[0]);
						}
						return new Token(Tokens.LiteralString, x, y, '"' + s + '"', s);
					}
					Token token = ReadOperator(ch);
					if (token != null) {
						lineEnd = false;
						return token;
					}
					errors.Error(Line, Col, String.Format("Unknown char({0}) which can't be read", ch));
				}
				
				return new Token(Tokens.EOF);
			}
		}
		
		string ReadIdent(char ch)
		{
			sb.Length = 0;
			sb.Append(ch);
			int peek;
			while ((peek = ReaderPeek()) != -1 && (Char.IsLetterOrDigit(ch = (char)peek) || ch == '_')) {
				ReaderRead();
				sb.Append(ch.ToString());
			}
			if (peek == -1) {
				return sb.ToString();
			}
			
			if ("%&@!#$".IndexOf((char)peek) != -1) {
				ReaderRead();
			}
			return sb.ToString();
		}
		
		char PeekUpperChar()
		{
			return Char.ToUpper((char)ReaderPeek(), CultureInfo.InvariantCulture);
		}
		
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1818:DoNotConcatenateStringsInsideLoops")]
		Token ReadDigit(char ch, int x)
		{
			sb.Length = 0;
			sb.Append(ch);
			
			int y = Line;
			string digit = "";
			if (ch != '&') {
				digit += ch;
			}
			
			bool ishex      = false;
			bool isokt      = false;
			bool issingle   = false;
			bool isdouble   = false;
			bool isdecimal  = false;
			
			if (ReaderPeek() == -1) {
				if (ch == '&') {
					errors.Error(Line, Col, String.Format("digit expected"));
				}
				return new Token(Tokens.LiteralInteger, x, y, sb.ToString() ,ch - '0');
			}
			if (ch == '.') {
				if (Char.IsDigit((char)ReaderPeek())) {
					isdouble = true; // double is default
					if (ishex || isokt) {
						errors.Error(Line, Col, String.Format("No hexadecimal or oktadecimal floating point values allowed"));
					}
					while (ReaderPeek() != -1 && Char.IsDigit((char)ReaderPeek())){ // read decimal digits beyond the dot
						digit += (char)ReaderRead();
					}
				}
			} else if (ch == '&' && PeekUpperChar() == 'H') {
				const string hex = "0123456789ABCDEF";
				sb.Append((char)ReaderRead()); // skip 'H'
				while (ReaderPeek() != -1 && hex.IndexOf(PeekUpperChar()) != -1) {
					ch = (char)ReaderRead();
					sb.Append(ch);
					digit += Char.ToUpper(ch, CultureInfo.InvariantCulture);
				}
				ishex = true;
			} else if (ReaderPeek() != -1 && ch == '&' && PeekUpperChar() == 'O') {
				const string okt = "01234567";
				sb.Append((char)ReaderRead()); // skip 'O'
				while (ReaderPeek() != -1 && okt.IndexOf(PeekUpperChar()) != -1) {
					ch = (char)ReaderRead();
					sb.Append(ch);
					digit += Char.ToUpper(ch, CultureInfo.InvariantCulture);
				}
				isokt = true;
			} else {
				while (ReaderPeek() != -1 && Char.IsDigit((char)ReaderPeek())) {
					ch = (char)ReaderRead();;
					digit += ch;
					sb.Append(ch);
				}
			}
			
			if (digit.Length == 0) {
				errors.Error(Line, Col, String.Format("digit expected"));
				return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), 0);
			}
			
			if (ReaderPeek() != -1 && ("%&SILU".IndexOf(PeekUpperChar()) != -1 || ishex || isokt)) {
				ch = (char)ReaderPeek();
				sb.Append(ch);
				ch = Char.ToUpper(ch, CultureInfo.InvariantCulture);
				bool unsigned = ch == 'U';
				if (unsigned) {
					ReaderRead(); // read the U
					ch = (char)ReaderPeek();
					sb.Append(ch);
					ch = Char.ToUpper(ch, CultureInfo.InvariantCulture);
					if (ch != 'I' && ch != 'L' && ch != 'S') {
						errors.Error(Line, Col, "Invalid type character: U" + ch);
					}
				}
				try {
					if (isokt) {
						ReaderRead();
						ulong number = 0L;
						for (int i = 0; i < digit.Length; ++i) {
							number = number * 8 + digit[i] - '0';
						}
						if (ch == 'S') {
							if (unsigned)
								return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (ushort)number);
							else
								return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (short)number);
						} else if (ch == '%' || ch == 'I') {
							if (unsigned)
								return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (uint)number);
							else
								return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (int)number);
						} else if (ch == '&' || ch == 'L') {
							if (unsigned)
								return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (ulong)number);
							else
								return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (long)number);
						} else {
							if (number > uint.MaxValue) {
								return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), unchecked((long)number));
							} else {
								return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), unchecked((int)number));
							}
						}
					}
					if (ch == 'S') {
						ReaderRead();
						if (unsigned)
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), UInt16.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
						else
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int16.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
					} else if (ch == '%' || ch == 'I') {
						ReaderRead();
						if (unsigned)
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), UInt32.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
						else
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int32.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
					} else if (ch == '&' || ch == 'L') {
						ReaderRead();
						if (unsigned)
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), UInt64.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
						else
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int64.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
					} else if (ishex) {
						ulong number = UInt64.Parse(digit, NumberStyles.HexNumber);
						if (number > uint.MaxValue) {
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), unchecked((long)number));
						} else {
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), unchecked((int)number));
						}
					}
				} catch (OverflowException ex) {
					errors.Error(Line, Col, ex.Message);
					return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), 0);
				}
			}
			Token nextToken = null; // if we accedently read a 'dot'
			if (!isdouble && ReaderPeek() == '.') { // read floating point number
				ReaderRead();
				if (ReaderPeek() != -1 && Char.IsDigit((char)ReaderPeek())) {
					isdouble = true; // double is default
					if (ishex || isokt) {
						errors.Error(Line, Col, String.Format("No hexadecimal or oktadecimal floating point values allowed"));
					}
					digit += '.';
					while (ReaderPeek() != -1 && Char.IsDigit((char)ReaderPeek())){ // read decimal digits beyond the dot
						digit += (char)ReaderRead();
					}
				} else {
					nextToken = new Token(Tokens.Dot, Col - 1, Line);
				}
			}
			
			if (ReaderPeek() != -1 && PeekUpperChar() == 'E') { // read exponent
				isdouble = true;
				digit +=  (char)ReaderRead();
				if (ReaderPeek() != -1 && (ReaderPeek() == '-' || ReaderPeek() == '+')) {
					digit += (char)ReaderRead();
				}
				while (ReaderPeek() != -1 && Char.IsDigit((char)ReaderPeek())) { // read exponent value
					digit += (char)ReaderRead();
				}
			}
			
			if (ReaderPeek() != -1) {
				switch (PeekUpperChar()) {
					case 'R':
					case '#':
						ReaderRead();
						isdouble = true;
						break;
					case 'D':
					case '@':
						ReaderRead();
						isdecimal = true;
						break;
					case 'F':
					case '!':
						ReaderRead();
						issingle = true;
						break;
				}
			}
			
			try {
				if (issingle) {
					return new Token(Tokens.LiteralSingle, x, y, sb.ToString(), Single.Parse(digit, CultureInfo.InvariantCulture));
				}
				if (isdecimal) {
					return new Token(Tokens.LiteralDecimal, x, y, sb.ToString(), Decimal.Parse(digit, NumberStyles.Currency | NumberStyles.AllowExponent, CultureInfo.InvariantCulture));
				}
				if (isdouble) {
					return new Token(Tokens.LiteralDouble, x, y, sb.ToString(), Double.Parse(digit, CultureInfo.InvariantCulture));
				}
			} catch (FormatException) {
				errors.Error(Line, Col, String.Format("{0} is not a parseable number", digit));
				if (issingle)
					return new Token(Tokens.LiteralSingle, x, y, sb.ToString(), 0f);
				if (isdecimal)
					return new Token(Tokens.LiteralDecimal, x, y, sb.ToString(), 0m);
				if (isdouble)
					return new Token(Tokens.LiteralDouble, x, y, sb.ToString(), 0.0);
			}
			Token token;
			try {
				token = new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int32.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
			} catch (Exception) {
				try {
					token = new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int64.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
				} catch (FormatException) {
					errors.Error(Line, Col, String.Format("{0} is not a parseable number", digit));
					// fallback, when nothing helps :)
					token = new Token(Tokens.LiteralInteger, x, y, sb.ToString(), 0);
				} catch (OverflowException) {
					errors.Error(Line, Col, String.Format("{0} is too long for a integer literal", digit));
					// fallback, when nothing helps :)
					token = new Token(Tokens.LiteralInteger, x, y, sb.ToString(), 0);
				}
			}
			token.next = nextToken;
			return token;
		}
		
		void ReadPreprocessorDirective()
		{
			Location start = new Location(Col - 1, Line);
			string directive = ReadIdent('#');
			string argument  = ReadToEndOfLine();
			this.specialTracker.AddPreprocessingDirective(directive, argument.Trim(), start, new Location(start.X + directive.Length + argument.Length, start.Y));
		}
		
		string ReadDate()
		{
			char ch = '\0';
			sb.Length = 0;
			int nextChar;
			while ((nextChar = ReaderRead()) != -1) {
				ch = (char)nextChar;
				if (ch == '#') {
					break;
				} else if (ch == '\n') {
					errors.Error(Line, Col, String.Format("No return allowed inside Date literal"));
				} else {
					sb.Append(ch);
				}
			}
			if (ch != '#') {
				errors.Error(Line, Col, String.Format("End of File reached before Date literal terminated"));
			}
			return sb.ToString();
		}
		
		string ReadString()
		{
			char ch = '\0';
			sb.Length = 0;
			int nextChar;
			while ((nextChar = ReaderRead()) != -1) {
				ch = (char)nextChar;
				if (ch == '"') {
					if (ReaderPeek() != -1 && ReaderPeek() == '"') {
						sb.Append('"');
						ReaderRead();
					} else {
						break;
					}
				} else if (ch == '\n') {
					errors.Error(Line, Col, String.Format("No return allowed inside String literal"));
				} else {
					sb.Append(ch);
				}
			}
			if (ch != '"') {
				errors.Error(Line, Col, String.Format("End of File reached before String terminated "));
			}
			return sb.ToString();
		}
		
		void ReadComment()
		{
			Location startPos = new Location(Col, Line);
			sb.Length = 0;
			StringBuilder curWord = specialCommentHash != null ? new StringBuilder() : null;
			int missingApostrophes = 2; // no. of ' missing until it is a documentation comment
			int nextChar;
			while ((nextChar = ReaderRead()) != -1) {
				char ch = (char)nextChar;
				
				if (HandleLineEnd(ch)) {
					break;
				}
				
				sb.Append(ch);
				
				if (missingApostrophes > 0) {
					if (ch == '\'' || ch == '\u2018' || ch == '\u2019') {
						if (--missingApostrophes == 0) {
							specialTracker.StartComment(CommentType.Documentation, startPos);
							sb.Length = 0;
						}
					} else {
						specialTracker.StartComment(CommentType.SingleLine, startPos);
						missingApostrophes = 0;
					}
				}
				
				if (specialCommentHash != null) {
					if (Char.IsLetter(ch)) {
						curWord.Append(ch);
					} else {
						string tag = curWord.ToString();
						curWord.Length = 0;
						if (specialCommentHash.ContainsKey(tag)) {
							Location p = new Location(Col, Line);
							string comment = ch + ReadToEndOfLine();
							this.TagComments.Add(new TagComment(tag, comment, p, new Location(Col, Line)));
							sb.Append(comment);
							break;
						}
					}
				}
			}
			if (missingApostrophes > 0) {
				specialTracker.StartComment(CommentType.SingleLine, startPos);
			}
			specialTracker.AddString(sb.ToString());
			specialTracker.FinishComment(new Location(Col, Line));
		}
		
		Token ReadOperator(char ch)
		{
			int x = Col - 1;
			int y = Line;
			switch(ch) {
				case '+':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.PlusAssign, x, y);
						default:
							break;
					}
					return new Token(Tokens.Plus, x, y);
				case '-':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.MinusAssign, x, y);
						default:
							break;
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
					return new Token(Tokens.Times, x, y, "*");
				case '/':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.DivAssign, x, y);
						default:
							break;
					}
					return new Token(Tokens.Div, x, y);
				case '\\':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.DivIntegerAssign, x, y);
						default:
							break;
					}
					return new Token(Tokens.DivInteger, x, y);
				case '&':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.ConcatStringAssign, x, y);
						default:
							break;
					}
					return new Token(Tokens.ConcatString, x, y);
				case '^':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.PowerAssign, x, y);
						default:
							break;
					}
					return new Token(Tokens.Power, x, y);
				case ':':
					return new Token(Tokens.Colon, x, y);
				case '=':
					return new Token(Tokens.Assign, x, y);
				case '<':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.LessEqual, x, y);
						case '>':
							ReaderRead();
							return new Token(Tokens.NotEqual, x, y);
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
					}
					return new Token(Tokens.LessThan, x, y);
				case '>':
					switch (ReaderPeek()) {
						case '=':
							ReaderRead();
							return new Token(Tokens.GreaterEqual, x, y);
						case '>':
							ReaderRead();
							if (ReaderPeek() != -1) {
								switch (ReaderPeek()) {
									case '=':
										ReaderRead();
										return new Token(Tokens.ShiftRightAssign, x, y);
									default:
										break;
								}
							}
							return new Token(Tokens.ShiftRight, x, y);
					}
					return new Token(Tokens.GreaterThan, x, y);
				case ',':
					return new Token(Tokens.Comma, x, y);
				case '.':
					// Prevent OverflowException when Peek returns -1
					int tmp = ReaderPeek();
					if (tmp > 0 && Char.IsDigit((char)tmp)) {
						return ReadDigit('.', Col);
					}
					return new Token(Tokens.Dot, x, y);
				case '(':
					return new Token(Tokens.OpenParenthesis, x, y);
				case ')':
					return new Token(Tokens.CloseParenthesis, x, y);
				case '{':
					return new Token(Tokens.OpenCurlyBrace, x, y);
				case '}':
					return new Token(Tokens.CloseCurlyBrace, x, y);
				case '?':
					return new Token(Tokens.QuestionMark, x, y);
			}
			return null;
		}
		
		public override void SkipCurrentBlock(int targetToken)
		{
			int lastKind = -1;
			int kind = base.lastToken.kind;
			while (kind != Tokens.EOF &&
			       !(lastKind == Tokens.End && kind == targetToken))
			{
				lastKind = kind;
				NextToken();
				kind = lastToken.kind;
			}
		}
	}
}
