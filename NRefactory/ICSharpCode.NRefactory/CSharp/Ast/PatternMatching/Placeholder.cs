// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	sealed class TypePlaceholder : AstType
	{
		public static readonly Role<AstNode> ChildRole = new Role<AstNode>("Child", AstNode.Null);
		
		public TypePlaceholder(AstNode child)
		{
			AddChild(child, TypePlaceholder.ChildRole);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, GetChildByRole(TypePlaceholder.ChildRole), data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return GetChildByRole(TypePlaceholder.ChildRole).DoMatch(other, match);
		}
	}
	
	sealed class ExpressionPlaceholder : Expression
	{
		public ExpressionPlaceholder(AstNode child)
		{
			AddChild(child, TypePlaceholder.ChildRole);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, GetChildByRole(TypePlaceholder.ChildRole), data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return GetChildByRole(TypePlaceholder.ChildRole).DoMatch(other, match);
		}
	}
	
	sealed class StatementPlaceholder : Statement
	{
		public StatementPlaceholder(AstNode child)
		{
			AddChild(child, TypePlaceholder.ChildRole);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, GetChildByRole(TypePlaceholder.ChildRole), data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return GetChildByRole(TypePlaceholder.ChildRole).DoMatch(other, match);
		}
	}
	
	sealed class BlockStatementPlaceholder : BlockStatement
	{
		public BlockStatementPlaceholder(AstNode child)
		{
			AddChild(child, TypePlaceholder.ChildRole);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, GetChildByRole(TypePlaceholder.ChildRole), data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return GetChildByRole(TypePlaceholder.ChildRole).DoMatch(other, match);
		}
	}
	
	sealed class VariablePlaceholder : VariableInitializer
	{
		public VariablePlaceholder(AstNode child)
		{
			AddChild(child, TypePlaceholder.ChildRole);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, GetChildByRole(TypePlaceholder.ChildRole), data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return GetChildByRole(TypePlaceholder.ChildRole).DoMatch(other, match);
		}
	}
}
