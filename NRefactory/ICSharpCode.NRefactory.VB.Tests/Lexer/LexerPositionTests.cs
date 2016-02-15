// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using ICSharpCode.NRefactory.VB.Parser;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Lexer
{
	[TestFixture]
	public class LexerPositionTests
	{
		VBLexer GenerateLexer(string s)
		{
			return new VBLexer(new StringReader(s));
		}
		
		[Test]
		public void TestNewLine()
		{
			VBLexer l = GenerateLexer("public\nstatic");
			Token t = l.NextToken();
			Assert.AreEqual(Tokens.Public, t.Kind);
			Assert.AreEqual(new TextLocation(1, 1), t.Location);
			Assert.AreEqual(new TextLocation(1, 7), t.EndLocation);
			t = l.NextToken();
			Assert.AreEqual(Tokens.EOL, t.Kind);
			Assert.AreEqual(new TextLocation(1, 7), t.Location);
			Assert.AreEqual(new TextLocation(2, 1), t.EndLocation);
			t = l.NextToken();
			Assert.AreEqual(Tokens.Static, t.Kind);
			Assert.AreEqual(new TextLocation(2, 1), t.Location);
			Assert.AreEqual(new TextLocation(2, 7), t.EndLocation);
		}
		
		[Test]
		public void TestCarriageReturnNewLine()
		{
			VBLexer l = GenerateLexer("public\r\nstatic");
			Token t = l.NextToken();
			Assert.AreEqual(Tokens.Public, t.Kind);
			Assert.AreEqual(new TextLocation(1, 1), t.Location);
			Assert.AreEqual(new TextLocation(1, 7), t.EndLocation);
			t = l.NextToken();
			Assert.AreEqual(Tokens.EOL, t.Kind);
			Assert.AreEqual(new TextLocation(1, 7), t.Location);
			Assert.AreEqual(new TextLocation(2, 1), t.EndLocation);
			t = l.NextToken();
			Assert.AreEqual(Tokens.Static, t.Kind);
			Assert.AreEqual(new TextLocation(2, 1), t.Location);
			Assert.AreEqual(new TextLocation(2, 7), t.EndLocation);
		}
		
		[Test]
		public void TestPositionOfEOF1()
		{
			VBLexer l = GenerateLexer("public");
			l.NextToken(); // public
			Token t = l.NextToken();
			Assert.AreEqual(Tokens.EOL, t.Kind);
			Assert.AreEqual(new TextLocation(1, 7), t.Location);
			Assert.AreEqual(new TextLocation(1, 7), t.EndLocation);
			
			t = l.NextToken();
			Assert.AreEqual(Tokens.EOF, t.Kind);
			Assert.AreEqual(new TextLocation(1, 7), t.Location);
			Assert.AreEqual(new TextLocation(1, 7), t.EndLocation);
		}
		
		[Test]
		public void TestPositionOfEOF2()
		{
			VBLexer l = GenerateLexer("public _\n ");
			l.NextToken(); // public
			Token t = l.NextToken();
			Assert.AreEqual(Tokens.EOL, t.Kind);
			Assert.AreEqual(new TextLocation(2, 2), t.Location);
			Assert.AreEqual(new TextLocation(2, 2), t.EndLocation);
			
			t = l.NextToken();
			Assert.AreEqual(Tokens.EOF, t.Kind);
			Assert.AreEqual(new TextLocation(2, 2), t.Location);
			Assert.AreEqual(new TextLocation(2, 2), t.EndLocation);
		}
	}
}
