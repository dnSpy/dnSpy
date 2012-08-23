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
using System.Linq;
using NUnit.Framework;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	using dynamic = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.Dynamic;
	using C = Conversion;
	
	[TestFixture]
	public class ExplicitConversionsTest : ResolverTestBase
	{
		CSharpConversions conversions;
		
		public override void SetUp()
		{
			base.SetUp();
			conversions = new CSharpConversions(compilation);
		}
		
		Conversion ExplicitConversion(Type from, Type to)
		{
			IType from2 = compilation.FindType(from);
			IType to2 = compilation.FindType(to);
			return conversions.ExplicitConversion(from2, to2);
		}
		
		[Test]
		public void PointerConversion()
		{
			Assert.AreEqual(C.ExplicitPointerConversion, ExplicitConversion(typeof(int*), typeof(short)));
			Assert.AreEqual(C.ExplicitPointerConversion, ExplicitConversion(typeof(short), typeof(void*)));
			
			Assert.AreEqual(C.ExplicitPointerConversion, ExplicitConversion(typeof(void*), typeof(int*)));
			Assert.AreEqual(C.ExplicitPointerConversion, ExplicitConversion(typeof(long*), typeof(byte*)));
		}
		
		[Test]
		public void ConversionFromDynamic()
		{
			// Explicit dynamic conversion is for resolve results only;
			// otherwise it's an explicit reference / unboxing conversion
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(dynamic), typeof(string)));
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(dynamic), typeof(int)));
			
			var dynamicRR = new ResolveResult(SpecialType.Dynamic);
			Assert.AreEqual(C.ExplicitDynamicConversion, conversions.ExplicitConversion(dynamicRR, compilation.FindType(typeof(string))));
			Assert.AreEqual(C.ExplicitDynamicConversion, conversions.ExplicitConversion(dynamicRR, compilation.FindType(typeof(int))));
		}
		
		[Test]
		public void NumericConversions()
		{
			Assert.AreEqual(C.ExplicitNumericConversion, ExplicitConversion(typeof(sbyte), typeof(uint)));
			Assert.AreEqual(C.ExplicitNumericConversion, ExplicitConversion(typeof(sbyte), typeof(char)));
			Assert.AreEqual(C.ExplicitNumericConversion, ExplicitConversion(typeof(byte), typeof(char)));
			Assert.AreEqual(C.ExplicitNumericConversion, ExplicitConversion(typeof(byte), typeof(sbyte)));
			// if an implicit conversion exists, ExplicitConversion() should return that
			Assert.AreEqual(C.ImplicitNumericConversion, ExplicitConversion(typeof(byte), typeof(int)));
			Assert.AreEqual(C.ExplicitNumericConversion, ExplicitConversion(typeof(double), typeof(float)));
			Assert.AreEqual(C.ExplicitNumericConversion, ExplicitConversion(typeof(double), typeof(decimal)));
			Assert.AreEqual(C.ExplicitNumericConversion, ExplicitConversion(typeof(decimal), typeof(double)));
			Assert.AreEqual(C.ImplicitNumericConversion, ExplicitConversion(typeof(int), typeof(decimal)));
			
			Assert.AreEqual(C.None, ExplicitConversion(typeof(bool), typeof(int)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(int), typeof(bool)));
		}
		
		[Test]
		public void EnumerationConversions()
		{
			var explicitEnumerationConversion = C.EnumerationConversion(false, false);
			Assert.AreEqual(explicitEnumerationConversion, ExplicitConversion(typeof(sbyte), typeof(StringComparison)));
			Assert.AreEqual(explicitEnumerationConversion, ExplicitConversion(typeof(char), typeof(StringComparison)));
			Assert.AreEqual(explicitEnumerationConversion, ExplicitConversion(typeof(int), typeof(StringComparison)));
			Assert.AreEqual(explicitEnumerationConversion, ExplicitConversion(typeof(decimal), typeof(StringComparison)));
			Assert.AreEqual(explicitEnumerationConversion, ExplicitConversion(typeof(StringComparison), typeof(char)));
			Assert.AreEqual(explicitEnumerationConversion, ExplicitConversion(typeof(StringComparison), typeof(int)));
			Assert.AreEqual(explicitEnumerationConversion, ExplicitConversion(typeof(StringComparison), typeof(decimal)));
			Assert.AreEqual(explicitEnumerationConversion, ExplicitConversion(typeof(StringComparison), typeof(StringSplitOptions)));
		}
		
		[Test]
		public void NullableConversion_BasedOnIdentityConversion()
		{
			Assert.AreEqual(C.IdentityConversion, ExplicitConversion(typeof(ArraySegment<dynamic>?), typeof(ArraySegment<object>?)));
			Assert.AreEqual(C.ImplicitNullableConversion, ExplicitConversion(typeof(ArraySegment<dynamic>), typeof(ArraySegment<object>?)));
			Assert.AreEqual(C.ExplicitNullableConversion, ExplicitConversion(typeof(ArraySegment<dynamic>?), typeof(ArraySegment<object>)));
		}
		
		[Test]
		public void NullableConversion_BasedOnImplicitNumericConversion()
		{
			Assert.AreEqual(C.ImplicitLiftedNumericConversion, ExplicitConversion(typeof(int?), typeof(long?)));
			Assert.AreEqual(C.ImplicitLiftedNumericConversion, ExplicitConversion(typeof(int), typeof(long?)));
			Assert.AreEqual(C.ExplicitLiftedNumericConversion, ExplicitConversion(typeof(int?), typeof(long)));
		}
		
		[Test]
		public void NullableConversion_BasedOnImplicitEnumerationConversion()
		{
			ResolveResult zero = new ConstantResolveResult(compilation.FindType(KnownTypeCode.Int32), 0);
			ResolveResult one = new ConstantResolveResult(compilation.FindType(KnownTypeCode.Int32), 1);
			Assert.AreEqual(C.EnumerationConversion(true, true), conversions.ExplicitConversion(zero, compilation.FindType(typeof(StringComparison?))));
			Assert.AreEqual(C.EnumerationConversion(false, true), conversions.ExplicitConversion(one, compilation.FindType(typeof(StringComparison?))));
		}
		
		[Test]
		public void NullableConversion_BasedOnExplicitNumericConversion()
		{
			Assert.AreEqual(C.ExplicitLiftedNumericConversion, ExplicitConversion(typeof(int?), typeof(short?)));
			Assert.AreEqual(C.ExplicitLiftedNumericConversion, ExplicitConversion(typeof(int), typeof(short?)));
			Assert.AreEqual(C.ExplicitLiftedNumericConversion, ExplicitConversion(typeof(int?), typeof(short)));
		}
		
		[Test]
		public void NullableConversion_BasedOnExplicitEnumerationConversion()
		{
			C c = C.EnumerationConversion(false, true); // c = explicit lifted enumeration conversion
			Assert.AreEqual(c, ExplicitConversion(typeof(int?), typeof(StringComparison?)));
			Assert.AreEqual(c, ExplicitConversion(typeof(int), typeof(StringComparison?)));
			Assert.AreEqual(c, ExplicitConversion(typeof(int?), typeof(StringComparison)));
			
			Assert.AreEqual(c, ExplicitConversion(typeof(StringComparison?), typeof(int?)));
			Assert.AreEqual(c, ExplicitConversion(typeof(StringComparison), typeof(int?)));
			Assert.AreEqual(c, ExplicitConversion(typeof(StringComparison?), typeof(int)));
			
			Assert.AreEqual(c, ExplicitConversion(typeof(StringComparison?), typeof(StringSplitOptions?)));
			Assert.AreEqual(c, ExplicitConversion(typeof(StringComparison), typeof(StringSplitOptions?)));
			Assert.AreEqual(c, ExplicitConversion(typeof(StringComparison?), typeof(StringSplitOptions)));
		}
		
		[Test]
		public void ExplicitReferenceConversion_SealedClass()
		{
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(object), typeof(string)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<char>), typeof(string)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(IEnumerable<int>), typeof(string)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(IEnumerable<object>), typeof(string)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ExplicitConversion(typeof(string), typeof(IEnumerable<char>)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(string), typeof(IEnumerable<int>)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(string), typeof(IEnumerable<object>)));
		}
		
		[Test]
		public void ExplicitReferenceConversion_NonSealedClass()
		{
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(object), typeof(List<string>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<object>), typeof(List<string>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<string>), typeof(List<string>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<int>), typeof(List<string>)));
			
			Assert.AreEqual(C.ImplicitReferenceConversion, ExplicitConversion(typeof(List<string>), typeof(IEnumerable<object>)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ExplicitConversion(typeof(List<string>), typeof(IEnumerable<string>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(List<string>), typeof(IEnumerable<int>)));
			
			Assert.AreEqual(C.None, ExplicitConversion(typeof(List<string>), typeof(List<object>)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(List<string>), typeof(List<int>)));
		}
		
		[Test]
		public void ExplicitReferenceConversion_Interfaces()
		{
			Assert.AreEqual(C.ImplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<string>), typeof(IEnumerable<object>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<int>), typeof(IEnumerable<object>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<object>), typeof(IEnumerable<string>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<object>), typeof(IEnumerable<int>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<object>), typeof(IConvertible)));
		}
		
		[Test]
		public void ExplicitReferenceConversion_Arrays()
		{
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(object[]), typeof(string[])));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(dynamic[]), typeof(string[])));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(object[]), typeof(object[,])));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(object[]), typeof(int[])));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(short[]), typeof(int[])));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(Array), typeof(int[])));
		}
		
		[Test]
		public void ExplicitReferenceConversion_InterfaceToArray()
		{
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(ICloneable), typeof(int[])));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<string>), typeof(string[])));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<object>), typeof(string[])));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<string>), typeof(object[])));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<string>), typeof(dynamic[])));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(IEnumerable<int>), typeof(int[])));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(IEnumerable<string>), typeof(object[,])));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(IEnumerable<short>), typeof(object[])));
		}
		
		[Test]
		public void ExplicitReferenceConversion_ArrayToInterface()
		{
			Assert.AreEqual(C.ImplicitReferenceConversion, ExplicitConversion(typeof(int[]), typeof(ICloneable)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ExplicitConversion(typeof(string[]), typeof(IEnumerable<string>)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ExplicitConversion(typeof(string[]), typeof(IEnumerable<object>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(object[]), typeof(IEnumerable<string>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(dynamic[]), typeof(IEnumerable<string>)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ExplicitConversion(typeof(int[]), typeof(IEnumerable<int>)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(object[,]), typeof(IEnumerable<string>)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(object[]), typeof(IEnumerable<short>)));
		}
		
		[Test]
		public void ExplicitReferenceConversion_Delegates()
		{
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(MulticastDelegate), typeof(Action)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(Delegate), typeof(Action)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(ICloneable), typeof(Action)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(System.Threading.ThreadStart), typeof(Action)));
		}
		
		[Test]
		public void ExplicitReferenceConversion_GenericDelegates()
		{
			Assert.AreEqual(C.ImplicitReferenceConversion, ExplicitConversion(typeof(Action<object>), typeof(Action<string>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(Action<string>), typeof(Action<object>)));
			
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(Func<object>), typeof(Func<string>)));
			Assert.AreEqual(C.ImplicitReferenceConversion, ExplicitConversion(typeof(Func<string>), typeof(Func<object>)));
			
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(Action<IFormattable>), typeof(Action<IConvertible>)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(Action<IFormattable>), typeof(Action<int>)));
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(Action<string>), typeof(Action<IEnumerable<int>>)));
			
			Assert.AreEqual(C.ExplicitReferenceConversion, ExplicitConversion(typeof(Func<IFormattable>), typeof(Func<IConvertible>)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(Func<IFormattable>), typeof(Func<int>)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(Func<string>), typeof(Func<IEnumerable<int>>)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(Func<string>), typeof(Func<IEnumerable<int>>)));
		}
		
		[Test]
		public void UnboxingConversion()
		{
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(object), typeof(int)));
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(object), typeof(decimal)));
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(ValueType), typeof(int)));
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(IFormattable), typeof(int)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(IEnumerable<object>), typeof(int)));
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(Enum), typeof(StringComparison)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(Enum), typeof(int)));
		}
		
		[Test]
		public void LiftedUnboxingConversion()
		{
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(object), typeof(int?)));
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(object), typeof(decimal?)));
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(ValueType), typeof(int?)));
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(IFormattable), typeof(int?)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(IEnumerable<object>), typeof(int?)));
			Assert.AreEqual(C.UnboxingConversion, ExplicitConversion(typeof(Enum), typeof(StringComparison?)));
			Assert.AreEqual(C.None, ExplicitConversion(typeof(Enum), typeof(int?)));
		}
		Conversion ResolveCast(string program)
		{
			return Resolve<ConversionResolveResult>(program).Conversion;
		}
		
		[Test]
		public void ObjectToTypeParameter()
		{
			string program = @"using System;
class Test {
	public void M<T>(object o) {
		T t = $(T)o$;
	}
}";
			Assert.AreEqual(C.UnboxingConversion, ResolveCast(program));
		}
		
		[Test]
		public void UnrelatedClassToTypeParameter()
		{
			string program = @"using System;
class Test {
	public void M<T>(string o) {
		T t = $(T)o$;
	}
}";
			Assert.AreEqual(C.None, ResolveCast(program));
		}
		
		[Test]
		public void IntefaceToTypeParameter()
		{
			string program = @"using System;
class Test {
	public void M<T>(IDisposable o) {
		T t = $(T)o$;
	}
}";
			Assert.AreEqual(C.UnboxingConversion, ResolveCast(program));
		}
		
		[Test]
		public void TypeParameterToInterface()
		{
			string program = @"using System;
class Test {
	public void M<T>(T t) {
		IDisposable d = $(IDisposable)t$;
	}
}";
			Assert.AreEqual(C.BoxingConversion, ResolveCast(program));
		}
		
		[Test]
		public void ValueTypeToTypeParameter()
		{
			string program = @"using System;
class Test {
	public void M<T>(ValueType o) where T : struct {
		T t = $(T)o$;
	}
}";
			Assert.AreEqual(C.UnboxingConversion, ResolveCast(program));
		}
		
		[Test]
		public void InvalidTypeParameterConversion()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T t) {
		U u = $(U)t$;
	}
}";
			Assert.AreEqual(C.None, ResolveCast(program));
		}
		
		[Test]
		public void TypeParameterConversion1()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T t) where T : U {
		U u = $(U)t$;
	}
}";
			Assert.AreEqual(C.BoxingConversion, ResolveCast(program));
		}
		
		[Test]
		public void TypeParameterConversion1Array()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T[] t) where T : U {
		U[] u = $(U[])t$;
	}
}";
			Assert.AreEqual(C.None, ResolveCast(program));
		}
		
		[Test]
		public void TypeParameterConversion2()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T t) where U : T {
		U u = $(U)t$;
	}
}";
			Assert.AreEqual(C.UnboxingConversion, ResolveCast(program));
		}
		
		[Test]
		public void TypeParameterConversion2Array()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T[] t) where U : T {
		U[] u = $(U[])t$;
	}
}";
			Assert.AreEqual(C.None, ResolveCast(program));
		}
		
		[Test]
		public void ImplicitTypeParameterConversionWithClassConstraint()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T t) where T : class where U : class, T {
		U u = $(U)t$;
	}
}";
			Assert.AreEqual(C.ExplicitReferenceConversion, ResolveCast(program));
		}
		
		[Test]
		public void ImplicitTypeParameterArrayConversionWithClassConstraint()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T[] t) where T : class where U : class, T {
		U[] u = $(U[])t$;
	}
}";
			Assert.AreEqual(C.ExplicitReferenceConversion, ResolveCast(program));
		}
		
		[Test]
		public void ImplicitTypeParameterConversionWithClassConstraintOnlyOnT()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T t) where U : class, T {
		U u = $(U)t$;
	}
}";
			Assert.AreEqual(C.ExplicitReferenceConversion, ResolveCast(program));
		}
		
		[Test]
		public void ImplicitTypeParameterArrayConversionWithClassConstraintOnlyOnT()
		{
			string program = @"using System;
class Test {
	public void M<T, U>(T[] t) where U : class, T {
		U[] u = $(U[])t$;
	}
}";
			Assert.AreEqual(C.ExplicitReferenceConversion, ResolveCast(program));
		}
		
		[Test]
		public void SimpleUserDefinedConversion()
		{
			var rr = Resolve<ConversionResolveResult>(@"
class C1 {}
class C2 {
	public static explicit operator C1(C2 c2) {
		return null;
	}
}
class C {
	public void M() {
		var c2 = new C2();
		C1 c1 = $(C1)c2$;
	}
}");
			Assert.IsTrue(rr.Conversion.IsValid);
			Assert.IsTrue(rr.Conversion.IsUserDefined);
			Assert.AreEqual("op_Explicit", rr.Conversion.Method.Name);
		}
		
		[Test]
		public void ExplicitReferenceConversionFollowedByUserDefinedConversion()
		{
			var rr = Resolve<ConversionResolveResult>(@"
		class B {}
		class S : B {}
		class T {
			public static explicit operator T(S s) { return null; }
		}
		class Test {
			void Run(B b) {
				T t = $(T)b$;
			}
		}");
			Assert.IsTrue(rr.Conversion.IsValid);
			Assert.IsTrue(rr.Conversion.IsUserDefined);
			Assert.AreEqual("B", rr.Input.Type.Name);
		}
		
		[Test]
		[Ignore("Not implemented yet.")]
		public void BothDirectConversionAndBaseClassConversionAvailable()
		{
			var rr = Resolve<ConversionResolveResult>(@"
		class B {}
		class S : B {}
		class T {
			public static explicit operator T(S s) { return null; }
			public static explicit operator T(B b) { return null; }
		}
		class Test {
			void Run(B b) {
				T t = $(T)b$;
			}
		}");
			Assert.IsTrue(rr.Conversion.IsValid);
			Assert.IsTrue(rr.Conversion.IsUserDefined);
			Assert.AreEqual("b", rr.Conversion.Method.Parameters.Single().Name);
		}
	}
}
