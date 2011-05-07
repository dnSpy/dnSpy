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
	public class UnaryOperatorExpressionTests
	{
		#region VB.NET
		void VBNetTestUnaryOperatorExpressionTest(string program, UnaryOperatorType op)
		{
			UnaryOperatorExpression uoe = ParseUtil.ParseExpression<UnaryOperatorExpression>(program);
			Assert.AreEqual(op, uoe.Op);
			
			Assert.IsTrue(uoe.Expression is SimpleNameExpression);
		}
		
		[Test]
		public void VBNetNotTest()
		{
			VBNetTestUnaryOperatorExpressionTest("Not a", UnaryOperatorType.Not);
		}
		
		[Test]
		public void VBNetInEqualsNotTest()
		{
			BinaryOperatorExpression e = ParseUtil.ParseExpression<BinaryOperatorExpression>("b <> Not a");
			Assert.AreEqual(BinaryOperatorType.InEquality, e.Op);
			UnaryOperatorExpression ue = (UnaryOperatorExpression)e.Right;
			Assert.AreEqual(UnaryOperatorType.Not, ue.Op);
		}
		
		[Test]
		public void VBNetNotEqualTest()
		{
			UnaryOperatorExpression e = ParseUtil.ParseExpression<UnaryOperatorExpression>("Not a = b");
			Assert.AreEqual(UnaryOperatorType.Not, e.Op);
			BinaryOperatorExpression boe = (BinaryOperatorExpression)e.Expression;
			Assert.AreEqual(BinaryOperatorType.Equality, boe.Op);
		}
		
		[Test]
		public void VBNetPlusTest()
		{
			VBNetTestUnaryOperatorExpressionTest("+a", UnaryOperatorType.Plus);
		}
		
		[Test]
		public void VBNetMinusTest()
		{
			VBNetTestUnaryOperatorExpressionTest("-a", UnaryOperatorType.Minus);
		}
		#endregion
	}
}
