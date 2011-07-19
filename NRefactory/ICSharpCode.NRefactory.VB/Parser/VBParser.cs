// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Visitors;

namespace ICSharpCode.NRefactory.VB.Parser
{
	internal partial class VBParser : IDisposable
	{
		VBLexer lexer;
		Stack<AstNode> stack;
		CompilationUnit compilationUnit;
		int errDist = MinErrDist;
		
		const int    MinErrDist   = 2;
		const string ErrMsgFormat = "-- line {0} col {1}: {2}";  // 0=line, 1=column, 2=text
		
		public VBParser(VBLexer lexer)
		{
			this.errors = lexer.Errors;
			errors.SynErr = new ErrorCodeProc(SynErr);
			this.lexer = (VBLexer)lexer;
			this.stack = new Stack<AstNode>();
		}
		
		#region Infrastructure
		void NodeStart(AstNode node)
		{
			stack.Push(node);
		}
		
		void NodeEnd(AstNode currentNode, Role role)
		{
			AstNode node = stack.Pop();
			Debug.Assert(currentNode == node);
			stack.Peek().AddChildUntyped(node, role);
		}
		
		void AddTerminal(Role<VBTokenNode> role)
		{
			stack.Peek().AddChild(new VBTokenNode(t.Location, t.EndLocation), role);
		}
		
		void AddChild<T>(T childNode, Role<T> role) where T : AstNode
		{
			if (childNode != null) {
				stack.Peek().AddChild(childNode, role);
			}
		}
		
		StringBuilder qualidentBuilder = new StringBuilder();

		Token t
		{
			[System.Diagnostics.DebuggerStepThrough]
			get {
				return lexer.Token;
			}
		}
		
		Token la
		{
			[System.Diagnostics.DebuggerStepThrough]
			get {
				return lexer.LookAhead;
			}
		}

		Token Peek(int n)
		{
			lexer.StartPeek();
			Token x = la;
			while (n > 0) {
				x = lexer.Peek();
				n--;
			}
			return x;
		}

		public void Error(string s)
		{
			if (errDist >= MinErrDist) {
				this.Errors.Error(la.line, la.col, s);
			}
			errDist = 0;
		}
		#endregion
		
		#region Parse
		public void Parse()
		{
			ParseRoot();
		}
		
		public AstType ParseAstType()
		{
			// TODO
			return null;
		}
		
//		public Expression ParseExpression()
//		{
//			lexer.SetInitialContext(SnippetType.Expression);
//			lexer.NextToken();
//			Location startLocation = la.Location;
//			Expression expr;
//			Expr(out expr);
//			while (la.kind == Tokens.EOL) lexer.NextToken();
//			if (expr != null) {
//				expr.StartLocation = startLocation;
//				expr.EndLocation = t.EndLocation;
//				expr.AcceptVisitor(new SetParentVisitor(), null);
//			}
//			Expect(Tokens.EOF);
//			return expr;
//		}
		
//		public BlockStatement ParseBlock()
//		{
//			lexer.NextToken();
//			compilationUnit = new CompilationUnit();
//
//			Location startLocation = la.Location;
//			Statement st;
//			Block(out st);
//			if (st != null) {
//				st.StartLocation = startLocation;
//				if (t != null)
//					st.EndLocation = t.EndLocation;
//				else
//					st.EndLocation = la.Location;
//				st.AcceptVisitor(new SetParentVisitor(), null);
//			}
//			Expect(Tokens.EOF);
//			return st as BlockStatement;
//		}
		
//		public List<AstNode> ParseTypeMembers()
//		{
//			lexer.NextToken();
//			TypeDeclaration newType = new TypeDeclaration(Modifiers.None, null);
//			BlockStart(newType);
//			ClassBody(newType);
//			BlockEnd();
//			Expect(Tokens.EOF);
//			newType.AcceptVisitor(new SetParentVisitor(), null);
//			return newType.Children;
//		}
		#endregion
		
