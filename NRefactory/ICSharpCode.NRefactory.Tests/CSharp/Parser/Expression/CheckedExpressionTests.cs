// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class CheckedExpressionTests
	{
		[Test]
		public void CheckedExpressionTest()
		{
			CheckedExpression ce = ParseUtilCSharp.ParseExpression<CheckedExpression>("checked(a)");
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
		
		[Test]
		public void UncheckedExpressionTest()
		{
			UncheckedExpression ce = ParseUtilCSharp.ParseExpression<UncheckedExpression>("unchecked(a)");
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
	}
}
