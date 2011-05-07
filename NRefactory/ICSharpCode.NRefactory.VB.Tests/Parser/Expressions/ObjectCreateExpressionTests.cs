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
	public class ObjectCreateExpressionTests
	{
		void CheckSimpleObjectCreateExpression(ObjectCreateExpression oce)
		{
			Assert.AreEqual("MyObject", oce.CreateType.Type);
			Assert.AreEqual(3, oce.Parameters.Count);
			Assert.IsTrue(oce.ObjectInitializer.IsNull);
			
			for (int i = 0; i < oce.Parameters.Count; ++i) {
				Assert.IsTrue(oce.Parameters[i] is PrimitiveExpression);
			}
		}
		
		Expression CheckPropertyInitializationExpression(Expression e, string name)
		{
			Assert.IsInstanceOf(typeof(MemberInitializerExpression), e);
			Assert.AreEqual(name, ((MemberInitializerExpression)e).Name);
			return ((MemberInitializerExpression)e).Expression;
		}
		
		[Test]
		public void VBNetAnonymousType()
		{
			ObjectCreateExpression oce = ParseUtil.ParseExpression<ObjectCreateExpression>(
				"New With {.Id = 1, .Name= \"Bill Gates\" }");
			
			Assert.IsTrue(oce.CreateType.IsNull);
			Assert.AreEqual(0, oce.Parameters.Count);
			Assert.AreEqual(2, oce.ObjectInitializer.CreateExpressions.Count);
			
			Assert.IsInstanceOf(typeof(PrimitiveExpression), CheckPropertyInitializationExpression(oce.ObjectInitializer.CreateExpressions[0], "Id"));
			Assert.IsInstanceOf(typeof(MemberInitializerExpression), oce.ObjectInitializer.CreateExpressions[1]);
		}
		
		[Test]
		public void VBNetAnonymousTypeWithoutProperty()
		{
			ObjectCreateExpression oce = ParseUtil.ParseExpression<ObjectCreateExpression>("New With { c }");
			
			Assert.IsTrue(oce.CreateType.IsNull);
			Assert.AreEqual(0, oce.Parameters.Count);
			Assert.AreEqual(1, oce.ObjectInitializer.CreateExpressions.Count);
			
			Assert.IsInstanceOf(typeof(SimpleNameExpression), oce.ObjectInitializer.CreateExpressions[0]);
			Assert.AreEqual("c", (oce.ObjectInitializer.CreateExpressions[0] as SimpleNameExpression).Identifier);
		}
		
		[Test]
		public void VBNetSimpleObjectCreateExpressionTest()
		{
			CheckSimpleObjectCreateExpression(ParseUtil.ParseExpression<ObjectCreateExpression>("New MyObject(1, 2, 3)"));
		}
		
		[Test]
		public void VBNetInvalidTypeArgumentListObjectCreateExpressionTest()
		{
			// this test was written because this bug caused the AbstractDomVisitor to crash
			
			InvocationExpression expr = ParseUtil.ParseExpression<InvocationExpression>("WriteLine(New SomeGenericType(Of Integer, )())", true);
			Assert.IsTrue(expr.TargetObject is SimpleNameExpression);
			Assert.AreEqual("WriteLine", ((SimpleNameExpression)expr.TargetObject).Identifier);
			Assert.AreEqual(1, expr.Arguments.Count); // here a second null parameter was added incorrectly
			
			Assert.IsTrue(expr.Arguments[0] is ObjectCreateExpression);
			TypeReference typeRef = ((ObjectCreateExpression)expr.Arguments[0]).CreateType;
			Assert.AreEqual("SomeGenericType", typeRef.Type);
			Assert.AreEqual(1, typeRef.GenericTypes.Count);
			Assert.AreEqual("System.Int32", typeRef.GenericTypes[0].Type);
		}
		
		[Test]
		public void VBNetMemberInitializationTest()
		{
			ObjectCreateExpression oce = ParseUtil.ParseExpression<ObjectCreateExpression>("new Contact() With { .FirstName = \"Bill\", .LastName = \"Gates\" }");
			Assert.AreEqual(2, oce.ObjectInitializer.CreateExpressions.Count);
			
			Assert.AreEqual("FirstName", ((MemberInitializerExpression)oce.ObjectInitializer.CreateExpressions[0]).Name);
			Assert.AreEqual("LastName", ((MemberInitializerExpression)oce.ObjectInitializer.CreateExpressions[1]).Name);
			
			Assert.IsInstanceOf(typeof(PrimitiveExpression), ((MemberInitializerExpression)oce.ObjectInitializer.CreateExpressions[0]).Expression);
			Assert.IsInstanceOf(typeof(PrimitiveExpression), ((MemberInitializerExpression)oce.ObjectInitializer.CreateExpressions[1]).Expression);
		}
		
		[Test]
		public void VBNetNullableObjectCreateExpressionTest()
		{
			ObjectCreateExpression oce = ParseUtil.ParseExpression<ObjectCreateExpression>("New Integer?");
			Assert.AreEqual("System.Nullable", oce.CreateType.Type);
			Assert.AreEqual(1, oce.CreateType.GenericTypes.Count);
			Assert.AreEqual("System.Int32", oce.CreateType.GenericTypes[0].Type);
		}

		[Test]
		public void VBNetNullableObjectArrayCreateExpressionTest()
		{
			ObjectCreateExpression oce = ParseUtil.ParseExpression<ObjectCreateExpression>("New Integer?()");
			Assert.AreEqual("System.Nullable", oce.CreateType.Type);
			Assert.AreEqual(1, oce.CreateType.GenericTypes.Count);
			Assert.AreEqual("System.Int32", oce.CreateType.GenericTypes[0].Type);
		}
	}
}
