// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	sealed class TypePlaceholder : AstType
	{
		public static readonly Role<Pattern> PatternRole = new Role<Pattern>("Pattern");
		
		public TypePlaceholder(Pattern pattern)
		{
			AddChild(pattern, TypePlaceholder.PatternRole);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Pattern; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return default(S);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return GetChildByRole(TypePlaceholder.PatternRole).DoMatch(other, match);
		}
	}
	
	sealed class ExpressionPlaceholder : Expression
	{
		public ExpressionPlaceholder(Pattern pattern)
		{
			AddChild(pattern, TypePlaceholder.PatternRole);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Pattern; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return default(S);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return GetChildByRole(TypePlaceholder.PatternRole).DoMatch(other, match);
		}
	}
	
	sealed class StatementPlaceholder : Statement
	{
		public StatementPlaceholder(Pattern pattern)
		{
			AddChild(pattern, TypePlaceholder.PatternRole);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Pattern; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return default(S);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return GetChildByRole(TypePlaceholder.PatternRole).DoMatch(other, match);
		}
	}
	
	sealed class BlockStatementPlaceholder : BlockStatement
	{
		public BlockStatementPlaceholder(Pattern pattern)
		{
			AddChild(pattern, TypePlaceholder.PatternRole);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Pattern; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return default(S);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return GetChildByRole(TypePlaceholder.PatternRole).DoMatch(other, match);
		}
	}
	
	sealed class VariablePlaceholder : VariableInitializer
	{
		public VariablePlaceholder(Pattern pattern)
		{
			AddChild(pattern, TypePlaceholder.PatternRole);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Pattern; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return default(S);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return GetChildByRole(TypePlaceholder.PatternRole).DoMatch(other, match);
		}
	}
}
