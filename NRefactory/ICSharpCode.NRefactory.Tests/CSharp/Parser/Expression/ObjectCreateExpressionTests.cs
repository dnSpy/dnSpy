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
	public class ObjectCreateExpressionTests
	{
		[Test]
		public void Simple()
		{
			ParseUtilCSharp.AssertExpression(
				"new MyObject(1, 2, 3)",
				new ObjectCreateExpression {
					Type = new SimpleType("MyObject"),
					Arguments = {
						new PrimitiveExpression(1),
						new PrimitiveExpression(2),
						new PrimitiveExpression(3)
					}});
		}
		
		[Test]
		public void Nullable()
		{
			ParseUtilCSharp.AssertExpression(
				"new IntPtr?(1)",
				new ObjectCreateExpression {
					Type = new SimpleType("IntPtr").MakeNullableType(),
					Arguments = { new PrimitiveExpression(1) }
				});
		}
		
		[Test]
		public void ObjectInitializer()
		{
			ParseUtilCSharp.AssertExpression(
				"new Point() { X = 0, Y = 1 }",
				new ObjectCreateExpression {
					Type = new SimpleType("Point"),
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new NamedExpression("X", new PrimitiveExpression(0)),
							new NamedExpression("Y", new PrimitiveExpression(1))
						}
					}});
		}
		
		[Test]
		public void ObjectInitializerWithoutParenthesis()
		{
			ParseUtilCSharp.AssertExpression(
				"new Point { X = 0, Y = 1 }",
				new ObjectCreateExpression {
					Type = new SimpleType("Point"),
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new NamedExpression("X", new PrimitiveExpression(0)),
							new NamedExpression("Y", new PrimitiveExpression(1))
						}
					}});
		}
		
		[Test]
		public void ObjectInitializerWithTrailingComma()
		{
			ParseUtilCSharp.AssertExpression(
				"new Point() { X = 0, Y = 1, }",
				new ObjectCreateExpression {
					Type = new SimpleType("Point"),
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new NamedExpression("X", new PrimitiveExpression(0)),
							new NamedExpression("Y", new PrimitiveExpression(1))
						}
					}});
		}
		
		[Test]
		public void NestedObjectInitializer()
		{
			ParseUtilCSharp.AssertExpression(
				"new Rectangle { P1 = new Point { X = 0, Y = 1 }, P2 = new Point { X = 2, Y = 3 } }",
				new ObjectCreateExpression {
					Type = new SimpleType("Rectangle"),
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new NamedExpression(
								"P1",
								new ObjectCreateExpression {
									Type = new SimpleType("Point"),
									Initializer = new ArrayInitializerExpression {
										Elements = {
											new NamedExpression("X", new PrimitiveExpression(0)),
											new NamedExpression("Y", new PrimitiveExpression(1))
										}
									}}),
							new NamedExpression(
								"P2",
								new ObjectCreateExpression {
									Type = new SimpleType("Point"),
									Initializer = new ArrayInitializerExpression {
										Elements = {
											new NamedExpression("X", new PrimitiveExpression(2)),
											new NamedExpression("Y", new PrimitiveExpression(3))
										}
									}})
						}}});
		}
		
		[Test]
		public void NestedObjectInitializerForPreinitializedProperty()
		{
			ParseUtilCSharp.AssertExpression(
				"new Rectangle { P1 = { X = 0, Y = 1 }, P2 = { X = 2, Y = 3 } }",
				new ObjectCreateExpression {
					Type = new SimpleType("Rectangle"),
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new NamedExpression(
								"P1",
								new ArrayInitializerExpression {
									Elements = {
										new NamedExpression("X", new PrimitiveExpression(0)),
										new NamedExpression("Y", new PrimitiveExpression(1))
									}
								}),
							new NamedExpression(
								"P2",
								new ArrayInitializerExpression {
									Elements = {
										new NamedExpression("X", new PrimitiveExpression(2)),
										new NamedExpression("Y", new PrimitiveExpression(3))
									}
								})
						}}});
		}
		
		[Test]
		public void CollectionInitializer()
		{
			ParseUtilCSharp.AssertExpression(
				"new List<int> { 0, 1, 2 }",
				new ObjectCreateExpression {
					Type = new SimpleType("List", new PrimitiveType("int")),
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new ArrayInitializerExpression(new PrimitiveExpression(0)),
							new ArrayInitializerExpression(new PrimitiveExpression(1)),
							new ArrayInitializerExpression(new PrimitiveExpression(2))
						}}});
		}
		
		
		[Test]
		public void DictionaryInitializer()
		{
			ParseUtilCSharp.AssertExpression(
				"new Dictionary<string, int> { { \"a\", 0 }, { \"b\", 1 } }",
				new ObjectCreateExpression {
					Type = new SimpleType("Dictionary", new PrimitiveType("string"), new PrimitiveType("int")),
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new ArrayInitializerExpression {
								Elements = { new PrimitiveExpression("a"), new PrimitiveExpression(0) }
							},
							new ArrayInitializerExpression {
								Elements = { new PrimitiveExpression("b"), new PrimitiveExpression(1) }
							}
						}}});
		}
		
		[Test]
		public void ComplexCollectionInitializer()
		{
			ParseUtilCSharp.AssertExpression(
				@"new List<Contact> {
					new Contact {
						Name = ""Chris"",
						PhoneNumbers = { ""206-555-0101"" }
					},
					new Contact(additionalParameter) {
						Name = ""Bob"",
						PhoneNumbers = { ""650-555-0199"", ""425-882-8080"" }
					}
				}",
				new ObjectCreateExpression {
					Type = new SimpleType("List", new SimpleType("Contact")),
					Initializer = new ArrayInitializerExpression(
						new ArrayInitializerExpression(
							new ObjectCreateExpression {
								Type = new SimpleType("Contact"),
								Initializer = new ArrayInitializerExpression {
									Elements = {
										new NamedExpression("Name", new PrimitiveExpression("Chris")),
										new NamedExpression("PhoneNumbers", new ArrayInitializerExpression () {
											Elements = { new ArrayInitializerExpression(new PrimitiveExpression("206-555-0101")) }
										})
									}}}),
						new ArrayInitializerExpression(
							new ObjectCreateExpression {
								Type = new SimpleType("Contact"),
								Arguments = { new IdentifierExpression("additionalParameter") },
								Initializer = new ArrayInitializerExpression {
									Elements = {
										new NamedExpression("Name", new PrimitiveExpression("Bob")),
										new NamedExpression("PhoneNumbers", new ArrayInitializerExpression () {
											Elements = {
												new ArrayInitializerExpression(new PrimitiveExpression("650-555-0199")),
												new ArrayInitializerExpression(new PrimitiveExpression("425-882-8080"))
											}
										})
									}}})
						)});
		}
		
		[Test]
		public void AssignmentInCollectionInitializer()
		{
			ParseUtilCSharp.AssertExpression(
				@"new List<int> { { a = 1 } }",
				new ObjectCreateExpression {
					Type = new SimpleType("List", new PrimitiveType("int")),
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new ArrayInitializerExpression {
								Elements = {
									new AssignmentExpression(new IdentifierExpression("a"), new PrimitiveExpression(1))
								}
							}
						}}});
		}
	}
}
