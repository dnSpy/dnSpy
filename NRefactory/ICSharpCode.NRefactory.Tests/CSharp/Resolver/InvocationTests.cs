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
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class InvocationTests : ResolverTestBase
	{
		[Test]
		public void MethodCallTest()
		{
			string program = @"class A {
	void Method() {
		$TargetMethod()$;
	}
	
	int TargetMethod() {
		return 3;
	}
}
";
			InvocationResolveResult result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("A.TargetMethod", result.Member.FullName);
			Assert.AreEqual("System.Int32", result.Type.ReflectionName);
		}
		
		[Test]
		public void InvalidMethodCall()
		{
			string program = @"class A {
	void Method(string b) {
		$b.ThisMethodDoesNotExistOnString(b)$;
	}
}
";
			UnknownMethodResolveResult result = Resolve<UnknownMethodResolveResult>(program);
			Assert.AreEqual("ThisMethodDoesNotExistOnString", result.MemberName);
			Assert.AreEqual("System.String", result.TargetType.FullName);
			Assert.AreEqual(1, result.Parameters.Count);
			Assert.AreEqual("b", result.Parameters[0].Name);
			Assert.AreEqual("System.String", result.Parameters[0].Type.Resolve(context).ReflectionName);
			
			Assert.AreSame(SharedTypes.UnknownType, result.Type);
		}
		
		[Test]
		public void OverriddenMethodCall()
		{
			string program = @"class A {
	void Method() {
		$new B().GetRandomNumber()$;
	}
	
	public abstract int GetRandomNumber();
}
class B : A {
	public override int GetRandomNumber() {
		return 4; // chosen by fair dice roll.
		          // guaranteed to be random
	}
}
";
			InvocationResolveResult result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("B.GetRandomNumber", result.Member.FullName);
		}
		
		[Test]
		public void OverriddenMethodCall2()
		{
			string program = @"class A {
	void Method() {
		$new B().GetRandomNumber(""x"", this)$;
	}
	
	public abstract int GetRandomNumber(string a, A b);
}
class B : A {
	public override int GetRandomNumber(string b, A a) {
		return 4;
	}
}
";
			InvocationResolveResult result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("B.GetRandomNumber", result.Member.FullName);
		}
		
		[Test]
		public void ThisMethodCallTest()
		{
			string program = @"class A {
	void Method() {
		$this.TargetMethod()$;
	}
	
	int TargetMethod() {
		return 3;
	}
}
";
			InvocationResolveResult result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("A.TargetMethod", result.Member.FullName);
			Assert.AreEqual("System.Int32", result.Type.ReflectionName);
		}
		
		[Test]
		public void VoidTest()
		{
			string program = @"using System;
class A {
	void TestMethod() {
		$TestMethod()$;
	}
}
";
			Assert.AreEqual("System.Void", Resolve(program).Type.ReflectionName);
		}
		
		[Test]
		public void EventCallTest()
		{
			string program = @"using System;
class A {
	void Method() {
		$TestEvent(this, EventArgs.Empty)$;
	}
	
	public event EventHandler TestEvent;
}
";
			Assert.AreEqual("System.Void", Resolve(program).Type.ReflectionName);
		}
		
		[Test]
		public void DelegateCallTest()
		{
			string program = @"using System; using System.Reflection;
class A {
	void Method(ModuleResolveEventHandler eh) {
		$eh(this, new ResolveEventArgs())$;
	}
}
";
			Assert.AreEqual("System.Reflection.Module", Resolve(program).Type.ReflectionName);
		}
		
		[Test]
		public void DelegateReturnedFromMethodCallTest()
		{
			string program = @"using System;
class A {
	void Method() {
		$GetHandler()(abc)$;
	}
	abstract Predicate<string> GetHandler();
}
";
			Assert.AreEqual("System.Boolean", Resolve(program).Type.ReflectionName);
		}
		
		/* TODO
		[Test]
		public void MethodGroupResolveTest()
		{
			string program = @"class A {
	void Method() {
		
	}
	
	void TargetMethod(int a) { }
	void TargetMethod<T>(T a) { }
}
";
			MethodGroupResolveResult result = Resolve<MethodGroupResolveResult>(program, "TargetMethod", 3);
			Assert.AreEqual("TargetMethod", result.Name);
			Assert.AreEqual(2, result.Methods.Count);
			
			result = Resolve<MethodGroupResolveResult>(program, "TargetMethod<string>", 3);
			Assert.AreEqual("TargetMethod", result.Name);
			Assert.AreEqual(1, result.Methods[0].Count);
			Assert.AreEqual("System.String", result.GetMethodIfSingleOverload().Parameters[0].ReturnType.FullyQualifiedName);
		}
		 */
		
		[Test]
		public void TestOverloadingByRef()
		{
			string program = @"using System;
class Program {
	public static void Main() {
		int a = 42;
		T(a);
		T(ref a);
	}
	static void T(int x) {}
	static void T(ref int y) {}
}";
			
			InvocationResolveResult mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("T(a)", "$T(a)$"));
			Assert.IsFalse(mrr.Member.Parameters[0].IsRef);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("T(ref a)", "$T(ref a)$"));
			Assert.IsTrue(mrr.Member.Parameters[0].IsRef);
		}
		
		[Test]
		public void AddedOverload()
		{
			string program = @"class BaseClass {
	static void Main(DerivedClass d) {
		$d.Test(3)$;
	}
	public void Test(int a) { }
}
class DerivedClass : BaseClass {
	public void Test(object a) { }
}";
			InvocationResolveResult mrr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("DerivedClass.Test", mrr.Member.FullName);
		}
		
		[Test]
		public void AddedOverloadOnInterface()
		{
			string program = @"
interface IBase { void Method(int a); }
interface IDerived { void Method(object a); }
class Test {
	static void Main(IDerived d) {
		$d.Method(3)$;
	}
}";
			InvocationResolveResult mrr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("IDerived.Method", mrr.Member.FullName);
		}
		
		[Test]
		public void AddedNonApplicableOverload()
		{
			string program = @"class BaseClass {
	static void Main(DerivedClass d) {
		$d.Test(3)$;
	}
	public void Test(int a) { }
}
class DerivedClass : BaseClass {
	public void Test(string a) { }
}";
			InvocationResolveResult mrr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("BaseClass.Test", mrr.Member.FullName);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("(3)", "(\"3\")"));
			Assert.AreEqual("DerivedClass.Test", mrr.Member.FullName);
		}
		
		[Test]
		public void OverrideShadowed()
		{
			string program = @"using System;
class BaseClass {
	static void Main() {
		$new DerivedClass().Test(3)$;
	}
	public virtual void Test(int a) { }
}
class MiddleClass : BaseClass {
	public void Test(object a) { }
}
class DerivedClass : MiddleClass {
	public override void Test(int a) { }
}";
			
			InvocationResolveResult mrr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("MiddleClass.Test", mrr.Member.FullName);
		}
		
		[Test]
		public void SubstituteClassAndMethodTypeParametersAtOnce()
		{
			string program = @"class C<X> { static void M<T>(X a, T b) { $C<T>.M(b, a)$; } }";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			
			var m = (SpecializedMethod)rr.Member;
			Assert.AreEqual("X", m.TypeArguments.Single().Name);
			Assert.AreEqual("T", m.Parameters[0].Type.Resolve(context).Name);
			Assert.AreEqual("X", m.Parameters[1].Type.Resolve(context).Name);
		}
		
		[Test]
		public void MemberHiddenOnOneAccessPath()
		{
			// If a member is hidden in any access path, it is hidden in all access paths
			string program = @"
interface IBase { int F { get; } }
interface ILeft: IBase { new int F { get; } }
interface IRight: IBase { void G(); }
interface IDerived: ILeft, IRight {}
class A {
   void Test(IDerived d) { var a = $d.F$; }
}";
			var rr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("ILeft.F", rr.Member.FullName);
		}
		
		[Test]
		public void PropertyClashesWithMethod()
		{
			string program = @"
interface IList { int Count { get; set; } }
interface ICounter { void Count(int i); }
interface IListCounter: IList, ICounter {}
class A {
 	void Test(IListCounter x) { var a = $x.Count$; }
}";
			var rr = Resolve<MethodGroupResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("ICounter.Count", rr.Methods.Single().FullName);
		}
		
		[Test]
		public void OverloadAmbiguousWithMethodInTwoInterfaces()
		{
			string program = @"
interface ILeft { void Method(); }
interface IRight { void Method(); }
interface IBoth : ILeft, IRight {}
class A {
 	void Test(IBoth x) { $x.Method()$; }
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsTrue(rr.IsError);
			Assert.AreEqual(OverloadResolutionErrors.AmbiguousMatch, rr.OverloadResolutionErrors);
		}
		
		[Test]
		public void AddedOverloadInOneInterfaceAndBetterOverloadInOtherInterface1()
		{
			string program = @"
interface IBase { void Method(int x); }
interface ILeft : IBase { void Method(object x); }
interface IRight { void Method(int x); }
interface IBoth : ILeft, IRight {}
class A {
 	void Test(IBoth x) { $x.Method(1)$; }
}";
			// IBase.Method is "hidden" because ILeft.Method is also applicable,
			// so IRight.Method is unambiguously the chosen overload.
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("IRight.Method", rr.Member.FullName);
		}
		
		[Test]
		public void AddedOverloadInOneInterfaceAndBetterOverloadInOtherInterface2()
		{
			// repeat the above test with Left/Right swapped to make sure we aren't order-sensitive
			string program = @"
interface IBase { void Method(int x); }
interface ILeft : IBase { void Method(object x); }
interface IRight { void Method(int x); }
interface IBoth : IRight, ILeft {}
class A {
 	void Test(IBoth x) { $x.Method(1)$; }
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("IRight.Method", rr.Member.FullName);
		}
		
		[Test]
		public void AddedOverloadHidesCommonBaseMethod_Generic1()
		{
			string program = @"
interface IBase<T> {
	void Method(int x);
}
interface ILeft : IBase<int> { void Method(object x); }
interface IRight : IBase<int> { }
interface IBoth : ILeft, IRight {}
class A {
	void Test(IBoth x) { $x.Method(1)$; }
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("ILeft.Method", rr.Member.FullName);
		}
		
		[Test]
		public void AddedOverloadHidesCommonBaseMethod_Generic2()
		{
			string program = @"
interface IBase<T> {
	void Method(int x);
}
interface ILeft : IBase<int> { void Method(object x); }
interface IRight : IBase<int> { }
interface IBoth : ILeft, IRight {}
class A {
	void Test(IBoth x) { $x.Method(1)$; }
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("ILeft.Method", rr.Member.FullName);
		}
		
		[Test]
		public void AddedOverloadDoesNotHideCommonBaseMethodWithDifferentTypeArgument1()
		{
			string program = @"
interface IBase<T> {
	void Method(int x);
}
interface ILeft : IBase<int> { void Method(object x); }
interface IRight : IBase<long> { }
interface IBoth : IRight, ILeft {}
class A {
	void Test(IBoth x) { $x.Method(1)$; }
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("IBase`1[[System.Int64]]", rr.Member.DeclaringType.ReflectionName);
		}
		
		[Test]
		public void AddedOverloadDoesNotHideCommonBaseMethodWithDifferentTypeArgument2()
		{
			string program = @"
interface IBase<T> {
	void Method(int x);
}
interface ILeft : IBase<int> { void Method(object x); }
interface IRight : IBase<long> { }
interface IBoth : IRight, ILeft {}
class A {
	void Test(IBoth x) { $x.Method(1)$; }
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("IBase`1[[System.Int64]]", rr.Member.DeclaringType.ReflectionName);
		}
		
		[Test]
		public void AmbiguityBetweenMemberAndMethodIsNotAnError()
		{
			string program = @"
interface ILeft { void Method(object x); }
interface IRight { Action<object> Method { get; } }
interface IBoth : ILeft, IRight {}
class A {
	void Test(IBoth x) { $x.Method(null)$; }
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("ILeft.Method", rr.Member.FullName);
		}
	}
}
