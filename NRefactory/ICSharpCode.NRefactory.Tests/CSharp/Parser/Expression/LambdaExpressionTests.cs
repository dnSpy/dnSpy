// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
	}
}
