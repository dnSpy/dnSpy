// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.NRefactory.Visitors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Parser.CSharp
{
	internal sealed partial class Parser : AbstractParser
	{
		Lexer lexer;
		Stack<INode> blockStack;
		
		public Parser(ILexer lexer) : base(lexer)
		{
			this.lexer = (Lexer)lexer;
			this.blockStack = new Stack<INode>();
		}
		
		void BlockStart(INode block)
		{
			blockStack.Push(block);
		}
		
		void BlockEnd()
		{
			blockStack.Pop();
		}
		
		void AddChild(INode childNode)
		{
			if (childNode != null) {
				INode parent = (INode)blockStack.Peek();
				parent.Children.Add(childNode);
				childNode.Parent = parent;
			}
		}
		
		StringBuilder qualidentBuilder = new StringBuilder();

		Token t {
			[System.Diagnostics.DebuggerStepThrough]
			get {
				return lexer.Token;
			}
		}

		Token la {
			[System.Diagnostics.DebuggerStepThrough]
			get {
				return lexer.LookAhead;
			}
		}

		public void Error(string s)
		{
			if (errDist >= MinErrDist) {
				this.Errors.Error(la.line, la.col, s);
			}
			errDist = 0;
		}

		public override void Parse()
		{
			ParseRoot();
			compilationUnit.AcceptVisitor(new SetParentVisitor(), null);
		}
		
		public override TypeReference ParseTypeReference ()
		{
			lexer.NextToken();
			TypeReference type;
			Type(out type);
			return type;
		}

		public override Expression ParseExpression()
		{
			lexer.NextToken();
			Location startLocation = la.Location;
			Expression expr;
			Expr(out expr);
			// SEMICOLON HACK : without a trailing semicolon, parsing expressions does not work correctly
			if (la.kind == Tokens.Semicolon) lexer.NextToken();
			if (expr != null) {
				if (expr.StartLocation.IsEmpty)
					expr.StartLocation = startLocation;
				if (expr.EndLocation.IsEmpty)
					expr.EndLocation = (t ?? la).EndLocation;
				expr.AcceptVisitor(new SetParentVisitor(), null);
			}
			Expect(Tokens.EOF);
			return expr;
		}
		
		public override BlockStatement ParseBlock()
		{
			lexer.NextToken();
			compilationUnit = new CompilationUnit();
			
			BlockStatement blockStmt = new BlockStatement();
			blockStmt.StartLocation = la.Location;
			BlockStart(blockStmt);
			
			while (la.kind != Tokens.EOF) {
				Token oldLa = la;
				Statement();
				if (la == oldLa) {
					// did not advance lexer position, we cannot parse this as a statement block
					return null;
				}
			}
			
			BlockEnd();
			// if lexer didn't return any tokens, use position of the EOF token in "la"
			blockStmt.EndLocation = (t ?? la).EndLocation;
			Expect(Tokens.EOF);
			blockStmt.AcceptVisitor(new SetParentVisitor(), null);
			return blockStmt;
		}
		
		public override List<INode> ParseTypeMembers()
		{
			lexer.NextToken();
			compilationUnit = new CompilationUnit();
			
			TypeDeclaration newType = new TypeDeclaration(Modifiers.None, null);
			BlockStart(newType);
			ClassBody();
			BlockEnd();
			Expect(Tokens.EOF);
			newType.AcceptVisitor(new SetParentVisitor(), null);
			return newType.Children;
		}
		
		// Begin ISTypeCast
		bool IsTypeCast()
		{
			if (la.kind != Tokens.OpenParenthesis) {
				return false;
			}
			bool isPossibleExpression = true;
			
			lexer.StartPeek();
			Token pt = lexer.Peek();
			
			if (!IsTypeNameOrKWForTypeCast(ref pt, ref isPossibleExpression)) {
				return false;
			}
			
			// ")"
			if (pt.kind != Tokens.CloseParenthesis) {
				return false;
			}
			if (isPossibleExpression) {
				// check successor
				pt = lexer.Peek();
				return Tokens.CastFollower[pt.kind];
			} else {
				// not possibly an expression: don't check cast follower
				return true;
			}
		}

		/* !!! Proceeds from current peek position !!! */
		bool IsTypeKWForTypeCast(ref Token pt)
		{
			if (Tokens.TypeKW[pt.kind]) {
				pt = lexer.Peek();
				return IsPointerOrDims(ref pt) && SkipQuestionMark(ref pt);
			} else if (pt.kind == Tokens.Void) {
				pt = lexer.Peek();
				return IsPointerOrDims(ref pt);
			}
			return false;
		}

		/* !!! Proceeds from current peek position !!! */
		bool IsTypeNameOrKWForTypeCast(ref Token pt, ref bool isPossibleExpression)
		{
			if (Tokens.TypeKW[pt.kind] || pt.kind == Tokens.Void) {
				isPossibleExpression = false;
				return IsTypeKWForTypeCast(ref pt);
			} else {
				return IsTypeNameForTypeCast(ref pt, ref isPossibleExpression);
			}
		}
		
		bool IsTypeNameOrKWForTypeCast(ref Token pt)
		{
			bool tmp = false;
			return IsTypeNameOrKWForTypeCast(ref pt, ref tmp);
		}

		// TypeName = ident [ "::" ident ] { ["<" TypeNameOrKW { "," TypeNameOrKW } ">" ] "." ident } ["?"] PointerOrDims
		/* !!! Proceeds from current peek position !!! */
		bool IsTypeNameForTypeCast(ref Token pt, ref bool isPossibleExpression)
		{
			// ident
			if (!IsIdentifierToken(pt)) {
				return false;
			}
			pt = Peek();
			// "::" ident
			if (pt.kind == Tokens.DoubleColon) {
				pt = Peek();
				if (!IsIdentifierToken(pt)) {
					return false;
				}
				pt = Peek();
			}
			// { ["<" TypeNameOrKW { "," TypeNameOrKW } ">" ] "." ident }
			while (true) {
				if (pt.kind == Tokens.LessThan) {
					do {
						pt = Peek();
						if (!IsTypeNameOrKWForTypeCast(ref pt)) {
							return false;
						}
					} while (pt.kind == Tokens.Comma);
					if (pt.kind != Tokens.GreaterThan) {
						return false;
					}
					pt = Peek();
				}
				if (pt.kind != Tokens.Dot)
					break;
				pt = Peek();
				if (pt.kind != Tokens.Identifier) {
					return false;
				}
				pt = Peek();
			}
			// ["?"]
			if (pt.kind == Tokens.Question) {
				pt = Peek();
			}
			if (pt.kind == Tokens.Times || pt.kind == Tokens.OpenSquareBracket) {
				isPossibleExpression = false;
				return IsPointerOrDims(ref pt);
			}
			return true;
		}
		// END IsTypeCast
		
		// Gets if the token is a possible token for an expression start
		// Is used to determine if "a is Type ? token" a the start of a ternary
		// expression or a type test for Nullable<Type>
		bool IsPossibleExpressionStart(int token)
		{
			return Tokens.CastFollower[token] || Tokens.UnaryOp[token];
		}
		
		// ( { [TypeNameOrKWForTypeCast] ident "," } )
		bool IsLambdaExpression()
		{
			if (la.kind != Tokens.OpenParenthesis) {
				return false;
			}
			StartPeek();
			Token pt = Peek();
			while (pt.kind != Tokens.CloseParenthesis) {
				if (pt.kind == Tokens.Out || pt.kind == Tokens.Ref) {
					pt = Peek();
				}
				if (!IsTypeNameOrKWForTypeCast(ref pt)) {
					return false;
				}
				if (IsIdentifierToken(pt)) {
					// make ident optional: if implicitly typed lambda arguments are used, IsTypeNameForTypeCast
					// has already accepted the identifier
					pt = Peek();
				}
				if (pt.kind == Tokens.CloseParenthesis) {
					break;
				}
				// require comma between parameters:
				if (pt.kind == Tokens.Comma) {
					pt = Peek();
				} else {
					return false;
				}
			}
			pt = Peek();
			return pt.kind == Tokens.LambdaArrow;
		}

		/* Checks whether the next sequences of tokens is a qualident *
		 * and returns the qualident string                           */
		/* !!! Proceeds from current peek position !!! */
		bool IsQualident(ref Token pt, out string qualident)
		{
			if (IsIdentifierToken(pt)) {
				qualidentBuilder.Length = 0; qualidentBuilder.Append(pt.val);
				pt = Peek();
				while (pt.kind == Tokens.Dot || pt.kind == Tokens.DoubleColon) {
					pt = Peek();
					if (!IsIdentifierToken(pt)) {
						qualident = String.Empty;
						return false;
					}
					qualidentBuilder.Append('.');
					qualidentBuilder.Append(pt.val);
					pt = Peek();
				}
				qualident = qualidentBuilder.ToString();
				return true;
			}
			qualident = String.Empty;
			return false;
		}

		/* Skips generic type extensions */
		/* !!! Proceeds from current peek position !!! */

		/* skip: { "*" | "[" { "," } "]" } */
		/* !!! Proceeds from current peek position !!! */
		bool IsPointerOrDims (ref Token pt)
		{
			for (;;) {
				if (pt.kind == Tokens.OpenSquareBracket) {
					do pt = Peek();
					while (pt.kind == Tokens.Comma);
					if (pt.kind != Tokens.CloseSquareBracket) return false;
				} else if (pt.kind != Tokens.Times) break;
				pt = Peek();
			}
			return true;
		}

		/* Return the n-th token after the current lookahead token */
		void StartPeek()
		{
			lexer.StartPeek();
		}

		Token Peek()
		{
			return lexer.Peek();
		}

		Token Peek (int n)
		{
			lexer.StartPeek();
			Token x = la;
			while (n > 0) {
				x = lexer.Peek();
				n--;
			}
			return x;
		}

		/*-----------------------------------------------------------------*
		 * Resolver routines to resolve LL(1) conflicts:                   *                                                  *
		 * These resolution routine return a boolean value that indicates  *
		 * whether the alternative at hand shall be choosen or not.        *
		 * They are used in IF ( ... ) expressions.                        *
		 *-----------------------------------------------------------------*/

		/* True, if ident is followed by "=" */
		bool IdentAndAsgn ()
		{
			return IsIdentifierToken(la) && Peek(1).kind == Tokens.Assign;
		}
		
		bool IdentAndDoubleColon ()
		{
			return IsIdentifierToken(la) && Peek(1).kind == Tokens.DoubleColon;
		}

		bool IsAssignment () { return IdentAndAsgn(); }

		/* True, if ident is followed by ",", "=", "[" or ";" */
		bool IsVarDecl () {
			int peek = Peek(1).kind;
			return IsIdentifierToken(la) &&
				(peek == Tokens.Comma || peek == Tokens.Assign || peek == Tokens.Semicolon || peek == Tokens.OpenSquareBracket);
		}

		/* True, if the comma is not a trailing one, *
		 * like the last one in: a, b, c,            */
		bool NotFinalComma () {
			int peek = Peek(1).kind;
			return la.kind == Tokens.Comma &&
				peek != Tokens.CloseCurlyBrace && peek != Tokens.CloseSquareBracket;
		}

		/* True, if "void" is followed by "*" */
		bool NotVoidPointer () {
			return la.kind == Tokens.Void && Peek(1).kind != Tokens.Times;
		}

		/* True, if "checked" or "unchecked" are followed by "{" */
		bool UnCheckedAndLBrace () {
			return (la.kind == Tokens.Checked || la.kind == Tokens.Unchecked) &&
				Peek(1).kind == Tokens.OpenCurlyBrace;
		}

		/* True, if "." is followed by an ident */
		bool DotAndIdent () {
			return la.kind == Tokens.Dot && IsIdentifierToken(Peek(1));
		}

		/* True, if ident is followed by ":" */
		bool IdentAndColon () {
			return IsIdentifierToken(la) && Peek(1).kind == Tokens.Colon;
		}

		bool IsLabel () { return IdentAndColon(); }

		/* True, if ident is followed by "(" */
		bool IdentAndLPar () {
			return IsIdentifierToken(la) && Peek(1).kind == Tokens.OpenParenthesis;
		}

		/* True, if "catch" is followed by "(" */
		bool CatchAndLPar () {
			return la.kind == Tokens.Catch && Peek(1).kind == Tokens.OpenParenthesis;
		}
		bool IsTypedCatch () { return CatchAndLPar(); }

		/* True, if "[" is followed by the ident "assembly" */
		bool IsGlobalAttrTarget () {
			Token pt = Peek(1);
			return la.kind == Tokens.OpenSquareBracket &&
				IsIdentifierToken(pt) && (pt.val == "assembly" || pt.val == "module");
		}

		/* True, if "[" is followed by "," or "]" */
		bool LBrackAndCommaOrRBrack () {
			int peek = Peek(1).kind;
			return la.kind == Tokens.OpenSquareBracket &&
				(peek == Tokens.Comma || peek == Tokens.CloseSquareBracket);
		}

		/* True, if "[" is followed by "," or "]" */
		/* or if the current token is "*"         */
		bool TimesOrLBrackAndCommaOrRBrack () {
			return la.kind == Tokens.Times || LBrackAndCommaOrRBrack();
		}
		bool IsPointerOrDims () { return TimesOrLBrackAndCommaOrRBrack(); }
		bool IsPointer () { return la.kind == Tokens.Times; }


		bool SkipGeneric(ref Token pt)
		{
			if (pt.kind == Tokens.LessThan) {
				do {
					pt = Peek();
					if (!IsTypeNameOrKWForTypeCast(ref pt)) return false;
				} while (pt.kind == Tokens.Comma);
				if (pt.kind != Tokens.GreaterThan) return false;
				pt = Peek();
			}
			return true;
		}
		bool SkipQuestionMark(ref Token pt)
		{
			if (pt.kind == Tokens.Question) {
				pt = Peek();
			}
			return true;
		}

		/* True, if lookahead is a primitive type keyword, or */
		/* if it is a type declaration followed by an ident   */
		bool IsLocalVarDecl () {
			if (IsYieldStatement()) {
				return false;
			}
			if ((Tokens.TypeKW[la.kind] && Peek(1).kind != Tokens.Dot) || la.kind == Tokens.Void) {
				return true;
			}
			
			StartPeek();
			Token pt = la;
			return IsTypeNameOrKWForTypeCast(ref pt) && IsIdentifierToken(pt);
		}

		/* True if lookahead is a type argument list (<...>) followed by
		 * one of "(  )  ]  }  :  ;  ,  .  ?  ==  !=" */
		bool IsGenericInSimpleNameOrMemberAccess()
		{
			Token t = la;
			if (t.kind != Tokens.LessThan) return false;
			StartPeek();
			return SkipGeneric(ref t) && Tokens.GenericFollower[t.kind];
		}

		bool IsExplicitInterfaceImplementation()
		{
			StartPeek();
			Token pt = la;
			pt = Peek();
			if (pt.kind == Tokens.Dot || pt.kind == Tokens.DoubleColon)
				return true;
			if (pt.kind == Tokens.LessThan) {
				if (SkipGeneric(ref pt))
					return pt.kind == Tokens.Dot;
			}
			return false;
		}

		/* True, if lookahead ident is "yield" and than follows a break or return */
		bool IsYieldStatement () {
			return la.kind == Tokens.Yield && (Peek(1).kind == Tokens.Return || Peek(1).kind == Tokens.Break);
		}

		/* True, if lookahead is a local attribute target specifier, *
		 * i.e. one of "event", "return", "field", "method",         *
		 *             "module", "param", "property", or "type"      */
		bool IsLocalAttrTarget () {
			int cur = la.kind;
			string val = la.val;

			return (cur == Tokens.Event || cur == Tokens.Return ||
			        Tokens.IdentifierTokens[cur]) &&
				Peek(1).kind == Tokens.Colon;
		}

		bool IsShiftRight()
		{
			Token next = Peek(1);
			// TODO : Add col test (seems not to work, lexer bug...) :  && la.col == next.col - 1
			return (la.kind == Tokens.GreaterThan && next.kind == Tokens.GreaterThan);
		}

		bool IsGenericExpression(Expression expr)
		{
			if (expr is IdentifierExpression)
				return ((IdentifierExpression)expr).TypeArguments.Count > 0;
			else if (expr is MemberReferenceExpression)
				return ((MemberReferenceExpression)expr).TypeArguments.Count > 0;
			else
				return false;
		}

		bool ShouldConvertTargetExpressionToTypeReference(Expression targetExpr)
		{
			if (targetExpr is IdentifierExpression)
				return ((IdentifierExpression)targetExpr).TypeArguments.Count > 0;
			else if (targetExpr is MemberReferenceExpression)
				return ((MemberReferenceExpression)targetExpr).TypeArguments.Count > 0;
			else
				return false;
		}
		
		TypeReference GetTypeReferenceFromExpression(Expression expr)
		{
			if (expr is TypeReferenceExpression)
				return (expr as TypeReferenceExpression).TypeReference;
			
			IdentifierExpression ident = expr as IdentifierExpression;
			if (ident != null) {
				return new TypeReference(ident.Identifier, ident.TypeArguments);
			}
			
			MemberReferenceExpression member = expr as MemberReferenceExpression;
			if (member != null) {
				TypeReference targetType = GetTypeReferenceFromExpression(member.TargetObject);
				if (targetType != null) {
					if (targetType.GenericTypes.Count == 0 && targetType.IsArrayType == false) {
						TypeReference tr = targetType.Clone();
						tr.Type = tr.Type + "." + member.MemberName;
						tr.GenericTypes.AddRange(member.TypeArguments);
						return tr;
					} else {
						return new InnerClassTypeReference(targetType, member.MemberName, member.TypeArguments);
					}
				}
			}
			return null;
		}
		
		bool IsMostNegativeIntegerWithoutTypeSuffix()
		{
			Token token = la;
			if (token.kind == Tokens.Literal) {
				return token.val == "2147483648" || token.val == "9223372036854775808";
			} else {
				return false;
			}
		}
		
		bool LastExpressionIsUnaryMinus(System.Collections.ArrayList expressions)
		{
			if (expressions.Count == 0) return false;
			UnaryOperatorExpression uoe = expressions[expressions.Count - 1] as UnaryOperatorExpression;
			if (uoe != null) {
				return uoe.Op == UnaryOperatorType.Minus;
			} else {
				return false;
			}
		}
		
		bool StartOfQueryExpression()
		{
			if (la.kind == Tokens.From) {
				Token p = Peek(1);
				if (IsIdentifierToken(p) || Tokens.TypeKW[p.kind])
					return true;
			}
			return false;
		}
		
		static bool IsIdentifierToken(Token tk)
		{
			return Tokens.IdentifierTokens[tk.kind];
		}
		
		/// <summary>
		/// Adds a child item to a collection stored in the parent node.
		/// Also set's the item's parent to <paramref name="parent"/>.
		/// Does nothing if item is null.
		/// </summary>
		static void SafeAdd<T>(INode parent, List<T> list, T item) where T : class, INode
		{
			Debug.Assert(parent != null);
			Debug.Assert((parent is INullable) ? !(parent as INullable).IsNull : true);
			if (item != null) {
				list.Add(item);
				item.Parent = parent;
			}
		}
		
		internal static string GetReflectionNameForOperator(OverloadableOperatorType op)
		{
			switch (op) {
				case OverloadableOperatorType.Add:
					return "op_Addition";
				case OverloadableOperatorType.BitNot:
					return "op_OnesComplement";
				case OverloadableOperatorType.BitwiseAnd:
					return "op_BitwiseAnd";
				case OverloadableOperatorType.BitwiseOr:
					return "op_BitwiseOr";
				case OverloadableOperatorType.Concat:
				case OverloadableOperatorType.CType:
					return "op_unknown";
				case OverloadableOperatorType.Decrement:
					return "op_Decrement";
				case OverloadableOperatorType.Divide:
					return "op_Division";
				case OverloadableOperatorType.DivideInteger:
					return "op_unknown";
				case OverloadableOperatorType.Equality:
					return "op_Equality";
				case OverloadableOperatorType.ExclusiveOr:
					return "op_ExclusiveOr";
				case OverloadableOperatorType.GreaterThan:
					return "op_GreaterThan";
				case OverloadableOperatorType.GreaterThanOrEqual:
					return "op_GreaterThanOrEqual";
				case OverloadableOperatorType.Increment:
					return "op_Increment";
				case OverloadableOperatorType.InEquality:
					return "op_Inequality";
				case OverloadableOperatorType.IsFalse:
					return "op_False";
				case OverloadableOperatorType.IsTrue:
					return "op_True";
				case OverloadableOperatorType.LessThan:
					return "op_LessThan";
				case OverloadableOperatorType.LessThanOrEqual:
					return "op_LessThanOrEqual";
				case OverloadableOperatorType.Like:
					return "op_unknown";
				case OverloadableOperatorType.Modulus:
					return "op_Modulus";
				case OverloadableOperatorType.Multiply:
					return "op_Multiply";
				case OverloadableOperatorType.Not:
					return "op_LogicalNot";
				case OverloadableOperatorType.Power:
					return "op_unknown";
				case OverloadableOperatorType.ShiftLeft:
					return "op_LeftShift";
				case OverloadableOperatorType.ShiftRight:
					return "op_RightShift";
				case OverloadableOperatorType.Subtract:
					return "op_Subtraction";
				case OverloadableOperatorType.UnaryMinus:
					return "op_UnaryNegation";
				case OverloadableOperatorType.UnaryPlus:
					return "op_UnaryPlus";
				default:
					return "op_unknown";
			}
		}
	}
}
