// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture, Ignore]
	public class VariableDeclarationStatementTests
	{
		[Test]
		public void VariableDeclarationStatementTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("int a = 5;");
			Assert.AreEqual(1, lvd.Variables.Count());
			/*Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Int32", type.Type);
			Assert.AreEqual(5, ((PrimitiveExpression)lvd.Variables[0].Initializer).Value);*/ throw new NotImplementedException();
		}
		
		/* TODO port unit tests
		[Test]
		public void VoidPointerVariableDeclarationTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("void *a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Void", type.Type);
			Assert.AreEqual(1, type.PointerNestingLevel);
		}
		
		[Test]
		public void ComplexGenericVariableDeclarationStatementTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("Generic<Namespace.Printable, G<Printable[]> > where = new Generic<Namespace.Printable, G<Printable[]>>();");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("where", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("Generic", type.Type);
			Assert.AreEqual(2, type.GenericTypes.Count);
			Assert.AreEqual("Namespace.Printable", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.AreEqual("G", type.GenericTypes[1].Type);
			Assert.AreEqual(1, type.GenericTypes[1].GenericTypes.Count);
			Assert.AreEqual("Printable", type.GenericTypes[1].GenericTypes[0].Type);
			
			// TODO: Check initializer
		}
		
		[Test]
		public void NestedGenericVariableDeclarationStatementTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("MyType<string>.InnerClass<int>.InnerInnerClass a;");
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
		public void GenericWithArrayVariableDeclarationStatementTest1()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("G<int>[] a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.IsFalse(type.GenericTypes[0].IsArrayType);
			Assert.AreEqual(new int[] {0}, type.RankSpecifier);
		}
		
		[Test]
		public void GenericWithArrayVariableDeclarationStatementTest2()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("G<int[]> a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].Type);
			Assert.AreEqual(0, type.GenericTypes[0].GenericTypes.Count);
			Assert.IsFalse(type.IsArrayType);
			Assert.AreEqual(new int[] {0}, type.GenericTypes[0].RankSpecifier);
		}
		
		[Test]
		public void GenericVariableDeclarationStatementTest2()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("G<G<int> > a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("G", type.GenericTypes[0].Type);
			Assert.AreEqual(1, type.GenericTypes[0].GenericTypes.Count);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].GenericTypes[0].Type);
		}
		
		[Test]
		public void GenericVariableDeclarationStatementTest2WithoutSpace()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("G<G<int>> a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("G", type.GenericTypes[0].Type);
			Assert.AreEqual(1, type.GenericTypes[0].GenericTypes.Count);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].GenericTypes[0].Type);
		}
		
		[Test]
		public void GenericVariableDeclarationStatementTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("G<int> a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("G", type.Type);
			Assert.AreEqual(1, type.GenericTypes.Count);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].Type);
		}
		
		[Test]
		public void SimpleVariableDeclarationStatementTest()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("MyVar var = new MyVar();");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("var", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("MyVar", type.Type);
			// TODO: Check initializer
		}
		
		[Test]
		public void SimpleVariableDeclarationStatementTest1()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("yield yield = new yield();");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("yield", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("yield", type.Type);
			// TODO: Check initializer
		}
		
		[Test]
		public void NullableVariableDeclarationStatementTest1()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("int? a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Nullable", type.Type);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].Type);
		}
		
		[Test]
		public void NullableVariableDeclarationStatementTest2()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("DateTime? a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Nullable", type.Type);
			Assert.AreEqual("DateTime", type.GenericTypes[0].Type);
		}
		
		[Test]
		public void NullableVariableDeclarationStatementTest3()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("DateTime?[] a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.IsTrue(type.IsArrayType);
			Assert.AreEqual("System.Nullable", type.Type);
			Assert.AreEqual("DateTime", type.GenericTypes[0].Type);
		}
		
		[Test]
		public void NullableVariableDeclarationStatementTest4()
		{
			VariableDeclarationStatement lvd = ParseUtilCSharp.ParseStatement<VariableDeclarationStatement>("SomeStruct<int?>? a;");
			Assert.AreEqual(1, lvd.Variables.Count);
			Assert.AreEqual("a", ((VariableDeclaration)lvd.Variables[0]).Name);
			TypeReference type = lvd.GetTypeForVariable(0);
			Assert.AreEqual("System.Nullable", type.Type);
			Assert.AreEqual("SomeStruct", type.GenericTypes[0].Type);
			Assert.AreEqual("System.Nullable", type.GenericTypes[0].GenericTypes[0].Type);
			Assert.AreEqual("System.Int32", type.GenericTypes[0].GenericTypes[0].GenericTypes[0].Type);
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
			Assert.AreEqual(Modifiers.Const, lvd.Modifier);
			Assert.AreEqual(2, lvd.StartLocation.Line);
			Assert.AreEqual(1, lvd.StartLocation.Column);
			Assert.AreEqual(2, lvd.EndLocation.Line);
			Assert.AreEqual(20, lvd.EndLocation.Column);
		}*/
	}
}
