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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class FindReferencesTest
	{
		CompilationUnit compilationUnit;
		CSharpParsedFile parsedFile;
		ICompilation compilation;
		
		void Init(string code)
		{
			compilationUnit = new CSharpParser().Parse(new StringReader(code), "test.cs");
			parsedFile = compilationUnit.ToTypeSystem();
			compilation = TypeSystemHelper.CreateCompilation(parsedFile);
		}
		
		AstNode[] FindReferences(IEntity entity)
		{
			var result = new List<AstNode>();
			var findReferences = new FindReferences();
			var searchScopes = findReferences.GetSearchScopes(entity);
			findReferences.FindReferencesInFile(searchScopes, parsedFile, compilationUnit, compilation,
			                                    (node, rr) => result.Add(node), CancellationToken.None);
			return result.OrderBy(n => n.StartLocation).ToArray();
		}
		
		[Test]
		public void FindMethodGroupReference()
		{
			Init(@"using System;
class Test {
  Action<int> x = M;
  Action<string> y = M;
  static void M(int a) {}
  static void M(string a) {}
}");
			var test = compilation.MainAssembly.TopLevelTypeDefinitions.Single();
			var m_int = test.Methods.Single(m => m.Name == "M" && m.Parameters.Single().Type.Name == "Int32");
			var m_string = test.Methods.Single(m => m.Name == "M" && m.Parameters.Single().Type.Name == "String");
			Assert.AreEqual(new int[] { 3, 5 }, FindReferences(m_int).Select(n => n.StartLocation.Line).ToArray());
			Assert.AreEqual(new int[] { 4, 6 }, FindReferences(m_string).Select(n => n.StartLocation.Line).ToArray());
		}
		
		[Test]
		public void FindMethodGroupReferenceInOtherMethodCall()
		{
			Init(@"using System;
class Test {
 static void T(Action<int> a, Action<string> b) {
  this.T(M, M);
 }
 static void M(int a) {}
 static void M(string a) {}
}");
			var test = compilation.MainAssembly.TopLevelTypeDefinitions.Single();
			var m_int = test.Methods.Single(m => m.Name == "M" && m.Parameters.Single().Type.Name == "Int32");
			var m_string = test.Methods.Single(m => m.Name == "M" && m.Parameters.Single().Type.Name == "String");
			Assert.AreEqual(new [] { new TextLocation(4, 10), new TextLocation(6, 2) },
			                FindReferences(m_int).Select(n => n.StartLocation).ToArray());
			Assert.AreEqual(new [] { new TextLocation(4, 13), new TextLocation(7, 2) },
			                FindReferences(m_string).Select(n => n.StartLocation).ToArray());
		}
		
		[Test]
		public void FindMethodGroupReferenceInExplicitDelegateCreation()
		{
			Init(@"using System;
class Test {
 static void T(Action<int> a, Action<string> b) {
  this.T(new Action<int>(M), new Action<string>(M));
 }
 static void M(int a) {}
 static void M(string a) {}
}");
			var test = compilation.MainAssembly.TopLevelTypeDefinitions.Single();
			var m_int = test.Methods.Single(m => m.Name == "M" && m.Parameters.Single().Type.Name == "Int32");
			var m_string = test.Methods.Single(m => m.Name == "M" && m.Parameters.Single().Type.Name == "String");
			Assert.AreEqual(new [] { new TextLocation(4, 26), new TextLocation(6, 2) },
			                FindReferences(m_int).Select(n => n.StartLocation).ToArray());
			Assert.AreEqual(new [] { new TextLocation(4, 49), new TextLocation(7, 2) },
			                FindReferences(m_string).Select(n => n.StartLocation).ToArray());
		}
	}
}
