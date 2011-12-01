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
					Name = "this",
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
