// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore("tests need to be ported")]
	public class DefaultValueExpressionTests
	{
		[Test]
		public void SimpleDefaultValue()
		{
			DefaultValueExpression toe = ParseUtilCSharp.ParseExpression<DefaultValueExpression>("default(T)");
			Assert.AreEqual("T", toe.Type);
		}
		
		/*
		[Test]
		public void FullQualifiedDefaultValue()
		{
			DefaultValueExpression toe = ParseUtilCSharp.ParseExpression<DefaultValueExpression>("default(global::MyNamespace.N1.MyType)");
			Assert.IsTrue(toe.TypeReference.IsGlobal);
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
		}
		
		[Test]
		public void GenericDefaultValue()
		{
			DefaultValueExpression toe = ParseUtilCSharp.ParseExpression<DefaultValueExpression>("default(MyNamespace.N1.MyType<string>)");
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
			Assert.AreEqual("System.String", toe.TypeReference.GenericTypes[0].Type);
		}
		
		[Test]
		public void DefaultValueAsIntializer()
		{
			// This test is failing because we need a resolver for the "default:" / "default(" conflict.
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("T a = default(T);");
			DefaultValueExpression dve = (DefaultValueExpression)lvd.Variables[0].Initializer;
			Assert.AreEqual("T", dve.TypeReference.Type);
		}
		
		[Test]
		public void DefaultValueInReturnStatement()
		{
			ReturnStatement rs = ParseUtilCSharp.ParseStatement<ReturnStatement>("return default(T);");
			DefaultValueExpression dve = (DefaultValueExpression)rs.Expression;
			Assert.AreEqual("T", dve.TypeReference.Type);
		}*/
	}
}
