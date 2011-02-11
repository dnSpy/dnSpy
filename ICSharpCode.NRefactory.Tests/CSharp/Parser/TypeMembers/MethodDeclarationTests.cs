// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture]
	public class MethodDeclarationTests
	{
		[Test, Ignore("type references not yet implemented")]
		public void SimpleMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyMethod() {} ");
			Assert.AreEqual("System.Void", md.ReturnType);
			Assert.AreEqual(0, md.Parameters.Count());
			Assert.IsFalse(md.IsExtensionMethod);
		}
		
		[Test, Ignore("type references not yet implemented")]
		public void AbstractMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("abstract void MyMethod();");
			Assert.AreEqual("System.Void", md.ReturnType);
			Assert.AreEqual(0, md.Parameters.Count());
			Assert.IsFalse(md.IsExtensionMethod);
			Assert.IsTrue(md.Body.IsNull);
			Assert.AreEqual(Modifiers.Abstract, md.Modifiers);
		}
		
		[Test, Ignore("type references not yet implemented")]
		public void DefiningPartialMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("partial void MyMethod();");
			Assert.AreEqual("System.Void", md.ReturnType);
			Assert.AreEqual(0, md.Parameters.Count());
			Assert.IsFalse(md.IsExtensionMethod);
			Assert.IsTrue(md.Body.IsNull);
			Assert.AreEqual(Modifiers.Partial, md.Modifiers);
		}
		
		[Test, Ignore("type references not yet implemented")]
		public void ImplementingPartialMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("partial void MyMethod() { }");
			Assert.AreEqual("System.Void", md.ReturnType);
			Assert.AreEqual(0, md.Parameters.Count());
			Assert.IsFalse(md.IsExtensionMethod);
			Assert.IsFalse(md.Body.IsNull);
			Assert.AreEqual(Modifiers.Partial, md.Modifiers);
		}
		
		[Test]
		public void SimpleMethodRegionTest()
		{
			const string program = @"
		void MyMethod()
		{
			OtherMethod();
		}
";
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(2, md.StartLocation.Line, "StartLocation.Y");
			Assert.AreEqual(5, md.EndLocation.Line, "EndLocation.Y");
			Assert.AreEqual(3, md.StartLocation.Column, "StartLocation.X");
			Assert.AreEqual(4, md.EndLocation.Column, "EndLocation.X");
		}
		
		[Test]
		public void MethodWithModifiersRegionTest()
		{
			const string program = @"
		public static void MyMethod()
		{
			OtherMethod();
		}
";
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(2, md.StartLocation.Line, "StartLocation.Y");
			Assert.AreEqual(5, md.EndLocation.Line, "EndLocation.Y");
			Assert.AreEqual(3, md.StartLocation.Column, "StartLocation.X");
		}
		
		[Test]
		public void MethodWithUnnamedParameterDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyMethod(int) {} ", true);
			Assert.AreEqual("System.Void", md.ReturnType);
			Assert.AreEqual(1, md.Parameters.Count());
			//Assert.AreEqual("?", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
		}
		
		/* TODO: port unit tests
		[Test]
		public void GenericVoidMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyMethod<T>(T a) {} ");
			Assert.AreEqual("System.Void", md.ReturnType);
			Assert.AreEqual(1, md.Parameters.Count());
			Assert.AreEqual("T", md.Parameters.Single().Type);
			Assert.AreEqual("a", md.Parameters.Single().Name);
			
			Assert.AreEqual(1, md.TypeParameters.Count());
			Assert.AreEqual("T", md.Templates[0].Name);
		}
		
		[Test]
		public void GenericMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("T MyMethod<T>(T a) {} ");
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
		}
		
		[Test]
		public void GenericMethodDeclarationWithConstraintTest()
		{
			string program = "T MyMethod<T>(T a) where T : ISomeInterface {} ";
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void GenericMethodInInterface()
		{
			const string program = @"interface MyInterface {
	T MyMethod<T>(T a) where T : ISomeInterface;
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			MethodDeclaration md = (MethodDeclaration)td.Children[0];
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void GenericVoidMethodInInterface()
		{
			const string program = @"interface MyInterface {
	void MyMethod<T>(T a) where T : ISomeInterface;
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			MethodDeclaration md = (MethodDeclaration)td.Children[0];
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void ShadowingMethodInInterface()
		{
			const string program = @"interface MyInterface : IDisposable {
	new void Dispose();
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			MethodDeclaration md = (MethodDeclaration)td.Children[0];
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(0, md.Parameters.Count);
			Assert.AreEqual(Modifiers.New, md.Modifier);
		}
		
		[Test]
		public void MethodImplementingInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("int MyInterface.MyMethod() {} ");
			Assert.AreEqual("System.Int32", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
		}
		
		[Test]
		public void MethodImplementingGenericInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("int MyInterface<string>.MyMethod() {} ");
			Assert.AreEqual("System.Int32", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
			Assert.AreEqual("System.String", md.InterfaceImplementations[0].InterfaceType.GenericTypes[0].Type);
		}
		
		[Test]
		public void VoidMethodImplementingInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyInterface.MyMethod() {} ");
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
		}
		
		[Test]
		public void VoidMethodImplementingGenericInterfaceTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>("void MyInterface<string>.MyMethod() {} ");
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			
			Assert.AreEqual("MyInterface", md.InterfaceImplementations[0].InterfaceType.Type);
			Assert.AreEqual("System.String", md.InterfaceImplementations[0].InterfaceType.GenericTypes[0].Type);
		}
		
		[Test]
		public void IncompleteConstraintsTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(
				"void a<T>() where T { }", true // expect errors
			);
			Assert.AreEqual("a", md.Name);
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(0, md.Templates[0].Bases.Count);
		}
		
		[Test]
		public void ExtensionMethodTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(
				"public static int ToInt32(this string s) { return int.Parse(s); }"
			);
			Assert.AreEqual("ToInt32", md.Name);
			Assert.IsTrue(md.IsExtensionMethod);
			Assert.AreEqual("s", md.Parameters[0].ParameterName);
			Assert.AreEqual("System.String", md.Parameters[0].TypeReference.Type);
		}
		 */
		
		[Test]
		public void VoidExtensionMethodTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(
				"public static void Print(this string s) { Console.WriteLine(s); }"
			);
			Assert.AreEqual("Print", md.Name);
			Assert.AreEqual("s", md.Parameters.First().Name);
			Assert.AreEqual(ParameterModifier.This, md.Parameters.First().ParameterModifier);
			Assert.AreEqual("string", ((PrimitiveType)md.Parameters.First().Type).Keyword);
			Assert.IsTrue(md.IsExtensionMethod);
		}
		
		/* TODO
		[Test]
		public void MethodWithEmptyAssignmentErrorInBody()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(
				"void A\n" +
				"{\n" +
				"int a = 3;\n" +
				" = 4;\n" +
				"}", true // expect errors
			);
			Assert.AreEqual("A", md.Name);
			Assert.AreEqual(new Location(1, 2), md.Body.StartLocation);
			Assert.AreEqual(new Location(2, 5), md.Body.EndLocation);
		}
		
		[Test]
		public void OptionalParameterTest()
		{
			MethodDeclaration md = ParseUtilCSharp.ParseTypeMember<MethodDeclaration>(
				"public void Foo(string bar = null, int baz = 0) { }"
			);
			Assert.AreEqual("Foo", md.Name);
			
			Assert.AreEqual("bar", md.Parameters[0].ParameterName);
			Assert.AreEqual("System.String", md.Parameters[0].TypeReference.Type);
			Assert.AreEqual(ParameterModifiers.In | ParameterModifiers.Optional, md.Parameters[0].ParamModifier);
			Assert.IsNull(((PrimitiveExpression)md.Parameters[0].DefaultValue).Value);
			
			Assert.AreEqual("baz", md.Parameters[1].ParameterName);
			Assert.AreEqual("System.Int32", md.Parameters[1].TypeReference.Type);
			Assert.AreEqual(ParameterModifiers.In | ParameterModifiers.Optional, md.Parameters[1].ParamModifier);
			Assert.AreEqual(0, ((PrimitiveExpression)md.Parameters[1].DefaultValue).Value);
		}*/
	}
}
