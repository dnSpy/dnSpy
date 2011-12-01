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
using System.Linq;
using System.Runtime.Serialization;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	using Unbound = ReflectionHelper.UnboundTypeArgument;
	
	[TestFixture]
	public class GetAllBaseTypesTest
	{
		ICompilation compilation = new SimpleCompilation(CecilLoaderTests.Mscorlib);
		
		IType[] GetAllBaseTypes(Type type)
		{
			return compilation.FindType(type).GetAllBaseTypes().OrderBy(t => t.ReflectionName).ToArray();
		}
		
		IType[] GetTypes(params Type[] types)
		{
			return types.Select(t => compilation.FindType(t)).OrderBy(t => t.ReflectionName).ToArray();;
		}
		
		ITypeDefinition Resolve(IUnresolvedTypeDefinition typeDef)
		{
			return compilation.MainAssembly.GetTypeDefinition(typeDef);
		}
		
		[Test]
		public void ObjectBaseTypes()
		{
			Assert.AreEqual(GetTypes(typeof(object)), GetAllBaseTypes(typeof(object)));
		}
		
		[Test]
		public void StringBaseTypes()
		{
			Assert.AreEqual(GetTypes(typeof(string), typeof(object), typeof(IComparable), typeof(ICloneable), typeof(IConvertible),
			                         typeof(IComparable<string>), typeof(IEquatable<string>), typeof(IEnumerable<char>), typeof(IEnumerable)),
			                GetAllBaseTypes(typeof(string)));
		}
		
		[Test]
		public void ArrayOfString()
		{
			Assert.AreEqual(GetTypes(typeof(string[]), typeof(Array), typeof(object),
			                         typeof(IList), typeof(ICollection), typeof(IEnumerable),
			                         typeof(IList<string>), typeof(ICollection<string>), typeof(IEnumerable<string>),
			                         typeof(IStructuralEquatable), typeof(IStructuralComparable), typeof(ICloneable)),
			                GetAllBaseTypes(typeof(string[])));
		}
		
		[Test]
		public void MultidimensionalArrayOfString()
		{
			Assert.AreEqual(GetTypes(typeof(string[,]), typeof(Array), typeof(object),
			                         typeof(IList), typeof(ICollection), typeof(IEnumerable),
			                         typeof(IStructuralEquatable), typeof(IStructuralComparable), typeof(ICloneable)),
			                GetAllBaseTypes(typeof(string[,])));
		}
		
		[Test]
		public void ClassDerivingFromItself()
		{
			// class C : C {}
			var c = new DefaultUnresolvedTypeDefinition(string.Empty, "C");
			c.BaseTypes.Add(c);
			ITypeDefinition resolvedC = Resolve(c);
			Assert.AreEqual(new [] { resolvedC }, resolvedC.GetAllBaseTypes().ToArray());
		}
		
		[Test]
		public void TwoClassesDerivingFromEachOther()
		{
			// class C1 : C2 {} class C2 : C1 {}
			var c1 = new DefaultUnresolvedTypeDefinition(string.Empty, "C1");
			var c2 = new DefaultUnresolvedTypeDefinition(string.Empty, "C2");
			c1.BaseTypes.Add(c2);
			c2.BaseTypes.Add(c1);
			ITypeDefinition resolvedC1 = Resolve(c1);
			ITypeDefinition resolvedC2 = Resolve(c2);
			Assert.AreEqual(new [] { resolvedC2, resolvedC1 }, resolvedC1.GetAllBaseTypes().ToArray());
		}
		
		[Test]
		public void ClassDerivingFromParameterizedVersionOfItself()
		{
			// class C<X> : C<C<X>> {}
			var c = new DefaultUnresolvedTypeDefinition(string.Empty, "C");
			c.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 0, "X"));
			c.BaseTypes.Add(new ParameterizedTypeReference(c, new [] { new ParameterizedTypeReference(c, new [] { new TypeParameterReference(EntityType.TypeDefinition, 0) }) }));
			ITypeDefinition resolvedC = Resolve(c);
			Assert.AreEqual(new [] { resolvedC }, resolvedC.GetAllBaseTypes().ToArray());
		}
		
		[Test]
		public void ClassDerivingFromTwoInstanciationsOfIEnumerable()
		{
			// class C : IEnumerable<int>, IEnumerable<uint> {}
			var c = new DefaultUnresolvedTypeDefinition(string.Empty, "C");
			c.BaseTypes.Add(typeof(IEnumerable<int>).ToTypeReference());
			c.BaseTypes.Add(typeof(IEnumerable<uint>).ToTypeReference());
			ITypeDefinition resolvedC = Resolve(c);
			IType[] expected = {
				resolvedC,
				compilation.FindType(typeof(IEnumerable<int>)),
				compilation.FindType(typeof(IEnumerable<uint>)),
				compilation.FindType(typeof(IEnumerable)),
				compilation.FindType(typeof(object))
			};
			Assert.AreEqual(expected,
			                resolvedC.GetAllBaseTypes().OrderBy(t => t.ReflectionName).ToArray());
		}
		
		[Test]
		public void StructImplementingIEquatable()
		{
			// struct S : IEquatable<S> {}
			// don't use a Cecil-loaded struct for this test; we're testing the implicit addition of System.ValueType
			var s = new DefaultUnresolvedTypeDefinition(string.Empty, "S");
			s.Kind = TypeKind.Struct;
			s.BaseTypes.Add(new ParameterizedTypeReference(typeof(IEquatable<>).ToTypeReference(), new[] { s }));
			ITypeDefinition resolvedS = Resolve(s);
			IType[] expected = {
				resolvedS,
				s.BaseTypes[0].Resolve(new SimpleTypeResolveContext(resolvedS)),
				compilation.FindType(typeof(object)),
				compilation.FindType(typeof(ValueType))
			};
			Assert.AreEqual(expected,
			                resolvedS.GetAllBaseTypes().OrderBy(t => t.ReflectionName).ToArray());
		}
		
		[Test]
		public void BaseTypesOfListOfString()
		{
			Assert.AreEqual(
				GetTypes(typeof(List<string>), typeof(object),
				         typeof(IList), typeof(ICollection), typeof(IEnumerable),
				         typeof(IEnumerable<string>), typeof(ICollection<string>), typeof(IList<string>)),
				GetAllBaseTypes(typeof(List<string>)));
		}
		
		[Test]
		public void BaseTypesOfUnboundDictionary()
		{
			Assert.AreEqual(
				new [] {
					typeof(Dictionary<,>).FullName,
					typeof(ICollection<>).FullName + "[[" + typeof(KeyValuePair<,>).FullName + "[[`0],[`1]]]]",
					typeof(IDictionary<,>).FullName + "[[`0],[`1]]",
					typeof(IEnumerable<>).FullName + "[[" + typeof(KeyValuePair<,>).FullName + "[[`0],[`1]]]]",
					typeof(ICollection).FullName,
					typeof(IDictionary).FullName,
					typeof(IEnumerable).FullName,
					typeof(object).FullName,
					typeof(IDeserializationCallback).FullName,
					typeof(ISerializable).FullName,
				},
				GetAllBaseTypes(typeof(Dictionary<,>)).Select(t => t.ReflectionName).ToArray());
		}
		
		[Test]
		public void BaseTypeDefinitionsOfListOfString()
		{
			Assert.AreEqual(
				GetTypes(typeof(List<>), typeof(object),
				         typeof(IList), typeof(ICollection), typeof(IEnumerable),
				         typeof(IEnumerable<>), typeof(ICollection<>), typeof(IList<>)),
				compilation.FindType(typeof(List<string>)).GetAllBaseTypeDefinitions().OrderBy(t => t.ReflectionName).ToArray());
		}
		
		[Test]
		public void BaseTypeDefinitionsOfStringArray()
		{
			Assert.AreEqual(
				GetTypes(typeof(Array), typeof(object),
				         typeof(ICloneable), typeof(IStructuralComparable), typeof(IStructuralEquatable),
				         typeof(IList), typeof(ICollection), typeof(IEnumerable),
				         typeof(IEnumerable<>), typeof(ICollection<>), typeof(IList<>)),
				compilation.FindType(typeof(string[])).GetAllBaseTypeDefinitions().OrderBy(t => t.ReflectionName).ToArray());
		}
	}
}
