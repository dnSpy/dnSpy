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
using ICSharpCode.NRefactory.CSharp.TypeSystem;
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
		
		IProjectContent pc;
		ICompilation compilation;
		ITypeDefinition baseClass, derivedClass, nestedClass, systemClass;
		CSharpUnresolvedFile unresolvedFile;
		
		[SetUp]
		public void SetUp()
		{
			pc = new CSharpProjectContent();
			pc = pc.SetAssemblyName("MyAssembly");
			unresolvedFile = SyntaxTree.Parse(program, "program.cs").ToTypeSystem();
			pc = pc.AddOrUpdateFiles(unresolvedFile);
			pc = pc.AddAssemblyReferences(new [] { CecilLoaderTests.Mscorlib });
			
			compilation = pc.CreateCompilation();
			
			baseClass = compilation.RootNamespace.GetTypeDefinition("Base", 1);
			nestedClass = baseClass.NestedTypes.Single();
			derivedClass = compilation.RootNamespace.GetTypeDefinition("Derived", 2);
			systemClass = compilation.RootNamespace.GetChildNamespace("NS").GetTypeDefinition("System", 0);
		}
		
		TypeSystemAstBuilder CreateBuilder(ITypeDefinition currentTypeDef = null)
		{
			UsingScope usingScope = currentTypeDef != null ? unresolvedFile.GetUsingScope(currentTypeDef.Region.Begin) : unresolvedFile.RootUsingScope;
			return new TypeSystemAstBuilder(new CSharpResolver(
				new CSharpTypeResolveContext(compilation.MainAssembly, usingScope.Resolve(compilation), currentTypeDef)));
		}
		
		string TypeToString(IType type, ITypeDefinition currentTypeDef = null)
		{
			var builder = CreateBuilder(currentTypeDef);
			AstType node = builder.ConvertType(type);
			return node.ToString();
		}
		
		[Test]
		public void PrimitiveVoid()
		{
			Assert.AreEqual("void", TypeToString(compilation.FindType(KnownTypeCode.Void)));
		}
		
		[Test]
		public void PrimitiveInt()
		{
			Assert.AreEqual("int", TypeToString(compilation.FindType(KnownTypeCode.Int32)));
		}
		
		[Test]
		public void PrimitiveDecimal()
		{
			Assert.AreEqual("decimal", TypeToString(compilation.FindType(KnownTypeCode.Decimal)));
		}
		
		[Test]
		public void SystemType()
		{
			Assert.AreEqual("Type", TypeToString(compilation.FindType(KnownTypeCode.Type)));
		}
		
		[Test]
		public void ListOfNSSystem()
		{
			var type = new ParameterizedType(compilation.FindType(typeof(List<>)).GetDefinition(), new[] { systemClass });
			Assert.AreEqual("List<NS.System>", TypeToString(type));
			Assert.AreEqual("List<System>", TypeToString(type, systemClass));
		}
		
		[Test]
		public void NonGenericIEnumerable()
		{
			Assert.AreEqual("System.Collections.IEnumerable", TypeToString(compilation.FindType(typeof(IEnumerable))));
		}
		
		[Test]
		public void NonGenericIEnumerableWithSystemNamespaceCollision()
		{
			Assert.AreEqual("global::System.Collections.IEnumerable", TypeToString(compilation.FindType(typeof(IEnumerable)), systemClass));
		}
		
		[Test]
		public void AliasedNamespace()
		{
			var type = compilation.FindType(typeof(System.Reflection.Assembly));
			Assert.AreEqual("R.Assembly", TypeToString(type, systemClass));
		}
		
		[Test]
		public void AliasedType()
		{
			var type = new ParameterizedType(compilation.FindType(typeof(List<>)).GetDefinition(), new[] { compilation.FindType(KnownTypeCode.Char) });
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
			var type = new ParameterizedType(nestedClass, new[] { compilation.FindType(KnownTypeCode.Char), compilation.FindType(KnownTypeCode.String) });
			Assert.AreEqual("Base<char>.Nested<string>", TypeToString(type));
			// The short form "Nested<string>" refers to "Base<T>.Nested<string>",
			// so we need to use the long form to specify that T=char.
			Assert.AreEqual("Base<char>.Nested<string>", TypeToString(type, baseClass));
			Assert.AreEqual("Base<char>.Nested<string>", TypeToString(type, nestedClass));
			Assert.AreEqual("Base<char>.Nested<string>", TypeToString(type, derivedClass));
		}
		
		[Test]
		public void NestedTypeInCurrentClass()
		{
			var type = new ParameterizedType(nestedClass, new[] { baseClass.TypeParameters[0], compilation.FindType(KnownTypeCode.String) });
			Assert.AreEqual("Nested<string>", TypeToString(type, baseClass));
			Assert.AreEqual("Nested<string>", TypeToString(type, nestedClass));
		}
		
		[Test]
		public void NestedTypeInDerivedClass()
		{
			var type1 = new ParameterizedType(nestedClass, new[] { derivedClass.TypeParameters[0], compilation.FindType(KnownTypeCode.String) });
			// short form "Nested<string>" cannot be used as it would refer to "Base<S>.Nested<string>"
			Assert.AreEqual("Base<T>.Nested<string>", TypeToString(type1, derivedClass));
			
			var type2 = new ParameterizedType(nestedClass, new[] { derivedClass.TypeParameters[1], compilation.FindType(KnownTypeCode.String) });
			Assert.AreEqual("Nested<string>", TypeToString(type2, derivedClass));
		}
		
		[Test]
		public void MultidimensionalArray()
		{
			Assert.AreEqual("byte[][,]", TypeToString(compilation.FindType(typeof(byte[][,]))));
		}
		
		[Test]
		public void Pointer()
		{
			Assert.AreEqual("long*", TypeToString(compilation.FindType(typeof(long*))));
		}
		
		[Test]
		public void NullableType()
		{
			Assert.AreEqual("ulong?", TypeToString(compilation.FindType(typeof(ulong?))));
		}
		
		[Test]
		public void AmbiguousType()
		{
			Assert.AreEqual("System.Array", TypeToString(compilation.FindType(typeof(Array))));
			Assert.AreEqual("OtherNS.Array", TypeToString(compilation.MainAssembly.GetTypeDefinition("OtherNS", "Array", 0)));
		}
	}
}
