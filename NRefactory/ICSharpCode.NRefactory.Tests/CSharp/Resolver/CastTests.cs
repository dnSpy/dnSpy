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
	public class CastTests : ResolverTestBase
	{
		CSharpResolver resolver;
		
		public override void SetUp()
		{
			base.SetUp();
			resolver = new CSharpResolver(compilation);
		}
		
		void TestCast(Type targetType, ResolveResult input, Conversion expectedConversion)
		{
			IType type = compilation.FindType(targetType);
			ResolveResult rr = resolver.ResolveCast(type, input);
			AssertType(targetType, rr);
			Assert.AreEqual(typeof(ConversionResolveResult), rr.GetType());
			var crr = (ConversionResolveResult)rr;
			Assert.AreEqual(expectedConversion, crr.Conversion, "ConversionResolveResult.Conversion");
			Assert.AreSame(input, crr.Input, "ConversionResolveResult.Input");
		}
		
		[Test]
		public void SimpleCast()
		{
			TestCast(typeof(int), MakeResult(typeof(float)), Conversion.ExplicitNumericConversion);
			TestCast(typeof(string), MakeResult(typeof(object)), Conversion.ExplicitReferenceConversion);
			TestCast(typeof(byte), MakeResult(typeof(dynamic)), Conversion.ExplicitDynamicConversion);
			TestCast(typeof(dynamic), MakeResult(typeof(double)), Conversion.BoxingConversion);
		}
		
		[Test]
		public void NullableCasts()
		{
			TestCast(typeof(int), MakeResult(typeof(int?)), Conversion.ExplicitNullableConversion);
			TestCast(typeof(int?), MakeResult(typeof(int)), Conversion.ImplicitNullableConversion);
			
			TestCast(typeof(int?), MakeResult(typeof(long?)), Conversion.ExplicitNullableConversion);
			TestCast(typeof(long?), MakeResult(typeof(int?)), Conversion.ImplicitNullableConversion);
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
			AssertConstant(uint.MaxValue, resolver.WithCheckForOverflow(false).ResolveCast(ResolveType(typeof(uint)), MakeConstant(-1.6)));
			AssertError(typeof(uint), resolver.WithCheckForOverflow(true).ResolveCast(ResolveType(typeof(uint)), MakeConstant(-1.6)));
		}
		
		[Test]
		public void FailingStringCast()
		{
			AssertError(typeof(string), resolver.ResolveCast(ResolveType(typeof(string)), MakeConstant(1)));
		}
		
		[Test]
		public void OverflowingCastToEnum()
		{
			AssertError(typeof(StringComparison), resolver.WithCheckForOverflow(true).ResolveCast(ResolveType(typeof(StringComparison)), MakeConstant(long.MaxValue)));
		}
	}
}
