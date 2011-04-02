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
	public class ArrayCreateExpressionTests
	{
		[Test]
		public void ArrayCreateExpressionTest1()
		{
			ArrayCreateExpression ace = ParseUtil.ParseExpression<ArrayCreateExpression>("new Integer() {1, 2, 3, 4}");
			
			Assert.AreEqual("System.Int32", ace.CreateType.Type);
			Assert.AreEqual(0, ace.Arguments.Count);
			Assert.AreEqual(new int[] {0}, ace.CreateType.RankSpecifier);
		}
		
		[Test]
		public void ArrayCreateExpressionTest2()
		{
			ArrayCreateExpression ace = ParseUtil.ParseExpression<ArrayCreateExpression>("New Integer(0 To 5){0, 1, 2, 3, 4, 5}");
			
			Assert.AreEqual("System.Int32", ace.CreateType.Type);
			Assert.AreEqual(1, ace.Arguments.Count);
			Assert.AreEqual(5, (ace.Arguments[0] as PrimitiveExpression).Value);
			Assert.AreEqual(new int[] {0}, ace.CreateType.RankSpecifier);
		}
	}
}
