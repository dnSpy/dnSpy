// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class ReturnStatementTests
	{
		[Test]
		public void EmptyReturnStatementTest()
		{
			ReturnStatement returnStatement = ParseUtilCSharp.ParseStatement<ReturnStatement>("return;");
			Assert.IsTrue(returnStatement.Expression.IsNull);
		}
		
		[Test]
		public void ReturnStatementTest()
		{
			ReturnStatement returnStatement = ParseUtilCSharp.ParseStatement<ReturnStatement>("return 5;");
			Assert.IsTrue(returnStatement.Expression is PrimitiveExpression);
		}
		
		[Test]
		public void ReturnStatementTest1()
		{
			ReturnStatement returnStatement = ParseUtilCSharp.ParseStatement<ReturnStatement>("return yield;");
			Assert.IsTrue(returnStatement.Expression is IdentifierExpression);
		}
	}
}
