// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class LockStatementTests
	{
		[Test]
		public void LockStatementTest()
		{
			LockStatement lockStmt = ParseUtilCSharp.ParseStatement<LockStatement>("lock (myObj) {}");
			// TODO : Extend test.
		}
	}
}
