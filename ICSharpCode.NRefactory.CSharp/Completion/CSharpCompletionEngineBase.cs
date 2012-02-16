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

		public CompilationUnit Unit { get; private set; }

		public CSharpParsedFile CSharpParsedFile { get; private set; }

		public IProjectContent ProjectContent { get; private set; }
		
		ICompilation compilation;

		protected ICompilation Compilation {
			get {
				if (compilation == null)
					compilation = ProjectContent.Resolve (ctx).Compilation;
				return compilation;
			}
		}
		#endregion
		
		protected CSharpCompletionEngineBase (IProjectContent content, CSharpTypeResolveContext ctx, CompilationUnit unit, CSharpParsedFile parsedFile)
		{
			if (content == null)
				throw new ArgumentNullException ("content");
			if (ctx == null)
				throw new ArgumentNullException ("ctx");
			if (unit == null)
				throw new ArgumentNullException ("unit");
			if (parsedFile == null)
				throw new ArgumentNullException ("parsedFile");
			
			this.ProjectContent = content;
			this.ctx = ctx;
			this.Unit = unit;
			this.CSharpParsedFile = parsedFile;
		}
		
		
		public IMemberProvider MemberProvider {
			get;
			set;
		}
		
		protected void SetOffset (int offset)
		{
			Reset ();
			
			this.offset = offset;
			this.location = document.GetLocation (offset);
			var provider = MemberProvider ?? new DefaultMemberProvider (this);
			provider.GetCurrentMembers (offset, out currentType, out currentMember);
		}
		
		#region Context helper methods
		protected bool IsInsideCommentOrString ()
		{
			var text = GetMemberTextToCaret ();
			bool inSingleComment = false, inString = false, inVerbatimString = false, inChar = false, inMultiLineComment = false;
			
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
			
			return inSingleComment || inString || inVerbatimString || inChar || inMultiLineComment;
		}
		
		protected bool IsInsideComment (int offset)
		{
			var loc = document.GetLocation (offset);
			return Unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Comment> (loc.Line, loc.Column) != null;
		}
		
		protected bool IsInsideDocComment ()
		{
			var loc = document.GetLocation (offset);
			var cmt = Unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Comment> (loc.Line, loc.Column - 1);
			return cmt != null && cmt.CommentType == CommentType.Documentation;
		}
		
		protected bool IsInsideString (int offset)
		{
			
			var loc = document.GetLocation (offset);
			var expr = Unit.GetNodeAt<PrimitiveExpression> (loc.Line, loc.Column);
			return expr != null && expr.Value is string;
		}
		
		protected CSharpResolver GetState ()
		{
			return new CSharpResolver (ctx);
			/*var state = new CSharpResolver (ctx);
			
			state.CurrentMember = currentMember;
			state.CurrentTypeDefinition = currentType;
			state.CurrentUsingScope = CSharpParsedFile.GetUsingScope (location);
			if (state.CurrentMember != null) {
				var node = Unit.GetNodeAt (location);
				if (node == null)
					return state;
				var navigator = new NodeListResolveVisitorNavigator (new[] { node });
				var visitor = new ResolveVisitor (state, CSharpParsedFile, navigator);
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
		Stack<Tuple<char, int>> GetBracketStack (string memberText)
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
				default :
					break;
				}
			}
			return bracketStack;
		}
		
		protected void AppendMissingClosingBrackets (StringBuilder wrapper, string memberText, bool appendSemicolon)
		{
			var bracketStack = GetBracketStack (memberText);
			bool didAppendSemicolon = !appendSemicolon;
			char lastBracket = '\0';
			while (bracketStack.Count > 0) {
				var t = bracketStack.Pop ();
				switch (t.Item1) {
				case '(':
					wrapper.Append (')');
					didAppendSemicolon = false;
					lastBracket = ')';
					break;
				case '[':
					wrapper.Append (']');
					didAppendSemicolon = false;
					lastBracket = ']';
					break;
				case '<':
					wrapper.Append ('>');
					didAppendSemicolon = false;
					lastBracket = '>';
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
							if (ch == 'y' && memberText [o - 1] == 'r' && memberText [o - 2] == 't') {
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
			if (currentMember == null && lastBracket == ']') {
				// attribute context
				wrapper.Append ("class GenAttr {}");
			} else {
				if (!didAppendSemicolon)
					wrapper.Append (';');
			}
		}

		protected CompilationUnit ParseStub (string continuation, bool appendSemicolon = true, string afterContinuation = null)
		{
			var mt = GetMemberTextToCaret ();
			if (mt == null)
				return null;
			
			string memberText = mt.Item1;
			bool wrapInClass = mt.Item2;
			
			var wrapper = new StringBuilder ();
			
			if (wrapInClass) {
/*				foreach (var child in Unit.Children) {
					if (child is UsingDeclaration) {
						var offset = document.GetOffset (child.StartLocation);
						wrapper.Append (document.GetText (offset, document.GetOffset (child.EndLocation) - offset));
					}
				}*/
				wrapper.Append ("class Stub {");
				wrapper.AppendLine ();
			}
			
			wrapper.Append (memberText);
			wrapper.Append (continuation);
			AppendMissingClosingBrackets (wrapper, memberText, appendSemicolon);
			wrapper.Append (afterContinuation);
			
			if (wrapInClass)
				wrapper.Append ('}');
			
			TextLocation memberLocation;
			if (currentMember != null && currentType != null && currentType.Kind != TypeKind.Enum) {
				memberLocation = currentMember.Region.Begin;
			} else if (currentType != null) {
				memberLocation = currentType.Region.Begin;
			} else {
				memberLocation = new TextLocation (1, 1);
			}
			                   
			using (var stream = new System.IO.StringReader (wrapper.ToString ())) {
				try {
					var parser = new CSharpParser ();
					return parser.Parse (stream, "stub.cs", wrapInClass ? memberLocation.Line - 2 : 0);
				} catch (Exception) {
					Console.WriteLine ("------");
					Console.WriteLine (wrapper);
					throw;
				}
			}
		}
		
		string cachedText = null;
		
		protected virtual void Reset ()
		{
			cachedText = null;
		}
		
		protected Tuple<string, bool> GetMemberTextToCaret ()
		{
			int startOffset;
			if (currentMember != null && currentType != null && currentType.Kind != TypeKind.Enum) {
				startOffset = document.GetOffset (currentMember.Region.BeginLine, currentMember.Region.BeginColumn);
			} else if (currentType != null) {
				startOffset = document.GetOffset (currentType.Region.BeginLine, currentType.Region.BeginColumn);
			} else {
				startOffset = 0;
			}
			while (startOffset > 0) {
				char ch = document.GetCharAt (startOffset - 1);
				if (ch != ' ' && ch != '\t')
					break;
				--startOffset;
			}
			if (cachedText == null)
				cachedText = document.GetText (startOffset, offset - startOffset);
			
			return Tuple.Create (cachedText, startOffset != 0);
		}
		
		protected Tuple<CSharpParsedFile, AstNode, CompilationUnit> GetInvocationBeforeCursor (bool afterBracket)
		{
			CompilationUnit baseUnit;
			if (currentMember == null) {
				baseUnit = ParseStub ("", false);
				var section = baseUnit.GetNodeAt<AttributeSection> (location.Line, location.Column - 2);
				var attr = section != null ? section.Attributes.LastOrDefault () : null;
				if (attr != null) {
					// insert target type into compilation unit, to respect the 
					attr.Remove ();
					var node = Unit.GetNodeAt (location) ?? Unit;
					node.AddChild (attr, AttributeSection.AttributeRole);
					return Tuple.Create (CSharpParsedFile, (AstNode)attr, Unit);
				}
			}
			if (currentMember == null && currentType == null) {
				return null;
			}
			baseUnit = ParseStub (afterBracket ? "" : "x");
			
			var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			var mref = baseUnit.GetNodeAt (location.Line, location.Column - 1, n => n is InvocationExpression || n is ObjectCreateExpression); 
			AstNode expr = null;
			if (mref is InvocationExpression) {
				expr = ((InvocationExpression)mref).Target;
			} else if (mref is ObjectCreateExpression) {
				expr = mref;
			} else {
				baseUnit = ParseStub (")};", false);
				mref = baseUnit.GetNodeAt (location.Line, location.Column - 1, n => n is InvocationExpression || n is ObjectCreateExpression); 
				if (mref is InvocationExpression) {
					expr = ((InvocationExpression)mref).Target;
				} else if (mref is ObjectCreateExpression) {
					expr = mref;
				}
				if (expr == null)
					return null;
			}
			var member = Unit.GetNodeAt<AttributedNode> (memberLocation);
			var member2 = baseUnit.GetNodeAt<AttributedNode> (memberLocation);
			member2.Remove ();
			member.ReplaceWith (member2);
			var tsvisitor = new TypeSystemConvertVisitor (CSharpParsedFile.FileName);
			Unit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, (AstNode)expr, Unit);
		}
		
		protected Tuple<ResolveResult, CSharpResolver> ResolveExpression (CSharpParsedFile file, AstNode expr, CompilationUnit unit)
		{
			if (expr == null)
				return null;
			AstNode resolveNode;
			if (expr is Expression || expr is AstType) {
				resolveNode = expr;
			} else if (expr is VariableDeclarationStatement) {
				resolveNode = ((VariableDeclarationStatement)expr).Type;
			} else {
				resolveNode = expr;
			}
			
//			var newContent = ProjectContent.UpdateProjectContent (CSharpParsedFile, file);
			
			var csResolver = new CSharpAstResolver(new CSharpResolver (ctx), unit, CSharpParsedFile);
			
			var result = csResolver.Resolve (resolveNode);
			var state = csResolver.GetResolverStateBefore (resolveNode);
			return Tuple.Create (result, state);
		}
		
		protected static void Print (AstNode node)
		{
			var v = new CSharpOutputVisitor (Console.Out, new CSharpFormattingOptions ());
			node.AcceptVisitor (v, null);
		}
		
		#endregion
		
		class DefaultMemberProvider : IMemberProvider
	{
		CSharpCompletionEngineBase engine;
		
		
		public DefaultMemberProvider (CSharpCompletionEngineBase engine)
		{
			this.engine = engine;
		}
		
		public void GetCurrentMembers (int offset, out IUnresolvedTypeDefinition currentType, out IUnresolvedMember currentMember)
		{
			var document = engine.document;
			var location = engine.location;
			
			currentType = null;
			
			foreach (var type in engine.CSharpParsedFile.TopLevelTypeDefinitions) {
				if (type.Region.Begin < location)
					currentType = type;
			}
			currentType = FindInnerType (currentType, location);
			
			// location is beyond last reported end region, now we need to check, if the end region changed
			if (currentType != null && currentType.Region.End < location) {
				if (!IsInsideType (currentType, location))
					currentType = null;
			}
			currentMember = null;
			if (currentType != null) {
				foreach (var member in currentType.Members) {
					if (member.Region.Begin < location && (currentMember == null || currentMember.Region.Begin < member.Region.Begin))
						currentMember = member;
				}
			}
			
			// location is beyond last reported end region, now we need to check, if the end region changed
			// NOTE: Enums are a special case, there the "last" field needs to be treated as current member
			if (currentMember != null && currentMember.Region.End < location && currentType.Kind != TypeKind.Enum) {
				if (!IsInsideType (currentMember, location))
					currentMember = null;
			}
			var stack = engine.GetBracketStack (engine.GetMemberTextToCaret ().Item1);
			if (stack.Count == 0)
				currentMember = null;
		}

		IUnresolvedTypeDefinition FindInnerType (IUnresolvedTypeDefinition parent, TextLocation location)
		{
			if (parent == null)
				return null;
			var currentType = parent;
			foreach (var type in parent.NestedTypes) {
				if (type.Region.Begin < location  && location < type.Region.End)
					currentType = FindInnerType (type, location);
			}
			
			return currentType;
		}
		
		bool IsInsideType (IUnresolvedEntity currentType, TextLocation location)
		{
			var document = engine.document;
			
			int startOffset = document.GetOffset (currentType.Region.Begin);
			int endOffset = document.GetOffset (location);
			bool foundEndBracket = false;
		
			var bracketStack = new Stack<char> ();
		
			bool isInString = false, isInChar = false;
			bool isInLineComment = false, isInBlockComment = false;
			
			for (int i = startOffset; i < endOffset; i++) {
				char ch = document.GetCharAt (i);
				switch (ch) {
					case '(':
					case '[':
					case '{':
						if (!isInString && !isInChar && !isInLineComment && !isInBlockComment)
							bracketStack.Push (ch);
						break;
					case ')':
					case ']':
					case '}':
						if (!isInString && !isInChar && !isInLineComment && !isInBlockComment)
						if (bracketStack.Count > 0)
							bracketStack.Pop ();
						break;
					case '\r':
					case '\n':
						isInLineComment = false;
						break;
					case '/':
						if (isInBlockComment) {
							if (i > 0 && document.GetCharAt (i - 1) == '*') 
								isInBlockComment = false;
						} else if (!isInString && !isInChar && i + 1 < document.TextLength) {
							char nextChar = document.GetCharAt (i + 1);
							if (nextChar == '/')
								isInLineComment = true;
							if (!isInLineComment && nextChar == '*')
								isInBlockComment = true;
						}
						break;
					case '"':
						if (!(isInChar || isInLineComment || isInBlockComment)) 
							isInString = !isInString;
						break;
					case '\'':
						if (!(isInString || isInLineComment || isInBlockComment)) 
							isInChar = !isInChar;
						break;
					default :
						break;
					}
				}
			return bracketStack.Any (t => t == '{');
		}		
	}
	
	}
}