// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
		
		[Test]
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
		
		[Test]
		public void UnboundTypeOfExpressionTest()
		{
			var type = new SimpleType("MyType");
			type.AddChild (new SimpleType (), SimpleType.Roles.TypeArgument);
			type.AddChild (new SimpleType (), SimpleType.Roles.TypeArgument);
			ParseUtilCSharp.AssertExpression(
				"typeof(MyType<,>)",
				new TypeOfExpression {
					Type = type
				});
		}
		
		[Test]
		public void NestedArraysTest()
		{
			ParseUtilCSharp.AssertExpression(
				"typeof(int[,][])",
				new TypeOfExpression {
					Type = new ComposedType {
						BaseType = new PrimitiveType("int"),
						ArraySpecifiers = {
							new ArraySpecifier(2),
							new ArraySpecifier(1)
						}
					}});
		}
	}
}
