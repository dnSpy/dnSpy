// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class LambdaExpressionTests
	{
		static LambdaExpression Parse(string program)
		{
			return ParseUtil.ParseExpression<LambdaExpression>(program);
		}
		
		[Test]
		public void LambdaWithParameters()
		{
			LambdaExpression e = Parse("Function(x As Boolean) x Or True");
			Assert.AreEqual(1, e.Parameters.Count);
			Assert.AreEqual("x", e.Parameters[0].ParameterName);
			Assert.AreEqual("System.Boolean", e.Parameters[0].TypeReference.Type);
			Assert.IsTrue(e.ExpressionBody is BinaryOperatorExpression);
			Assert.IsTrue(e.ReturnType.IsNull);
		}

		[Test]
		public void LambdaWithoutParameters()
		{
			LambdaExpression e = Parse("Function() x Or True");
			Assert.AreEqual(0, e.Parameters.Count);
			Assert.IsTrue(e.ExpressionBody is BinaryOperatorExpression);
			Assert.IsTrue(e.ReturnType.IsNull, "ReturnType");
		}
		
		[Test]
		public void NestedLambda()
		{
			LambdaExpression e = Parse("Function(x As Boolean) Function(y As Boolean) x And y");
			Assert.AreEqual(1, e.Parameters.Count);
			Assert.IsTrue(e.ExpressionBody is LambdaExpression);
			Assert.IsTrue(e.ReturnType.IsNull, "ReturnType");
		}
		
		[Test]
		public void SubLambda()
		{
			LambdaExpression e = Parse("Sub(x As Integer) Console.WriteLine(x)");
			Assert.AreEqual(1, e.Parameters.Count);
			Assert.IsTrue(e.StatementBody is ExpressionStatement);
			Assert.IsNotNull(e.ReturnType);
			Assert.AreEqual("System.Void", e.ReturnType.Type);
			Assert.IsTrue(e.ReturnType.IsKeyword);
		}
		
		[Test]
		public void SubWithStatementLambda()
		{
			LambdaExpression e = Parse("Sub(x As Integer) Call Console.WriteLine(x)");
			Assert.AreEqual(1, e.Parameters.Count);
			Assert.IsTrue(e.StatementBody is ExpressionStatement);
			Assert.IsNotNull(e.ReturnType);
			Assert.AreEqual("System.Void", e.ReturnType.Type);
			Assert.IsTrue(e.ReturnType.IsKeyword);
		}
		
		[Test]
		public void MultilineSubLambda()
		{
			LambdaExpression e = Parse("Sub(x As Integer)\n" +
			                                "	For i As Integer = 0 To x\n" +
			                                "		Console.WriteLine(i)\n" +
			                                "	Next\n" +
			                                "End Sub");
			Assert.AreEqual(1, e.Parameters.Count);
			Assert.IsTrue(e.StatementBody is BlockStatement);
			Assert.IsNotNull(e.ReturnType);
			Assert.AreEqual("System.Void", e.ReturnType.Type);
			Assert.IsTrue(e.ReturnType.IsKeyword);
			
			BlockStatement b = e.StatementBody as BlockStatement;
			
			Assert.AreEqual(1, b.Children.Count);
			Assert.IsTrue(b.Children[0] is ForNextStatement);
		}
		
		[Test]
		public void MultilineFunctionLambda()
		{
			LambdaExpression e = Parse("Function(x As Integer)\n" +
			                                "	Dim prod As Integer = 1\n" +
			                                "	For i As Integer = 1 To x\n" +
			                                "		prod = prod * i\n" +
			                                "	Next\n" +
			                                "	Return prod\n" +
			                                "End Function");
			Assert.AreEqual(1, e.Parameters.Count);
			Assert.IsTrue(e.StatementBody is BlockStatement);
			Assert.IsTrue(e.ReturnType.IsNull);
			
			BlockStatement b = e.StatementBody as BlockStatement;
			
			Assert.AreEqual(3, b.Children.Count);
			Assert.IsTrue(b.Children[0] is LocalVariableDeclaration);
			Assert.IsTrue(b.Children[1] is ForNextStatement);
			Assert.IsTrue(b.Children[2] is ReturnStatement);
		}
		
		[Test]
		public void MultilineFunctionWithReturnTypeLambda()
		{
			LambdaExpression e = Parse("Function(x As Integer) As Integer\n" +
			                                "	Dim prod As Integer = 1\n" +
			                                "	For i As Integer = 1 To x\n" +
			                                "		prod = prod * i\n" +
			                                "	Next\n" +
			                                "	Return prod\n" +
			                                "End Function");
			Assert.AreEqual(1, e.Parameters.Count);
			Assert.IsTrue(e.StatementBody is BlockStatement);
			Assert.IsNotNull(e.ReturnType);
			Assert.AreEqual("System.Int32", e.ReturnType.Type);
			Assert.IsTrue(e.ReturnType.IsKeyword);
			
			BlockStatement b = e.StatementBody as BlockStatement;
			
			Assert.AreEqual(3, b.Children.Count);
			Assert.IsTrue(b.Children[0] is LocalVariableDeclaration);
			Assert.IsTrue(b.Children[1] is ForNextStatement);
			Assert.IsTrue(b.Children[2] is ReturnStatement);
		}
	}
}
