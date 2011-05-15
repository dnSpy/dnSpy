// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using ICSharpCode.NRefactory.VB.Parser;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Lexer
{
	[TestFixture]
	public sealed class LiteralsTests
	{
		VBLexer GenerateLexer(StringReader sr)
		{
			return new VBLexer(sr);
		}
		
		Token GetSingleToken(string text)
		{
			VBLexer lexer = GenerateLexer(new StringReader(text));
			Token t = lexer.NextToken();
			Assert.AreEqual(Tokens.EOL, lexer.NextToken().Kind, "Tokens.EOL");
			Assert.AreEqual(Tokens.EOF, lexer.NextToken().Kind, "Tokens.EOF");
			Assert.AreEqual("", lexer.Errors.ErrorOutput);
			return t;
		}
		
		void CheckToken(string text, int tokenType, object val)
		{
			Token t = GetSingleToken(text);
			Assert.AreEqual(tokenType, t.Kind, "Tokens.Literal");
			Assert.IsNotNull(t.LiteralValue, "literalValue is null");
			Assert.AreEqual(val.GetType(), t.LiteralValue.GetType(), "literalValue.GetType()");
			Assert.AreEqual(val, t.LiteralValue, "literalValue");
		}
		
		[Test]
		public void TestSingleDigit()
		{
			CheckToken("5", Tokens.LiteralInteger, 5);
		}
		
		[Test]
		public void TestZero()
		{
			CheckToken("0", Tokens.LiteralInteger, 0);
		}
		
		[Test]
		public void TestInteger()
		{
			CheckToken("15", Tokens.LiteralInteger, 15);
			CheckToken("8581", Tokens.LiteralInteger, 8581);
		}
		
		[Test]
		public void InvalidTypeCharacter()
		{
			// just check that we don't get exceptions:
			GenerateLexer(new StringReader(".5s")).NextToken();
			GenerateLexer(new StringReader(".5ul")).NextToken();
		}
		
		[Test]
		public void TestHexadecimalInteger()
		{
			CheckToken("&H10", Tokens.LiteralInteger, 0x10);
			CheckToken("&H10&", Tokens.LiteralInteger, (long)0x10);
			CheckToken("&h3ff%", Tokens.LiteralInteger, 0x3ff);
			CheckToken("&h8000s", Tokens.LiteralInteger, short.MinValue);
			CheckToken("&h8000us", Tokens.LiteralInteger, (ushort)0x8000);
			CheckToken("&HffffFFFF", Tokens.LiteralInteger, -1);
			CheckToken("&HffffFFFF%", Tokens.LiteralInteger, -1);
			CheckToken("&HffffFFFFui", Tokens.LiteralInteger, uint.MaxValue);
			CheckToken("&HffffFFFF&", Tokens.LiteralInteger, (long)uint.MaxValue);
		}
		
		[Test]
		public void TestLongHexadecimalInteger()
		{
			CheckToken("&H4244636f446c6d58", Tokens.LiteralInteger, 0x4244636f446c6d58);
			CheckToken("&hf244636f446c6d58", Tokens.LiteralInteger, -989556688574190248);
			CheckToken("&hf244636f446c6d58&", Tokens.LiteralInteger, -989556688574190248);
			CheckToken("&hf244636f446c6d58ul", Tokens.LiteralInteger, 0xf244636f446c6d58);
		}
		
		[Test]
		public void InvalidHexadecimalInteger()
		{
			// just check that we don't get exceptions:
			GenerateLexer(new StringReader("&H")).NextToken();
			// >ulong.MaxValue
			GenerateLexer(new StringReader("&hff244636f446c6d58")).NextToken();
			// needs an ulong, but "i" postfix specified integer
			GenerateLexer(new StringReader("&hf244636f446c6d58i")).NextToken();
			GenerateLexer(new StringReader("&hf244636f446c6d58ui")).NextToken();
		}
		
		[Test]
		public void TestIncompleteHexadecimal()
		{
			VBLexer lexer = GenerateLexer(new StringReader("&H\r\nabc"));
			Token t = lexer.NextToken();
			Assert.AreEqual(Tokens.LiteralInteger, t.Kind);
			Assert.AreEqual(0, (int)t.LiteralValue);
			Assert.AreEqual(Tokens.EOL, lexer.NextToken().Kind, "Tokens.EOL (1)");
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind, "Tokens.Identifier");
			Assert.AreEqual(Tokens.EOL, lexer.NextToken().Kind, "Tokens.EOL (2)");
			Assert.AreEqual(Tokens.EOF, lexer.NextToken().Kind, "Tokens.EOF");
			Assert.AreNotEqual("", lexer.Errors.ErrorOutput);
		}
		
		[Test]
		public void TestStringLiterals()
		{
			CheckToken("\"\"", Tokens.LiteralString, "");
			CheckToken("\"Hello, World!\"", Tokens.LiteralString, "Hello, World!");
			CheckToken("\"\"\"\"", Tokens.LiteralString, "\"");
		}
		
		[Test]
		public void TestCharacterLiterals()
		{
			CheckToken("\" \"c", Tokens.LiteralCharacter, ' ');
			CheckToken("\"!\"c", Tokens.LiteralCharacter, '!');
			CheckToken("\"\"\"\"c", Tokens.LiteralCharacter, '"');
		}
		
		[Test]
		public void TestDateLiterals()
		{
			CheckToken("# 8/23/1970 #", Tokens.LiteralDate, new DateTime(1970, 8, 23, 0, 0, 0));
			CheckToken("#8/23/1970#", Tokens.LiteralDate, new DateTime(1970, 8, 23, 0, 0, 0));
			CheckToken("# 8/23/1970  3:45:39AM #", Tokens.LiteralDate, new DateTime(1970, 8, 23, 3, 45, 39));
			CheckToken("# 3:45:39AM #", Tokens.LiteralDate, new DateTime(1, 1, 1, 3, 45, 39));
			CheckToken("# 3:45:39  PM #", Tokens.LiteralDate, new DateTime(1, 1, 1, 15, 45, 39));
			CheckToken("# 3:45:39 #", Tokens.LiteralDate, new DateTime(1, 1, 1, 3, 45, 39));
			CheckToken("# 13:45:39 #", Tokens.LiteralDate, new DateTime(1, 1, 1, 13, 45, 39));
			CheckToken("# 1AM #", Tokens.LiteralDate, new DateTime(1, 1, 1, 1, 0, 0));
		}
		
		[Test]
		public void TestDouble()
		{
			CheckToken("1.0", Tokens.LiteralDouble, 1.0);
			CheckToken("1.1", Tokens.LiteralDouble, 1.1);
			CheckToken("2e-5", Tokens.LiteralDouble, 2e-5);
			CheckToken("2.0e-5", Tokens.LiteralDouble, 2e-5);
			CheckToken("2e5", Tokens.LiteralDouble, 2e5);
			CheckToken("2.2e5", Tokens.LiteralDouble, 2.2e5);
			CheckToken("2e+5", Tokens.LiteralDouble, 2e5);
			CheckToken("2.2e+5", Tokens.LiteralDouble, 2.2e5);
			
			CheckToken("1r", Tokens.LiteralDouble, 1.0);
			CheckToken("1.0r", Tokens.LiteralDouble, 1.0);
			CheckToken("1.1r", Tokens.LiteralDouble, 1.1);
			CheckToken("2e-5r", Tokens.LiteralDouble, 2e-5);
			CheckToken("2.0e-5r", Tokens.LiteralDouble, 2e-5);
			CheckToken("2e5r", Tokens.LiteralDouble, 2e5);
			CheckToken("2.2e5r", Tokens.LiteralDouble, 2.2e5);
			CheckToken("2e+5r", Tokens.LiteralDouble, 2e5);
			CheckToken("2.2e+5r", Tokens.LiteralDouble, 2.2e5);
		}
		
		[Test]
		public void TestSingle()
		{
			CheckToken("1f", Tokens.LiteralSingle, 1.0f);
			CheckToken("1.0f", Tokens.LiteralSingle, 1.0f);
			CheckToken("1.1f", Tokens.LiteralSingle, 1.1f);
			CheckToken("2e-5f", Tokens.LiteralSingle, 2e-5f);
			CheckToken("2.0e-5f", Tokens.LiteralSingle, 2e-5f);
			CheckToken("2e5f", Tokens.LiteralSingle, 2e5f);
			CheckToken("2.2e5f", Tokens.LiteralSingle, 2.2e5f);
			CheckToken("2e+5f", Tokens.LiteralSingle, 2e5f);
			CheckToken("2.2e+5f", Tokens.LiteralSingle, 2.2e5f);
		}
		
		[Test]
		public void TestDecimal()
		{
			CheckToken("1d", Tokens.LiteralDecimal, 1m);
			CheckToken("1.0d", Tokens.LiteralDecimal, 1.0m);
			CheckToken("1.1d", Tokens.LiteralDecimal, 1.1m);
			CheckToken("2e-5d", Tokens.LiteralDecimal, 2e-5m);
			CheckToken("2.0e-5d", Tokens.LiteralDecimal, 2.0e-5m);
			CheckToken("2e5d", Tokens.LiteralDecimal, 2e5m);
			CheckToken("2.2e5d", Tokens.LiteralDecimal, 2.2e5m);
			CheckToken("2e+5d", Tokens.LiteralDecimal, 2e5m);
			CheckToken("2.2e+5d", Tokens.LiteralDecimal, 2.2e5m);
		}
	}
}
