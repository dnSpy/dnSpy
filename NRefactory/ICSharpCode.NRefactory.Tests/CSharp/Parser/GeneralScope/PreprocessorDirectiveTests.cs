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
	public class PreprocessorDirectiveTests
	{
		[Test]
		public void InactiveIf()
		{
			string program = @"namespace NS {
	#if SOMETHING
	class A {}
	#endif
}";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			Assert.AreEqual(0, ns.Members.Count);
			
			Assert.AreEqual(new Role[] {
			                	Roles.NamespaceKeyword,
			                	Roles.Identifier,
			                	Roles.LBrace,
			                	Roles.PreProcessorDirective,
			                	Roles.Comment,
			                	Roles.PreProcessorDirective,
			                	Roles.RBrace
			                }, ns.Children.Select(c => c.Role).ToArray());
			
			var pp = ns.GetChildrenByRole(Roles.PreProcessorDirective);
			
			Assert.AreEqual(PreProcessorDirectiveType.If, pp.First().Type);
			Assert.IsFalse(pp.First().Take);
			Assert.AreEqual("SOMETHING", pp.First().Argument);
			Assert.AreEqual(new TextLocation(2, 2), pp.First().StartLocation);
			Assert.AreEqual(new TextLocation(2, 15), pp.First().EndLocation);
			
			var comment = ns.GetChildByRole(Roles.Comment);
			Assert.AreEqual(CommentType.InactiveCode, comment.CommentType);
			Assert.AreEqual(new TextLocation(3, 1), comment.StartLocation);
			Assert.AreEqual(new TextLocation(4, 2), comment.EndLocation);
			Assert.AreEqual("\tclass A {}\n\t", comment.Content.Replace("\r", ""));
			
			Assert.AreEqual(PreProcessorDirectiveType.Endif, pp.Last().Type);
			Assert.AreEqual(string.Empty, pp.Last().Argument);
			Assert.AreEqual(new TextLocation(4, 2), pp.Last().StartLocation);
			Assert.AreEqual(new TextLocation(4, 8), pp.Last().EndLocation);
		}
		
		[Ignore("Fixme!")]
		[Test]
		public void NestedInactiveIf()
		{
			string program = @"namespace NS {
	#if SOMETHING
	class A {
		#if B
		void M() {}
		#endif
	}
	#endif
}";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			Assert.AreEqual(0, ns.Members.Count);
			
			Assert.AreEqual(new Role[] {
			                	Roles.NamespaceKeyword,
			                	Roles.Identifier,
			                	Roles.LBrace,
			                	Roles.PreProcessorDirective,
			                	Roles.Comment,
			                	Roles.PreProcessorDirective,
			                	Roles.Comment,
			                	Roles.PreProcessorDirective,
			                	Roles.Comment,
			                	Roles.PreProcessorDirective,
			                	Roles.RBrace
			                }, ns.Children.Select(c => c.Role).ToArray());
		}
		
		[Ignore("Fixme!")]
		[Test]
		public void CommentOnEndOfIfDirective()
		{
			string program = @"namespace NS {
	#if SOMETHING // comment
	class A { }
	#endif
}";
			NamespaceDeclaration ns = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			Assert.AreEqual(0, ns.Members.Count);
			
			Assert.AreEqual(new Role[] {
			                	Roles.NamespaceKeyword,
			                	Roles.Identifier,
			                	Roles.LBrace,
			                	Roles.PreProcessorDirective,
			                	Roles.Comment,
			                	Roles.Comment,
			                	Roles.PreProcessorDirective,
			                	Roles.RBrace
			                }, ns.Children.Select(c => c.Role).ToArray());
			Assert.AreEqual(CommentType.SingleLine, ns.GetChildrenByRole(Roles.Comment).First().CommentType);
			Assert.AreEqual(CommentType.InactiveCode, ns.GetChildrenByRole(Roles.Comment).Last().CommentType);
		}
		
		[Ignore("Fixme!")]
		[Test]
		public void PragmaWarning()
		{
			string program = "#pragma warning disable 809";
			var ppd = ParseUtilCSharp.ParseGlobal<PreProcessorDirective>(program);
			Assert.AreEqual(PreProcessorDirectiveType.Pragma, ppd.Type);
			Assert.AreEqual("warning disable 809", ppd.Argument);
		}
		
		const string elifProgram = @"
