// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;

using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.PrettyPrinter;

using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.PrettyPrinter
{
	[TestFixture]
	public class SpecialOutputVisitorTest
	{
		void TestProgram(string program)
		{
			VBParser parser = ParserFactory.CreateParser(new StringReader(program));
			parser.Parse();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			VBNetOutputVisitor outputVisitor = new VBNetOutputVisitor();
			outputVisitor.Options.IndentationChar = ' ';
			outputVisitor.Options.TabSize = 2;
			outputVisitor.Options.IndentSize = 2;
			using (SpecialNodesInserter.Install(parser.Lexer.SpecialTracker.RetrieveSpecials(),
			                                    outputVisitor)) {
				outputVisitor.VisitCompilationUnit(parser.CompilationUnit, null);
			}
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(program.Replace("\r", ""), outputVisitor.Text.TrimEnd().Replace("\r", ""));
			parser.Dispose();
		}
		
		[Test]
		public void Enum()
		{
			TestProgram("Enum Test\n" +
			              "  ' a\n" +
			              "  m1\n" +
			              "  ' b\n" +
			              "  m2\n" +
			              "  ' c\n" +
			              "End Enum\n" +
			              "' d");
		}
		
		[Test]
		public void CommentsInsideMethod()
		{
			TestProgram(@"Public Class Class1
  Private Function test(l As Integer, lvw As Integer) As Boolean
    ' Begin
    Dim i As Integer = 1
    Return False
    ' End of method
  End Function
End Class");
		}
		
		[Test]
		public void BlankLines()
		{
			TestProgram("Imports System\n" +
			              "\n" +
			              "Imports System.IO");
			TestProgram("Imports System\n" +
			              "\n" +
			              "\n" +
			              "Imports System.IO");
			TestProgram("\n" +
			              "' Some comment\n" +
			              "\n" +
			              "Imports System.IO");
		}
	}
}
