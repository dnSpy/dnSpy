// 
// AccessToModifiedClosureTests.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
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

using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class AccessToModifiedClosureTests : InspectionActionTestBase
	{
		static void Test(string input, int issueCount, string output = null, int fixIndex = -1)
		{
			TestRefactoringContext context;
			var issues = GetIssues (new AccessToModifiedClosureIssue (), input, out context);
			Assert.AreEqual (issueCount, issues.Count);
			if (issueCount > 0) {
				if (output != null) {
					if (fixIndex == -1)
						CheckFix (context, issues, output);
					else
						CheckFix (context, issues [fixIndex], output);
				} else {
					foreach (var issue in issues)
						Assert.AreEqual (0, issue.Actions.Count);
				}
			}
		}

		[Test]
		public void TestForeachVariable ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		System.Func<int, int> f;
		foreach (var i in a)
			f = new System.Func<int, int> (x => x + i);
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		System.Func<int, int> f;
		foreach (var i in a) {
			var i1 = i;
			f = new System.Func<int, int> (x => x + i1);
		}
	}
}";
			Test (input, 1, output);
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
			var f = new System.Func<int, int> (x => x + i);
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		for (int i = 0; i < 10; i++) {
			var i1 = i;
			var f = new System.Func<int, int> (x => x + i1);
		}
	}
}";
			Test (input, 1, output);
		}
	
		[Test]
		public void TestForInitializer ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int j)
	{
		int i;
		for (i = 0; j < 10; j++) {
			var f = new System.Func<int, int> (x => x + i);
		}
	}
}";
			Test (input, 0);
		}

		[Test]
		public void TestForBody ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		for (int i = 0; i < 10;) {
			var f = new System.Func<int, int> (delegate (int x) { return x + i; });
			i++;
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		for (int i = 0; i < 10;) {
			var i1 = i;
			var f = new System.Func<int, int> (delegate (int x) { return x + i1; });
			i++;
		}
	}
}";
			Test (input, 1, output);
		}
		
		[Test]
		public void TestWhileBody ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		while (i < 10) {
			i++;
			var f = new System.Func<int, int> (x => x + i);
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		while (i < 10) {
			i++;
			var i1 = i;
			var f = new System.Func<int, int> (x => x + i1);
		}
	}
}";
			Test (input, 1, output);
		}
		
		[Test]
		public void TestWhileCondition ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		while (i++ < 10) {
			var f = new System.Func<int, int> (delegate (int x) { return x + i; });
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		while (i++ < 10) {
			var i1 = i;
			var f = new System.Func<int, int> (delegate (int x) { return x + i1; });
		}
	}
}";
			Test (input, 1, output);
		}

		[Test]
		public void TestDoWhileBody ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		do {
			i += 1;
			var f = new System.Func<int, int> (x => x + i);
		} while (i < 10);
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		do {
			i += 1;
			var i1 = i;
			var f = new System.Func<int, int> (x => x + i1);
		} while (i < 10);
	}
}";
			Test (input, 1, output);
		}

		[Test]
		public void TestDoWhileCondition ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		while (i++ < 10) {
			var f = new System.Func<int, int> (x => x + i);
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		while (i++ < 10) {
			var i1 = i;
			var f = new System.Func<int, int> (x => x + i1);
		}
	}
}";
			Test (input, 1, output);
		}

		[Test]
		public void TestMultipleLambdas ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		foreach (var i in a) {
			var f = new System.Func<int, int> (x => x + i);
			var f2 = new System.Func<int, bool> (x => x + i > 10);
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		foreach (var i in a) {
			var i1 = i;
			var f = new System.Func<int, int> (x => x + i1);
			var i1 = i;
			var f2 = new System.Func<int, bool> (x => x + i1 > 10);
		}
	}
}";
			Test (input, 2, output);
		}

		[Test]
		public void TestFixAllInLambda ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int[] array)
	{
		foreach (var i in array) {
			var f = new System.Func<int, int> (x => {
				int a = i + 1;
				int b = i + 2;
				return a + b + i;
			});
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int[] array)
	{
		foreach (var i in array) {
			var i1 = i;
			var f = new System.Func<int, int> (x => {
				int a = i1 + 1;
				int b = i1 + 2;
				return a + b + i1;
			});
		}
	}
}";
			Test (input, 3, output, 0);
		}
		
		[Test]
		public void TestModifiedInLambda ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int[] array)
	{
		foreach (var i in array) {
			var f = new System.Func<int, int> (x => {
				i = 0;
				int a = i + 1;
				int b = i + 2;
				return a + b + i;
			});
		}
	}
}";
			Test (input, 0);
			input = @"
