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
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	// assign short name to the fake reflection type
	using dynamic = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.Dynamic;
	
	[TestFixture]
	public class ConditionalOperatorTests : ResolverTestBase
	{
		CSharpResolver resolver;
		
		public override void SetUp()
		{
			base.SetUp();
			resolver = new CSharpResolver(compilation);
		}
		
		void TestOperator(ResolveResult condition, ResolveResult trueExpr, ResolveResult falseExpr,
		                  Conversion conditionConv, Conversion trueConv, Conversion falseConv,
		                  Type expectedResultType)
		{
			var corr = (OperatorResolveResult)resolver.ResolveConditional(condition, trueExpr, falseExpr);
			AssertType(expectedResultType, corr);
			AssertConversion(corr.Operands[0], condition, conditionConv, "Condition Conversion");
			AssertConversion(corr.Operands[1], trueExpr, trueConv, "True Conversion");
			AssertConversion(corr.Operands[2], falseExpr, falseConv, "False Conversion");
		}
		
		[Test]
		public void PickMoreGeneralOfTheTypes()
		{
			TestOperator(MakeResult(typeof(bool)), MakeResult(typeof(string)), MakeResult(typeof(object)),
			             Conversion.IdentityConversion, Conversion.ImplicitReferenceConversion, Conversion.IdentityConversion,
			             typeof(object));
			
			TestOperator(MakeResult(typeof(bool)), MakeResult(typeof(int)), MakeResult(typeof(long)),
			             Conversion.IdentityConversion, Conversion.ImplicitNumericConversion, Conversion.IdentityConversion,
			             typeof(long));
		}
		
		[Test]
		public void StringAndNull()
		{
			ResolveResult condition = MakeResult(typeof(bool));
			ResolveResult trueExpr = MakeResult(typeof(string));
			var result = (OperatorResolveResult)resolver.ResolveConditional(
				condition, trueExpr, MakeConstant(null));
			AssertType(typeof(string), result);
			AssertConversion(result.Operands[0], condition, Conversion.IdentityConversion, "Condition Conversion");
			AssertConversion(result.Operands[1], trueExpr, Conversion.IdentityConversion, "True Conversion");
			Assert.IsTrue(result.Operands[2].IsCompileTimeConstant);
			Assert.IsNull(result.Operands[2].ConstantValue);
			Assert.AreEqual("System.String", result.Operands[2].Type.FullName);
		}
		
		[Test]
		public void NullAndString()
		{
			ResolveResult condition = MakeResult(typeof(bool));
			ResolveResult falseExpr = MakeResult(typeof(string));
			var result = (OperatorResolveResult)resolver.ResolveConditional(
				condition, MakeConstant(null), falseExpr);
			AssertType(typeof(string), result);
			AssertConversion(result.Operands[0], condition, Conversion.IdentityConversion, "Condition Conversion");
			Assert.IsTrue(result.Operands[1].IsCompileTimeConstant);
			Assert.IsNull(result.Operands[1].ConstantValue);
			Assert.AreEqual("System.String", result.Operands[1].Type.FullName);
			AssertConversion(result.Operands[2], falseExpr, Conversion.IdentityConversion, "False Conversion");
		}
		
		[Test]
		public void DynamicInArguments()
		{
			TestOperator(MakeResult(typeof(bool)), MakeResult(typeof(dynamic)), MakeResult(typeof(double)),
			             Conversion.IdentityConversion, Conversion.IdentityConversion, Conversion.BoxingConversion,
			             typeof(dynamic));
			
			TestOperator(MakeResult(typeof(bool)), MakeResult(typeof(double)), MakeResult(typeof(dynamic)),
			             Conversion.IdentityConversion, Conversion.BoxingConversion, Conversion.IdentityConversion,
			             typeof(dynamic));
		}
		
		[Test]
		public void DynamicInCondition()
		{
			TestOperator(MakeResult(typeof(dynamic)), MakeResult(typeof(float)), MakeResult(typeof(double)),
			             Conversion.ImplicitDynamicConversion, Conversion.ImplicitNumericConversion, Conversion.IdentityConversion,
			             typeof(double));
		}
		
		[Test]
		public void AllDynamic()
		{
			TestOperator(MakeResult(typeof(dynamic)), MakeResult(typeof(dynamic)), MakeResult(typeof(dynamic)),
			             Conversion.ImplicitDynamicConversion, Conversion.IdentityConversion, Conversion.IdentityConversion,
			             typeof(dynamic));
		}
		
		[Test]
		public void ListOfDynamicAndListOfObject()
		{
			AssertError(typeof(List<object>), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(List<object>)), MakeResult(typeof(List<dynamic>))));
			
			AssertError(typeof(List<dynamic>), resolver.ResolveConditional(
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
		
		[Test]
		public void ByteAndZeroLiteral()
		{
			AssertType(typeof(int), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(byte)), MakeConstant(0)));
		}
		
		[Test]
		public void ByteAndUShort()
		{
			AssertType(typeof(ushort), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(byte)), MakeResult(typeof(ushort))));
		}
		
		[Test]
		public void EnumAndZeroLiteral()
		{
			AssertType(typeof(StringComparison), resolver.ResolveConditional(
				MakeResult(typeof(bool)), MakeResult(typeof(StringComparison)), MakeConstant(0)));
		}
		
		[Test]
		public void TypeWithImplicitConversionToBool()
		{
			string program = @"struct MyBool {
	public static implicit operator bool(MyBool b) {}
	void Test() {
		var x = $this$ ? 1 : 0;
	}
}";
			Assert.AreEqual("System.Boolean", GetExpectedType(program).ReflectionName);
			Conversion c = GetConversion(program);
			Assert.IsTrue(c.IsValid);
			Assert.IsTrue(c.IsUserDefined);
			Assert.AreEqual("op_Implicit", c.Method.Name);
		}
		
		[Test]
		public void TypeWithOperatorTrue()
		{
			string program = @"struct MyBool {
	public static bool operator true(MyBool b) {}
	void Test() {
		var x = $this$ ? 1 : 0;
	}
}";
			Assert.AreEqual("System.Boolean", GetExpectedType(program).ReflectionName);
			Conversion c = GetConversion(program);
			Assert.IsTrue(c.IsValid);
			Assert.IsTrue(c.IsUserDefined);
			Assert.AreEqual("op_True", c.Method.Name);
		}
	}
}
