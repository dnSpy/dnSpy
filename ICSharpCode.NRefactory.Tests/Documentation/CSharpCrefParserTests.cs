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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Parser;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Documentation
{
	[TestFixture]
	public class CSharpCrefParserTests
	{
		[Test]
		public void M()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"M",
				new DocumentationReference {
					MemberName = "M"
				});
		}
		
		[Test]
		[Ignore("mcs bug")]
		public void This()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"this",
				new DocumentationReference {
					EntityType = EntityType.Indexer
				});
		}
		
		[Test]
		[Ignore("mcs bug (Unexpected symbol `this', expecting `explicit', `implicit', `operator', or `type')")]
		public void ThisWithParameter()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"this[int]",
				new DocumentationReference {
					EntityType = EntityType.Indexer,
					HasParameterList = true,
					Parameters = { new ParameterDeclaration { Type = new PrimitiveType("int") } }
				});
		}
		
		[Test]
		public void ThisWithDeclaringType()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"List{T}.this",
				new DocumentationReference {
					EntityType = EntityType.Indexer,
					DeclaringType = new SimpleType("List", new SimpleType("T"))
				});
		}
		
		[Test]
		public void NestedTypeInGenericType()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"List{T}.Enumerator",
				new DocumentationReference {
					DeclaringType = new SimpleType("List", new SimpleType("T")),
					MemberName = "Enumerator"
				});
		}
		
		[Test]
		public void GenericTypeWithFullNamespace()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"System.Collections.Generic.List{T}",
				new DocumentationReference {
					DeclaringType = new SimpleType("System").MemberType("Collections").MemberType("Generic"),
					MemberName = "List",
					TypeArguments = { new SimpleType("T") }
				});
		}
		
		[Test]
		public void PrimitiveType()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"int",
				new DocumentationReference {
					EntityType = EntityType.TypeDefinition,
					DeclaringType = new PrimitiveType("int")
				});
		}
		
		[Test]
		public void VerbatimIdentifier()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"@int",
				new DocumentationReference {
					MemberName = "int"
				});
		}
		
		[Test]
		public void IntParse()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"int.Parse(string)",
				new DocumentationReference {
					DeclaringType = new PrimitiveType("int"),
					MemberName = "Parse",
					HasParameterList = true,
					Parameters = {
						new ParameterDeclaration { Type = new PrimitiveType("string") }
					}
				});
		}
		
		[Test]
		public void Generic()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"IGeneric{X, Y}",
				new DocumentationReference {
					MemberName = "IGeneric",
					TypeArguments = { new SimpleType("X"), new SimpleType("Y") }
				});
		}
		
		[Test]
		public void MixedGeneric()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"IGeneric<X, Y}",
				new DocumentationReference {
					MemberName = "IGeneric",
					TypeArguments = { new SimpleType("X"), new SimpleType("Y") }
				});
		}
		
		[Test]
		public void MethodInGeneric()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"IGeneric{X, Y}.Test",
				new DocumentationReference {
					DeclaringType = new SimpleType("IGeneric", new SimpleType("X"), new SimpleType("Y")),
					MemberName = "Test"
				});
		}
		
		[Test]
		public void GenericMethodInGeneric()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"IGeneric{X, Y}.Test{Z}",
				new DocumentationReference {
					DeclaringType = new SimpleType("IGeneric", new SimpleType("X"), new SimpleType("Y")),
					MemberName = "Test",
					TypeArguments = { new SimpleType("Z") }
				});
		}
		
		[Test]
		public void GenericMethodInGenericWithParameterList()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"IGeneric{X, Y}.Test{Z}(ref Z[,])",
				new DocumentationReference {
					DeclaringType = new SimpleType("IGeneric", new SimpleType("X"), new SimpleType("Y")),
					MemberName = "Test",
					TypeArguments = { new SimpleType("Z") },
					HasParameterList = true,
					Parameters = {
						new ParameterDeclaration {
							ParameterModifier = ParameterModifier.Ref,
							Type = new SimpleType("Z").MakeArrayType(2)
						}
					}});
		}
		
		[Test]
		public void EmptyParameterList()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"Window1()",
				new DocumentationReference {
					MemberName = "Window1",
					HasParameterList = true
				});
		}
		
		[Test]
		public void OperatorPlus()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"operator +",
				new DocumentationReference {
					EntityType = EntityType.Operator,
					OperatorType = OperatorType.Addition
				});
		}
		
		[Test]
		[Ignore("mcs bug (Unexpected symbol `operator', expecting `identifier' or `this')")]
		public void OperatorPlusWithDeclaringType()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"Test.operator +",
				new DocumentationReference {
					DeclaringType = new SimpleType("Test"),
					EntityType = EntityType.Operator,
					OperatorType = OperatorType.Addition
				});
		}
		
		[Test]
		public void OperatorPlusWithParameterList()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"operator +(Test, int)",
				new DocumentationReference {
					EntityType = EntityType.Operator,
					OperatorType = OperatorType.Addition,
					HasParameterList = true,
					Parameters = {
						new ParameterDeclaration { Type = new SimpleType("Test") },
						new ParameterDeclaration { Type = new PrimitiveType("int") }
					}});
		}
		
		[Test]
		public void ImplicitOperator()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"implicit operator int",
				new DocumentationReference {
					EntityType = EntityType.Operator,
					OperatorType = OperatorType.Implicit,
					ConversionOperatorReturnType = new PrimitiveType("int")
				});
		}
		
		[Test]
		public void ExplicitOperatorWithParameterList()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"explicit operator int(Test)",
				new DocumentationReference {
					EntityType = EntityType.Operator,
					OperatorType = OperatorType.Explicit,
					ConversionOperatorReturnType = new PrimitiveType("int"),
					HasParameterList = true,
					Parameters = {
						new ParameterDeclaration { Type = new SimpleType("Test") },
					}
				});
		}
		
		[Test]
		[Ignore("mcs bug (Unexpected symbol `explicit', expecting `identifier' or `this')")]
		public void ExplicitOperatorWithParameterListAndDeclaringType()
		{
			ParseUtilCSharp.AssertDocumentationReference(
				"Test.explicit operator int(Test)",
				new DocumentationReference {
					EntityType = EntityType.Operator,
					OperatorType = OperatorType.Explicit,
					DeclaringType = new SimpleType("Test"),
					ConversionOperatorReturnType = new PrimitiveType("int"),
					HasParameterList = true,
					Parameters = {
						new ParameterDeclaration { Type = new SimpleType("Test") },
					}
				});
		}
	}
}
