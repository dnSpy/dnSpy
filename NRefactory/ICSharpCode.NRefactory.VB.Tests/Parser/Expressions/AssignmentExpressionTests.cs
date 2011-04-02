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
	public class AssignmentExpressionTests
	{
		void TestAssignmentExpression(string program, AssignmentOperatorType op)
		{
			ExpressionStatement se = ParseUtil.ParseStatement<ExpressionStatement>(program);
			AssignmentExpression ae = se.Expression as AssignmentExpression;
			Assert.AreEqual(op, ae.Op);
			
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
		public void ExclusiveOrTest()
		{
			TestAssignmentExpression("a ^= b", AssignmentOperatorType.Power);
		}
		
		[Test]
		public void StringConcatTest()
		{
			TestAssignmentExpression("a &= b", AssignmentOperatorType.ConcatString);
		}

		[Test]
		public void ModulusTest()
		{
			TestAssignmentExpression("a \\= b", AssignmentOperatorType.DivideInteger);
		}
	}
}
