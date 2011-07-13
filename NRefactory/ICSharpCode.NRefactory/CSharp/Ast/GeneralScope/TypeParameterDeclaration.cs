// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// [in|out] Name
	/// 
	/// Represents a type parameter.
	/// Note: mirroring the C# syntax, constraints are not part of the type parameter declaration, but belong
	/// to the parent type or method.
	/// </summary>
	public class TypeParameterDeclaration : AstNode
	{
		public static readonly Role<AttributeSection> AttributeRole = AttributedNode.AttributeRole;
		public static readonly Role<CSharpTokenNode> VarianceRole = new Role<CSharpTokenNode>("Variance");
		
		public override NodeType NodeType {
			get { return NodeType.Unknown; }
		}
		
		public AstNodeCollection<AttributeSection> Attributes {
			get { return GetChildrenByRole (AttributeRole); }
		}
		
		public VarianceModifier Variance {
			get; set;
		}
		
		public string Name {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole(Roles.Identifier, Identifier.Create (value, AstLocation.Empty));
			}
		}
		
		public Identifier NameToken {
			get {
				return GetChildByRole (Roles.Identifier);
			}
			set {
				SetChildByRole (Roles.Identifier, value);
			}
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTypeParameterDeclaration(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			TypeParameterDeclaration o = other as TypeParameterDeclaration;
			return o != null && this.Variance == o.Variance && MatchString(this.Name, o.Name) && this.Attributes.DoMatch(o.Attributes, match);
		}
	}
}
