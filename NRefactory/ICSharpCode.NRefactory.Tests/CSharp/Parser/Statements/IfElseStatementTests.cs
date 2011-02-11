// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class IfElseStatementTests
	{
		[Test]
		public void SimpleIfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilCSharp.ParseStatement<IfElseStatement>("if (true) { }");
			Assert.IsTrue(ifElseStatement.Condition is PrimitiveExpression);
			Assert.IsTrue(ifElseStatement.TrueStatement is BlockStatement);
			Assert.IsTrue(ifElseStatement.FalseStatement.IsNull);
		}
		
		[Test]
		public void SimpleIfElseStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilCSharp.ParseStatement<IfElseStatement>("if (true) { } else { }");
			Assert.IsTrue(ifElseStatement.Condition is PrimitiveExpression);
			Assert.IsTrue(ifElseStatement.TrueStatement is BlockStatement);
			Assert.IsTrue(ifElseStatement.FalseStatement is BlockStatement);
		}
		
		[Test]
		public void IfElseIfStatementTest()
		{
			IfElseStatement ifElseStatement = ParseUtilCSharp.ParseStatement<IfElseStatement>("if (1) { } else if (2) { } else if (3) { } else { }");
			Assert.IsTrue(ifElseStatement.Condition is PrimitiveExpression);
			Assert.IsTrue(ifElseStatement.TrueStatement is BlockStatement);
			Assert.IsTrue(ifElseStatement.FalseStatement is IfElseStatement);
		}
	}
}
