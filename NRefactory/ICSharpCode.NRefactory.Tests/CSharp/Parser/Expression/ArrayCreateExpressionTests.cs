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
	public class ArrayCreateExpressionTests
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
		
		[Test]
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
		
		[Test]
		public void ArrayWithImplicitSize()
		{
			ParseUtilCSharp.AssertExpression(
				"new int[] { 1 }",
				new ArrayCreateExpression {
					Type = new PrimitiveType("int"),
					AdditionalArraySpecifiers = {
						new ArraySpecifier(0)
					},
					Initializer = new ArrayInitializerExpression {
						Elements = { new PrimitiveExpression(1) }
					}
				});
		}
		
		[Test]
		public void ArrayWithImplicitSize2D()
		{
			ParseUtilCSharp.AssertExpression(
				"new int[,] { { 1 } }",
				new ArrayCreateExpression {
					Type = new PrimitiveType("int"),
					AdditionalArraySpecifiers = { new ArraySpecifier (2) },
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new ArrayInitializerExpression {
								Elements = { new PrimitiveExpression(1) }
							}
						}
					}
				});
		}
		
		[Test]
		public void ImplicitlyTypedArrayCreateExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"new[] { 1, 10, 100, 1000 }",
				new ArrayCreateExpression {
					AdditionalArraySpecifiers = {
						new ArraySpecifier(0)
					},
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new PrimitiveExpression(1),
							new PrimitiveExpression(10),
							new PrimitiveExpression(100),
							new PrimitiveExpression(1000)
						}
					}});
		}
		
		[Test]
		public void ImplicitlyTypedArrayCreateExpression2D()
		{
			ParseUtilCSharp.AssertExpression(
				"new [,] { { 1, 10 }, { 100, 1000 } }",
				new ArrayCreateExpression {
					AdditionalArraySpecifiers = {
						new ArraySpecifier(2),
					},
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new ArrayInitializerExpression {
								Elements = {
									new PrimitiveExpression(1),
									new PrimitiveExpression(10)
								}
							},
							new ArrayInitializerExpression {
								Elements = {
									new PrimitiveExpression(100),
									new PrimitiveExpression(1000)
								}
							}
						}
					}});
		}
		
		[Test]
		public void AssignmentInArrayInitializer()
		{
			ParseUtilCSharp.AssertExpression(
				"new [] { a = 10 }",
				new ArrayCreateExpression {
					AdditionalArraySpecifiers = {
						new ArraySpecifier(0)
					},
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new AssignmentExpression(new IdentifierExpression("a"), new PrimitiveExpression(10))
						}
					}});
		}
		
		[Test, Ignore("Parser bug")]
		public void ArrayInitializerWithCommaAtEnd()
		{
			var ace = ParseUtilCSharp.ParseExpression<ArrayCreateExpression>("new [] { 1, }");
			Assert.AreEqual(new Role[] {
			                	AstNode.Roles.LBrace,
			                	AstNode.Roles.Expression,
			                	AstNode.Roles.Comma,
			                	AstNode.Roles.RBrace
			                }, ace.Initializer.Children.Select(c => c.Role).ToArray());
		}
	}
}
