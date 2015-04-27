// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// new Type[Dimensions]
	/// </summary>
	public class ArrayCreateExpression : Expression
	{
		public readonly static TokenRole NewKeywordRole = new TokenRole ("new");
		public readonly static Role<ArraySpecifier> AdditionalArraySpecifierRole = new Role<ArraySpecifier>("AdditionalArraySpecifier");
		public readonly static Role<ArrayInitializerExpression> InitializerRole = new Role<ArrayInitializerExpression>("Initializer", ArrayInitializerExpression.Null);
		
		public CSharpTokenNode NewToken {
			get { return GetChildByRole (NewKeywordRole); }
		}
		
		public AstType Type {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole (Roles.Type, value); }
		}
		
		public AstNodeCollection<Expression> Arguments {
			get { return GetChildrenByRole (Roles.Argument); }
		}
		
		/// <summary>
		/// Gets additional array ranks (those without size info).
		/// Empty for "new int[5,1]"; will contain a single element for "new int[5][]".
		/// </summary>
		public AstNodeCollection<ArraySpecifier> AdditionalArraySpecifiers {
			get { return GetChildrenByRole(AdditionalArraySpecifierRole); }
		}
		
		public ArrayInitializerExpression Initializer {
			get { return GetChildByRole (InitializerRole); }
			set { SetChildByRole (InitializerRole, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitArrayCreateExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitArrayCreateExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitArrayCreateExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ArrayCreateExpression o = other as ArrayCreateExpression;
			return o != null && this.Type.DoMatch(o.Type, match) && this.Arguments.DoMatch(o.Arguments, match) && this.AdditionalArraySpecifiers.DoMatch(o.AdditionalArraySpecifiers, match) && this.Initializer.DoMatch(o.Initializer, match);
		}
	}
}
