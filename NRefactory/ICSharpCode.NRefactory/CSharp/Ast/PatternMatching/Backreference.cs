// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	/// <summary>
	/// Matches the last entry in the specified named group.
	/// </summary>
	public class Backreference : Pattern
	{
		readonly string referencedGroupName;
		
		public Backreference(string referencedGroupName)
		{
			if (referencedGroupName == null)
				throw new ArgumentNullException("referencedGroupName");
			this.referencedGroupName = referencedGroupName;
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return match.Get(referencedGroupName).Last().Match(other) != null;
		}
	}
	
	/// <summary>
	/// Matches identifier expressions that have the same identifier as the referenced variable/type definition/method definition.
	/// </summary>
	public class IdentifierExpressionBackreference : Pattern
	{
		readonly string referencedGroupName;
		
		public IdentifierExpressionBackreference(string referencedGroupName)
		{
			if (referencedGroupName == null)
				throw new ArgumentNullException("referencedGroupName");
			this.referencedGroupName = referencedGroupName;
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			IdentifierExpression ident = other as IdentifierExpression;
			if (ident == null || ident.TypeArguments.Any())
				return false;
			AstNode referenced = match.Get(referencedGroupName).Last();
			return ident.Identifier == referenced.GetChildByRole(AstNode.Roles.Identifier).Name;
		}
	}
}
