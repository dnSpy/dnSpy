// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class IsExpressionTests
	{
		[Test, Ignore]
		public void GenericArrayIsExpression()
		{
			/* TODO
			TypeOfIsExpression ce = ParseUtilCSharp.ParseExpression<TypeOfIsExpression>("o is List<string>[]");
			Assert.AreEqual("List", ce.TypeReference.Type);
			Assert.AreEqual("System.String", ce.TypeReference.GenericTypes[0].Type);
			Assert.AreEqual(new int[] { 0 }, ce.TypeReference.RankSpecifier);
			Assert.IsTrue(ce.Expression is IdentifierExpression);*/
		}
		
		[Test]
		public void NullableIsExpression()
		{
			IsExpression ce = ParseUtilCSharp.ParseExpression<IsExpression>("o is int?");
			ComposedType type = (ComposedType)ce.Type;
			Assert.IsTrue(type.HasNullableSpecifier);
			Assert.AreEqual("int", ((PrimitiveType)type.BaseType).Keyword);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
		
		[Test]
		public void NullableIsExpressionInBinaryOperatorExpression()
		{
			BinaryOperatorExpression boe;
			boe = ParseUtilCSharp.ParseExpression<BinaryOperatorExpression>("o is int? == true");
			IsExpression ce = (IsExpression)boe.Left;
			ComposedType type = (ComposedType)ce.Type;
			Assert.IsTrue(type.HasNullableSpecifier);
			Assert.AreEqual("int", ((PrimitiveType)type.BaseType).Keyword);
			Assert.IsTrue(ce.Expression is IdentifierExpression);
		}
	}
}
