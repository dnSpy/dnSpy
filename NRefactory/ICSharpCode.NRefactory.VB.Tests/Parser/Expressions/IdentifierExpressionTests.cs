// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class IdentifierExpressionTests
	{
		#region VB.NET
		[Test]
		public void VBNetIdentifierExpressionTest1()
		{
			IdentifierExpression ie = ParseUtil.ParseExpression<IdentifierExpression>("MyIdentifier");
			Assert.AreEqual("MyIdentifier", ie.Identifier);
		}
		
		[Test]
		public void VBNetIdentifierExpressionTest2()
		{
			IdentifierExpression ie = ParseUtil.ParseExpression<IdentifierExpression>("[Public]");
			Assert.AreEqual("Public", ie.Identifier);
		}
		
		[Test]
		public void VBNetContextKeywordsTest()
		{
			Assert.AreEqual("Assembly", ParseUtil.ParseExpression<IdentifierExpression>("Assembly").Identifier);
			Assert.AreEqual("Custom", ParseUtil.ParseExpression<IdentifierExpression>("Custom").Identifier);
			Assert.AreEqual("Off", ParseUtil.ParseExpression<IdentifierExpression>("Off").Identifier);
			Assert.AreEqual("Explicit", ParseUtil.ParseExpression<IdentifierExpression>("Explicit").Identifier);
		}
		#endregion
	}
}
