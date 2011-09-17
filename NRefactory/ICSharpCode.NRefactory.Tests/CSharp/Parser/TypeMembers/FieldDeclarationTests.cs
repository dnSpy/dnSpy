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

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture]
	public class FieldDeclarationTests
	{
		[Test]
		public void SimpleFieldDeclarationTest()
		{
			ParseUtilCSharp.AssertTypeMember(
				"int[,,,] myField;",
				new FieldDeclaration {
					ReturnType = new PrimitiveType("int").MakeArrayType(4),
					Variables = { new VariableInitializer("myField") }
				});
		}
		
		[Test]
		public void MultipleFieldDeclarationTest()
		{
			ParseUtilCSharp.AssertTypeMember(
				"int a = 1, b = 2;",
				new FieldDeclaration {
					ReturnType = new PrimitiveType("int"),
					Variables = {
						new VariableInitializer("a", new PrimitiveExpression(1)),
						new VariableInitializer("b", new PrimitiveExpression(2)),
					}
				});
		}
		
		[Test]
		public void FieldWithArrayInitializer()
		{
			ParseUtilCSharp.AssertTypeMember(
				"public static readonly int[] arr = { 1, 2, 3 };",
				new FieldDeclaration {
					Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Readonly,
					ReturnType = new PrimitiveType("int").MakeArrayType(),
					Variables = {
						new VariableInitializer {
							Name = "arr",
							Initializer = new ArrayInitializerExpression {
								Elements = {
									new PrimitiveExpression(1),
									new PrimitiveExpression(2),
									new PrimitiveExpression(3)
								}
							}
						}
					}});
		}
		
		[Test]
		public void FieldWithFixedSize()
		{
			ParseUtilCSharp.AssertTypeMember(
				"public unsafe fixed int Field[100];",
				new FixedFieldDeclaration() {
					Modifiers =  Modifiers.Public | Modifiers.Unsafe,
					ReturnType = new PrimitiveType("int"),
					Variables = {
						new FixedVariableInitializer {
							Name = "Field",
							CountExpression = new PrimitiveExpression(100)
						}
					}
				});
		}
	}
}
