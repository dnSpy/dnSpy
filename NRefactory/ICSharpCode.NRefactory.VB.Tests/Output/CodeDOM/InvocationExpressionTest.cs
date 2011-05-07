// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.CodeDom;
using System.Collections.Generic;

using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Visitors;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Output.CodeDom.Tests
{
	[TestFixture]
	public class InvocationExpressionsTests
	{
		[Test]
		public void IdentifierOnlyInvocation()
		{
			// InitializeComponents();
			SimpleNameExpression identifier = new SimpleNameExpression("InitializeComponents");
			InvocationExpression invocation = new InvocationExpression(identifier, new List<Expression>());
			object output = invocation.AcceptVisitor(new CodeDomVisitor(), null);
			Assert.IsTrue(output is CodeMethodInvokeExpression);
			CodeMethodInvokeExpression mie = (CodeMethodInvokeExpression)output;
			Assert.AreEqual("InitializeComponents", mie.Method.MethodName);
			Assert.IsTrue(mie.Method.TargetObject is CodeThisReferenceExpression);
		}
		
		[Test]
		public void MethodOnThisReferenceInvocation()
		{
			// InitializeComponents();
			MemberReferenceExpression field = new MemberReferenceExpression(new ThisReferenceExpression(), "InitializeComponents");
			InvocationExpression invocation = new InvocationExpression(field, new List<Expression>());
			object output = invocation.AcceptVisitor(new CodeDomVisitor(), null);
			Assert.IsTrue(output is CodeMethodInvokeExpression);
			CodeMethodInvokeExpression mie = (CodeMethodInvokeExpression)output;
			Assert.AreEqual("InitializeComponents", mie.Method.MethodName);
			Assert.IsTrue(mie.Method.TargetObject is CodeThisReferenceExpression);
		}
		
		[Test]
		public void InvocationOfStaticMethod()
		{
			// System.Drawing.Color.FromArgb();
			MemberReferenceExpression field = new MemberReferenceExpression(new SimpleNameExpression("System"), "Drawing");
			field = new MemberReferenceExpression(field, "Color");
			field = new MemberReferenceExpression(field, "FromArgb");
			InvocationExpression invocation = new InvocationExpression(field, new List<Expression>());
			object output = invocation.AcceptVisitor(new CodeDomVisitor(), null);
			Assert.IsTrue(output is CodeMethodInvokeExpression);
			CodeMethodInvokeExpression mie = (CodeMethodInvokeExpression)output;
			Assert.AreEqual("FromArgb", mie.Method.MethodName);
			Assert.IsTrue(mie.Method.TargetObject is CodeTypeReferenceExpression);
			Assert.AreEqual("System.Drawing.Color", (mie.Method.TargetObject as CodeTypeReferenceExpression).Type.BaseType);
		}
	}
}
