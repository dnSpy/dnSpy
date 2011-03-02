// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture]
	public class FieldDeclarationTests
	{
		[Test, Ignore("multidimensional array rank incorrect?")]
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
		
		[Test, Ignore("How do we represent fixed-size fields in the AST?")]
		public void FieldWithFixedSize()
		{
			ParseUtilCSharp.AssertTypeMember(
				"public unsafe fixed int Field[100];",
				new FieldDeclaration());
		}
	}
}
