// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore("Port unit tests to new DOM")]
	public class CastExpressionTests
	{
		/*
		[Test]
		public void SimpleCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(MyObject)o");
			Assert.AreEqual("MyObject", ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void ArrayCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(MyType[])o");
			Assert.AreEqual("MyType", ce.CastTo.Type);
			Assert.AreEqual(new int[] { 0 }, ce.CastTo.RankSpecifier);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void NullablePrimitiveCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(int?)o");
			Assert.AreEqual("System.Nullable", ce.CastTo.Type);
			Assert.AreEqual("System.Int32", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void NullableCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(MyType?)o");
			Assert.AreEqual("System.Nullable", ce.CastTo.Type);
			Assert.AreEqual("MyType", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void NullableTryCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("o as int?");
			Assert.AreEqual("System.Nullable", ce.CastTo.Type);
			Assert.IsTrue(ce.CastTo.IsKeyword);
			Assert.AreEqual("System.Int32", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.TryCast, ce.CastType);
		}
		
		[Test]
		public void GenericCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(List<string>)o");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("System.String", ce.CastTo.GenericTypes[0].Type);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void GenericArrayCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(List<string>[])o");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("System.String", ce.CastTo.GenericTypes[0].Type);
			Assert.AreEqual(new int[] { 0 }, ce.CastTo.RankSpecifier);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void GenericArrayAsCastExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("o as List<string>[]");
			Assert.AreEqual("List", ce.CastTo.Type);
			Assert.AreEqual("System.String", ce.CastTo.GenericTypes[0].Type);
			Assert.AreEqual(new int[] { 0 }, ce.CastTo.RankSpecifier);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
			Assert.AreEqual(CastType.TryCast, ce.CastType);
		}
		
		[Test]
		public void CastMemberReferenceOnParenthesizedExpression()
		{
			// yes, we really wanted to evaluate .Member on expr and THEN cast the result to MyType
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(MyType)(expr).Member");
			Assert.AreEqual("MyType", ce.CastTo.Type);
			Assert.IsTrue(ce.Expression is MemberReferenceExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		
		[Test]
		public void TryCastParenthesizedExpression()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(o) as string");
			Assert.AreEqual("System.String", ce.CastTo.ToString());
			Assert.IsTrue(ce.Expression is ParenthesizedExpression);
			Assert.AreEqual(CastType.TryCast, ce.CastType);
		}
		
		[Test]
		public void CastNegation()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(uint)-negativeValue");
			Assert.AreEqual("System.UInt32", ce.CastTo.ToString());
			Assert.IsTrue(ce.Expression is UnaryOperatorExpression);
			Assert.AreEqual(CastType.Cast, ce.CastType);
		}
		*/
		
		[Test]
		public void SubtractionIsNotCast()
		{
			BinaryOperatorExpression boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("(BigInt)-negativeValue");
			Assert.IsTrue(boe.Left is ParenthesizedExpression);
			Assert.IsTrue(boe.Right is IdentifierExpression);
		}
		
		[Test]
		public void IntMaxValueToBigInt()
		{
			CastExpression ce = ParseUtilCSharp.ParseExpression<CastExpression>("(BigInt)int.MaxValue");
			Assert.AreEqual("BigInt", ce.CastTo.ToString());
			Assert.IsTrue(ce.Expression is MemberReferenceExpression);
		}
	}
}
