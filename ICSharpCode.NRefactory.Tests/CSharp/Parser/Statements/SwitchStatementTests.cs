// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class SwitchStatementTests
	{
		[Test]
		public void SwitchStatementTest()
		{
			SwitchStatement switchStmt = ParseUtilCSharp.ParseStatement<SwitchStatement>("switch (a) { case 4: case 5: break; case 6: break; default: break; }");
			Assert.AreEqual("a", ((IdentifierExpression)switchStmt.Expression).Identifier);
			// TODO: Extend test
		}
	}
}
