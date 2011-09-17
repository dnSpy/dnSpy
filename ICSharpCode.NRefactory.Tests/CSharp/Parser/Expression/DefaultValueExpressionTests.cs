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
	public class DefaultValueExpressionTests
	{
		[Test]
		public void SimpleDefaultValue()
		{
			DefaultValueExpression toe = ParseUtilCSharp.ParseExpression<DefaultValueExpression>("default(T)");
			Assert.AreEqual("T", ((SimpleType)toe.Type).Identifier);
		}
		
		[Test]
		public void FullQualifiedDefaultValue()
		{
			ParseUtilCSharp.AssertExpression(
				"default(global::MyNamespace.N1.MyType)",
				new DefaultValueExpression {
					Type = new MemberType {
						Target = new MemberType {
							Target = new MemberType {
								Target = new SimpleType("global"),
								IsDoubleColon = true,
								MemberName = "MyNamespace"
							},
							MemberName = "N1"
						},
						MemberName = "MyType"
					}
				});
		}
		
		[Test]
		public void GenericDefaultValue()
		{
			ParseUtilCSharp.AssertExpression(
				"default(MyNamespace.N1.MyType<string>)",
				new DefaultValueExpression {
					Type = new MemberType {
						Target = new MemberType {
							Target = new SimpleType("MyNamespace"),
							MemberName = "N1"
						},
						MemberName = "MyType",
						TypeArguments = { new PrimitiveType("string") }
					}
				});
		}
		
		[Test]
		public void DefaultValueAsIntializer()
		{
			// This test was problematic (in old NRefactory) because we need a resolver for the "default:" / "default(" conflict.
			ParseUtilCSharp.AssertStatement(
				"T a = default(T);",
				new VariableDeclarationStatement {
					Type = new SimpleType("T"),
					Variables = {
						new VariableInitializer("a", new DefaultValueExpression { Type = new SimpleType("T") })
					}});
		}
		
		[Test]
		public void DefaultValueInReturnStatement()
		{
			ParseUtilCSharp.AssertStatement(
				"return default(T);",
				new ReturnStatement {
					Expression = new DefaultValueExpression { Type = new SimpleType("T") }
				});
		}
	}
}
