// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// CastType(Expression, AstType)
	/// </summary>
	public class CastExpression : Expression
	{
		public CastType CastType { get; set; }
		
		public VBTokenNode CastTypeToken {
			get { return GetChildByRole (Roles.Keyword); }
		}
		
		public AstType Type {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole (Roles.Type, value); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public CastExpression ()
		{
		}
		
		public CastExpression (CastType castType, AstType castToType, Expression expression)
		{
			CastType = castType;
			AddChild (castToType, Roles.Type);
			AddChild (expression, Roles.Expression);
		}
		
		public CastExpression (CastType castType, Expression expression)
		{
			CastType = castType;
			AddChild (expression, Roles.Expression);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCastExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			CastExpression o = other as CastExpression;
			return o != null && this.CastType == o.CastType && this.Type.DoMatch(o.Type, match) && this.Expression.DoMatch(o.Expression, match);
		}
	}
	
	public enum CastType
	{
		DirectCast,
		TryCast,
		CType,
		CBool,
		CByte,
		CChar,
		CDate,
		CDec,
		CDbl,
		CInt,
		CLng,
		CObj,
		CSByte,
		CShort,
		CSng,
		CStr,
		CUInt,
		CULng,
		CUShort
	}
}
