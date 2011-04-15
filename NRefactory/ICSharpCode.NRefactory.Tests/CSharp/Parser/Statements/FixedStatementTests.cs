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
			ParseUtilCSharp.AssertStatement(
				"fixed (int* ptr = myIntArr) { }",
				new FixedStatement {
					Type = new PrimitiveType("int").MakePointerType(),
					Variables = {
						new VariableInitializer {
							Name = "ptr",
							Initializer = new IdentifierExpression("myIntArr")
						}
					},
					EmbeddedStatement = new BlockStatement()
				});
		}
		
		[Test]
		public void FixedStatementWithMultipleVariables()
		{
			ParseUtilCSharp.AssertStatement(
				"fixed (int* ptr1 = &myIntArr[1], ptr2 = myIntArr) { }",
				new FixedStatement {
					Type = new PrimitiveType("int").MakePointerType(),
					Variables = {
						new VariableInitializer {
							Name = "ptr1",
							Initializer = new UnaryOperatorExpression(
								UnaryOperatorType.AddressOf, 
								new IndexerExpression { Target = new IdentifierExpression("myIntArr"), Arguments = { new PrimitiveExpression(1) } })
						},
						new VariableInitializer {
							Name = "ptr2",
							Initializer = new IdentifierExpression("myIntArr")
						}
					},
					EmbeddedStatement = new BlockStatement()
				});
		}
	}
}
