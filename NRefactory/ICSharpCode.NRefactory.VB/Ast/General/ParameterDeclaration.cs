// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class ParameterDeclaration : AttributedNode
	{
		public Identifier Name {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public Expression OptionalValue {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public AstType Type {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var param = other as ParameterDeclaration;
			return param != null &&
				MatchAttributesAndModifiers(param, match) &&
				Name.DoMatch(param.Name, match) &&
				OptionalValue.DoMatch(param.OptionalValue, match) &&
				Type.DoMatch(param.Type, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitParameterDeclaration(this, data);
		}
	}
}
