// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class TypeOfExpressionTests
	{
		[Test]
		public void SimpleTypeOfExpressionTest()
		{
			ParseUtilCSharp.AssertExpression(
				"typeof(MyNamespace.N1.MyType)",
				new TypeOfExpression {
					Type = new MemberType {
						Target = new MemberType {
							Target = new SimpleType("MyNamespace"),
							MemberName = "N1"
						},
						MemberName = "MyType"
					}});
		}
		
		[Test, Ignore("Aliases not yet implemented")]
		public void GlobalTypeOfExpressionTest()
		{
			ParseUtilCSharp.AssertExpression(
				"typeof(global::System.Console)",
				new TypeOfExpression {
					Type = new MemberType {
						Target = new MemberType {
							Target = new SimpleType("global"),
							IsDoubleColon = true,
							MemberName = "System"
						},
						MemberName = "Console"
					}});
		}
		
		[Test]
		public void PrimitiveTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(int)");
			Assert.AreEqual("int", ((PrimitiveType)toe.Type).Keyword);
		}
		
		[Test]
		public void VoidTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(void)");
			Assert.AreEqual("void", ((PrimitiveType)toe.Type).Keyword);
		}
		
		[Test]
		public void ArrayTypeOfExpressionTest()
		{
			ParseUtilCSharp.AssertExpression(
				"typeof(MyType[])",
				new TypeOfExpression {
					Type = new SimpleType("MyType").MakeArrayType()
				});
		}
		
		[Test]
		public void GenericTypeOfExpressionTest()
		{
			ParseUtilCSharp.AssertExpression(
				"typeof(MyNamespace.N1.MyType<string>)",
				new TypeOfExpression {
					Type = new MemberType {
						Target = new MemberType {
							Target = new SimpleType("MyNamespace"),
							MemberName = "N1"
						},
						MemberName = "MyType",
						TypeArguments = { new PrimitiveType("string") }
					}});
		}
		
		[Test]
		public void NestedGenericTypeOfExpressionTest()
		{
			ParseUtilCSharp.AssertExpression(
				"typeof(MyType<string>.InnerClass<int>.InnerInnerClass)",
				new TypeOfExpression {
					Type = new MemberType {
						Target = new MemberType {
							Target = new SimpleType("MyType") { TypeArguments = { new PrimitiveType("string") } },
							MemberName = "InnerClass",
							TypeArguments = { new PrimitiveType("int") }
						},
						MemberName = "InnerInnerClass"
					}});
		}
		
		[Test]
		public void NullableTypeOfExpressionTest()
		{
			ParseUtilCSharp.AssertExpression(
				"typeof(MyStruct?)",
				new TypeOfExpression {
					Type = new ComposedType {
						BaseType = new SimpleType("MyType"),
						HasNullableSpecifier = true
					}});
		}
		
		[Test, Ignore("How do we represent unbound types in the AST?")]
		public void UnboundTypeOfExpressionTest()
		{
			TypeOfExpression toe = ParseUtilCSharp.ParseExpression<TypeOfExpression>("typeof(MyType<,>)");
			throw new NotImplementedException("How do we represent unbound types in the AST?");
		}
	}
}
