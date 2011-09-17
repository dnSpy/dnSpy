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

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	// assign short names to the fake reflection types
	using Null = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.Null;
	using dynamic = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.Dynamic;
	using C = Conversion;
	
	[TestFixture]
	public unsafe class ConversionsTest
	{
		ITypeResolveContext ctx = CecilLoaderTests.Mscorlib;
		Conversions conversions = new Conversions(CecilLoaderTests.Mscorlib);
		
		Conversion ImplicitConversion(Type from, Type to)
		{
			IType from2 = from.ToTypeReference().Resolve(ctx);
			IType to2 = to.ToTypeReference().Resolve(ctx);
			return conversions.ImplicitConversion(from2, to2);
		}
		
		Conversion ExplicitConversion(Type from, Type to)
		{
			IType from2 = from.ToTypeReference().Resolve(ctx);
			IType to2 = to.ToTypeReference().Resolve(ctx);
			return conversions.ExplicitConversion(from2, to2);
		}
		
		[Test]
		public void IdentityConversions()
		{
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(char), typeof(char)));
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(string), typeof(string)));
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(object), typeof(object)));
			Assert.AreEqual(C.None,               ImplicitConversion(typeof(bool), typeof(char)));
			
			Assert.AreEqual(C.IdentityConversion, conversions.ImplicitConversion(SharedTypes.Dynamic, SharedTypes.Dynamic));
			Assert.AreEqual(C.IdentityConversion, conversions.ImplicitConversion(SharedTypes.UnknownType, SharedTypes.UnknownType));
			Assert.AreEqual(C.IdentityConversion, conversions.ImplicitConversion(SharedTypes.Null, SharedTypes.Null));
		}
		
		[Test]
		public void DynamicIdentityConversions()
		{
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(object), typeof(ReflectionHelper.Dynamic)));
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(ReflectionHelper.Dynamic), typeof(object)));
		}
		
		[Test]
		public void ComplexDynamicIdentityConversions()
		{
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(List<object>), typeof(List<dynamic>)));
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(List<dynamic>), typeof(List<object>)));
			Assert.AreEqual(C.None,               ImplicitConversion(typeof(List<string>), typeof(List<dynamic>)));
			Assert.AreEqual(C.None,               ImplicitConversion(typeof(List<dynamic>), typeof(List<string>)));
			
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(List<List<dynamic>[]>), typeof(List<List<object>[]>)));
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(List<List<object>[]>), typeof(List<List<dynamic>[]>)));
			Assert.AreEqual(C.None,               ImplicitConversion(typeof(List<List<object>[,]>), typeof(List<List<dynamic>[]>)));
		}
		
		[Test]
		public void PrimitiveConversions()
		{
			Assert.AreEqual(C.ImplicitNumericConversion, ImplicitConversion(typeof(char), typeof(ushort)));
			Assert.AreEqual(C.None,                      ImplicitConversion(typeof(byte), typeof(char)));
			Assert.AreEqual(C.ImplicitNumericConversion, ImplicitConversion(typeof(int), typeof(long)));
			Assert.AreEqual(C.None,                      ImplicitConversion(typeof(long), typeof(int)));
			Assert.AreEqual(C.ImplicitNumericConversion, ImplicitConversion(typeof(int), typeof(float)));
			Assert.AreEqual(C.None,                      ImplicitConversion(typeof(bool), typeof(float)));
			Assert.AreEqual(C.ImplicitNumericConversion, ImplicitConversion(typeof(float), typeof(double)));
			Assert.AreEqual(C.None,                      ImplicitConversion(typeof(float), typeof(decimal)));
			Assert.AreEqual(C.ImplicitNumericConversion, ImplicitConversion(typeof(char), typeof(long)));
			Assert.AreEqual(C.ImplicitNumericConversion, ImplicitConversion(typeof(uint), typeof(long)));
		}
		
		[Test]
		public void NullableConversions()
		{
			Assert.AreEqual(C.ImplicitNullableConversion, ImplicitConversion(typeof(char), typeof(ushort?)));
			Assert.AreEqual(C.None,                       ImplicitConversion(typeof(byte), typeof(char?)));
			Assert.AreEqual(C.ImplicitNullableConversion, ImplicitConversion(typeof(int), typeof(long?)));
			Assert.AreEqual(C.None,                       ImplicitConversion(typeof(long), typeof(int?)));
			Assert.AreEqual(C.ImplicitNullableConversion, ImplicitConversion(typeof(int), typeof(float?)));
			Assert.AreEqual(C.None                      , ImplicitConversion(typeof(bool), typeof(float?)));
			Assert.AreEqual(C.ImplicitNullableConversion, ImplicitConversion(typeof(float), typeof(double?)));
			Assert.AreEqual(C.None,                       ImplicitConversion(typeof(float), typeof(decimal?)));
		}
		
		[Test]
		public void NullableConversions2()
		{
			Assert.AreEqual(C.ImplicitNullableConversion, ImplicitConversion(typeof(char?), typeof(ushort?)));
			Assert.AreEqual(C.None,                       ImplicitConversion(typeof(byte?), typeof(char?)));
			Assert.AreEqual(C.ImplicitNullableConversion, ImplicitConversion(typeof(int?), typeof(long?)));
			Assert.AreEqual(C.None,                       ImplicitConversion(typeof(long?), typeof(int?)));
			Assert.AreEqual(C.ImplicitNullableConversion, ImplicitConversion(typeof(int?), typeof(float?)));
			Assert.AreEqual(C.None,                       ImplicitConversion(typeof(bool?), typeof(float?)));
			Assert.AreEqual(C.ImplicitNullableConversion, ImplicitConversion(typeof(float?), typeof(double?)));
			Assert.AreEqual(C.None,                       ImplicitConversion(typeof(float?), typeof(decimal?)));
		}
		
		[Test]
		public void NullLiteralConversions()
		{
			Assert.AreEqual(C.NullLiteralConversion, ImplicitConversion(typeof(Null), typeof(int?)));
			Assert.AreEqual(C.NullLiteralConversion, ImplicitConversion(typeof(Null), typeof(char?)));
			Assert.AreEqual(C.None,                  ImplicitConversion(typeof(Null), typeof(int)));
			Assert.AreEqual(C.NullLiteralConversion, ImplicitConversion(typeof(Null), typeof(object)));
			Assert.AreEqual(C.NullLiteralConversion, ImplicitConversion(typeof(Null), typeof(dynamic)));
			Assert.AreEqual(C.NullLiteralConversion, ImplicitConversion(typeof(Null), typeof(string)));
			Assert.AreEqual(C.NullLiteralConversion, ImplicitConversion(typeof(Null), typeof(int[])));
		}
		
		[Test]
		public void SimpleReferenceConversions()
		{
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(string), typeof(object)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(BitArray), typeof(ICollection)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(IList), typeof(IEnumerable)));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(object), typeof(string)));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(ICollection), typeof(BitArray)));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(IEnumerable), typeof(IList)));
		}
		
		[Test]
		public void SimpleDynamicConversions()
		{
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(string),  typeof(dynamic)));
			Assert.AreEqual(C.ImplicitDynamicConversion,   ImplicitConversion(typeof(dynamic), typeof(string)));
			Assert.AreEqual(C.BoxingConversion,            ImplicitConversion(typeof(int),     typeof(dynamic)));
			Assert.AreEqual(C.ImplicitDynamicConversion,   ImplicitConversion(typeof(dynamic), typeof(int)));
		}
		
		[Test]
		public void ParameterizedTypeConversions()
		{
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(List<string>), typeof(ICollection<string>)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(IList<string>), typeof(ICollection<string>)));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(List<string>), typeof(ICollection<object>)));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(IList<string>), typeof(ICollection<object>)));
		}
		
		[Test]
		public void ArrayConversions()
		{
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(string[]), typeof(object[])));
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(string[,]), typeof(object[,])));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(string[]), typeof(object[,])));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(object[]), typeof(string[])));
			
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(string[]), typeof(IList<string>)));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(string[,]), typeof(IList<string>)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(string[]), typeof(IList<object>)));
			
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(string[]), typeof(Array)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(string[]), typeof(ICloneable)));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(Array), typeof(string[])));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(object), typeof(object[])));
		}
		
		[Test]
		public void VarianceConversions()
		{
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                ImplicitConversion(typeof(List<string>), typeof(IEnumerable<object>)));
			Assert.AreEqual(C.None,
			                ImplicitConversion(typeof(List<object>), typeof(IEnumerable<string>)));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                ImplicitConversion(typeof(IEnumerable<string>), typeof(IEnumerable<object>)));
			Assert.AreEqual(C.None,
			                ImplicitConversion(typeof(ICollection<string>), typeof(ICollection<object>)));
			
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                ImplicitConversion(typeof(Comparer<object>), typeof(IComparer<string>)));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                ImplicitConversion(typeof(Comparer<object>), typeof(IComparer<Array>)));
			Assert.AreEqual(C.None,
			                ImplicitConversion(typeof(Comparer<object>), typeof(Comparer<string>)));
			
			Assert.AreEqual(C.None,
			                ImplicitConversion(typeof(List<object>), typeof(IEnumerable<string>)));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                ImplicitConversion(typeof(IEnumerable<string>), typeof(IEnumerable<object>)));
			
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                ImplicitConversion(typeof(Func<ICollection, ICollection>), typeof(Func<IList, IEnumerable>)));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                ImplicitConversion(typeof(Func<IEnumerable, IList>), typeof(Func<ICollection, ICollection>)));
			Assert.AreEqual(C.None,
			                ImplicitConversion(typeof(Func<ICollection, ICollection>), typeof(Func<IEnumerable, IList>)));
			Assert.AreEqual(C.None,
			                ImplicitConversion(typeof(Func<IList, IEnumerable>), typeof(Func<ICollection, ICollection>)));
		}
		
		[Test]
		public void ImplicitPointerConversion()
		{
			Assert.AreEqual(C.ImplicitPointerConversion, ImplicitConversion(typeof(Null), typeof(int*)));
			Assert.AreEqual(C.ImplicitPointerConversion, ImplicitConversion(typeof(int*), typeof(void*)));
		}
		
		[Test]
		public void ExplicitPointerConversion()
		{
			Assert.AreEqual(C.ExplicitPointerConversion, ExplicitConversion(typeof(int*), typeof(short)));
			Assert.AreEqual(C.ExplicitPointerConversion, ExplicitConversion(typeof(short), typeof(void*)));
			
			Assert.AreEqual(C.ExplicitPointerConversion, ExplicitConversion(typeof(void*), typeof(int*)));
			Assert.AreEqual(C.ExplicitPointerConversion, ExplicitConversion(typeof(long*), typeof(byte*)));
		}
		
		[Test]
		public void NoConversionFromPointerTypeToObject()
		{
			Assert.AreEqual(C.None, ImplicitConversion(typeof(int*), typeof(object)));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(int*), typeof(dynamic)));
		}
		
		[Test]
		public void UnconstrainedTypeParameter()
		{
			DefaultTypeParameter t = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T");
			DefaultTypeParameter t2 = new DefaultTypeParameter(EntityType.TypeDefinition, 1, "T2");
			DefaultTypeParameter tm = new DefaultTypeParameter(EntityType.Method, 0, "TM");
			
			Assert.AreEqual(C.None, conversions.ImplicitConversion(SharedTypes.Null, t));
			Assert.AreEqual(C.BoxingConversion, conversions.ImplicitConversion(t, KnownTypeReference.Object.Resolve(ctx)));
			Assert.AreEqual(C.BoxingConversion, conversions.ImplicitConversion(t, SharedTypes.Dynamic));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(ValueType))));
			
			Assert.AreEqual(C.IdentityConversion, conversions.ImplicitConversion(t, t));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t2, t));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, t2));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, tm));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(tm, t));
		}
		
		[Test]
		public void TypeParameterWithReferenceTypeConstraint()
		{
			DefaultTypeParameter t = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T");
			t.HasReferenceTypeConstraint = true;
			
			Assert.AreEqual(C.NullLiteralConversion, conversions.ImplicitConversion(SharedTypes.Null, t));
			Assert.AreEqual(C.ImplicitReferenceConversion, conversions.ImplicitConversion(t, KnownTypeReference.Object.Resolve(ctx)));
			Assert.AreEqual(C.ImplicitReferenceConversion, conversions.ImplicitConversion(t, SharedTypes.Dynamic));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(ValueType))));
		}
		
		[Test]
		public void TypeParameterWithValueTypeConstraint()
		{
			DefaultTypeParameter t = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T");
			t.HasValueTypeConstraint = true;
			
			Assert.AreEqual(C.None, conversions.ImplicitConversion(SharedTypes.Null, t));
			Assert.AreEqual(C.BoxingConversion, conversions.ImplicitConversion(t, KnownTypeReference.Object.Resolve(ctx)));
			Assert.AreEqual(C.BoxingConversion, conversions.ImplicitConversion(t, SharedTypes.Dynamic));
			Assert.AreEqual(C.BoxingConversion, conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(ValueType))));
		}
		
		[Test]
		public void TypeParameterWithClassConstraint()
		{
			DefaultTypeParameter t = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T");
			t.Constraints.Add(ctx.GetTypeDefinition(typeof(StringComparer)));
			
			Assert.AreEqual(C.NullLiteralConversion,
			                conversions.ImplicitConversion(SharedTypes.Null, t));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                conversions.ImplicitConversion(t, KnownTypeReference.Object.Resolve(ctx)));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                conversions.ImplicitConversion(t, SharedTypes.Dynamic));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(ValueType))));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(StringComparer))));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(IComparer))));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, typeof(IComparer<int>).ToTypeReference().Resolve(ctx)));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                conversions.ImplicitConversion(t, typeof(IComparer<string>).ToTypeReference().Resolve(ctx)));
		}
		
		[Test]
		public void TypeParameterWithInterfaceConstraint()
		{
			DefaultTypeParameter t = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T");
			t.Constraints.Add(ctx.GetTypeDefinition(typeof(IList)));
			
			Assert.AreEqual(C.None, conversions.ImplicitConversion(SharedTypes.Null, t));
			Assert.AreEqual(C.BoxingConversion,
			                conversions.ImplicitConversion(t, KnownTypeReference.Object.Resolve(ctx)));
			Assert.AreEqual(C.BoxingConversion,
			                conversions.ImplicitConversion(t, SharedTypes.Dynamic));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(ValueType))));
			Assert.AreEqual(C.BoxingConversion,
			                conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(IList))));
			Assert.AreEqual(C.BoxingConversion,
			                conversions.ImplicitConversion(t, ctx.GetTypeDefinition(typeof(IEnumerable))));
		}
		
		[Test]
		public void UserDefinedImplicitConversion()
		{
			Conversion c = ImplicitConversion(typeof(DateTime), typeof(DateTimeOffset));
			Assert.IsTrue(c.IsImplicitConversion && c.IsUserDefined);
			Assert.AreEqual("System.DateTimeOffset.op_Implicit", c.Method.FullName);
			
			Assert.AreEqual(C.None, ImplicitConversion(typeof(DateTimeOffset), typeof(DateTime)));
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
		
		Conversion IntegerLiteralConversion(object value, Type to)
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
		
		[Test]
		public void ExpansiveInheritance()
		{
			SimpleProjectContent pc = new SimpleProjectContent();
			DefaultTypeDefinition a = new DefaultTypeDefinition(pc, string.Empty, "A");
			DefaultTypeDefinition b = new DefaultTypeDefinition(pc, string.Empty, "B");
			// interface A<in U>
			a.Kind = TypeKind.Interface;
			a.TypeParameters.Add(new DefaultTypeParameter(EntityType.TypeDefinition, 0, "U") { Variance = VarianceModifier.Contravariant });
			// interface B<X> : A<A<B<X>>> { }
			DefaultTypeParameter x = new DefaultTypeParameter(EntityType.TypeDefinition, 0, "X");
			b.TypeParameters.Add(x);
			b.BaseTypes.Add(new ParameterizedType(a, new[] { new ParameterizedType(a, new [] { new ParameterizedType(b, new [] { x }) } ) }));
			
			IType type1 = new ParameterizedType(b, new[] { KnownTypeReference.Double.Resolve(ctx) });
			IType type2 = new ParameterizedType(a, new [] { new ParameterizedType(b, new[] { KnownTypeReference.String.Resolve(ctx) }) });
			Assert.IsFalse(conversions.ImplicitConversion(type1, type2));
		}
	}
}
