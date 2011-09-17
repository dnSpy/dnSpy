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
