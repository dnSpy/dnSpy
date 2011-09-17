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
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp
{
	[TestFixture]
	public class CSharpOutputVisitorTests
	{
		void AssertOutput(string expected, Expression expr, CSharpFormattingOptions policy = null)
		{
			if (policy == null)
				policy = new CSharpFormattingOptions();;
			StringWriter w = new StringWriter();
			w.NewLine = "\n";
			expr.AcceptVisitor(new CSharpOutputVisitor(new TextWriterOutputFormatter(w) { IndentationString = "\t" }, policy), null);
			Assert.AreEqual(expected.Replace("\r", ""), w.ToString());
		}
		
		[Test, Ignore("Incorrect whitespace")]
		public void AssignmentInCollectionInitialize()
		{
			Expression expr = new ObjectCreateExpression {
				Type = new SimpleType("List"),
				Initializer = new ArrayInitializerExpression(
					new ArrayInitializerExpression(
						new AssignmentExpression(new IdentifierExpression("a"), new PrimitiveExpression(1))
					)
				)
			};
			
			AssertOutput("new List {\n  {\n    a = 1\n  }\n}", expr);
		}
	}
}
