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
	public class InvocationExpressionTests
	{
		void CheckSimpleInvoke(InvocationExpression ie)
		{
			Assert.AreEqual(0, ie.Arguments.Count);
			Assert.IsTrue(ie.TargetObject is SimpleNameExpression);
			Assert.AreEqual("myMethod", ((SimpleNameExpression)ie.TargetObject).Identifier);
		}
		
		void CheckGenericInvoke(InvocationExpression expr)
		{
			Assert.AreEqual(1, expr.Arguments.Count);
			Assert.IsTrue(expr.TargetObject is SimpleNameExpression);
			SimpleNameExpression ident = (SimpleNameExpression)expr.TargetObject;
			Assert.AreEqual("myMethod", ident.Identifier);
			Assert.AreEqual(1, ident.TypeArguments.Count);
			Assert.AreEqual("System.Char", ident.TypeArguments[0].Type);
		}
		
		void CheckGenericInvoke2(InvocationExpression expr)
		{
			Assert.AreEqual(0, expr.Arguments.Count);
			Assert.IsTrue(expr.TargetObject is SimpleNameExpression);
			SimpleNameExpression ident = (SimpleNameExpression)expr.TargetObject;
			Assert.AreEqual("myMethod", ident.Identifier);
			Assert.AreEqual(2, ident.TypeArguments.Count);
			Assert.AreEqual("T", ident.TypeArguments[0].Type);
			Assert.IsFalse(ident.TypeArguments[0].IsKeyword);
			Assert.AreEqual("System.Boolean", ident.TypeArguments[1].Type);
			Assert.IsTrue(ident.TypeArguments[1].IsKeyword);
		}
		
		#region VB.NET
		[Test]
		public void VBNetSimpleInvocationExpressionTest()
		{
			CheckSimpleInvoke(ParseUtil.ParseExpression<InvocationExpression>("myMethod()"));
		}
		
		[Test]
		public void VBNetGenericInvocationExpressionTest()
		{
			CheckGenericInvoke(ParseUtil.ParseExpression<InvocationExpression>("myMethod(Of Char)(\"a\"c)"));
		}
		
		[Test]
		public void VBNetGenericInvocation2ExpressionTest()
		{
			CheckGenericInvoke2(ParseUtil.ParseExpression<InvocationExpression>("myMethod(Of T, Boolean)()"));
		}
		
		[Test]
		public void PrimitiveExpression1Test()
		{
			InvocationExpression ie = ParseUtil.ParseExpression<InvocationExpression>("546.ToString()");
			Assert.AreEqual(0, ie.Arguments.Count);
		}
		
		[Test]
		public void VBInvocationOnGenericType()
		{
			InvocationExpression expr = ParseUtil.ParseExpression<InvocationExpression>("A(Of T).Foo()");
			MemberReferenceExpression mre = (MemberReferenceExpression)expr.TargetObject;
			Assert.AreEqual("Foo", mre.MemberName);
			SimpleNameExpression tre = (SimpleNameExpression)mre.TargetObject;
			Assert.AreEqual("A", tre.Identifier);
			Assert.AreEqual("T", tre.TypeArguments[0].Type);
		}
		
		[Test]
		public void VBInvocationOnInnerClassInGenericType()
		{
			InvocationExpression expr = ParseUtil.ParseExpression<InvocationExpression>("A(Of T).B.Foo()");
			MemberReferenceExpression mre = (MemberReferenceExpression)expr.TargetObject;
			Assert.AreEqual("Foo", mre.MemberName);
			MemberReferenceExpression mre2 = (MemberReferenceExpression)mre.TargetObject;
			Assert.AreEqual("B", mre2.MemberName);
			SimpleNameExpression tre = (SimpleNameExpression)mre2.TargetObject;
			Assert.AreEqual("A", tre.Identifier);
			Assert.AreEqual("T", tre.TypeArguments[0].Type);
		}
		
		[Test]
		public void VBInvocationOnGenericInnerClassInGenericType()
		{
			InvocationExpression expr = ParseUtil.ParseExpression<InvocationExpression>("A(Of T).B.C(Of U).Foo()");
			MemberReferenceExpression mre = (MemberReferenceExpression)expr.TargetObject;
			Assert.AreEqual("Foo", mre.MemberName);
			
			MemberReferenceExpression mre2 = (MemberReferenceExpression)mre.TargetObject;
			Assert.AreEqual("C", mre2.MemberName);
			Assert.AreEqual("U", mre2.TypeArguments[0].Type);
			
			MemberReferenceExpression mre3 = (MemberReferenceExpression)mre2.TargetObject;
			Assert.AreEqual("B", mre3.MemberName);
			
			SimpleNameExpression tre = (SimpleNameExpression)mre3.TargetObject;
			Assert.AreEqual("A", tre.Identifier);
			Assert.AreEqual("T", tre.TypeArguments[0].Type);
		}
		
		#endregion
	}
}
