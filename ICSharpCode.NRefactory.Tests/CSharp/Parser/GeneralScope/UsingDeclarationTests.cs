// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.GeneralScope
{
	[TestFixture]
	public class UsingDeclarationTests
	{
		[Test]
		[Ignore("error reporting not yet implemented")]
		public void WrongUsingTest()
		{
			string program = "using\n";
			CSharpParser parser = new CSharpParser();
			CompilationUnit cu = parser.Parse(new StringReader(program));
			Assert.AreEqual(0, cu.Children.Count());
			Assert.IsTrue(parser.HasErrors);
		}
		
		[Test]
		public void DeclarationTest()
		{
			string program = "using System;\n" +
				"using My.Name.Space;\n";
			CSharpParser parser = new CSharpParser();
			CompilationUnit cu = parser.Parse(new StringReader(program));
			Assert.IsFalse(parser.HasErrors);
			
			Assert.AreEqual(2, cu.Children.Count());
			Assert.IsTrue(cu.Children.ElementAt(0) is UsingDeclaration);
			Assert.IsFalse(cu.Children.ElementAt(0) is UsingAliasDeclaration);
			UsingDeclaration ud = (UsingDeclaration)cu.Children.ElementAt(0);
			Assert.AreEqual("System", ud.Namespace);
			
			Assert.IsTrue(cu.Children.ElementAt(1) is UsingDeclaration);
			Assert.IsFalse(cu.Children.ElementAt(1) is UsingAliasDeclaration);
			ud = (UsingDeclaration)cu.Children.ElementAt(1);
			Assert.AreEqual("My.Name.Space", ud.Namespace);
		}
		
		[Test]
		public void UsingAliasDeclarationTest()
		{
			string program = "using TESTME=System;\n" +
				"using myAlias=My.Name.Space;\n" +
				"using StringCollection = System.Collections.Generic.List<string>;\n";
			CSharpParser parser = new CSharpParser();
			CompilationUnit cu = parser.Parse(new StringReader(program));
			Assert.IsFalse(parser.HasErrors);
			
			Assert.AreEqual(3, cu.Children.Count());
			
			Assert.IsTrue(cu.Children.ElementAt(0) is UsingAliasDeclaration);
			UsingAliasDeclaration ud = (UsingAliasDeclaration)cu.Children.ElementAt(0);
			Assert.AreEqual("TESTME", ud.Alias);
			Assert.AreEqual("System", ud.Import.ToString());
			
			Assert.IsTrue(cu.Children.ElementAt(1) is UsingAliasDeclaration);
			ud = (UsingAliasDeclaration)cu.Children.ElementAt(1);
			Assert.AreEqual("myAlias", ud.Alias);
			Assert.AreEqual("My.Name.Space", ud.Import.ToString());
			
			Assert.IsTrue(cu.Children.ElementAt(2) is UsingAliasDeclaration);
			ud = (UsingAliasDeclaration)cu.Children.ElementAt(2);
			Assert.AreEqual("StringCollection", ud.Alias);
			Assert.AreEqual("System.Collections.Generic.List<string>", ud.Import.ToString());
		}
		
		[Test]
		public void UsingWithAliasing()
		{
			string program = "using global::System;\n" +
				"using myAlias=global::My.Name.Space;\n" +
				"using a::b.c;\n";
			CSharpParser parser = new CSharpParser();
			CompilationUnit cu = parser.Parse(new StringReader(program));
			Assert.IsFalse(parser.HasErrors);
			
			Assert.AreEqual(3, cu.Children.Count());
			
			Assert.IsTrue(cu.Children.ElementAt(0) is UsingDeclaration);
			Assert.IsFalse(cu.Children.ElementAt(0) is UsingAliasDeclaration);
			UsingDeclaration ud = (UsingDeclaration)cu.Children.ElementAt(0);
			Assert.AreEqual("global::System", ud.Namespace);
			
			Assert.IsTrue(cu.Children.ElementAt(1) is UsingAliasDeclaration);
			UsingAliasDeclaration uad = (UsingAliasDeclaration)cu.Children.ElementAt(1);
			Assert.AreEqual("myAlias", uad.Alias);
			Assert.AreEqual("global::My.Name.Space", uad.Import.ToString());
			
			Assert.IsTrue(cu.Children.ElementAt(2) is UsingDeclaration);
			Assert.IsFalse(cu.Children.ElementAt(2) is UsingAliasDeclaration);
			ud = (UsingDeclaration)cu.Children.ElementAt(2);
			Assert.AreEqual("a::b.c", ud.Namespace);
		}
	}
}
