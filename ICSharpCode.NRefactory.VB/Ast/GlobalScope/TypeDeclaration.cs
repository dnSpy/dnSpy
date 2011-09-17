// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public enum ClassType
	{
		Class,
		Struct,
		Interface,
		Module
	}
	
	public class TypeDeclaration : AttributedNode
	{
		public readonly static Role<AttributedNode> MemberRole = new Role<AttributedNode>("Member");
		public readonly static Role<AstType> InheritsTypeRole = new Role<AstType>("InheritsType", AstType.Null);
		public readonly static Role<AstType> ImplementsTypesRole = new Role<AstType>("ImplementsTypes", AstType.Null);

		public AstNodeCollection<AttributedNode> Members {
			get { return base.GetChildrenByRole(MemberRole); }
		}
		
		public ClassType ClassType { get; set; }
		
		public Identifier Name {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public AstNodeCollection<TypeParameterDeclaration> TypeParameters {
			get { return GetChildrenByRole(Roles.TypeParameter); }
		}
		
		public AstType InheritsType {
			get { return GetChildByRole(InheritsTypeRole); }
			set { SetChildByRole(InheritsTypeRole, value); }
		}
		
		public AstNodeCollection<AstType> ImplementsTypes {
			get { return GetChildrenByRole(ImplementsTypesRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			TypeDeclaration t = other as TypeDeclaration;
			return t != null &&
				MatchAttributesAndModifiers(t, match) &&
				Members.DoMatch(t.Members, match) &&
				ClassType == t.ClassType &&
				Name.DoMatch(t.Name, match) &&
				TypeParameters.DoMatch(t.TypeParameters, match) &&
				InheritsType.DoMatch(t.InheritsType, match) &&
				ImplementsTypes.DoMatch(t.ImplementsTypes, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTypeDeclaration(this, data);
		}
	}
}