class TestClass
{
	void TestMethod (int[] array)
	{
		foreach (var i in array) {
			var f = new System.Func<int, int> (x => {
				i++;
				int a = i + 1;
				i = 3;
				int b = i + 2;
				return a + b + i;
			});
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int[] array)
	{
		foreach (var i in array) {
			var i1 = i;
			var f = new System.Func<int, int> (x => {
				i1++;
				int a = i1 + 1;
				i1 = 3;
				int b = i1 + 2;
				return a + b + i1;
			});
		}
	}
}";
			Test (input, 1, output);
		}
		
		[Test]
		public void TestNestedLambda ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		foreach (var i in a) {
			var f = new System.Func<int, int> (x => {
				var f2 =  new System.Func<int, int> (y => y - i);
				return f2(x) + i;
			});
		}
	}
}";
			var output1 = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		foreach (var i in a) {
			var i1 = i;
			var f = new System.Func<int, int> (x => {
				var f2 =  new System.Func<int, int> (y => y - i1);
				return f2(x) + i1;
			});
		}
	}
}";
			Test (input, 2, output1, 1);
			var output2 = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		foreach (var i in a) {
			var f = new System.Func<int, int> (x => {
				var i1 = i;
				var f2 =  new System.Func<int, int> (y => y - i1);
				return f2(x) + i;
			});
		}
	}
}";
			Test (input, 2, output2, 0);
		}
	
		[Test]
		public void TestMultipleVariables ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		foreach (var i in a) {
			foreach (var j in a) {
				var f = new System.Func<int, int> (x => x + i + j);
			}
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		foreach (var i in a) {
			foreach (var j in a) {
				var i1 = i;
				var j1 = j;
				var f = new System.Func<int, int> (x => x + i1 + j1);
			}
		}
	}
}";
			Test (input, 2, output);
		}

		[Test]
		public void TestModificationAfterLambdaDecl ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		System.Func<int, int> f = x => i + x;
		i += 1;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		var i1 = i;
		System.Func<int, int> f = x => i1 + x;
		i += 1;
	}
}";
			Test (input, 1, output);
		}

		[Test]
		public void TestModificationBeforeLambdaDecl ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		i += 1;
		System.Func<int, int> f = x => i + x;
	}
}";
			Test (input, 0);
		}

		[Test]
		public void TestUnreachable ()
		{
			var returnCase = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		System.Func<int, int> f = x => i + x;
		return;
		i += 1;
	}
}";
			var ifCase = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		System.Func<int, int> f;
		if (i > 0)
			f = x => i + x;
		else
			i += 1;
	}
}";
			Test (returnCase, 0);
			Test (ifCase, 0);
		}

		[Test]
		public void TestParameter ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int i)
	{
		System.Func<int, int> f = x => i + x;
		i += 1;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int i)
	{
		var i1 = i;
		System.Func<int, int> f = x => i1 + x;
		i += 1;
	}
}";
			Test (input, 1, output);
		}

		[Test]
		public void TestModificationInSameStatement ()
		{
			var input1 = @"
class TestClass
{
	void TestMethod2 (int b, System.Func<int, int> a)
	{
	    TestMethod2 (b++, c => c + b);
	}
}";
			var input2 = @"
class TestClass
{
	void TestMethod3 (System.Func<int, int> a, int b)
	{
	    TestMethod3 (c => c + b, b++);
	}
}";
			var output2 = @"
class TestClass
{
	void TestMethod3 (System.Func<int, int> a, int b)
	{
		var b1 = b;
	    TestMethod3 (c => c + b1, b++);
	}
}";
			Test (input1, 0);
			Test (input2, 1, output2);
		}
		
		[Test]
		public void TestInsertion ()
		{
			var input1 = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		System.Func<int, int> f = null;
		if ((f = x => x + i) != null)
			i++;
	}
}";
			var output1 = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		System.Func<int, int> f = null;
		var i1 = i;
		if ((f = x => x + i1) != null)
			i++;
	}
}";
			Test (input1, 1, output1);

			var input2 = @"
