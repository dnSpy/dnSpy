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
	public class AssignmentExpressionTests
	{
		void TestAssignmentExpression(string program, AssignmentOperatorType op)
		{
			AssignmentExpression ae = ParseUtilCSharp.ParseExpression<AssignmentExpression>(program);
			
			Assert.AreEqual(op, ae.Operator);
			
			Assert.IsTrue(ae.Left is IdentifierExpression);
			Assert.IsTrue(ae.Right is IdentifierExpression);
		}
		
		[Test]
		public void AssignTest()
		{
			TestAssignmentExpression("a = b", AssignmentOperatorType.Assign);
		}
		
		[Test]
		public void AddTest()
		{
			TestAssignmentExpression("a += b", AssignmentOperatorType.Add);
		}
		
		[Test]
		public void SubtractTest()
		{
			TestAssignmentExpression("a -= b", AssignmentOperatorType.Subtract);
		}
		
		[Test]
		public void MultiplyTest()
		{
			TestAssignmentExpression("a *= b", AssignmentOperatorType.Multiply);
		}
		
		[Test]
		public void DivideTest()
		{
			TestAssignmentExpression("a /= b", AssignmentOperatorType.Divide);
		}
		
		[Test]
		public void ModulusTest()
		{
			TestAssignmentExpression("a %= b", AssignmentOperatorType.Modulus);
		}
		
		[Test]
		public void ShiftLeftTest()
		{
			TestAssignmentExpression("a <<= b", AssignmentOperatorType.ShiftLeft);
		}
		
		[Test]
		public void ShiftRightTest()
		{
			TestAssignmentExpression("a >>= b", AssignmentOperatorType.ShiftRight);
		}
		
		[Test]
		public void BitwiseAndTest()
		{
			TestAssignmentExpression("a &= b", AssignmentOperatorType.BitwiseAnd);
		}
		
		[Test]
		public void BitwiseOrTest()
		{
			TestAssignmentExpression("a |= b", AssignmentOperatorType.BitwiseOr);
		}
		
		[Test]
		public void ExclusiveOrTest()
		{
			TestAssignmentExpression("a ^= b", AssignmentOperatorType.ExclusiveOr);
		}
		
		[Test]
		public void NestedAssignment()
		{
			ParseUtilCSharp.AssertExpression(
				"a = b = c",
				new AssignmentExpression(
					new IdentifierExpression("a"),
					new AssignmentExpression(new IdentifierExpression("b"), new IdentifierExpression("c"))));
		}
	}
}
