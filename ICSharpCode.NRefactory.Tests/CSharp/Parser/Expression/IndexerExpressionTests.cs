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
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class IndexerExpressionTests
	{
		[Test]
		public void IndexerExpressionTest()
		{
			IndexerExpression ie = ParseUtilCSharp.ParseExpression<IndexerExpression>("field[1, \"Hello\", 'a']");
			Assert.IsTrue(ie.Target is IdentifierExpression);
			
			Assert.AreEqual(3, ie.Arguments.Count());
			
			Assert.IsTrue(ie.Arguments.ElementAt(0) is PrimitiveExpression);
			Assert.AreEqual(1, (int)((PrimitiveExpression)ie.Arguments.ElementAt(0)).Value);
			Assert.IsTrue(ie.Arguments.ElementAt(1) is PrimitiveExpression);
			Assert.AreEqual("Hello", (string)((PrimitiveExpression)ie.Arguments.ElementAt(1)).Value);
			Assert.IsTrue(ie.Arguments.ElementAt(2) is PrimitiveExpression);
			Assert.AreEqual('a', (char)((PrimitiveExpression)ie.Arguments.ElementAt(2)).Value);
		}
	}
}
