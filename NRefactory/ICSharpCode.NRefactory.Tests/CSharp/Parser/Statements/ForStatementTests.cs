// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class ForStatementTests
	{
		[Test]
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
		
		[Test]
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
