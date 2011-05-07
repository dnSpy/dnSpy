// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class ImportsStatementTests
	{
		[Test]
		public void InvalidImportsStatement()
		{
			string program = "Imports\n";
			ParseUtil.ParseGlobal<ImportsStatement>(program, true);
		}
		
		[Test]
		public void InvalidImportsStatement2()
		{
			string program = "Imports ,\n";
			ParseUtil.ParseGlobal<ImportsStatement>(program, true);
		}
		
		[Test]
		public void SimpleImportsStatement()
		{
			string program = "Imports System\n";
			
			var clause1 = new MemberImportsClause {
				Member = new SimpleType("System")
			};
			
			var node = new ImportsStatement();
			node.AddChild(clause1, ImportsStatement.ImportsClauseRole);
			
			ParseUtil.AssertGlobal(program, node);
		}
		
		[Test]
		public void QualifiedTypeImportsStatement()
		{
			string program = "Imports My.Name.Space\n";
			
			var clause2 = new MemberImportsClause {
				Member = new QualifiedType(new QualifiedType(new SimpleType("My"), new Identifier("Name", AstLocation.Empty)), new Identifier("Space", AstLocation.Empty))
			};
			
			var node = new ImportsStatement();
			node.AddChild(clause2, ImportsStatement.ImportsClauseRole);
			
			ParseUtil.AssertGlobal(program, node);
		}
//
//		[Test]
//		public void VBNetUsingAliasDeclarationTest()
//		{
//			string program = "Imports TESTME=System\n" +
//				"Imports myAlias=My.Name.Space\n" +
//				"Imports StringCollection = System.Collections.Generic.List(Of string)\n";
//			VBParser parser = ParserFactory.CreateParser(new StringReader(program));
//			parser.Parse();
//
//			Assert.AreEqual("", parser.Errors.ErrorOutput);
//			CheckAliases(parser.CompilationUnit);
//		}
//
//		[Test]
//		public void VBNetComplexUsingAliasDeclarationTest()
//		{
//			string program = "Imports NS1, AL=NS2, NS3, AL2=NS4, NS5\n";
//			VBParser parser = ParserFactory.CreateParser(new StringReader(program));
//			parser.Parse();
//
//			Assert.AreEqual("", parser.Errors.ErrorOutput);
//			// TODO : Extend test ...
//		}
//
//		[Test]
//		public void VBNetXmlNamespaceUsingTest()
//		{
//			string program = "Imports <xmlns=\"http://icsharpcode.net/sharpdevelop/avalonedit\">";
//			VBParser parser = ParserFactory.CreateParser(new StringReader(program));
//			parser.Parse();
//
//			Assert.AreEqual("", parser.Errors.ErrorOutput);
//			CompilationUnit unit = parser.CompilationUnit;
//
//			Assert.AreEqual(1, unit.Children.Count);
//			Assert.IsTrue(unit.Children[0] is ImportsStatement);
//			ImportsStatement ud = (ImportsStatement)unit.Children[0];
//			Assert.AreEqual(1, ud.ImportsClauses.Count);
//			Assert.IsFalse(ud.ImportsClauses[0].IsAlias);
//			Assert.IsTrue(ud.ImportsClauses[0].IsXml);
//
//			Assert.AreEqual("xmlns", ud.ImportsClauses[0].XmlPrefix);
//			Assert.AreEqual("http://icsharpcode.net/sharpdevelop/avalonedit", ud.ImportsClauses[0].Name);
//		}
//
//		[Test]
//		public void VBNetXmlNamespaceWithPrefixUsingTest()
//		{
//			string program = "Imports <xmlns:avalonedit=\"http://icsharpcode.net/sharpdevelop/avalonedit\">";
//			VBParser parser = ParserFactory.CreateParser(new StringReader(program));
//			parser.Parse();
//
//			Assert.AreEqual("", parser.Errors.ErrorOutput);
//			CompilationUnit unit = parser.CompilationUnit;
//
//			Assert.AreEqual(1, unit.Children.Count);
//			Assert.IsTrue(unit.Children[0] is ImportsStatement);
//			ImportsStatement ud = (ImportsStatement)unit.Children[0];
//			Assert.AreEqual(1, ud.ImportsClauses.Count);
//			Assert.IsFalse(ud.ImportsClauses[0].IsAlias);
//			Assert.IsTrue(ud.ImportsClauses[0].IsXml);
//
//			Assert.AreEqual("xmlns:avalonedit", ud.ImportsClauses[0].XmlPrefix);
//			Assert.AreEqual("http://icsharpcode.net/sharpdevelop/avalonedit", ud.ImportsClauses[0].Name);
//		}
//
//		[Test]
//		public void VBNetXmlNamespaceSingleQuotedUsingTest()
//		{
//			string program = "Imports <xmlns='http://icsharpcode.net/sharpdevelop/avalonedit'>";
//			VBParser parser = ParserFactory.CreateParser(new StringReader(program));
//			parser.Parse();
//
//			Assert.AreEqual("", parser.Errors.ErrorOutput);
//			CompilationUnit unit = parser.CompilationUnit;
//
//			Assert.AreEqual(1, unit.Children.Count);
//			Assert.IsTrue(unit.Children[0] is ImportsStatement);
//			ImportsStatement ud = (ImportsStatement)unit.Children[0];
//			Assert.AreEqual(1, ud.ImportsClauses.Count);
//			Assert.IsFalse(ud.ImportsClauses[0].IsAlias);
//			Assert.IsTrue(ud.ImportsClauses[0].IsXml);
//
//			Assert.AreEqual("xmlns", ud.ImportsClauses[0].XmlPrefix);
//			Assert.AreEqual("http://icsharpcode.net/sharpdevelop/avalonedit", ud.ImportsClauses[0].Name);
//		}
//
//		[Test]
//		public void VBNetXmlNamespaceSingleQuotedWithPrefixUsingTest()
//		{
//			string program = "Imports <xmlns:avalonedit='http://icsharpcode.net/sharpdevelop/avalonedit'>";
//			VBParser parser = ParserFactory.CreateParser(new StringReader(program));
//			parser.Parse();
//
//			Assert.AreEqual("", parser.Errors.ErrorOutput);
//			CompilationUnit unit = parser.CompilationUnit;
//
//			Assert.AreEqual(1, unit.Children.Count);
//			Assert.IsTrue(unit.Children[0] is ImportsStatement);
//			ImportsStatement ud = (ImportsStatement)unit.Children[0];
//			Assert.AreEqual(1, ud.ImportsClauses.Count);
//			Assert.IsFalse(ud.ImportsClauses[0].IsAlias);
//			Assert.IsTrue(ud.ImportsClauses[0].IsXml);
//
//			Assert.AreEqual("xmlns:avalonedit", ud.ImportsClauses[0].XmlPrefix);
//			Assert.AreEqual("http://icsharpcode.net/sharpdevelop/avalonedit", ud.ImportsClauses[0].Name);
//		}
//		#endregion
	}
}
