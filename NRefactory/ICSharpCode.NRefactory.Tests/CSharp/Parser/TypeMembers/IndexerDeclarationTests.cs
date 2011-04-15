// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture]
	public class IndexerDeclarationTests
	{
		[Test]
		public void IndexerDeclarationTest()
		{
			IndexerDeclaration id = ParseUtilCSharp.ParseTypeMember<IndexerDeclaration>("public int this[int a, string b] { get { } protected set { } }");
			Assert.AreEqual(2, id.Parameters.Count());
			Assert.IsNotNull(id.Getter, "No get region found!");
			Assert.IsNotNull(id.Setter, "No set region found!");
			Assert.AreEqual(Modifiers.Public, id.Modifiers);
			Assert.AreEqual(Modifiers.None, id.Getter.Modifiers);
			Assert.AreEqual(Modifiers.Protected, id.Setter.Modifiers);
		}
		
		[Test]
		public void IndexerImplementingInterfaceTest()
		{
			IndexerDeclaration id = ParseUtilCSharp.ParseTypeMember<IndexerDeclaration>("int MyInterface.this[int a, string b] { get { } set { } }");
			Assert.AreEqual(2, id.Parameters.Count());
			Assert.IsNotNull(id.Getter, "No get region found!");
			Assert.IsNotNull(id.Setter, "No set region found!");
			
			Assert.AreEqual("MyInterface", ((SimpleType)id.PrivateImplementationType).Identifier);
		}
		
		[Test]
		public void IndexerImplementingGenericInterfaceTest()
		{
			ParseUtilCSharp.AssertTypeMember(
				"int MyInterface<string>.this[int a, string b] { get { } [Attr] set { } }",
				new IndexerDeclaration {
					ReturnType = new PrimitiveType("int"),
					PrivateImplementationType = new SimpleType {
						Identifier = "MyInterface",
						TypeArguments = { new PrimitiveType("string") }
					},
					Parameters = {
						new ParameterDeclaration(new PrimitiveType("int"), "a"),
						new ParameterDeclaration(new PrimitiveType("string"), "b")
					},
					Getter = new Accessor { Body = new BlockStatement() },
					Setter = new Accessor {
						Attributes = { new AttributeSection(new Attribute { Type = new SimpleType("Attr") }) },
						Body = new BlockStatement()
					}});
		}
	}
}
