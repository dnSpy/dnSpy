// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class MemberReferenceExpressionTests
	{
		[Test]
		public void SimpleFieldReferenceExpressionTest()
		{
			ParseUtilCSharp.AssertExpression(
				"myTargetObject.myField",
				new IdentifierExpression("myTargetObject").Member("myField")
			);
		}
		
		[Test, Ignore("parser is broken and produces IdentifierExpression instead of PrimitiveType")]
		public void ShortMaxValueTest()
		{
			ParseUtilCSharp.AssertExpression(
				"short.MaxValue",
				new PrimitiveType("short").Member("MaxValue")
			);
		}
		
		[Test, Ignore("Parsing of @-identifiers is broken")]
		public void IdentShortMaxValueTest()
		{
			ParseUtilCSharp.AssertExpression(
				"@short.MaxValue",
				new IdentifierExpression("short").Member("MaxValue")
			);
		}
		
		[Test]
		public void GenericFieldReferenceExpressionTest()
		{
			ParseUtilCSharp.AssertExpression(
				"SomeClass<string>.myField",
				new IdentifierExpression("SomeClass") { TypeArguments = { new PrimitiveType("string") } }.Member("myField")
			);
		}
		
		[Test]
		public void FullNamespaceGenericFieldReferenceExpressionTest()
		{
			ParseUtilCSharp.AssertExpression(
				"Namespace.Subnamespace.SomeClass<string>.myField",
				new MemberReferenceExpression {
					Target = new IdentifierExpression("Namespace").Member("Subnamespace"),
					TypeArguments = { new PrimitiveType("string") }
				}.Member("myField")
			);
		}
		
		[Test]
		public void GlobalFullNamespaceGenericFieldReferenceExpressionTest()
		{
			var target = new MemberType {
						Target = new SimpleType("global"),
						IsDoubleColon = true,
						MemberName = "Namespace"
					}.Member("Subnamespace").Member ("SomeClass");
			
			target.AddChild (new PrimitiveType("string"), MemberReferenceExpression.Roles.TypeArgument);
			
			ParseUtilCSharp.AssertExpression(
				"global::Namespace.Subnamespace.SomeClass<string>.myField",
				new MemberReferenceExpression {
					Target = target,
					MemberName = "myField"
				}
			);
		}
		
		[Test]
		public void NestedGenericFieldReferenceExpressionTest()
		{
			ParseUtilCSharp.AssertExpression(
				"MyType<string>.InnerClass<int>.myField",
				new MemberReferenceExpression {
					Target = new IdentifierExpression("MyType") { TypeArguments = { new PrimitiveType("string") } },
					MemberName = "InnerClass",
					TypeArguments = { new PrimitiveType("int") }
				}.Member("myField")
			);
		}
	}
}
