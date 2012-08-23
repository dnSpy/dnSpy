//
// ParameterCouldBeDeclaredWithBaseTypeTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class ParameterCanBeDemotedTests : InspectionActionTestBase
	{
		
		[Test]
		public void BasicTest()
		{
			var input = @"
class A
{
	public virtual void Foo() {}
}
class B : A
{
	public virtual void Bar() {}
}
class C
{
	void F(B b)
	{
		b.Foo();
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues [0];
			Assert.AreEqual(1, issue.Actions.Count);

			CheckFix(context, issues [0], @"
class A
{
	public virtual void Foo() {}
}
class B : A
{
	public virtual void Bar() {}
}
class C
{
	void F(A b)
	{
		b.Foo();
	}
}");
		}
		
		[Test]
		public void IgnoresUnusedParameters()
		{
			var input = @"
class A
{
	void F(A a1)
	{
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void IgnoresDirectionalParameters()
		{
			var input = @"
interface IA
{
}
class A : IA
{
	void F(out A a1)
	{
		object.Equals(a1, null);
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}

		[Test]
		public void IgnoresOverrides()
		{
			var input = @"
interface IA
{
	void Foo();
}
class B : IA
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class TestBase
{
	public void F(B b) {
		b.Foo();
		b.Bar();
	}
}
class TestClass : TestBase
{
	public override void F(B b)
	{
		b.Foo();
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void IgnoresOverridables()
		{
			var input = @"
interface IA
{
	void Foo();
}
class B : IA
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class TestClass
{
	public virtual void F(B b)
	{
		b.Foo();
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void HandlesNeededProperties()
		{
			var input = @"
interface IA
{
	void Foo(string s);
}
class B : IA
{
	public virtual void Foo(string s) {}
	public string Property { get; }
}
class TestClass
{
	public void F(B b)
	{
		b.Foo(b.Property);
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void InterfaceTest()
		{
			var input = @"
interface IA
{
	void Foo();
}
class B : IA
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class C
{
	void F(B b)
	{
		b.Foo();
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues [0];
			Assert.AreEqual(1, issue.Actions.Count);
			
			CheckFix(context, issues [0], @"
interface IA
{
	void Foo();
}
class B : IA
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class C
{
	void F(IA b)
	{
		b.Foo();
	}
}");
		}
		
		[Test]
		public void RespectsExpectedTypeInIfStatement()
		{
			var input = @"
class C
{
	void F (bool b, bool c)
	{
		if (b && c)
			return;
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void MultipleInterfaceTest()
		{
			var input = @"
interface IA1
{
	void Foo();
}
interface IA2
{
	void Bar();
}
class B : IA1, IA2
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class C : B {}
class Test
{
	void F(C c)
	{
		c.Foo();
		c.Bar();
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues [0];
			Assert.AreEqual(1, issue.Actions.Count);

			CheckFix(context, issues [0], @"
interface IA1
{
	void Foo();
}
interface IA2
{
	void Bar();
}
class B : IA1, IA2
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class C : B {}
class Test
{
	void F(B c)
	{
		c.Foo();
		c.Bar();
	}
}");
		}

		string baseInput = @"
interface IA
{
	void Foo();
}
interface IB : IA
{
	void Bar();
}
interface IC : IA
{
	new void Foo();
	void Baz();
}
class D : IB
{
	public void Foo() {}
	public void Bar() {}
}
class E : D, IC
{
	public void Baz() {}
	void IC.Foo() {}
}";
		
		[Test]
		public void FindsTopInterface()
		{
			var input = baseInput + @"
class Test
{
	void F(E e)
	{
		e.Foo();
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues [0];
			Assert.AreEqual(4, issue.Actions.Count);
			
			CheckFix(context, issues [0], baseInput + @"
class Test
{
	void F(IA e)
	{
		e.Foo();
	}
}");
		}
		
		[Test]
		public void DoesNotChangeOverload()
		{
			var input = baseInput + @"
class Test
{
	void F(IB b)
	{
		Bar (b);
	}
	
	void Bar (IA a)
	{
	}

	void Bar (IB b)
	{
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void AssignmentToExplicitlyTypedVariable()
		{
			var input = baseInput + @"
class Test
{
	void F(IB b)
	{
		IB b2;
		b2 = b;
		object.Equals(b, b2);
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void GenericMethod()
		{
			var input = baseInput + @"
class Test
{
	void F(IB b)
	{
		Generic (b);
	}

	void Generic<T> (T arg) where T : IA
	{
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues [0];
			Assert.AreEqual(1, issue.Actions.Count);
			
			CheckFix(context, issues [0], baseInput + @"
class Test
{
	void F(IA b)
	{
		Generic (b);
	}

	void Generic<T> (T arg) where T : IA
	{
	}
}");
		}

		[Test]
		public void VariableDeclarationWithTypeInference()
		{
			var input = baseInput + @"
class Test
{
	void Foo (IB b)
	{
		var b2 = b;
		Foo (b2);
	}

	void Foo (IA a)
	{
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues [0];
			Assert.AreEqual(1, issue.Actions.Count);
			
			CheckFix(context, issues [0], baseInput + @"
class Test
{
	void Foo (IA b)
	{
		var b2 = b;
		Foo (b2);
	}

	void Foo (IA a)
	{
	}
}");
		}

		[Test]
		public void RespectsOutgoingCallsTypeRestrictions()
		{
			var input = baseInput + @"
class Test
{
	void F(E e)
	{
		e.Foo();
		DemandType(e);
	}

	void DemandType(D d)
	{
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues [0];
			Assert.AreEqual(1, issue.Actions.Count);
			
			CheckFix(context, issues [0], baseInput + @"
class Test
{
	void F(D e)
	{
		e.Foo();
		DemandType(e);
	}

	void DemandType(D d)
	{
	}
}");
		}
		
		[Test]
		public void AccountsForNonInvocationMethodGroupUsageInMethodCall()
		{
			var input = @"
delegate void FooDelegate (string s);
interface IBase
{
	void Bar();
}
interface IDerived : IBase
{
	void Foo(string s);
}
class TestClass
{
	public void Bar (IDerived derived)
	{
		derived.Bar();
		Baz (derived.Foo);
	}

	void Baz (FooDelegate fd)
	{
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void AccountsForNonInvocationMethodGroupUsageInVariableDeclaration()
		{
			var input = @"
delegate void FooDelegate (string s);
interface IBase
{
	void Bar();
}
interface IDerived : IBase
{
	void Foo(string s);
}
class TestClass
{
	public void Bar (IDerived derived)
	{
		derived.Bar();
		FooDelegate d = derived.Foo;
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void AccountsForNonInvocationMethodGroupUsageInAssignmentExpression()
		{
			var input = @"
delegate void FooDelegate (string s);
interface IBase
{
	void Bar();
}
interface IDerived : IBase
{
	void Foo(string s);
}
class TestClass
{
	public void Bar (IDerived derived)
	{
		derived.Bar();
		FooDelegate d;
		d = derived.Foo;
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void AccountsForIndexers()
		{
			var input = @"
class TestClass
{
	void Write(string[] s)
	{
		object.Equals(s, s);
		var element = s[1];
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues[0];
			// Suggested types: IList<T> and IReadOnlyList<T>
			Assert.AreEqual(2, issue.Actions.Count);

			CheckFix(context, issues [0], @"
class TestClass
{
	void Write(System.Collections.Generic.IList<string> s)
	{
		object.Equals(s, s);
		var element = s[1];
	}
}");
		}
		
		[Test]
		public void AccountsForArrays()
		{
			var input = @"
class TestClass
{
	void Write(string[] s)
	{
		var i = s.Length;
		SetValue (out s[1]);
	}

	void SetValue (out string s)
	{
	} 
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void IgnoresImplicitInterfaceImplementations()
		{
			var input = @"
interface IHasFoo
{
	void Foo (string s);
}
class TestClass : IHasFoo
{
	public void Foo(string s)
	{
		object o = s;
	} 
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}

		[Test]
		public void IgnoresEnumParameters()
		{
			var input = @"
enum ApplicableValues
{
	None,
	Some
}
class TestClass
{
	public void Foo(ApplicableValues av)
	{
		object o = av;
	} 
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void CallToOverriddenMember()
		{
			var input = @"
class TestBase
{
	public virtual void Foo()
	{
	}
}
class Test : TestBase
{
	void F (Test t)
	{
		t.Foo();
	}
	
	public override void Foo()
	{
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues[0];
			Assert.AreEqual(1, issue.Actions.Count);
			
			CheckFix(context, issue, @"
class TestBase
{
	public virtual void Foo()
	{
	}
}
class Test : TestBase
{
	void F (TestBase t)
	{
		t.Foo();
	}
	
	public override void Foo()
	{
	}
}");
		}
		
		[Test]
		public void CallToShadowingMember()
		{
			var input = @"
class TestBase
{
	public virtual void Foo()
	{
	}
}
class Test : TestBase
{
	void F (Test t)
	{
		t.Foo();
	}
	
	public new void Foo()
	{
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void CallToShadowingMember2()
		{
			var input = @"
class TestBaseBase
{
	public virtual void Foo()
	{
	}
}
class TestBase : TestBaseBase
{
	protected virtual new void Foo()
	{
	}
}
class Test : TestBase
{
	void F (Test t)
	{
		t.Foo();
	}
	
	public override void Foo()
	{
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues[0];
			Assert.AreEqual(1, issue.Actions.Count);
			
			CheckFix(context, issue, @"
class TestBaseBase
{
	public virtual void Foo()
	{
	}
}
class TestBase : TestBaseBase
{
	protected virtual new void Foo()
	{
	}
}
class Test : TestBase
{
	void F (TestBase t)
	{
		t.Foo();
	}
	
	public override void Foo()
	{
	}
}");
		}
		
		[Test]
		public void CallToShadowingMemberWithBaseInterface()
		{
			var input = @"
interface IFoo
{
	void Foo();
}
class TestBaseBase : IFoo
{
	public virtual void Foo()
	{
	}
}
class TestBase : TestBaseBase
{
	protected virtual new void Foo()
	{
	}
}
class Test : TestBase
{
	void F (Test t)
	{
		t.Foo();
	}
	
	protected override void Foo()
	{
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ParameterCanBeDemotedIssue(false), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues[0];
			Assert.AreEqual(1, issue.Actions.Count);
			
			CheckFix(context, issue, @"
interface IFoo
{
	void Foo();
}
class TestBaseBase : IFoo
{
	public virtual void Foo()
	{
	}
}
class TestBase : TestBaseBase
{
	protected virtual new void Foo()
	{
	}
}
class Test : TestBase
{
	void F (TestBase t)
	{
		t.Foo();
	}
	
	protected override void Foo()
	{
	}
}");
		}
	}
}

