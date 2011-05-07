// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	// assign short name to the fake reflection type
	using dynamic = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.Dynamic;
	
	[TestFixture]
	public class ConditionalOperatorTests : ResolverTestBase
	{
		[Test]
		public void PickMoreGeneralOfTheTypes()
		{
			AssertType(typeof(object), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(string)), MakeResult(typeof(object))));
			AssertType(typeof(long), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(int)), MakeResult(typeof(long))));
		}
		
		[Test]
		public void Null()
		{
			AssertType(typeof(string), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(string)), MakeConstant(null)));
			AssertType(typeof(string), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeConstant(null), MakeResult(typeof(string))));
		}
		
		[Test]
		public void DynamicInArguments()
		{
			AssertType(typeof(dynamic), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(dynamic)), MakeResult(typeof(double))));
			
			AssertType(typeof(dynamic), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(double)), MakeResult(typeof(dynamic))));
		}
		
		[Test]
		public void DynamicInCondition()
		{
			AssertType(typeof(double), resolver.ResolveConditional(
				MakeResult(typeof(dynamic)), MakeResult(typeof(float)), MakeResult(typeof(double))));
		}
		
		[Test]
		public void AllDynamic()
		{
			AssertType(typeof(dynamic), resolver.ResolveConditional(
				MakeResult(typeof(dynamic)), MakeResult(typeof(dynamic)), MakeResult(typeof(dynamic))));
		}
		
		[Test]
		public void ListOfDynamicAndListOfObject()
		{
			AssertError(typeof(List<dynamic>), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(List<object>)), MakeResult(typeof(List<dynamic>))));
			
			AssertError(typeof(List<object>), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(List<dynamic>)), MakeResult(typeof(List<object>))));
		}
		
		[Test]
		public void Constant()
		{
			AssertConstant(1L, resolver.ResolveConditional(
				MakeConstant(true), MakeConstant(1), MakeConstant(2L)));
			
			AssertConstant(2L, resolver.ResolveConditional(
				MakeConstant(false), MakeConstant(1), MakeConstant(2L)));
		}
		
		[Test]
		public void NotConstantIfFalsePortionNotConstant()
		{
			AssertType(typeof(long), resolver.ResolveConditional(
				MakeConstant(true), MakeConstant(1), MakeResult(typeof(long))));
		}
	}
}
