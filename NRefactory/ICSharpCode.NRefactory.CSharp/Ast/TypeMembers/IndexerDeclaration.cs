// 
// IndexerDeclaration.cs
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

using System;
using System.ComponentModel;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	public class IndexerDeclaration : EntityDeclaration
	{
		public static readonly TokenRole ThisKeywordRole = new TokenRole ("this");
		public static readonly Role<Accessor> GetterRole = PropertyDeclaration.GetterRole;
		public static readonly Role<Accessor> SetterRole = PropertyDeclaration.SetterRole;
		
		public override SymbolKind SymbolKind {
			get { return SymbolKind.Indexer; }
		}
		
		/// <summary>
		/// Gets/Sets the type reference of the interface that is explicitly implemented.
		/// Null node if this member is not an explicit interface implementation.
		/// </summary>
		public AstType PrivateImplementationType {
			get { return GetChildByRole (PrivateImplementationTypeRole); }
			set { SetChildByRole (PrivateImplementationTypeRole, value); }
		}
		
		public override string Name {
			get { return "Item"; }
			set { throw new NotSupportedException(); }
		}
		
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override Identifier NameToken {
			get { return Identifier.Null; }
			set { throw new NotSupportedException(); }
		}

		public CSharpTokenNode LBracketToken {
			get { return GetChildByRole (Roles.LBracket); }
		}

		public CSharpTokenNode ThisToken {
			get { return GetChildByRole (ThisKeywordRole); }
		}
		
		public AstNodeCollection<ParameterDeclaration> Parameters {
			get { return GetChildrenByRole (Roles.Parameter); }
		}
		
		public CSharpTokenNode RBracketToken {
			get { return GetChildByRole (Roles.RBracket); }
		}
		
		public CSharpTokenNode LBraceToken {
			get { return GetChildByRole (Roles.LBrace); }
		}
		
		public Accessor Getter {
			get { return GetChildByRole(GetterRole); }
			set { SetChildByRole(GetterRole, value); }
		}
		
		public Accessor Setter {
			get { return GetChildByRole(SetterRole); }
			set { SetChildByRole(SetterRole, value); }
		}
		
		public CSharpTokenNode RBraceToken {
			get { return GetChildByRole (Roles.RBrace); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitIndexerDeclaration (this);
		}
		
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitIndexerDeclaration (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitIndexerDeclaration (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			IndexerDeclaration o = other as IndexerDeclaration;
			return o != null
				&& this.MatchAttributesAndModifiers(o, match) && this.ReturnType.DoMatch(o.ReturnType, match)
				&& this.PrivateImplementationType.DoMatch(o.PrivateImplementationType, match)
				&& this.Parameters.DoMatch(o.Parameters, match)
				&& this.Getter.DoMatch(o.Getter, match) && this.Setter.DoMatch(o.Setter, match);
		}
	}
}
