// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class IndexerExpressionTests
	{
		[Test]
		public void IndexerExpressionTest()
		{
			IndexerExpression ie = ParseUtilCSharp.ParseExpression<IndexerExpression>("field[1, \"Hello\", 'a']");
			Assert.IsTrue(ie.Target is IdentifierExpression);
			
			Assert.AreEqual(3, ie.Arguments.Count());
			
			Assert.IsTrue(ie.Arguments.ElementAt(0) is PrimitiveExpression);
			Assert.AreEqual(1, (int)((PrimitiveExpression)ie.Arguments.ElementAt(0)).Value);
			Assert.IsTrue(ie.Arguments.ElementAt(1) is PrimitiveExpression);
			Assert.AreEqual("Hello", (string)((PrimitiveExpression)ie.Arguments.ElementAt(1)).Value);
			Assert.IsTrue(ie.Arguments.ElementAt(2) is PrimitiveExpression);
			Assert.AreEqual('a', (char)((PrimitiveExpression)ie.Arguments.ElementAt(2)).Value);
		}
	}
}
