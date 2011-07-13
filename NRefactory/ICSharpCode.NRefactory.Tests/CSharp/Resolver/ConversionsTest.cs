// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	// assign short names to the fake reflection types
	using Null = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.Null;
	using dynamic = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.Dynamic;
	
	[TestFixture]
	public unsafe class ConversionsTest
	{
		ITypeResolveContext ctx = CecilLoaderTests.Mscorlib;
		Conversions conversions = new Conversions(CecilLoaderTests.Mscorlib);
		
		bool ImplicitConversion(Type from, Type to)
		{
			IType from2 = from.ToTypeReference().Resolve(ctx);
			IType to2 = to.ToTypeReference().Resolve(ctx);
			return conversions.ImplicitConversion(from2, to2);
		}
		
		[Test]
		public void IdentityConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(char), typeof(char)));
			Assert.IsTrue(ImplicitConversion(typeof(string), typeof(string)));
			Assert.IsTrue(ImplicitConversion(typeof(object), typeof(object)));
			Assert.IsFalse(ImplicitConversion(typeof(bool), typeof(char)));
			
			Assert.IsTrue(conversions.ImplicitConversion(SharedTypes.Dynamic, SharedTypes.Dynamic));
			Assert.IsTrue(conversions.ImplicitConversion(SharedTypes.UnknownType, SharedTypes.UnknownType));
			Assert.IsTrue(conversions.ImplicitConversion(SharedTypes.Null, SharedTypes.Null));
		}
		
		[Test]
		public void DynamicIdentityConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(object), typeof(ReflectionHelper.Dynamic)));
			Assert.IsTrue(ImplicitConversion(typeof(ReflectionHelper.Dynamic), typeof(object)));
		}
		
		[Test]
		public void ComplexDynamicIdentityConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(List<object>), typeof(List<dynamic>)));
			Assert.IsTrue(ImplicitConversion(typeof(List<dynamic>), typeof(List<object>)));
			Assert.IsFalse(ImplicitConversion(typeof(List<string>), typeof(List<dynamic>)));
			Assert.IsFalse(ImplicitConversion(typeof(List<dynamic>), typeof(List<string>)));
			
			Assert.IsTrue(ImplicitConversion(typeof(List<List<dynamic>[]>), typeof(List<List<object>[]>)));
			Assert.IsTrue(ImplicitConversion(typeof(List<List<object>[]>), typeof(List<List<dynamic>[]>)));
			Assert.IsFalse(ImplicitConversion(typeof(List<List<object>[,]>), typeof(List<List<dynamic>[]>)));
		}
		
		[Test]
		public void PrimitiveConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(char), typeof(ushort)));
			Assert.IsFalse(ImplicitConversion(typeof(byte), typeof(char)));
			Assert.IsTrue(ImplicitConversion(typeof(int), typeof(long)));
			Assert.IsFalse(ImplicitConversion(typeof(long), typeof(int)));
			Assert.IsTrue(ImplicitConversion(typeof(int), typeof(float)));
			Assert.IsFalse(ImplicitConversion(typeof(bool), typeof(float)));
			Assert.IsTrue(ImplicitConversion(typeof(float), typeof(double)));
			Assert.IsFalse(ImplicitConversion(typeof(float), typeof(decimal)));
			Assert.IsTrue(ImplicitConversion(typeof(char), typeof(long)));
			Assert.IsTrue(ImplicitConversion(typeof(uint), typeof(long)));
		}
		
		[Test]
		public void NullableConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(char), typeof(ushort?)));
			Assert.IsFalse(ImplicitConversion(typeof(byte), typeof(char?)));
			Assert.IsTrue(ImplicitConversion(typeof(int), typeof(long?)));
			Assert.IsFalse(ImplicitConversion(typeof(long), typeof(int?)));
			Assert.IsTrue(ImplicitConversion(typeof(int), typeof(float?)));
			Assert.IsFalse(ImplicitConversion(typeof(bool), typeof(float?)));
			Assert.IsTrue(ImplicitConversion(typeof(float), typeof(double?)));
			Assert.IsFalse(ImplicitConversion(typeof(float), typeof(decimal?)));
		}
		
		[Test]
		public void NullableConversions2()
		{
			Assert.IsTrue(ImplicitConversion(typeof(char?), typeof(ushort?)));
			Assert.IsFalse(ImplicitConversion(typeof(byte?), typeof(char?)));
			Assert.IsTrue(ImplicitConversion(typeof(int?), typeof(long?)));
			Assert.IsFalse(ImplicitConversion(typeof(long?), typeof(int?)));
			Assert.IsTrue(ImplicitConversion(typeof(int?), typeof(float?)));
			Assert.IsFalse(ImplicitConversion(typeof(bool?), typeof(float?)));
			Assert.IsTrue(ImplicitConversion(typeof(float?), typeof(double?)));
			Assert.IsFalse(ImplicitConversion(typeof(float?), typeof(decimal?)));
		}
		
		[Test]
		public void NullLiteralConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(Null), typeof(int?)));
			Assert.IsTrue(ImplicitConversion(typeof(Null), typeof(char?)));
			Assert.IsFalse(ImplicitConversion(typeof(Null), typeof(int)));
			Assert.IsTrue(ImplicitConversion(typeof(Null), typeof(object)));
			Assert.IsTrue(ImplicitConversion(typeof(Null), typeof(dynamic)));
			Assert.IsTrue(ImplicitConversion(typeof(Null), typeof(string)));
			Assert.IsTrue(ImplicitConversion(typeof(Null), typeof(int[])));
		}
		
		[Test]
		public void SimpleReferenceConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(string), typeof(object)));
			Assert.IsTrue(ImplicitConversion(typeof(BitArray), typeof(ICollection)));
			Assert.IsTrue(ImplicitConversion(typeof(IList), typeof(IEnumerable)));
			Assert.IsFalse(ImplicitConversion(typeof(object), typeof(string)));
			Assert.IsFalse(ImplicitConversion(typeof(ICollection), typeof(BitArray)));
			Assert.IsFalse(ImplicitConversion(typeof(IEnumerable), typeof(IList)));
		}
		
		[Test]
		public void SimpleDynamicConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(string), typeof(dynamic)));
			Assert.IsTrue(ImplicitConversion(typeof(dynamic), typeof(string)));
			Assert.IsTrue(ImplicitConversion(typeof(int), typeof(dynamic)));
			Assert.IsTrue(ImplicitConversion(typeof(dynamic), typeof(int)));
		}
		
		[Test]
		public void ParameterizedTypeConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(List<string>), typeof(ICollection<string>)));
			Assert.IsTrue(ImplicitConversion(typeof(IList<string>), typeof(ICollection<string>)));
			Assert.IsFalse(ImplicitConversion(typeof(List<string>), typeof(ICollection<object>)));
			Assert.IsFalse(ImplicitConversion(typeof(IList<string>), typeof(ICollection<object>)));
		}
		
		[Test]
		public void ArrayConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(string[]), typeof(object[])));
			Assert.IsTrue(ImplicitConversion(typeof(string[,]), typeof(object[,])));
			Assert.IsFalse(ImplicitConversion(typeof(string[]), typeof(object[,])));
			Assert.IsFalse(ImplicitConversion(typeof(object[]), typeof(string[])));
			
			Assert.IsTrue(ImplicitConversion(typeof(string[]), typeof(IList<string>)));
			Assert.IsFalse(ImplicitConversion(typeof(string[,]), typeof(IList<string>)));
			Assert.IsTrue(ImplicitConversion(typeof(string[]), typeof(IList<object>)));
			
			Assert.IsTrue(ImplicitConversion(typeof(string[]), typeof(Array)));
			Assert.IsTrue(ImplicitConversion(typeof(string[]), typeof(ICloneable)));
			Assert.IsFalse(ImplicitConversion(typeof(Array), typeof(string[])));
			Assert.IsFalse(ImplicitConversion(typeof(object), typeof(object[])));
		}
		
		[Test]
		public void VarianceConversions()
		{
			Assert.IsTrue(ImplicitConversion(typeof(List<string>), typeof(IEnumerable<object>)));
			Assert.IsFalse(ImplicitConversion(typeof(List<object>), typeof(IEnumerable<string>)));
			Assert.IsTrue(ImplicitConversion(typeof(IEnumerable<string>), typeof(IEnumerable<object>)));
			Assert.IsFalse(ImplicitConversion(typeof(ICollection<string>), typeof(ICollection<object>)));
			
			Assert.IsTrue(ImplicitConversion(typeof(Comparer<object>), typeof(IComparer<string>)));
			Assert.IsTrue(ImplicitConversion(typeof(Comparer<object>), typeof(IComparer<Array>)));
			Assert.IsFalse(ImplicitConversion(typeof(Comparer<object>), typeof(Comparer<string>)));
			
			Assert.IsFalse(ImplicitConversion(typeof(List<object>), typeof(IEnumerable<string>)));
			Assert.IsTrue(ImplicitConversion(typeof(IEnumerable<string>), typeof(IEnumerable<object>)));
			
			Assert.IsTrue(ImplicitConversion(typeof(Func<ICollection, ICollection>), typeof(Func<IList, IEnumerable>)));
			Assert.IsTrue(ImplicitConversion(typeof(Func<IEnumerable, IList>), typeof(Func<ICollection, ICollection>)));
			Assert.IsFalse(ImplicitConversion(typeof(Func<ICollection, ICollection>), typeof(Func<IEnumerable, IList>)));
			Assert.IsFalse(ImplicitConversion(typeof(Func<IList, IEnumerable>), typeof(Func<ICollection, ICollection>)));
		}
		
		[Test]
		public void PointerConversion()
		{
			Assert.IsTrue(ImplicitConversion(typeof(Null), typeof(int*)));
			Assert.IsTrue(ImplicitConversion(typeof(int*), typeof(void*)));
		}
		
		[Test]
		public void NoConversionFromPointerTypeToObject()
		{
			Assert.IsFalse(ImplicitConversion(typeof(int*), typeof(object)));
			Assert.IsFalse(ImplicitConversion(typeof(int*), typeof(dynamic)));
		}
		
		[Test]
		public void UnconstrainedTypeParameter()
		{
			DefaultTypeParameter t = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T");
			DefaultTypeParameter t2 = new DefaultTypeParameter(EntityType.TypeDefinition, 1, "T2");
			DefaultTypeParameter tm = new DefaultTypeParameter(EntityType.Method, 0, "TM");
			
			Assert.IsFalse(conversions.ImplicitConversion(SharedTypes.Null, t));
			Assert.IsTrue(conversions.ImplicitConversion(t, KnownTypeReference.Object.Resolve(ctx)));
			Assert.IsTrue(conversions.ImplicitConversion(t, SharedTypes.Dynamic));
			Assert.IsFalse(conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(ValueType))));
			
			Assert.IsTrue(conversions.ImplicitConversion(t, t));
			Assert.IsFalse(conversions.ImplicitConversion(t2, t));
			Assert.IsFalse(conversions.ImplicitConversion(t, t2));
			Assert.IsFalse(conversions.ImplicitConversion(t, tm));
			Assert.IsFalse(conversions.ImplicitConversion(tm, t));
		}
		
		[Test]
		public void TypeParameterWithReferenceTypeConstraint()
		{
			DefaultTypeParameter t = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T");
			t.HasReferenceTypeConstraint = true;
			
			Assert.IsTrue(conversions.ImplicitConversion(SharedTypes.Null, t));
			Assert.IsTrue(conversions.ImplicitConversion(t, KnownTypeReference.Object.Resolve(ctx)));
			Assert.IsTrue(conversions.ImplicitConversion(t, SharedTypes.Dynamic));
			Assert.IsFalse(conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(ValueType))));
		}
		
		[Test]
		public void TypeParameterWithValueTypeConstraint()
		{
			DefaultTypeParameter t = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T");
			t.HasValueTypeConstraint = true;
			
			Assert.IsFalse(conversions.ImplicitConversion(SharedTypes.Null, t));
			Assert.IsTrue(conversions.ImplicitConversion(t, KnownTypeReference.Object.Resolve(ctx)));
			Assert.IsTrue(conversions.ImplicitConversion(t, SharedTypes.Dynamic));
			Assert.IsTrue(conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(ValueType))));
		}
		
		[Test]
		public void TypeParameterWithClassConstraint()
		{
			DefaultTypeParameter t = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T");
			t.Constraints.Add(ctx.GetTypeDefinition(typeof(StringComparer)));
			
			Assert.IsTrue(conversions.ImplicitConversion(SharedTypes.Null, t));
			Assert.IsTrue(conversions.ImplicitConversion(t, KnownTypeReference.Object.Resolve(ctx)));
			Assert.IsTrue(conversions.ImplicitConversion(t, SharedTypes.Dynamic));
			Assert.IsFalse(conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(ValueType))));
			Assert.IsTrue(conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(StringComparer))));
			Assert.IsTrue(conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(IComparer))));
			Assert.IsFalse(conversions.ImplicitConversion(t, typeof(IComparer<int>).ToTypeReference().Resolve(ctx)));
			Assert.IsTrue(conversions.ImplicitConversion(t, typeof(IComparer<string>).ToTypeReference().Resolve(ctx)));
		}
		
		[Test]
		public void TypeParameterWithInterfaceConstraint()
		{
			DefaultTypeParameter t = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T");
			t.Constraints.Add(ctx.GetTypeDefinition(typeof(IList)));
			
			Assert.IsFalse(conversions.ImplicitConversion(SharedTypes.Null, t));
			Assert.IsTrue(conversions.ImplicitConversion(t, KnownTypeReference.Object.Resolve(ctx)));
			Assert.IsTrue(conversions.ImplicitConversion(t, SharedTypes.Dynamic));
			Assert.IsFalse(conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(ValueType))));
			Assert.IsTrue(conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(IList))));
			Assert.IsTrue(conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(IEnumerable))));
		}
		
		[Test]
		public void UserDefinedImplicitConversion()
		{
			Assert.IsTrue(ImplicitConversion(typeof(DateTime), typeof(DateTimeOffset)));
			Assert.IsFalse(ImplicitConversion(typeof(DateTimeOffset), typeof(DateTime)));
		}
		
		[Test]
		public void UserDefinedImplicitNullableConversion()
		{
			// User-defined conversion followed by nullable conversion
			Assert.IsTrue(ImplicitConversion(typeof(DateTime), typeof(DateTimeOffset?)));
			// Lifted user-defined conversion
			Assert.IsTrue(ImplicitConversion(typeof(DateTime?), typeof(DateTimeOffset?)));
			// User-defined conversion doesn't drop the nullability
			Assert.IsFalse(ImplicitConversion(typeof(DateTime?), typeof(DateTimeOffset)));
		}
		
		bool IntegerLiteralConversion(object value, Type to)
		{
			IType fromType = value.GetType().ToTypeReference().Resolve(ctx);
			ConstantResolveResult crr = new ConstantResolveResult(fromType, value);
			IType to2 = to.ToTypeReference().Resolve(ctx);
			return conversions.ImplicitConversion(crr, to2);
		}
		
		[Test]
		public void IntegerLiteralToEnumConversions()
		{
			Assert.IsTrue(IntegerLiteralConversion(0, typeof(LoaderOptimization)));
			Assert.IsTrue(IntegerLiteralConversion(0L, typeof(LoaderOptimization)));
			Assert.IsTrue(IntegerLiteralConversion(0, typeof(LoaderOptimization?)));
			Assert.IsFalse(IntegerLiteralConversion(0, typeof(string)));
			Assert.IsFalse(IntegerLiteralConversion(1, typeof(LoaderOptimization)));
		}
		
		[Test]
		public void ImplicitConstantExpressionConversion()
		{
			Assert.IsTrue(IntegerLiteralConversion(0, typeof(int)));
			Assert.IsTrue(IntegerLiteralConversion(0, typeof(ushort)));
			Assert.IsTrue(IntegerLiteralConversion(0, typeof(sbyte)));
			
			Assert.IsTrue (IntegerLiteralConversion(-1, typeof(int)));
			Assert.IsFalse(IntegerLiteralConversion(-1, typeof(ushort)));
			Assert.IsTrue (IntegerLiteralConversion(-1, typeof(sbyte)));
			
			Assert.IsTrue (IntegerLiteralConversion(200, typeof(int)));
			Assert.IsTrue (IntegerLiteralConversion(200, typeof(ushort)));
			Assert.IsFalse(IntegerLiteralConversion(200, typeof(sbyte)));
		}
		
		[Test]
		public void ImplicitLongConstantExpressionConversion()
		{
			Assert.IsFalse(IntegerLiteralConversion(0L, typeof(int)));
			Assert.IsTrue(IntegerLiteralConversion(0L, typeof(long)));
			Assert.IsTrue(IntegerLiteralConversion(0L, typeof(ulong)));
			
			Assert.IsTrue(IntegerLiteralConversion(-1L, typeof(long)));
			Assert.IsFalse(IntegerLiteralConversion(-1L, typeof(ulong)));
		}
		
		[Test]
		public void ImplicitConstantExpressionConversionToNullable()
		{
			Assert.IsTrue(IntegerLiteralConversion(0, typeof(uint?)));
			Assert.IsTrue(IntegerLiteralConversion(0, typeof(short?)));
			Assert.IsTrue(IntegerLiteralConversion(0, typeof(byte?)));
			
			Assert.IsFalse(IntegerLiteralConversion(-1, typeof(uint?)));
			Assert.IsTrue (IntegerLiteralConversion(-1, typeof(short?)));
			Assert.IsFalse(IntegerLiteralConversion(-1, typeof(byte?)));
			
			Assert.IsTrue(IntegerLiteralConversion(200, typeof(uint?)));
			Assert.IsTrue(IntegerLiteralConversion(200, typeof(short?)));
			Assert.IsTrue(IntegerLiteralConversion(200, typeof(byte?)));
			
			Assert.IsFalse(IntegerLiteralConversion(0L, typeof(uint?)));
			Assert.IsTrue (IntegerLiteralConversion(0L, typeof(long?)));
			Assert.IsTrue (IntegerLiteralConversion(0L, typeof(ulong?)));
			
			Assert.IsTrue(IntegerLiteralConversion(-1L, typeof(long?)));
			Assert.IsFalse(IntegerLiteralConversion(-1L, typeof(ulong?)));
		}
		
		[Test]
		public void ImplicitConstantExpressionConversionNumberInterfaces()
		{
			Assert.IsTrue(IntegerLiteralConversion(0, typeof(IFormattable)));
			Assert.IsTrue(IntegerLiteralConversion(0, typeof(IComparable<int>)));
			Assert.IsFalse(IntegerLiteralConversion(0, typeof(IComparable<short>)));
			Assert.IsFalse(IntegerLiteralConversion(0, typeof(IComparable<long>)));
		}
		
		int BetterConversion(Type s, Type t1, Type t2)
		{
			IType sType = s.ToTypeReference().Resolve(ctx);
			IType t1Type = t1.ToTypeReference().Resolve(ctx);
			IType t2Type = t2.ToTypeReference().Resolve(ctx);
			return conversions.BetterConversion(sType, t1Type, t2Type);
		}
		
		int BetterConversion(object value, Type t1, Type t2)
		{
			IType fromType = value.GetType().ToTypeReference().Resolve(ctx);
			ConstantResolveResult crr = new ConstantResolveResult(fromType, value);
			IType t1Type = t1.ToTypeReference().Resolve(ctx);
			IType t2Type = t2.ToTypeReference().Resolve(ctx);
			return conversions.BetterConversion(crr, t1Type, t2Type);
		}
		
		[Test]
		public void BetterConversion()
		{
			Assert.AreEqual(1, BetterConversion(typeof(string), typeof(string), typeof(object)));
			Assert.AreEqual(2, BetterConversion(typeof(string), typeof(object), typeof(IComparable<string>)));
			Assert.AreEqual(0, BetterConversion(typeof(string), typeof(IEnumerable<char>), typeof(IComparable<string>)));
		}
		
		[Test]
		public void BetterPrimitiveConversion()
		{
			Assert.AreEqual(1, BetterConversion(typeof(short), typeof(int), typeof(long)));
			Assert.AreEqual(1, BetterConversion(typeof(short), typeof(int), typeof(uint)));
			Assert.AreEqual(2, BetterConversion(typeof(ushort), typeof(uint), typeof(int)));
			Assert.AreEqual(1, BetterConversion(typeof(char), typeof(short), typeof(int)));
			Assert.AreEqual(1, BetterConversion(typeof(char), typeof(ushort), typeof(int)));
			Assert.AreEqual(1, BetterConversion(typeof(sbyte), typeof(long), typeof(ulong)));
			Assert.AreEqual(2, BetterConversion(typeof(byte), typeof(ushort), typeof(short)));
			
			Assert.AreEqual(1, BetterConversion(1, typeof(sbyte), typeof(byte)));
			Assert.AreEqual(2, BetterConversion(1, typeof(ushort), typeof(sbyte)));
		}
		
		[Test]
		public void BetterNullableConversion()
		{
			Assert.AreEqual(0, BetterConversion(typeof(byte), typeof(int), typeof(uint?)));
			Assert.AreEqual(0, BetterConversion(typeof(byte?), typeof(int?), typeof(uint?)));
			Assert.AreEqual(1, BetterConversion(typeof(byte), typeof(ushort?), typeof(uint?)));
			Assert.AreEqual(2, BetterConversion(typeof(byte?), typeof(ulong?), typeof(uint?)));
			Assert.AreEqual(0, BetterConversion(typeof(byte), typeof(ushort?), typeof(uint)));
			Assert.AreEqual(0, BetterConversion(typeof(byte), typeof(ushort?), typeof(int)));
			Assert.AreEqual(2, BetterConversion(typeof(byte), typeof(ulong?), typeof(uint)));
			Assert.AreEqual(0, BetterConversion(typeof(byte), typeof(ulong?), typeof(int)));
			Assert.AreEqual(2, BetterConversion(typeof(ushort?), typeof(long?), typeof(int?)));
			Assert.AreEqual(0, BetterConversion(typeof(sbyte), typeof(int?), typeof(uint?)));
		}
	}
}
