// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore]
	public class TypeOfExpressionTests
	{
		[Test]
		public void SimpleTypeOfExpressionTest()
		{
			//TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyNamespace.N1.MyType)");
			//Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
			throw new NotImplementedException();
		}
		
		/* TODO
		[Test]
		public void GlobalTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(global::System.Console)");
			Assert.AreEqual("System.Console", toe.TypeReference.Type);
		}
		
		[Test]
		public void PrimitiveTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(int)");
			Assert.AreEqual("System.Int32", toe.TypeReference.Type);
		}
		
		[Test]
		public void VoidTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(void)");
			Assert.AreEqual("System.Void", toe.TypeReference.Type);
		}
		
		[Test]
		public void ArrayTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyType[])");
			Assert.AreEqual("MyType", toe.TypeReference.Type);
			Assert.AreEqual(new int[] {0}, toe.TypeReference.RankSpecifier);
		}
		
		[Test]
		public void GenericTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyNamespace.N1.MyType<string>)");
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
			Assert.AreEqual("System.String", toe.TypeReference.GenericTypes[0].Type);
		}
		
		[Test]
		public void NestedGenericTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyType<string>.InnerClass<int>.InnerInnerClass)");
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
		
		[Test]
		public void NullableTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyStruct?)");
			Assert.AreEqual("System.Nullable", toe.TypeReference.Type);
			Assert.AreEqual("MyStruct", toe.TypeReference.GenericTypes[0].Type);
		}
		
		[Test]
		public void UnboundTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyType<,>)");
			Assert.AreEqual("MyType", toe.TypeReference.Type);
			Assert.IsTrue(toe.TypeReference.GenericTypes[0].IsNull);
			Assert.IsTrue(toe.TypeReference.GenericTypes[1].IsNull);
		}*/
	}
}
