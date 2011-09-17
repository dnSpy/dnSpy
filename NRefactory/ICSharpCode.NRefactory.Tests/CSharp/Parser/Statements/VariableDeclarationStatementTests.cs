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
using ICSharpCode.NRefactory.PatternMatching;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class VariableDeclarationStatementTests
	{
		[Test]
		public void VariableDeclarationStatementTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("int a = 5;");
			Assert.AreEqual(1, lvd.Variables.Count());
			Assert.AreEqual("a", lvd.Variables.First ().Name);
			var type = lvd.Type;
			Assert.AreEqual("int", type.ToString ());
			Assert.AreEqual(5, ((PrimitiveExpression)lvd.Variables.First ().Initializer).Value);
		}
		
		[Test]
		public void VoidPointerVariableDeclarationTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("void *a;");
			Assert.IsTrue(new VariableDeclarationStatement(new PrimitiveType("void").MakePointerType(), "a").IsMatch(lvd));
		}
		
		[Test]
		public void ComplexGenericVariableDeclarationStatementTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("Generic<Namespace.Printable, G<Printable[]> > where = new Generic<Namespace.Printable, G<Printable[]>>();");
			AstType type = new SimpleType("Generic") {
				TypeArguments = {
					new MemberType { Target = new SimpleType("Namespace"), MemberName = "Printable" },
					new SimpleType("G") { TypeArguments = { new SimpleType("Printable").MakeArrayType() } }
				}};
			Assert.IsTrue(new VariableDeclarationStatement(type, "where", new ObjectCreateExpression { Type = type.Clone() }).IsMatch(lvd));
		}
		
		[Test]
		public void NestedGenericVariableDeclarationStatementTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("MyType<string>.InnerClass<int>.InnerInnerClass a;");
			AstType type = new MemberType {
				Target = new MemberType {
					Target = new SimpleType("MyType") { TypeArguments = { new PrimitiveType("string") } },
					MemberName = "InnerClass",
					TypeArguments = { new PrimitiveType("int") }
				},
				MemberName = "InnerInnerClass"
			};
			Assert.IsTrue(new VariableDeclarationStatement(type, "a").IsMatch(lvd));
		}
		
		[Test]
		public void GenericWithArrayVariableDeclarationStatementTest1()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("G<int>[] a;");
			AstType type = new SimpleType("G") {
				TypeArguments = { new PrimitiveType("int") }
			}.MakeArrayType();
			Assert.IsTrue(new VariableDeclarationStatement(type, "a").IsMatch(lvd));
		}
		
		[Test]
		public void GenericWithArrayVariableDeclarationStatementTest2()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("G<int[]> a;");
			AstType type = new SimpleType("G") {
				TypeArguments = { new PrimitiveType("int").MakeArrayType() }
			};
			Assert.IsTrue(new VariableDeclarationStatement(type, "a").IsMatch(lvd));
		}
		
		[Test]
		public void GenericVariableDeclarationStatementTest2()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("G<G<int> > a;");
			AstType type = new SimpleType("G") {
				TypeArguments = {
					new SimpleType("G") { TypeArguments = { new PrimitiveType("int") } }
				}
			};
			Assert.IsTrue(new VariableDeclarationStatement(type, "a").IsMatch(lvd));
		}
		
		[Test]
		public void GenericVariableDeclarationStatementTest2WithoutSpace()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("G<G<int>> a;");
			AstType type = new SimpleType("G") {
				TypeArguments = {
					new SimpleType("G") { TypeArguments = { new PrimitiveType("int") } }
				}
			};
			Assert.IsTrue(new VariableDeclarationStatement(type, "a").IsMatch(lvd));
		}
		
		[Test]
		public void GenericVariableDeclarationStatementTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("G<int> a;");
			AstType type = new SimpleType("G") {
				TypeArguments = { new PrimitiveType("int") }
			};
			Assert.IsTrue(new VariableDeclarationStatement(type, "a").IsMatch(lvd));
		}
		
		[Test]
		public void SimpleVariableDeclarationStatementTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("MyVar var = new MyVar();");
			Assert.IsTrue(new VariableDeclarationStatement(new SimpleType("MyVar"), "var", new ObjectCreateExpression { Type = new SimpleType("MyVar") }).IsMatch(lvd));
		}
		
		[Test]
		public void SimpleVariableDeclarationStatementTest1()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("yield yield = new yield();");
			Assert.IsTrue(new VariableDeclarationStatement(new SimpleType("yield"), "yield", new ObjectCreateExpression { Type = new SimpleType("yield") }).IsMatch(lvd));
		}
		
		[Test]
		public void NullableVariableDeclarationStatementTest1()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("int? a;");
			Assert.IsTrue(new VariableDeclarationStatement(new PrimitiveType("int").MakeNullableType(), "a").IsMatch(lvd));
		}
		
		[Test]
		public void NullableVariableDeclarationStatementTest2()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("DateTime? a;");
			Assert.IsTrue(new VariableDeclarationStatement(new SimpleType("DateTime").MakeNullableType(), "a").IsMatch(lvd));
		}
		
		[Test, Ignore("The parser creates nested ComposedTypes while MakeArrayType() adds the specifier to the existing ComposedType")]
		public void NullableVariableDeclarationStatementTest3()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("DateTime?[] a;");
			Assert.IsTrue(new VariableDeclarationStatement(new SimpleType("DateTime").MakeNullableType().MakeArrayType(), "a").IsMatch(lvd));
		}
		
		[Test]
		public void NullableVariableDeclarationStatementTest4()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("SomeStruct<int?>? a;");
			AstType type = new SimpleType("SomeStruct") {
				TypeArguments = { new PrimitiveType("int").MakeNullableType() }
			}.MakeNullableType();
			Assert.IsTrue(new VariableDeclarationStatement(type, "a").IsMatch(lvd));
		}
		
		[Test]
		public void PositionTestWithoutModifier()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("\ndouble w = 7;");
			Assert.AreEqual(2, lvd.StartLocation.Line);
			Assert.AreEqual(1, lvd.StartLocation.Column);
			Assert.AreEqual(2, lvd.EndLocation.Line);
			Assert.AreEqual(14, lvd.EndLocation.Column);
		}
		
		[Test]
		public void PositionTestWithModifier()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("\nconst double w = 7;");
			Assert.AreEqual(Modifiers.Const, lvd.Modifiers);
			Assert.AreEqual(2, lvd.StartLocation.Line);
			Assert.AreEqual(1, lvd.StartLocation.Column);
			Assert.AreEqual(2, lvd.EndLocation.Line);
			Assert.AreEqual(20, lvd.EndLocation.Column);
		}
		
		[Test, Ignore("Nested arrays are broken in the parser")]
		public void NestedArray()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("DateTime[,][] a;");
			Assert.IsTrue(new VariableDeclarationStatement(new SimpleType("DateTime").MakeArrayType(1).MakeArrayType(2), "a").IsMatch(lvd));
		}
		
		[Test, Ignore("Nested pointers are broken in the parser")]
		public void NestedPointers()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("DateTime*** a;");
			Assert.IsTrue(new VariableDeclarationStatement(new SimpleType("DateTime").MakePointerType().MakePointerType().MakePointerType(), "a").IsMatch(lvd));
		}
		
		[Test, Ignore("The parser creates nested ComposedTypes while MakeArrayType() adds the specifier to the existing ComposedType")]
		public void ArrayOfPointers()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("DateTime*[] a;");
			Assert.IsTrue(new VariableDeclarationStatement(new SimpleType("DateTime").MakePointerType().MakeArrayType(), "a").IsMatch(lvd));
		}
		
		[Test, Ignore("The parser creates nested ComposedTypes while MakeArrayType() adds the specifier to the existing ComposedType")]
		public void ArrayOfNullables()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("DateTime?[] a;");
			Assert.IsTrue(new VariableDeclarationStatement(new SimpleType("DateTime").MakeNullableType().MakeArrayType(), "a").IsMatch(lvd));
		}
		
		[Test]
		public void Global()
		{
			ParseUtilCSharp.AssertStatement(
				"global::System.String a;",
				new VariableDeclarationStatement {
					Type = new MemberType {
						Target = new MemberType {
							Target = new SimpleType("global"),
							IsDoubleColon = true,
							MemberName = "System"
						},
						IsDoubleColon = false,
						MemberName = "String",
					},
					Variables = {
						new VariableInitializer("a")
					}
				});
		}
		
		[Test]
		public void ArrayDeclarationWithInitializer()
		{
			ParseUtilCSharp.AssertStatement(
				"int[] a = { 0 };",
				new VariableDeclarationStatement {
					Type = new PrimitiveType("int").MakeArrayType(),
					Variables = {
						new VariableInitializer {
							Name = "a",
							Initializer = new ArrayInitializerExpression {
								Elements = { new PrimitiveExpression(0) }
							}
						}
					}});
		}
	}
}
