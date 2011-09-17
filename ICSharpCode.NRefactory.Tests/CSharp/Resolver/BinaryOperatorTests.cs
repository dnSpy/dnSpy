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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	// assign short name to the fake reflection type
	using dynamic = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.Dynamic;
	
	[TestFixture]
	public unsafe class BinaryOperatorTests : ResolverTestBase
	{
		[Test]
		public void Multiplication()
		{
			TestOperator(MakeResult(typeof(int)), BinaryOperatorType.Multiply, MakeResult(typeof(int)),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(int));
			
			TestOperator(MakeResult(typeof(int)), BinaryOperatorType.Multiply, MakeConstant(0.0f),
			             Conversion.ImplicitNumericConversion, Conversion.IdentityConversion, typeof(float));
			
			AssertConstant(3.0f, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Multiply, MakeConstant(1.5f), MakeConstant(2)));
			
			AssertConstant(6, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Multiply, MakeConstant((byte)2), MakeConstant((byte)3)));
			
			TestOperator(MakeResult(typeof(uint?)), BinaryOperatorType.Multiply, MakeResult(typeof(int?)),
			             Conversion.ImplicitNullableConversion, Conversion.ImplicitNullableConversion, typeof(long?));
			
			AssertError(typeof(decimal), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Multiply, MakeResult(typeof(float)), MakeResult(typeof(decimal))));
		}
		
		[Test]
		public void Addition()
		{
			TestOperator(MakeResult(typeof(short)), BinaryOperatorType.Add, MakeResult(typeof(byte?)),
			             Conversion.ImplicitNullableConversion, Conversion.ImplicitNullableConversion, typeof(int?));
			
			AssertConstant(3.0, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(1.0f), MakeConstant(2.0)));
			
			AssertConstant("Text", resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant("Te"), MakeConstant("xt")));
			
			AssertConstant("", resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(null), resolver.ResolveCast(ResolveType(typeof(string)), MakeConstant(null))));
			
			AssertError(typeof(ReflectionHelper.Null), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(null), MakeConstant(null)));
			
			TestOperator(MakeResult(typeof(int?)), BinaryOperatorType.Add, MakeResult(typeof(uint?)),
			             Conversion.ImplicitNullableConversion, Conversion.ImplicitNullableConversion, typeof(long?));
			
			TestOperator(MakeResult(typeof(ushort?)), BinaryOperatorType.Add, MakeResult(typeof(ushort?)),
			             Conversion.ImplicitNullableConversion, Conversion.ImplicitNullableConversion, typeof(int?));
			
			TestOperator(MakeConstant(1), BinaryOperatorType.Add, MakeConstant(null),
			             Conversion.ImplicitNullableConversion, Conversion.NullLiteralConversion, typeof(int?));
		}
		
		[Test]
		public void StringPlusNull()
		{
			ResolveResult left = MakeResult(typeof(string));
			var rr = (BinaryOperatorResolveResult)resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, left, MakeConstant(null));
			AssertType(typeof(string), rr);
			Assert.AreSame(left, rr.Left);
			Assert.AreEqual("System.String", rr.Right.Type.FullName);
			Assert.IsTrue(rr.Right.IsCompileTimeConstant);
			Assert.IsNull(rr.Right.ConstantValue);
		}
		
		[Test]
		public void DelegateAddition()
		{
			TestOperator(MakeResult(typeof(Action)), BinaryOperatorType.Add, MakeResult(typeof(Action)),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(Action));
			
			TestOperator(MakeResult(typeof(Action<object>)), BinaryOperatorType.Add, MakeResult(typeof(Action<string>)),
			             Conversion.ImplicitReferenceConversion, Conversion.IdentityConversion, typeof(Action<string>));
			
			TestOperator(MakeResult(typeof(Action<string>)), BinaryOperatorType.Add, MakeResult(typeof(Action<object>)),
			             Conversion.IdentityConversion, Conversion.ImplicitReferenceConversion, typeof(Action<string>));
			
			Assert.IsTrue(resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(Action<int>)), MakeResult(typeof(Action<long>))).IsError);
		}
		
		
		[Test]
		public void EnumAddition()
		{
			AssertConstant(StringComparison.Ordinal, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(StringComparison.InvariantCulture), MakeConstant(2)));
			
			AssertConstant(StringComparison.OrdinalIgnoreCase, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant((short)3), MakeConstant(StringComparison.InvariantCulture)));
			
			TestOperator(MakeResult(typeof(StringComparison?)), BinaryOperatorType.Add, MakeResult(typeof(int)),
			             Conversion.IdentityConversion, Conversion.ImplicitNullableConversion, typeof(StringComparison?));
			
			TestOperator(MakeResult(typeof(StringComparison?)), BinaryOperatorType.Add, MakeResult(typeof(int?)),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(StringComparison?));
			
			TestOperator(MakeResult(typeof(int)), BinaryOperatorType.Add, MakeResult(typeof(StringComparison?)),
			             Conversion.ImplicitNullableConversion, Conversion.IdentityConversion, typeof(StringComparison?));
			
			TestOperator(MakeResult(typeof(int?)), BinaryOperatorType.Add, MakeResult(typeof(StringComparison?)),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(StringComparison?));
		}
		
		[Test]
		public void PointerAddition()
		{
			TestOperator(MakeResult(typeof(int*)), BinaryOperatorType.Add, MakeConstant(1),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(int*));
			
			TestOperator(MakeResult(typeof(long)), BinaryOperatorType.Add, MakeResult(typeof(byte*)),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(byte*));
		}
		
		[Test]
		public void AdditionWithOverflow()
		{
			resolver.CheckForOverflow = false;
			AssertConstant(int.MinValue, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(int.MaxValue), MakeConstant(1)));
			
			resolver.CheckForOverflow = true;
			AssertError(typeof(int), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(int.MaxValue), MakeConstant(1)));
		}
		
		
		[Test]
		public void Subtraction()
		{
			TestOperator(MakeResult(typeof(short)), BinaryOperatorType.Subtract, MakeResult(typeof(byte?)),
			             Conversion.ImplicitNullableConversion, Conversion.ImplicitNullableConversion, typeof(int?));
			
			TestOperator(MakeResult(typeof(float)), BinaryOperatorType.Subtract, MakeResult(typeof(long)),
			             Conversion.IdentityConversion, Conversion.ImplicitNumericConversion, typeof(float));
			
			AssertConstant(-1.0, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeConstant(1.0f), MakeConstant(2.0)));
			
			Assert.IsTrue(resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeConstant("Te"), MakeConstant("xt")).IsError);
		}
		
		[Test]
		public void EnumSubtraction()
		{
			AssertConstant(StringComparison.InvariantCulture, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeConstant(StringComparison.Ordinal), MakeConstant(2)));
			
			AssertConstant(3, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeConstant(StringComparison.OrdinalIgnoreCase), MakeConstant(StringComparison.InvariantCulture)));
			
			TestOperator(MakeResult(typeof(StringComparison?)), BinaryOperatorType.Subtract, MakeResult(typeof(int)),
			             Conversion.IdentityConversion, Conversion.ImplicitNullableConversion, typeof(StringComparison?));
			
			TestOperator(MakeResult(typeof(StringComparison?)), BinaryOperatorType.Subtract, MakeResult(typeof(StringComparison)),
			             Conversion.IdentityConversion, Conversion.ImplicitNullableConversion, typeof(int?));
			
			Assert.IsTrue(resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(int?)), MakeResult(typeof(StringComparison))).IsError);
		}
		
		[Test]
		public void DelegateSubtraction()
		{
			TestOperator(MakeResult(typeof(Action)), BinaryOperatorType.Subtract, MakeResult(typeof(Action)),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(Action));
			
			TestOperator(MakeResult(typeof(Action<object>)), BinaryOperatorType.Subtract, MakeResult(typeof(Action<string>)),
			             Conversion.ImplicitReferenceConversion, Conversion.IdentityConversion, typeof(Action<string>));
			
			TestOperator(MakeResult(typeof(Action<string>)), BinaryOperatorType.Subtract, MakeResult(typeof(Action<object>)),
			             Conversion.IdentityConversion, Conversion.ImplicitReferenceConversion, typeof(Action<string>));
			
			Assert.IsTrue(resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(Action<int>)), MakeResult(typeof(Action<long>))).IsError);
		}
		
		[Test]
		public void PointerSubtraction()
		{
			TestOperator(MakeResult(typeof(int*)), BinaryOperatorType.Subtract, MakeConstant(1),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(int*));
			
			TestOperator(MakeResult(typeof(byte*)), BinaryOperatorType.Subtract, MakeResult(typeof(uint)),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(byte*));
			
			TestOperator(MakeResult(typeof(byte*)), BinaryOperatorType.Subtract, MakeResult(typeof(short)),
			             Conversion.IdentityConversion, Conversion.ImplicitNumericConversion, typeof(byte*));
			
			TestOperator(MakeResult(typeof(byte*)), BinaryOperatorType.Subtract, MakeResult(typeof(byte*)),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(long));
			
			AssertError(typeof(long), resolver.ResolveBinaryOperator(BinaryOperatorType.Subtract, MakeResult(typeof(byte*)), MakeResult(typeof(int*))));
		}
		
		[Test]
		public void ShiftTest()
		{
			AssertConstant(6, resolver.ResolveBinaryOperator(
				BinaryOperatorType.ShiftLeft, MakeConstant(3), MakeConstant(1)));
			
			AssertConstant(ulong.MaxValue >> 2, resolver.ResolveBinaryOperator(
				BinaryOperatorType.ShiftRight, MakeConstant(ulong.MaxValue), MakeConstant(2)));
			
			TestOperator(MakeResult(typeof(ushort?)), BinaryOperatorType.ShiftLeft, MakeConstant(1),
			             Conversion.ImplicitNullableConversion, Conversion.ImplicitNullableConversion, typeof(int?));
			
			TestOperator(MakeConstant(null), BinaryOperatorType.ShiftLeft, MakeConstant(1),
			             Conversion.NullLiteralConversion, Conversion.ImplicitNullableConversion, typeof(int?));
			
			TestOperator(MakeResult(typeof(long)), BinaryOperatorType.ShiftLeft, MakeConstant(null),
			             Conversion.ImplicitNullableConversion, Conversion.NullLiteralConversion, typeof(long?));
			
			TestOperator(MakeConstant(null), BinaryOperatorType.ShiftLeft,  MakeConstant(null),
			             Conversion.NullLiteralConversion, Conversion.NullLiteralConversion, typeof(int?));
		}
		
		[Test]
		public void ConstantEquality()
		{
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(3), MakeConstant(3)));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(3), MakeConstant(3.0)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(2.9), MakeConstant(3)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(double.NaN), MakeConstant(double.NaN)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(float.NaN), MakeConstant(float.NaN)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant("A"), MakeConstant("B")));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant("A"), MakeConstant("A")));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(""), MakeConstant(null)));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(null), MakeConstant(null)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(1), MakeConstant(null)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(null), MakeConstant('a')));
		}
		
		[Test]
		public void Equality()
		{
			TestOperator(MakeResult(typeof(int*)), BinaryOperatorType.Equality, MakeResult(typeof(uint*)),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, typeof(bool));
			
			TestOperator(MakeResult(typeof(int)), BinaryOperatorType.Equality, MakeResult(typeof(int?)),
			             Conversion.ImplicitNullableConversion, Conversion.IdentityConversion, typeof(bool));
			
			TestOperator(MakeResult(typeof(int)), BinaryOperatorType.Equality, MakeResult(typeof(float)),
			             Conversion.ImplicitNumericConversion, Conversion.IdentityConversion, typeof(bool));
		}
		
		[Test]
		public void Inequality()
		{
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant(3), MakeConstant(3)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant(3), MakeConstant(3.0)));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant(2.9), MakeConstant(3)));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant(double.NaN), MakeConstant(double.NaN)));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant(float.NaN), MakeConstant(double.NaN)));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant(float.NaN), MakeConstant(float.NaN)));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant("A"), MakeConstant("B")));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant("A"), MakeConstant("A")));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant(""), MakeConstant(null)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant(null), MakeConstant(null)));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant(1), MakeConstant(null)));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeConstant(null), MakeConstant('a')));
			
			AssertType(typeof(bool), resolver.ResolveBinaryOperator(
				BinaryOperatorType.InEquality, MakeResult(typeof(int*)), MakeResult(typeof(uint*))));
		}
		
		[Test]
		public void EqualityEnum()
		{
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(0), MakeConstant(StringComparison.Ordinal)));
			
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(0), MakeConstant(StringComparison.CurrentCulture)));
			
			Assert.IsFalse(resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(StringComparison.Ordinal), MakeConstant(1)).IsCompileTimeConstant);
		}
		
		[Test]
		public void RelationalOperators()
		{
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.LessThan, MakeConstant(0), MakeConstant(0)));
			
			AssertType(typeof(bool), resolver.ResolveBinaryOperator(
				BinaryOperatorType.LessThan, MakeResult(typeof(int*)), MakeResult(typeof(uint*))));
		}
		
		[Test]
		public void RelationalEnum()
		{
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.LessThan, MakeConstant(0), MakeConstant(StringComparison.Ordinal)));
			
			AssertError(typeof(bool), resolver.ResolveBinaryOperator(
				BinaryOperatorType.LessThanOrEqual, MakeConstant(1), MakeConstant(StringComparison.Ordinal)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.GreaterThan, MakeConstant(StringComparison.CurrentCultureIgnoreCase), MakeConstant(StringComparison.Ordinal)));
			
			AssertType(typeof(bool), resolver.ResolveBinaryOperator(
				BinaryOperatorType.GreaterThan, MakeResult(typeof(StringComparison?)), MakeResult(typeof(StringComparison?))));
		}
		
		[Test]
		public void BitAnd()
		{
			AssertConstant(5, resolver.ResolveBinaryOperator(
				BinaryOperatorType.BitwiseAnd, MakeConstant(7), MakeConstant(13)));
			
			AssertType(typeof(int?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.BitwiseAnd, MakeConstant(null), MakeConstant((short)13)));
			
			AssertType(typeof(long?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.BitwiseAnd, MakeResult(typeof(uint?)), MakeConstant((short)13)));
			
			AssertType(typeof(uint?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.BitwiseAnd, MakeResult(typeof(uint?)), MakeConstant((int)13)));
			
			AssertType(typeof(ulong?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.BitwiseAnd, MakeResult(typeof(ulong?)), MakeConstant((long)13)));
			
			Assert.IsTrue(resolver.ResolveBinaryOperator(
				BinaryOperatorType.BitwiseAnd, MakeResult(typeof(ulong?)), MakeConstant((short)13)).IsError);
		}
		
		[Test]
		public void BitXor()
		{
			AssertConstant(6L ^ 3, resolver.ResolveBinaryOperator(
				BinaryOperatorType.ExclusiveOr, MakeConstant(6L), MakeConstant(3)));
			
			AssertConstant(6UL ^ 3L, resolver.ResolveBinaryOperator(
				BinaryOperatorType.ExclusiveOr, MakeConstant(6UL), MakeConstant(3L)));
			
			AssertError(typeof(ulong), resolver.ResolveBinaryOperator(
				BinaryOperatorType.ExclusiveOr, MakeConstant(6UL), MakeConstant(-3L)));
		}
		
		[Test]
		public void BitwiseEnum()
		{
			AssertConstant(AttributeTargets.Field | AttributeTargets.Property, resolver.ResolveBinaryOperator(
				BinaryOperatorType.BitwiseOr, MakeConstant(AttributeTargets.Field), MakeConstant(AttributeTargets.Property)));
			
			AssertConstant(AttributeTargets.Field & AttributeTargets.All, resolver.ResolveBinaryOperator(
				BinaryOperatorType.BitwiseAnd, MakeConstant(AttributeTargets.Field), MakeConstant(AttributeTargets.All)));
			
			AssertConstant(AttributeTargets.Field & 0, resolver.ResolveBinaryOperator(
				BinaryOperatorType.BitwiseAnd, MakeConstant(AttributeTargets.Field), MakeConstant(0)));
			
			AssertConstant(0 | AttributeTargets.Field, resolver.ResolveBinaryOperator(
				BinaryOperatorType.BitwiseOr, MakeConstant(0), MakeConstant(AttributeTargets.Field)));
		}
		
		[Test]
		public void LogicalAnd()
		{
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.ConditionalAnd, MakeConstant(true), MakeConstant(true)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.ConditionalAnd, MakeConstant(false), MakeConstant(true)));
			
			AssertError(typeof(bool), resolver.ResolveBinaryOperator(
				BinaryOperatorType.ConditionalAnd, MakeConstant(false), MakeResult(typeof(bool?))));
		}
		
		[Test]
		public void LogicalOr()
		{
			AssertConstant(true, resolver.ResolveBinaryOperator(
				BinaryOperatorType.ConditionalOr, MakeConstant(false), MakeConstant(true)));
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.ConditionalOr, MakeConstant(false), MakeConstant(false)));
			
			AssertError(typeof(bool), resolver.ResolveBinaryOperator(
				BinaryOperatorType.ConditionalOr, MakeConstant(false), MakeResult(typeof(bool?))));
		}
		
		[Test]
		public void NullCoalescing()
		{
			AssertType(typeof(int), resolver.ResolveBinaryOperator(
				BinaryOperatorType.NullCoalescing, MakeResult(typeof(int?)), MakeResult(typeof(short))));
			
			AssertType(typeof(int?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.NullCoalescing, MakeResult(typeof(int?)), MakeResult(typeof(short?))));
			
			AssertType(typeof(object), resolver.ResolveBinaryOperator(
				BinaryOperatorType.NullCoalescing, MakeResult(typeof(string)), MakeResult(typeof(object))));
			
			AssertError(typeof(string), resolver.ResolveBinaryOperator(
				BinaryOperatorType.NullCoalescing, MakeResult(typeof(string)), MakeResult(typeof(int))));
			
			AssertType(typeof(dynamic), resolver.ResolveBinaryOperator(
				BinaryOperatorType.NullCoalescing, MakeResult(typeof(dynamic)), MakeResult(typeof(string))));
			
			AssertType(typeof(dynamic), resolver.ResolveBinaryOperator(
				BinaryOperatorType.NullCoalescing, MakeResult(typeof(string)), MakeResult(typeof(dynamic))));
		}
		
		[Test]
		public void LiftedUserDefined()
		{
			AssertType(typeof(TimeSpan), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(DateTime)), MakeResult(typeof(DateTime))));
			AssertType(typeof(TimeSpan?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(DateTime?)), MakeResult(typeof(DateTime))));
			AssertType(typeof(TimeSpan?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(DateTime)), MakeResult(typeof(DateTime?))));
			AssertType(typeof(TimeSpan?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(DateTime?)), MakeResult(typeof(DateTime?))));
		}
		
		[Test]
		public void UserDefinedNeedsLiftingDueToImplicitConversion()
		{
			string program = @"struct S {}
struct A {
	public static implicit operator S?(A a) { return null; }
	
	public static S operator +(A a, S s) { return s; }
}
class Test {
	void M(A a) {
		var s = $a + a$;
	}
}
";
			var irr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(irr.IsError);
			Assert.IsTrue(irr.IsLiftedOperatorInvocation);
			Assert.AreEqual("A.op_Addition", irr.Member.FullName);
			// even though we're calling the lifted operator, trr.Member should be the original operator method
			Assert.AreEqual("S", irr.Member.ReturnType.Resolve(context).ReflectionName);
			Assert.AreEqual("System.Nullable`1[[S]]", irr.Type.ReflectionName);
			
			Conversion lhsConv = ((ConversionResolveResult)irr.Arguments[0]).Conversion;
			Conversion rhsConv = ((ConversionResolveResult)irr.Arguments[1]).Conversion;
			Assert.AreEqual(Conversion.ImplicitNullableConversion, lhsConv);
			Assert.IsTrue(rhsConv.IsUserDefined);
			Assert.AreEqual("A.op_Implicit", rhsConv.Method.FullName);
		}
		
		[Test]
		public void ThereAreNoLiftedOperatorsForClasses()
		{
			string program = @"struct S {}
class A {
	public static implicit operator S?(A a) { return null; }
	
	public static S operator +(A a, S s) { return s; }
}
class Test {
	void M(A a) {
		var s = $a + a$;
	}
}
";
			var irr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsTrue(irr.IsError); // cannot convert from A to S
			Assert.AreEqual("A.op_Addition", irr.Member.FullName);
			Assert.AreEqual("S", irr.Type.ReflectionName);
		}
	}
}