		#region Conflict Resolvers
		bool IsAliasImportsClause()
		{
			return IsIdentifierToken(la) && Peek(1).Kind == Tokens.Assign;
		}
		
		static bool IsIdentifierToken(Token tk)
		{
			return Tokens.IdentifierTokens[tk.kind] || tk.kind == Tokens.Identifier;
		}
		#endregion
		
		/* True, if "." is followed by an ident */
		bool DotAndIdentOrKw () {
			int peek = Peek(1).kind;
			return la.kind == Tokens.Dot && (peek == Tokens.Identifier || peek >= Tokens.AddHandler);
		}
		
		bool IsIdentifiedExpressionRange()
		{
			// t = Select
			// la = Identifier
			// Peek(1) = As or Assign
			Token token = Peek(1);
			return IsIdentifierToken(la) && (token.kind == Tokens.As || token.kind == Tokens.Assign);
		}
		
		bool IsQueryExpression()
		{
			return (la.kind == Tokens.From || la.kind == Tokens.Aggregate) && IsIdentifierToken(Peek(1));
		}
		
		bool IsEndStmtAhead()
		{
			int peek = Peek(1).kind;
			return la.kind == Tokens.End && (peek == Tokens.EOL || peek == Tokens.Colon);
		}

		bool IsNotClosingParenthesis() {
			return la.kind != Tokens.CloseParenthesis;
		}

		/*
			True, if ident is followed by "=" or by ":" and "="
		 */
		bool IsNamedAssign() {
			return Peek(1).kind == Tokens.ColonAssign;
		}

		bool IsObjectCreation() {
			return la.kind == Tokens.As && Peek(1).kind == Tokens.New;
		}
		
		bool IsNewExpression() {
			return la.kind == Tokens.New;
		}

		/*
			True, if "<" is followed by the ident "assembly" or "module"
		 */
		bool IsGlobalAttrTarget () {
			Token pt = Peek(1);
			return la.kind == Tokens.LessThan && ( string.Equals(pt.val, "assembly", StringComparison.InvariantCultureIgnoreCase) || string.Equals(pt.val, "module", StringComparison.InvariantCultureIgnoreCase));
		}

		/*
			True if the next token is a "(" and is followed by "," or ")"
		 */
		bool IsDims()
		{
			int peek = Peek(1).kind;
			return la.kind == Tokens.OpenParenthesis
				&& (peek == Tokens.Comma || peek == Tokens.CloseParenthesis);
		}
		
		/*
			True if the next token is an identifier
		 */
		bool IsLoopVariableDeclaration()
		{
			if (!IsIdentifierToken(la))
				return false;
			lexer.StartPeek();
			Token x = lexer.Peek();
			if (x.kind == Tokens.OpenParenthesis) {
				do {
					x = lexer.Peek();
				} while (x.kind == Tokens.Comma);
				if (x.kind != Tokens.CloseParenthesis)
					return false;
				x = lexer.Peek();
			}
			return x.kind == Tokens.As || x.kind == Tokens.Assign;
		}

		bool IsSize()
		{
			return la.kind == Tokens.OpenParenthesis;
		}

		/*
			True, if the comma is not a trailing one,
			like the last one in: a, b, c,
		 */
		bool NotFinalComma() {
			int peek = Peek(1).kind;
			return la.kind == Tokens.Comma &&
				peek != Tokens.CloseCurlyBrace;
		}

		/*
			True, if the next token is "Else" and this one
			if followed by "If"
		 */
		bool IsElseIf()
		{
			int peek = Peek(1).kind;
			return la.kind == Tokens.Else && peek == Tokens.If;
		}

		/*
	True if the next token is goto and this one is
	followed by minus ("-") (this is allowd in in
	error clauses)
		 */
		bool IsNegativeLabelName()
		{
			int peek = Peek(1).kind;
			return la.kind == Tokens.GoTo && peek == Tokens.Minus;
		}

