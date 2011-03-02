// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class ArrayObjectCreateExpressionTests
	{
		[Test]
		public void ArrayCreateExpressionTest1()
		{
			ParseUtilCSharp.AssertExpression(
				"new int[5]",
				new ArrayCreateExpression {
					Type = new PrimitiveType("int"),
					Arguments = { new PrimitiveExpression(5) }
				});
		}
		
		[Test, Ignore("AdditionalArraySpecifiers not yet implemented")]
		public void MultidimensionalNestedArray()
		{
			ParseUtilCSharp.AssertExpression(
				"new int[5,2][,,][]",
				new ArrayCreateExpression {
					Type = new PrimitiveType("int"),
					Arguments = { new PrimitiveExpression(5), new PrimitiveExpression(2) },
					AdditionalArraySpecifiers = {
						new ArraySpecifier(3),
						new ArraySpecifier(1)
					}
				});
		}
		
		[Test, Ignore("Array initializers not yet implemented")]
		public void ImplicitlyTypedArrayCreateExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"new[] { 1, 10, 100, 1000 }",
				new ArrayCreateExpression {
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new PrimitiveExpression(1),
							new PrimitiveExpression(10),
							new PrimitiveExpression(100),
							new PrimitiveExpression(1000)
						}
					}
				});
			
		}
	}
}
