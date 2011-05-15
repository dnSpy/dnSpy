// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class XmlExpressionTests
	{
		#region VB.NET
		[Test]
		public void VBNetSimpleCommentTest()
		{
			XmlContentExpression content = ParseUtil.ParseExpression<XmlContentExpression>("<!-- test -->");
			Assert.AreEqual(XmlContentType.Comment, content.Type);
			Assert.AreEqual(" test ", content.Content);
			Assert.AreEqual(new Location(1,1), content.StartLocation);
			Assert.AreEqual(new Location(14,1), content.EndLocation);
		}
		
		[Test]
		public void VBNetSimplePreprocessingInstructionTest()
		{
			XmlContentExpression content = ParseUtil.ParseExpression<XmlContentExpression>("<?xml version='1.0'?>");
			Assert.AreEqual(XmlContentType.ProcessingInstruction, content.Type);
			Assert.AreEqual("xml version='1.0'", content.Content);
			Assert.AreEqual(new Location(1,1), content.StartLocation);
			Assert.AreEqual(new Location(22,1), content.EndLocation);
		}
		
		[Test]
		public void VBNetSimpleCDataTest()
		{
			XmlContentExpression content = ParseUtil.ParseExpression<XmlContentExpression>("<![CDATA[<simple> <cdata>]]>");
			Assert.AreEqual(XmlContentType.CData, content.Type);
			Assert.AreEqual("<simple> <cdata>", content.Content);
			Assert.AreEqual(new Location(1,1), content.StartLocation);
			Assert.AreEqual(new Location(29,1), content.EndLocation);
		}
		
		[Test]
		public void VBNetSimpleEmptyElementTest()
		{
			XmlElementExpression element = ParseUtil.ParseExpression<XmlElementExpression>("<Test />");
			Assert.IsFalse(element.NameIsExpression);
			Assert.AreEqual("Test", element.XmlName);
			Assert.IsEmpty(element.Attributes);
			Assert.IsEmpty(element.Children);
			Assert.AreEqual(new Location(1,1), element.StartLocation);
			Assert.AreEqual(new Location(9,1), element.EndLocation);
		}
		
		[Test]
		public void VBNetSimpleEmptyElementWithAttributeTest()
		{
			XmlElementExpression element = ParseUtil.ParseExpression<XmlElementExpression>("<Test id='0' />");
			Assert.IsFalse(element.NameIsExpression);
			Assert.AreEqual("Test", element.XmlName);
			Assert.IsNotEmpty(element.Attributes);
			Assert.AreEqual(1, element.Attributes.Count);
			Assert.IsTrue(element.Attributes[0] is XmlAttributeExpression);
			XmlAttributeExpression attribute = element.Attributes[0] as XmlAttributeExpression;
			Assert.AreEqual("id", attribute.Name);
			Assert.IsTrue(attribute.IsLiteralValue);
			Assert.IsTrue(attribute.ExpressionValue.IsNull);
			Assert.AreEqual("0", attribute.LiteralValue);
			Assert.AreEqual(new Location(7,1), attribute.StartLocation);
			Assert.AreEqual(new Location(13,1), attribute.EndLocation);
			Assert.IsEmpty(element.Children);
			Assert.AreEqual(new Location(1,1), element.StartLocation);
			Assert.AreEqual(new Location(16,1), element.EndLocation);
		}
		
		[Test]
		public void VBNetSimpleEmptyElementWithAttributesTest()
		{
			XmlElementExpression element = ParseUtil.ParseExpression<XmlElementExpression>("<Test id='0' name=<%= name %> <%= contentData %> />");			Assert.IsFalse(element.NameIsExpression);
			Assert.AreEqual("Test", element.XmlName);
			Assert.IsNotEmpty(element.Attributes);
			Assert.AreEqual(3, element.Attributes.Count);
			
			Assert.IsTrue(element.Attributes[0] is XmlAttributeExpression);
			XmlAttributeExpression attribute = element.Attributes[0] as XmlAttributeExpression;
			Assert.AreEqual("id", attribute.Name);
			Assert.IsTrue(attribute.IsLiteralValue);
			Assert.IsTrue(attribute.ExpressionValue.IsNull);
			Assert.AreEqual("0", attribute.LiteralValue);
			Assert.AreEqual(new Location(7,1), attribute.StartLocation);
			Assert.AreEqual(new Location(13,1), attribute.EndLocation);
			
			Assert.IsTrue(element.Attributes[1] is XmlAttributeExpression);
			XmlAttributeExpression attribute2 = element.Attributes[1] as XmlAttributeExpression;
			Assert.AreEqual("name", attribute2.Name);
			Assert.IsFalse(attribute2.IsLiteralValue);
			Assert.IsFalse(attribute2.ExpressionValue.IsNull);
			
			Assert.IsTrue(attribute2.ExpressionValue is IdentifierExpression);
			IdentifierExpression identifier = attribute2.ExpressionValue as IdentifierExpression;
			Assert.AreEqual("name", identifier.Identifier);
			Assert.AreEqual(new Location(23,1), identifier.StartLocation);
			Assert.AreEqual(new Location(27,1), identifier.EndLocation);
			
			Assert.AreEqual(new Location(14,1), attribute2.StartLocation);
			Assert.AreEqual(new Location(30,1), attribute2.EndLocation);
			
			Assert.IsTrue(element.Attributes[2] is XmlEmbeddedExpression);
			XmlEmbeddedExpression attribute3 = element.Attributes[2] as XmlEmbeddedExpression;
			
			Assert.IsTrue(attribute3.InlineVBExpression is IdentifierExpression);
			IdentifierExpression identifier2 = attribute3.InlineVBExpression as IdentifierExpression;
			
			Assert.AreEqual("contentData", identifier2.Identifier);
			Assert.AreEqual(new Location(35,1), identifier2.StartLocation);
			Assert.AreEqual(new Location(46,1), identifier2.EndLocation);
			
			Assert.AreEqual(new Location(31,1), attribute3.StartLocation);
			Assert.AreEqual(new Location(49,1), attribute3.EndLocation);
			
			Assert.IsEmpty(element.Children);
			Assert.AreEqual(new Location(1,1), element.StartLocation);
			Assert.AreEqual(new Location(52,1), element.EndLocation);
		}
		
		[Test]
		public void VBNetElementWithAttributeTest()
		{
			XmlElementExpression element = ParseUtil.ParseExpression<XmlElementExpression>("<Test id='0'>\n" +
			                                                                                    "	<Item />\n" +
			                                                                                    "	<Item />\n" +
			                                                                                    "</Test>");
			Assert.IsFalse(element.NameIsExpression);
			Assert.AreEqual("Test", element.XmlName);
			
			Assert.IsNotEmpty(element.Attributes);
			Assert.AreEqual(1, element.Attributes.Count);
			Assert.IsTrue(element.Attributes[0] is XmlAttributeExpression);
			XmlAttributeExpression attribute = element.Attributes[0] as XmlAttributeExpression;
			Assert.AreEqual("id", attribute.Name);
			Assert.IsTrue(attribute.IsLiteralValue);
			Assert.IsTrue(attribute.ExpressionValue.IsNull);
			Assert.AreEqual("0", attribute.LiteralValue);
			Assert.AreEqual(new Location(7,1), attribute.StartLocation);
			Assert.AreEqual(new Location(13,1), attribute.EndLocation);
			
			Assert.IsNotEmpty(element.Children);
			Assert.AreEqual(5, element.Children.Count);
			
			CheckContent(element.Children[0], "\n\t", XmlContentType.Text, new Location(14,1), new Location(2,2));
			CheckContent(element.Children[2], "\n\t", XmlContentType.Text, new Location(10,2), new Location(2,3));
			CheckContent(element.Children[4], "\n", XmlContentType.Text, new Location(10,3), new Location(1,4));
			
			CheckElement(element.Children[1], "Item", new Location(2,2), new Location(10,2));
			CheckElement(element.Children[3], "Item", new Location(2,3), new Location(10,3));
			
			Assert.AreEqual(new Location(1,1), element.StartLocation);
			Assert.AreEqual(new Location(8,4), element.EndLocation);
		}
		
		[Test]
		public void VBNetElementWithMixedContentTest()
		{
			XmlElementExpression element = ParseUtil.ParseExpression<XmlElementExpression>("<Test id='0'>\n" +
			                                                                                    "	<!-- test -->\n" +
			                                                                                    "	<Item />\n" +
			                                                                                    "	<Item />\n" +
			                                                                                    "	<![CDATA[<cdata> section]]>\n" +
			                                                                                    "</Test>");
			Assert.IsFalse(element.NameIsExpression);
			Assert.AreEqual("Test", element.XmlName);
			
			Assert.IsNotEmpty(element.Attributes);
			Assert.AreEqual(1, element.Attributes.Count);
			Assert.IsTrue(element.Attributes[0] is XmlAttributeExpression);
			XmlAttributeExpression attribute = element.Attributes[0] as XmlAttributeExpression;
			Assert.AreEqual("id", attribute.Name);
			Assert.IsTrue(attribute.IsLiteralValue);
			Assert.IsTrue(attribute.ExpressionValue.IsNull);
			Assert.AreEqual("0", attribute.LiteralValue);
			Assert.AreEqual(new Location(7,1), attribute.StartLocation);
			Assert.AreEqual(new Location(13,1), attribute.EndLocation);
			
			Assert.IsNotEmpty(element.Children);
			Assert.AreEqual(9, element.Children.Count);
			
			CheckContent(element.Children[0], "\n\t", XmlContentType.Text, new Location(14,1), new Location(2,2));
			CheckContent(element.Children[2], "\n\t", XmlContentType.Text, new Location(15,2), new Location(2,3));
			CheckContent(element.Children[4], "\n\t", XmlContentType.Text, new Location(10,3), new Location(2,4));
			CheckContent(element.Children[6], "\n\t", XmlContentType.Text, new Location(10,4), new Location(2,5));
			CheckContent(element.Children[7], "<cdata> section", XmlContentType.CData, new Location(2,5), new Location(29,5));
			CheckContent(element.Children[8], "\n", XmlContentType.Text, new Location(29,5), new Location(1,6));
			
			CheckContent(element.Children[1], " test ", XmlContentType.Comment, new Location(2,2), new Location(15,2));
			CheckElement(element.Children[3], "Item", new Location(2,3), new Location(10,3));
			CheckElement(element.Children[5], "Item", new Location(2,4), new Location(10,4));
			
			Assert.AreEqual(new Location(1,1), element.StartLocation);
			Assert.AreEqual(new Location(8,6), element.EndLocation);
		}
		
		[Test]
		public void VBNetElementWithMixedContentTest2()
		{
			XmlElementExpression element = ParseUtil.ParseExpression<XmlElementExpression>("<Test>  aaaa	</Test>");
			Assert.IsFalse(element.NameIsExpression);
			Assert.AreEqual("Test", element.XmlName);
			
			Assert.IsNotEmpty(element.Children);
			Assert.AreEqual(1, element.Children.Count);
			
			CheckContent(element.Children[0], "  aaaa	", XmlContentType.Text, new Location(7,1), new Location(14,1));
		}
		
		[Test]
		public void VBNetProcessingInstructionAndCommentAtEndTest()
		{
			XmlDocumentExpression document = ParseUtil.ParseExpression<XmlDocumentExpression>("<Test />\n" +
			                                                                                       "<!-- test -->\n" +
			                                                                                       "<?target some text?>");
			Assert.IsNotEmpty(document.Expressions);
			Assert.AreEqual(3, document.Expressions.Count);
			
			CheckElement(document.Expressions[0], "Test", new Location(1,1), new Location(9,1));
			CheckContent(document.Expressions[1], " test ", XmlContentType.Comment, new Location(1,2), new Location(14,2));
			CheckContent(document.Expressions[2], "target some text", XmlContentType.ProcessingInstruction, new Location(1,3), new Location(21,3));
		}
		#endregion
		
		void CheckElement(AstNode node, string name, AstLocation start, AstLocation end)
		{
			Assert.IsTrue(node is XmlElementExpression);
			XmlElementExpression expr = node as XmlElementExpression;
			Assert.IsEmpty(expr.Attributes);
			Assert.IsEmpty(expr.Children);
			Assert.IsFalse(expr.NameIsExpression);
			Assert.AreEqual(name, expr.XmlName);
			Assert.AreEqual(start, expr.StartLocation);
			Assert.AreEqual(end, expr.EndLocation);
		}
		
		void CheckContent(AstNode node, string content, XmlContentType type, AstLocation start, AstLocation end)
		{
			Assert.IsTrue(node is XmlContentExpression);
			XmlContentExpression expr = node as XmlContentExpression;
			Assert.AreEqual(type, expr.Type);
			Assert.AreEqual(content, expr.Content);
			Assert.AreEqual(start, expr.StartLocation);
			Assert.AreEqual(end, expr.EndLocation);
		}
	}
}
