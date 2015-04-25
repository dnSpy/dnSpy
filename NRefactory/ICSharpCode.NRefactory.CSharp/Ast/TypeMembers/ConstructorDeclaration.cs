// 
// ConstructorDeclaration.cs
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

using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	public class ConstructorDeclaration : EntityDeclaration
	{
		public static readonly Role<ConstructorInitializer> InitializerRole = new Role<ConstructorInitializer>("Initializer", ConstructorInitializer.Null);
		
		public override SymbolKind SymbolKind {
			get { return SymbolKind.Constructor; }
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public AstNodeCollection<ParameterDeclaration> Parameters {
			get { return GetChildrenByRole (Roles.Parameter); }
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}
		
		public CSharpTokenNode ColonToken {
			get { return GetChildByRole (Roles.Colon); }
		}
		
		public ConstructorInitializer Initializer {
			get { return GetChildByRole (InitializerRole); }
			set { SetChildByRole( InitializerRole, value); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole (Roles.Body); }
			set { SetChildByRole (Roles.Body, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitConstructorDeclaration (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitConstructorDeclaration (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitConstructorDeclaration (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ConstructorDeclaration o = other as ConstructorDeclaration;
			return o != null && this.MatchAttributesAndModifiers(o, match) && this.Parameters.DoMatch(o.Parameters, match)
				&& this.Initializer.DoMatch(o.Initializer, match) && this.Body.DoMatch(o.Body, match);
		}
	}
	
	public enum ConstructorInitializerType {
		Any,
		Base,
		This
	}
	
	public class ConstructorInitializer : AstNode
	{
		public static readonly TokenRole BaseKeywordRole = new TokenRole ("base");
		public static readonly TokenRole ThisKeywordRole = new TokenRole ("this");
		
		public static readonly new ConstructorInitializer Null = new NullConstructorInitializer ();
		class NullConstructorInitializer : ConstructorInitializer
		{
			public override NodeType NodeType {
				get {
					return NodeType.Unknown;
				}
			}
			
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override void AcceptVisitor (IAstVisitor visitor)
			{
				visitor.VisitNullNode(this);
			}
			
			public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
			{
				return visitor.VisitNullNode(this);
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitNullNode(this, data);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public ConstructorInitializerType ConstructorInitializerType {
			get;
			set;
		}
		
		public CSharpTokenNode Keyword {
			get {
				if (ConstructorInitializerType == ConstructorInitializerType.Base)
					return GetChildByRole(BaseKeywordRole);
				else
					return GetChildByRole(ThisKeywordRole);
			}
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public AstNodeCollection<Expression> Arguments {
			get { return GetChildrenByRole (Roles.Argument); }
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitConstructorInitializer (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitConstructorInitializer (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitConstructorInitializer (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ConstructorInitializer o = other as ConstructorInitializer;
			return o != null && !o.IsNull 
				&& (this.ConstructorInitializerType == ConstructorInitializerType.Any || this.ConstructorInitializerType == o.ConstructorInitializerType)
				&& this.Arguments.DoMatch(o.Arguments, match);
		}
	}
}
