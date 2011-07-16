// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public abstract class MemberDeclaration : AttributedNode
	{
		
	}
	
	/// <summary>
	/// Description of ConstructorDeclaration.
	/// </summary>
	public class ConstructorDeclaration : MemberDeclaration
	{
		public ConstructorDeclaration()
		{
		}
		
		public AstNodeCollection<ParameterDeclaration> Parameters {
			get { return GetChildrenByRole(Roles.Parameter); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole(Roles.Body); }
			set { SetChildByRole(Roles.Body, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var ctor = other as ConstructorDeclaration;
			return ctor != null &&
				MatchAttributesAndModifiers(ctor, match) &&
				Parameters.DoMatch(ctor.Parameters, match) &&
				Body.DoMatch(ctor.Body, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitConstructorDeclaration(this, data);
		}
	}
}
