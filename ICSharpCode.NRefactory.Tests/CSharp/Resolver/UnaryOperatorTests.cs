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
	public unsafe class UnaryOperatorTests : ResolverTestBase
	{
		[Test]
		public void TestAddressOf()
		{
			AssertType(typeof(int*), resolver.ResolveUnaryOperator(UnaryOperatorType.AddressOf, MakeResult(typeof(int))));
			AssertType(typeof(byte**), resolver.ResolveUnaryOperator(UnaryOperatorType.AddressOf, MakeResult(typeof(byte*))));
			AssertType(typeof(dynamic), resolver.ResolveUnaryOperator(UnaryOperatorType.AddressOf, MakeResult(typeof(dynamic))));
		}
		
		[Test]
		public void TestDereference()
		{
			AssertType(typeof(int), resolver.ResolveUnaryOperator(UnaryOperatorType.Dereference, MakeResult(typeof(int*))));
			AssertType(typeof(long*), resolver.ResolveUnaryOperator(UnaryOperatorType.Dereference, MakeResult(typeof(long**))));
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.Dereference, MakeResult(typeof(int))).IsError);
			AssertType(typeof(dynamic), resolver.ResolveUnaryOperator(UnaryOperatorType.Dereference, MakeResult(typeof(dynamic))));
		}
		
		[Test]
		public void TestIncrementDecrement()
		{
			AssertType(typeof(byte), resolver.ResolveUnaryOperator(UnaryOperatorType.Increment, MakeResult(typeof(byte))));
			AssertType(typeof(ulong), resolver.ResolveUnaryOperator(UnaryOperatorType.Decrement, MakeResult(typeof(ulong))));
			AssertType(typeof(short?), resolver.ResolveUnaryOperator(UnaryOperatorType.PostDecrement, MakeResult(typeof(short?))));
			AssertType(typeof(TypeCode), resolver.ResolveUnaryOperator(UnaryOperatorType.PostIncrement, MakeResult(typeof(TypeCode))));
			AssertType(typeof(TypeCode?), resolver.ResolveUnaryOperator(UnaryOperatorType.PostIncrement, MakeResult(typeof(TypeCode?))));
			AssertType(typeof(dynamic), resolver.ResolveUnaryOperator(UnaryOperatorType.PostIncrement, MakeResult(typeof(dynamic))));
			AssertError(typeof(object), resolver.ResolveUnaryOperator(UnaryOperatorType.Increment, MakeResult(typeof(object))));
			
			AssertType(typeof(int*), resolver.ResolveUnaryOperator(UnaryOperatorType.Increment, MakeResult(typeof(int*))));
			AssertType(typeof(uint*), resolver.ResolveUnaryOperator(UnaryOperatorType.PostDecrement, MakeResult(typeof(uint*))));
		}
		
		[Test]
		public void TestUnaryPlus()
		{
			AssertConstant(1, resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeConstant((sbyte)1)));
			AssertConstant(1, resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeConstant((byte)1)));
			AssertConstant(1, resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeConstant((short)1)));
			AssertConstant(1, resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeConstant((ushort)1)));
			AssertConstant(65, resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeConstant('A')));
			AssertConstant(1, resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeConstant(1)));
			AssertConstant((uint)1, resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeConstant((uint)1)));
			AssertConstant(1L, resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeConstant((long)1)));
			AssertConstant((ulong)1, resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeConstant((ulong)1)));
			
			AssertType(typeof(dynamic), resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeResult(typeof(dynamic))));
			AssertType(typeof(int?), resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeResult(typeof(ushort?))));
		}
		
		[Test]
		public void TestUnaryMinus()
		{
			AssertConstant(-1, resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeConstant(1)));
			AssertConstant(-1L, resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeConstant((uint)1)));
			AssertConstant(-2147483648L, resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeConstant(2147483648)));
			AssertConstant(-1.0f, resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeConstant(1.0f)));
			AssertConstant(-1.0, resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeConstant(1.0)));
			AssertConstant(1m, resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeConstant(-1m)));
			AssertConstant(-65, resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeConstant('A')));
			
			AssertType(typeof(dynamic), resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeResult(typeof(dynamic))));
			AssertType(typeof(long?), resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeResult(typeof(uint?))));
		}
		
		[Test]
		public void TestUnaryMinusUncheckedOverflow()
		{
			AssertConstant(-2147483648, resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeConstant(-2147483648)));
		}
		
		[Test]
		public void TestUnaryMinusCheckedOverflow()
		{
			resolver.CheckForOverflow = true;
			AssertError(typeof(int), resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeConstant(-2147483648)));
		}
		
		[Test]
		public void TestBitwiseNot()
		{
			AssertConstant(1, resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant(-2)));
			AssertConstant(~'A', resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant('A')));
			AssertConstant(~(sbyte)1, resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant((sbyte)1)));
			AssertConstant(~(byte)1, resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant((byte)1)));
			AssertConstant(~(short)1, resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant((short)1)));
			AssertConstant(~(ushort)1, resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant((ushort)1)));
			AssertConstant(~(uint)1, resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant((uint)1)));
			AssertConstant(~(long)1, resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant((long)1)));
			AssertConstant(~(ulong)1, resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant((ulong)1)));
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant(1.0)).IsError);
			
			AssertType(typeof(dynamic), resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeResult(typeof(dynamic))));
			AssertType(typeof(uint), resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeResult(typeof(uint))));
			AssertType(typeof(int?), resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeResult(typeof(ushort?))));
		}
		
		[Test]
		public void TestLogicalNot()
		{
			AssertConstant(true, resolver.ResolveUnaryOperator(UnaryOperatorType.Not, MakeConstant(false)));
			AssertConstant(false, resolver.ResolveUnaryOperator(UnaryOperatorType.Not, MakeConstant(true)));
			AssertType(typeof(dynamic), resolver.ResolveUnaryOperator(UnaryOperatorType.Not, MakeResult(typeof(dynamic))));
			AssertType(typeof(bool), resolver.ResolveUnaryOperator(UnaryOperatorType.Not, MakeResult(typeof(bool))));
			AssertType(typeof(bool?), resolver.ResolveUnaryOperator(UnaryOperatorType.Not, MakeResult(typeof(bool?))));
		}
		
		[Test]
		public void TestInvalidUnaryOperatorsOnEnum()
		{
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.Not, MakeConstant(StringComparison.Ordinal)).IsError);
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeConstant(StringComparison.Ordinal)).IsError);
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeConstant(StringComparison.Ordinal)).IsError);
			
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.Not, MakeResult(typeof(StringComparison))).IsError);
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeResult(typeof(StringComparison))).IsError);
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeResult(typeof(StringComparison))).IsError);
			
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.Not, MakeResult(typeof(StringComparison?))).IsError);
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.Plus, MakeResult(typeof(StringComparison?))).IsError);
			Assert.IsTrue(resolver.ResolveUnaryOperator(UnaryOperatorType.Minus, MakeResult(typeof(StringComparison?))).IsError);
		}
		
		[Test]
		public void TestBitwiseNotOnEnum()
		{
			AssertConstant(~StringComparison.Ordinal, resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant(StringComparison.Ordinal)));
			AssertConstant(~StringComparison.CurrentCultureIgnoreCase, resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeConstant(StringComparison.CurrentCultureIgnoreCase)));
			AssertType(typeof(StringComparison), resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeResult(typeof(StringComparison))));
			AssertType(typeof(StringComparison?), resolver.ResolveUnaryOperator(UnaryOperatorType.BitNot, MakeResult(typeof(StringComparison?))));
		}
	}
}
