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
	/// <summary>
	/// class Name&lt;TypeParameters&gt; : BaseTypes where Constraints;
	/// </summary>
	public class TypeDeclaration : AttributedNode
	{
		public readonly static Role<CSharpTokenNode> ColonRole = Roles.Colon;
		public readonly static Role<AstType> BaseTypeRole = new Role<AstType>("BaseType", AstType.Null);
		public readonly static Role<AttributedNode> MemberRole = new Role<AttributedNode>("Member");
		
		public override NodeType NodeType {
			get {
				return NodeType.TypeDeclaration;
			}
		}
		
		public ClassType ClassType {
			get;
			set;
		}
		
		public string Name {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole (Roles.Identifier, new Identifier(value, AstLocation.Empty));
			}
		}
		
		public IEnumerable<TypeParameterDeclaration> TypeParameters {
			get { return GetChildrenByRole (Roles.TypeParameter); }
			set { SetChildrenByRole (Roles.TypeParameter, value); }
		}
		
		public IEnumerable<AstType> BaseTypes {
			get { return GetChildrenByRole (BaseTypeRole); }
			set { SetChildrenByRole (BaseTypeRole, value); }
		}
		
		public IEnumerable<Constraint> Constraints {
			get { return GetChildrenByRole (Roles.Constraint); }
			set { SetChildrenByRole (Roles.Constraint, value); }
		}
		
		public CSharpTokenNode LBraceToken {
			get { return GetChildByRole (Roles.LBrace); }
		}
		
		public IEnumerable<AttributedNode> Members {
			get { return GetChildrenByRole (MemberRole); }
			set { SetChildrenByRole (MemberRole, value); }
		}
		
		public CSharpTokenNode RBraceToken {
			get { return GetChildByRole (Roles.RBrace); }
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTypeDeclaration (this, data);
		}
	}
}
