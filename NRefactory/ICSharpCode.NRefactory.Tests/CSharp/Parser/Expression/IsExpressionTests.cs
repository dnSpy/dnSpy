// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class IsExpressionTests
	{
		[Test]
		public void GenericArrayIsExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"o is List<string>[]",
				new IsExpression {
					Expression = new IdentifierExpression("o"),
					Type = new SimpleType("List") { TypeArguments = { new PrimitiveType("string") } }.MakeArrayType(1)
				}
			);
		}
		
		[Test]
		public void NullableIsExpression()
		{
			IsExpression ce = ParseUtilCSharp.ParseExpression<IsExpression>("o is int?");
			ComposedType type = (ComposedType)ce.Type;
			Assert.IsTrue(type.HasNullableSpecifier);
			Assert.AreEqual("int", ((PrimitiveType)type.BaseType).Keyword);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
		
		[Test]
		public void NullableIsExpressionInBinaryOperatorExpression()
		{
			BinaryOperatorExpression boe;
			boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("o is int? == true");
			IsExpression ce = (IsExpression)boe.Left;
			ComposedType type = (ComposedType)ce.Type;
			Assert.IsTrue(type.HasNullableSpecifier);
			Assert.AreEqual("int", ((PrimitiveType)type.BaseType).Keyword);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
	}
}
