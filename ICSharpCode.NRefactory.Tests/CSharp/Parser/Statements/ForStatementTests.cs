// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class ForStatementTests
	{
		[Test, Ignore("variable type in foreach is broken")]
		public void ForeachStatementTest()
		{
			ParseUtilCSharp.AssertStatement(
				"foreach (int i in myColl) {} ",
				new ForeachStatement {
					VariableType = new PrimitiveType("int"),
					VariableName = "i",
					InExpression = new IdentifierExpression("myColl"),
					EmbeddedStatement = new BlockStatement()
				});
		}
		
		[Test, Ignore("for statement is broken when Initializers.Count()!=1")]
		public void EmptyForStatementTest()
		{
			ForStatement forStmt = ParseUtilCSharp.ParseStatement<ForStatement>("for (;;) ;");
			Assert.AreEqual(0, forStmt.Initializers.Count());
			Assert.AreEqual(0, forStmt.Iterators.Count());
			Assert.IsTrue(forStmt.Condition.IsNull);
			Assert.IsTrue(forStmt.EmbeddedStatement is EmptyStatement);
		}
		
		[Test]
		public void ForStatementTest()
		{
			ForStatement forStmt = ParseUtilCSharp.ParseStatement<ForStatement>("for (int i = 5; i < 6; ++i) {} ");
			var init = (VariableDeclarationStatement)forStmt.Initializers.Single();
			Assert.AreEqual("i", init.Variables.Single().Name);
			
			Assert.IsTrue(forStmt.Condition is BinaryOperatorExpression);
			
			var inc = (ExpressionStatement)forStmt.Iterators.Single();
			Assert.IsTrue(inc.Expression is UnaryOperatorExpression);
		}
		
		[Test]
		public void ForStatementTestMultipleInitializers()
		{
			ForStatement forStmt = ParseUtilCSharp.ParseStatement<ForStatement>("for (i = 0, j = 1; i < 6; ++i) {} ");
			Assert.AreEqual(2, forStmt.Initializers.Count());
			Assert.IsTrue(forStmt.Iterators.All(i => i is ExpressionStatement));
		}
		
		[Test]
		public void ForStatementTestMultipleIterators()
		{
			ForStatement forStmt = ParseUtilCSharp.ParseStatement<ForStatement>("for (int i = 5, j = 10; i < 6; ++i, j--) {} ");
			Assert.AreEqual(1, forStmt.Initializers.Count());
			Assert.AreEqual(2, ((VariableDeclarationStatement)forStmt.Initializers.Single()).Variables.Count());
			Assert.AreEqual(2, forStmt.Iterators.Count());
			Assert.IsTrue(forStmt.Iterators.All(i => i is ExpressionStatement));
		}
	}
}
