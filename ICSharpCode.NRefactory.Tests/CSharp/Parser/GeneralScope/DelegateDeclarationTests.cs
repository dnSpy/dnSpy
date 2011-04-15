// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.GeneralScope
{
	[TestFixture]
	public class DelegateDeclarationTests
	{
		[Test]
		public void SimpleCSharpDelegateDeclarationTest()
		{
			ParseUtilCSharp.AssertGlobal(
				"public delegate void MyDelegate(int a, int secondParam, MyObj lastParam);",
				new DelegateDeclaration {
					Modifiers = Modifiers.Public,
					ReturnType = new PrimitiveType("void"),
					Name = "MyDelegate",
					Parameters = {
						new ParameterDeclaration(new PrimitiveType("int"), "a"),
						new ParameterDeclaration(new PrimitiveType("int"), "secondParam"),
						new ParameterDeclaration(new SimpleType("MyObj"), "lastParam")
					}});
		}
		
		[Test]
		public void GenericDelegateDeclarationTest()
		{
			ParseUtilCSharp.AssertGlobal(
				"public delegate T CreateObject<T>() where T : ICloneable;",
				new DelegateDeclaration {
					Modifiers = Modifiers.Public,
					ReturnType = new SimpleType("T"),
					Name = "CreateObject",
					TypeParameters = { new TypeParameterDeclaration { Name = "T" } },
					Constraints = {
						new Constraint {
							TypeParameter = "T",
							BaseTypes = { new SimpleType("ICloneable") }
						}
					}});
		}
		
		[Test]
		public void DelegateDeclarationInNamespace()
		{
			string program = "namespace N { delegate void MyDelegate(); }";
			NamespaceDeclaration nd = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			Assert.AreEqual("MyDelegate", ((DelegateDeclaration)nd.Members.Single()).Name);
		}
		
		[Test]
		public void DelegateDeclarationInClass()
		{
			string program = "class Outer { delegate void Inner(); }";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			Assert.AreEqual("Inner", ((DelegateDeclaration)td.Members.Single()).Name);
		}
	}
}
