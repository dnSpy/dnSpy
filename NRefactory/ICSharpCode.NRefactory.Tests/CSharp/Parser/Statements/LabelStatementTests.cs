// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class LabelStatementTests
	{
		[Test]
		public void LabelStatementTest()
		{
			BlockStatement block = ParseUtilCSharp.ParseStatement<BlockStatement>("{ myLabel: ; }");
			LabelStatement labelStmt = (LabelStatement)block.Statements.First();
			Assert.AreEqual("myLabel", labelStmt.Label);
		}
		
		[Test]
		public void Label2StatementTest()
		{
			BlockStatement block = ParseUtilCSharp.ParseStatement<BlockStatement>("{ yield: ; }");
			LabelStatement labelStmt = (LabelStatement)block.Statements.First();
			Assert.AreEqual("yield", labelStmt.Label);
		}
	}
}
