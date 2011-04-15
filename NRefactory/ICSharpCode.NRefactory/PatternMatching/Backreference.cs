// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;

namespace ICSharpCode.NRefactory.PatternMatching
{
	/// <summary>
	/// Matches the last entry in the specified named group.
	/// </summary>
	public class Backreference : Pattern
	{
		readonly string referencedGroupName;
		
		public string ReferencedGroupName {
			get { return referencedGroupName; }
		}
		
		public Backreference(string referencedGroupName)
		{
			if (referencedGroupName == null)
				throw new ArgumentNullException("referencedGroupName");
			this.referencedGroupName = referencedGroupName;
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			return match.Get(referencedGroupName).Last().IsMatch(other);
		}
		
		public override S AcceptVisitor<T, S>(IPatternAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitBackreference(this, data);
		}
	}
	
	/// <summary>
	/// Matches identifier expressions that have the same identifier as the referenced variable/type definition/method definition.
	/// </summary>
	public class IdentifierExpressionBackreference : Pattern
	{
		readonly string referencedGroupName;
		
		public string ReferencedGroupName {
			get { return referencedGroupName; }
		}
		
		public IdentifierExpressionBackreference(string referencedGroupName)
		{
			if (referencedGroupName == null)
				throw new ArgumentNullException("referencedGroupName");
			this.referencedGroupName = referencedGroupName;
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			CSharp.IdentifierExpression ident = other as CSharp.IdentifierExpression;
			if (ident == null || ident.TypeArguments.Any())
				return false;
			CSharp.AstNode referenced = (CSharp.AstNode)match.Get(referencedGroupName).Last();
			if (referenced == null)
				return false;
			return ident.Identifier == referenced.GetChildByRole(CSharp.AstNode.Roles.Identifier).Name;
		}
		
		public override S AcceptVisitor<T, S>(IPatternAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitIdentifierExpressionBackreference(this, data);
		}
	}
}
