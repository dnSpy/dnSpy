// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class PointerReferenceExpressionTests
	{
		[Test]
		public void PointerReferenceExpressionTest()
		{
			PointerReferenceExpression pre = ParseUtilCSharp.ParseExpression<PointerReferenceExpression>("myObj.field->b");
			Assert.IsTrue(pre.Target is MemberReferenceExpression);
			Assert.AreEqual("b", pre.MemberName);
		}
		
		[Test]
		public void PointerReferenceGenericMethodTest()
		{
			ParseUtilCSharp.AssertExpression(
				"ptr->M<string>();",
				new InvocationExpression {
					Target = new PointerReferenceExpression {
						Target = new IdentifierExpression("ptr"),
						MemberName = "M",
						TypeArguments = { new PrimitiveType("string") }
					}});
		}
	}
}
