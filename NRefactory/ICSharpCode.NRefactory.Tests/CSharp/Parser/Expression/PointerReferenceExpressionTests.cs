// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class PointerReferenceExpressionTests
	{
		[Test, Ignore("where did PointerReferenceExpression.MemberName go?")]
		public void PointerReferenceExpressionTest()
		{
			PointerReferenceExpression pre = ParseUtilCSharp.ParseExpression<PointerReferenceExpression>("myObj.field->b");
			Assert.IsTrue(pre.Target is MemberReferenceExpression);
			//Assert.AreEqual("b", pre.MemberName);
			throw new NotImplementedException();
		}
	}
}
