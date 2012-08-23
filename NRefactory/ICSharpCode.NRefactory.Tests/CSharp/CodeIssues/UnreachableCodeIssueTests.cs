// 
// UnreachableCodeIssueTests.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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

using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class UnreachableCodeIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestReturn ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		return;
		int a = 1;
	}
}";
			Test<UnreachableCodeIssue> (input, 1);
		}

		[Test]
		public void TestBreak ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		while (true) {
			break;
			int a = 1;
		}
	}
}";
			Test<UnreachableCodeIssue> (input, 1);
		}

		[Test]
		public void TestContinue ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		while (true) {
			continue;
			break;
		}
	}
}";
			Test<UnreachableCodeIssue> (input, 1);
		}

		[Test]
		public void TestFor ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		for (int i = 0; i < 10; i++) {
			break;
		}
	}
}";
			Test<UnreachableCodeIssue> (input, 1);
		}

		[Test]
		public void TestConstantCondition ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		if (true) {
			return;
		}
		int a = 1;
	}
}";
			Test<UnreachableCodeIssue> (input, 1);
		}

		[Test]
		public void TestConditionalExpression ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int a = true ? 1 : 0;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		int a = 1;
	}
}";
			Test<UnreachableCodeIssue> (input, 1, output);
		}

		[Test]
		public void TestInsideLambda ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		System.Action action = () => {
			return;
			int a = 1;
		};
	}
}";
			Test<UnreachableCodeIssue> (input, 1);
		}

		[Test]
		public void TestInsideAnonymousMethod ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		System.Action action = delegate () {
			return;
			int a = 1;
		};
	}
}";
			Test<UnreachableCodeIssue> (input, 1);
		}

		[Test]
		public void TestIgnoreLambdaBody ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		return;
		System.Action action = () => {
			return;
			int a = 1;
		};
	}
}";
			Test<UnreachableCodeIssue> (input, 1);
		}

		[Test]
		public void TestIgnoreAnonymousMethodBody ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		return;
		System.Action action = delegate() {
			return;
			int a = 1;
		};
	}
}";
			Test<UnreachableCodeIssue> (input, 1);
		}

		[Test]
		public void TestGroupMultipleStatements ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		return;
		int a = 1;
		a++;
	}
}";
			Test<UnreachableCodeIssue> (input, 1);
		}

		[Test]
		public void TestRemoveCode ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		return;
		int a = 1;
		a++;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		return;
	}
}";
			Test<UnreachableCodeIssue> (input, output, 0);
		}

		[Test]
		public void TestCommentCode ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		return;
		int a = 1;
		a++;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		return;
/*
		int a = 1;
		a++;
*/
	}
}";
			Test<UnreachableCodeIssue> (input, output, 1);
		}
	}
}
