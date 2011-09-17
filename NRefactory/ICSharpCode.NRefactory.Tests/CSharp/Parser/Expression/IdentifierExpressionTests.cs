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
using NUnit.Framework;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class IdentifierExpressionTests
	{
		void CheckIdentifier(string sourceCode, string identifier)
		{
			IdentifierExpression ident = ParseUtilCSharp.ParseExpression<IdentifierExpression>(sourceCode);
			Assert.AreEqual(identifier, ident.Identifier);
		}
		
		[Test]
		public void TestIdentifier()
		{
			CheckIdentifier("a_Bc05", "a_Bc05");
		}
		
		[Test]
		public void TestIdentifierStartingWithUnderscore()
		{
			CheckIdentifier("_Bc05", "_Bc05");
		}
		
		[Test]
		public void TestIdentifierStartingWithEscapeSequence()
		{
			CheckIdentifier(@"\u006cexer", "lexer");
		}
		
		[Test]
		public void TestIdentifierContainingEscapeSequence()
		{
			CheckIdentifier(@"l\U00000065xer", "lexer");
		}
		
		[Test, Ignore("The @ should not be part of IdentifierExpression.Identifier")]
		public void TestKeyWordAsIdentifier()
		{
			CheckIdentifier("@int", "int");
		}
		
		[Test, Ignore("Mono parser bug?")]
		public void TestKeywordWithEscapeSequenceIsIdentifier()
		{
			CheckIdentifier(@"i\u006et", "int");
		}
		
		[Test, Ignore("The @ should not be part of IdentifierExpression.Identifier")]
		public void TestKeyWordAsIdentifierStartingWithUnderscore()
		{
			CheckIdentifier("@_int", "_int");
		}
		
		[Test]
		public void GenericMethodReference()
		{
			IdentifierExpression ident = ParseUtilCSharp.ParseExpression<IdentifierExpression>("M<int>");
			Assert.IsTrue(
				new IdentifierExpression {
					Identifier = "M" ,
					TypeArguments = {
						new PrimitiveType("int")
					}
				}.IsMatch(ident));
		}
		
		[Test]
		public void GenericMethodReference2()
		{
			IdentifierExpression ident = ParseUtilCSharp.ParseExpression<IdentifierExpression>("TargetMethod<string>");
			Assert.IsTrue(
				new IdentifierExpression {
					Identifier = "TargetMethod" ,
					TypeArguments = {
						new PrimitiveType("string")
					}
				}.IsMatch(ident));
		}
	}
}
