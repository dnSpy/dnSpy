// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	// assign short name to the fake reflection type
	using dynamic = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.Dynamic;
	
	[TestFixture]
	public class CastTests : ResolverTestBase
	{
		[Test]
		public void SimpleCast()
		{
			AssertType(typeof(int), resolver.ResolveCast(ResolveType(typeof(int)), MakeResult(typeof(float))));
			AssertType(typeof(string), resolver.ResolveCast(ResolveType(typeof(string)), MakeResult(typeof(object))));
			AssertType(typeof(byte), resolver.ResolveCast(ResolveType(typeof(byte)), MakeResult(typeof(dynamic))));
			AssertType(typeof(dynamic), resolver.ResolveCast(ResolveType(typeof(dynamic)), MakeResult(typeof(double))));
		}
		
		[Test]
		public void ConstantValueCast()
		{
			AssertConstant("Hello", resolver.ResolveCast(ResolveType(typeof(string)), MakeConstant("Hello")));
			AssertConstant((byte)1L, resolver.ResolveCast(ResolveType(typeof(byte)), MakeConstant(1L)));
			AssertConstant(3, resolver.ResolveCast(ResolveType(typeof(int)), MakeConstant(3.1415)));
			AssertConstant(3, resolver.ResolveCast(ResolveType(typeof(int)), MakeConstant(3.99)));
			AssertConstant((short)-3, resolver.ResolveCast(ResolveType(typeof(short)), MakeConstant(-3.99f)));
			AssertConstant(-3L, resolver.ResolveCast(ResolveType(typeof(long)), MakeConstant(-3.5)));
		}
		
		[Test]
		public void OverflowingCast()
		{
			resolver.CheckForOverflow = false;
			AssertConstant(uint.MaxValue, resolver.ResolveCast(ResolveType(typeof(uint)), MakeConstant(-1.6)));
			resolver.CheckForOverflow = true;
			AssertError(typeof(uint), resolver.ResolveCast(ResolveType(typeof(uint)), MakeConstant(-1.6)));
		}
		
		[Test]
		public void FailingStringCast()
		{
			AssertError(typeof(string), resolver.ResolveCast(ResolveType(typeof(string)), MakeConstant(1)));
		}
		
		[Test]
		public void OverflowingCastToEnum()
		{
			resolver.CheckForOverflow = true;
			AssertError(typeof(StringComparison), resolver.ResolveCast(ResolveType(typeof(StringComparison)), MakeConstant(long.MaxValue)));
		}
	}
}
