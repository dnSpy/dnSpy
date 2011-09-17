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
		IProjectContent mscorlib = CecilLoaderTests.Mscorlib;
		ITypeResolveContext context = CecilLoaderTests.Mscorlib;
		
		IType[] GetAllBaseTypes(Type type)
		{
			return type.ToTypeReference().Resolve(context).GetAllBaseTypes(context).OrderBy(t => t.ReflectionName).ToArray();
		}
		
		IType[] GetTypes(params Type[] types)
		{
			return types.Select(t => t.ToTypeReference().Resolve(context)).OrderBy(t => t.ReflectionName).ToArray();;
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
			DefaultTypeDefinition c = new DefaultTypeDefinition(mscorlib, string.Empty, "C");
			c.BaseTypes.Add(c);
			Assert.AreEqual(new [] { c }, c.GetAllBaseTypes(context).ToArray());
		}
		
		[Test]
		public void TwoClassesDerivingFromEachOther()
		{
			// class C1 : C2 {} class C2 : C1 {}
			DefaultTypeDefinition c1 = new DefaultTypeDefinition(mscorlib, string.Empty, "C1");
			DefaultTypeDefinition c2 = new DefaultTypeDefinition(mscorlib, string.Empty, "C2");
			c1.BaseTypes.Add(c2);
			c2.BaseTypes.Add(c1);
			Assert.AreEqual(new [] { c2, c1 }, c1.GetAllBaseTypes(context).ToArray());
		}
		
		[Test]
		public void ClassDerivingFromParameterizedVersionOfItself()
		{
			// class C<X> : C<C<X>> {}
			DefaultTypeDefinition c = new DefaultTypeDefinition(mscorlib, string.Empty, "C");
			c.TypeParameters.Add(new DefaultTypeParameter(EntityType.TypeDefinition, 0, "X"));
			c.BaseTypes.Add(new ParameterizedType(c, new [] { new ParameterizedType(c, new [] { c.TypeParameters[0] }) }));
			Assert.AreEqual(new [] { c }, c.GetAllBaseTypes(context).ToArray());
		}
		
		[Test]
		public void ClassDerivingFromTwoInstanciationsOfIEnumerable()
		{
			// class C : IEnumerable<int>, IEnumerable<uint> {}
			DefaultTypeDefinition c = new DefaultTypeDefinition(mscorlib, string.Empty, "C");
			c.BaseTypes.Add(typeof(IEnumerable<int>).ToTypeReference());
			c.BaseTypes.Add(typeof(IEnumerable<uint>).ToTypeReference());
			IType[] expected = {
				c,
				c.BaseTypes[0].Resolve(context),
				c.BaseTypes[1].Resolve(context),
				mscorlib.GetTypeDefinition(typeof(IEnumerable)),
				mscorlib.GetTypeDefinition(typeof(object))
			};
			Assert.AreEqual(expected,
			                c.GetAllBaseTypes(context).OrderBy(t => t.ReflectionName).ToArray());
		}
		
		[Test]
		public void StructImplementingIEquatable()
		{
			// struct S : IEquatable<S> {}
			// don't use a Cecil-loaded struct for this test; we're testing the implicit addition of System.ValueType
			DefaultTypeDefinition s = new DefaultTypeDefinition(mscorlib, string.Empty, "S");
			s.Kind = TypeKind.Struct;
			s.BaseTypes.Add(new ParameterizedType(mscorlib.GetTypeDefinition(typeof(IEquatable<>)), new[] { s }));
			IType[] expected = {
				s,
				s.BaseTypes[0].Resolve(context),
				mscorlib.GetTypeDefinition(typeof(object)),
				mscorlib.GetTypeDefinition(typeof(ValueType))
			};
			Assert.AreEqual(expected,
			                s.GetAllBaseTypes(context).OrderBy(t => t.ReflectionName).ToArray());
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
				typeof(List<string>).ToTypeReference().Resolve(context).GetAllBaseTypeDefinitions(context).OrderBy(t => t.ReflectionName).ToArray());
		}
		
		[Test]
		public void BaseTypeDefinitionsOfStringArray()
		{
			Assert.AreEqual(
				GetTypes(typeof(Array), typeof(object),
				         typeof(ICloneable), typeof(IStructuralComparable), typeof(IStructuralEquatable),
				         typeof(IList), typeof(ICollection), typeof(IEnumerable),
				         typeof(IEnumerable<>), typeof(ICollection<>), typeof(IList<>)),
				typeof(string[]).ToTypeReference().Resolve(context).GetAllBaseTypeDefinitions(context).OrderBy(t => t.ReflectionName).ToArray());
		}
	}
}
