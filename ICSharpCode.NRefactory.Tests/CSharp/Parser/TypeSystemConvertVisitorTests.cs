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
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.TypeSystem.TestCase;
using ICSharpCode.NRefactory.Utils;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser
{
	[TestFixture]
	public class TypeSystemConvertVisitorTests : TypeSystemTests
	{
		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			compilation = ParseTestCase().CreateCompilation();
		}
		
		internal static IProjectContent ParseTestCase()
		{
			const string fileName = "TypeSystemTests.TestCase.cs";
			
			CSharpParser parser = new CSharpParser();
			SyntaxTree syntaxTree;
			using (Stream s = typeof(TypeSystemTests).Assembly.GetManifestResourceStream(typeof(TypeSystemTests), fileName)) {
				syntaxTree = parser.Parse(s, fileName);
			}
			
			var unresolvedFile = syntaxTree.ToTypeSystem();
			return new CSharpProjectContent()
				.AddOrUpdateFiles(unresolvedFile)
				.AddAssemblyReferences(new[] { CecilLoaderTests.Mscorlib })
				.SetAssemblyName(typeof(TypeSystemTests).Assembly.GetName().Name);
		}
		
		[Test]
		public void ConvertStandaloneTypeReference()
		{
			var typeRef = new MemberType(new SimpleType("System"), "Array").ToTypeReference();
			Assert.AreEqual(compilation.FindType(KnownTypeCode.Array), typeRef.Resolve(compilation.TypeResolveContext));
		}
		
		[Test]
		public void PartialMethodWithImplementation()
		{
			var t = compilation.FindType(typeof(PartialClass));
			var methods = t.GetMethods(m => m.Name == "PartialMethodWithImplementation").ToList();
			Assert.AreEqual(2, methods.Count);
			var method1 = methods.Single(m => m.Parameters[0].Type.FullName == "System.Int32");
			var method2 = methods.Single(m => m.Parameters[0].Type.FullName == "System.String");
			Assert.AreEqual(2, method1.Parts.Count);
			Assert.AreEqual(2, method2.Parts.Count);
		}
		
		[Test]
		public void PartialMethodWithoutImplementation()
		{
			var t = compilation.FindType(typeof(PartialClass));
			var method = t.GetMethods(m => m.Name == "PartialMethodWithoutImplementation").Single();
			Assert.AreEqual(1, method.Parts.Count);
		}
	}
	
	[TestFixture]
	public class SerializedTypeSystemConvertVisitorTests : TypeSystemTests
	{
		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			FastSerializer serializer = new FastSerializer();
			using (MemoryStream ms = new MemoryStream()) {
				serializer.Serialize(ms, TypeSystemConvertVisitorTests.ParseTestCase());
				ms.Position = 0;
				var pc = (IProjectContent)serializer.Deserialize(ms);
				compilation = pc.CreateCompilation();
			}
		}
	}
}
