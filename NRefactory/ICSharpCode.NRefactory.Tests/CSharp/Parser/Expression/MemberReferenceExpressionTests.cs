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
		
		[Test]
		public void ShortMaxValueTest()
		{
			ParseUtilCSharp.AssertExpression(
				"short.MaxValue",
				new PrimitiveType("short").Member("MaxValue")
			);
		}
		
		[Test]
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
					MemberName = "SomeClass",
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
			
			target.AddChild (new PrimitiveType("string"), Roles.TypeArgument);
			
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
		
		[Test]
		public void AliasedNamespace()
		{
			ParseUtilCSharp.AssertExpression(
				"a::b.c",
				new MemberReferenceExpression {
					Target = new TypeReferenceExpression {
						Type = new MemberType {
							Target = new SimpleType("a"),
							IsDoubleColon = true,
							MemberName = "b"
						}
					},
					MemberName = "c"
				});
		}
	}
}
