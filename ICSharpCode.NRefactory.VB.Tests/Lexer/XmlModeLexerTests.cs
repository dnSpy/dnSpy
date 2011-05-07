// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Parser;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Lexer
{
	[TestFixture]
	public class XmlModeLexerTests
	{
		#region Xml Tests
		[Test]
		public void TagWithContent()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("Dim x = <Test>Hello World</Test>")));
			
			CheckHead(lexer);
			
			Assert.AreEqual(Tokens.Dim, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Assign, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlOpenTag, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlCloseTag, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlContent, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlOpenEndTag, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlCloseTag, lexer.NextToken().Kind);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void HtmlText()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("Dim x = <div><h1>Title</h1>" +
			                                                            "<p>test test <br /> test</p></div>")));
			
			CheckHead(lexer);
			
			Assert.AreEqual(Tokens.Dim, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Assign, lexer.NextToken().Kind);
			
			// <div>
			Assert.AreEqual(Tokens.XmlOpenTag, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlCloseTag, lexer.NextToken().Kind);
			
			// <h1>
			Assert.AreEqual(Tokens.XmlOpenTag, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlCloseTag, lexer.NextToken().Kind);
			
			// Title
			Assert.AreEqual(Tokens.XmlContent, lexer.NextToken().Kind);
			
			// </h1>
			Assert.AreEqual(Tokens.XmlOpenEndTag, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlCloseTag, lexer.NextToken().Kind);
			
			// <p>
			Assert.AreEqual(Tokens.XmlOpenTag, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlCloseTag, lexer.NextToken().Kind);
			
			// test test
			Assert.AreEqual(Tokens.XmlContent, lexer.NextToken().Kind);
			
			// <br />
			Assert.AreEqual(Tokens.XmlOpenTag, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlCloseTagEmptyElement, lexer.NextToken().Kind);
			
			// test
			Assert.AreEqual(Tokens.XmlContent, lexer.NextToken().Kind);
			
			// </p>
			Assert.AreEqual(Tokens.XmlOpenEndTag, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlCloseTag, lexer.NextToken().Kind);
			
			// </div>
			Assert.AreEqual(Tokens.XmlOpenEndTag, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.Identifier, lexer.NextToken().Kind);
			Assert.AreEqual(Tokens.XmlCloseTag, lexer.NextToken().Kind);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void XmlLiteralsExample1()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("Dim xml = <menu>\n" +
			                                                            "              <course name=\"appetizer\">\n" +
			                                                            "                  <dish>Shrimp Cocktail</dish>\n" +
			                                                            "                  <dish>Escargot</dish>\n" +
			                                                            "              </course>\n" +
			                                                            "              <course name=\"main\">\n" +
			                                                            "                  <dish>Filet Mignon</dish>\n" +
			                                                            "                  <dish>Garlic Potatoes</dish>\n" +
			                                                            "                  <dish>Broccoli</dish>\n" +
			                                                            "              </course>\n" +
			                                                            "              <course name=\"dessert\">\n" +
			                                                            "                  <dish>Chocolate Cheesecake</dish>\n" +
			                                                            "              </course>\n" +
			                                                            "          </menu>")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign,
			            // <menu>
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // <course name=\"appetizer\">
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // <dish>Shrimp Cocktail</dish>
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // <dish>Escargot</dish>
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // </course>
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // <course name=\"main\">
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // <dish>Filet Mignon</dish>
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // <dish>Garlic Potatoes</dish>
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // <dish>Broccoli</dish>
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // </course>
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // <course name=\"dessert\">
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // <dish>Chocolate Cheesecake</dish>
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // </course>
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            // whitespaces
			            Tokens.XmlContent,
			            // </menu>
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void SimpleXmlWithComments()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(@"Dim x = <?xml version=""1.0""?> <!-- Test file -->
			                                                                      <Test>
			                                                                        <!-- Test data -->
			                                                                        <Data />
			                                                                      </Test>
			                                                                      <!-- eof -->
			                                                                      <!-- hey, wait! -->
			                                                                      <?target some data?>")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign,
			            Tokens.XmlProcessingInstruction, Tokens.XmlContent, Tokens.XmlComment, Tokens.XmlContent, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            Tokens.XmlContent, Tokens.XmlComment, Tokens.XmlContent, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement,
			            Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlComment, Tokens.XmlComment, Tokens.XmlProcessingInstruction);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void SimpleEmptyTag()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("Dim x = <Test />")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign,
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void SimpleTag()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("Dim x = <Test></Test>")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.XmlOpenTag,
			            Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlOpenEndTag,
			            Tokens.Identifier, Tokens.XmlCloseTag);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void XmlImport()
		{
			string code = @"Imports System
Imports System.Linq

Imports <xmlns='http://icsharpcode.net/sharpdevelop/avalonedit'>
Imports <xmlns:h='http://www.w3.org/TR/html4/'>

Class TestClass
	Sub TestSub()
		Dim xml = <h:table>
					<h:tr>
						<h:td>1. Cell</h:td>
					</h:tr>
				  </h:table>
	End Sub
End Class";
			
			VBLexer lexer = GenerateLexer(new StringReader(code));
			
			CheckTokens(lexer, Tokens.Imports, Tokens.Identifier, Tokens.EOL,
			            Tokens.Imports, Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.EOL,
			            Tokens.Imports, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.XmlCloseTag, Tokens.EOL,
			            Tokens.Imports, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.XmlCloseTag, Tokens.EOL,
			            Tokens.Class, Tokens.Identifier, Tokens.EOL, Tokens.Sub, Tokens.Identifier, Tokens.OpenParenthesis, Tokens.CloseParenthesis, Tokens.EOL,
			            Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            Tokens.XmlContent, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            Tokens.XmlContent, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            Tokens.EOL, Tokens.End, Tokens.Sub, Tokens.EOL, Tokens.End, Tokens.Class
			           );
		}
		
		[Test]
		public void CDataSection()
		{
			string xml = @"Dim xml = <template>
				<name>test</name>
				<language>VB</languge>
				<file language='XAML'>
					<![CDATA[<Window x:Class='DefaultNamespace.Window1'
	xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
	xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
	Title='DefaultNamespace' Height='300' Width='300'>
	<Grid>
		
	</Grid>
</Window>]]>
				</file>
				<file language='CSharp'>
				<![CDATA[using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DefaultNamespace
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();
		}
	}
}]]>
				</file>
			</template>
			";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(xml)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign, // 2
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, // 6
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, // 10
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, // 14
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, // 18
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, // 22
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.XmlCloseTag, // 28
			            Tokens.XmlContent, Tokens.XmlCData, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag, // 34
			            Tokens.XmlContent, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.XmlCloseTag,
			            Tokens.XmlContent, Tokens.XmlCData, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag
			           );
			
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void InlineVB()
		{
			string code = @"Dim xml = <?xml version='1.0'?>
                  <menu>
                      <course name=""appetizer"">
                          <%= From m In menu _
                              Where m.Course = ""appetizer"" _
                              Select <dish><%= m.Food %></dish> _
                          %>
                      </course>
                      <course name=""main"">
                          <%= From m In menu _
                              Where m.Course = ""main"" _
                              Select <dish><%= m.Food %></dish> _
                          %>
                      </course>
                      <course name=""dessert"">
                          <%= From m In menu _
                              Where m.Course = ""dessert"" _
                              Select <dish><%= m.Food %></dish> _
                          %>
                      </course>
                  </menu>";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(code)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.XmlProcessingInstruction, Tokens.XmlContent,
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenTag, Tokens.Identifier,
			            Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlStartInlineVB,
			            Tokens.From, Tokens.Identifier, Tokens.In, Tokens.Identifier, Tokens.Where, Tokens.Identifier, Tokens.Dot,
			            Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.Select, Tokens.XmlOpenTag, Tokens.Identifier,
			            Tokens.XmlCloseTag, Tokens.XmlStartInlineVB, Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.XmlEndInlineVB,
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlEndInlineVB, Tokens.XmlContent, Tokens.XmlOpenEndTag,
			            Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenTag, Tokens.Identifier,
			            Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlStartInlineVB,
			            Tokens.From, Tokens.Identifier, Tokens.In, Tokens.Identifier, Tokens.Where, Tokens.Identifier, Tokens.Dot,
			            Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.Select, Tokens.XmlOpenTag, Tokens.Identifier,
			            Tokens.XmlCloseTag, Tokens.XmlStartInlineVB, Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.XmlEndInlineVB,
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlEndInlineVB, Tokens.XmlContent, Tokens.XmlOpenEndTag,
			            Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenTag, Tokens.Identifier,
			            Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlStartInlineVB,
			            Tokens.From, Tokens.Identifier, Tokens.In, Tokens.Identifier, Tokens.Where, Tokens.Identifier, Tokens.Dot,
			            Tokens.Identifier, Tokens.Assign, Tokens.LiteralString, Tokens.Select, Tokens.XmlOpenTag, Tokens.Identifier,
			            Tokens.XmlCloseTag, Tokens.XmlStartInlineVB, Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.XmlEndInlineVB,
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlEndInlineVB, Tokens.XmlContent, Tokens.XmlOpenEndTag,
			            Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void InlineVB2()
		{
			string code = @"Dim contact As XElement =  <<%=elementName %>>
                               <name><%= MyName %></name>
                           </>";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(code)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.As, Tokens.Identifier, Tokens.Assign, Tokens.XmlOpenTag,
			            Tokens.XmlStartInlineVB, Tokens.Identifier, Tokens.XmlEndInlineVB, Tokens.XmlCloseTag, Tokens.XmlContent,
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlStartInlineVB, Tokens.Identifier, Tokens.XmlEndInlineVB,
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag, Tokens.XmlCloseTag);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void XmlAccessOperators()
		{
			string code = @"Dim childAxis = xml.<menu>.<course>
Dim course3 = xml...<course>(2)
Dim childAxis = xml...<course>
For Each item In childAxis
    Console.WriteLine(item.@name)
Next";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(code)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.Identifier, Tokens.Dot, Tokens.XmlOpenTag,
			            Tokens.Identifier, Tokens.XmlCloseTag, Tokens.Dot, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag,
			            Tokens.EOL, Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.Identifier, Tokens.TripleDot, Tokens.XmlOpenTag,
			            Tokens.Identifier, Tokens.XmlCloseTag, Tokens.OpenParenthesis, Tokens.LiteralInteger, Tokens.CloseParenthesis,
			            Tokens.EOL, Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.Identifier, Tokens.TripleDot, Tokens.XmlOpenTag,
			            Tokens.Identifier, Tokens.XmlCloseTag, Tokens.EOL, Tokens.For, Tokens.Each, Tokens.Identifier, Tokens.In, Tokens.Identifier, Tokens.EOL,
			            Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.OpenParenthesis, Tokens.Identifier, Tokens.DotAt, Tokens.Identifier, Tokens.CloseParenthesis, Tokens.EOL,
			            Tokens.Next);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void GetXmlNamespace()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("Dim name = GetXmlNamespace(x)")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign,
			            Tokens.GetXmlNamespace, Tokens.OpenParenthesis, Tokens.Identifier, Tokens.CloseParenthesis);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void GetXmlNamespace2()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("Dim name = GetXmlNamespace(db-name)")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign,
			            Tokens.GetXmlNamespace, Tokens.OpenParenthesis, Tokens.Identifier, Tokens.CloseParenthesis);
			
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void XmlInSelect()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("Dim data = From x In list Select <test>x</test>")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign,
			            Tokens.From, Tokens.Identifier, Tokens.In, Tokens.Identifier, Tokens.Select,
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent, Tokens.XmlOpenEndTag,
			            Tokens.Identifier, Tokens.XmlCloseTag);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void IfExpressionTest()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("Dim name = If(a <> 2, 4, 8)")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign,
			            Tokens.If, Tokens.OpenParenthesis, Tokens.Identifier, Tokens.NotEqual, Tokens.LiteralInteger,
			            Tokens.Comma, Tokens.LiteralInteger, Tokens.Comma, Tokens.LiteralInteger, Tokens.CloseParenthesis);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void IfStatementTest()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("If a <> 2 Then Return")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.If, Tokens.Identifier, Tokens.NotEqual, Tokens.LiteralInteger,
			            Tokens.Then, Tokens.Return);
			
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void Bug1()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(@"Dim xml = <!-- test -->")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.XmlComment);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void Bug2()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(@"Dim xml = <?xml?><Data /><!-- test -->")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign,
			            Tokens.XmlProcessingInstruction, Tokens.XmlOpenTag, Tokens.Identifier,
			            Tokens.XmlCloseTagEmptyElement, Tokens.XmlComment);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void Bug3()
		{
			VBLexer lexer = GenerateLexerForSnippet(new StringReader("New String() {}"), SnippetType.Expression);
			
			CheckTokens(lexer, Tokens.New, Tokens.String, Tokens.OpenParenthesis,
			            Tokens.CloseParenthesis, Tokens.OpenCurlyBrace, Tokens.CloseCurlyBrace);
		}
		
		[Test]
		public void Bug4()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(@"Dim x = From kvp As KeyValuePair(Of String, DataGridViewCellStyle) In styleCache.CellStyleCache _
                            Select includeStyle(kvp.Key, kvp.Value)")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign, Tokens.From, Tokens.Identifier, Tokens.As, Tokens.Identifier,
			            Tokens.OpenParenthesis, Tokens.Of, Tokens.String, Tokens.Comma, Tokens.Identifier, Tokens.CloseParenthesis,
			            Tokens.In, Tokens.Identifier, Tokens.Dot, Tokens.Identifier,
			            Tokens.Select, Tokens.Identifier, Tokens.OpenParenthesis, Tokens.Identifier, Tokens.Dot, Tokens.Key, Tokens.Comma,
			            Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.CloseParenthesis);
						
			CheckFoot(lexer);
		}
		
		[Test]
		public void LessThanCheck()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(@"Dim xml = <!-- test --><Data")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Dim, Tokens.Identifier, Tokens.Assign,
			            Tokens.XmlComment, Tokens.LessThan, Tokens.Identifier);
			
			CheckFoot(lexer);
		}
		#endregion
		
		#region Context Tests
		[Test]
		public void MethodInvocation()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("DoSomething(<Test />, True)")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Identifier, Tokens.OpenParenthesis, Tokens.XmlOpenTag,
			            Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.Comma, Tokens.True,
			            Tokens.CloseParenthesis);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void AddHandlerStatement()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("AddHandler <Test />, True")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.AddHandler, Tokens.XmlOpenTag,
			            Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.Comma, Tokens.True);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void AddHandlerStatement2()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("AddHandler <x />, <y />")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.AddHandler, Tokens.XmlOpenTag,
			            Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.Comma, Tokens.XmlOpenTag,
			            Tokens.Identifier, Tokens.XmlCloseTagEmptyElement);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void RemoveHandlerStatement()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("RemoveHandler <x />, <Data>5</Data>")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.RemoveHandler, Tokens.XmlOpenTag,
			            Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.Comma,
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlContent,
			            Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void ErrorHandlingStatement()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("On Error Resume Next\n" +
			                                                            "On Error GoTo -1\n" +
			                                                            "On Error GoTo 0\n" +
			                                                            "On Error GoTo Test\n" +
			                                                            "Error 5\n" +
			                                                            "Error <Test />\n" +
			                                                            "Resume Next\n" +
			                                                            "Resume Label\n" +
			                                                            "Resume 4")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.On, Tokens.Error, Tokens.Resume, Tokens.Next, Tokens.EOL,
			            Tokens.On, Tokens.Error, Tokens.GoTo, Tokens.Minus, Tokens.LiteralInteger, Tokens.EOL,
			            Tokens.On, Tokens.Error, Tokens.GoTo, Tokens.LiteralInteger, Tokens.EOL,
			            Tokens.On, Tokens.Error, Tokens.GoTo, Tokens.Identifier, Tokens.EOL,
			            Tokens.Error, Tokens.LiteralInteger, Tokens.EOL,
			            Tokens.Error, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.Resume, Tokens.Next, Tokens.EOL,
			            Tokens.Resume, Tokens.Identifier, Tokens.EOL,
			            Tokens.Resume, Tokens.LiteralInteger
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void ForLoopStatement()
		{
			string statement = @"For <Test /> = <Test /> To <Test /> Step <Test />
Next <Test />, <Test />

For Each <Test /> In <Test />
Next <Test />";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.For, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement,
			            Tokens.Assign, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement,
			            Tokens.To, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement,
			            Tokens.Step, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.Next, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.Comma,
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.For, Tokens.Each, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement,
			            Tokens.In, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.Next, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void WhileLoopStatement()
		{
			string statement = @"While <Test />
End While";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.While, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.End, Tokens.While
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void WhileLoopStatement2()
		{
			string statement = @"Do While <Test />
Loop";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Do, Tokens.While, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.Loop
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void WhileLoopStatement3()
		{
			string statement = @"Do
Loop While <Test />";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Do, Tokens.EOL,
			            Tokens.Loop, Tokens.While, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void UntilLoopStatement()
		{
			string statement = @"Do Until <Test />
Loop";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Do, Tokens.Until, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.Loop
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void UntilLoopStatement2()
		{
			string statement = @"Do
Loop Until <Test />";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Do, Tokens.EOL,
			            Tokens.Loop, Tokens.Until, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void IfStatementLarge()
		{
			string statement = @"If <Test /> Then
Else If <Test /> Then
ElseIf <Test></Test> Then
Else
End If";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.If, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.Then, Tokens.EOL,
			            Tokens.Else, Tokens.If, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.Then, Tokens.EOL,
			            Tokens.ElseIf, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.XmlOpenEndTag, Tokens.Identifier, Tokens.XmlCloseTag, Tokens.Then, Tokens.EOL,
			            Tokens.Else, Tokens.EOL,
			            Tokens.End, Tokens.If
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void SelectStatement()
		{
			string statement = @"Select Case <Test />
	Case <Test />, <Test />
	Case Else
End Select";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Select, Tokens.Case, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.Case, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.Comma,
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.Case, Tokens.Else, Tokens.EOL,
			            Tokens.End, Tokens.Select
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void TryStatement()
		{
			string statement = @"Try
	Catch x
	Catch y As Exception
	Catch When <Test />
	Finally
End Try";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Try, Tokens.EOL,
			            Tokens.Catch, Tokens.Identifier, Tokens.EOL,
			            Tokens.Catch, Tokens.Identifier, Tokens.As, Tokens.Identifier, Tokens.EOL,
			            Tokens.Catch, Tokens.When, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.Finally, Tokens.EOL,
			            Tokens.End, Tokens.Try
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void ThrowStatement()
		{
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement("Throw <Test />")));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Throw, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement);
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void BranchStatements()
		{
			string statement = @"GoTo 5
GoTo LabelName
Exit Do
Exit For
Exit While
Exit Select
Exit Sub
Exit Function
Exit Property
Exit Try
Continue Do
Continue For
Continue While
Stop
End
Return
Return 5
Return <Test />";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.GoTo, Tokens.LiteralInteger, Tokens.EOL,
			            Tokens.GoTo, Tokens.Identifier, Tokens.EOL,
			            Tokens.Exit, Tokens.Do, Tokens.EOL,
			            Tokens.Exit, Tokens.For, Tokens.EOL,
			            Tokens.Exit, Tokens.While, Tokens.EOL,
			            Tokens.Exit, Tokens.Select, Tokens.EOL,
			            Tokens.Exit, Tokens.Sub, Tokens.EOL,
			            Tokens.Exit, Tokens.Function, Tokens.EOL,
			            Tokens.Exit, Tokens.Property, Tokens.EOL,
			            Tokens.Exit, Tokens.Try, Tokens.EOL,
			            Tokens.Continue, Tokens.Do, Tokens.EOL,
			            Tokens.Continue, Tokens.For, Tokens.EOL,
			            Tokens.Continue, Tokens.While, Tokens.EOL,
			            Tokens.Stop, Tokens.EOL,
			            Tokens.End, Tokens.EOL,
			            Tokens.Return, Tokens.EOL,
			            Tokens.Return, Tokens.LiteralInteger, Tokens.EOL,
			            Tokens.Return, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void ArrayHandlingStatements()
		{
			string statement = @"Erase <Test />
Erase <Test />, <Test />
ReDim Preserve <Test />";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Erase, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.Erase, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.Comma,
			            Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.ReDim, Tokens.Preserve, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void UsingStatement()
		{
			string statement = @"Using <Test />
End Using";
			
			VBLexer lexer = GenerateLexer(new StringReader(TestStatement(statement)));
			
			CheckHead(lexer);
			
			CheckTokens(lexer, Tokens.Using, Tokens.XmlOpenTag, Tokens.Identifier, Tokens.XmlCloseTagEmptyElement, Tokens.EOL,
			            Tokens.End, Tokens.Using
			           );
			
			CheckFoot(lexer);
		}
		
		[Test]
		public void NewExpressionWithObjectInitializer()
		{
			string code = @"New Common.ComboBoxItem With {.Item = _
                            Localizer.GetString(""Month"" & initParameters.SelectedDate.FirstDayOfPreviousMonth.Month) & "" "" &
                            initParameters.SelectedDate.FirstDayOfPreviousMonth.Year, .Value = New Date(2010, initParameters.SelectedDate.FirstDayOfPreviousMonth.Month, 1)}";
			
			VBLexer lexer = GenerateLexerForSnippet(new StringReader(code), SnippetType.Expression);
			
			CheckTokens(lexer, Tokens.New, Tokens.Identifier, Tokens.Dot, Tokens.Identifier,
			            Tokens.With, Tokens.OpenCurlyBrace, Tokens.Dot, Tokens.Identifier, Tokens.Assign,
			            Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.OpenParenthesis, Tokens.LiteralString,
			            Tokens.ConcatString, Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.Dot,
			            Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.CloseParenthesis, Tokens.ConcatString,
			            Tokens.LiteralString, Tokens.ConcatString, Tokens.Identifier, Tokens.Dot, Tokens.Identifier,
			            Tokens.Dot, Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.Comma, Tokens.Dot,
			            Tokens.Identifier, Tokens.Assign, Tokens.New, Tokens.Date, Tokens.OpenParenthesis, Tokens.LiteralInteger,
			            Tokens.Comma, Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.Dot, Tokens.Identifier, Tokens.Dot,
			            Tokens.Identifier, Tokens.Comma, Tokens.LiteralInteger, Tokens.CloseParenthesis, Tokens.CloseCurlyBrace);
		}
		#endregion
		
		#region Helpers
		VBLexer GenerateLexer(StringReader sr)
		{
			return new VBLexer(sr);
		}
		
		VBLexer GenerateLexerForSnippet(StringReader sr, SnippetType type)
		{
			var lexer = new VBLexer(sr);
			lexer.SetInitialContext(type);
			return lexer;
		}
		
		string TestStatement(string stmt)
		{
			return "Class Test\n" +
				"Sub A\n" +
				stmt + "\n" +
				"End Sub\n" +
				"End Class";
		}
		
		void CheckFoot(VBLexer lexer)
		{
			CheckTokens(lexer, Tokens.EOL, Tokens.End, Tokens.Sub, Tokens.EOL, Tokens.End, Tokens.Class);
		}
		
		void CheckHead(VBLexer lexer)
		{
			CheckTokens(lexer, Tokens.Class, Tokens.Identifier, Tokens.EOL,
			            Tokens.Sub, Tokens.Identifier, Tokens.EOL);
		}
		
		void CheckTokens(VBLexer lexer, params int[] tokens)
		{
			for (int i = 0; i < tokens.Length; i++) {
				int token = tokens[i];
				Token t = lexer.NextToken();
				int next = t.Kind;
				Assert.IsEmpty(lexer.Errors.ErrorOutput);
				Assert.AreEqual(token, next, "{2} of {3}: expected: \"{0}\", was: \"{1}\"; at {4}", Tokens.GetTokenString(token), Tokens.GetTokenString(next), i + 1, tokens.Length, t.Location);
			}
		}
		#endregion
	}
}
