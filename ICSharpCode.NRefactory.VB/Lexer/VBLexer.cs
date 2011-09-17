// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ICSharpCode.NRefactory.VB.Parser
{
	public class VBLexer : IDisposable
	{
		bool lineEnd = true;
		bool isAtLineBegin = false; // TODO: handle line begin, if neccessarry
		bool misreadExclamationMarkAsTypeCharacter;
		bool encounteredLineContinuation;
		
		ExpressionFinder ef;
		
		bool inXmlMode;
		
		Stack<XmlModeInfo> xmlModeStack = new Stack<XmlModeInfo>();
		
		public VBLexer(TextReader reader)
		{
			this.reader = new LATextReader(reader);
			ef = new ExpressionFinder();
		}
		
		public VBLexer(TextReader reader, VBLexerMemento state) : this(reader)
		{
			SetInitialLocation(new TextLocation(state.Line, state.Column));
			lastToken = new Token(state.PrevTokenKind, 0, 0);
			ef = new ExpressionFinder(state.ExpressionFinder);
			lineEnd = state.LineEnd;
			isAtLineBegin = state.IsAtLineBegin;
			encounteredLineContinuation = state.EncounteredLineContinuation;
			misreadExclamationMarkAsTypeCharacter = state.MisreadExclamationMarkAsTypeCharacter;
			xmlModeStack = new Stack<XmlModeInfo>(state.XmlModeInfoStack.Select(i => (XmlModeInfo)i.Clone()).Reverse());
			inXmlMode = state.InXmlMode;
		}
		
		Token NextInternal()
		{
			if (misreadExclamationMarkAsTypeCharacter) {
				misreadExclamationMarkAsTypeCharacter = false;
				return new Token(Tokens.ExclamationMark, Col - 1, Line);
			}
			
			unchecked {
				while (true) {
					TextLocation startLocation = new TextLocation(Line, Col);
					int nextChar = ReaderRead();
					if (nextChar == -1)
						return new Token(Tokens.EOF, Col, Line, string.Empty);
					char ch = (char)nextChar;
					#region XML mode
					CheckXMLState(startLocation);
					if (inXmlMode && xmlModeStack.Peek().level <= 0 && !xmlModeStack.Peek().isDocumentStart && !xmlModeStack.Peek().inXmlTag) {
						XmlModeInfo info = xmlModeStack.Peek();
						int peek = nextChar;
						while (true) {
							int step = -1;
							while (peek != -1 && XmlConvert.IsWhitespaceChar((char)peek)) {
								step++;
								peek = ReaderPeek(step);
							}
							
							if (peek == '<' && (ReaderPeek(step + 1) == '!' || ReaderPeek(step + 1) == '?')) {
								char lastCh = '\0';
								for (int i = 0; i < step + 2; i++)
									lastCh = (char)ReaderRead();
								
								if (lastCh == '!')
									return ReadXmlCommentOrCData(Col - 2, Line);
								else
									return ReadXmlProcessingInstruction(Col - 2, Line);
							}
							
							break;
						}
						inXmlMode = false;
						xmlModeStack.Pop();
					}
					if (inXmlMode) {
						XmlModeInfo info = xmlModeStack.Peek();
						int x = Col - 1;
						int y = Line;
						switch (ch) {
							case '<':
								if (ReaderPeek() == '/') {
									ReaderRead();
									info.inXmlCloseTag = true;
									return new Token(Tokens.XmlOpenEndTag, new TextLocation(y, x), new TextLocation(Line, Col));
								}
								if (ReaderPeek() == '%' && ReaderPeek(1) == '=') {
									inXmlMode = false;
									ReaderRead(); ReaderRead();
									return new Token(Tokens.XmlStartInlineVB, new TextLocation(y, x), new TextLocation(Line, Col));
								}
								if (ReaderPeek() == '?') {
									ReaderRead();
									Token t = ReadXmlProcessingInstruction(x, y);
									return t;
								}
								if (ReaderPeek() == '!') {
									ReaderRead();
									Token token = ReadXmlCommentOrCData(x, y);
									return token;
								}
								info.level++;
								info.isDocumentStart = false;
								info.inXmlTag = true;
								return new Token(Tokens.XmlOpenTag, x, y);
							case '/':
								if (ReaderPeek() == '>') {
									ReaderRead();
									info.inXmlTag = false;
									info.level--;
									return new Token(Tokens.XmlCloseTagEmptyElement, new TextLocation(y, x), new TextLocation(Line, Col));
								}
								break;
							case '>':
								if (info.inXmlCloseTag)
									info.level--;
								info.inXmlTag = info.inXmlCloseTag = false;
								return new Token(Tokens.XmlCloseTag, x, y);
							case '=':
								return new Token(Tokens.Assign, x, y);
							case '\'':
							case '"':
								string s = ReadXmlString(ch);
								return new Token(Tokens.LiteralString, x, y, ch + s + ch, s);
							default:
								if (info.inXmlCloseTag || info.inXmlTag) {
									if (XmlConvert.IsWhitespaceChar(ch))
										continue;
									return new Token(Tokens.Identifier, x, y, ReadXmlIdent(ch));
								} else {
									string content = ReadXmlContent(ch);
									return new Token(Tokens.XmlContent, startLocation, new TextLocation(Line, Col), content, null);
								}
						}
						#endregion
					} else {
						#region Standard Mode
						if (Char.IsWhiteSpace(ch)) {
							if (HandleLineEnd(ch)) {
								if (lineEnd) {
									// second line end before getting to a token
									// -> here was a blank line
//									specialTracker.AddEndOfLine(startLocation);
								} else {
									lineEnd = true;
									return new Token(Tokens.EOL, startLocation, new TextLocation(Line, Col), null, null);
								}
							}
							continue;
						}
						if (ch == '_') {
							if (ReaderPeek() == -1) {
								errors.Error(Line, Col, String.Format("No EOF expected after _"));
								return new Token(Tokens.EOF, Col, Line, string.Empty);
							}
							if (!Char.IsWhiteSpace((char)ReaderPeek())) {
								int x = Col - 1;
								int y = Line;
								string s = ReadIdent('_');
								lineEnd = false;
								return new Token(Tokens.Identifier, x, y, s);
							}
							encounteredLineContinuation = true;
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
									return new Token(Tokens.EOF, Col, Line, string.Empty);
								}
							}
							if (!lineEnd) {
								errors.Error(Line, Col, String.Format("NewLine expected"));
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
							char typeCharacter;
							string s = ReadIdent(ch, out typeCharacter);
							if (typeCharacter == '\0') {
								int keyWordToken = Keywords.GetToken(s);
								if (keyWordToken >= 0) {
									// handle 'REM' comments
									if (keyWordToken == Tokens.Rem) {
										ReadComment();
										if (!lineEnd) {
											lineEnd = true;
											return new Token(Tokens.EOL, Col, Line, "\n");
										}
										continue;
									}
									
									lineEnd = false;
									return new Token(keyWordToken, x, y, s);
								}
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
						if (ch == '%' && ReaderPeek() == '>') {
							int x = Col - 1;
							int y = Line;
							inXmlMode = true;
							ReaderRead();
							return new Token(Tokens.XmlEndInlineVB, new TextLocation(y, x), new TextLocation(Line, Col));
						}
						#endregion
						if (ch == '<' && (ef.NextTokenIsPotentialStartOfExpression || ef.NextTokenIsStartOfImportsOrAccessExpression)) {
							xmlModeStack.Push(new XmlModeInfo(ef.NextTokenIsStartOfImportsOrAccessExpression));
							XmlModeInfo info = xmlModeStack.Peek();
							int x = Col - 1;
							int y = Line;
							inXmlMode = true;
							if (ReaderPeek() == '/') {
								ReaderRead();
								info.inXmlCloseTag = true;
								return new Token(Tokens.XmlOpenEndTag, new TextLocation(y, x), new TextLocation(Line, Col));
							}
							// should we allow <%= at start of an expression? not valid with vbc ...
							if (ReaderPeek() == '%' && ReaderPeek(1) == '=') {
								inXmlMode = false;
								ReaderRead(); ReaderRead();
								return new Token(Tokens.XmlStartInlineVB, new TextLocation(y, x), new TextLocation(Line, Col));
							}
							if (ReaderPeek() == '!') {
								ReaderRead();
								Token t = ReadXmlCommentOrCData(x, y);
								return t;
							}
							if (ReaderPeek() == '?') {
								ReaderRead();
								Token t = ReadXmlProcessingInstruction(x, y);
								info.isDocumentStart = t.val.Trim().StartsWith("xml", StringComparison.OrdinalIgnoreCase);
								return t;
							}
							info.inXmlTag = true;
							info.level++;
							return new Token(Tokens.XmlOpenTag, x, y);
						}
						Token token = ReadOperator(ch);
						if (token != null) {
							lineEnd = false;
							return token;
						}
					}
					
					errors.Error(Line, Col, String.Format("Unknown char({0}) which can't be read", ch));
				}
			}
		}

		void CheckXMLState(TextLocation startLocation)
		{
			if (inXmlMode && !xmlModeStack.Any())
				throw new InvalidOperationException("invalid XML stack state at " + startLocation);
		}
		
		Token prevToken;
		
		Token Next()
		{
			Token t = NextInternal();
			if (t.kind == Tokens.EOL) {
				Debug.Assert(t.next == null); // NextInternal() must return only 1 token
				t.next = NextInternal();
				Debug.Assert(t.next.next == null);
				if (SkipEOL(prevToken.kind, t.next.kind)) {
					t = t.next;
				}
			} else
				encounteredLineContinuation = false;
			// inform EF only once we're sure it's really a token
			// this means we inform it about EOL tokens "1 token too late", but that's not a problem because
			// XML literals cannot start immediately after an EOL token
			ef.InformToken(t);
			if (t.next != null) {
				// Next() isn't called again when it returns 2 tokens, so we need to process both tokens
				ef.InformToken(t.next);
				prevToken = t.next;
			} else {
				prevToken = t;
			}
			ef.Advance();
			Debug.Assert(t != null);
			return t;
		}
		
		/// <remarks>see VB language specification 10; pg. 6</remarks>
		bool SkipEOL(int prevTokenKind, int nextTokenKind)
		{
			// exception directly after _
			if (encounteredLineContinuation) {
				return encounteredLineContinuation = false;
			}
			
			// 1st rule
			// after a comma (,), open parenthesis ((), open curly brace ({), or open embedded expression (<%=)
			if (new[] { Tokens.Comma, Tokens.OpenParenthesis, Tokens.OpenCurlyBrace, Tokens.XmlStartInlineVB }
			    .Contains(prevTokenKind))
				return true;
			
			// 2nd rule
			// after a member qualifier (. or .@ or ...), provided that something is being qualified (i.e. is not
			// using an implicit With context)
			if (new[] { Tokens.Dot, Tokens.DotAt, Tokens.TripleDot }.Contains(prevTokenKind)
			    && !ef.WasQualifierTokenAtStart)
				return true;
			
			// 3rd rule
			// before a close parenthesis ()), close curly brace (}), or close embedded expression (%>)
			if (new[] { Tokens.CloseParenthesis, Tokens.CloseCurlyBrace, Tokens.XmlEndInlineVB }
			    .Contains(nextTokenKind))
				return true;
			
			// 4th rule
			// after a less-than (<) in an attribute context
			if (prevTokenKind == Tokens.LessThan && ef.InContext(Context.Attribute))
				return true;
			
			// 5th rule
			// before a greater-than (>) in an attribute context
			if (nextTokenKind == Tokens.GreaterThan && ef.InContext(Context.Attribute))
				return true;
			
			// 6th rule
			// after a greater-than (>) in a non-file-level attribute context
			if (ef.WasNormalAttribute && prevTokenKind == Tokens.GreaterThan)
				return true;
			
			// 7th rule
			// before and after query operators (Where, Order, Select, etc.)
			var queryOperators = new int[] { Tokens.From, Tokens.Aggregate, Tokens.Select, Tokens.Distinct,
				Tokens.Where, Tokens.Order, Tokens.By, Tokens.Ascending, Tokens.Descending, Tokens.Take,
				Tokens.Skip, Tokens.Let, Tokens.Group, Tokens.Into, Tokens.On, Tokens.While, Tokens.Join };
			if (ef.InContext(Context.Query)) {
				// Ascending, Descending, Distinct are special
				// fixes http://community.sharpdevelop.net/forums/p/12068/32893.aspx#32893
				var specialQueryOperators = new int[] { Tokens.Ascending, Tokens.Descending, Tokens.Distinct };
				if (specialQueryOperators.Contains(prevTokenKind) && !queryOperators.Contains(nextTokenKind))
					return false;
				
				if ((queryOperators.Contains(prevTokenKind) || queryOperators.Contains(nextTokenKind)))
					return true;
			}
			
			// 8th rule
			// after binary operators (+, -, /, *, etc.) in an expression context
			if (new[] { Tokens.Plus, Tokens.Minus, Tokens.Div, Tokens.DivInteger, Tokens.Times, Tokens.Mod, Tokens.Power,
			    	Tokens.Assign, Tokens.NotEqual, Tokens.LessThan, Tokens.LessEqual, Tokens.GreaterThan, Tokens.GreaterEqual,
			    	Tokens.Like, Tokens.ConcatString, Tokens.AndAlso, Tokens.OrElse, Tokens.And, Tokens.Or, Tokens.Xor,
			    	Tokens.ShiftLeft, Tokens.ShiftRight }.Contains(prevTokenKind) && ef.CurrentBlock.context == Context.Expression)
				return true;
			
			// 9th rule
			// after assignment operators (=, :=, +=, -=, etc.) in any context.
			if (new[] { Tokens.Assign, Tokens.ColonAssign, Tokens.ConcatStringAssign, Tokens.DivAssign,
			    	Tokens.DivIntegerAssign, Tokens.MinusAssign, Tokens.PlusAssign, Tokens.PowerAssign,
			    	Tokens.ShiftLeftAssign, Tokens.ShiftRightAssign, Tokens.TimesAssign }.Contains(prevTokenKind))
				return true;
			
			return false;
		}
		
		/// <summary>
		/// Reads the next token.
		/// </summary>
		/// <returns>A <see cref="Token"/> object.</returns>
		public Token NextToken()
		{
			if (curToken == null) { // first call of NextToken()
				curToken = Next();
				//Console.WriteLine("Tok:" + Tokens.GetTokenString(curToken.kind) + " --- " + curToken.val);
				return curToken;
			}
			
			lastToken = curToken;
			
			if (curToken.next == null) {
				curToken.next = Next();
			}
			
			curToken = curToken.next;
			
			if (curToken.kind == Tokens.EOF && !(lastToken.kind == Tokens.EOL)) { // be sure that before EOF there is an EOL token
				curToken = new Token(Tokens.EOL, curToken.col, curToken.line, string.Empty);
				curToken.next = new Token(Tokens.EOF, curToken.col, curToken.line, string.Empty);
			}
			//Console.WriteLine("Tok:" + Tokens.GetTokenString(curToken.kind) + " --- " + curToken.val);
			return curToken;
		}
		
		#region VB Readers
		string ReadIdent(char ch)
		{
			char typeCharacter;
			return ReadIdent(ch, out typeCharacter);
		}
		
		string ReadIdent(char ch, out char typeCharacter)
		{
			typeCharacter = '\0';
			
			if (ef.ReadXmlIdentifier) {
				ef.ReadXmlIdentifier = false;
				return ReadXmlIdent(ch);
			}
			
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
				typeCharacter = (char)peek;
				ReaderRead();
				if (typeCharacter == '!') {
					peek = ReaderPeek();
					if (peek != -1 && (peek == '_' || peek == '[' || char.IsLetter((char)peek))) {
						misreadExclamationMarkAsTypeCharacter = true;
					}
				}
			}
			return sb.ToString();
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
			
			bool isHex      = false;
			bool isOct      = false;
			bool isSingle   = false;
			bool isDouble   = false;
			bool isDecimal  = false;
			
			if (ReaderPeek() == -1) {
				if (ch == '&') {
					errors.Error(Line, Col, String.Format("digit expected"));
				}
				return new Token(Tokens.LiteralInteger, x, y, sb.ToString() ,ch - '0');
			}
			if (ch == '.') {
				if (Char.IsDigit((char)ReaderPeek())) {
					isDouble = true; // double is default
					if (isHex || isOct) {
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
				isHex = true;
			} else if (ReaderPeek() != -1 && ch == '&' && PeekUpperChar() == 'O') {
				const string okt = "01234567";
				sb.Append((char)ReaderRead()); // skip 'O'
				while (ReaderPeek() != -1 && okt.IndexOf(PeekUpperChar()) != -1) {
					ch = (char)ReaderRead();
					sb.Append(ch);
					digit += Char.ToUpper(ch, CultureInfo.InvariantCulture);
				}
				isOct = true;
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
			
			if (ReaderPeek() != -1 && "%&SILU".IndexOf(PeekUpperChar()) != -1 || isHex || isOct) {
				bool unsigned = false;
				if (ReaderPeek() != -1) {
					ch = (char)ReaderPeek();
					sb.Append(ch);
					ch = Char.ToUpper(ch, CultureInfo.InvariantCulture);
					unsigned = ch == 'U';
					if (unsigned) {
						ReaderRead(); // read the U
						ch = (char)ReaderPeek();
						sb.Append(ch);
						ch = Char.ToUpper(ch, CultureInfo.InvariantCulture);
						if (ch != 'I' && ch != 'L' && ch != 'S') {
							errors.Error(Line, Col, "Invalid type character: U" + ch);
						}
					}
				}
				try {
					if (isOct) {
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
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), UInt16.Parse(digit, isHex ? NumberStyles.HexNumber : NumberStyles.Number));
						else
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int16.Parse(digit, isHex ? NumberStyles.HexNumber : NumberStyles.Number));
					} else if (ch == '%' || ch == 'I') {
						ReaderRead();
						if (unsigned)
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), UInt32.Parse(digit, isHex ? NumberStyles.HexNumber : NumberStyles.Number));
						else
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int32.Parse(digit, isHex ? NumberStyles.HexNumber : NumberStyles.Number));
					} else if (ch == '&' || ch == 'L') {
						ReaderRead();
						if (unsigned)
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), UInt64.Parse(digit, isHex ? NumberStyles.HexNumber : NumberStyles.Number));
						else
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int64.Parse(digit, isHex ? NumberStyles.HexNumber : NumberStyles.Number));
					} else if (isHex) {
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
				} catch (FormatException) {
					errors.Error(Line, Col, String.Format("{0} is not a parseable number", digit));
					return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), 0);
				}
			}
			Token nextToken = null; // if we accidently read a 'dot'
			if (!isDouble && ReaderPeek() == '.') { // read floating point number
				ReaderRead();
				if (ReaderPeek() != -1 && Char.IsDigit((char)ReaderPeek())) {
					isDouble = true; // double is default
					if (isHex || isOct) {
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
				isDouble = true;
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
						isDouble = true;
						break;
					case 'D':
					case '@':
						ReaderRead();
						isDecimal = true;
						break;
					case 'F':
					case '!':
						ReaderRead();
						isSingle = true;
						break;
				}
			}
			
			try {
				if (isSingle) {
					return new Token(Tokens.LiteralSingle, x, y, sb.ToString(), Single.Parse(digit, CultureInfo.InvariantCulture));
				}
				if (isDecimal) {
					return new Token(Tokens.LiteralDecimal, x, y, sb.ToString(), Decimal.Parse(digit, NumberStyles.Currency | NumberStyles.AllowExponent, CultureInfo.InvariantCulture));
				}
				if (isDouble) {
					return new Token(Tokens.LiteralDouble, x, y, sb.ToString(), Double.Parse(digit, CultureInfo.InvariantCulture));
				}
			} catch (FormatException) {
				errors.Error(Line, Col, String.Format("{0} is not a parseable number", digit));
				if (isSingle)
					return new Token(Tokens.LiteralSingle, x, y, sb.ToString(), 0f);
				if (isDecimal)
					return new Token(Tokens.LiteralDecimal, x, y, sb.ToString(), 0m);
				if (isDouble)
					return new Token(Tokens.LiteralDouble, x, y, sb.ToString(), 0.0);
			}
			Token token;
			try {
				token = new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int32.Parse(digit, isHex ? NumberStyles.HexNumber : NumberStyles.Number));
			} catch (Exception) {
				try {
					token = new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int64.Parse(digit, isHex ? NumberStyles.HexNumber : NumberStyles.Number));
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
			TextLocation start = new TextLocation(Line, Col - 1);
			string directive = ReadIdent('#');
			// TODO : expression parser for PP directives
			// needed for proper conversion to e. g. C#
			string argument  = ReadToEndOfLine();
//			this.specialTracker.AddPreprocessingDirective(new PreprocessingDirective(directive, argument.Trim(), start, new AstLocation(start.Line, start.Column + directive.Length + argument.Length)));
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
			TextLocation startPos = new TextLocation(Line, Col);
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
//							specialTracker.StartComment(CommentType.Documentation, isAtLineBegin, startPos);
							sb.Length = 0;
						}
					} else {
//						specialTracker.StartComment(CommentType.SingleLine, isAtLineBegin, startPos);
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
							TextLocation p = new TextLocation(Line, Col);
							string comment = ch + ReadToEndOfLine();
//							this.TagComments.Add(new TagComment(tag, comment, isAtLineBegin, p, new Location(Col, Line)));
							sb.Append(comment);
							break;
						}
					}
				}
			}
