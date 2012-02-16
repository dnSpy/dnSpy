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
	public class UnaryOperatorExpressionTests
	{
		void TestUnaryOperatorExpressionTest(string program, UnaryOperatorType op)
		{
			UnaryOperatorExpression uoe = ParseUtilCSharp.ParseExpression<UnaryOperatorExpression>(program);
			Assert.AreEqual(op, uoe.Operator);
			
			Assert.IsTrue(uoe.Expression is IdentifierExpression);
			Assert.AreEqual(new TextLocation(1, 1), uoe.StartLocation);
			Assert.AreEqual(new TextLocation(1, program.Length + 1), uoe.EndLocation);
		}
		
		[Test]
		public void NotTest()
		{
			TestUnaryOperatorExpressionTest("!a", UnaryOperatorType.Not);
		}
		
		[Test]
		public void BitNotTest()
		{
			TestUnaryOperatorExpressionTest("~a", UnaryOperatorType.BitNot);
		}
		
		[Test]
		public void MinusTest()
		{
			TestUnaryOperatorExpressionTest("-a", UnaryOperatorType.Minus);
		}
		
		[Test]
		public void PlusTest()
		{
			TestUnaryOperatorExpressionTest("+a", UnaryOperatorType.Plus);
		}
		
		[Test]
		public void IncrementTest()
		{
			TestUnaryOperatorExpressionTest("++a", UnaryOperatorType.Increment);
		}
		
		[Test]
		public void DecrementTest()
		{
			TestUnaryOperatorExpressionTest("--a", UnaryOperatorType.Decrement);
		}
		
		[Test]
		public void PostIncrementTest()
		{
			TestUnaryOperatorExpressionTest("a++", UnaryOperatorType.PostIncrement);
		}
		
		[Test]
		public void PostDecrementTest()
		{
			TestUnaryOperatorExpressionTest("a--", UnaryOperatorType.PostDecrement);
		}
		
		[Test, Ignore("Incorrect start position")]
		public void Dereference()
		{
			TestUnaryOperatorExpressionTest("*a", UnaryOperatorType.Dereference);
		}
		
		[Test]
		public void AddressOf()
		{
			TestUnaryOperatorExpressionTest("&a", UnaryOperatorType.AddressOf);
		}
		
		[Test]
		public void Await()
		{
			ParseUtilCSharp.AssertExpression(
				"async a => await a",
				new LambdaExpression {
					IsAsync = true,
					Parameters = { new ParameterDeclaration { Name = "a" } },
					Body = new UnaryOperatorExpression(UnaryOperatorType.Await, new IdentifierExpression("a"))
				});
		}
		
		[Test]
		public void AwaitAwait()
		{
			ParseUtilCSharp.AssertExpression(
				"async a => await await a",
				new LambdaExpression {
					IsAsync = true,
					Parameters = { new ParameterDeclaration { Name = "a" } },
					Body = new UnaryOperatorExpression(
						UnaryOperatorType.Await,
						new UnaryOperatorExpression(UnaryOperatorType.Await, new IdentifierExpression("a")))
				});
		}
		
		[Test]
		public void DereferenceAfterCast()
		{
			UnaryOperatorExpression uoe = ParseUtilCSharp.ParseExpression<UnaryOperatorExpression>("*((SomeType*) &w)");
			Assert.AreEqual(UnaryOperatorType.Dereference, uoe.Operator);
			ParenthesizedExpression pe = (ParenthesizedExpression)uoe.Expression;
			CastExpression ce = (CastExpression)pe.Expression;
			ComposedType type = (ComposedType)ce.Type;
			Assert.AreEqual("SomeType", ((SimpleType)type.BaseType).Identifier);
			Assert.AreEqual(1, type.PointerRank);
			
			UnaryOperatorExpression adrOf = (UnaryOperatorExpression)ce.Expression;
			Assert.AreEqual(UnaryOperatorType.AddressOf, adrOf.Operator);
		}
	}
}
