// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class UsingStatementTests
	{
		[Test, Ignore("Parser doesn't report the VariableDeclarationStatement")]
		public void UsingStatementWithVariableDeclaration()
		{
			UsingStatement usingStmt = ParseUtilCSharp.ParseStatement<UsingStatement>("using (MyVar var = new MyVar()) { } ");
			VariableDeclarationStatement varDecl = (VariableDeclarationStatement)usingStmt.ResourceAcquisition;
			Assert.AreEqual("var", varDecl.Variables.Single().Name);
			Assert.IsTrue(varDecl.Variables.Single().Initializer is ObjectCreateExpression);
			Assert.AreEqual("MyVar", ((SimpleType)varDecl.Type).Identifier);
			Assert.IsTrue(usingStmt.EmbeddedStatement is BlockStatement);
		}
		
		public void UsingStatementWithExpression()
		{
			UsingStatement usingStmt = ParseUtilCSharp.ParseStatement<UsingStatement>("using (new MyVar()) { } ");
			Assert.IsTrue(usingStmt.ResourceAcquisition is ObjectCreateExpression);
			Assert.IsTrue(usingStmt.EmbeddedStatement is BlockStatement);
		}
	}
}
