// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	/// <summary>
	/// Base class for all patterns.
	/// </summary>
	public abstract class Pattern : AstNode
	{
		public override NodeType NodeType {
			get { return NodeType.Pattern; }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return default(S);
		}
		
		public AstType ToType()
		{
			return new TypePlaceholder(this);
		}
		
		public Expression ToExpression()
		{
			return new ExpressionPlaceholder(this);
		}
		
		public BlockStatement ToBlock()
		{
			return new BlockStatementPlaceholder(this);
		}
		
		public VariableInitializer ToVariable()
		{
			return new VariablePlaceholder(this);
		}
	}
}