#if AAA
class A { }
#elif BBB
class B { }
#endif";
		
		[Test]
		[Ignore("parser bug (missing comment node)")]
		public void ElifBothFalse()
		{
			CSharpParser parser = new CSharpParser();
			var syntaxTree = parser.Parse(elifProgram, "elif.cs");
			Assert.IsFalse(parser.HasErrors);
			
			Assert.AreEqual(new Role[] {
			                	Roles.PreProcessorDirective,
			                	Roles.Comment,
			                	Roles.PreProcessorDirective,
			                	Roles.Comment,
			                	Roles.PreProcessorDirective
			                }, syntaxTree.Children.Select(c => c.Role).ToArray());
			var aaa = syntaxTree.GetChildrenByRole(Roles.PreProcessorDirective).ElementAt(0);
			Assert.IsFalse(aaa.Take);
			Assert.AreEqual(PreProcessorDirectiveType.If, aaa.Type);
			Assert.AreEqual("AAA", aaa.Argument);
			
			var bbb = syntaxTree.GetChildrenByRole(Roles.PreProcessorDirective).ElementAt(1);
			Assert.IsFalse(bbb.Take);
			Assert.AreEqual(PreProcessorDirectiveType.Elif, bbb.Type);
			Assert.AreEqual("BBB", bbb.Argument);
		}
		
		[Test]
		[Ignore("parser bug (bbb.Take is true, should be false)")]
		public void ElifBothTrue()
		{
			CSharpParser parser = new CSharpParser();
			parser.CompilerSettings.ConditionalSymbols.Add("AAA");
			var syntaxTree = parser.Parse(elifProgram, "elif.cs");
			Assert.IsFalse(parser.HasErrors);
			
			Assert.AreEqual(new Role[] {
			                	Roles.PreProcessorDirective,
			                	NamespaceDeclaration.MemberRole,
			                	Roles.PreProcessorDirective,
			                	Roles.Comment,
			                	Roles.PreProcessorDirective
			                }, syntaxTree.Children.Select(c => c.Role).ToArray());
			var aaa = syntaxTree.GetChildrenByRole(Roles.PreProcessorDirective).ElementAt(0);
			Assert.IsTrue(aaa.Take);
			Assert.AreEqual(PreProcessorDirectiveType.If, aaa.Type);
			Assert.AreEqual("AAA", aaa.Argument);
			
			var bbb = syntaxTree.GetChildrenByRole(Roles.PreProcessorDirective).ElementAt(1);
			Assert.IsFalse(bbb.Take);
			Assert.AreEqual(PreProcessorDirectiveType.Elif, bbb.Type);
			Assert.AreEqual("BBB", bbb.Argument);
		}
		
		[Test]
		[Ignore("parser bug (bbb.Take is true, should be false)")]
		public void ElifFirstTaken()
		{
			CSharpParser parser = new CSharpParser();
			parser.CompilerSettings.ConditionalSymbols.Add("AAA");
			var syntaxTree = parser.Parse(elifProgram, "elif.cs");
			Assert.IsFalse(parser.HasErrors);
			
			Assert.AreEqual(new Role[] {
			                	Roles.PreProcessorDirective,
			                	NamespaceDeclaration.MemberRole,
			                	Roles.PreProcessorDirective,
			                	Roles.Comment,
			                	Roles.PreProcessorDirective
			                }, syntaxTree.Children.Select(c => c.Role).ToArray());
			var aaa = syntaxTree.GetChildrenByRole(Roles.PreProcessorDirective).ElementAt(0);
			Assert.IsTrue(aaa.Take);
			Assert.AreEqual(PreProcessorDirectiveType.If, aaa.Type);
			Assert.AreEqual("AAA", aaa.Argument);
			
			var bbb = syntaxTree.GetChildrenByRole(Roles.PreProcessorDirective).ElementAt(1);
			Assert.IsFalse(bbb.Take);
			Assert.AreEqual(PreProcessorDirectiveType.Elif, bbb.Type);
			Assert.AreEqual("BBB", bbb.Argument);
		}
		
		[Test]
		public void ElifSecondTaken()
		{
			CSharpParser parser = new CSharpParser();
			parser.CompilerSettings.ConditionalSymbols.Add("BBB");
			var syntaxTree = parser.Parse(elifProgram, "elif.cs");
			Assert.IsFalse(parser.HasErrors);
			
			Assert.AreEqual(new Role[] {
			                	Roles.PreProcessorDirective,
			                	Roles.Comment,
			                	Roles.PreProcessorDirective,
			                	NamespaceDeclaration.MemberRole,
			                	Roles.PreProcessorDirective
			                }, syntaxTree.Children.Select(c => c.Role).ToArray());
			var aaa = syntaxTree.GetChildrenByRole(Roles.PreProcessorDirective).ElementAt(0);
			Assert.IsFalse(aaa.Take);
			Assert.AreEqual(PreProcessorDirectiveType.If, aaa.Type);
			Assert.AreEqual("AAA", aaa.Argument);
			
			var bbb = syntaxTree.GetChildrenByRole(Roles.PreProcessorDirective).ElementAt(1);
			Assert.IsTrue(bbb.Take);
			Assert.AreEqual(PreProcessorDirectiveType.Elif, bbb.Type);
			Assert.AreEqual("BBB", bbb.Argument);
		}
		
		[Test]
		[Ignore("parser bug (BBB is missing)")]
		public void ConditionalSymbolTest()
		{
			const string program = @"// Test
#if AAA
#undef AAA
#define CCC
#else
#define DDD
#endif
class C {}";
			CSharpParser parser = new CSharpParser();
			parser.CompilerSettings.ConditionalSymbols.Add("AAA");
			parser.CompilerSettings.ConditionalSymbols.Add("BBB");
			var syntaxTree = parser.Parse(program, "elif.cs");
			Assert.AreEqual(new[] { "BBB", "CCC" }, syntaxTree.ConditionalSymbols);
		}
	}
}
