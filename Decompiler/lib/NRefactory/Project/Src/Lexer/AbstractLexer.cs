// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ICSharpCode.NRefactory.Parser
{
	/// <summary>
	/// This is the base class for the C# and VB.NET lexer
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1708:IdentifiersShouldDifferByMoreThanCase")]
	public abstract class AbstractLexer : ILexer
	{
		TextReader reader;
		int col  = 1;
		int line = 1;
		
		[CLSCompliant(false)]
		protected Errors errors = new Errors();
		
		protected Token lastToken = null;
		protected Token curToken  = null;
		protected Token peekToken = null;
		
		string[]  specialCommentTags = null;
		protected Hashtable specialCommentHash  = null;
		List<TagComment> tagComments  = new List<TagComment>();
		protected StringBuilder sb              = new StringBuilder();
		[CLSCompliant(false)]
		protected SpecialTracker specialTracker = new SpecialTracker();
		
		// used for the original value of strings (with escape sequences).
		protected StringBuilder originalValue = new StringBuilder();
		
		bool skipAllComments = false;
		
		public bool SkipAllComments {
			get {
				return skipAllComments;
			}
			set {
				skipAllComments = value;
			}
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
		protected int ReaderRead()
		{
			++col;
			return reader.Read();
		}
		protected int ReaderPeek()
		{
			return reader.Peek();
		}
		
		public Errors Errors {
			get {
				return errors;
			}
		}
		
		/// <summary>
		/// Returns the comments that had been read and containing tag key words.
		/// </summary>
		public List<TagComment> TagComments {
			get {
				return tagComments;
			}
		}
		
		public SpecialTracker SpecialTracker {
			get {
				return specialTracker;
			}
		}
		
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
		/// The current Token. <seealso cref="ICSharpCode.NRefactory.Parser.Token"/>
		/// </summary>
		public Token Token {
			get {
//				Console.WriteLine("Call to Token");
				return lastToken;
			}
		}
		
		/// <summary>
		/// The next Token (The <see cref="Token"/> after <see cref="NextToken"/> call) . <seealso cref="ICSharpCode.NRefactory.Parser.Token"/>
		/// </summary>
		public Token LookAhead {
			get {
//				Console.WriteLine("Call to LookAhead");
				return curToken;
			}
		}
		
		/// <summary>
		/// Constructor for the abstract lexer class.
		/// </summary>
		protected AbstractLexer(TextReader reader)
		{
			this.reader = reader;
		}
		
		#region System.IDisposable interface implementation
		public virtual void Dispose()
		{
			reader.Close();
			reader = null;
			errors = null;
			lastToken = curToken = peekToken = null;
			specialCommentHash = null;
			tagComments = null;
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
				specialTracker.InformToken(peekToken.next.kind);
			}
			peekToken = peekToken.next;
			return peekToken;
		}
		
		/// <summary>
		/// Reads the next token and gives it back.
		/// </summary>
		/// <returns>An <see cref="Token"/> object.</returns>
		public virtual Token NextToken()
		{
			if (curToken == null) {
				curToken = Next();
				specialTracker.InformToken(curToken.kind);
				//Console.WriteLine(ICSharpCode.NRefactory.Parser.CSharp.Tokens.GetTokenString(curToken.kind) + " -- " + curToken.val + "(" + curToken.kind + ")");
				return curToken;
			}
			
			lastToken = curToken;
			
			if (curToken.next == null) {
				curToken.next = Next();
				if (curToken.next != null) {
					specialTracker.InformToken(curToken.next.kind);
				}
			}
			
			curToken  = curToken.next;
			//Console.WriteLine(ICSharpCode.NRefactory.Parser.CSharp.Tokens.GetTokenString(curToken.kind) + " -- " + curToken.val + "(" + curToken.kind + ")");
			return curToken;
		}
		
		protected abstract Token Next();
		
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
		
		protected bool WasLineEnd(char ch)
		{
			// Handle MS-DOS or MacOS line ends.
			if (ch == '\r') {
				if (reader.Peek() == '\n') { // MS-DOS line end '\r\n'
					ch = (char)reader.Read();
					++col;
				} else { // assume MacOS line end which is '\r'
					ch = '\n';
				}
			}
			return ch == '\n';
		}
		
		protected bool HandleLineEnd(char ch)
		{
			if (WasLineEnd(ch)) {
				++line;
				col = 1;
				return true;
			}
			return false;
		}
		
		protected void SkipToEndOfLine()
		{
			int nextChar;
			while ((nextChar = reader.Read()) != -1) {
				if (HandleLineEnd((char)nextChar)) {
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
				
				// Return read string, if EOL is reached
				if (HandleLineEnd(ch)) {
					return sb.ToString();
				}
				
				sb.Append(ch);
			}
			
			// Got EOF before EOL
			string retStr = sb.ToString();
			col += retStr.Length;
			return retStr;
		}
		
		/// <summary>
		/// Skips to the end of the current code block.
		/// For this, the lexer must have read the next token AFTER the token opening the
		/// block (so that Lexer.Token is the block-opening token, not Lexer.LookAhead).
		/// After the call, Lexer.LookAhead will be the block-closing token.
		/// </summary>
		public abstract void SkipCurrentBlock(int targetToken);
	}
}
