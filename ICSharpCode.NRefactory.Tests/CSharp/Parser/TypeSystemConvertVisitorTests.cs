// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)


using System;
using System.IO;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser
{
	[TestFixture]
	public class TypeSystemConvertVisitorTests : TypeSystemTests
	{
		ITypeResolveContext ctx = CecilLoaderTests.Mscorlib;
		
		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			const string fileName = "TypeSystemTests.TestCase.cs";
			
			CSharpParser parser = new CSharpParser();
			CompilationUnit cu;
			using (Stream s = typeof(TypeSystemTests).Assembly.GetManifestResourceStream(typeof(TypeSystemTests), fileName)) {
				cu = parser.Parse(s);
			}
			
			testCasePC = new SimpleProjectContent();
			TypeSystemConvertVisitor visitor = new TypeSystemConvertVisitor(testCasePC, fileName);
			cu.AcceptVisitor(visitor, null);
			ParsedFile parsedFile = visitor.ParsedFile;
			((SimpleProjectContent)testCasePC).UpdateProjectContent(null, parsedFile);
		}
	}
}
