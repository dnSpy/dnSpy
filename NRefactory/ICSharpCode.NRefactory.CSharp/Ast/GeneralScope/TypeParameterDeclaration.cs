// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
		public static readonly Role<CSharpTokenNode> VarianceRole = new Role<CSharpTokenNode>("Variance", CSharpTokenNode.Null);
		
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
				SetChildByRole(Roles.Identifier, Identifier.Create (value, TextLocation.Empty));
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
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data = default(T))
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
