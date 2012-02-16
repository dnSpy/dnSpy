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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Microsoft.CSharp;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp
{
	[TestFixture]
	public class CodeDomConvertVisitorTests : ResolverTestBase
	{
		CodeDomConvertVisitor convertVisitor;
		CSharpParsedFile parsedFile;
		
		public override void SetUp()
		{
			base.SetUp();
			parsedFile = new CSharpParsedFile("test.cs");
			parsedFile.RootUsingScope.Usings.Add(MakeReference("System"));
			parsedFile.RootUsingScope.Usings.Add(MakeReference("System.Collections.Generic"));
			parsedFile.RootUsingScope.Usings.Add(MakeReference("System.Linq"));
			
			convertVisitor = new CodeDomConvertVisitor();
			convertVisitor.UseFullyQualifiedTypeNames = true;
		}
		
		string Convert(Expression expr)
		{
			CSharpResolver resolver = new CSharpResolver(compilation);
			resolver = resolver.WithCurrentUsingScope(parsedFile.RootUsingScope.Resolve(compilation));
			resolver = resolver.WithCurrentTypeDefinition(compilation.FindType(KnownTypeCode.Object).GetDefinition());
			var codeExpr = (CodeExpression)convertVisitor.Convert(expr, new CSharpAstResolver(resolver, expr, parsedFile));
			
			StringWriter writer = new StringWriter();
			writer.NewLine = " ";
			new CSharpCodeProvider().GenerateCodeFromExpression(codeExpr, writer, new CodeGeneratorOptions { IndentString = " " });
			return Regex.Replace(writer.ToString(), @"\s+", " ");
		}
		
		[Test]
		public void CreateArray()
		{
			Assert.AreEqual("new int[10]", Convert(
				new ArrayCreateExpression {
					Type = new PrimitiveType("int"),
					Arguments = { new PrimitiveExpression(10) }
				}));
		}
		
		[Test]
		public void CreateJaggedArray()
		{
			Assert.AreEqual("new int[10][]", Convert(
				new ArrayCreateExpression {
					Type = new PrimitiveType("int"),
					Arguments = { new PrimitiveExpression(10) },
					AdditionalArraySpecifiers = { new ArraySpecifier() }
				}));
		}
		
		[Test]
		public void Create2DArray()
		{
			Assert.AreEqual("new int[10, 20]", Convert(
				new ArrayCreateExpression {
					Type = new PrimitiveType("int"),
					Arguments = { new PrimitiveExpression(10), new PrimitiveExpression(20) }
				}));
		}
		
		[Test]
		public void CreateImplicitlyTypedArray()
		{
			// implicitly-typed array not supported in CodeDom, so the conversion should infer the type
			Assert.AreEqual("new int[] { 1, 2, 3}", Convert(
				new ArrayCreateExpression {
					AdditionalArraySpecifiers = { new ArraySpecifier() },
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new PrimitiveExpression(1),
							new PrimitiveExpression(2),
							new PrimitiveExpression(3)
						}
					}
				}));
		}
		
		[Test]
		public void Create2DImplicitlyTypedArray()
		{
			Assert.AreEqual("new int[,] { { 1, 2 }, { 3, 4 }}", Convert(
				new ArrayCreateExpression {
					AdditionalArraySpecifiers = { new ArraySpecifier(2) },
					Initializer = new ArrayInitializerExpression {
						Elements = {
							new ArrayInitializerExpression(new PrimitiveExpression(1), new PrimitiveExpression(2)),
							new ArrayInitializerExpression(new PrimitiveExpression(3), new PrimitiveExpression(4))
						}
					}
				}));
		}
		
		[Test]
		public void AdditionOperator()
		{
			Assert.AreEqual("(0 + 1)", Convert(
				new BinaryOperatorExpression(new PrimitiveExpression(0), BinaryOperatorType.Add, new PrimitiveExpression(1))));
		}
		
		[Test]
		public void EqualityOperator()
		{
			Assert.AreEqual("(0 == 1)", Convert(
				new BinaryOperatorExpression(new PrimitiveExpression(0), BinaryOperatorType.Equality, new PrimitiveExpression(1))));
		}
		
		[Test]
		public void InEqualityOperator()
		{
			Assert.AreEqual("((0 == 1) == false)", Convert(
				new BinaryOperatorExpression(new PrimitiveExpression(0), BinaryOperatorType.InEquality, new PrimitiveExpression(1))));
		}
		
		[Test]
		public void ReferenceInEqualityOperator()
		{
			Assert.AreEqual("(default(object) != null)", Convert(
				new BinaryOperatorExpression(new DefaultValueExpression(new PrimitiveType("object")), BinaryOperatorType.InEquality, new NullReferenceExpression())));
		}
		
		[Test]
		public void StaticProperty()
		{
			Assert.AreEqual("System.Environment.TickCount", Convert(
				new IdentifierExpression("Environment").Member("TickCount")));
		}
		
		[Test]
		public void InstanceMethodInvocation()
		{
			Assert.AreEqual("this.Equals(null)",
			                Convert(new IdentifierExpression("Equals").Invoke(new NullReferenceExpression())));
		}
		
		[Test]
		public void StaticMethodInvocation()
		{
			Assert.AreEqual("object.Equals(null, null)",
			                Convert(new IdentifierExpression("Equals").Invoke(new NullReferenceExpression(), new NullReferenceExpression())));
		}
		
		[Test]
		public void NotOperator()
		{
			Assert.AreEqual("(a == false)", Convert(new UnaryOperatorExpression(UnaryOperatorType.Not, new IdentifierExpression("a"))));
		}
	}
}