class TestClass
{
	void TestMethod (int k)
	{
		int i = 0;
		System.Func<int, int> f = null;
		switch (k) {
		default:
			f = x => x + i;
			break;
		}
		i++;
	}
}";
			var output2 = @"
class TestClass
{
	void TestMethod (int k)
	{
		int i = 0;
		System.Func<int, int> f = null;
		switch (k) {
		default:
			var i1 = i;
			f = x => x + i1;
			break;
		}
		i++;
	}
}";
			Test (input2, 1, output2);
		}

		[Test]
		public void TestLambdaInLoopCondition ()
		{
			var forCase = @"
class TestClass
{
	void TestMethod ()
	{
		System.Func<int, int> f = null;
		for (int i = 0; i < 10 && ((f = x => x + i) != null); i++) {
		}
	}
}";
			var whileCase = @"
class TestClass
{
	void TestMethod ()
	{
		System.Func<int, int> f = null;
		int i = 0;
		while (i < 10 && (f = x => x + i) != null) i++;
	}
}";
			var doWhileCase = @"
class TestClass
{
	void TestMethod ()
	{
		System.Func<int, int> f = null;
		int i = 0;
		do { i++; } while (i < 10 && (f = x => x + i) != null);
	}
}";
			Test (forCase, 1);
			Test (whileCase, 1);
			Test (doWhileCase, 1);
		}

		[Test]
		public void TestLambdaInForIterator ()
		{

			var input = @"
class TestClass
{
	void TestMethod ()
	{
		System.Func<int, int> f = null;
		for (int i = 0; i < 10; i++, f = x => x + i) {
		}
	}
}";
			Test (input, 1);
		}

		[Test]
		public void TestExistingVariableName ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		foreach (var i in a) {
			int i1;
			var f = new System.Func<int, int> (x => x + i);
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int[] a)
	{
		foreach (var i in a) {
			int i1;
			var i2 = i;
			var f = new System.Func<int, int> (x => x + i2);
		}
	}
}";
			Test (input, 1, output);
		}

		[Test]
		public void TestConstructor ()
		{
			var input = @"
class TestClass
{
	public TestClass (int[] a)
	{
		foreach (var i in a) {
			int i1;
			var f = new System.Func<int, int> (x => x + i);
		}
	}
}";
			var output = @"
class TestClass
{
	public TestClass (int[] a)
	{
		foreach (var i in a) {
			int i1;
			var i2 = i;
			var f = new System.Func<int, int> (x => x + i2);
		}
	}
}";
			Test (input, 1, output);
		}

		[Test]
		public void TestField ()
		{
			var input = @"
class TestClass
{
	System.Action<int> a = i =>
	{
		System.Func<int> f = () => i + 1;
		i++;
	};
}";
			var output = @"
class TestClass
{
	System.Action<int> a = i =>
	{
		var i1 = i;
		System.Func<int> f = () => i1 + 1;
		i++;
	};
}";
			Test (input, 1, output);
		}

		
	}
}
