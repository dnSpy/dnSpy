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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.GeneralScope
{
	[TestFixture]
	public class AttributeSectionTests
	{
		[Test]
		public void AttributesUsingNamespaceAlias()
		{
			string program = @"[global::Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
[someprefix::DesignerGenerated()]
public class Form1 {
}";
			
			TypeDeclaration decl = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			Assert.AreEqual(2, decl.Attributes.Count);
			Assert.AreEqual("global::Microsoft.VisualBasic.CompilerServices.DesignerGenerated",
			                decl.Attributes.First().Attributes.Single().Type.ToString());
			Assert.AreEqual("someprefix::DesignerGenerated", decl.Attributes.Last().Attributes.Single().Type.ToString());
		}
		
		[Test]
		public void AssemblyAttribute()
		{
			string program = @"[assembly: System.Attribute()]";
			AttributeSection decl = ParseUtilCSharp.ParseGlobal<AttributeSection>(program);
			Assert.AreEqual(new TextLocation(1, 1), decl.StartLocation);
			Assert.AreEqual("assembly", decl.AttributeTarget);
		}
		
		[Test]
		public void ModuleAttribute()
		{
			string program = @"[module: System.Attribute()]";
			AttributeSection decl = ParseUtilCSharp.ParseGlobal<AttributeSection>(program);
			Assert.AreEqual(new TextLocation(1, 1), decl.StartLocation);
			Assert.AreEqual("module", decl.AttributeTarget);
		}
		
		[Test]
		public void TypeAttribute()
		{
			string program = @"[type: System.Attribute()] class Test {}";
			TypeDeclaration type = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			AttributeSection decl = type.Attributes.Single();
			Assert.AreEqual(new TextLocation(1, 1), decl.StartLocation);
			Assert.AreEqual("type", decl.AttributeTarget);
		}
		
		[Test]
		public void AttributeWithoutParenthesis()
		{
			string program = @"[Attr] class Test {}";
			TypeDeclaration type = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			var attr = type.Attributes.Single().Attributes.Single();
			Assert.IsTrue(attr.GetChildByRole(Roles.LPar).IsNull);
			Assert.IsTrue(attr.GetChildByRole(Roles.RPar).IsNull);
		}
		
		[Test, Ignore("Parser bug - parenthesis are missing")]
		public void AttributeWithEmptyParenthesis()
		{
			string program = @"[Attr()] class Test {}";
			TypeDeclaration type = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			var attr = type.Attributes.Single().Attributes.Single();
			Assert.IsFalse(attr.GetChildByRole(Roles.LPar).IsNull);
			Assert.IsFalse(attr.GetChildByRole(Roles.RPar).IsNull);
		}
		
		[Test]
		public void TwoAttributesInSameSection()
		{
			ParseUtilCSharp.AssertGlobal(
				@"[A, B] class Test {}",
				new TypeDeclaration {
					ClassType = ClassType.Class,
					Name = "Test",
					Attributes = {
						new AttributeSection {
							Attributes = {
								new Attribute { Type = new SimpleType("A") },
								new Attribute { Type = new SimpleType("B") }
							}
						}}});
		}
		
		[Test]
		public void AttributesOnTypeParameter()
		{
			ParseUtilCSharp.AssertGlobal(
				"class Test<[A,B]C> {}",
				new TypeDeclaration {
					ClassType = ClassType.Class,
					Name = "Test",
					TypeParameters = {
						new TypeParameterDeclaration {
							Attributes = {
								new AttributeSection {
									Attributes = {
										new Attribute { Type = new SimpleType("A") },
										new Attribute { Type = new SimpleType("B") }
									}
								}
							},
							Name = "C"
						}
					}});
		}
		
		[Test]
		public void AttributeOnMethodParameter()
		{
			ParseUtilCSharp.AssertTypeMember(
				"void M([In] int p);",
				new MethodDeclaration {
					ReturnType = new PrimitiveType("void"),
					Name = "M",
					Parameters = {
						new ParameterDeclaration {
							Attributes = { new AttributeSection(new Attribute { Type = new SimpleType("In") }) },
							Type = new PrimitiveType("int"),
							Name = "p"
						}
					}});
		}
		
		[Test]
		public void AttributeOnSetterValue()
		{
			ParseUtilCSharp.AssertTypeMember(
				"int P { get; [param: In] set; }",
				new PropertyDeclaration {
					ReturnType = new PrimitiveType("int"),
					Name = "P",
					Getter = new Accessor(),
					Setter = new Accessor {
						Attributes = {
							new AttributeSection {
								AttributeTarget = "param",
								Attributes = { new Attribute { Type = new SimpleType("In") } },
							} },
					}});
		}
		
		// TODO: Tests for other contexts where attributes can appear
		
		[Test]
		public void AttributeWithNamedArguments ()
		{
			ParseUtilCSharp.AssertTypeMember (
				@"[A(0, a:1, b=2)] class Test {}",
				new TypeDeclaration {
					ClassType = ClassType.Class,
					Name = "Test",
					Attributes = {
						new AttributeSection {
							Attributes = {
								new Attribute {
									Type = new SimpleType("A"),
									Arguments = {
										new PrimitiveExpression(0),
										new NamedArgumentExpression("a", new PrimitiveExpression(1)),
										new NamedExpression("b", new PrimitiveExpression(2))
									}
								}
							}
						}
					}});
		}
		
		[Ignore("Fixme!")]
		[Test]
		public void AssemblyAttributeBeforeNamespace()
		{
			var cu = new CSharpParser().Parse(new StringReader("using System; [assembly: Attr] namespace X {}"), "code.cs");
			Assert.AreEqual(
				new Type[] {
					typeof(UsingDeclaration),
					typeof(AttributeSection),
					typeof(NamespaceDeclaration)
				}, cu.Children.Select(c => c.GetType()).ToArray());
		}
	}
}
