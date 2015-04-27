// 
// CSharpCompletionEngineBase.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Text;

using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Completion
{
	/// <summary>
	/// Acts as a common base between code completion and parameter completion.
	/// </summary>
	public class CSharpCompletionEngineBase
	{
		protected IDocument document;
		protected int offset;
		protected TextLocation location;
		protected IUnresolvedTypeDefinition currentType;
		protected IUnresolvedMember currentMember;
		
		#region Input properties
		public CSharpTypeResolveContext ctx { get; private set; }

		public IProjectContent ProjectContent { get; private set; }
		
		ICompilation compilation;

		protected ICompilation Compilation {
			get {
				if (compilation == null)
					compilation = ProjectContent.Resolve (ctx).Compilation;
				return compilation;
			}
		}

		Version languageVersion = new Version (5, 0);
		public Version LanguageVersion {
			get {
				return languageVersion;
			}
			set {
				languageVersion = value;
			}
		}
		#endregion
		
		protected CSharpCompletionEngineBase(IProjectContent content, ICompletionContextProvider completionContextProvider, CSharpTypeResolveContext ctx)
		{
			if (content == null)
				throw new ArgumentNullException("content");
			if (ctx == null)
				throw new ArgumentNullException("ctx");
			if (completionContextProvider == null)
				throw new ArgumentNullException("completionContextProvider");
			
			this.ProjectContent = content;
			this.CompletionContextProvider = completionContextProvider;
			this.ctx = ctx;
		}
		
		
		public ICompletionContextProvider CompletionContextProvider {
			get;
			private set;
		}
		
		public void SetOffset (int offset)
		{
			Reset ();
			
			this.offset = offset;
			this.location = document.GetLocation (offset);
			CompletionContextProvider.GetCurrentMembers (offset, out currentType, out currentMember);
		}

		public bool GetParameterCompletionCommandOffset (out int cpos)
		{
			// Start calculating the parameter offset from the beginning of the
			// current member, instead of the beginning of the file. 
			cpos = offset - 1;
			var mem = currentMember;
			if (mem == null || (mem is IType) || IsInsideCommentStringOrDirective ()) {
				return false;
			}
			int startPos = document.GetOffset (mem.Region.BeginLine, mem.Region.BeginColumn);
			int parenDepth = 0;
			int chevronDepth = 0;
			Stack<int> indexStack = new Stack<int> ();
			while (cpos > startPos) {
				char c = document.GetCharAt (cpos);
				if (c == ')') {
					parenDepth++;
				}
				if (c == '>') {
					chevronDepth++;
				}
				if (c == '}') {
					if (indexStack.Count > 0) {
						parenDepth = indexStack.Pop ();
					} else {
						parenDepth = 0;
					}
					chevronDepth = 0;
				}
				if (indexStack.Count == 0 && (parenDepth == 0 && c == '(' || chevronDepth == 0 && c == '<')) {
					int p = GetCurrentParameterIndex (startPos, cpos + 1);
					if (p != -1) {
						cpos++;
						return true;
					} else {
						return false;
					}
				}
				if (c == '(') {
					parenDepth--;
				}
				if (c == '<') {
					chevronDepth--;
				}
				if (c == '{') {
					indexStack.Push (parenDepth);
					chevronDepth = 0;
				}
				cpos--;
			}
			return false;
		}
		
		public int GetCurrentParameterIndex(int triggerOffset, int endOffset)
		{
			List<string> list;
			return  GetCurrentParameterIndex (triggerOffset, endOffset, out list);
		}

		public int GetCurrentParameterIndex (int triggerOffset, int endOffset, out List<string> usedNamedParameters)
		{
			usedNamedParameters =new List<string> ();
			var parameter = new Stack<int> ();
			var bracketStack = new Stack<Stack<int>> ();
			bool inSingleComment = false, inString = false, inVerbatimString = false, inChar = false, inMultiLineComment = false;
			var word = new StringBuilder ();
			bool foundCharAfterOpenBracket = false;
			for (int i = triggerOffset; i < endOffset; i++) {
				char ch = document.GetCharAt (i);
				char nextCh = i + 1 < document.TextLength ? document.GetCharAt (i + 1) : '\0';
				if (ch == ':') {
					usedNamedParameters.Add (word.ToString ());
					word.Length = 0;
				} else if (char.IsLetterOrDigit (ch) || ch =='_') {
					word.Append (ch);
				} else if (char.IsWhiteSpace (ch)) {

				} else {
					word.Length = 0;
				}
				if (!char.IsWhiteSpace(ch) && parameter.Count > 0)
					foundCharAfterOpenBracket = true;

				switch (ch) {
					case '{':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						bracketStack.Push (parameter);
						parameter = new Stack<int> ();
						break;
					case '[':
					case '(':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						parameter.Push (0);
						break;
					case '}':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (bracketStack.Count > 0) {
							parameter = bracketStack.Pop ();
						} else {
							return -1;
						}
						break;
					case ']':
					case ')':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (parameter.Count > 0) {
							parameter.Pop ();
						} else {
							return -1;
						}
						break;
					case '<':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						parameter.Push (0);
						break;
					case '=':
						if (nextCh == '>') {
							i++;
							continue;
						}
						break;
					case '>':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (parameter.Count > 0) {
							parameter.Pop ();
						}
						break;
					case ',':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (parameter.Count > 0) {
							parameter.Push (parameter.Pop () + 1);
						}
						break;
					case '/':
						if (inString || inChar || inVerbatimString) {
							break;
						}
						if (nextCh == '/') {
							i++;
							inSingleComment = true;
						}
						if (nextCh == '*') {
							inMultiLineComment = true;
						}
						break;
					case '*':
						if (inString || inChar || inVerbatimString || inSingleComment) {
							break;
						}
						if (nextCh == '/') {
							i++;
							inMultiLineComment = false;
						}
						break;
					case '@':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (nextCh == '"') {
							i++;
							inVerbatimString = true;
						}
						break;
					case '\\':
						if (inString || inChar) {
							i++;
						}
						break;
					case '"':
						if (inSingleComment || inMultiLineComment || inChar) {
							break;
						}
						if (inVerbatimString) {
							if (nextCh == '"') {
								i++;
								break;
							}
							inVerbatimString = false;
							break;
						}
						inString = !inString;
						break;
					case '\'':
						if (inSingleComment || inMultiLineComment || inString || inVerbatimString) {
							break;
						}
						inChar = !inChar;
						break;
					default:
						if (NewLine.IsNewLine(ch)) {
							inSingleComment = false;
							inString = false;
							inChar = false;
						}
						break;
				}
			}
			if (parameter.Count != 1 || bracketStack.Count > 0) {
				return -1;
			}
			if (!foundCharAfterOpenBracket)
				return 0;
			return parameter.Pop() + 1;
		}

		#region Context helper methods
		public class MiniLexer
		{
			readonly string text;

			public bool IsFistNonWs               = true;
			public bool IsInSingleComment         = false;
			public bool IsInString                = false;
			public bool IsInVerbatimString        = false;
			public bool IsInChar                  = false;
			public bool IsInMultiLineComment      = false;
			public bool IsInPreprocessorDirective = false;

			public MiniLexer(string text)
			{
				this.text = text;
			}

			/// <summary>
			/// Parsing all text and calling act delegate on almost every character.
			/// Skipping begining of comments, begining of verbatim strings and escaped characters.
			/// </summary>
			/// <param name="act">Return true to abort parsing. Integer argument represent offset in text.</param>
			/// <returns>True if aborted.</returns>
			public bool Parse(Func<char, int, bool> act = null)
			{
				return Parse(0, text.Length, act);
			}


			/// <summary>
			/// Parsing text from start to start+length and calling act delegate on almost every character.
			/// Skipping begining of comments, begining of verbatim strings and escaped characters.
			/// </summary>
			/// <param name="start">Start offset.</param>
			/// <param name="length">Lenght to parse.</param>
			/// <param name="act">Return true to abort parsing. Integer argument represent offset in text.</param>
			/// <returns>True if aborted.</returns>
			public bool Parse(int start, int length, Func<char, int, bool> act = null)
			{
				for (int i = start; i < length; i++) {
					char ch = text [i];
					char nextCh = i + 1 < text.Length ? text [i + 1] : '\0';
					switch (ch) {
						case '#':
							if (IsFistNonWs)
								IsInPreprocessorDirective = true;
							break; 
						case '/':
							if (IsInString || IsInChar || IsInVerbatimString || IsInSingleComment || IsInMultiLineComment)
								break;
							if (nextCh == '/') {
								i++;
								IsInSingleComment = true;
								IsInPreprocessorDirective = false;
							}
							if (nextCh == '*' && !IsInPreprocessorDirective) {
								IsInMultiLineComment = true;
								i++;
							}
							break;
						case '*':
							if (IsInString || IsInChar || IsInVerbatimString || IsInSingleComment)
								break;
							if (nextCh == '/') {
								i++;
								IsInMultiLineComment = false;
							}
							break;
						case '@':
							if (IsInString || IsInChar || IsInVerbatimString || IsInSingleComment || IsInMultiLineComment)
								break;
							if (nextCh == '"') {
								i++;
								IsInVerbatimString = true;
							}
							break;
						case '\n':
						case '\r':
							IsInSingleComment = false;
							IsInString = false;
							IsInChar = false;
							IsFistNonWs = true;
							IsInPreprocessorDirective = false;
							break;
						case '\\':
							if (IsInString || IsInChar)
								i++;
							break;
						case '"':
							if (IsInSingleComment || IsInMultiLineComment || IsInChar)
								break;
							if (IsInVerbatimString) {
								if (nextCh == '"') {
									i++;
									break;
								}
								IsInVerbatimString = false;
								break;
							}
							IsInString = !IsInString;
							break;
						case '\'':
							if (IsInSingleComment || IsInMultiLineComment || IsInString || IsInVerbatimString)
								break;
							IsInChar = !IsInChar;
							break;
					}
					if (act != null)
					if (act (ch, i))
						return true;
					IsFistNonWs &= ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
				}
				return false;
			}
		}

		
		protected bool IsInsideCommentStringOrDirective(int offset)
		{
			var lexer = new MiniLexer(document.Text);
			lexer.Parse(0, offset);
			return
				lexer.IsInSingleComment || 
				lexer.IsInString ||
				lexer.IsInVerbatimString ||
				lexer.IsInChar ||
				lexer.IsInMultiLineComment || 
				lexer.IsInPreprocessorDirective;
		}


		protected bool IsInsideCommentStringOrDirective()
		{
			var text = GetMemberTextToCaret();
			var lexer = new MiniLexer(text.Item1);
			lexer.Parse();
			return
				lexer.IsInSingleComment || 
					lexer.IsInString ||
					lexer.IsInVerbatimString ||
					lexer.IsInChar ||
					lexer.IsInMultiLineComment || 
					lexer.IsInPreprocessorDirective;
		}

		protected bool IsInsideDocComment ()
		{
			var text = GetMemberTextToCaret ();
			bool inSingleComment = false, inString = false, inVerbatimString = false, inChar = false, inMultiLineComment = false;
			bool singleLineIsDoc = false;
			
			for (int i = 0; i < text.Item1.Length - 1; i++) {
				char ch = text.Item1 [i];
				char nextCh = text.Item1 [i + 1];
				
				switch (ch) {
				case '/':
					if (inString || inChar || inVerbatimString)
						break;
					if (nextCh == '/') {
						i++;
						inSingleComment = true;
						singleLineIsDoc = i + 1 < text.Item1.Length && text.Item1 [i + 1] == '/';
						if (singleLineIsDoc) {
							i++;
						}
					}
					if (nextCh == '*')
						inMultiLineComment = true;
					break;
				case '*':
					if (inString || inChar || inVerbatimString || inSingleComment)
						break;
					if (nextCh == '/') {
						i++;
						inMultiLineComment = false;
					}
					break;
				case '@':
					if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
						break;
					if (nextCh == '"') {
						i++;
						inVerbatimString = true;
					}
					break;
				case '\n':
				case '\r':
					inSingleComment = false;
					inString = false;
					inChar = false;
					break;
				case '\\':
					if (inString || inChar)
						i++;
					break;
				case '"':
					if (inSingleComment || inMultiLineComment || inChar)
						break;
					if (inVerbatimString) {
						if (nextCh == '"') {
							i++;
							break;
						}
						inVerbatimString = false;
						break;
					}
					inString = !inString;
					break;
				case '\'':
					if (inSingleComment || inMultiLineComment || inString || inVerbatimString)
						break;
					inChar = !inChar;
					break;
				}
			}
			
			return inSingleComment && singleLineIsDoc;
		}

		protected CSharpResolver GetState ()
		{
			return new CSharpResolver (ctx);
			/*var state = new CSharpResolver (ctx);
			
			state.CurrentMember = currentMember;
			state.CurrentTypeDefinition = currentType;
			state.CurrentUsingScope = CSharpUnresolvedFile.GetUsingScope (location);
			if (state.CurrentMember != null) {
				var node = Unit.GetNodeAt (location);
				if (node == null)
					return state;
				var navigator = new NodeListResolveVisitorNavigator (new[] { node });
				var visitor = new ResolveVisitor (state, CSharpUnresolvedFile, navigator);
				Unit.AcceptVisitor (visitor, null);
				try {
					var newState = visitor.GetResolverStateBefore (node);
					if (newState != null)
						state = newState;
				} catch (Exception) {
				}
			}
			
			return state;*/
		}
				#endregion
		
		#region Basic parsing/resolving functions
		static Stack<Tuple<char, int>> GetBracketStack (string memberText)
		{
			var bracketStack = new Stack<Tuple<char, int>> ();
			
			bool inSingleComment = false, inString = false, inVerbatimString = false, inChar = false, inMultiLineComment = false;
			
			for (int i = 0; i < memberText.Length; i++) {
				char ch = memberText [i];
				char nextCh = i + 1 < memberText.Length ? memberText [i + 1] : '\0';
				switch (ch) {
				case '(':
				case '[':
				case '{':
					if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
						break;
					bracketStack.Push (Tuple.Create (ch, i));
					break;
				case ')':
				case ']':
				case '}':
					if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
						break;
					if (bracketStack.Count > 0)
						bracketStack.Pop ();
					break;
				case '/':
					if (inString || inChar || inVerbatimString)
						break;
					if (nextCh == '/') {
						i++;
						inSingleComment = true;
					}
					if (nextCh == '*')
						inMultiLineComment = true;
					break;
				case '*':
					if (inString || inChar || inVerbatimString || inSingleComment)
						break;
					if (nextCh == '/') {
						i++;
						inMultiLineComment = false;
					}
					break;
				case '@':
					if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
						break;
					if (nextCh == '"') {
						i++;
						inVerbatimString = true;
					}
					break;
				case '\\':
					if (inString || inChar)
						i++;
					break;
				case '"':
					if (inSingleComment || inMultiLineComment || inChar)
						break;
					if (inVerbatimString) {
						if (nextCh == '"') {
							i++;
							break;
						}
						inVerbatimString = false;
						break;
					}
					inString = !inString;
					break;
				case '\'':
					if (inSingleComment || inMultiLineComment || inString || inVerbatimString)
						break;
					inChar = !inChar;
					break;
				default :
					if (NewLine.IsNewLine(ch)) {
						inSingleComment = false;
						inString = false;
						inChar = false;
					}
					break;
				}
			}
			return bracketStack;
		}
		
		public static void AppendMissingClosingBrackets (StringBuilder wrapper, bool appendSemicolon)
		{
			var memberText = wrapper.ToString();
			var bracketStack = GetBracketStack(memberText);
			bool didAppendSemicolon = !appendSemicolon;
			//char lastBracket = '\0';
			while (bracketStack.Count > 0) {
				var t = bracketStack.Pop ();
				switch (t.Item1) {
				case '(':
					wrapper.Append (')');
					if (appendSemicolon)
						didAppendSemicolon = false;
					//lastBracket = ')';
					break;
				case '[':
					wrapper.Append (']');
					if (appendSemicolon)
						didAppendSemicolon = false;
					//lastBracket = ']';
					break;
				case '<':
					wrapper.Append ('>');
					if (appendSemicolon)
						didAppendSemicolon = false;
					//lastBracket = '>';
					break;
				case '{':
					int o = t.Item2 - 1;
					if (!didAppendSemicolon) {
						didAppendSemicolon = true;
						wrapper.Append (';');
					}
						
					bool didAppendCatch = false;
					while (o >= "try".Length) {
						char ch = memberText [o];
						if (!char.IsWhiteSpace (ch)) {
								if (ch == 'y' && memberText [o - 1] == 'r' && memberText [o - 2] == 't' && (o - 3 < 0 || !char.IsLetterOrDigit(memberText [o - 3]))) {
								wrapper.Append ("} catch {}");
								didAppendCatch = true;
							}
							break;
						}
						o--;
					}
					if (!didAppendCatch)
						wrapper.Append ('}');
					break;
				}
			}
			if (!didAppendSemicolon)
				wrapper.Append (';');
		}

		protected StringBuilder CreateWrapper(string continuation, bool appendSemicolon, string afterContinuation, string memberText, TextLocation memberLocation, ref int closingBrackets, ref int generatedLines)
		{
			var wrapper = new StringBuilder();
			bool wrapInClass = memberLocation != new TextLocation(1, 1);
			if (wrapInClass) {
				wrapper.Append("class Stub {");
				wrapper.AppendLine();
				closingBrackets++;
				generatedLines++;
			}
			wrapper.Append(memberText);
			wrapper.Append(continuation);
			AppendMissingClosingBrackets(wrapper, appendSemicolon);
			wrapper.Append(afterContinuation);
			if (closingBrackets > 0) {
				wrapper.Append(new string('}', closingBrackets));
			}
			return wrapper;
		}

		protected SyntaxTree ParseStub(string continuation, bool appendSemicolon = true, string afterContinuation = null)
		{
			var mt = GetMemberTextToCaret();
			if (mt == null) {
				return null;
			}

			string memberText = mt.Item1;
			var memberLocation = mt.Item2;
			int closingBrackets = 1;
			int generatedLines = 0;
			var wrapper = CreateWrapper(continuation, appendSemicolon, afterContinuation, memberText, memberLocation, ref closingBrackets, ref generatedLines);
			var parser = new CSharpParser ();
			foreach (var sym in CompletionContextProvider.ConditionalSymbols)
				parser.CompilerSettings.ConditionalSymbols.Add (sym);
			parser.InitialLocation = new TextLocation(memberLocation.Line - generatedLines, 1);
			var result = parser.Parse(wrapper.ToString ());
			return result;
		}
		
		protected virtual void Reset ()
		{
			memberText = null;
		}

		Tuple<string, TextLocation> memberText;
		protected Tuple<string, TextLocation> GetMemberTextToCaret()
		{
			if (memberText == null)
				memberText = CompletionContextProvider.GetMemberTextToCaret(offset, currentType, currentMember);
			return memberText;
		}

		protected ExpressionResult GetInvocationBeforeCursor(bool afterBracket)
		{
			SyntaxTree baseUnit;
			baseUnit = ParseStub("a", false);

			var section = baseUnit.GetNodeAt<AttributeSection>(location.Line, location.Column - 2);
			var attr = section != null ? section.Attributes.LastOrDefault() : null;
			if (attr != null) {
				return new ExpressionResult((AstNode)attr, baseUnit);
			}

			//var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			var mref = baseUnit.GetNodeAt(location.Line, location.Column - 1, n => n is InvocationExpression || n is ObjectCreateExpression); 
			AstNode expr = null;
			if (mref is InvocationExpression) {
				expr = ((InvocationExpression)mref).Target;
			} else if (mref is ObjectCreateExpression) {
				expr = mref;
			} else {
				baseUnit = ParseStub(")};", false);
				mref = baseUnit.GetNodeAt(location.Line, location.Column - 1, n => n is InvocationExpression || n is ObjectCreateExpression); 
				if (mref is InvocationExpression) {
					expr = ((InvocationExpression)mref).Target;
				} else if (mref is ObjectCreateExpression) {
					expr = mref;
				}
			}

			if (expr == null) {
				// work around for missing ';' bug in mcs:
				baseUnit = ParseStub("a", true);
			
				section = baseUnit.GetNodeAt<AttributeSection>(location.Line, location.Column - 2);
				attr = section != null ? section.Attributes.LastOrDefault() : null;
				if (attr != null) {
					return new ExpressionResult((AstNode)attr, baseUnit);
				}
	
				//var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
				mref = baseUnit.GetNodeAt(location.Line, location.Column - 1, n => n is InvocationExpression || n is ObjectCreateExpression); 
				expr = null;
				if (mref is InvocationExpression) {
					expr = ((InvocationExpression)mref).Target;
				} else if (mref is ObjectCreateExpression) {
					expr = mref;
				}
			}

			if (expr == null) {
				return null;
			}
			return new ExpressionResult ((AstNode)expr, baseUnit);
		}
		
		public class ExpressionResult
		{
			public AstNode Node { get; private set; }
			public SyntaxTree Unit  { get; private set; }
			
			
			public ExpressionResult (AstNode item2, SyntaxTree item3)
			{
				this.Node = item2;
				this.Unit = item3;
			}
			
			public override string ToString ()
			{
				return string.Format ("[ExpressionResult: Node={0}, Unit={1}]", Node, Unit);
			}
		}
		
		protected ExpressionResolveResult ResolveExpression (ExpressionResult tuple)
		{
			return ResolveExpression (tuple.Node);
		}

		protected class ExpressionResolveResult
		{
			public ResolveResult Result { get; set; }
			public CSharpResolver Resolver { get; set; }
			public CSharpAstResolver AstResolver { get; set; }

			public ExpressionResolveResult(ResolveResult item1, CSharpResolver item2, CSharpAstResolver item3)
			{
				this.Result = item1;
				this.Resolver = item2;
				this.AstResolver = item3;
			}
		}

		protected ExpressionResolveResult ResolveExpression(AstNode expr)
		{
			if (expr == null) {
				return null;
			}
			AstNode resolveNode;
			if (expr is Expression || expr is AstType) {
				resolveNode = expr;
			} else if (expr is VariableDeclarationStatement) {
				resolveNode = ((VariableDeclarationStatement)expr).Type;
			} else {
				resolveNode = expr;
			}
			try {
				var root = expr.AncestorsAndSelf.FirstOrDefault(n => n is EntityDeclaration || n is SyntaxTree);
				if (root == null) {
					return null;
				}
				var curState = GetState();
				// current member needs to be in the setter because of the 'value' parameter
				if (root is Accessor) {
					var prop = curState.CurrentMember as IProperty;
					if (prop != null && prop.CanSet && (root.Role == IndexerDeclaration.SetterRole || root.Role == PropertyDeclaration.SetterRole))
					    curState = curState.WithCurrentMember(prop.Setter);
				}

				// Rood should be the 'body' - otherwise the state -> current member isn't correct.
				var body = root.Children.FirstOrDefault(r => r.Role == Roles.Body);
				if (body != null && body.Contains(expr.StartLocation))
					root = body;

				var csResolver = CompletionContextProvider.GetResolver (curState, root);
				var result = csResolver.Resolve(resolveNode);
				var state = csResolver.GetResolverStateBefore(resolveNode);
				if (state.CurrentMember == null)
					state = state.WithCurrentMember(curState.CurrentMember);
				if (state.CurrentTypeDefinition == null)
					state = state.WithCurrentTypeDefinition(curState.CurrentTypeDefinition);
				if (state.CurrentUsingScope == null)
					state = state.WithCurrentUsingScope(curState.CurrentUsingScope);
				return new ExpressionResolveResult(result, state, csResolver);
			} catch (Exception e) {
				Console.WriteLine(e);
				return null;
			}
		}
		
		#endregion
	}
}