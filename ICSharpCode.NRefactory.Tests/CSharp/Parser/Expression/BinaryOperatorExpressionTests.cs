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
	public class BinaryOperatorExpressionTests
	{
		#region Precedence Tests
		void OperatorPrecedenceTest(string strongOperator, BinaryOperatorType strongOperatorType,
		                            string weakOperator, BinaryOperatorType weakOperatorType, bool vb)
		{
			string program = "a " + weakOperator + " b " + strongOperator + " c";
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(weakOperatorType, boe.Operator);
			Assert.IsTrue(boe.Left is IdentifierExpression);
			boe = (BinaryOperatorExpression)boe.Right;
			Assert.AreEqual(strongOperatorType, boe.Operator);
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			
			program = "a " + strongOperator + " b " + weakOperator + " c";
			
			boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(weakOperatorType, boe.Operator);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			boe = (BinaryOperatorExpression)boe.Left;
			Assert.AreEqual(strongOperatorType, boe.Operator);
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
		}
		
		void SameOperatorPrecedenceTest(string firstOperator, BinaryOperatorType firstOperatorType,
		                                string secondOperator, BinaryOperatorType secondOperatorType, bool vb)
		{
			string program = "a " + secondOperator + " b " + firstOperator + " c";
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(firstOperatorType, boe.Operator);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			boe = (BinaryOperatorExpression)boe.Left;
			Assert.AreEqual(secondOperatorType, boe.Operator);
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			
			program = "a " + firstOperator + " b " + secondOperator + " c";
			boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(secondOperatorType, boe.Operator);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			boe = (BinaryOperatorExpression)boe.Left;
			Assert.AreEqual(firstOperatorType, boe.Operator);
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
		}
		
		[Test]
		public void OperatorPrecedenceTest()
		{
			SameOperatorPrecedenceTest("*", BinaryOperatorType.Multiply, "/", BinaryOperatorType.Divide, false);
			SameOperatorPrecedenceTest("*", BinaryOperatorType.Multiply, "%", BinaryOperatorType.Modulus, false);
			OperatorPrecedenceTest("*", BinaryOperatorType.Multiply, "+", BinaryOperatorType.Add, false);
			SameOperatorPrecedenceTest("-", BinaryOperatorType.Subtract, "+", BinaryOperatorType.Add, false);
			OperatorPrecedenceTest("+", BinaryOperatorType.Add, "<<", BinaryOperatorType.ShiftLeft, false);
			SameOperatorPrecedenceTest(">>", BinaryOperatorType.ShiftRight, "<<", BinaryOperatorType.ShiftLeft, false);
			OperatorPrecedenceTest("<<", BinaryOperatorType.ShiftLeft, "==", BinaryOperatorType.Equality, false);
			SameOperatorPrecedenceTest("!=", BinaryOperatorType.InEquality, "==", BinaryOperatorType.Equality, false);
			OperatorPrecedenceTest("==", BinaryOperatorType.Equality, "&", BinaryOperatorType.BitwiseAnd, false);
			OperatorPrecedenceTest("&", BinaryOperatorType.BitwiseAnd, "^", BinaryOperatorType.ExclusiveOr, false);
			OperatorPrecedenceTest("^", BinaryOperatorType.ExclusiveOr, "|", BinaryOperatorType.BitwiseOr, false);
			OperatorPrecedenceTest("|", BinaryOperatorType.BitwiseOr, "&&", BinaryOperatorType.ConditionalAnd, false);
			OperatorPrecedenceTest("&&", BinaryOperatorType.ConditionalAnd, "||", BinaryOperatorType.ConditionalOr, false);
			OperatorPrecedenceTest("||", BinaryOperatorType.ConditionalOr, "??", BinaryOperatorType.NullCoalescing, false);
		}
		#endregion
		
		void TestBinaryOperatorExpressionTest(string program, BinaryOperatorType op)
		{
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(program);
			Assert.AreEqual(op, boe.Operator);
			
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
			
		}
		
		[Test]
		public void SubtractionLeftToRight()
		{
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("a - b - c");
			Assert.IsTrue(boe.Right is IdentifierExpression);
			Assert.IsTrue(boe.Left is BinaryOperatorExpression);
		}
		
		[Test]
		public void NullCoalescingRightToLeft()
		{
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("a ?? b ?? c");
			Assert.IsTrue(boe.Left is IdentifierExpression);
			Assert.IsTrue(boe.Right is BinaryOperatorExpression);
		}
		
		[Test]
		public void BitwiseAndTest()
		{
			TestBinaryOperatorExpressionTest("a & b", BinaryOperatorType.BitwiseAnd);
		}
		
		[Test]
		public void BitwiseOrTest()
		{
			TestBinaryOperatorExpressionTest("a | b", BinaryOperatorType.BitwiseOr);
		}
		
		[Test]
		public void LogicalAndTest()
		{
			TestBinaryOperatorExpressionTest("a && b", BinaryOperatorType.ConditionalAnd);
		}
		
		[Test]
		public void LogicalOrTest()
		{
			TestBinaryOperatorExpressionTest("a || b", BinaryOperatorType.ConditionalOr);
		}
		
		[Test]
		public void ExclusiveOrTest()
		{
			TestBinaryOperatorExpressionTest("a ^ b", BinaryOperatorType.ExclusiveOr);
		}
		
		
		[Test]
		public void GreaterThanTest()
		{
			TestBinaryOperatorExpressionTest("a > b", BinaryOperatorType.GreaterThan);
		}
		
		[Test]
		public void GreaterThanOrEqualTest()
		{
			TestBinaryOperatorExpressionTest("a >= b", BinaryOperatorType.GreaterThanOrEqual);
		}
		
		[Test]
		public void EqualityTest()
		{
			TestBinaryOperatorExpressionTest("a == b", BinaryOperatorType.Equality);
		}
		
		[Test]
		public void InEqualityTest()
		{
			TestBinaryOperatorExpressionTest("a != b", BinaryOperatorType.InEquality);
		}
		
		[Test]
		public void LessThanTest()
		{
			TestBinaryOperatorExpressionTest("a < b", BinaryOperatorType.LessThan);
		}
		
		[Test]
		public void LessThanOrEqualTest()
		{
			TestBinaryOperatorExpressionTest("a <= b", BinaryOperatorType.LessThanOrEqual);
		}
		
		[Test]
		public void AddTest()
		{
			TestBinaryOperatorExpressionTest("a + b", BinaryOperatorType.Add);
		}
		
		[Test]
		public void SubtractTest()
		{
			TestBinaryOperatorExpressionTest("a - b", BinaryOperatorType.Subtract);
		}
		
		[Test]
		public void MultiplyTest()
		{
			TestBinaryOperatorExpressionTest("a * b", BinaryOperatorType.Multiply);
		}
		
		[Test]
		public void DivideTest()
		{
			TestBinaryOperatorExpressionTest("a / b", BinaryOperatorType.Divide);
		}
		
		[Test]
		public void ModulusTest()
		{
			TestBinaryOperatorExpressionTest("a % b", BinaryOperatorType.Modulus);
		}
		
		[Test]
		public void ShiftLeftTest()
		{
			TestBinaryOperatorExpressionTest("a << b", BinaryOperatorType.ShiftLeft);
		}
		
		[Test]
		public void ShiftRightTest()
		{
			TestBinaryOperatorExpressionTest("a >> b", BinaryOperatorType.ShiftRight);
		}
		
		[Test]
		public void NullCoalescingTest()
		{
			TestBinaryOperatorExpressionTest("a ?? b", BinaryOperatorType.NullCoalescing);
		}
		
		[Test]
		public void LessThanOrGreaterTest()
		{
			const string expr = "i1 < 0 || i1 > (Count - 1)";
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>(expr);
			Assert.AreEqual(BinaryOperatorType.ConditionalOr, boe.Operator);
		}
	}
}
