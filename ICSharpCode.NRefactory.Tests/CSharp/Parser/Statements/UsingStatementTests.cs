// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class UsingStatementTests
	{
		[Test]
		public void UsingStatementTest()
		{
			UsingStatement usingStmt = ParseUtilCSharp.ParseStatement<UsingStatement>("using (MyVar var = new MyVar()) { } ");
			// TODO : Extend test.
		}
	}
}
