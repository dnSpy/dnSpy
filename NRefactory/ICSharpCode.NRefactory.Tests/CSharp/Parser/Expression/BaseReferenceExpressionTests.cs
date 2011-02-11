// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class BaseReferenceExpressionTests
	{
		[Test]
		public void BaseReferenceExpressionTest1()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("base.myField");
			Assert.IsTrue(fre.Target is BaseReferenceExpression);
		}
	}
}
