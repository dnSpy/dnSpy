// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class PropertyDeclaration : MemberDeclaration
	{
		// TODO : support automatic properties
		
		public static readonly Role<Accessor> GetterRole = new Role<Accessor>("Getter", Accessor.Null);
		public static readonly Role<Accessor> SetterRole = new Role<Accessor>("Setter", Accessor.Null);
		
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
		
		public AstNodeCollection<InterfaceMemberSpecifier> ImplementsClause {
			get { return GetChildrenByRole(InterfaceMemberSpecifier.InterfaceMemberSpecifierRole); }
		}
		
		public Accessor Getter {
			get { return GetChildByRole(GetterRole); }
			set { SetChildByRole(GetterRole, value); }
		}
		
		public Accessor Setter {
			get { return GetChildByRole(SetterRole); }
			set { SetChildByRole(SetterRole, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitPropertyDeclaration(this, data);
		}
	}
}
