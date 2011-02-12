// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore("Port unit tests to new DOM")]
	public class InvocationExpressionTests
	{
		[Test]
		public void SimpleInvocationExpressionTest()
		{
			var ie = ParseUtilCSharp.ParseExpression<InvocationExpression>("myMethod()");
			Assert.AreEqual(0, ie.Arguments.Count());
			Assert.IsTrue(ie.Target is IdentifierExpression);
			Assert.AreEqual("myMethod", ((IdentifierExpression)ie.Target).Identifier);
		}
		
		/* TODO port unit tests to new DOM
		[Test]
		public void GenericInvocationExpressionTest()
		{
			var expr = ParseUtilCSharp.ParseExpression<InvocationExpression>("myMethod<char>('a')");
			Assert.AreEqual(1, expr.Arguments.Count());
			Assert.IsTrue(expr.TargetObject is IdentifierExpression);
			IdentifierExpression ident = (IdentifierExpression)expr.TargetObject;
			Assert.AreEqual("myMethod", ident.Identifier);
			Assert.AreEqual(1, ident.TypeArguments.Count);
			Assert.AreEqual("System.Char", ident.TypeArguments[0].Type);
		}
		
		[Test]
		public void GenericInvocation2ExpressionTest()
		{
			var expr = ParseUtilCSharp.ParseExpression<InvocationExpression>("myMethod<T,bool>()");
			Assert.AreEqual(0, expr.Arguments.Count);
			Assert.IsTrue(expr.TargetObject is IdentifierExpression);
			IdentifierExpression ident = (IdentifierExpression)expr.TargetObject;
			Assert.AreEqual("myMethod", ident.Identifier);
			Assert.AreEqual(2, ident.TypeArguments.Count);
			Assert.AreEqual("T", ident.TypeArguments[0].Type);
			Assert.IsFalse(ident.TypeArguments[0].IsKeyword);
			Assert.AreEqual("System.Boolean", ident.TypeArguments[1].Type);
			Assert.IsTrue(ident.TypeArguments[1].IsKeyword);
		}
		
		[Test]
		public void AmbiguousGrammarGenericMethodCall()
		{
			InvocationExpression ie = ParseUtilCSharp.ParseExpression<InvocationExpression>("F(G<A,B>(7))");
			Assert.IsTrue(ie.TargetObject is IdentifierExpression);
			Assert.AreEqual(1, ie.Arguments.Count);
			ie = (InvocationExpression)ie.Arguments[0];
			Assert.AreEqual(1, ie.Arguments.Count);
			Assert.IsTrue(ie.Arguments[0] is PrimitiveExpression);
			IdentifierExpression ident = (IdentifierExpression)ie.TargetObject;
			Assert.AreEqual("G", ident.Identifier);
			Assert.AreEqual(2, ident.TypeArguments.Count);
		}
		
		[Test]
		public void AmbiguousGrammarNotAGenericMethodCall()
		{
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("F<A>+y");
			Assert.AreEqual(BinaryOperatorType.GreaterThan, boe.Op);
			Assert.IsTrue(boe.Left is BinaryOperatorExpression);
			Assert.IsTrue(boe.Right is UnaryOperatorExpression);
		}
		
		[Test]
		public void InvalidNestedInvocationExpressionTest()
		{
			// this test was written because this bug caused the AbstractASTVisitor to crash
			
			InvocationExpression expr = ParseUtilCSharp.ParseExpression<InvocationExpression>("WriteLine(myMethod(,))", true);
			Assert.IsTrue(expr.TargetObject is IdentifierExpression);
			Assert.AreEqual("WriteLine", ((IdentifierExpression)expr.TargetObject).Identifier);
			
			Assert.AreEqual(1, expr.Arguments.Count); // here a second null parameter was added incorrectly
			
			Assert.IsTrue(expr.Arguments[0] is InvocationExpression);
			CheckSimpleInvoke((InvocationExpression)expr.Arguments[0]);
		}
		
		[Test]
		public void NestedInvocationPositions()
		{
			InvocationExpression expr = ParseUtilCSharp.ParseExpression<InvocationExpression>("a.B().C(args)");
			Assert.AreEqual(new Location(8, 1), expr.StartLocation);
			Assert.AreEqual(new Location(14, 1), expr.EndLocation);
			MemberReferenceExpression mre = (MemberReferenceExpression)expr.TargetObject;
			Assert.AreEqual(new Location(6, 1), mre.StartLocation);
			Assert.AreEqual(new Location(8, 1), mre.EndLocation);
			
			Assert.AreEqual(new Location(4, 1), mre.TargetObject.StartLocation);
			Assert.AreEqual(new Location(6, 1), mre.TargetObject.EndLocation);
		}
		
		[Test]
		public void InvocationOnGenericType()
		{
			InvocationExpression expr = ParseUtilCSharp.ParseExpression<InvocationExpression>("A<T>.Foo()");
			MemberReferenceExpression mre = (MemberReferenceExpression)expr.TargetObject;
			Assert.AreEqual("Foo", mre.MemberName);
			TypeReferenceExpression tre = (TypeReferenceExpression)mre.TargetObject;
			Assert.AreEqual("A", tre.TypeReference.Type);
			Assert.AreEqual("T", tre.TypeReference.GenericTypes[0].Type);
		}
		
		[Test]
		public void InvocationOnInnerClassInGenericType()
		{
			InvocationExpression expr = ParseUtilCSharp.ParseExpression<InvocationExpression>("A<T>.B.Foo()");
			MemberReferenceExpression mre = (MemberReferenceExpression)expr.TargetObject;
			Assert.AreEqual("Foo", mre.MemberName);
			MemberReferenceExpression mre2 = (MemberReferenceExpression)mre.TargetObject;
			Assert.AreEqual("B", mre2.MemberName);
			TypeReferenceExpression tre = (TypeReferenceExpression)mre2.TargetObject;
			Assert.AreEqual("A", tre.TypeReference.Type);
			Assert.AreEqual("T", tre.TypeReference.GenericTypes[0].Type);
		}
		
		[Test]
		public void InvocationOnGenericInnerClassInGenericType()
		{
			InvocationExpression expr = ParseUtilCSharp.ParseExpression<InvocationExpression>("A<T>.B.C<U>.Foo()");
			MemberReferenceExpression mre = (MemberReferenceExpression)expr.TargetObject;
			Assert.AreEqual("Foo", mre.MemberName);
			TypeReferenceExpression tre = (TypeReferenceExpression)mre.TargetObject;
			InnerClassTypeReference ictr = (InnerClassTypeReference)tre.TypeReference;
			Assert.AreEqual("B.C", ictr.Type);
			Assert.AreEqual(1, ictr.GenericTypes.Count);
			Assert.AreEqual("U", ictr.GenericTypes[0].Type);
			
			Assert.AreEqual("A", ictr.BaseType.Type);
			Assert.AreEqual(1, ictr.BaseType.GenericTypes.Count);
			Assert.AreEqual("T", ictr.BaseType.GenericTypes[0].Type);
		}
		
		[Test]
		public void InvocationWithNamedArgument()
		{
			InvocationExpression expr = ParseUtilCSharp.ParseExpression<InvocationExpression>("a(arg: ref v)");
			Assert.AreEqual(1, expr.Arguments.Count);
			NamedArgumentExpression nae = (NamedArgumentExpression)expr.Arguments[0];
			Assert.AreEqual("arg", nae.Name);
			DirectionExpression dir = (DirectionExpression)nae.Expression;
			Assert.AreEqual(FieldDirection.Ref, dir.FieldDirection);
			Assert.IsInstanceOf<IdentifierExpression>(dir.Expression);
		}*/
	}
}
