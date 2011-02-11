// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)


using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class GotoStatementTests
	{
		[Test, Ignore("regular goto statement is broken")]
		public void GotoStatementTest()
		{
			GotoStatement gotoStmt = ParseUtilCSharp.ParseStatement<GotoStatement>("goto myLabel;");
			Assert.AreEqual(GotoType.Label, gotoStmt.GotoType);
			Assert.AreEqual("myLabel", gotoStmt.Label);
		}
		
		[Test]
		public void GotoDefaultStatementTest()
		{
			GotoStatement gotoCaseStmt = ParseUtilCSharp.ParseStatement<GotoStatement>("goto default;");
			Assert.AreEqual(GotoType.CaseDefault, gotoCaseStmt.GotoType);
		}
		
		[Test]
		public void GotoCaseStatementTest()
		{
			GotoStatement gotoCaseStmt = ParseUtilCSharp.ParseStatement<GotoStatement>("goto case 6;");
			Assert.AreEqual(GotoType.Case, gotoCaseStmt.GotoType);
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
