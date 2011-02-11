// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)


using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class YieldStatementTests
	{
		[Test]
		public void YieldReturnStatementTest()
		{
			YieldStatement yieldStmt = ParseUtilCSharp.ParseStatement<YieldStatement>("yield return \"Foo\";");
			PrimitiveExpression expr =  (PrimitiveExpression)yieldStmt.Expression;
			Assert.AreEqual("Foo", expr.Value);
		}
		
		[Test]
		public void YieldBreakStatementTest()
		{
			YieldStatement yieldStmt = ParseUtilCSharp.ParseStatement<YieldStatement>("yield break;");
			Assert.IsTrue(yieldStmt.Expression.IsNull);
		}
		
		[Test]
		public void YieldAsVariableTest()
		{
			ExpressionStatement se = ParseUtilCSharp.ParseStatement<ExpressionStatement>("yield = 3;");
			AssignmentExpression ae = se.Expression as AssignmentExpression;
			
			Assert.AreEqual(AssignmentOperatorType.Assign, ae.Operator);
			
			Assert.IsTrue(ae.Left is IdentifierExpression);
			Assert.AreEqual("yield", ((IdentifierExpression)ae.Left).Identifier);
			Assert.IsTrue(ae.Right is PrimitiveExpression);
		}
	}
}
