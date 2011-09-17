// 
// DestructorDeclaration.cs
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

namespace ICSharpCode.NRefactory.CSharp
{
	public class DestructorDeclaration : AttributedNode
	{
		public static readonly Role<CSharpTokenNode> TildeRole = new Role<CSharpTokenNode>("Tilde", CSharpTokenNode.Null);
		
		public CSharpTokenNode TildeToken {
			get { return GetChildByRole (TildeRole); }
		}
		
		/// <summary>
		/// Gets/Sets the name of the class containing the destructor.
		/// This property can be used to inform the output visitor about the class name when writing a destructor declaration
		/// without writing the complete type declaration. It is ignored when the destructor has a type declaration as parent.
		/// </summary>
		public string Name { get; set; }
		
		public Identifier IdentifierToken {
			get { return GetChildByRole (Roles.Identifier); }
			set { SetChildByRole (Roles.Identifier, value); }
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}
		public BlockStatement Body {
			get { return GetChildByRole (Roles.Body); }
			set { SetChildByRole (Roles.Body, value); }
		}
		
		public override NodeType NodeType {
			get { return NodeType.Member; }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data = default(T))
		{
			return visitor.VisitDestructorDeclaration (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			DestructorDeclaration o = other as DestructorDeclaration;
			return o != null && this.MatchAttributesAndModifiers(o, match) && this.Body.DoMatch(o.Body, match);
		}
	}
}
