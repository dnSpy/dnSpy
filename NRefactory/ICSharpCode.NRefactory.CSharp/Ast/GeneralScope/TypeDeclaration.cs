// 
// TypeDeclaration.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	public enum ClassType
	{
		Class,
		Struct,
		Interface,
		Enum
	}
	
	/// <summary>
	/// class Name&lt;TypeParameters&gt; : BaseTypes where Constraints;
	/// </summary>
	public class TypeDeclaration : EntityDeclaration
	{
		public override NodeType NodeType {
			get { return NodeType.TypeDeclaration; }
		}
		
		public override SymbolKind SymbolKind {
			get { return SymbolKind.TypeDefinition; }
		}
		
		ClassType classType;

		public CSharpTokenNode TypeKeyword {
			get {
				switch (classType) {
					case ClassType.Class:
						return GetChildByRole(Roles.ClassKeyword);
					case ClassType.Struct:
						return GetChildByRole(Roles.StructKeyword);
					case ClassType.Interface:
						return GetChildByRole(Roles.InterfaceKeyword);
					case ClassType.Enum:
						return GetChildByRole(Roles.EnumKeyword);
					default:
						return CSharpTokenNode.Null;
				}
			}
		}
		
		public ClassType ClassType {
			get { return classType; }
			set {
				ThrowIfFrozen();
				classType = value;
			}
		}

		public CSharpTokenNode LChevronToken {
			get { return GetChildByRole (Roles.LChevron); }
		}

		public AstNodeCollection<TypeParameterDeclaration> TypeParameters {
			get { return GetChildrenByRole (Roles.TypeParameter); }
		}

		public CSharpTokenNode RChevronToken {
			get { return GetChildByRole (Roles.RChevron); }
		}



		public CSharpTokenNode ColonToken {
			get {
				return GetChildByRole(Roles.Colon);
			}
		}
		
		public AstNodeCollection<AstType> BaseTypes {
			get { return GetChildrenByRole(Roles.BaseType); }
		}
		
		public AstNodeCollection<Constraint> Constraints {
			get { return GetChildrenByRole(Roles.Constraint); }
		}
		
		public CSharpTokenNode LBraceToken {
			get { return GetChildByRole (Roles.LBrace); }
		}

		public AstNodeCollection<EntityDeclaration> Members {
			get { return GetChildrenByRole (Roles.TypeMemberRole); }
		}
		
		public CSharpTokenNode RBraceToken {
			get { return GetChildByRole (Roles.RBrace); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitTypeDeclaration (this);
		}
		
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitTypeDeclaration (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTypeDeclaration (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			TypeDeclaration o = other as TypeDeclaration;
			return o != null && this.ClassType == o.ClassType && MatchString(this.Name, o.Name)
				&& this.MatchAttributesAndModifiers(o, match) && this.TypeParameters.DoMatch(o.TypeParameters, match)
				&& this.BaseTypes.DoMatch(o.BaseTypes, match) && this.Constraints.DoMatch(o.Constraints, match)
				&& this.Members.DoMatch(o.Members, match);
		}
	}
}
