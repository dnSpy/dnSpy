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
using ICSharpCode.NRefactory.CSharp.TypeSystem;
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
	public unsafe class ConversionsTest : ResolverTestBase
	{
		CSharpConversions conversions;
		
		public override void SetUp()
		{
			base.SetUp();
			conversions = new CSharpConversions(compilation);
		}
		
		Conversion ImplicitConversion(Type from, Type to)
		{
			IType from2 = compilation.FindType(from);
			IType to2 = compilation.FindType(to);
			return conversions.ImplicitConversion(from2, to2);
		}
		
		[Test]
		public void IdentityConversions()
		{
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(char), typeof(char)));
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(string), typeof(string)));
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(object), typeof(object)));
			Assert.AreEqual(C.None,               ImplicitConversion(typeof(bool), typeof(char)));
			
			Assert.AreEqual(C.IdentityConversion, conversions.ImplicitConversion(SpecialType.Dynamic, SpecialType.Dynamic));
			Assert.AreEqual(C.IdentityConversion, conversions.ImplicitConversion(SpecialType.UnknownType, SpecialType.UnknownType));
			Assert.AreEqual(C.IdentityConversion, conversions.ImplicitConversion(SpecialType.NullType, SpecialType.NullType));
		}
		
		[Test]
		public void DynamicIdentityConversions()
		{
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(object), typeof(dynamic)));
			Assert.AreEqual(C.IdentityConversion, ImplicitConversion(typeof(dynamic), typeof(object)));
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
		public void EnumerationConversion()
		{
			ResolveResult zero = new ConstantResolveResult(compilation.FindType(KnownTypeCode.Int32), 0);
			ResolveResult one = new ConstantResolveResult(compilation.FindType(KnownTypeCode.Int32), 1);
			C implicitEnumerationConversion = C.EnumerationConversion(true, false);
			Assert.AreEqual(implicitEnumerationConversion, conversions.ImplicitConversion(zero, compilation.FindType(typeof(StringComparison))));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(one, compilation.FindType(typeof(StringComparison))));
		}
		
		[Test]
		public void NullableConversions()
		{
			Assert.AreEqual(C.ImplicitLiftedNumericConversion, ImplicitConversion(typeof(char), typeof(ushort?)));
			Assert.AreEqual(C.None,                            ImplicitConversion(typeof(byte), typeof(char?)));
			Assert.AreEqual(C.ImplicitLiftedNumericConversion, ImplicitConversion(typeof(int), typeof(long?)));
			Assert.AreEqual(C.None,                            ImplicitConversion(typeof(long), typeof(int?)));
			Assert.AreEqual(C.ImplicitLiftedNumericConversion, ImplicitConversion(typeof(int), typeof(float?)));
			Assert.AreEqual(C.None                           , ImplicitConversion(typeof(bool), typeof(float?)));
			Assert.AreEqual(C.ImplicitLiftedNumericConversion, ImplicitConversion(typeof(float), typeof(double?)));
			Assert.AreEqual(C.None,                            ImplicitConversion(typeof(float), typeof(decimal?)));
		}
		
		[Test]
		public void NullableConversions2()
		{
			Assert.AreEqual(C.ImplicitLiftedNumericConversion, ImplicitConversion(typeof(char?), typeof(ushort?)));
			Assert.AreEqual(C.None,                            ImplicitConversion(typeof(byte?), typeof(char?)));
			Assert.AreEqual(C.ImplicitLiftedNumericConversion, ImplicitConversion(typeof(int?), typeof(long?)));
			Assert.AreEqual(C.None,                            ImplicitConversion(typeof(long?), typeof(int?)));
			Assert.AreEqual(C.ImplicitLiftedNumericConversion, ImplicitConversion(typeof(int?), typeof(float?)));
			Assert.AreEqual(C.None,                            ImplicitConversion(typeof(bool?), typeof(float?)));
			Assert.AreEqual(C.ImplicitLiftedNumericConversion, ImplicitConversion(typeof(float?), typeof(double?)));
			Assert.AreEqual(C.None,                            ImplicitConversion(typeof(float?), typeof(decimal?)));
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
		public void ConversionToDynamic()
		{
			Assert.AreEqual(C.ImplicitReferenceConversion, ImplicitConversion(typeof(string),  typeof(dynamic)));
			Assert.AreEqual(C.BoxingConversion,            ImplicitConversion(typeof(int),     typeof(dynamic)));
		}
		
		[Test]
		public void ConversionFromDynamic()
		{
			// There is no conversion from the type 'dynamic' to other types (except the identity conversion to object).
			// Such conversions only exists from dynamic expression.
			// This is an important distinction for type inference (see TypeInferenceTests.IEnumerableCovarianceWithDynamic)
			Assert.AreEqual(C.None, ImplicitConversion(typeof(dynamic), typeof(string)));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(dynamic), typeof(int)));
			
			var dynamicRR = new ResolveResult(SpecialType.Dynamic);
			Assert.AreEqual(C.ImplicitDynamicConversion, conversions.ImplicitConversion(dynamicRR, compilation.FindType(typeof(string))));
			Assert.AreEqual(C.ImplicitDynamicConversion, conversions.ImplicitConversion(dynamicRR, compilation.FindType(typeof(int))));
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
		public void NoConversionFromPointerTypeToObject()
		{
			Assert.AreEqual(C.None, ImplicitConversion(typeof(int*), typeof(object)));
			Assert.AreEqual(C.None, ImplicitConversion(typeof(int*), typeof(dynamic)));
		}
		
		[Test]
		public void UnconstrainedTypeParameter()
		{
			ITypeParameter t = new DefaultTypeParameter(compilation, EntityType.TypeDefinition, 0, "T");
			ITypeParameter t2 = new DefaultTypeParameter(compilation, EntityType.TypeDefinition, 1, "T2");
			ITypeParameter tm = new DefaultTypeParameter(compilation, EntityType.Method, 0, "TM");
			
			Assert.AreEqual(C.None, conversions.ImplicitConversion(SpecialType.NullType, t));
			Assert.AreEqual(C.BoxingConversion, conversions.ImplicitConversion(t, compilation.FindType(KnownTypeCode.Object)));
			Assert.AreEqual(C.BoxingConversion, conversions.ImplicitConversion(t, SpecialType.Dynamic));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, compilation.FindType(typeof(ValueType))));
			
			Assert.AreEqual(C.IdentityConversion, conversions.ImplicitConversion(t, t));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t2, t));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, t2));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, tm));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(tm, t));
		}
		
		[Test]
		public void TypeParameterWithReferenceTypeConstraint()
		{
			ITypeParameter t = new DefaultTypeParameter(compilation, EntityType.TypeDefinition, 0, "T", hasReferenceTypeConstraint: true);
			
			Assert.AreEqual(C.NullLiteralConversion, conversions.ImplicitConversion(SpecialType.NullType, t));
			Assert.AreEqual(C.ImplicitReferenceConversion, conversions.ImplicitConversion(t, compilation.FindType(KnownTypeCode.Object)));
			Assert.AreEqual(C.ImplicitReferenceConversion, conversions.ImplicitConversion(t, SpecialType.Dynamic));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, compilation.FindType(typeof(ValueType))));
		}
		
		[Test]
		public void TypeParameterWithValueTypeConstraint()
		{
			ITypeParameter t = new DefaultTypeParameter(compilation, EntityType.TypeDefinition, 0, "T", hasValueTypeConstraint: true);
			
			Assert.AreEqual(C.None, conversions.ImplicitConversion(SpecialType.NullType, t));
			Assert.AreEqual(C.BoxingConversion, conversions.ImplicitConversion(t, compilation.FindType(KnownTypeCode.Object)));
			Assert.AreEqual(C.BoxingConversion, conversions.ImplicitConversion(t, SpecialType.Dynamic));
			Assert.AreEqual(C.BoxingConversion, conversions.ImplicitConversion(t, compilation.FindType(typeof(ValueType))));
		}
		
		[Test]
		public void TypeParameterWithClassConstraint()
		{
			ITypeParameter t = new DefaultTypeParameter(compilation, EntityType.TypeDefinition, 0, "T",
			                                            constraints: new[] { compilation.FindType(typeof(StringComparer)) });
			
			Assert.AreEqual(C.NullLiteralConversion,
			                conversions.ImplicitConversion(SpecialType.NullType, t));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                conversions.ImplicitConversion(t, compilation.FindType(KnownTypeCode.Object)));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                conversions.ImplicitConversion(t, SpecialType.Dynamic));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, compilation.FindType(typeof(ValueType))));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                conversions.ImplicitConversion(t, compilation.FindType(typeof(StringComparer))));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                conversions.ImplicitConversion(t, compilation.FindType(typeof(IComparer))));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, compilation.FindType(typeof(IComparer<int>))));
			Assert.AreEqual(C.ImplicitReferenceConversion,
			                conversions.ImplicitConversion(t, compilation.FindType(typeof(IComparer<string>))));
		}
		
		[Test]
		public void TypeParameterWithInterfaceConstraint()
		{
			ITypeParameter t = new DefaultTypeParameter(compilation, EntityType.TypeDefinition, 0, "T",
			                                            constraints: new [] { compilation.FindType(typeof(IList)) });
			
			Assert.AreEqual(C.None, conversions.ImplicitConversion(SpecialType.NullType, t));
			Assert.AreEqual(C.BoxingConversion,
			                conversions.ImplicitConversion(t, compilation.FindType(KnownTypeCode.Object)));
			Assert.AreEqual(C.BoxingConversion,
			                conversions.ImplicitConversion(t, SpecialType.Dynamic));
			Assert.AreEqual(C.None, conversions.ImplicitConversion(t, compilation.FindType(typeof(ValueType))));
			Assert.AreEqual(C.BoxingConversion,
			                conversions.ImplicitConversion(t, compilation.FindType(typeof(IList))));
			Assert.AreEqual(C.BoxingConversion,
			                conversions.ImplicitConversion(t, compilation.FindType(typeof(IEnumerable))));
		}
		
		[Test]
		public void UserDefinedImplicitConversion()
		{
			Conversion c = ImplicitConversion(typeof(DateTime), typeof(DateTimeOffset));
			Assert.IsTrue(c.IsImplicit && c.IsUserDefined);
			Assert.AreEqual("System.DateTimeOffset.op_Implicit", c.Method.FullName);
			
			Assert.AreEqual(C.None, ImplicitConversion(typeof(DateTimeOffset), typeof(DateTime)));
		}
		
		[Test]
		public void UserDefinedImplicitNullableConversion()
		{
			// User-defined conversion followed by nullable conversion
			Conversion c = ImplicitConversion(typeof(DateTime), typeof(DateTimeOffset?));
			Assert.IsTrue(c.IsValid && c.IsUserDefined);
			Assert.IsFalse(c.IsLifted);
			// Lifted user-defined conversion
			c = ImplicitConversion(typeof(DateTime?), typeof(DateTimeOffset?));
			Assert.IsTrue(c.IsValid && c.IsUserDefined && c.IsLifted);
			// User-defined conversion doesn't drop the nullability
			c = ImplicitConversion(typeof(DateTime?), typeof(DateTimeOffset));
			Assert.IsFalse(c.IsValid);
		}
		
		bool IntegerLiteralConversion(object value, Type to)
		{
			IType fromType = compilation.FindType(value.GetType());
			ConstantResolveResult crr = new ConstantResolveResult(fromType, value);
			IType to2 = compilation.FindType(to);
			return conversions.ImplicitConversion(crr, to2).IsValid;
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
			IType sType = compilation.FindType(s);
			IType t1Type = compilation.FindType(t1);
			IType t2Type = compilation.FindType(t2);
			return conversions.BetterConversion(sType, t1Type, t2Type);
		}
		
		int BetterConversion(object value, Type t1, Type t2)
		{
			IType fromType = compilation.FindType(value.GetType());
			ConstantResolveResult crr = new ConstantResolveResult(fromType, value);
			IType t1Type = compilation.FindType(t1);
			IType t2Type = compilation.FindType(t2);
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
			var a = new DefaultUnresolvedTypeDefinition(string.Empty, "A");
			var b = new DefaultUnresolvedTypeDefinition(string.Empty, "B");
			// interface A<in U>
			a.Kind = TypeKind.Interface;
			a.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 0, "U") { Variance = VarianceModifier.Contravariant });
			// interface B<X> : A<A<B<X>>> { }
			b.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 0, "X"));
			b.BaseTypes.Add(new ParameterizedTypeReference(
				a, new[] { new ParameterizedTypeReference(
					a, new [] { new ParameterizedTypeReference(
						b, new [] { new TypeParameterReference(EntityType.TypeDefinition, 0) }
					) } ) }));
			
			ICompilation compilation = TypeSystemHelper.CreateCompilation(a, b);
			ITypeDefinition resolvedA = compilation.MainAssembly.GetTypeDefinition(a);
			ITypeDefinition resolvedB = compilation.MainAssembly.GetTypeDefinition(b);
			
			IType type1 = new ParameterizedType(resolvedB, new [] { compilation.FindType(KnownTypeCode.Double) });
			IType type2 = new ParameterizedType(resolvedA, new [] { new ParameterizedType(resolvedB, new[] { compilation.FindType(KnownTypeCode.String) }) });
			Assert.IsFalse(conversions.ImplicitConversion(type1, type2).IsValid);
		}

		[Test]
		public void ImplicitTypeParameterConversion()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T t) where T : U {
		U u = $t$;
	}
}";
			Assert.AreEqual(C.BoxingConversion, GetConversion(program));
		}
		
		[Test]
		public void InvalidImplicitTypeParameterConversion()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T t) where U : T {
		U u = $t$;
	}
}";
			Assert.AreEqual(C.None, GetConversion(program));
		}
		
		[Test]
		public void ImplicitTypeParameterArrayConversion()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T[] t) where T : U {
		U[] u = $t$;
	}
}";
			// invalid, e.g. T=int[], U=object[]
			Assert.AreEqual(C.None, GetConversion(program));
		}
		
		[Test]
		public void ImplicitTypeParameterConversionWithClassConstraint()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T t) where T : class, U where U : class {
		U u = $t$;
	}
}";
			Assert.AreEqual(C.ImplicitReferenceConversion, GetConversion(program));
		}
		
		[Test]
		public void ImplicitTypeParameterArrayConversionWithClassConstraint()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T[] t) where T : class, U where U : class {
		U[] u = $t$;
	}
}";
			Assert.AreEqual(C.ImplicitReferenceConversion, GetConversion(program));
		}
		
		[Test]
		public void ImplicitTypeParameterConversionWithClassConstraintOnlyOnT()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T t) where T : class, U {
		U u = $t$;
	}
}";
			Assert.AreEqual(C.ImplicitReferenceConversion, GetConversion(program));
		}
		
		[Test]
		public void ImplicitTypeParameterArrayConversionWithClassConstraintOnlyOnT()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T[] t) where T : class, U {
		U[] u = $t$;
	}
}";
			Assert.AreEqual(C.ImplicitReferenceConversion, GetConversion(program));
		}
		
	}
}
