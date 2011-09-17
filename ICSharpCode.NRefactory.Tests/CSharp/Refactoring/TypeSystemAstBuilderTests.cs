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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[TestFixture]
	public class TypeSystemAstBuilderTests
	{
		const string program = @"
using System;
using System.Collections.Generic;
using OtherNS;

class Base<T> {
	public class Nested<X> { }
}
class Derived<T, S> : Base<S> { }

namespace NS {
	using R = global::System.Reflection;
	using L = List<char>;
	
	class System { }
}
namespace OtherNS {
	class Array { }
}
";
		
		SimpleProjectContent pc;
		ITypeResolveContext ctx;
		ITypeDefinition baseClass, derivedClass, nestedClass, systemClass;
		CSharpParsedFile parsedFile;
		
		[SetUp]
		public void SetUp()
		{
			pc = new SimpleProjectContent();
			var cu = new CSharpParser().Parse(new StringReader(program));
			parsedFile = new TypeSystemConvertVisitor(pc, "program.cs").Convert(cu);
			pc.UpdateProjectContent(null, parsedFile);
			
			ctx = new CompositeTypeResolveContext(new[] { pc, CecilLoaderTests.Mscorlib });
			
			baseClass = pc.GetTypeDefinition(string.Empty, "Base", 1, StringComparer.Ordinal);
			nestedClass = baseClass.NestedTypes.Single();
			derivedClass = pc.GetTypeDefinition(string.Empty, "Derived", 2, StringComparer.Ordinal);
			systemClass = pc.GetTypeDefinition("NS", "System", 0, StringComparer.Ordinal);
		}
		
		TypeSystemAstBuilder CreateBuilder(ITypeDefinition currentTypeDef = null)
		{
			return new TypeSystemAstBuilder(
				new CSharpResolver(ctx) {
					CurrentUsingScope = currentTypeDef != null ? parsedFile.GetUsingScope(currentTypeDef.Region.Begin) : parsedFile.RootUsingScope,
					CurrentTypeDefinition = currentTypeDef
				});
		}
		
		string TypeToString(ITypeReference type, ITypeDefinition currentTypeDef = null)
		{
			var builder = CreateBuilder(currentTypeDef);
			IType resolvedType = type.Resolve(ctx);
			AstType node = builder.ConvertType(resolvedType);
			return node.ToString();
		}
		
		[Test]
		public void PrimitiveVoid()
		{
			Assert.AreEqual("void", TypeToString(KnownTypeReference.Void));
		}
		
		[Test]
		public void PrimitiveInt()
		{
			Assert.AreEqual("int", TypeToString(KnownTypeReference.Int32));
		}
		
		[Test]
		public void PrimitiveDecimal()
		{
			Assert.AreEqual("decimal", TypeToString(KnownTypeReference.Decimal));
		}
		
		[Test]
		public void SystemType()
		{
			Assert.AreEqual("Type", TypeToString(KnownTypeReference.Type));
		}
		
		[Test]
		public void ListOfNSSystem()
		{
			var type = new ParameterizedType(ctx.GetTypeDefinition(typeof(List<>)), new[] { systemClass });
			Assert.AreEqual("List<NS.System>", TypeToString(type));
			Assert.AreEqual("List<System>", TypeToString(type, systemClass));
		}
		
		[Test]
		public void NonGenericIEnumerable()
		{
			Assert.AreEqual("System.Collections.IEnumerable", TypeToString(typeof(IEnumerable).ToTypeReference()));
		}
		
		[Test]
		public void NonGenericIEnumerableWithSystemNamespaceCollision()
		{
			Assert.AreEqual("global::System.Collections.IEnumerable", TypeToString(typeof(IEnumerable).ToTypeReference(), systemClass));
		}
		
		[Test]
		public void AliasedNamespace()
		{
			var type = typeof(System.Reflection.Assembly).ToTypeReference();
			Assert.AreEqual("R.Assembly", TypeToString(type, systemClass));
		}
		
		[Test]
		public void AliasedType()
		{
			var type = new ParameterizedTypeReference(ctx.GetTypeDefinition(typeof(List<>)), new[] { KnownTypeReference.Char });
			Assert.AreEqual("List<char>", TypeToString(type));
			Assert.AreEqual("L", TypeToString(type, systemClass));
		}
		
		[Test]
		public void UnboundType()
		{
			Assert.AreEqual("Base<>", TypeToString(baseClass));
			Assert.AreEqual("Base<>.Nested<>", TypeToString(nestedClass));
		}
		
		[Test]
		public void NestedType()
		{
			var type = new ParameterizedTypeReference(nestedClass, new[] { KnownTypeReference.Char, KnownTypeReference.String });
			Assert.AreEqual("Base<char>.Nested<string>", TypeToString(type));
			Assert.AreEqual("Base<char>.Nested<string>", TypeToString(type, baseClass));
			Assert.AreEqual("Base<char>.Nested<string>", TypeToString(type, nestedClass));
			Assert.AreEqual("Base<char>.Nested<string>", TypeToString(type, derivedClass));
		}
		
		[Test]
		public void NestedTypeInCurrentClass()
		{
			var type = new ParameterizedTypeReference(nestedClass, new[] { baseClass.TypeParameters[0], KnownTypeReference.String });
			Assert.AreEqual("Nested<string>", TypeToString(type, baseClass));
			Assert.AreEqual("Nested<string>", TypeToString(type, nestedClass));
		}
		
		[Test]
		public void NestedTypeInDerivedClass()
		{
			var type1 = new ParameterizedTypeReference(nestedClass, new[] { derivedClass.TypeParameters[0], KnownTypeReference.String });
			Assert.AreEqual("Base<T>.Nested<string>", TypeToString(type1, derivedClass));
			
			var type2 = new ParameterizedTypeReference(nestedClass, new[] { derivedClass.TypeParameters[1], KnownTypeReference.String });
			Assert.AreEqual("Nested<string>", TypeToString(type2, derivedClass));
		}
		
		[Test]
		public void MultidimensionalArray()
		{
			Assert.AreEqual("byte[][,]", TypeToString(typeof(byte[][,]).ToTypeReference()));
		}
		
		[Test]
		public void Pointer()
		{
			Assert.AreEqual("long*", TypeToString(typeof(long*).ToTypeReference()));
		}
		
		[Test]
		public void NullableType()
		{
			Assert.AreEqual("ulong?", TypeToString(typeof(ulong?).ToTypeReference()));
		}
		
		[Test]
		public void AmbiguousType()
		{
			Assert.AreEqual("System.Array", TypeToString(typeof(Array).ToTypeReference()));
			Assert.AreEqual("OtherNS.Array", TypeToString(ctx.GetTypeDefinition("OtherNS", "Array", 0, StringComparer.Ordinal)));
		}
	}
}
