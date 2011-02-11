// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.GeneralScope
{
	[TestFixture]
	public class NamespaceDeclarationTests
	{
		[Test]
		public void SimpleNamespaceTest()
		{
			string program = "namespace TestNamespace {\n" +
				"}\n";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			Assert.AreEqual("TestNamespace", ns.Name);
		}
		
		[Test]
		public void NestedNamespaceTest()
		{
			string program = "namespace N1 {//TestNamespace\n" +
				"    namespace N2 {// Declares a namespace named N2 within N1.\n" +
				"    }\n" +
				"}\n";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			
			Assert.AreEqual("N1", ns.Name);
			
			Assert.AreEqual("N2", ns.Children.OfType<NamespaceDeclaration>().Single().Name);
		}
	}
}
