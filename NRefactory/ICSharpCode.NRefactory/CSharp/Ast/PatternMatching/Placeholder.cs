// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	// Placeholders do not store their child in the AST tree; but keep it as a separate child.
	// This allows reusing the child in multiple placeholders; thus enabling the sharing of AST subtrees.
	sealed class TypePlaceholder : AstType
	{
		readonly AstNode child;
		
		public TypePlaceholder(AstNode child)
		{
			this.child = child;
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, child, data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return child.DoMatch(other, match);
		}
		
		internal override bool DoMatchCollection(Role role, AstNode pos, Match match, Stack<Pattern.PossibleMatch> backtrackingStack)
		{
			return child.DoMatchCollection(role, pos, match, backtrackingStack);
		}
	}
	
	sealed class ExpressionPlaceholder : Expression
	{
		readonly AstNode child;
		
		public ExpressionPlaceholder(AstNode child)
		{
			this.child = child;
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, child, data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return child.DoMatch(other, match);
		}
		
		internal override bool DoMatchCollection(Role role, AstNode pos, Match match, Stack<Pattern.PossibleMatch> backtrackingStack)
		{
			return child.DoMatchCollection(role, pos, match, backtrackingStack);
		}
	}
	
	sealed class StatementPlaceholder : Statement
	{
		readonly AstNode child;
		
		public StatementPlaceholder(AstNode child)
		{
			this.child = child;
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, child, data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return child.DoMatch(other, match);
		}
		
		internal override bool DoMatchCollection(Role role, AstNode pos, Match match, Stack<Pattern.PossibleMatch> backtrackingStack)
		{
			return child.DoMatchCollection(role, pos, match, backtrackingStack);
		}
	}
	
	sealed class BlockStatementPlaceholder : BlockStatement
	{
		readonly AstNode child;
		
		public BlockStatementPlaceholder(AstNode child)
		{
			this.child = child;
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, child, data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return child.DoMatch(other, match);
		}
		
		internal override bool DoMatchCollection(Role role, AstNode pos, Match match, Stack<Pattern.PossibleMatch> backtrackingStack)
		{
			return child.DoMatchCollection(role, pos, match, backtrackingStack);
		}
	}
	
	sealed class VariablePlaceholder : VariableInitializer
	{
		readonly AstNode child;
		
		public VariablePlaceholder(AstNode child)
		{
			this.child = child;
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, child, data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return child.DoMatch(other, match);
		}
		
		internal override bool DoMatchCollection(Role role, AstNode pos, Match match, Stack<Pattern.PossibleMatch> backtrackingStack)
		{
			return child.DoMatchCollection(role, pos, match, backtrackingStack);
		}
	}
	
	sealed class AttributeSectionPlaceholder : AttributeSection
	{
		readonly AstNode child;
		
		public AttributeSectionPlaceholder(AstNode child)
		{
			this.child = child;
		}
		
		public override NodeType NodeType {
			get { return NodeType.Placeholder; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitPlaceholder(this, child, data);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return child.DoMatch(other, match);
		}
		
		internal override bool DoMatchCollection(Role role, AstNode pos, Match match, Stack<Pattern.PossibleMatch> backtrackingStack)
		{
			return child.DoMatchCollection(role, pos, match, backtrackingStack);
		}
	}
}
