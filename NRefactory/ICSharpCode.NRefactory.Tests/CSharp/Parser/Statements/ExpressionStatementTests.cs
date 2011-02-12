// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class ExpressionStatementTests
	{
		[Test]
		public void StatementExpressionTest()
		{
			ExpressionStatement stmtExprStmt = ParseUtilCSharp.ParseStatement<ExpressionStatement>("a = my.Obj.PropCall;");
			Assert.IsTrue(stmtExprStmt.Expression is AssignmentExpression);
		}
		
		[Test]
		public void StatementExpressionTest1()
		{
			ExpressionStatement stmtExprStmt = ParseUtilCSharp.ParseStatement<ExpressionStatement>("yield.yield();");
			Assert.IsTrue(stmtExprStmt.Expression is InvocationExpression);
		}
	}
}
