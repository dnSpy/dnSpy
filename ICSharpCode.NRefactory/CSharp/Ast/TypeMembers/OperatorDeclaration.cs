// 
// OperatorDeclaration.cs
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

namespace ICSharpCode.NRefactory.CSharp
{
	public enum OperatorType {
		// Unary operators
		LogicalNot,
		OnesComplement,
		Increment,
		Decrement,
		True,
		False,

		// Unary and Binary operators
		Addition,
		Subtraction,

		UnaryPlus,
		UnaryNegation,
		
		// Binary operators
		Multiply,
		Division,
		Modulus,
		BitwiseAnd,
		BitwiseOr,
		ExclusiveOr,
		LeftShift,
		RightShift,
		Equality,
		Inequality,
		GreaterThan,
		LessThan,
		GreaterThanOrEqual,
		LessThanOrEqual,

		// Implicit and Explicit
		Implicit,
		Explicit
	}
	
	public class OperatorDeclaration : MemberDeclaration
	{
		public static readonly Role<CSharpTokenNode> OperatorTypeRole = new Role<CSharpTokenNode>("OperatorType", CSharpTokenNode.Null);
		public static readonly Role<CSharpTokenNode> OperatorKeywordRole = Roles.Keyword;
		
		public OperatorType OperatorType {
			get;
			set;
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public IEnumerable<ParameterDeclaration> Parameters { 
			get { return GetChildrenByRole (Roles.Parameter); }
			set { SetChildrenByRole (Roles.Parameter, value); }
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole (Roles.Body); }
			set { SetChildByRole (Roles.Body, value); }
		}
		
		public static string GetName(OperatorType type)
		{
			return Mono.CSharp.Operator.GetMetadataName((Mono.CSharp.Operator.OpType)type);
		}
		
		public static string GetToken(OperatorType type)
		{
			return Mono.CSharp.Operator.GetName((Mono.CSharp.Operator.OpType)type);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitOperatorDeclaration (this, data);
		}
	}
}
