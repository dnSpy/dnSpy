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
	public class UndocumentedExpressionTests
	{
		[Test, Ignore("Name on ParameterDeclaration is missing")]
		public void ArglistAccess()
		{
			ParseUtilCSharp.AssertTypeMember(
				@"public static int GetArgCount(__arglist)
				{
					ArgIterator argIterator = new ArgIterator(__arglist);
					return argIterator.GetRemainingCount();
				}",
				new MethodDeclaration {
					Modifiers = Modifiers.Public | Modifiers.Static,
					ReturnType = new PrimitiveType("int"),
					Name = "GetArgCount",
					Parameters = {
						new ParameterDeclaration(AstType.Null, "__arglist")
					},
					Body = new BlockStatement {
						new VariableDeclarationStatement(
							new SimpleType("ArgIterator"),
							"argIterator",
							new ObjectCreateExpression(
								new SimpleType("ArgIterator"),
								new UndocumentedExpression {
									UndocumentedExpressionType = UndocumentedExpressionType.ArgListAccess
								})
						),
						new ReturnStatement(new IdentifierExpression("argIterator").Invoke("GetRemainingCount"))
					}});
		}
		
		[Test]
		public void ArglistCall()
		{
			ParseUtilCSharp.AssertExpression(
				"GetArgCount(__arglist(a, b, c))",
				new IdentifierExpression("GetArgCount").Invoke(
					new UndocumentedExpression {
						UndocumentedExpressionType = UndocumentedExpressionType.ArgList,
						Arguments = {
							new IdentifierExpression("a"),
							new IdentifierExpression("b"),
							new IdentifierExpression("c")
						}
					}));
		}
		
		[Test]
		public void MakeTypedRef()
		{
			ParseUtilCSharp.AssertStatement(
				"TypedReference tr = __makeref(o);",
				new VariableDeclarationStatement(
					new SimpleType("TypedReference"),
					"tr",
					new UndocumentedExpression {
						UndocumentedExpressionType = UndocumentedExpressionType.MakeRef,
						Arguments = { new IdentifierExpression("o") }
					}));
		}
		
		[Test]
		public void RefType()
		{
			ParseUtilCSharp.AssertExpression(
				"t = __reftype(tr)",
				new AssignmentExpression(
					new IdentifierExpression("t"),
					new UndocumentedExpression {
						UndocumentedExpressionType = UndocumentedExpressionType.RefType,
						Arguments = { new IdentifierExpression("tr") }
					}));
		}
		
		[Test]
		public void GetRefValue()
		{
			ParseUtilCSharp.AssertExpression(
				"o = __refvalue(tr, object)",
				new AssignmentExpression(
					new IdentifierExpression("o"),
					new UndocumentedExpression {
						UndocumentedExpressionType = UndocumentedExpressionType.RefValue,
						Arguments = {
							new IdentifierExpression("tr"),
							new TypeReferenceExpression(new PrimitiveType("object"))
						}
					}));
		}
		
		[Test]
		public void SetRefValue()
		{
			ParseUtilCSharp.AssertExpression(
				"__refvalue(tr, object) = o",
				new AssignmentExpression(
					new UndocumentedExpression {
						UndocumentedExpressionType = UndocumentedExpressionType.RefValue,
						Arguments = {
							new IdentifierExpression("tr"),
							new TypeReferenceExpression(new PrimitiveType("object"))
						}
					},
					new IdentifierExpression("o")));
		}
	}
}
