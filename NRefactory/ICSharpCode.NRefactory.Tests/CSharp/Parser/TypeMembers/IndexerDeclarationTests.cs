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
			IndexerDeclaration id = ParseUtilCSharp.ParseTypeMember<IndexerDeclaration>("int this[int a, string b] { get { } set { } }");
			Assert.AreEqual(2, id.Parameters.Count());
			Assert.IsNotNull(id.Getter, "No get region found!");
			Assert.IsNotNull(id.Setter, "No set region found!");
		}
		
		[Test, Ignore("type reference is not yet implemented")]
		public void IndexerImplementingInterfaceTest()
		{
			IndexerDeclaration id = ParseUtilCSharp.ParseTypeMember<IndexerDeclaration>("int MyInterface.this[int a, string b] { get { } set { } }");
			Assert.AreEqual(2, id.Parameters.Count());
			Assert.IsNotNull(id.Getter, "No get region found!");
			Assert.IsNotNull(id.Setter, "No set region found!");
			
			Assert.AreEqual("MyInterface", id.PrivateImplementationType);
		}
		
		[Test, Ignore]
		public void IndexerImplementingGenericInterfaceTest()
		{
			throw new NotImplementedException();
			/*
			IndexerDeclaration id = ParseUtilCSharp.ParseTypeMember<IndexerDeclaration>("int MyInterface<string>.this[int a, string b] { get { } set { } }");
			Assert.AreEqual(2, id.Parameters.Count);
			Assert.IsNotNull(id.GetAccessor, "No get region found!");
			Assert.IsNotNull(id.SetAccessor, "No set region found!");
			
			Assert.AreEqual("MyInterface", id.InterfaceImplementations[0].InterfaceType.Type);
			Assert.AreEqual("System.String", id.InterfaceImplementations[0].InterfaceType.GenericTypes[0].Type);*/
		}
	}
}
