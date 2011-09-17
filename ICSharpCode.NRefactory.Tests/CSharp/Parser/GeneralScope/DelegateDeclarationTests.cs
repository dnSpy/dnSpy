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
