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
	public enum OperatorType
	{
		// Values must correspond to Mono.CSharp.Operator.OpType
		// due to the casts used in OperatorDeclaration.
		
		// Unary operators
		LogicalNot = Mono.CSharp.Operator.OpType.LogicalNot,
		OnesComplement = Mono.CSharp.Operator.OpType.OnesComplement,
		Increment = Mono.CSharp.Operator.OpType.Increment,
		Decrement = Mono.CSharp.Operator.OpType.Decrement,
		True = Mono.CSharp.Operator.OpType.True,
		False = Mono.CSharp.Operator.OpType.False,

		// Unary and Binary operators
		Addition = Mono.CSharp.Operator.OpType.Addition,
		Subtraction = Mono.CSharp.Operator.OpType.Subtraction,

		UnaryPlus = Mono.CSharp.Operator.OpType.UnaryPlus,
		UnaryNegation = Mono.CSharp.Operator.OpType.UnaryNegation,
		
		// Binary operators
		Multiply = Mono.CSharp.Operator.OpType.Multiply,
		Division = Mono.CSharp.Operator.OpType.Division,
		Modulus = Mono.CSharp.Operator.OpType.Modulus,
		BitwiseAnd = Mono.CSharp.Operator.OpType.BitwiseAnd,
		BitwiseOr = Mono.CSharp.Operator.OpType.BitwiseOr,
		ExclusiveOr = Mono.CSharp.Operator.OpType.ExclusiveOr,
		LeftShift = Mono.CSharp.Operator.OpType.LeftShift,
		RightShift = Mono.CSharp.Operator.OpType.RightShift,
		Equality = Mono.CSharp.Operator.OpType.Equality,
		Inequality = Mono.CSharp.Operator.OpType.Inequality,
		GreaterThan = Mono.CSharp.Operator.OpType.GreaterThan,
		LessThan = Mono.CSharp.Operator.OpType.LessThan,
		GreaterThanOrEqual = Mono.CSharp.Operator.OpType.GreaterThanOrEqual,
		LessThanOrEqual = Mono.CSharp.Operator.OpType.LessThanOrEqual,

		// Implicit and Explicit
		Implicit = Mono.CSharp.Operator.OpType.Implicit,
		Explicit = Mono.CSharp.Operator.OpType.Explicit
	}
	
	public class OperatorDeclaration : AttributedNode
	{
		public static readonly Role<CSharpTokenNode> OperatorTypeRole = new Role<CSharpTokenNode> ("OperatorType", CSharpTokenNode.Null);
		public static readonly Role<CSharpTokenNode> OperatorKeywordRole = Roles.Keyword;
		
		public OperatorType OperatorType {
			get;
			set;
		}
		
		public AstType ReturnType {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole (Roles.Type, value); }
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
		
		public BlockStatement Body {
			get { return GetChildByRole (Roles.Body); }
			set { SetChildByRole (Roles.Body, value); }
		}
		
		/// <summary>
		/// Gets the operator type from the method name, or null, if the method does not represent one of the known operator types.
		/// </summary>
		public static OperatorType? GetOperatorType(string methodName)
		{
			return (OperatorType?)Mono.CSharp.Operator.GetType(methodName);
		}
		
		/// <summary>
		/// Gets the method name for the operator type. ("op_Addition", "op_Implicit", etc.)
		/// </summary>
		public static string GetName (OperatorType type)
		{
			return Mono.CSharp.Operator.GetMetadataName ((Mono.CSharp.Operator.OpType)type);
		}
		
		/// <summary>
		/// Gets the token for the operator type ("+", "implicit", etc.)
		/// </summary>
		public static string GetToken (OperatorType type)
		{
			return Mono.CSharp.Operator.GetName ((Mono.CSharp.Operator.OpType)type);
		}
		
		public override NodeType NodeType {
			get { return NodeType.Member; }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data = default(T))
		{
			return visitor.VisitOperatorDeclaration (this, data);
		}
		
		public string Name {
			get { return GetName (this.OperatorType); }
		}
		
		protected internal override bool DoMatch (AstNode other, PatternMatching.Match match)
		{
			OperatorDeclaration o = other as OperatorDeclaration;
			return o != null && this.MatchAttributesAndModifiers (o, match) && this.OperatorType == o.OperatorType
				&& this.ReturnType.DoMatch (o.ReturnType, match)
				&& this.Parameters.DoMatch (o.Parameters, match) && this.Body.DoMatch (o.Body, match);
		}
	}
}
