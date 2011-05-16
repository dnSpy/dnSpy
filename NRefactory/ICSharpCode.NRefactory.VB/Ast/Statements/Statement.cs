// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Base class for statements.
	/// </summary>
	/// <remarks>
	/// This class is useful even though it doesn't provide any additional functionality:
	/// It can be used to communicate more information in APIs, e.g. "this subnode will always be a statement"
	/// </remarks>
	public abstract class Statement : AstNode
	{
		#region Null
		public new static readonly Statement Null = new NullStatement ();
		
		sealed class NullStatement : Statement
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
		
		#region PatternPlaceholder
		public static implicit operator Statement(PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder(pattern) : null;
		}
		
		sealed class PatternPlaceholder : Statement, PatternMatching.INode
		{
			readonly PatternMatching.Pattern child;
			
			public PatternPlaceholder(PatternMatching.Pattern child)
			{
				this.child = child;
			}
			
			public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitPatternPlaceholder(this, child, data);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return child.DoMatch(other, match);
			}
			
			bool PatternMatching.INode.DoMatchCollection(Role role, PatternMatching.INode pos, PatternMatching.Match match, PatternMatching.BacktrackingInfo backtrackingInfo)
			{
				return child.DoMatchCollection(role, pos, match, backtrackingInfo);
			}
		}
		#endregion
		
		/// <summary>
		/// Gets the previous statement within the current block.
		/// This is usually equivalent to <see cref="PrevSibling"/>, but will skip any non-statements (e.g. comments)
		/// </summary>
		public Statement PreviousStatement {
			get {
				AstNode node = this;
				while ((node = node.PrevSibling) != null) {
					Statement stmt = node as Statement;
					if (stmt != null)
						return stmt;
				}
				return null;
			}
		}
		
		/// <summary>
		/// Gets the next statement within the current block.
		/// This is usually equivalent to <see cref="NextSibling"/>, but will skip any non-statements (e.g. comments)
		/// </summary>
		public Statement NextStatement {
			get {
				AstNode node = this;
				while ((node = node.NextSibling) != null) {
					Statement stmt = node as Statement;
					if (stmt != null)
						return stmt;
				}
				return null;
			}
		}
		
		public new Statement Clone()
		{
			return (Statement)base.Clone();
		}
		
		public Statement ReplaceWith(Func<Statement, Statement> replaceFunction)
		{
			if (replaceFunction == null)
				throw new ArgumentNullException("replaceFunction");
			return (Statement)base.ReplaceWith(node => replaceFunction((Statement)node));
		}
		
		// Make debugging easier by giving Statements a ToString() implementation
		public override string ToString()
		{
//			if (IsNull)
//				return "Null";
//			StringWriter w = new StringWriter();
//			AcceptVisitor(new OutputVisitor(w, new CSharpFormattingOptions()), null);
//			string text = w.ToString().TrimEnd().Replace("\t", "").Replace(w.NewLine, " ");
//			if (text.Length > 100)
//				return text.Substring(0, 97) + "...";
//			else
//				return text;
			throw new NotImplementedException();
		}
	}
	
	/// <summary>
	/// Label:
	/// </summary>
	public class LabelDeclarationStatement : Statement
	{
		public Identifier Label {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public VBTokenNode Colon {
			get { return GetChildByRole(Roles.Colon); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitLabelDeclarationStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			LabelDeclarationStatement o = other as LabelDeclarationStatement;
			return o != null && MatchString(this.Label.Name, o.Label.Name);
		}
	}
	
	/// <summary>
	/// ( Dim | Static | Const ) VariableDeclarator { , VariableDeclarator }
	/// </summary>
	public class LocalDeclarationStatement : Statement
	{
		public AstNodeCollection<VariableDeclarator> Variables {
			get { return GetChildrenByRole(VariableDeclarator.VariableDeclaratorRole); }
		}
		
		public Modifiers Modifiers {
			get { return AttributedNode.GetModifiers(this); }
			set { AttributedNode.SetModifiers(this, value); }
		}
		
		public VBModifierToken ModifierToken {
			get { return GetChildByRole(AttributedNode.ModifierRole); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitLocalDeclarationStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
	}
	
	/// <summary>
	/// With Expression <br />
	/// 	Block <br />
	/// End With
	/// </summary>
	public class WithStatement : Statement
	{
		public Expression Expression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole(Roles.Body); }
			set { SetChildByRole(Roles.Body, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitWithStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
	}
	
	/// <summary>
	/// SyncLock Expression <br />
	/// 	Block <br />
	/// End SyncLock
	/// </summary>
	public class SyncLockStatement : Statement
	{
		public Expression Expression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole(Roles.Body); }
			set { SetChildByRole(Roles.Body, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSyncLockStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
	}
	
	/// <summary>
	/// SyncLock Expression <br />
	/// 	Block <br />
	/// End SyncLock
	/// </summary>
	public class TryStatement : Statement
	{
		public static readonly Role<BlockStatement> FinallyBlockRole = new Role<BlockStatement>("FinallyBlock", Ast.BlockStatement.Null);
		
		public BlockStatement Body {
			get { return GetChildByRole(Roles.Body); }
			set { SetChildByRole(Roles.Body, value); }
		}
		
		public AstNodeCollection<CatchBlock> CatchBlocks {
			get { return GetChildrenByRole(CatchBlock.CatchBlockRole); }
		}
		
		public BlockStatement FinallyBlock {
			get { return GetChildByRole(FinallyBlockRole); }
			set { SetChildByRole(FinallyBlockRole, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTryStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
	}
	
	public class CatchBlock : BlockStatement
	{
		public static readonly Role<CatchBlock> CatchBlockRole = new Role<CatchBlock>("CatchBlockRole");
		
		public Identifier ExceptionVariable {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public AstType ExceptionType {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		public Expression WhenExpression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCatchBlock(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
	}
	
	public class IfElseStatement : Statement
	{
		public static readonly Role<Statement> FalseStatementRole = new Role<Statement>("False", Ast.Statement.Null);
		public static readonly Role<Statement> TrueStatementRole = new Role<Statement>("True", Ast.Statement.Null);
		
		public Expression Condition {
			get { return GetChildByRole(Roles.Condition); }
			set { SetChildByRole(Roles.Condition, value); }
		}
		
		public Statement Body {
			get { return GetChildByRole(TrueStatementRole); }
			set { SetChildByRole(TrueStatementRole, value); }
		}
		
		public Statement ElseBlock {
			get { return GetChildByRole(FalseStatementRole); }
			set { SetChildByRole(FalseStatementRole, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitIfElseStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
	}
	
	/// <summary>
	/// Expression
	/// </summary>
	public class ExpressionStatement : Statement
	{
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitExpressionStatement(this, data);
		}
		
		public ExpressionStatement()
		{
		}
		
		public ExpressionStatement(Expression expression)
		{
			this.Expression = expression;
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ExpressionStatement o = other as ExpressionStatement;
			return o != null && this.Expression.DoMatch(o.Expression, match);
		}
	}
	
	/// <summary>
	/// Throw Expression
	/// </summary>
	public class ThrowStatement : Statement
	{
		public VBTokenNode ThrowToken {
			get { return GetChildByRole (Roles.Keyword); }
		}
		
		public Expression Expression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public ThrowStatement()
		{
		}
		
		public ThrowStatement(Expression expression)
		{
			AddChild (expression, Roles.Expression);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitThrowStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ThrowStatement o = other as ThrowStatement;
			return o != null && this.Expression.DoMatch(o.Expression, match);
		}
	}
	
	/// <summary>
	/// Return Expression
	/// </summary>
	public class ReturnStatement : Statement
	{
		public VBTokenNode ReturnToken {
			get { return GetChildByRole (Roles.Keyword); }
		}
		
		public Expression Expression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public ReturnStatement()
		{
		}
		
		public ReturnStatement(Expression expression)
		{
			AddChild (expression, Roles.Expression);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitReturnStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ReturnStatement o = other as ReturnStatement;
			return o != null && this.Expression.DoMatch(o.Expression, match);
		}
	}

}
