// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// [In|Out] Name [As Contraints]
	/// 
	/// Represents a type parameter.
	/// </summary>
	public class TypeParameterDeclaration : AstNode
	{
		public static readonly Role<AstType> TypeConstraintRole = TypeDeclaration.InheritsTypeRole;
		public static readonly Role<VBTokenNode> VarianceRole = new Role<VBTokenNode>("Variance");
		
		public VarianceModifier Variance { get; set; }
		
		public string Name {
			get { return GetChildByRole (Roles.Identifier).Name; }
			set { SetChildByRole(Roles.Identifier, new Identifier (value, TextLocation.Empty)); }
		}
		
		public AstNodeCollection<AstType> Constraints {
			get { return GetChildrenByRole(TypeConstraintRole); }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTypeParameterDeclaration(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			TypeParameterDeclaration o = other as TypeParameterDeclaration;
			return o != null && this.Variance == o.Variance
				&& MatchString(this.Name, o.Name)
				&& this.Constraints.DoMatch(o.Constraints, match);
		}
	}
}
