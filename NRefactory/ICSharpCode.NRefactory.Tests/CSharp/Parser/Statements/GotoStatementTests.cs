// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)


using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class GotoStatementTests
	{
		[Test]
		public void GotoStatementTest()
		{
			var gotoStmt = ParseUtilCSharp.ParseStatement<GotoStatement>("goto myLabel;");
			Assert.AreEqual("myLabel", gotoStmt.Label);
		}
		
		[Test]
		public void GotoDefaultStatementTest()
		{
			var gotoCaseStmt = ParseUtilCSharp.ParseStatement<GotoDefaultStatement>("goto default;");
		}
		
		[Test]
		public void GotoCaseStatementTest()
		{
			var gotoCaseStmt = ParseUtilCSharp.ParseStatement<GotoCaseStatement>("goto case 6;");
			Assert.IsTrue(gotoCaseStmt.LabelExpression is PrimitiveExpression);
		}
		
		[Test]
		public void BreakStatementTest()
		{
			BreakStatement breakStmt = ParseUtilCSharp.ParseStatement<BreakStatement>("break;");
		}
		
		[Test]
		public void ContinueStatementTest()
		{
			ContinueStatement continueStmt = ParseUtilCSharp.ParseStatement<ContinueStatement>("continue;");
		}
	}
}
