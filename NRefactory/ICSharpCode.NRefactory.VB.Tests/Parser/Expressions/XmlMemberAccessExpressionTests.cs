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
	public class XmlMemberAccessExpressionTests
	{
		#region VB.NET
		[Test]
		public void VBNetSimpleElementReferenceTest()
		{
			XmlMemberAccessExpression xmae = ParseUtil.ParseExpression<XmlMemberAccessExpression>("xml.<ns:MyElement>");
			Assert.AreEqual("ns:MyElement", xmae.Identifier);
			Assert.IsTrue(xmae.IsXmlIdentifier);
			Assert.AreEqual(XmlAxisType.Element, xmae.AxisType);
			Assert.IsTrue(xmae.TargetObject is IdentifierExpression);
			Assert.AreEqual("xml", ((IdentifierExpression)xmae.TargetObject).Identifier);
		}
		
		[Test]
		public void VBNetSimpleAttributeReferenceTest()
		{
			XmlMemberAccessExpression xmae = ParseUtil.ParseExpression<XmlMemberAccessExpression>("xml.@attribute");
			Assert.AreEqual("attribute", xmae.Identifier);
			Assert.IsFalse(xmae.IsXmlIdentifier);
			Assert.AreEqual(XmlAxisType.Attribute, xmae.AxisType);
			Assert.IsTrue(xmae.TargetObject is IdentifierExpression);
			Assert.AreEqual("xml", ((IdentifierExpression)xmae.TargetObject).Identifier);
		}
		
		[Test]
		public void VBNetXmlNameAttributeReferenceTest()
		{
			XmlMemberAccessExpression xmae = ParseUtil.ParseExpression<XmlMemberAccessExpression>("xml.@<ns:attribute>");
			Assert.AreEqual("ns:attribute", xmae.Identifier);
			Assert.IsTrue(xmae.IsXmlIdentifier);
			Assert.AreEqual(XmlAxisType.Attribute, xmae.AxisType);
			Assert.IsTrue(xmae.TargetObject is IdentifierExpression);
			Assert.AreEqual("xml", ((IdentifierExpression)xmae.TargetObject).Identifier);
		}
		
		[Test]
		public void VBNetSimpleDescendentsReferenceTest()
		{
			XmlMemberAccessExpression xmae = ParseUtil.ParseExpression<XmlMemberAccessExpression>("xml...<ns:Element>");
			Assert.AreEqual("ns:Element", xmae.Identifier);
			Assert.IsTrue(xmae.IsXmlIdentifier);
			Assert.AreEqual(XmlAxisType.Descendents, xmae.AxisType);
			Assert.IsTrue(xmae.TargetObject is IdentifierExpression);
			Assert.AreEqual("xml", ((IdentifierExpression)xmae.TargetObject).Identifier);
		}
		
		[Test]
		public void VBNetSimpleElementReferenceWithDotTest()
		{
			XmlMemberAccessExpression xmae = ParseUtil.ParseExpression<XmlMemberAccessExpression>(".<ns:MyElement>");
			Assert.AreEqual("ns:MyElement", xmae.Identifier);
			Assert.IsTrue(xmae.IsXmlIdentifier);
			Assert.AreEqual(XmlAxisType.Element, xmae.AxisType);
			Assert.IsTrue(xmae.TargetObject.IsNull);
		}
		
		[Test]
		public void VBNetSimpleAttributeReferenceWithDotTest()
		{
			XmlMemberAccessExpression xmae = ParseUtil.ParseExpression<XmlMemberAccessExpression>(".@attribute");
			Assert.AreEqual("attribute", xmae.Identifier);
			Assert.IsFalse(xmae.IsXmlIdentifier);
			Assert.AreEqual(XmlAxisType.Attribute, xmae.AxisType);
			Assert.IsTrue(xmae.TargetObject.IsNull);
		}
		
		[Test]
		public void VBNetXmlNameAttributeReferenceWithDotTest()
		{
			XmlMemberAccessExpression xmae = ParseUtil.ParseExpression<XmlMemberAccessExpression>(".@<ns:attribute>");
			Assert.AreEqual("ns:attribute", xmae.Identifier);
			Assert.IsTrue(xmae.IsXmlIdentifier);
			Assert.AreEqual(XmlAxisType.Attribute, xmae.AxisType);
			Assert.IsTrue(xmae.TargetObject.IsNull);
		}
		
		[Test]
		public void VBNetSimpleDescendentsReferenceWithDotTest()
		{
			XmlMemberAccessExpression xmae = ParseUtil.ParseExpression<XmlMemberAccessExpression>("...<ns:Element>");
			Assert.AreEqual("ns:Element", xmae.Identifier);
			Assert.IsTrue(xmae.IsXmlIdentifier);
			Assert.AreEqual(XmlAxisType.Descendents, xmae.AxisType);
			Assert.IsTrue(xmae.TargetObject.IsNull);
		}
		#endregion
	}
}
