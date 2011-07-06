// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
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
			AssertType(typeof(int), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Multiply, MakeResult(typeof(int)), MakeResult(typeof(int))));
			
			AssertType(typeof(float), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Multiply, MakeResult(typeof(int)), MakeConstant(0.0f)));
			
			AssertConstant(3.0f, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Multiply, MakeConstant(1.5f), MakeConstant(2)));
			
			AssertConstant(6, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Multiply, MakeConstant((byte)2), MakeConstant((byte)3)));
			
			AssertType(typeof(long?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Multiply, MakeResult(typeof(uint?)), MakeResult(typeof(int?))));
			
			AssertError(typeof(decimal), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Multiply, MakeResult(typeof(float)), MakeResult(typeof(decimal))));
		}
		
		[Test]
		public void Addition()
		{
			AssertType(typeof(int?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(short)), MakeResult(typeof(byte?))));
			
			AssertConstant(3.0, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(1.0f), MakeConstant(2.0)));
			
			AssertConstant(StringComparison.Ordinal, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(StringComparison.InvariantCulture), MakeConstant(2)));
			
			AssertConstant(StringComparison.OrdinalIgnoreCase, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant((short)3), MakeConstant(StringComparison.InvariantCulture)));
			
			AssertConstant("Text", resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant("Te"), MakeConstant("xt")));
			
			AssertConstant("", resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(null), resolver.ResolveCast(ResolveType(typeof(string)), MakeConstant(null))));
			
			AssertError(typeof(ReflectionHelper.Null), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(null), MakeConstant(null)));
			
			AssertType(typeof(Action), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(Action)), MakeResult(typeof(Action))));
			
			AssertType(typeof(Action<string>), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(Action<object>)), MakeResult(typeof(Action<string>))));
			
			Assert.IsTrue(resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(Action<int>)), MakeResult(typeof(Action<long>))).IsError);
			
			AssertType(typeof(StringComparison?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(StringComparison?)), MakeResult(typeof(int))));
			
			AssertType(typeof(StringComparison?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(int?)), MakeResult(typeof(StringComparison))));
			
			AssertType(typeof(long?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(int?)), MakeResult(typeof(uint?))));
			
			AssertType(typeof(int?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(ushort?)), MakeResult(typeof(ushort?))));
			
			Assert.IsTrue(resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(null), MakeConstant(null)).IsError);
			
			AssertType(typeof(int?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeConstant(1), MakeConstant(null)));
			
			AssertType(typeof(int*), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(int*)), MakeConstant(1)));
			
			AssertType(typeof(byte*), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Add, MakeResult(typeof(long)), MakeResult(typeof(byte*))));
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
			AssertType(typeof(int?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(short)), MakeResult(typeof(byte?))));
			
			AssertConstant(-1.0, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeConstant(1.0f), MakeConstant(2.0)));
			
			AssertConstant(StringComparison.InvariantCulture, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeConstant(StringComparison.Ordinal), MakeConstant(2)));
			
			AssertConstant(3, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeConstant(StringComparison.OrdinalIgnoreCase), MakeConstant(StringComparison.InvariantCulture)));
			
			Assert.IsTrue(resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeConstant("Te"), MakeConstant("xt")).IsError);
			
			AssertType(typeof(Action), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(Action)), MakeResult(typeof(Action))));
			
			AssertType(typeof(Action<string>), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(Action<object>)), MakeResult(typeof(Action<string>))));
			
			Assert.IsTrue(resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(Action<int>)), MakeResult(typeof(Action<long>))).IsError);
			
			AssertType(typeof(StringComparison?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(StringComparison?)), MakeResult(typeof(int))));
			
			AssertType(typeof(int?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(StringComparison?)), MakeResult(typeof(StringComparison))));
			
			Assert.IsTrue(resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(int?)), MakeResult(typeof(StringComparison))).IsError);
			
			AssertType(typeof(byte*), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(byte*)), MakeResult(typeof(uint))));
			
			AssertType(typeof(long), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Subtract, MakeResult(typeof(byte*)), MakeResult(typeof(byte*))));
		}
		
		[Test]
		public void ShiftTest()
		{
			AssertConstant(6, resolver.ResolveBinaryOperator(
				BinaryOperatorType.ShiftLeft, MakeConstant(3), MakeConstant(1)));
			
			AssertConstant(ulong.MaxValue >> 2, resolver.ResolveBinaryOperator(
				BinaryOperatorType.ShiftRight, MakeConstant(ulong.MaxValue), MakeConstant(2)));
			
			AssertType(typeof(int?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.ShiftLeft, MakeResult(typeof(ushort?)), MakeConstant(1)));
			
			AssertType(typeof(int?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.ShiftLeft, MakeConstant(null), MakeConstant(1)));
			
			AssertType(typeof(int?), resolver.ResolveBinaryOperator(
				BinaryOperatorType.ShiftLeft, MakeConstant(null), MakeConstant(null)));
		}
		
		[Test]
		public void Equality()
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
			
			AssertType(typeof(bool), resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeResult(typeof(int*)), MakeResult(typeof(uint*))));
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
			
			AssertConstant(false, resolver.ResolveBinaryOperator(
				BinaryOperatorType.Equality, MakeConstant(0), MakeConstant(StringComparison.Ordinal)));
			
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
		
		[Test, Ignore("user-defined operators not yet implemented")]
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
		
		[Test, Ignore("user-defined operators not yet implemented")]
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
			MemberResolveResult trr = Resolve<MemberResolveResult>(program);
			Assert.IsFalse(trr.IsError);
			Assert.AreEqual("A.op_Addition", trr.Member.FullName);
			// even though we're calling the lifted operator, trr.Member should be the original operator method
			Assert.AreEqual("S", trr.Member.ReturnType.Resolve(context).ReflectionName);
			Assert.AreEqual("System.Nullable`1[[S]]", trr.Type.ReflectionName);
		}
		
		[Test, Ignore("user-defined operators not yet implemented")]
		public void ThereIsNoLiftedOperatorsForClasses()
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
			MemberResolveResult trr = Resolve<MemberResolveResult>(program);
			Assert.IsTrue(trr.IsError); // cannot convert from A to S
			Assert.AreEqual("A.op_Addition", trr.Member.FullName);
			Assert.AreEqual("S", trr.Type.ReflectionName);
		}
	}
}
