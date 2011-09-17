// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
