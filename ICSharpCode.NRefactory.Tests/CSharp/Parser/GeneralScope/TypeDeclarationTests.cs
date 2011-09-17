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
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.GeneralScope
{
	[TestFixture]
	public class TypeDeclarationTests
	{
		[Test]
		public void SimpleClassTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("class MyClass  : My.Base.Class  { }");
			
			Assert.AreEqual(ClassType.Class, td.ClassType);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual("My.Base.Class", td.BaseTypes.First ().ToString ());
			Assert.AreEqual(Modifiers.None, td.Modifiers);
		}
		
		[Test]
		public void SimpleClassRegionTest()
		{
			const string program = "class MyClass\n{\n}\n";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			Assert.AreEqual(1, td.StartLocation.Line, "StartLocation.Y");
			Assert.AreEqual(1, td.StartLocation.Column, "StartLocation.X");
			TextLocation bodyStartLocation = td.GetChildByRole(AstNode.Roles.LBrace).PrevSibling.EndLocation;
			Assert.AreEqual(1, bodyStartLocation.Line, "BodyStartLocation.Y");
			Assert.AreEqual(14, bodyStartLocation.Column, "BodyStartLocation.X");
			Assert.AreEqual(3, td.EndLocation.Line, "EndLocation.Y");
			Assert.AreEqual(2, td.EndLocation.Column, "EndLocation.Y");
		}
		
		[Test]
		public void SimplePartialClassTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("partial class MyClass { }");
			Assert.IsFalse(td.IsNull);
			Assert.AreEqual(ClassType.Class, td.ClassType);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual(Modifiers.Partial, td.Modifiers);
		}
		
		[Test]
		public void NestedClassesTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("class MyClass { partial class P1 {} public partial class P2 {} static class P3 {} internal static class P4 {} }");
			Assert.IsFalse(td.IsNull);
			Assert.AreEqual(ClassType.Class, td.ClassType);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual(Modifiers.Partial, ((TypeDeclaration)td.Members.ElementAt(0)).Modifiers);
			Assert.AreEqual(Modifiers.Partial | Modifiers.Public, ((TypeDeclaration)td.Members.ElementAt(1)).Modifiers);
			Assert.AreEqual(Modifiers.Static, ((TypeDeclaration)td.Members.ElementAt(2)).Modifiers);
			Assert.AreEqual(Modifiers.Static | Modifiers.Internal, ((TypeDeclaration)td.Members.ElementAt(3)).Modifiers);
		}
		
		[Test]
		public void SimpleStaticClassTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("static class MyClass { }");
			Assert.IsFalse(td.IsNull);
			Assert.AreEqual(ClassType.Class, td.ClassType);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual(Modifiers.Static, td.Modifiers);
		}
		
		[Test]
		public void GenericClassTypeDeclarationTest()
		{
			ParseUtilCSharp.AssertGlobal(
				"public class G<T> {}",
				new TypeDeclaration {
					Modifiers = Modifiers.Public,
					ClassType = ClassType.Class,
					Name = "G",
					TypeParameters = { new TypeParameterDeclaration { Name = "T" } }
				});
		}
		
		[Test]
		public void GenericClassWithWhere()
		{
			ParseUtilCSharp.AssertGlobal(
				@"public class Test<T> where T : IMyInterface { }",
				new TypeDeclaration {
					Modifiers = Modifiers.Public,
					ClassType = ClassType.Class,
					Name = "Test",
					TypeParameters = { new TypeParameterDeclaration { Name = "T" } },
					Constraints = {
						new Constraint {
							TypeParameter = "T",
							BaseTypes = { new SimpleType("IMyInterface") }
						}
					}});
		}
		
		[Test, Ignore ("Mono parser bug.")]
		public void ComplexGenericClassTypeDeclarationTest()
		{
			ParseUtilCSharp.AssertGlobal(
				"public class Generic<in T, out S> : System.IComparable where S : G<T[]>, new() where  T : MyNamespace.IMyInterface",
				new TypeDeclaration {
					Modifiers = Modifiers.Public,
					ClassType = ClassType.Class,
					Name = "Generic",
					TypeParameters = {
						new TypeParameterDeclaration { Variance = VarianceModifier.Contravariant, Name = "T" },
						new TypeParameterDeclaration { Variance = VarianceModifier.Covariant, Name = "S" }
					},
					BaseTypes = {
						new MemberType {
							Target = new SimpleType("System"),
							MemberName = "IComparable"
						}
					},
					Constraints = {
						new Constraint {
							TypeParameter = "S",
							BaseTypes = {
								new SimpleType {
									Identifier = "G",
									TypeArguments = { new SimpleType("T").MakeArrayType() }
								},
								new PrimitiveType("new")
							}
						},
						new Constraint {
							TypeParameter = "T",
							BaseTypes = {
								new MemberType {
									Target = new SimpleType("MyNamespace"),
									MemberName = "IMyInterface"
								}
							}
						}
					}
				});
		}
		
		[Test]
		public void ComplexClassTypeDeclarationTest()
		{
			ParseUtilCSharp.AssertGlobal(
				@"
[MyAttr()]
public abstract class MyClass : MyBase, Interface1, My.Test.Interface2
{
}",
				new TypeDeclaration {
					Attributes = {
						new AttributeSection {
							Attributes = {
								new Attribute { Type = new SimpleType("MyAttr") }
							}
						}
					},
					Modifiers = Modifiers.Public | Modifiers.Abstract,
					ClassType = ClassType.Class,
					Name = "MyClass",
					BaseTypes = {
						new SimpleType("MyBase"),
						new SimpleType("Interface1"),
						new MemberType {
							Target = new MemberType {
								Target = new SimpleType("My"),
								MemberName = "Test"
							},
							MemberName = "Interface2"
						}
					}});
		}
		
		[Test]
		public void SimpleStructTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("struct MyStruct {}");
			
			Assert.AreEqual(ClassType.Struct, td.ClassType);
			Assert.AreEqual("MyStruct", td.Name);
		}
		
		[Test]
		public void SimpleInterfaceTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("interface MyInterface {}");
			
			Assert.AreEqual(ClassType.Interface, td.ClassType);
			Assert.AreEqual("MyInterface", td.Name);
		}
		
		[Test]
		public void SimpleEnumTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("enum MyEnum {}");
			
			Assert.AreEqual(ClassType.Enum, td.ClassType);
			Assert.AreEqual("MyEnum", td.Name);
		}
		
		[Test, Ignore("Mono parser bug?")]
		public void ContextSensitiveKeywordTest()
		{
			ParseUtilCSharp.AssertGlobal(
				"partial class partial<[partial: where] where> where where : partial<where> { }",
				new TypeDeclaration {
					Modifiers = Modifiers.Partial,
					ClassType = ClassType.Class,
					Name = "partial",
					TypeParameters = {
						new TypeParameterDeclaration {
							Attributes = {
								new AttributeSection {
									AttributeTarget = "partial",
									Attributes = { new Attribute { Type = new SimpleType("where") } }
								}
							},
							Name = "where"
						}
					},
					Constraints = {
						new Constraint {
							TypeParameter = "where",
							BaseTypes = {
								new SimpleType {
									Identifier = "partial",
									TypeArguments = { new SimpleType("where") }
								}
							}
						}
					}});
		}
		
		[Test]
		public void TypeInNamespaceTest()
		{
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>("namespace N { class MyClass { } }");
			
			Assert.AreEqual("N", ns.Name);
			Assert.AreEqual("MyClass", ((TypeDeclaration)ns.Members.Single()).Name);
		}
		
		[Test]
		public void StructInNamespaceTest()
		{
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>("namespace N { struct MyClass { } }");
			
			Assert.AreEqual("N", ns.Name);
			Assert.AreEqual("MyClass", ((TypeDeclaration)ns.Members.Single()).Name);
		}
		
		[Test]
		public void EnumInNamespaceTest()
		{
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>("namespace N { enum MyClass { } }");
			
			Assert.AreEqual("N", ns.Name);
			Assert.AreEqual("MyClass", ((TypeDeclaration)ns.Members.Single()).Name);
		}
		
		[Test]
		public void InterfaceInNamespaceTest()
		{
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>("namespace N { interface MyClass { } }");
			
			Assert.AreEqual("N", ns.Name);
			Assert.AreEqual("MyClass", ((TypeDeclaration)ns.Members.Single()).Name);
		}
		
		[Test]
		public void EnumWithInitializer()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("enum MyEnum { Val1 = 10 }");
			EnumMemberDeclaration member = (EnumMemberDeclaration)td.Members.Single();
			Assert.AreEqual("Val1", member.Name);
			Assert.AreEqual(10, ((PrimitiveExpression)member.Initializer).Value);
		}
		
		[Test]
		public void EnumWithBaseType()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("enum MyEnum : short { }");
			Assert.AreEqual("MyEnum", td.Name);
			Assert.AreEqual("short", ((PrimitiveType)td.BaseTypes.Single()).Keyword);
		}
	}
}