//			if (missingApostrophes > 0) {
//				specialTracker.StartComment(CommentType.SingleLine, isAtLineBegin, startPos);
//			}
//			specialTracker.AddString(sb.ToString());
//			specialTracker.FinishComment(new Location(Col, Line));
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
					if (ReaderPeek() == '=') {
						ReaderRead();
						return new Token(Tokens.ColonAssign, x, y);
					}
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
					int tmp = ReaderPeek(); int tmp2 = ReaderPeek(1);
					if (tmp > 0) {
						if (char.IsDigit((char)tmp))
							return ReadDigit('.', Col);
						else if ((char)tmp == '@') {
							ReaderRead();
							return new Token(Tokens.DotAt, x, y);
						} else if ((char)tmp == '.' && tmp2 > 0 && (char)tmp2 == '.') {
							ReaderRead(); ReaderRead();
							return new Token(Tokens.TripleDot, x, y);
						}
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
				case '!':
					return new Token(Tokens.ExclamationMark, x, y);
			}
			return null;
		}
		#endregion
		
		#region XML Readers
		Token ReadXmlProcessingInstruction(int x, int y)
		{
			sb.Length = 0;
			int nextChar = -1;
			
			while (ReaderPeek() != '?' || ReaderPeek(1) != '>') {
				nextChar = ReaderRead();
				if (nextChar == -1)
					break;
				sb.Append((char)nextChar);
			}
			
			ReaderSkip("?>".Length);
			
			return new Token(Tokens.XmlProcessingInstruction, new TextLocation(y, x), new TextLocation(Line, Col), sb.ToString(), null);
		}
		
		Token ReadXmlCommentOrCData(int x, int y)
		{
			sb.Length = 0;
			int nextChar = -1;
			
			if (string.CompareOrdinal(ReaderPeekString("--".Length), "--") == 0) {
				ReaderSkip("--".Length);
				while ((nextChar = ReaderRead()) != -1) {
					sb.Append((char)nextChar);
					if (string.CompareOrdinal(ReaderPeekString("-->".Length), "-->") == 0) {
						ReaderSkip("-->".Length);
						return new Token(Tokens.XmlComment, new TextLocation(y, x), new TextLocation(Line, Col), sb.ToString(), null);
					}
				}
			}
			
			if (string.CompareOrdinal(ReaderPeekString("[CDATA[".Length), "[CDATA[") == 0) {
				ReaderSkip("[CDATA[".Length);
				while ((nextChar = ReaderRead()) != -1) {
					sb.Append((char)nextChar);
					if (string.CompareOrdinal(ReaderPeekString("]]>".Length), "]]>") == 0) {
						ReaderSkip("]]>".Length);
						return new Token(Tokens.XmlCData, new TextLocation(y, x), new TextLocation(Line, Col), sb.ToString(), null);
					}
				}
			}
			
			return new Token(Tokens.XmlComment, new TextLocation(y, x), new TextLocation(Line, Col), sb.ToString(), null);
		}
		
		string ReadXmlContent(char ch)
		{
			sb.Length = 0;
			while (true) {
				sb.Append(ch);
				int next = ReaderPeek();
				
				if (next == -1 || next == '<')
					break;
				ch = (char)ReaderRead();
			}
			
			return sb.ToString();
		}
		
		string ReadXmlString(char terminator)
		{
			char ch = '\0';
			sb.Length = 0;
			int nextChar;
			while ((nextChar = ReaderRead()) != -1) {
				ch = (char)nextChar;
				if (ch == terminator) {
					break;
				} else if (ch == '\n') {
					errors.Error(Line, Col, String.Format("No return allowed inside String literal"));
				} else {
					sb.Append(ch);
				}
			}
			if (ch != terminator) {
				errors.Error(Line, Col, String.Format("End of File reached before String terminated "));
			}
			return sb.ToString();
		}
		
		string ReadXmlIdent(char ch)
		{
			sb.Length = 0;
			sb.Append(ch);
			
			int peek;
			
			while ((peek = ReaderPeek()) != -1 && (peek == ':' || XmlConvert.IsNCNameChar((char)peek))) {
				sb.Append((char)ReaderRead());
			}
			
			return sb.ToString();
		}
		#endregion
		
		char PeekUpperChar()
		{
			return Char.ToUpper((char)ReaderPeek(), CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Skips to the end of the current code block.
		/// For this, the lexer must have read the next token AFTER the token opening the
		/// block (so that Lexer.Token is the block-opening token, not Lexer.LookAhead).
		/// After the call, Lexer.LookAhead will be the block-closing token.
		/// </summary>
		public void SkipCurrentBlock(int targetToken)
		{
			int lastKind = -1;
			int kind = lastToken.kind;
			while (kind != Tokens.EOF &&
			       !(lastKind == Tokens.End && kind == targetToken))
			{
				lastKind = kind;
				NextToken();
				kind = lastToken.kind;
			}
		}
		
		public void SetInitialContext(SnippetType type)
		{
			ef.SetContext(type);
		}
		
		public VBLexerMemento Export()
		{
			return new VBLexerMemento() {
				Column = Col,
				Line = Line,
				EncounteredLineContinuation = encounteredLineContinuation,
				ExpressionFinder = ef.Export(),
				InXmlMode = inXmlMode,
				IsAtLineBegin = isAtLineBegin,
				LineEnd = lineEnd,
				PrevTokenKind = prevToken.kind,
				MisreadExclamationMarkAsTypeCharacter = misreadExclamationMarkAsTypeCharacter,
				XmlModeInfoStack = new Stack<XmlModeInfo>(xmlModeStack.Select(i => (XmlModeInfo)i.Clone()).Reverse())
			};
		}
		
		LATextReader reader;
		int col  = 1;
		int line = 1;
		
		protected Errors errors = new Errors();
		
		protected Token lastToken = null;
		protected Token curToken  = null;
		protected Token peekToken = null;
		
		string[]  specialCommentTags = null;
		protected Hashtable specialCommentHash  = null;
//		List<TagComment> tagComments  = new List<TagComment>();
		protected StringBuilder sb              = new StringBuilder();
//		protected SpecialTracker specialTracker = new SpecialTracker();
		
		// used for the original value of strings (with escape sequences).
		protected StringBuilder originalValue = new StringBuilder();
		
		public bool SkipAllComments { get; set; }
		public bool EvaluateConditionalCompilation { get; set; }
		public virtual IDictionary<string, object> ConditionalCompilationSymbols {
			get { throw new NotSupportedException(); }
		}
		
		protected static IEnumerable<string> GetSymbols (string symbols)
		{
			if (!string.IsNullOrEmpty(symbols)) {
				foreach (string symbol in symbols.Split (';', ' ', '\t')) {
					string s = symbol.Trim ();
					if (s.Length == 0)
						continue;
					yield return s;
				}
			}
		}
		
		public void SetConditionalCompilationSymbols (string symbols)
		{
			throw new NotSupportedException ();
		}
		
		protected int Line {
			get {
				return line;
			}
		}
		protected int Col {
			get {
				return col;
			}
		}
		
		protected bool recordRead = false;
		protected StringBuilder recordedText = new StringBuilder ();
		
		protected int ReaderRead()
		{
			int val = reader.Read();
			if (recordRead && val >= 0)
				recordedText.Append ((char)val);
			if ((val == '\r' && reader.Peek() != '\n') || val == '\n') {
				++line;
				col = 1;
				LineBreak();
			} else if (val >= 0) {
				col++;
			}
			return val;
		}
		
		protected int ReaderPeek()
		{
			return reader.Peek();
		}
		
		protected int ReaderPeek(int step)
		{
			return reader.Peek(step);
		}
		
		protected void ReaderSkip(int steps)
		{
			for (int i = 0; i < steps; i++) {
				ReaderRead();
			}
		}
		
		protected string ReaderPeekString(int length)
		{
			StringBuilder builder = new StringBuilder();
			
			for (int i = 0; i < length; i++) {
				int peek = ReaderPeek(i);
				if (peek != -1)
					builder.Append((char)peek);
			}
			
			return builder.ToString();
		}
		
		public void SetInitialLocation(TextLocation location)
		{
			if (lastToken != null || curToken != null || peekToken != null)
				throw new InvalidOperationException();
			this.line = location.Line;
			this.col = location.Column;
		}
		
		public Errors Errors {
			get {
				return errors;
			}
		}
		
		/// <summary>
		/// Returns the comments that had been read and containing tag key words.
		/// </summary>
//		public List<TagComment> TagComments {
//			get {
//				return tagComments;
//			}
//		}
		
//		public SpecialTracker SpecialTracker {
//			get {
//				return specialTracker;
//			}
//		}
		
		/// <summary>
		/// Special comment tags are tags like TODO, HACK or UNDONE which are read by the lexer and stored in <see cref="TagComments"/>.
		/// </summary>
		public string[] SpecialCommentTags {
			get {
				return specialCommentTags;
			}
			set {
				specialCommentTags = value;
				specialCommentHash = null;
				if (specialCommentTags != null && specialCommentTags.Length > 0) {
					specialCommentHash = new Hashtable();
					foreach (string str in specialCommentTags) {
						specialCommentHash.Add(str, null);
					}
				}
			}
		}
		
		/// <summary>
		/// The current Token. <seealso cref="ICSharpCode.NRefactory.VB.Parser.Token"/>
		/// </summary>
		public Token Token {
			get {
//				Console.WriteLine("Call to Token");
				return lastToken;
			}
		}
		
		/// <summary>
		/// The next Token (The <see cref="Token"/> after <see cref="NextToken"/> call) . <seealso cref="ICSharpCode.NRefactory.VB.Parser.Token"/>
		/// </summary>
		public Token LookAhead {
			get {
//				Console.WriteLine("Call to LookAhead");
				return curToken;
			}
		}
		
		#region System.IDisposable interface implementation
		public virtual void Dispose()
		{
			reader.Close();
			reader = null;
			errors = null;
			lastToken = curToken = peekToken = null;
			specialCommentHash = null;
			sb = originalValue = null;
		}
		#endregion
		
		/// <summary>
		/// Must be called before a peek operation.
		/// </summary>
		public void StartPeek()
		{
			peekToken = curToken;
		}
		
		/// <summary>
		/// Gives back the next token. A second call to Peek() gives the next token after the last call for Peek() and so on.
		/// </summary>
		/// <returns>An <see cref="Token"/> object.</returns>
		public Token Peek()
		{
//			Console.WriteLine("Call to Peek");
			if (peekToken.next == null) {
				peekToken.next = Next();
			}
			peekToken = peekToken.next;
			return peekToken;
		}
		
		protected static bool IsIdentifierPart(int ch)
		{
			if (ch == 95) return true;  // 95 = '_'
			if (ch == -1) return false;
			return char.IsLetterOrDigit((char)ch); // accept unicode letters
		}
		
		protected static bool IsHex(char digit)
		{
			return Char.IsDigit(digit) || ('A' <= digit && digit <= 'F') || ('a' <= digit && digit <= 'f');
		}
		
		protected int GetHexNumber(char digit)
		{
			if (Char.IsDigit(digit)) {
				return digit - '0';
			}
			if ('A' <= digit && digit <= 'F') {
				return digit - 'A' + 0xA;
			}
			if ('a' <= digit && digit <= 'f') {
				return digit - 'a' + 0xA;
			}
			errors.Error(line, col, String.Format("Invalid hex number '" + digit + "'"));
			return 0;
		}
		protected TextLocation lastLineEnd = new TextLocation(1, 1);
		protected TextLocation curLineEnd = new TextLocation(1, 1);
		protected void LineBreak ()
		{
			lastLineEnd = curLineEnd;
			curLineEnd = new TextLocation (line, col - 1);
		}
		protected bool HandleLineEnd(char ch)
		{
			// Handle MS-DOS or MacOS line ends.
			if (ch == '\r') {
				if (reader.Peek() == '\n') { // MS-DOS line end '\r\n'
					ReaderRead(); // LineBreak (); called by ReaderRead ();
					return true;
				} else { // assume MacOS line end which is '\r'
					LineBreak ();
					return true;
				}
			}
			if (ch == '\n') {
				LineBreak ();
				return true;
			}
			return false;
		}
		
		protected void SkipToEndOfLine()
		{
			int nextChar;
			while ((nextChar = reader.Read()) != -1) {
				if (nextChar == '\r') {
					if (reader.Peek() == '\n')
						reader.Read();
					nextChar = '\n';
				}
				if (nextChar == '\n') {
					++line;
					col = 1;
					break;
				}
			}
		}
		
		protected string ReadToEndOfLine()
		{
			sb.Length = 0;
			int nextChar;
			while ((nextChar = reader.Read()) != -1) {
				char ch = (char)nextChar;
				
				if (nextChar == '\r') {
					if (reader.Peek() == '\n')
						reader.Read();
					nextChar = '\n';
				}
				// Return read string, if EOL is reached
				if (nextChar == '\n') {
					++line;
					col = 1;
					return sb.ToString();
				}
				
				sb.Append(ch);
			}
			
			// Got EOF before EOL
			string retStr = sb.ToString();
			col += retStr.Length;
			return retStr;
		}
		
		public event EventHandler<SavepointEventArgs> SavepointReached;
		
		protected virtual void OnSavepointReached(SavepointEventArgs e)
		{
			if (SavepointReached != null) {
				SavepointReached(this, e);
			}
		}
	}
}
