// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;

namespace ICSharpCode.NRefactory.VB.Tests.Lexer
{
	[TestFixture]
	public class CustomLexerTests
	{
		VBLexer GenerateLexer(StringReader sr)
		{
			return new VBLexer(sr);
		}
		
		[Test]
		public void TestSingleEOLForMulitpleLines()
		{
			VBLexer lexer = GenerateLexer(new StringReader("Stop\n\n\nEnd"));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Stop));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.End));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void TestSingleEOLForMulitpleLinesWithContinuation()
		{
			VBLexer lexer = GenerateLexer(new StringReader("Stop\n _\n\nEnd"));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Stop));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.End));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void EscapedIdentifier()
		{
			VBLexer lexer = GenerateLexer(new StringReader("[Stop]"));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void IdentifierWithTypeCharacter()
		{
			VBLexer lexer = GenerateLexer(new StringReader("Stop$"));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void ExclamationMarkIsTypeCharacter()
		{
			VBLexer lexer = GenerateLexer(new StringReader("a!=b"));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Assign));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void ExclamationMarkIsTypeCharacter2()
		{
			VBLexer lexer = GenerateLexer(new StringReader("a! b"));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void ExclamationMarkIsIdentifier()
		{
			VBLexer lexer = GenerateLexer(new StringReader("a!b"));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.ExclamationMark));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void ExclamationMarkIsIdentifier2()
		{
			VBLexer lexer = GenerateLexer(new StringReader("a![b]"));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.ExclamationMark));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void RemCommentTest()
		{
			VBLexer lexer = GenerateLexer(new StringReader("a rem b"));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.Identifier));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOL));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOF));
		}
		
		[Test]
		public void RemCommentTest2()
		{
			VBLexer lexer = GenerateLexer(new StringReader("REM c"));
			Assert.That(lexer.NextToken().Kind, Is.EqualTo(Tokens.EOF));
		}
	}
}
