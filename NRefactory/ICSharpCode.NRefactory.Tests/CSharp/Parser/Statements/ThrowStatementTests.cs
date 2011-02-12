// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class ThrowStatementTests
	{
		[Test]
		public void EmptyThrowStatementTest()
		{
			ThrowStatement throwStmt = ParseUtilCSharp.ParseStatement<ThrowStatement>("throw;");
			Assert.IsTrue(throwStmt.Expression.IsNull);
		}
		
		[Test]
		public void ThrowStatementTest()
		{
			ThrowStatement throwStmt = ParseUtilCSharp.ParseStatement<ThrowStatement>("throw new Exception();");
			Assert.IsTrue(throwStmt.Expression is ObjectCreateExpression);
		}
	}
}
