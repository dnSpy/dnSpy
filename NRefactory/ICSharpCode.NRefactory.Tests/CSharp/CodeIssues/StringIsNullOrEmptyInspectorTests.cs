// 
// StringIsNullOrEmptyInspectorTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.CodeActions;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class StringIsNullOrEmptyInspectorTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCaseNS1 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (str != null && str != """")
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS2 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (null != str && str != """")
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS3 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (null != str && """" != str)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS4 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (str != null && str != """")
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN1 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (str != """" && str != null)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN2 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if ("""" != str && str != null)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN3 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if ("""" != str && null != str)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}


		[Test]
		public void TestInspectorCaseSN4 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (str != """" && null != str)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS5 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (str == null || str == """")
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS6 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (null == str || str == """")
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS7 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (null == str || """" == str)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS8 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (str == null || """" == str)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN5 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (str == """" || str == null)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN6 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if ("""" == str || str == null)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN7 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if ("""" == str || null == str)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN8 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		if (str == """" || null == str)
			;
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new StringIsNullOrEmptyIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}
	}
}
