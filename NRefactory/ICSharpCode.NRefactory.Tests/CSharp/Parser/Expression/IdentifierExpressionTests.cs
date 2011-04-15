// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
