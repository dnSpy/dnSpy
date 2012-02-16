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
using System.IO;
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
		
		[Test]
		public void ExternAliasTest()
		{
			string program = "extern alias X; extern alias Y; using X::System; namespace TestNamespace { extern alias Z; using Y::System; }";
			var cu = new CSharpParser().Parse(new StringReader(program), "code.cs");
			Assert.AreEqual(
				new Type[] {
					typeof(ExternAliasDeclaration),
					typeof(ExternAliasDeclaration),
					typeof(UsingDeclaration),
					typeof(NamespaceDeclaration)
				}, cu.Children.Select(c => c.GetType()).ToArray());
			var namespaceMembers = ((NamespaceDeclaration)cu.LastChild).Members;
			Assert.AreEqual(
				new Type[] {
					typeof(ExternAliasDeclaration),
					typeof(UsingDeclaration)
				}, namespaceMembers.Select(c => c.GetType()).ToArray());
		}
	}
}
