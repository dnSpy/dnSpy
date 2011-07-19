// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class DelegateDeclaration : AttributedNode
	{
		public bool IsSub { get; set; }
		
		public AstNodeCollection<TypeParameterDeclaration> TypeParameters {
			get { return GetChildrenByRole(Roles.TypeParameter); }
		}
		
		public Identifier Name {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public AstNodeCollection<ParameterDeclaration> Parameters {
			get { return GetChildrenByRole(Roles.Parameter); }
		}
		
		public AstNodeCollection<AttributeBlock> ReturnTypeAttributes {
			get { return GetChildrenByRole(AttributeBlock.ReturnTypeAttributeBlockRole); }
		}
		
		public AstType ReturnType {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var o = other as DelegateDeclaration;
			return o != null &&
				MatchAttributesAndModifiers(o, match) &&
				IsSub == o.IsSub &&
				TypeParameters.DoMatch(o.TypeParameters, match) &&
				Name.DoMatch(o.Name, match) &&
				Parameters.DoMatch(o.Parameters, match) &&
				ReturnTypeAttributes.DoMatch(o.ReturnTypeAttributes, match) &&
				ReturnType.DoMatch(o.ReturnType, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitDelegateDeclaration(this, data);
		}
	}
}
