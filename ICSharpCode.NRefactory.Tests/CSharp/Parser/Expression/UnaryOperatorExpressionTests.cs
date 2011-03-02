// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
		
		[Test]
		public void StarTest()
		{
			TestUnaryOperatorExpressionTest("*a", UnaryOperatorType.Dereference);
		}
		
		[Test]
		public void BitWiseAndTest()
		{
			TestUnaryOperatorExpressionTest("&a", UnaryOperatorType.AddressOf);
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
