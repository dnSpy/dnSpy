// 
// ParameterDeclarationExpression.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	public enum ParameterModifier {
		None,
		Ref,
		Out,
		Params,
		This
	}
	
	public class ParameterDeclaration : AstNode
	{
		public static readonly Role<AttributeSection> AttributeRole = EntityDeclaration.AttributeRole;
		public static readonly TokenRole RefModifierRole = new TokenRole("ref");
		public static readonly TokenRole OutModifierRole = new TokenRole("out");
		public static readonly TokenRole ParamsModifierRole = new TokenRole("params");
		public static readonly TokenRole ThisModifierRole = new TokenRole("this");
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public AstNodeCollection<AttributeSection> Attributes {
			get { return GetChildrenByRole (AttributeRole); }
		}
		
		ParameterModifier parameterModifier;
		
		public ParameterModifier ParameterModifier {
			get { return parameterModifier; }
			set {
				ThrowIfFrozen();
				parameterModifier = value;
			}
		}
		
		public AstType Type {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole (Roles.Type, value); }
		}
		
		public string Name {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole (Roles.Identifier, Identifier.Create (value));
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

		public CSharpTokenNode AssignToken {
			get { return GetChildByRole (Roles.Assign); }
		}

		public Expression DefaultExpression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitParameterDeclaration (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitParameterDeclaration (this);
		}

		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitParameterDeclaration (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ParameterDeclaration o = other as ParameterDeclaration;
			return o != null && this.Attributes.DoMatch(o.Attributes, match) && this.ParameterModifier == o.ParameterModifier
				&& this.Type.DoMatch(o.Type, match) && MatchString(this.Name, o.Name)
				&& this.DefaultExpression.DoMatch(o.DefaultExpression, match);
		}
		
		public ParameterDeclaration()
		{
		}
		
		public ParameterDeclaration(AstType type, string name, ParameterModifier modifier = ParameterModifier.None)
		{
			Type = type;
			Name = name;
			ParameterModifier = modifier;
		}

		public ParameterDeclaration(string name, ParameterModifier modifier = ParameterModifier.None)
		{
			Name = name;
			ParameterModifier = modifier;
		}

		public new ParameterDeclaration Clone()
		{
			return (ParameterDeclaration)base.Clone();
		}
	}
}

