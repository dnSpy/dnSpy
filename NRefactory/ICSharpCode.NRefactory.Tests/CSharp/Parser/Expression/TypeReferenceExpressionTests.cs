// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore]
	public class TypeReferenceExpressionTests
	{
		[Test]
		public void GlobalTypeReferenceExpression()
		{
			/*TypeReferenceExpression tr = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("global::System");
			Assert.AreEqual("System", tr.TypeReference.Type);
			Assert.IsTrue(tr.TypeReference.IsGlobal);*/
			throw new NotImplementedException();
		}
		
		/* TODO
		[Test]
		public void GlobalTypeReferenceExpressionWithoutTypeName()
		{
			TypeReferenceExpression tr = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("global::", true);
			Assert.AreEqual("?", tr.TypeReference.Type);
			Assert.IsTrue(tr.TypeReference.IsGlobal);
		}
		
		[Test]
		public void IntReferenceExpression()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("int.MaxValue");
			Assert.AreEqual("MaxValue", fre.MemberName);
			Assert.AreEqual("System.Int32", ((TypeReferenceExpression)fre.TargetObject).TypeReference.Type);
		}
		
		[Test]
		public void StandaloneIntReferenceExpression()
		{
			TypeReferenceExpression tre = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("int");
			Assert.AreEqual("System.Int32", tre.TypeReference.Type);
		}
		*/
	}
}
