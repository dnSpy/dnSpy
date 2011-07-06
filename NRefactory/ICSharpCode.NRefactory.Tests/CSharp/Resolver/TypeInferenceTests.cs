// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class TypeInferenceTests
	{
		readonly ITypeResolveContext ctx = CecilLoaderTests.Mscorlib;
		TypeInference ti;
		
		[SetUp]
		public void Setup()
		{
			ti = new TypeInference(ctx);
		}
		
		#region Type Inference
		IType[] Resolve(params Type[] types)
		{
			IType[] r = new IType[types.Length];
			for (int i = 0; i < types.Length; i++) {
				r[i] = types[i].ToTypeReference().Resolve(CecilLoaderTests.Mscorlib);
				Assert.AreNotSame(r[i], SharedTypes.UnknownType);
			}
			Array.Sort(r, (a,b)=>a.ReflectionName.CompareTo(b.ReflectionName));
			return r;
		}
		
		[Test]
		public void ArrayToEnumerable()
		{
			ITypeParameter tp = new DefaultTypeParameter(EntityType.Method, 0, "T");
			IType stringType = KnownTypeReference.String.Resolve(ctx);
			ITypeDefinition enumerableType = ctx.GetTypeDefinition(typeof(IEnumerable<>));
			
			bool success;
			Assert.AreEqual(
				new [] { stringType },
				ti.InferTypeArguments(new [] { tp },
				                      new [] { new ResolveResult(new ArrayType(stringType)) },
				                      new [] { new ParameterizedType(enumerableType, new [] { tp }) },
				                      out success));
			Assert.IsTrue(success);
		}
		
		[Test]
		public void EnumerableToArrayInContravariantType()
		{
			ITypeParameter tp = new DefaultTypeParameter(EntityType.Method, 0, "T");
			IType stringType = KnownTypeReference.String.Resolve(ctx);
			ITypeDefinition enumerableType = ctx.GetTypeDefinition(typeof(IEnumerable<>));
			ITypeDefinition comparerType = ctx.GetTypeDefinition(typeof(IComparer<>));
			
			var comparerOfIEnumerableOfString = new ParameterizedType(comparerType, new [] { new ParameterizedType(enumerableType, new [] { stringType} ) });
			var comparerOfTpArray = new ParameterizedType(comparerType, new [] { new ArrayType(tp) });
			
			bool success;
			Assert.AreEqual(
				new [] { stringType },
				ti.InferTypeArguments(new [] { tp },
				                      new [] { new ResolveResult(comparerOfIEnumerableOfString) },
				                      new [] { comparerOfTpArray },
				                      out success));
			Assert.IsTrue(success);
		}
		#endregion
		
		#region FindTypeInBounds
		IType[] FindAllTypesInBounds(IList<IType> lowerBounds, IList<IType> upperBounds = null)
		{
			ti.Algorithm = TypeInferenceAlgorithm.ImprovedReturnAllResults;
			IType type = ti.FindTypeInBounds(lowerBounds, upperBounds ?? new IType[0]);
			return ExpandIntersections(type).OrderBy(t => t.ReflectionName).ToArray();
		}
		
		static IEnumerable<IType> ExpandIntersections(IType type)
		{
			IntersectionType it = type as IntersectionType;
			if (it != null) {
				return it.Types.SelectMany(t => ExpandIntersections(t));
			}
			ParameterizedType pt = type as ParameterizedType;
			if (pt != null) {
				IType[][] typeArguments = new IType[pt.TypeArguments.Count][];
				for (int i = 0; i < typeArguments.Length; i++) {
					typeArguments[i] = ExpandIntersections(pt.TypeArguments[i]).ToArray();
				}
				return AllCombinations(typeArguments).Select(ta => new ParameterizedType(pt.GetDefinition(), ta));
			}
			return new [] { type };
		}
		
		/// <summary>
		/// Performs the combinatorial explosion.
		/// </summary>
		static IEnumerable<IType[]> AllCombinations(IType[][] typeArguments)
		{
			int[] index = new int[typeArguments.Length];
			index[typeArguments.Length - 1] = -1;
			while (true) {
				int i;
				for (i = index.Length - 1; i >= 0; i--) {
					if (++index[i] == typeArguments[i].Length)
						index[i] = 0;
					else
						break;
				}
				if (i < 0)
					break;
				IType[] r = new IType[typeArguments.Length];
				for (i = 0; i < r.Length; i++) {
					r[i] = typeArguments[i][index[i]];
				}
				yield return r;
			}
		}
		
		[Test]
		public void ListOfShortAndInt()
		{
			Assert.AreEqual(
				Resolve(typeof(IList)),
				FindAllTypesInBounds(Resolve(typeof(List<short>), typeof(List<int>))));
		}
		
		[Test]
		public void ListOfStringAndObject()
		{
			Assert.AreEqual(
				Resolve(typeof(IList), typeof(IEnumerable<object>)),
				FindAllTypesInBounds(Resolve(typeof(List<string>), typeof(List<object>))));
		}
		
		[Test]
		public void ListOfListOfStringAndObject()
		{
			Assert.AreEqual(
				Resolve(typeof(IList), typeof(IEnumerable<IList>), typeof(IEnumerable<IEnumerable<object>>)),
				FindAllTypesInBounds(Resolve(typeof(List<List<string>>), typeof(List<List<object>>))));
		}
		
		[Test]
		public void ShortAndInt()
		{
			Assert.AreEqual(
				Resolve(typeof(int)),
				FindAllTypesInBounds(Resolve(typeof(short), typeof(int))));
		}
		
		[Test]
		public void StringAndVersion()
		{
			Assert.AreEqual(
				Resolve(typeof(ICloneable), typeof(IComparable)),
				FindAllTypesInBounds(Resolve(typeof(string), typeof(Version))));
		}
		
		[Test]
		public void CommonSubTypeClonableComparable()
		{
			Assert.AreEqual(
				Resolve(typeof(string), typeof(Version)),
				FindAllTypesInBounds(Resolve(), Resolve(typeof(ICloneable), typeof(IComparable))));
		}
		
		[Test]
		public void EnumerableOfStringAndVersion()
		{
			Assert.AreEqual(
				Resolve(typeof(IEnumerable<ICloneable>), typeof(IEnumerable<IComparable>)),
				FindAllTypesInBounds(Resolve(typeof(IList<string>), typeof(IList<Version>))));
		}
		
		[Test]
		public void CommonSubTypeIEnumerableClonableIEnumerableComparable()
		{
			Assert.AreEqual(
				Resolve(typeof(IEnumerable<string>), typeof(IEnumerable<Version>)),
				FindAllTypesInBounds(Resolve(), Resolve(typeof(IEnumerable<ICloneable>), typeof(IEnumerable<IComparable>))));
		}
		
		[Test]
		public void CommonSubTypeIEnumerableClonableIEnumerableComparableList()
		{
			Assert.AreEqual(
				Resolve(typeof(List<string>), typeof(List<Version>), typeof(Collection<string>), typeof(Collection<Version>), typeof(ReadOnlyCollection<string>), typeof(ReadOnlyCollection<Version>)),
				FindAllTypesInBounds(Resolve(), Resolve(typeof(IEnumerable<ICloneable>), typeof(IEnumerable<IComparable>), typeof(IList))));
		}
		#endregion
	}
}
