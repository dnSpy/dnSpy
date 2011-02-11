// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore("Needs to be ported to new NRefactory")]
	public class ArrayObjectCreateExpressionTests
	{
		[Test]
		public void ArrayCreateExpressionTest1()
		{
			/*
			ArrayCreateExpression ace = ParseUtilCSharp.ParseExpression<ArrayCreateExpression>("new int[5]");
			Assert.AreEqual("System.Int32", ace.CreateType.Type);
			Assert.IsTrue(ace.CreateType.IsKeyword);
			Assert.AreEqual(1, ace.Arguments.Count);
			Assert.AreEqual(new int[] {0}, ace.CreateType.RankSpecifier);
			*/
			throw new NotImplementedException();
		}
		
		[Test]
		public void ImplicitlyTypedArrayCreateExpression()
		{
			/*
			ArrayCreateExpression ace = ParseUtilCSharp.ParseExpression<ArrayCreateExpression>("new[] { 1, 10, 100, 1000 }");
			Assert.AreEqual("", ace.CreateType.Type);
			Assert.AreEqual(0, ace.Arguments.Count);
			Assert.AreEqual(4, ace.ArrayInitializer.CreateExpressions.Count);*/
			throw new NotImplementedException();
		}
	}
}
