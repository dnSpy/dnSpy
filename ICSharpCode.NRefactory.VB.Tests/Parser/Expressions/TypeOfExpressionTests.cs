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
	public class TypeOfExpressionTests
	{
		#region VB.NET
		[Test]
		public void VBSimpleTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtil.ParseExpression<TypeOfExpression>("GetType(MyNamespace.N1.MyType)");
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
		}
		
		
		[Test]
		public void VBGlobalTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtil.ParseExpression<TypeOfExpression>("GetType(Global.System.Console)");
			Assert.AreEqual("System.Console", toe.TypeReference.Type);
		}
		
		[Test]
		public void VBPrimitiveTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtil.ParseExpression<TypeOfExpression>("GetType(integer)");
			Assert.AreEqual("System.Int32", toe.TypeReference.Type);
		}
		
		[Test]
		public void VBVoidTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtil.ParseExpression<TypeOfExpression>("GetType(void)");
			Assert.AreEqual("void", toe.TypeReference.Type);
		}
		
		[Test]
		public void VBArrayTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtil.ParseExpression<TypeOfExpression>("GetType(MyType())");
			Assert.AreEqual("MyType", toe.TypeReference.Type);
			Assert.AreEqual(new int[] {0}, toe.TypeReference.RankSpecifier);
		}
		
		[Test]
		public void VBGenericTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtil.ParseExpression<TypeOfExpression>("GetType(MyNamespace.N1.MyType(Of string))");
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
			Assert.AreEqual("System.String", toe.TypeReference.GenericTypes[0].Type);
		}
		
		[Test]
		public void VBUnboundTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtil.ParseExpression<TypeOfExpression>("GetType(MyType(Of ,))");
			Assert.AreEqual("MyType", toe.TypeReference.Type);
			Assert.IsTrue(toe.TypeReference.GenericTypes[0].IsNull);
			Assert.IsTrue(toe.TypeReference.GenericTypes[1].IsNull);
		}
		
		[Test]
		public void VBNestedGenericTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtil.ParseExpression<TypeOfExpression>("GetType(MyType(Of string).InnerClass(of integer).InnerInnerClass)");
			InnerClassTypeReference ic = (InnerClassTypeReference)toe.TypeReference;
			Assert.AreEqual("InnerInnerClass", ic.Type);
			Assert.AreEqual(0, ic.GenericTypes.Count);
			ic = (InnerClassTypeReference)ic.BaseType;
			Assert.AreEqual("InnerClass", ic.Type);
			Assert.AreEqual(1, ic.GenericTypes.Count);
			Assert.AreEqual("System.Int32", ic.GenericTypes[0].Type);
			Assert.AreEqual("MyType", ic.BaseType.Type);
			Assert.AreEqual(1, ic.BaseType.GenericTypes.Count);
			Assert.AreEqual("System.String", ic.BaseType.GenericTypes[0].Type);
		}
		#endregion
	}
}
