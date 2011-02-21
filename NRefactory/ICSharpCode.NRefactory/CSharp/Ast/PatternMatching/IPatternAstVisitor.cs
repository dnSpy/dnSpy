// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	/// <summary>
	/// Extended AST visitor that works for patterns.
	/// </summary>
	public interface IPatternAstVisitor<in T, out S> : IAstVisitor<T, S>
	{
		S VisitPlaceholder(AstNode placeholder, AstNode child, T data);
		
		S VisitAnyNode(AnyNode anyNode, T data);
		S VisitBackreference(Backreference backreference, T data);
		S VisitChoice(Choice choice, T data);
		S VisitNamedNode(NamedNode namedNode, T data);
		S VisitRepeat(Repeat repeat, T data);
		S VisitIdentifierExpressionBackreference(IdentifierExpressionBackreference identifierExpressionBackreference, T data);
	}
}
