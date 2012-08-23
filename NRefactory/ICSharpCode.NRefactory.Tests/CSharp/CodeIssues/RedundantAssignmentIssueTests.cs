// 
// RedundantAssignmentIssueTests.cs
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
	public class RedundantAssignmentIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestVariableInitializerNotUsed ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 1;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		int i;
	}
}";
			Test<RedundantAssignmentIssue> (input, 1, output);
		}

		[Test]
		public void TestVariableAssignmentNotUsed ()
		{
			var input = @"
class TestClass
{
	int TestMethod ()
	{
		int i = 1;
		int j = i;
		i = 2;
		return j;
	}
}";
			var output = @"
class TestClass
{
	int TestMethod ()
	{
		int i = 1;
		int j = i;
		return j;
	}
}";
			Test<RedundantAssignmentIssue> (input, 1, output);
		}

		[Test]
		public void TestParameterAssignmentNotUsed ()
		{
			var input = @"
class TestClass
{
	int TestMethod (int i)
	{
		int j = i;
		i = 2;
		return j;
	}
}";
			var output = @"
class TestClass
{
	int TestMethod (int i)
	{
		int j = i;
		return j;
	}
}";
			Test<RedundantAssignmentIssue> (input, 1, output);
		}

		[Test]
		public void TestAssignmentInExpression ()
		{
			var input = @"
class TestClass
{
	int TestMethod (int i)
	{
		int j = i = 2;
		return j;
	}
}";
			var output = @"
class TestClass
{
	int TestMethod (int i)
	{
		int j = 2;
		return j;
	}
}";
			Test<RedundantAssignmentIssue> (input, 1, output);
		}

		[Test]
		public void TestOutArgument ()
		{
			var input = @"
class TestClass
{
	void Test (out int i)
	{
		i = 0;
	}
	int TestMethod ()
	{
		int i = 2;
		Test (out i);
		return i;
	}
}";
			var output = @"
class TestClass
{
	void Test (out int i)
	{
		i = 0;
	}
	int TestMethod ()
	{
		int i;
		Test (out i);
		return i;
	}
}";
			Test<RedundantAssignmentIssue> (input, 1, output);
		}

		[Test]
		public void TestOutArgument2 ()
		{
			var input = @"
class TestClass
{
	void Test (out int i)
	{
		i = 0;
	}
	int TestMethod ()
	{
		int i;
		Test (out i);
		i = 2;
		return i;
	}
}";
			Test<RedundantAssignmentIssue> (input, 0);
		}

		[Test]
		public void TestRefArgument ()
		{
			var input = @"
class TestClass
{
	void Test (ref int i)
	{
		i = 0;
	}
}";
			Test<RedundantAssignmentIssue> (input, 0);
		}

		[Test]
		public void TestAssignmentOperator ()
		{
			var input = @"
class TestClass
{
	int TestMethod ()
	{
		int i = 1;
		i += 2;
		return i;
	}
}";
			Test<RedundantAssignmentIssue> (input, 0);
		}

		[Test]
		public void TestIf ()
		{
			var input = @"
class TestClass
{
	int TestMethod (int j)
	{
		int i = 1;
		if (j > 0) {
			i += 2;
		} else {
		}
		return i;
	}
}";
			Test<RedundantAssignmentIssue> (input, 0);
		}

		[Test]
		public void TestConditionalExpression ()
		{
			var input = @"
class TestClass
{
	int TestMethod (int j)
	{
		int i = 1;
		return j > 0 ? i : 0;
	}
}";
			Test<RedundantAssignmentIssue> (input, 0);
		}

		[Test]
		public void TestLoop ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		var x = 0;
		for (int i = 0; i < 10; i++) {
			if (i > 5) {
				x++;
			} else {
				x = 2;
			}
		}
		if (x > 1) ;
	}
}";
			Test<RedundantAssignmentIssue> (input, 0);
		}

		[Test]
		public void TestForeach ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int[] array)
	{
		foreach (int j in array) {
			bool x = false;
			foreach (int k in array)
				foreach (int i in array)
					if (i > 5) x = true;
			if (x) break;
		}
	}
}";
			Test<RedundantAssignmentIssue> (input, 0);
		}
	}
}
