// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class EnumDeclaration : AttributedNode
	{
		public readonly static Role<EnumMemberDeclaration> MemberRole = new Role<EnumMemberDeclaration>("Member");
		public readonly static Role<AstType> UnderlyingTypeRole = new Role<AstType>("UnderlyingType", AstType.Null);

		public Identifier Name {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public AstType UnderlyingType {
			get { return GetChildByRole(UnderlyingTypeRole); }
			set { SetChildByRole(UnderlyingTypeRole, value); }
		}
		
		public AstNodeCollection<EnumMemberDeclaration> Members {
			get { return GetChildrenByRole(MemberRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var decl = other as EnumDeclaration;
			return decl != null &&
				MatchAttributesAndModifiers(decl, match) &&
				Name.DoMatch(decl.Name, match) &&
				UnderlyingType.DoMatch(decl.UnderlyingType, match) &&
				Members.DoMatch(decl.Members, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitEnumDeclaration(this, data);
		}
	}
}
