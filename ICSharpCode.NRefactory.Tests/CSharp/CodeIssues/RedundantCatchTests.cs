//
// RedundantCatchTests.cs
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
	public class RedundantCatchTests : InspectionActionTestBase
	{
		const string BaseInput = @"
using System;
class A
{
	void F()
	{";

		[Test]
		public void TestEmptyCatch()
		{
			var input = BaseInput + @"
		try {
			F ();
		} catch (ArgumentOutOfRangeException aoore) {
			Console.WriteLine (aoore);
		} catch (ArgumentException) {
			throw;
		} catch {
			throw;
		}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new RedundantCatchIssue(), input, out context);
			Assert.AreEqual(2, issues.Count);
			
			CheckFix(context, issues, BaseInput + @"
		try {
			F ();
		} catch (ArgumentOutOfRangeException aoore) {
			Console.WriteLine (aoore);
		}  
	}
}");
		}
		
		[Test]
		public void TestOnlyRedundantCatches()
		{
			var input = BaseInput + @"
		try {
			F ();
			Console.WriteLine (""Inside try"");
		} catch {
			throw;
		}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new RedundantCatchIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			
			CheckFix(context, issues, BaseInput + @"
		F ();
		Console.WriteLine (""Inside try"");
	}
}");
		}
		
		[Test]
		public void AddsBlockIfNeccessary()
		{
			var input = BaseInput + @"
		if (true)
			try {
				F ();
				Console.WriteLine (""Inside try"");
			} catch {
				throw;
			}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new RedundantCatchIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			
			CheckFix(context, issues, BaseInput + @"
		if (true) {
			F ();
			Console.WriteLine (""Inside try"");
		}
	}
}");
		}

		
		[Test]
		public void AddsBlockIfNeccessaryOnEmptyTryBlock()
		{
			var input = BaseInput + @"
		if (true)
			try {
			} catch {
				throw;
			}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new RedundantCatchIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			
			CheckFix(context, issues, BaseInput + @"
		if (true) {
		}
	}
}");
		}
		
		[Test]
		public void EmptyTryCatchSkeleton()
		{
			var input = BaseInput + @"
		try {
		} catch {
		}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new RedundantCatchIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void DoesNotAddBlockIfUnneccessary()
		{
			var input = BaseInput + @"
		if (true)
			try {
				F ();
			} catch {
				throw;
			}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new RedundantCatchIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			
			CheckFix(context, issues, BaseInput + @"
		if (true)
			F ();
	}
}");
		}
		
		[Test]
		public void NoIssuesWhenMissingCatch()
		{
			var input = BaseInput + @"
		try {
			F ();
		}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new RedundantCatchIssue(), input, out context, true);
			Assert.AreEqual(0, issues.Count);
		}

		[Test]
		public void TestEmptyCatchWithFinally()
		{
			var input = BaseInput + @"
		try {
			F ();
		} catch {
			throw;
		} finally {
			Console.WriteLine (""Inside finally"");
		}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new RedundantCatchIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			
			CheckFix(context, issues, BaseInput + @"
		try {
			F ();
		}  finally {
			Console.WriteLine (""Inside finally"");
		}
	}
}");
		}
	}
}

