// 
// MethodDeclaration.cs
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
	public class MethodDeclaration : EntityDeclaration
	{
		public override SymbolKind SymbolKind {
			get { return SymbolKind.Method; }
		}
		
		/// <summary>
		/// Gets/Sets the type reference of the interface that is explicitly implemented.
		/// Null node if this member is not an explicit interface implementation.
		/// </summary>
		public AstType PrivateImplementationType {
			get { return GetChildByRole (PrivateImplementationTypeRole); }
			set { SetChildByRole (PrivateImplementationTypeRole, value); }
		}
		
		public AstNodeCollection<TypeParameterDeclaration> TypeParameters {
			get { return GetChildrenByRole (Roles.TypeParameter); }
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
		
		public AstNodeCollection<Constraint> Constraints {
			get { return GetChildrenByRole (Roles.Constraint); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole (Roles.Body); }
			set { SetChildByRole (Roles.Body, value); }
		}
		
		public bool IsExtensionMethod {
			get {
				ParameterDeclaration pd = (ParameterDeclaration)GetChildByRole (Roles.Parameter);
				return pd != null && pd.ParameterModifier == ParameterModifier.This;
			}
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitMethodDeclaration (this);
		}
		
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitMethodDeclaration (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitMethodDeclaration (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			MethodDeclaration o = other as MethodDeclaration;
			return o != null && MatchString(this.Name, o.Name)
				&& this.MatchAttributesAndModifiers(o, match) && this.ReturnType.DoMatch(o.ReturnType, match)
				&& this.PrivateImplementationType.DoMatch(o.PrivateImplementationType, match)
				&& this.TypeParameters.DoMatch(o.TypeParameters, match)
				&& this.Parameters.DoMatch(o.Parameters, match) && this.Constraints.DoMatch(o.Constraints, match)
				&& this.Body.DoMatch(o.Body, match);
		}
	}
}
