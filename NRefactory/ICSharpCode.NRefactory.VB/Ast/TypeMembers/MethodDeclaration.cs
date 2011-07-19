// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class MethodDeclaration : MemberDeclaration
	{
		public MethodDeclaration()
		{
		}
		
		public bool IsSub { get; set; }
		
		public Identifier Name {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public AstNodeCollection<TypeParameterDeclaration> TypeParameters {
			get { return GetChildrenByRole(Roles.TypeParameter); }
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
		
		public AstNodeCollection<EventMemberSpecifier> HandlesClause {
			get { return GetChildrenByRole(EventMemberSpecifier.EventMemberSpecifierRole); }
		}
		
		public AstNodeCollection<InterfaceMemberSpecifier> ImplementsClause {
			get { return GetChildrenByRole(InterfaceMemberSpecifier.InterfaceMemberSpecifierRole); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole(Roles.Body); }
			set { SetChildByRole(Roles.Body, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var method = other as MethodDeclaration;
			return method != null &&
				MatchAttributesAndModifiers(method, match) &&
				IsSub == method.IsSub &&
				Name.DoMatch(method.Name, match) &&
				TypeParameters.DoMatch(method.TypeParameters, match) &&
				Parameters.DoMatch(method.Parameters, match) &&
				ReturnTypeAttributes.DoMatch(method.ReturnTypeAttributes, match) &&
				ReturnType.DoMatch(method.ReturnType, match) &&
				HandlesClause.DoMatch(method.HandlesClause, match) &&
				ImplementsClause.DoMatch(method.ImplementsClause, match) &&
				Body.DoMatch(method.Body, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitMethodDeclaration(this, data);
		}
	}
}
