//
// CallToStaticMemberViaDerivedType.cs
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
	public class ReferenceToStaticMemberViaDerivedTypeTests : InspectionActionTestBase
	{
		[Test]
		public void MemberInvocation()
		{
			var input = @"
class A
{
	public static void F() { }
}
class B : A { }
class C
{
	void Main()
	{
		B.F();
	}
}";
			TestRefactoringContext context;			
			var issues = GetIssues(new ReferenceToStaticMemberViaDerivedTypeIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			Assert.AreEqual(11, issues [0].Start.Line);

			CheckFix(context, issues [0], @"
class A
{
	public static void F() { }
}
class B : A { }
class C
{
	void Main()
	{
		A.F();
	}
}"
			);
		}

		[Test]
		public void PropertyAccess()
		{
			var input = @"
class A
{
	public static string Property { get; set; }
}
class B : A { }
class C
{
	void Main()
	{
		System.Console.WriteLine(B.Property);
	}
}";
			TestRefactoringContext context;			
			var issues = GetIssues(new ReferenceToStaticMemberViaDerivedTypeIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			Assert.AreEqual(11, issues [0].Start.Line);
			
			CheckFix(context, issues [0], @"
class A
{
	public static string Property { get; set; }
}
class B : A { }
class C
{
	void Main()
	{
		System.Console.WriteLine(A.Property);
	}
}"
			);
		}
		
		[Test]
		public void FieldAccess()
		{
			var input = @"
class A
{
	public static string Property;
}
class B : A { }
class C
{
	void Main()
	{
		System.Console.WriteLine(B.Property);
	}
}";
			TestRefactoringContext context;			
			var issues = GetIssues(new ReferenceToStaticMemberViaDerivedTypeIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			Assert.AreEqual(11, issues [0].Start.Line);
			
			CheckFix(context, issues [0], @"
class A
{
	public static string Property;
}
class B : A { }
class C
{
	void Main()
	{
		System.Console.WriteLine(A.Property);
	}
}"
			);
		}

		[Test]
		public void NestedClass()
		{
			var input = @"
class A
{
	public class B
	{
		public static void F() { }
	}
	public class C : B { }
}
class D
{
	void Main()
	{
		A.C.F();
	}
}";
			TestRefactoringContext context;			
			var issues = GetIssues(new ReferenceToStaticMemberViaDerivedTypeIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			Assert.AreEqual(14, issues [0].Start.Line);
			
			CheckFix(context, issues [0], @"
class A
{
	public class B
	{
		public static void F() { }
	}
	public class C : B { }
}
class D
{
	void Main()
	{
		A.B.F();
	}
}"
			);
		}
		
		[Test]
		public void ExpandsTypeWithNamespaceIfNeccessary()
		{
			var input = @"
namespace First
{
	class A
	{
		public static void F() { }
	}
}
namespace Second
{
	public class B : First.A { }
	class C
	{
		void Main()
		{
			B.F();
		}
	}
}";
			TestRefactoringContext context;			
			var issues = GetIssues(new ReferenceToStaticMemberViaDerivedTypeIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			Assert.AreEqual(16, issues [0].Start.Line);
			
			CheckFix(context, issues [0], @"
namespace First
{
	class A
	{
		public static void F() { }
	}
}
namespace Second
{
	public class B : First.A { }
	class C
	{
		void Main()
		{
			First.A.F();
		}
	}
}"
			);
		}
		
		[Test]
		public void IgnoresCorrectCalls()
		{
			var input = @"
class A
{
	public static void F() { }
}
class B
{
	void Main()
	{
		A.F();
	}
}";
			TestRefactoringContext context;			
			var issues = GetIssues(new ReferenceToStaticMemberViaDerivedTypeIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void IgnoresNonStaticCalls()
		{
			var input = @"
class A
{
	public void F() { }
}
class B : A { }
class C
{
	void Main()
	{
		B b = new B();
		b.F();
	}
}";
			TestRefactoringContext context;			
			var issues = GetIssues(new ReferenceToStaticMemberViaDerivedTypeIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void IgnoresOwnMemberFunctions()
		{
			var input = @"
class A
{
	protected static void F() { }
}
class B : A
{
	void Main()
	{
		F();
		this.F();
		base.F();
	}
}";
			TestRefactoringContext context;			
			var issues = GetIssues(new ReferenceToStaticMemberViaDerivedTypeIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
	}
}

