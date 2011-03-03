// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore("Aliases not yet implemented")]
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
