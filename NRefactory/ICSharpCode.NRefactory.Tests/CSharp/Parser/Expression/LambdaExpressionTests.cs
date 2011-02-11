// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore("Port unit tests")]
	public class LambdaExpressionTests
	{
		static LambdaExpression ParseCSharp(string program)
		{
			return ParseUtilCSharp.ParseExpression<LambdaExpression>(program);
		}
		
		[Test]
		public void ImplicitlyTypedExpressionBody()
		{
			/*
			LambdaExpression e = ParseCSharp("(x) => x + 1");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.ExpressionBody is BinaryOperatorExpression);
			Assert.IsTrue(e.ReturnType.IsNull);*/
			throw new NotImplementedException();
		}
		
		/* TODO Port unit tests
		[Test]
		public void ImplicitlyTypedExpressionBodyWithoutParenthesis()
		{
			LambdaExpression e = ParseCSharp("x => x + 1");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.ExpressionBody is BinaryOperatorExpression);
			Assert.IsTrue(e.ReturnType.IsNull);
		}
		
		[Test]
		public void ImplicitlyTypedStatementBody()
		{
			LambdaExpression e = ParseCSharp("(x) => { return x + 1; }");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.StatementBody.Children[0] is ReturnStatement);
			Assert.IsTrue(e.ReturnType.IsNull);
		}
		
		[Test]
		public void ImplicitlyTypedStatementBodyWithoutParenthesis()
		{
			LambdaExpression e = ParseCSharp("x => { return x + 1; }");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.Parameters[0].TypeReference.IsNull);
			Assert.IsTrue(e.StatementBody.Children[0] is ReturnStatement);
			Assert.IsTrue(e.ReturnType.IsNull);
		}
		
		[Test]
		public void ExplicitlyTypedStatementBody()
		{
			LambdaExpression e = ParseCSharp("(int x) => { return x + 1; }");
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.AreEqual("System.Int32", e.Parameters[0].TypeReference.Type);
			Assert.IsTrue(e.StatementBody.Children[0] is ReturnStatement);
			Assert.IsTrue(e.ReturnType.IsNull);
		}
		
		[Test]
		public void ExplicitlyTypedStatementBodyWithRefParameter()
		{
			LambdaExpression e = ParseCSharp("(ref int i) => i = 1");
			Assert.AreEqual("i", e.Parameters[0].ParameterName);
			Assert.IsTrue((e.Parameters[0].ParamModifier & ParameterModifiers.Ref) == ParameterModifiers.Ref);
			Assert.AreEqual("System.Int32", e.Parameters[0].TypeReference.Type);
			Assert.IsTrue(e.ReturnType.IsNull);
		}
		
		[Test]
		public void LambdaExpressionContainingConditionalExpression()
		{
			LambdaExpression e = ParseCSharp("rr => rr != null ? rr.ResolvedType : null");
			Assert.AreEqual("rr", e.Parameters[0].ParameterName);
			Assert.IsTrue(e.ExpressionBody is ConditionalExpression);
			Assert.IsTrue(e.ReturnType.IsNull);
		}*/
	}
}
