// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
			//Assert.AreEqual("My.Base.Class", td.BaseTypes[0].Type);
			Assert.Ignore("need to check base type"); // TODO
			Assert.AreEqual(Modifiers.None, td.Modifiers);
		}
		
		[Test]
		public void SimpleClassRegionTest()
		{
			const string program = "class MyClass\n{\n}\n";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			Assert.AreEqual(1, td.StartLocation.Line, "StartLocation.Y");
			Assert.AreEqual(1, td.StartLocation.Column, "StartLocation.X");
			AstLocation bodyStartLocation = td.GetChildByRole(AstNode.Roles.LBrace).PrevSibling.EndLocation;
			Assert.AreEqual(1, bodyStartLocation.Line, "BodyStartLocation.Y");
			Assert.AreEqual(14, bodyStartLocation.Column, "BodyStartLocation.X");
			Assert.AreEqual(3, td.EndLocation.Line, "EndLocation.Y");
			Assert.AreEqual(2, td.EndLocation.Column, "EndLocation.Y");
		}
		
		[Test, Ignore("partial modifier is broken")]
		public void SimplePartialClassTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("partial class MyClass { }");
			Assert.IsFalse(td.IsNull);
			Assert.AreEqual(ClassType.Class, td.ClassType);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual(Modifiers.Partial, td.Modifiers);
		}
		
		[Test, Ignore("nested classes are broken")]
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
		
		[Test, Ignore]
		public void GenericClassTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("public class G<T> {}");
			
			Assert.AreEqual(ClassType.Class, td.ClassType);
			Assert.AreEqual("G", td.Name);
			Assert.AreEqual(Modifiers.Public, td.Modifiers);
			/*Assert.AreEqual(0, td.BaseTypes.Count);
			Assert.AreEqual(1, td.TypeArguments.Count());
			Assert.AreEqual("T", td.TypeArguments.Single().Name);*/ throw new NotImplementedException();
		}
		
		
		[Test, Ignore]
		public void GenericClassWithWhere()
		{
			string declr = @"
public class Test<T> where T : IMyInterface
{
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(declr);
			
			Assert.AreEqual(ClassType.Class, td.ClassType);
			Assert.AreEqual("Test", td.Name);
			
			/*Assert.AreEqual(1, td.Templates.Count);
			Assert.AreEqual("T", td.Templates[0].Name);
			Assert.AreEqual("IMyInterface", td.Templates[0].Bases[0].Type);*/ throw new NotImplementedException();
		}
		
		[Test, Ignore]
		public void ComplexGenericClassTypeDeclarationTest()
		{
			string declr = @"
public class Generic<T, S> : System.IComparable where S : G<T[]> where  T : MyNamespace.IMyInterface
{
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(declr);
			
			Assert.AreEqual(ClassType.Class, td.ClassType);
			Assert.AreEqual("Generic", td.Name);
			Assert.AreEqual(Modifiers.Public, td.Modifiers);
			/*Assert.AreEqual(1, td.BaseTypes.Count);
			Assert.AreEqual("System.IComparable", td.BaseTypes[0].Type);
			
			Assert.AreEqual(2, td.Templates.Count);
			Assert.AreEqual("T", td.Templates[0].Name);
			Assert.AreEqual("MyNamespace.IMyInterface", td.Templates[0].Bases[0].Type);
			
			Assert.AreEqual("S", td.Templates[1].Name);
			Assert.AreEqual("G", td.Templates[1].Bases[0].Type);
			Assert.AreEqual(1, td.Templates[1].Bases[0].GenericTypes.Count);
			Assert.IsTrue(td.Templates[1].Bases[0].GenericTypes[0].IsArrayType);
			Assert.AreEqual("T", td.Templates[1].Bases[0].GenericTypes[0].Type);
			Assert.AreEqual(new int[] {0}, td.Templates[1].Bases[0].GenericTypes[0].RankSpecifier);*/  throw new NotImplementedException();
		}
		
		[Test, Ignore]
		public void ComplexClassTypeDeclarationTest()
		{
			string declr = @"
[MyAttr()]
public abstract class MyClass : MyBase, Interface1, My.Test.Interface2
{
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(declr);
			
			Assert.AreEqual(ClassType.Class, td.ClassType);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual(Modifiers.Public | Modifiers.Abstract, td.Modifiers);
			Assert.AreEqual(1, td.Attributes.Count());
			/*			Assert.AreEqual(3, td.BaseTypes.Count);
			Assert.AreEqual("MyBase", td.BaseTypes[0].Type);
			Assert.AreEqual("Interface1", td.BaseTypes[1].Type);
			Assert.AreEqual("My.Test.Interface2", td.BaseTypes[2].Type);*/  throw new NotImplementedException();
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
		
		[Test, Ignore]
		public void ContextSensitiveKeywordTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("partial class partial<[partial: where] where> where where : partial<where> { }");
			
			Assert.AreEqual(Modifiers.Partial, td.Modifiers);
			Assert.AreEqual("partial", td.Name);
			
			/*
			Assert.AreEqual(1, td.Templates.Count);
			TemplateDefinition tp = td.Templates[0];
			Assert.AreEqual("where", tp.Name);
			
			Assert.AreEqual(1, tp.Attributes.Count);
			Assert.AreEqual("partial", tp.Attributes[0].AttributeTarget);
			Assert.AreEqual(1, tp.Attributes[0].Attributes.Count);
			Assert.AreEqual("where", tp.Attributes[0].Attributes[0].Name);
			
			Assert.AreEqual(1, tp.Bases.Count);
			Assert.AreEqual("partial", tp.Bases[0].Type);
			Assert.AreEqual("where", tp.Bases[0].GenericTypes[0].Type);*/ throw new NotImplementedException();
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
	}
}
