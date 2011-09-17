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
