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
	public class ConditionalExpressionTests
	{
		#region VB.NET
		
		[Test]
		public void VBNetConditionalExpressionTest()
		{
			ConditionalExpression ce = ParseUtil.ParseExpression<ConditionalExpression>("If(x IsNot Nothing, x.Test, \"nothing\")");
			
			Assert.IsTrue(ce.Condition is BinaryOperatorExpression);
			Assert.IsTrue(ce.TrueExpression is MemberReferenceExpression);
			Assert.IsTrue(ce.FalseExpression is PrimitiveExpression);
		}
		
		#endregion
	}
}
