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
	public class PrimitiveExpressionTests
	{
		[Test]
		public void HexIntegerTest1()
		{
			InvocationExpression invExpr = ParseUtilCSharp.ParseExpression<InvocationExpression>("0xAFFE.ToString()");
			Assert.AreEqual(0, invExpr.Arguments.Count());
			Assert.IsTrue(invExpr.Target is MemberReferenceExpression);
			MemberReferenceExpression fre = invExpr.Target as MemberReferenceExpression;
			Assert.AreEqual("ToString", fre.MemberName);
			
			Assert.IsTrue(fre.Target is PrimitiveExpression);
			PrimitiveExpression pe = fre.Target as PrimitiveExpression;
			
			Assert.AreEqual(0xAFFE, (int)pe.Value);
			
		}
		
		void CheckLiteral(string code, object value)
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>(code);
			Assert.AreEqual(value.GetType(), pe.Value.GetType());
			Assert.AreEqual(value, pe.Value);
			Assert.AreEqual(code, pe.LiteralValue);
		}
		
		[Test]
		public void DoubleWithLeadingDot()
		{
			CheckLiteral(".5e-06", .5e-06);
		}
		
		[Test]
		public void FloatWithLeadingDot()
		{
			CheckLiteral(".5e-06f", .5e-06f);
		}
		
		[Test]
		public void CharTest1()
		{
			CheckLiteral("'\\u0356'", '\u0356');
		}
		
		[Test]
		public void IntMinValueTest()
		{
			ParseUtilCSharp.AssertExpression(
				"-2147483648",
				new UnaryOperatorExpression(UnaryOperatorType.Minus, new PrimitiveExpression(-2147483648)));
		}
		
		[Test]
		public void IntMaxValueTest()
		{
			CheckLiteral("2147483647", 2147483647); // int
			CheckLiteral("2147483648", 2147483648); // uint
		}
		
		[Test]
		public void LongMinValueTest()
		{
			ParseUtilCSharp.AssertExpression(
				"-9223372036854775808",
				new UnaryOperatorExpression(UnaryOperatorType.Minus, new PrimitiveExpression(9223372036854775808)));
		}
		
		[Test]
		public void LongMaxValueTest()
		{
			CheckLiteral("9223372036854775807", 9223372036854775807); // long
			CheckLiteral("9223372036854775808", 9223372036854775808); // ulong
		}
		
		[Test]
		public void StringTest1()
		{
			CheckLiteral("\"\\n\\t\\u0005 Hello World !!!\"", "\n\t\u0005 Hello World !!!");
		}
		
		[Test]
		public void TestSingleDigit()
		{
			CheckLiteral("5", 5);
		}
		
		[Test]
		public void TestZero()
		{
			CheckLiteral("0", 0);
		}
		
		[Test]
		public void TestInteger()
		{
			CheckLiteral("66", 66);
		}
		
		[Test]
		public void TestNonOctalInteger()
		{
			// C# does not have octal integers, so 077 should parse to 77
			Assert.IsTrue(077 == 77);
			
			CheckLiteral("077", 077);
			CheckLiteral("056", 056);
		}
		
		[Test]
		public void TestHexadecimalInteger()
		{
			CheckLiteral("0x99F", 0x99F);
			CheckLiteral("0xAB1f", 0xAB1f);
			CheckLiteral("0xffffffff", 0xffffffff);
			CheckLiteral("0xffffffffL", 0xffffffffL);
			CheckLiteral("0xffffffffuL", 0xffffffffuL);
		}
		
		[Test]
		public void InvalidHexadecimalInteger()
		{
			// don't check result, just make sure there is no exception
			ParseUtilCSharp.ParseExpression<PrimitiveExpression>("0x2GF", expectErrors: true);
			ParseUtilCSharp.ParseExpression<PrimitiveExpression>("0xG2F", expectErrors: true);
			ParseUtilCSharp.ParseExpression<PrimitiveExpression>("0x", expectErrors: true); // SD-457
			// hexadecimal integer >ulong.MaxValue
			ParseUtilCSharp.ParseExpression<PrimitiveExpression>("0xfedcba98765432100", expectErrors: true);
		}
		
		[Test]
		public void TestLongHexadecimalInteger()
		{
			CheckLiteral("0x4244636f446c6d58", 0x4244636f446c6d58);
			CheckLiteral("0xf244636f446c6d58", 0xf244636f446c6d58);
		}
		
		[Test]
		public void TestLongInteger()
		{
			CheckLiteral("9223372036854775807", 9223372036854775807); // long.MaxValue
			CheckLiteral("9223372036854775808", 9223372036854775808); // long.MaxValue+1
			CheckLiteral("18446744073709551615", 18446744073709551615); // ulong.MaxValue
		}
		
		[Test]
		public void TestTooLongInteger()
		{
			// ulong.MaxValue+1
			ParseUtilCSharp.ParseExpression<PrimitiveExpression>("18446744073709551616", expectErrors: true);
			
			CheckLiteral("18446744073709551616f", 18446744073709551616f); // ulong.MaxValue+1 as float
			CheckLiteral("18446744073709551616d", 18446744073709551616d); // ulong.MaxValue+1 as double
			CheckLiteral("18446744073709551616m", 18446744073709551616m); // ulong.MaxValue+1 as decimal
		}
		
		[Test]
		public void TestDouble()
		{
			CheckLiteral("1.0", 1.0);
			CheckLiteral("1.1", 1.1);
			CheckLiteral("1.1e-2", 1.1e-2);
		}
		
		[Test]
		public void TestFloat()
		{
			CheckLiteral("1f", 1f);
			CheckLiteral("1.0f", 1.0f);
			CheckLiteral("1.1f", 1.1f);
			CheckLiteral("1.1e-2f", 1.1e-2f);
		}
		
		[Test]
		public void TestDecimal()
		{
			CheckLiteral("1m", 1m);
			CheckLiteral("1.0m", 1.0m);
			CheckLiteral("1.1m", 1.1m);
			CheckLiteral("1.1e-2m", 1.1e-2m);
			CheckLiteral("2.0e-5m", 2.0e-5m);
		}
		
		[Test]
		public void TestString()
		{
			CheckLiteral(@"@""-->""""<--""", @"-->""<--");
			CheckLiteral(@"""-->\""<--""", "-->\"<--");
			
			CheckLiteral(@"""\U00000041""", "\U00000041");
			CheckLiteral(@"""\U00010041""", "\U00010041");
		}
		
		[Test]
		public void TestCharLiteral()
		{
			CheckLiteral(@"'a'", 'a');
			CheckLiteral(@"'\u0041'", '\u0041');
			CheckLiteral(@"'\x41'", '\x41');
			CheckLiteral(@"'\x041'", '\x041');
			CheckLiteral(@"'\x0041'", '\x0041');
			CheckLiteral(@"'\U00000041'", '\U00000041');
		}
		
		[Test]
		public void TestPositionOfIntegerAtEndOfLine()
		{
			var pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("0\r\n");
			Assert.AreEqual(new TextLocation(1, 1), pe.StartLocation);
			Assert.AreEqual(new TextLocation(1, 2), pe.EndLocation);
			Assert.AreEqual("0", pe.LiteralValue);
		}
	}
}
