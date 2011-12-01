// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class LambdaExpressionTests
	{
		[Test]
		public void ImplicitlyTypedExpressionBody()
		{
			ParseUtilCSharp.AssertExpression(
				"(x) => x + 1",
				new LambdaExpression {
					Parameters = { new ParameterDeclaration { Name = "x" } },
					Body = new BinaryOperatorExpression(new IdentifierExpression("x"), BinaryOperatorType.Add, new PrimitiveExpression(1))
				});
		}
		
		[Test]
		public void ImplicitlyTypedExpressionBodyWithoutParenthesis()
		{
			ParseUtilCSharp.AssertExpression(
				"x => x + 1",
				new LambdaExpression {
					Parameters = { new ParameterDeclaration { Name = "x" } },
					Body = new BinaryOperatorExpression(new IdentifierExpression("x"), BinaryOperatorType.Add, new PrimitiveExpression(1))
				});
		}
		
		[Test]
		public void ImplicitlyTypedStatementBody()
		{
			ParseUtilCSharp.AssertExpression(
				"(x) => { return x + 1; }",
				new LambdaExpression {
					Parameters = { new ParameterDeclaration { Name = "x" } },
					Body = new BlockStatement {
						new ReturnStatement {
							Expression = new BinaryOperatorExpression(
								new IdentifierExpression("x"), BinaryOperatorType.Add, new PrimitiveExpression(1))
						}}});
		}
		
		[Test]
		public void ImplicitlyTypedStatementBodyWithoutParenthesis()
		{
			ParseUtilCSharp.AssertExpression(
				"x => { return x + 1; }",
				new LambdaExpression {
					Parameters = { new ParameterDeclaration { Name = "x" } },
					Body = new BlockStatement {
						new ReturnStatement {
							Expression = new BinaryOperatorExpression(
								new IdentifierExpression("x"), BinaryOperatorType.Add, new PrimitiveExpression(1))
						}}});
		}
		
		[Test]
		public void ExplicitlyTypedStatementBody()
		{
			ParseUtilCSharp.AssertExpression(
				"(int x) => { return x + 1; }",
				new LambdaExpression {
					Parameters = { new ParameterDeclaration { Type = new PrimitiveType("int"), Name = "x" } },
					Body = new BlockStatement {
						new ReturnStatement {
							Expression = new BinaryOperatorExpression(
								new IdentifierExpression("x"), BinaryOperatorType.Add, new PrimitiveExpression(1))
						}}});
		}
		
		[Test]
		public void ExplicitlyTypedWithRefParameter()
		{
			ParseUtilCSharp.AssertExpression(
				"(ref int i) => i = 1",
				new LambdaExpression {
					Parameters = {
						new ParameterDeclaration {
							ParameterModifier = ParameterModifier.Ref,
							Type = new PrimitiveType("int"),
							Name = "i"
						}
					},
					Body = new AssignmentExpression(new IdentifierExpression("i"), new PrimitiveExpression(1))
				});
		}
		
		[Test]
		public void LambdaExpressionContainingConditionalExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"rr => rr != null ? rr.ResolvedType : null",
				new LambdaExpression {
					Parameters = { new ParameterDeclaration { Name = "rr" } },
					Body = new ConditionalExpression {
						Condition = new BinaryOperatorExpression(
							new IdentifierExpression("rr"), BinaryOperatorType.InEquality, new NullReferenceExpression()),
						TrueExpression = new IdentifierExpression("rr").Member("ResolvedType"),
						FalseExpression = new NullReferenceExpression()
					}});
		}
		
		[Test]
		public void AsyncLambdaExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"async x => x + 1",
				new LambdaExpression {
					IsAsync = true,
					Parameters = { new ParameterDeclaration { Name = "x" } },
					Body = new BinaryOperatorExpression(new IdentifierExpression("x"), BinaryOperatorType.Add, new PrimitiveExpression(1))
				});
		}
		
		[Test]
		public void AsyncLambdaExpressionWithMultipleParameters()
		{
			ParseUtilCSharp.AssertExpression(
				"async (x,y) => x + 1",
				new LambdaExpression {
					IsAsync = true,
					Parameters = { new ParameterDeclaration { Name = "x" }, new ParameterDeclaration { Name = "y" } },
					Body = new BinaryOperatorExpression(new IdentifierExpression("x"), BinaryOperatorType.Add, new PrimitiveExpression(1))
				});
		}
	}
}