		/*
	True if the next statement is a "Resume next" statement
		 */
		bool IsResumeNext()
		{
			int peek = Peek(1).kind;
			return la.kind == Tokens.Resume && peek == Tokens.Next;
		}
		
		/// <summary>
		/// Returns True, if ident/literal integer is followed by ":"
		/// </summary>
		bool IsLabel()
		{
			return (la.kind == Tokens.Identifier || la.kind == Tokens.LiteralInteger)
				&& Peek(1).kind == Tokens.Colon;
		}
		
		/// <summary>
		/// Returns true if a property declaration is an automatic property.
		/// </summary>
		bool IsAutomaticProperty()
		{
			lexer.StartPeek();
			Token tn = la;
			int braceCount = 0;

			// look for attributes
			while (tn.kind == Tokens.LessThan) {
				while (braceCount > 0 || tn.kind != Tokens.GreaterThan) {
					tn = lexer.Peek();
					if (tn.kind == Tokens.OpenParenthesis)
						braceCount++;
					if (tn.kind == Tokens.CloseParenthesis)
						braceCount--;
				}
				Debug.Assert(tn.kind == Tokens.GreaterThan);
				tn = lexer.Peek();
			}
			
			// look for modifiers
			var allowedTokens = new[] {
				Tokens.Public, Tokens.Protected,
				Tokens.Friend, Tokens.Private
			};

			while (allowedTokens.Contains(tn.kind))
				tn = lexer.Peek();
			
			if (tn.Kind != Tokens.Get && tn.Kind != Tokens.Set)
				return true;

			return false;
		}

		bool IsNotStatementSeparator()
		{
			return la.kind == Tokens.Colon && Peek(1).kind == Tokens.EOL;
		}

		static bool IsMustOverride(AttributedNode node)
		{
			return node.Modifiers.HasFlag(Modifiers.MustOverride);
		}

		/* Writes the type name represented through the expression into the string builder. */
		/* Returns true when the expression was converted successfully, returns false when */
		/* There was an unknown expression (e.g. TypeReferenceExpression) in it */
//		bool WriteFullTypeName(StringBuilder b, Expression expr)
//		{
//			MemberReferenceExpression fre = expr as MemberReferenceExpression;
//			if (fre != null) {
//				bool result = WriteFullTypeName(b, fre.TargetObject);
//				if (b.Length > 0) b.Append('.');
//				b.Append(fre.MemberName);
//				return result;
//			} else if (expr is SimpleNameExpression) {
//				b.Append(((SimpleNameExpression)expr).Identifier);
//				return true;
//			} else {
//				return false;
//			}
//		}
		
		void EnsureIsZero(Expression expr)
		{
			if (!(expr is PrimitiveExpression) || (expr as PrimitiveExpression).StringValue != "0")
				Error("lower bound of array must be zero");
		}
		
		public bool ParseMethodBodies { get; set; }
		
		public VBLexer Lexer {
			get {
				return lexer;
			}
		}
		
		public Errors Errors {
			get {
				return errors;
			}
		}
		
		public CompilationUnit CompilationUnit {
			get {
				return compilationUnit;
			}
		}
		
		void SynErr(int n)
		{
			if (errDist >= MinErrDist) {
				errors.SynErr(lexer.LookAhead.line, lexer.LookAhead.col, n);
			}
			errDist = 0;
		}
		
		void SemErr(string msg)
		{
			if (errDist >= MinErrDist) {
				errors.Error(lexer.Token.line, lexer.Token.col, msg);
			}
			errDist = 0;
		}
		
		void Expect(int n)
		{
			if (lexer.LookAhead.kind == n) {
				lexer.NextToken();
			} else {
				SynErr(n);
			}
		}
		
		#region System.IDisposable interface implementation
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
		public void Dispose()
		{
			errors = null;
			if (lexer != null) {
				lexer.Dispose();
			}
			lexer = null;
		}
		#endregion
	}
}
