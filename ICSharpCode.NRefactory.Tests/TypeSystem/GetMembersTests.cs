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
		ICompilation compilation = new SimpleCompilation(CecilLoaderTests.Mscorlib);
		
		[Test]
		public void EmptyClassHasToString()
		{
			DefaultUnresolvedTypeDefinition c = new DefaultUnresolvedTypeDefinition(string.Empty, "C");
			Assert.AreEqual("System.Object.ToString", compilation.MainAssembly.GetTypeDefinition(c).GetMethods(m => m.Name == "ToString").Single().FullName);
		}
		
		[Test]
		public void MultipleInheritanceTest()
		{
			DefaultUnresolvedTypeDefinition b1 = new DefaultUnresolvedTypeDefinition(string.Empty, "B1");
			b1.Kind = TypeKind.Interface;
			b1.Members.Add(new DefaultUnresolvedProperty(b1, "P1"));
			
			DefaultUnresolvedTypeDefinition b2 = new DefaultUnresolvedTypeDefinition(string.Empty, "B2");
			b2.Kind = TypeKind.Interface;
			b2.Members.Add(new DefaultUnresolvedProperty(b2, "P2"));
			
			DefaultUnresolvedTypeDefinition c = new DefaultUnresolvedTypeDefinition(string.Empty, "C");
			c.Kind = TypeKind.Interface;
			c.BaseTypes.Add(b1);
			c.BaseTypes.Add(b2);
			
			ITypeDefinition resolvedC = compilation.MainAssembly.GetTypeDefinition(c);
			Assert.AreEqual(new[] { "P1", "P2" }, resolvedC.GetProperties().Select(p => p.Name).ToArray());
			// Test that there's only one copy of ToString():
			Assert.AreEqual(1, resolvedC.GetMethods(m => m.Name == "ToString").Count());
		}
		
		[Test]
		public void GetNestedTypesOfUnboundGenericClass()
		{
			ITypeDefinition dictionary = compilation.FindType(typeof(Dictionary<,>)).GetDefinition();
			IType keyCollection = dictionary.GetNestedTypes().Single(t => t.Name == "KeyCollection");
			// the type should be parameterized
			Assert.AreEqual("System.Collections.Generic.Dictionary`2+KeyCollection[[`0],[`1]]", keyCollection.ReflectionName);
		}
		
		[Test]
		public void GetNestedTypesOfBoundGenericClass()
		{
			IType dictionary = compilation.FindType(typeof(Dictionary<string, int>));
			IType keyCollection = dictionary.GetNestedTypes().Single(t => t.Name == "KeyCollection");
			Assert.AreEqual(compilation.FindType(typeof(Dictionary<string, int>.KeyCollection)), keyCollection);
		}
		
		[Test]
		public void GetGenericNestedTypeOfBoundGenericClass()
		{
			// class A<X> { class B<Y> { } }
			DefaultUnresolvedTypeDefinition a = new DefaultUnresolvedTypeDefinition(string.Empty, "A");
			a.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 0, "X"));
			
			DefaultUnresolvedTypeDefinition b = new DefaultUnresolvedTypeDefinition(a, "B");
			b.TypeParameters.Add(a.TypeParameters[0]);
			b.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 1, "Y"));
			
			a.NestedTypes.Add(b);
			
			ITypeDefinition resolvedA = compilation.MainAssembly.GetTypeDefinition(a);
			ITypeDefinition resolvedB = compilation.MainAssembly.GetTypeDefinition(b);
			
			// A<> gets self-parameterized, B<> stays unbound
			Assert.AreEqual("A`1+B`1[[`0],[]]", resolvedA.GetNestedTypes().Single().ReflectionName);
			
			ParameterizedType pt = new ParameterizedType(resolvedA, new [] { compilation.FindType(KnownTypeCode.String) });
			Assert.AreEqual("A`1+B`1[[System.String],[]]", pt.GetNestedTypes().Single().ReflectionName);
		}
	}
}
