// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class StackAllocExpressionTests
	{
		[Test, Ignore]
		public void StackAllocExpressionTest()
		{
			var sae = ParseUtilCSharp.ParseExpression<StackAllocExpression>("stackalloc int[100]");
			throw new NotImplementedException(); // TODO: verify type + length expression
		}
	}
}
