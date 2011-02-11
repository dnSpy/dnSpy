// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
	}
}
