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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	[TestFixture]
	public class GetMembersTests
	{
		IProjectContent mscorlib = CecilLoaderTests.Mscorlib;
		
		[Test]
		public void EmptyClassHasToString()
		{
			DefaultTypeDefinition c = new DefaultTypeDefinition(mscorlib, string.Empty, "C");
			Assert.AreEqual("System.Object.ToString", c.GetMethods(mscorlib, m => m.Name == "ToString").Single().FullName);
		}
		
		[Test]
		public void MultipleInheritanceTest()
		{
			DefaultTypeDefinition b1 = new DefaultTypeDefinition(mscorlib, string.Empty, "B1");
			b1.Kind = TypeKind.Interface;
			b1.Properties.Add(new DefaultProperty(b1, "P1"));
			
			DefaultTypeDefinition b2 = new DefaultTypeDefinition(mscorlib, string.Empty, "B1");
			b2.Kind = TypeKind.Interface;
			b2.Properties.Add(new DefaultProperty(b1, "P2"));
			
			DefaultTypeDefinition c = new DefaultTypeDefinition(mscorlib, string.Empty, "C");
			c.Kind = TypeKind.Interface;
			c.BaseTypes.Add(b1);
			c.BaseTypes.Add(b2);
			
			Assert.AreEqual(new[] { "P1", "P2" }, c.GetProperties(mscorlib).Select(p => p.Name).ToArray());
			// Test that there's only one copy of ToString():
			Assert.AreEqual(1, c.GetMethods(mscorlib, m => m.Name == "ToString").Count());
		}
		
		[Test]
		public void ArrayType()
		{
			IType arrayType = typeof(string[]).ToTypeReference().Resolve(mscorlib);
			// Array inherits ToString() from System.Object
			Assert.AreEqual("System.Object.ToString", arrayType.GetMethods(mscorlib, m => m.Name == "ToString").Single().FullName);
			Assert.AreEqual("System.Array.GetLowerBound", arrayType.GetMethods(mscorlib, m => m.Name == "GetLowerBound").Single().FullName);
			Assert.AreEqual("System.Array.Length", arrayType.GetProperties(mscorlib, p => p.Name == "Length").Single().FullName);
			
			// test indexer
			IProperty indexer = arrayType.GetProperties(mscorlib, p => p.IsIndexer).Single();
			Assert.AreEqual("System.Array.Items", indexer.FullName);
			Assert.AreEqual("System.String", indexer.ReturnType.Resolve(mscorlib).ReflectionName);
			Assert.AreEqual(1, indexer.Parameters.Count);
			Assert.AreEqual("System.Int32", indexer.Parameters[0].Type.Resolve(mscorlib).ReflectionName);
		}
		
		[Test]
		public void MultidimensionalArrayType()
		{
			IType arrayType = typeof(string[,][]).ToTypeReference().Resolve(mscorlib);
			
			// test indexer
			IProperty indexer = arrayType.GetProperties(mscorlib, p => p.IsIndexer).Single();
			Assert.AreEqual("System.Array.Items", indexer.FullName);
			Assert.AreEqual("System.String[]", indexer.ReturnType.Resolve(mscorlib).ReflectionName);
			Assert.AreEqual(2, indexer.Parameters.Count);
			Assert.AreEqual("System.Int32", indexer.Parameters[0].Type.Resolve(mscorlib).ReflectionName);
			Assert.AreEqual("System.Int32", indexer.Parameters[1].Type.Resolve(mscorlib).ReflectionName);
		}
		
		[Test]
		public void GetNestedTypesOfUnboundGenericClass()
		{
			ITypeDefinition dictionary = mscorlib.GetTypeDefinition(typeof(Dictionary<,>));
			IType keyCollection = dictionary.GetNestedTypes(mscorlib).Single(t => t.Name == "KeyCollection");
			Assert.IsTrue(keyCollection is ITypeDefinition);
		}
		
		[Test]
		public void GetNestedTypesOfBoundGenericClass()
		{
			IType dictionary = typeof(Dictionary<string, int>).ToTypeReference().Resolve(mscorlib);
			IType keyCollection = dictionary.GetNestedTypes(mscorlib).Single(t => t.Name == "KeyCollection");
			Assert.AreEqual(typeof(Dictionary<string, int>.KeyCollection).ToTypeReference().Resolve(mscorlib), keyCollection);
		}
		
		[Test]
		public void GetGenericNestedTypeOfBoundGenericClass()
		{
			// class A<X> { class B<Y> { } }
			DefaultTypeDefinition a = new DefaultTypeDefinition(mscorlib, string.Empty, "A");
			a.TypeParameters.Add(new DefaultTypeParameter(EntityType.TypeDefinition, 0, "X"));
			
			DefaultTypeDefinition b = new DefaultTypeDefinition(a, "B");
			b.TypeParameters.Add(a.TypeParameters[0]);
			b.TypeParameters.Add(new DefaultTypeParameter(EntityType.TypeDefinition, 1, "Y"));
			
			a.NestedTypes.Add(b);
			
			// A<> gets self-parameterized, B<> stays unbound
			Assert.AreEqual("A`1+B`1[[`0],[]]", a.GetNestedTypes(mscorlib).Single().ReflectionName);
			
			ParameterizedType pt = new ParameterizedType(a, new [] { KnownTypeReference.String.Resolve(mscorlib) });
			Assert.AreEqual("A`1+B`1[[System.String],[]]", pt.GetNestedTypes(mscorlib).Single().ReflectionName);
		}
	}
}
