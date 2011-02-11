// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class FixedStatementTests
	{
		[Test]
		public void FixedStatementTest()
		{
			FixedStatement fixedStmt = ParseUtilCSharp.ParseStatement<FixedStatement>("fixed (int* ptr = &myIntArr) { }");
			// TODO : Extend test.
		}
	}
}
