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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	[TestFixture]
	public class CyclicProjectDependency
	{
		IProjectContent pc1;
		IProjectContent pc2;
		ISolutionSnapshot solution;
		
		[SetUp]
		public void Setup()
		{
			pc1 = new CSharpProjectContent()
				.SetAssemblyName("PC1")
				.SetProjectFileName("PC1.csproj")
				.AddAssemblyReferences(new IAssemblyReference[] { CecilLoaderTests.Mscorlib, new ProjectReference("PC2.csproj") });

			pc2 = new CSharpProjectContent()
				.SetAssemblyName("PC2")
				.SetProjectFileName("PC2.csproj")
				.AddAssemblyReferences(new IAssemblyReference[] { CecilLoaderTests.Mscorlib, new ProjectReference("PC1.csproj") });
			
			solution = new DefaultSolutionSnapshot(new[] { pc1, pc2 });
		}
		
		[Test]
		public void CreateCompilation1()
		{
			ICompilation c = solution.GetCompilation(pc1);
			Assert.AreEqual(new string[] { "PC1", "mscorlib", "PC2" }, c.Assemblies.Select(asm => asm.AssemblyName).ToArray());
		}
		
		[Test]
		public void CreateCompilation2()
		{
			ICompilation c = solution.GetCompilation(pc2);
			Assert.AreEqual(new string[] { "PC2", "mscorlib", "PC1" }, c.Assemblies.Select(asm => asm.AssemblyName).ToArray());
		}
	}
}
