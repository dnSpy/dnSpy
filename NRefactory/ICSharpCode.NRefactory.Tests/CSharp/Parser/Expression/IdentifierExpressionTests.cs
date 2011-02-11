// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

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
		
		[Test, Ignore("Mono parser bug?")]
		public void TestIdentifierContainingEscapeSequence()
		{
			CheckIdentifier(@"l\U00000065xer", "lexer");
		}
		
		[Test]
		public void TestKeyWordAsIdentifier()
		{
			CheckIdentifier("@int", "int");
		}
		
		[Test, Ignore("Mono parser bug?")]
		public void TestKeywordWithEscapeSequenceIsIdentifier()
		{
			CheckIdentifier(@"i\u006et", "int");
		}
		
		[Test]
		public void TestKeyWordAsIdentifierStartingWithUnderscore()
		{
			CheckIdentifier("@_int", "_int");
		}
		
		[Test, Ignore]
		public void GenericMethodReference()
		{
			IdentifierExpression ident = ParseUtilCSharp.ParseExpression<IdentifierExpression>("M<int>");
			Assert.AreEqual("M", ident.Identifier);
			//Assert.AreEqual(1, ident.TypeArguments.Count);
			throw new NotImplementedException();
		}
		
		[Test, Ignore]
		public void GenericMethodReference2()
		{
			IdentifierExpression ident = ParseUtilCSharp.ParseExpression<IdentifierExpression>("TargetMethod<string>");
			Assert.AreEqual("TargetMethod", ident.Identifier);
			//Assert.AreEqual(1, ident.TypeArguments.Count);
			throw new NotImplementedException();
		}
	}
}
