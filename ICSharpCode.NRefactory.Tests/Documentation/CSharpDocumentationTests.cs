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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Xml;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Documentation
{
	[TestFixture]
	public class CSharpDocumentationTests
	{
		ICompilation compilation;
		ITypeDefinition typeDefinition;
		
		void Init(string program)
		{
			var pc = new CSharpProjectContent().AddAssemblyReferences(new[] { CecilLoaderTests.Mscorlib });
			var cu = new CSharpParser().Parse(new StringReader(program), "program.cs");
			compilation = pc.UpdateProjectContent(null, cu.ToTypeSystem()).CreateCompilation();
			typeDefinition = compilation.MainAssembly.TopLevelTypeDefinitions.FirstOrDefault();
		}
		
		[Test]
		public void TypeDocumentationLookup()
		{
			Init(@"using System;
/// <summary/>
class Test { }");
			Assert.AreEqual("<summary/>", typeDefinition.Documentation.ToString());
		}
		
		[Test]
		public void TypeDocumentationLookup2()
		{
			Init(@"using System;
/// <summary>
/// Documentation
/// </summary>
class Test { }");
			Assert.AreEqual("<summary>" + Environment.NewLine + "Documentation" + Environment.NewLine + "</summary>", typeDefinition.Documentation.ToString());
		}
		
		[Test]
		public void TypeDocumentationLookupWithIndentation()
		{
			Init(@"using System;
/// <summary>
///   Documentation
/// </summary>
class Test { }");
			Assert.AreEqual("<summary>" + Environment.NewLine + "  Documentation" + Environment.NewLine + "</summary>", typeDefinition.Documentation.ToString());
		}
		
		[Test]
		public void MultilineDocumentation()
		{
			Init(@"using System;
/** <summary>Documentation</summary> */
class Test { }");
			Assert.AreEqual("<summary>Documentation</summary> ", typeDefinition.Documentation.ToString());
		}
		
		[Test]
		public void MultilineDocumentation2()
		{
			Init(@"using System;
/**
<summary>
  Documentation
</summary>
*/
class Test { }");
			Assert.AreEqual("<summary>" + Environment.NewLine + "  Documentation" + Environment.NewLine + "</summary>", typeDefinition.Documentation.ToString());
		}
		
		[Test]
		public void MultilineDocumentationCommonPattern()
		{
			Init(@"using System;
/**
 * <summary>
 *   Documentation
 * </summary>*/
class Test { }");
			Assert.AreEqual("<summary>" + Environment.NewLine + "  Documentation" + Environment.NewLine + "</summary>", typeDefinition.Documentation.ToString());
		}
		
		[Test]
		public void MultilineDocumentationNoCommonPattern()
		{
			Init(@"using System;
/**
   <summary>
 *   Documentation
 */
class Test { }");
			Assert.AreEqual("   <summary>" + Environment.NewLine + " *   Documentation", typeDefinition.Documentation.ToString());
		}
		
		[Test]
		public void InheritedDocumentation()
		{
			Init(@"using System;
class Derived : Base {
	/// <summary>Overridden summary</summary><inheritdoc/>
	public override void Method();
}
class Base {
	/// <summary>Base summary</summary><remarks>Base remarks</remarks>
	public virtual void Method();
}
");
			var element = XmlDocumentationElement.Get(typeDefinition.Methods.Single(m => m.Name == "Method"));
			Assert.AreEqual(2, element.Children.Count());
			Assert.AreEqual("summary", element.Children[0].Name);
			Assert.AreEqual("remarks", element.Children[1].Name);
			Assert.AreEqual("Overridden summary", element.Children[0].TextContent);
			Assert.AreEqual("Base remarks", element.Children[1].TextContent);
		}
	}
}
