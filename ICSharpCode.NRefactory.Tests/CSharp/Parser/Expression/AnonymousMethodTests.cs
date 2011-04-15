// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class AnonymousMethodTests
	{
		AnonymousMethodExpression Parse(string expression)
		{
			return ParseUtilCSharp.ParseExpression<AnonymousMethodExpression>(expression);
		}
		
		[Test]
		public void AnonymousMethodWithoutParameterList()
		{
			AnonymousMethodExpression ame = Parse("delegate {}");
			Assert.AreEqual(0, ame.Parameters.Count());
			Assert.AreEqual(0, ame.Body.Statements.Count());
			Assert.IsFalse(ame.HasParameterList);
		}
		
		[Test]
		public void AnonymousMethodAfterCast()
		{
			CastExpression c = ParseUtilCSharp.ParseExpression<CastExpression>("(ThreadStart)delegate {}");
			AnonymousMethodExpression ame = (AnonymousMethodExpression)c.Expression;
			Assert.AreEqual(0, ame.Parameters.Count());
			Assert.AreEqual(0, ame.Body.Statements.Count());
		}
		
		[Test]
		public void EmptyAnonymousMethod()
		{
			AnonymousMethodExpression ame = Parse("delegate() {}");
			Assert.AreEqual(0, ame.Parameters.Count());
			Assert.AreEqual(0, ame.Body.Statements.Count());
			Assert.IsTrue(ame.HasParameterList);
		}
		
		[Test]
		public void SimpleAnonymousMethod()
		{
			AnonymousMethodExpression ame = Parse("delegate(int a, int b) { return a + b; }");
			Assert.IsTrue(ame.HasParameterList);
			Assert.AreEqual(2, ame.Parameters.Count());
			Assert.AreEqual(1, ame.Body.Statements.Count());
			Assert.IsTrue(ame.Body.Statements.First() is ReturnStatement);
		}
	}
}
