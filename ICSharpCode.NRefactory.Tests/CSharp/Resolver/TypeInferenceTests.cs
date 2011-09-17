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
using System.Collections.ObjectModel;
using System.Linq;

using ICSharpCode.NRefactory.Semantics;
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
		
		#region Inference with Method Groups
		[Test]
		public void CannotInferFromMethodParameterTypes()
		{
			// static void M<A, B>(Func<A, B> f) {}
			// M(int.Parse); // type inference fails
			DefaultTypeParameter A = new DefaultTypeParameter(EntityType.Method, 0, "A");
			DefaultTypeParameter B = new DefaultTypeParameter(EntityType.Method, 1, "B");
			
			ITypeDefinition declType = ctx.GetTypeDefinition(typeof(int));
			var methods = new MethodListWithDeclaringType(declType, declType.Methods.Where(m => m.Name == "Parse"));
			var argument = new MethodGroupResolveResult(new TypeResolveResult(declType), "Parse", new[] { methods }, new IType[0]);
			
			bool success;
			ti.InferTypeArguments(new [] { A, B }, new [] { argument },
			                      new [] { new ParameterizedType(ctx.GetTypeDefinition(typeof(Func<,>)), new[] { A, B }) },
			                      out success);
			Assert.IsFalse(success);
		}
		
		[Test]
		public void InferFromMethodReturnType()
		{
			// static void M<T>(Func<T> f) {}
			// M(Console.ReadKey); // type inference produces ConsoleKeyInfo
			
			DefaultTypeParameter T = new DefaultTypeParameter(EntityType.Method, 0, "T");
			
			ITypeDefinition declType = ctx.GetTypeDefinition(typeof(Console));
			var methods = new MethodListWithDeclaringType(declType, declType.Methods.Where(m => m.Name == "ReadKey"));
			var argument = new MethodGroupResolveResult(new TypeResolveResult(declType), "ReadKey", new[] { methods }, new IType[0]);
			
			bool success;
			Assert.AreEqual(
				new [] { ctx.GetTypeDefinition(typeof(ConsoleKeyInfo)) },
				ti.InferTypeArguments(new [] { T }, new [] { argument },
				                      new [] { new ParameterizedType(ctx.GetTypeDefinition(typeof(Func<>)), new[] { T }) },
				                      out success));
			Assert.IsTrue(success);
		}
		#endregion
		
		#region Inference with Lambda
		#region MockImplicitLambda
		sealed class MockImplicitLambda : LambdaResolveResult
		{
			IType[] expectedParameterTypes;
			IType inferredReturnType;
			IParameter[] parameters;
			
			public MockImplicitLambda(IType[] expectedParameterTypes, IType inferredReturnType)
			{
				this.expectedParameterTypes = expectedParameterTypes;
				this.inferredReturnType = inferredReturnType;
				this.parameters = new IParameter[expectedParameterTypes.Length];
				for (int i = 0; i < parameters.Length; i++) {
					// UnknownType because this lambda is implicitly typed
					parameters[i] = new DefaultParameter(SharedTypes.UnknownType, "X" + i);
				}
			}
			
			public override IList<IParameter> Parameters {
				get { return parameters; }
			}
			
			public override Conversion IsValid(IType[] parameterTypes, IType returnType, Conversions conversions)
			{
				Assert.AreEqual(expectedParameterTypes, parameterTypes);
				return conversions.ImplicitConversion(inferredReturnType, returnType);
			}
			
			public override bool IsImplicitlyTyped {
				get { return true; }
			}
			
			public override bool IsAnonymousMethod {
				get { return false; }
			}
			
			public override bool HasParameterList {
				get { return true; }
			}
			
			public override bool IsAsync {
				get { return false; }
			}
			
			public override IType GetInferredReturnType(IType[] parameterTypes)
			{
				Assert.AreEqual(expectedParameterTypes, parameterTypes, "Parameters types passed to " + this);
				return inferredReturnType;
			}
			
			public override string ToString()
			{
				return "[MockImplicitLambda (" + string.Join<IType>(", ", expectedParameterTypes) + ") => " + inferredReturnType + "]";
			}
		}
		#endregion
		
		[Test]
		public void TestLambdaInference()
		{
			ITypeParameter[] typeParameters = {
				new DefaultTypeParameter(EntityType.Method, 0, "X"),
				new DefaultTypeParameter(EntityType.Method, 1, "Y"),
				new DefaultTypeParameter(EntityType.Method, 2, "Z")
			};
			IType[] parameterTypes = {
				typeParameters[0],
				new ParameterizedType(ctx.GetTypeDefinition(typeof(Func<,>)), new[] { typeParameters[0], typeParameters[1] }),
				new ParameterizedType(ctx.GetTypeDefinition(typeof(Func<,>)), new[] { typeParameters[1], typeParameters[2] })
			};
			// Signature:  M<X,Y,Z>(X x, Func<X,Y> y, Func<Y,Z> z) {}
			// Invocation: M(default(string), s => default(int), t => default(float));
			ResolveResult[] arguments = {
				new ResolveResult(KnownTypeReference.String.Resolve(ctx)),
				new MockImplicitLambda(new[] { KnownTypeReference.String.Resolve(ctx) }, KnownTypeReference.Int32.Resolve(ctx)),
				new MockImplicitLambda(new[] { KnownTypeReference.Int32.Resolve(ctx) }, KnownTypeReference.Single.Resolve(ctx))
			};
			bool success;
			Assert.AreEqual(
				new [] {
					KnownTypeReference.String.Resolve(ctx),
					KnownTypeReference.Int32.Resolve(ctx),
					KnownTypeReference.Single.Resolve(ctx)
				},
				ti.InferTypeArguments(typeParameters, arguments, parameterTypes, out success));
			Assert.IsTrue(success);
		}
		
		[Test]
		public void ConvertAllLambdaInference()
		{
			ITypeParameter[] classTypeParameters  = { new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T") };
			ITypeParameter[] methodTypeParameters = { new DefaultTypeParameter(EntityType.Method, 0, "R") };
			
			IType[] parameterTypes = {
				new ParameterizedType(ctx.GetTypeDefinition(typeof(Converter<,>)),
				                      new[] { classTypeParameters[0], methodTypeParameters[0] })
			};
			
			// Signature:  List<T>.ConvertAll<R>(Converter<T, R> converter);
			// Invocation: listOfString.ConvertAll(s => default(int));
			ResolveResult[] arguments = {
				new MockImplicitLambda(new[] { KnownTypeReference.String.Resolve(ctx) }, KnownTypeReference.Int32.Resolve(ctx))
			};
			IType[] classTypeArguments = {
				KnownTypeReference.String.Resolve(ctx)
			};
			
			bool success;
			Assert.AreEqual(
				new [] { KnownTypeReference.Int32.Resolve(ctx) },
				ti.InferTypeArguments(methodTypeParameters, arguments, parameterTypes, out success, classTypeArguments));
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
