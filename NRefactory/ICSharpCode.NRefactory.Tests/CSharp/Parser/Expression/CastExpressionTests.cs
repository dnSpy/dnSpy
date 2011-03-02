// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore("Port unit tests to new DOM")]
	public class CastExpressionTests
	{
		[Test]
		public void SimpleCastExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"(MyObject)o",
				new CastExpression {
					Type = new SimpleType("MyObject"),
					Expression = new IdentifierExpression("o")
				});
		}
		
		[Test]
		public void ArrayCastExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"(MyType[])o",
				new CastExpression {
					Type = new SimpleType("MyType").MakeArrayType(1),
					Expression = new IdentifierExpression("o")
				});
		}
		
		[Test]
		public void NullablePrimitiveCastExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"(int?)o",
				new CastExpression {
					Type = new ComposedType { BaseType = new PrimitiveType("int"), HasNullableSpecifier = true },
					Expression = new IdentifierExpression("o")
				});
		}
		
		[Test]
		public void NullableCastExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"(MyType?)o",
				new CastExpression {
					Type = new ComposedType { BaseType = new SimpleType("MyType"), HasNullableSpecifier = true },
					Expression = new IdentifierExpression("o")
				});
		}
		
		[Test]
		public void NullableTryCastExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"o as int?",
				new AsExpression {
					Type = new ComposedType { BaseType = new PrimitiveType("int"), HasNullableSpecifier = true },
					Expression = new IdentifierExpression("o")
				});
		}
		
		[Test]
		public void GenericCastExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"(List<string>)o",
				new CastExpression {
					Type = new SimpleType("List") { TypeArguments = { new PrimitiveType("string") } },
					Expression = new IdentifierExpression("o")
				});
		}
		
		[Test]
		public void GenericArrayCastExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"(List<string>[])o",
				new CastExpression {
					Type = new ComposedType {
						BaseType = new SimpleType("List") { TypeArguments = { new PrimitiveType("string") } },
						ArraySpecifiers = { new ArraySpecifier(1) }
					},
					Expression = new IdentifierExpression("o")
				});
		}
		
		[Test]
		public void GenericArrayAsCastExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"o as List<string>[]",
				new AsExpression {
					Type = new ComposedType {
						BaseType = new SimpleType("List") { TypeArguments = { new PrimitiveType("string") } },
						ArraySpecifiers = { new ArraySpecifier(1) }
					},
					Expression = new IdentifierExpression("o")
				});
		}
		
		[Test]
		public void CastMemberReferenceOnParenthesizedExpression()
		{
			// yes, we really want to evaluate .Member on expr and THEN cast the result to MyType
			ParseUtilCSharp.AssertExpression(
				"(MyType)(expr).Member",
				new CastExpression {
					Type = new SimpleType("MyType"),
					Expression = new ParenthesizedExpression { Expression = new IdentifierExpression("expr") }.Member("Member")
				});
		}
		
		[Test]
		public void TryCastParenthesizedExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"(o) as string",
				new AsExpression {
					Expression = new ParenthesizedExpression { Expression = new IdentifierExpression("o") },
					Type = new PrimitiveType("string")
				});
		}
		
		[Test]
		public void CastNegation()
		{
			ParseUtilCSharp.AssertExpression(
				"(uint)-negativeValue",
				new CastExpression {
					Type = new PrimitiveType("uint"),
					Expression = new UnaryOperatorExpression(
						UnaryOperatorType.Minus,
						new IdentifierExpression("negativeValue")
					)});
		}
		
		[Test]
		public void SubtractionIsNotCast()
		{
			ParseUtilCSharp.AssertExpression(
				"(BigInt)-negativeValue",
				new BinaryOperatorExpression {
					Left = new ParenthesizedExpression { Expression = new IdentifierExpression("BigInt") },
					Operator = BinaryOperatorType.Subtract,
					Right = new IdentifierExpression("negativeValue")
				});
		}
		
		[Test]
		public void IntMaxValueToBigInt()
		{
			ParseUtilCSharp.AssertExpression(
				"(BigInt)int.MaxValue",
				new CastExpression {
					Type = new SimpleType("BigInt"),
					Expression = new PrimitiveExpression("int").Member("MaxValue")
				});
		}
	}
}
