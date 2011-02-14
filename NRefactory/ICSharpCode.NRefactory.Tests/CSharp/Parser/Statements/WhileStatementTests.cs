// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class WhileStatementTests
	{
		[Test]
		public void WhileStatementTest()
		{
			WhileStatement loopStmt = ParseUtilCSharp.ParseStatement<WhileStatement>("while (true) { }");
			Assert.IsTrue(loopStmt.Condition is PrimitiveExpression);
			Assert.IsTrue(loopStmt.EmbeddedStatement is BlockStatement);
		}
		
		[Test]
		public void DoWhileStatementTest()
		{
			DoWhileStatement loopStmt = ParseUtilCSharp.ParseStatement<DoWhileStatement>("do { } while (true);");
			Assert.IsTrue(loopStmt.Condition is PrimitiveExpression);
			Assert.IsTrue(loopStmt.EmbeddedStatement is BlockStatement);
		}
	}
}
