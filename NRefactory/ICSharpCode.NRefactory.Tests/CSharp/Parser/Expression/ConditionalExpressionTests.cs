// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class ConditionalExpressionTests
	{
		[Test]
		public void ConditionalExpressionTest()
		{
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a == b ? a() : a.B");
			
			Assert.IsTrue(ce.Condition is BinaryOperatorExpression);
			Assert.IsTrue(ce.TrueExpression is InvocationExpression);
			Assert.IsTrue(ce.FalseExpression is MemberReferenceExpression);
		}
		
		[Test]
		public void ConditionalIsExpressionTest()
		{
			// (as is b?) ERROR (conflict with nullables, SD-419)
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a is b ? a() : a.B");
			
			Assert.IsTrue(ce.Condition is IsExpression);
			Assert.IsTrue(ce.TrueExpression is InvocationExpression);
			Assert.IsTrue(ce.FalseExpression is MemberReferenceExpression);
		}
		
		[Test]
		public void ConditionalIsWithNullableExpressionTest()
		{
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a is b? ? a() : a.B");
			
			Assert.IsTrue(ce.Condition is IsExpression);
			Assert.IsTrue(ce.TrueExpression is InvocationExpression);
			Assert.IsTrue(ce.FalseExpression is MemberReferenceExpression);
		}
		
		[Test]
		public void ConditionalIsExpressionTest2()
		{
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a is b ? (a()) : a.B");
			
			Assert.IsTrue(ce.Condition is IsExpression);
			Assert.IsTrue(ce.TrueExpression is ParenthesizedExpression);
			Assert.IsTrue(ce.FalseExpression is MemberReferenceExpression);
		}
		
		[Test]
		public void ConditionalExpressionNegativeValue()
		{
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("isNegative ? -1 : 1");
			
			Assert.IsTrue(ce.Condition is IdentifierExpression);
			Assert.IsTrue(ce.TrueExpression is UnaryOperatorExpression);
			Assert.IsTrue(ce.FalseExpression is PrimitiveExpression);
		}
		
		
		[Test]
		public void ConditionalIsWithNegativeValue()
		{
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a is b ? -1 : 1");
			
			Assert.IsTrue(ce.Condition is IsExpression);
			Assert.IsTrue(ce.TrueExpression is UnaryOperatorExpression);
			Assert.IsTrue(ce.FalseExpression is PrimitiveExpression);
		}
		
		[Test]
		public void ConditionalIsWithExplicitPositiveValue()
		{
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a is b ? +1 : 1");
			
			Assert.IsTrue(ce.Condition is IsExpression);
			Assert.IsTrue(ce.TrueExpression is UnaryOperatorExpression);
			Assert.IsTrue(ce.FalseExpression is PrimitiveExpression);
		}
		
		[Test]
		public void RepeatedConditionalExpr()
		{
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a ? b : c ? d : e");
			
			Assert.AreEqual("a", ((IdentifierExpression)ce.Condition).Identifier);
			Assert.AreEqual("b", ((IdentifierExpression)ce.TrueExpression).Identifier);
			Assert.IsTrue(ce.FalseExpression is ConditionalExpression);
		}
		
		[Test]
		public void NestedConditionalExpr()
		{
			ConditionalExpression ce = ParseUtilCSharp.ParseExpression<ConditionalExpression>("a ? b ? c : d : e");
			
			Assert.AreEqual("a", ((IdentifierExpression)ce.Condition).Identifier);
			Assert.AreEqual("e", ((IdentifierExpression)ce.FalseExpression).Identifier);
			Assert.IsTrue(ce.TrueExpression is ConditionalExpression);
		}
	}
}
