// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.CodeDom;
using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Visitors;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Output.CodeDom.Tests
{
	[TestFixture]
	public class CodeDOMParenthesizedExpressionTest
	{
		[Test]
		public void TestParenthesizedExpression()
		{
			object output = new ParenthesizedExpression(new PrimitiveExpression(5, "5")).AcceptVisitor(new CodeDomVisitor(), null);
			Assert.IsTrue(output is CodePrimitiveExpression);
		}
	}
}
