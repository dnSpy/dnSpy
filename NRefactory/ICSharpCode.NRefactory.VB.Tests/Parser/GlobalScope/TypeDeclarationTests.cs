// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class TypeDeclarationTests
	{
//		#region VB.NET
//		[Test]
//		public void VBNetSimpleClassTypeDeclarationTest()
//		{
//			string program = "Class TestClass\n" +
//				"End Class\n";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			
//			Assert.AreEqual("TestClass", td.Name);
//			Assert.AreEqual(ClassType.Class, td.Type);
//			Assert.AreEqual(1, td.StartLocation.Line, "start line");
//			Assert.AreEqual(1, td.BodyStartLocation.Line, "bodystart line");
//			Assert.AreEqual(16, td.BodyStartLocation.Column, "bodystart col");
//			Assert.AreEqual(2, td.EndLocation.Line, "end line");
//			Assert.AreEqual(10, td.EndLocation.Column, "end col");
//		}
//		
//		[Test]
//		public void VBNetMissingBaseClassTest()
//		{
//			// SD2-1499: test that this invalid code doesn't crash
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>("public class test inherits", true);
//			Assert.AreEqual(0, td.BaseTypes.Count);
//		}
//		
//		[Test]
//		public void VBNetEnumWithBaseClassDeclarationTest()
//		{
//			string program = "Enum TestEnum As Byte\n" +
//				"End Enum\n";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			
//			Assert.AreEqual("TestEnum", td.Name);
//			Assert.AreEqual(ClassType.Enum, td.Type);
//			Assert.AreEqual("System.Byte", td.BaseTypes[0].Type);
//			Assert.AreEqual(0, td.Children.Count);
//		}
//		
//		[Test]
//		public void VBNetEnumOnSingleLine()
//		{
//			string program = "Enum TestEnum : A : B = 1 : C : End Enum";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			
//			Assert.AreEqual("TestEnum", td.Name);
//			Assert.AreEqual(ClassType.Enum, td.Type);
//			Assert.AreEqual(3, td.Children.Count);
//		}
//		
//		[Test]
//		public void VBNetEnumOnSingleLine2()
//		{
//			string program = "Enum TestEnum : A : : B = 1 :: C : End Enum";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			
//			Assert.AreEqual("TestEnum", td.Name);
//			Assert.AreEqual(ClassType.Enum, td.Type);
//			Assert.AreEqual(3, td.Children.Count);
//		}
//		
//		
//		[Test]
//		public void VBNetEnumWithSystemBaseClassDeclarationTest()
//		{
//			string program = "Enum TestEnum As System.UInt16\n" +
//				"End Enum\n";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			
//			Assert.AreEqual("TestEnum", td.Name);
//			Assert.AreEqual(ClassType.Enum, td.Type);
//			Assert.AreEqual("System.UInt16", td.BaseTypes[0].Type);
//			Assert.AreEqual(0, td.Children.Count);
//		}
//		
//		[Test]
//		public void VBNetSimpleClassTypeDeclarationWithoutLastNewLineTest()
//		{
//			string program = "Class TestClass\n" +
//				"End Class";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			
//			Assert.AreEqual("TestClass", td.Name);
//			Assert.AreEqual(ClassType.Class, td.Type);
//			Assert.AreEqual(1, td.StartLocation.Line, "start line");
//			Assert.AreEqual(2, td.EndLocation.Line, "end line");
//		}
//		
//		[Test]
//		public void VBNetSimpleClassTypeDeclarationWithColon()
//		{
//			string program = "Class TestClass\n" +
//				" : \n" +
//				"End Class";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			
//			Assert.AreEqual("TestClass", td.Name);
//			Assert.AreEqual(ClassType.Class, td.Type);
//		}
//		
//		[Test]
//		public void VBNetSimplePartialClassTypeDeclarationTest()
//		{
//			string program = "Partial Class TestClass\n" +
//				"End Class\n";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			
//			Assert.AreEqual("TestClass", td.Name);
//			Assert.AreEqual(ClassType.Class, td.Type);
//			Assert.AreEqual(Modifiers.Partial, td.Modifier);
//		}
//		
//		[Test]
//		public void VBNetPartialPublicClass()
//		{
//			string program = "Partial Public Class TestClass\nEnd Class\n";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			
//			Assert.AreEqual("TestClass", td.Name);
//			Assert.AreEqual(ClassType.Class, td.Type);
//			Assert.AreEqual(Modifiers.Partial | Modifiers.Public, td.Modifier);
//		}
//		
//		[Test]
//		public void VBNetGenericClassTypeDeclarationTest()
//		{
//			string declr = @"
//Public Class Test(Of T)
//
//End Class
//";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(declr);
//			
//			Assert.AreEqual(ClassType.Class, td.Type);
//			Assert.AreEqual("Test", td.Name);
//			Assert.AreEqual(Modifiers.Public, td.Modifier);
//			Assert.AreEqual(0, td.BaseTypes.Count);
//			Assert.AreEqual(1, td.Templates.Count);
//			Assert.AreEqual("T", td.Templates[0].Name);
//		}
//		
//		[Test]
//		public void VBNetGenericClassWithConstraint()
//		{
//			string declr = @"
//Public Class Test(Of T As IMyInterface)
//
//End Class
//";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(declr);
//			
//			Assert.AreEqual(ClassType.Class, td.Type);
//			Assert.AreEqual("Test", td.Name);
//			
//			Assert.AreEqual(1, td.Templates.Count);
//			Assert.AreEqual("T", td.Templates[0].Name);
//			Assert.AreEqual("IMyInterface", td.Templates[0].Bases[0].Type);
//		}
//		
//		[Test]
//		public void VBNetComplexGenericClassTypeDeclarationTest()
//		{
//			string declr = @"
//Public Class Generic(Of T As MyNamespace.IMyInterface, S As {G(Of T()), IAnotherInterface})
//	Implements System.IComparable
//
//End Class
//";
//			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(declr);
//			
//			Assert.AreEqual(ClassType.Class, td.Type);
//			Assert.AreEqual("Generic", td.Name);
//			Assert.AreEqual(Modifiers.Public, td.Modifier);
//			Assert.AreEqual(1, td.BaseTypes.Count);
//			Assert.AreEqual("System.IComparable", td.BaseTypes[0].Type);
//			
//			Assert.AreEqual(2, td.Templates.Count);
//			Assert.AreEqual("T", td.Templates[0].Name);
//			Assert.AreEqual("MyNamespace.IMyInterface", td.Templates[0].Bases[0].Type);
//			
//			Assert.AreEqual("S", td.Templates[1].Name);
//			Assert.AreEqual(2, td.Templates[1].Bases.Count);
//			Assert.AreEqual("G", td.Templates[1].Bases[0].Type);
//			Assert.AreEqual(1, td.Templates[1].Bases[0].GenericTypes.Count);
//			Assert.IsTrue(td.Templates[1].Bases[0].GenericTypes[0].IsArrayType);
//			Assert.AreEqual("T", td.Templates[1].Bases[0].GenericTypes[0].Type);
//			Assert.AreEqual(new int[] {0}, td.Templates[1].Bases[0].GenericTypes[0].RankSpecifier);
//			Assert.AreEqual("IAnotherInterface", td.Templates[1].Bases[1].Type);
//		}
//		#endregion
	}
}
