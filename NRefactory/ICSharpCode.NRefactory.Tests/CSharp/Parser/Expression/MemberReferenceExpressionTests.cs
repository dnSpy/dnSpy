// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore]
	public class MemberReferenceExpressionTests
	{
		[Test]
		public void SimpleFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("myTargetObject.myField");
			//Assert.AreEqual("myField", fre.MemberName);
			//Assert.IsTrue(fre.TargetObject is IdentifierExpression);
			//Assert.AreEqual("myTargetObject", ((IdentifierExpression)fre.TargetObject).Identifier);
			throw new NotImplementedException();
		}
		
		/* TODO port unit tests
		[Test]
		public void GenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("SomeClass<string>.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].Type);
		}
		
		[Test]
		public void FullNamespaceGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("Namespace.Subnamespace.SomeClass<string>.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("Namespace.Subnamespace.SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].Type);
		}
		
		[Test]
		public void GlobalFullNamespaceGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("global::Namespace.Subnamespace.SomeClass<string>.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			TypeReference tr = ((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.IsFalse(tr is InnerClassTypeReference);
			Assert.AreEqual("Namespace.Subnamespace.SomeClass", tr.Type);
			Assert.AreEqual(1, tr.GenericTypes.Count);
			Assert.AreEqual("System.String", tr.GenericTypes[0].Type);
			Assert.IsTrue(tr.IsGlobal);
		}
		
		[Test]
		public void NestedGenericFieldReferenceExpressionTest()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("MyType<string>.InnerClass<int>.myField");
			Assert.AreEqual("myField", fre.MemberName);
			Assert.IsTrue(fre.TargetObject is TypeReferenceExpression);
			InnerClassTypeReference ic = (InnerClassTypeReference)((TypeReferenceExpression)fre.TargetObject).TypeReference;
			Assert.AreEqual("InnerClass", ic.Type);
			Assert.AreEqual(1, ic.GenericTypes.Count);
			Assert.AreEqual("System.Int32", ic.GenericTypes[0].Type);
			Assert.AreEqual("MyType", ic.BaseType.Type);
			Assert.AreEqual(1, ic.BaseType.GenericTypes.Count);
			Assert.AreEqual("System.String", ic.BaseType.GenericTypes[0].Type);
		}*/
	}
}
