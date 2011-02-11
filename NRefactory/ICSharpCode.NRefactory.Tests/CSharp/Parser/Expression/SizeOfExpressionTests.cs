// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class SizeOfExpressionTests
	{
		[Test, Ignore("type references not implemented yet")]
		public void SizeOfExpressionTest()
		{
			SizeOfExpression soe = ParseUtilCSharp.ParseExpression<SizeOfExpression>("sizeof(MyType)");
			Assert.AreEqual("MyType", soe.Type);
		}
	}
}
