// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class AnonymousMethodExpressionTests
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
			Assert.IsFalse(ame.IsAsync);
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
		
		[Test, Ignore("async/await not yet supported")]
		public void AsyncAnonymousMethod()
		{
			AnonymousMethodExpression ame = Parse("async delegate {}");
			Assert.AreEqual(0, ame.Parameters.Count());
			Assert.AreEqual(0, ame.Body.Statements.Count());
			Assert.IsTrue(ame.IsAsync);
			Assert.IsFalse(ame.HasParameterList);
		}
	}
}
