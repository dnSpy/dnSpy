// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class LocalVariableDeclarationTests
	{
		#region VB.NET
		[Test]
		public void VBNetLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a As Integer = 5");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Int32", type.Type);
			Assert.AreEqual(5, ((PrimitiveExpression)lvd.Variables[0].Initializer).Value);
		}
		
		[Test]
		public void VBNetLocalVariableNamedOverrideDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim override As Integer = 5");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("override", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Int32", type.Type);
			Assert.AreEqual(5, ((PrimitiveExpression)lvd.Variables[0].Initializer).Value);
		}
		
		[Test]
		public void VBNetLocalArrayDeclarationWithInitializationTest()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a(10) As Integer");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Int32", type.Type);
			Assert.AreEqual(new int[] { 0 } , type.RankSpecifier);
			ArrayCreateExpression ace = (ArrayCreateExpression)lvd.Variables[0].Initializer;
			Assert.AreEqual(new int[] { 0 } , ace.CreateType.RankSpecifier);
			Assert.AreEqual(1, ace.Arguments.Count);
			Assert.AreEqual(10, ((PrimitiveExpression)ace.Arguments[0]).Value);
		}
		
		[Test]
		public void VBNetLocalArrayDeclarationWithInitializationAndLowerBoundTest()
		{
			// VB.NET allows only "0" as lower bound
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a(0 To 10) As Integer");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Int32", type.Type);
			Assert.AreEqual(new int[] { 0 } , type.RankSpecifier);
			ArrayCreateExpression ace = (ArrayCreateExpression)lvd.Variables[0].Initializer;
			Assert.AreEqual(new int[] { 0 } , ace.CreateType.RankSpecifier);
			Assert.AreEqual(1, ace.Arguments.Count);
			Assert.AreEqual(10, ((PrimitiveExpression)ace.Arguments[0]).Value);
		}
		
		[Test]
		public void VBNetLocalArrayDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a() As Integer");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Int32", type.Type);
			Assert.AreEqual(new int[] { 0 } , type.RankSpecifier);
		}
		
		[Test]
		public void VBNetLocalJaggedArrayDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a(10)() As Integer");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Int32", type.Type);
			Assert.AreEqual(new int[] { 0, 0 } , type.RankSpecifier);
			ArrayCreateExpression ace = (ArrayCreateExpression)lvd.Variables[0].Initializer;
			Assert.AreEqual(new int[] {0, 0}, ace.CreateType.RankSpecifier);
			Assert.AreEqual(1, ace.Arguments.Count);
			Assert.AreEqual(10, ((PrimitiveExpression)ace.Arguments[0]).Value);
		}
		
		[Test]
		public void VBNetComplexGenericLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim where As Generic(Of Printable, G(Of Printable()))");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("where", lvd.Variables[0].Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("Generic", type.Type);
			Assert.AreEqual(2, type.GenericTypes.Count);
			Assert.AreEqual("Printable", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.AreEqual("G", type.GenericTypes[1].Type);
			Assert.AreEqual(1, type.GenericTypes[1].GenericTypes.Count);
			Assert.AreEqual("Printable", type.GenericTypes[1].GenericTypes[0].Type);
		}
		
		[Test]
		public void VBNetGenericWithArrayLocalVariableDeclarationTest1()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a As G(Of Integer)()");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.IsFalse(type.GenericTypes[0].IsArrayType);
			Assert.AreEqual(new int[] { 0 }, type.RankSpecifier);
		}
		
		[Test]
		public void VBNetGenericWithArrayLocalVariableDeclarationTest2()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a As G(Of Integer())");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.IsFalse(type.IsArrayType);
			Assert.AreEqual(1, type.GenericTypes[0].RankSpecifier.Length);
			Assert.AreEqual(0, type.GenericTypes[0].RankSpecifier[0]);
		}
		
		[Test]
		public void VBNetGenericLocalVariableDeclarationTest2()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a As G(Of G(Of Integer))");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("G", type.GenericTypes[0].Type);
			Assert.AreEqual(1, type.GenericTypes[0].GenericTypes.Count);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].GenericTypes[0].Type);
		}
		
		[Test]
		public void VBNetGenericLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a As G(Of Integer)");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].Type);
		}
		
		[Test]
		public void VBNetGenericLocalVariableInitializationTest()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a As New G(Of Integer)");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].Type);
			// TODO: Check initializer
		}
		
		[Test]
		public void VBNetNestedGenericLocalVariableDeclarationTest()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a as MyType(of string).InnerClass(of integer).InnerInnerClass");
			Assert.AreEqual(1, lvd.Variables.Count);
			InnerClassTypeReference ic = (InnerClassTypeReference)lvd.GetTypeForVariable(0);
			Assert.AreEqual("InnerInnerClass", ic.Type);
			Assert.AreEqual(0, ic.GenericTypes.Count);
			ic = (InnerClassTypeReference)ic.BaseType;
			Assert.AreEqual("InnerClass", ic.Type);
			Assert.AreEqual(1, ic.GenericTypes.Count);
			Assert.AreEqual("System.Int32", ic.GenericTypes[0].Type);
			Assert.AreEqual("MyType", ic.BaseType.Type);
			Assert.AreEqual(1, ic.BaseType.GenericTypes.Count);
			Assert.AreEqual("System.String", ic.BaseType.GenericTypes[0].Type);
		}
		
		[Test]
		public void VBNetDimInSingleLineIf()
		{
			IfElseStatement ifes = ParseUtil.ParseStatement<IfElseStatement>("If a Then Dim b As String");
			LocalVariableDeclaration lvd = (LocalVariableDeclaration)ifes.TrueStatement[0];
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.String", type.Type);
		}
		#endregion
	}
}
