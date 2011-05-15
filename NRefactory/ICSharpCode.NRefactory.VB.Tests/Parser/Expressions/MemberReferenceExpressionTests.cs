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
	public class MemberReferenceExpressionTests
	{
		#region VB.NET
		[Test]
		public void VBNetSimpleFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtil.ParseExpression<MemberReferenceExpression>("myTargetObject.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is SimpleNameExpression);
			Assert.AreEqual("myTargetObject", ((SimpleNameExpression)fre.TargetObject).Identifier);
		}
		
		[Test]
		public void VBNetFieldReferenceExpressionWithoutTargetTest()
		{
			MemberReferenceExpression fre = ParseUtil.ParseExpression<MemberReferenceExpression>(".myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject.IsNull);
		}
		
		[Test]
		public void VBNetGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtil.ParseExpression<MemberReferenceExpression>("SomeClass(of string).myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsInstanceOf(typeof(SimpleNameExpression), fre.TargetObject);
			TypeReference tr = ((SimpleNameExpression)fre.TargetObject).TypeArguments[0];
			Assert.AreEqual("System.String", tr.Type);
		}
		
		[Test]
		public void VBNetFullNamespaceGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtil.ParseExpression<MemberReferenceExpression>("System.Subnamespace.SomeClass(of string).myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsInstanceOf(typeof(MemberReferenceExpression), fre.TargetObject);
			
			MemberReferenceExpression inner = (MemberReferenceExpression)fre.TargetObject;
			Assert.AreEqual("SomeClass", inner.MemberName);
			Assert.AreEqual(1, inner.TypeArguments.Count);
			Assert.AreEqual("System.String", inner.TypeArguments[0].Type);
		}
		
		[Test]
		public void VBNetGlobalFullNamespaceGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtil.ParseExpression<MemberReferenceExpression>("Global.System.Subnamespace.SomeClass(of string).myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsInstanceOf(typeof(MemberReferenceExpression), fre.TargetObject);
			MemberReferenceExpression inner = (MemberReferenceExpression)fre.TargetObject;
			
			Assert.AreEqual("SomeClass", inner.MemberName);
			Assert.AreEqual(1, inner.TypeArguments.Count);
			Assert.AreEqual("System.String", inner.TypeArguments[0].Type);
		}
		
		[Test]
		public void VBNetNestedGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtil.ParseExpression<MemberReferenceExpression>("MyType(of string).InnerClass(of integer).myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsInstanceOf(typeof(MemberReferenceExpression), fre.TargetObject);
			
			MemberReferenceExpression inner = (MemberReferenceExpression)fre.TargetObject;
			Assert.AreEqual("InnerClass", inner.MemberName);
		}
		
		#endregion
	}
}
