// 
// ConvertIfToSwtichTests.cs
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

using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class ConvertIfToSwtichTests : ContextActionTestBase
	{

		[Test]
		public void TestBreak ()
		{
			Test<ConvertIfToSwitchAction> (@"
class TestClass
{
	void TestMethod (int a)
	{
		int b;
		$if (a == 0) {
			b = 0;
		} else if (a == 1) {
			b = 1;
		} else if (a == 2 || a == 3) {
			b = 2;
		} else {
			b = 3;
		}
	}
}", @"
class TestClass
{
	void TestMethod (int a)
	{
		int b;
		switch (a) {
		case 0:
			b = 0;
			break;
		case 1:
			b = 1;
			break;
		case 2:
		case 3:
			b = 2;
			break;
		default:
			b = 3;
			break;
		}
	}
}");
		}

		[Test]
		public void TestReturn ()
		{
			Test<ConvertIfToSwitchAction> (@"
class TestClass
{
	int TestMethod (int a)
	{
		$if (a == 0) {
			int b = 1;
			return b + 1;
		} else if (a == 2 || a == 3) {
			return 2;
		} else {
			return -1;
		}
	}
}", @"
class TestClass
{
	int TestMethod (int a)
	{
		switch (a) {
		case 0:
			int b = 1;
			return b + 1;
		case 2:
		case 3:
			return 2;
		default:
			return -1;
		}
	}
}");
		}
		
		[Test]
		public void TestConstantExpression ()
		{
			Test<ConvertIfToSwitchAction> (@"
class TestClass
{
	int TestMethod (int? a)
	{
		$if (a == (1 == 1 ? 11 : 12)) {
			return 1;
		} else if (a == (2 * 3) + 1 || a == 6 / 2) {
			return 2;
		} else if (a == null || a == (int)(10L + 2) || a == default(int) || a == sizeof(int)) {
			return 3;		
		} else {
			return -1;
		}
	}
}", @"
class TestClass
{
	int TestMethod (int? a)
	{
		switch (a) {
		case (1 == 1 ? 11 : 12):
			return 1;
		case (2 * 3) + 1:
		case 6 / 2:
			return 2;
		case null:
		case (int)(10L + 2):
		case default(int):
		case sizeof(int):
			return 3;
		default:
			return -1;
		}
	}
}");


			Test<ConvertIfToSwitchAction> (@"
class TestClass
{
	const int b = 0;
	int TestMethod (int a)
	{
		const int c = 1;
		$if (a == b) {
			return 1;
		} else if (a == b + c) {
			return 0;
		} else {
			return -1;
		}
	}
}", @"
class TestClass
{
	const int b = 0;
	int TestMethod (int a)
	{
		const int c = 1;
		switch (a) {
		case b:
			return 1;
		case b + c:
			return 0;
		default:
			return -1;
		}
	}
}");
		}

		[Test]
		public void TestNestedOr ()
		{
			Test<ConvertIfToSwitchAction> (@"
class TestClass
{
	int TestMethod (int a)
	{
		$if (a == 0) {
			return 1;
		} else if ((a == 2 || a == 4) || (a == 3 || a == 5)) {
			return 2;
		} else {
			return -1;
		}
	}
}", @"
class TestClass
{
	int TestMethod (int a)
	{
		switch (a) {
		case 0:
			return 1;
		case 2:
		case 4:
		case 3:
		case 5:
			return 2;
		default:
			return -1;
		}
	}
}");
		}

		[Test]
		public void TestComplexSwitchExpression ()
		{
			Test<ConvertIfToSwitchAction> (@"
class TestClass
{
	int TestMethod (int a, int b)
	{
		$if (a + b == 0) {
			return 1;
		} else if (1 == a + b) {
			return 0;
		} else {
			return -1;
		}
	}
}", @"
class TestClass
{
	int TestMethod (int a, int b)
	{
		switch (a + b) {
		case 0:
			return 1;
		case 1:
			return 0;
		default:
			return -1;
		}
	}
}");
		}

		[Test]
		public void TestNonConstantExpression ()
		{
			TestWrongContext<ConvertIfToSwitchAction> (@"
class TestClass
{
	void TestMethod (int a, int c)
	{
		int b;
		$if (a == 0) {
			b = 0;
		} else if (a == c) {
			b = 1;
		} else if (a == 2 || a == 3) {
			b = 2;
		} else {
			b = 3;
		}
	}
}");
			TestWrongContext<ConvertIfToSwitchAction> (@"
class TestClass
{
	void TestMethod (int a, int c)
	{
		int b;
		$if (a == c) {
			b = 0;
		} else if (a == 1) {
			b = 1;
		} else if (a == 2 || a == 3) {
			b = 2;
		} else {
			b = 3;
		}
	}
}");
			TestWrongContext<ConvertIfToSwitchAction> (@"
class TestClass
{
	void TestMethod (int a, int c)
	{
		int b;
		$if (a == 0) {
			b = 0;
		} else if (a == 1) {
			b = 1;
		} else if (a == 2 || a == c) {
			b = 2;
		} else {
			b = 3;
		}
	}
}");
		}
		
		[Test]
		public void TestNonEqualityComparison ()
		{
			TestWrongContext<ConvertIfToSwitchAction> (@"
class TestClass
{
	void TestMethod (int a)
	{
		int b;
		$if (a == 0) {
			b = 0;
		} else if (a > 4) {
			b = 1;
		} else if (a == 2 || a == 3) {
			b = 2;
		} else {
			b = 3;
		}
	}
}");
		}

		[Test]
		public void TestValidType ()
		{
			// enum
			Test<ConvertIfToSwitchAction> (@"
enum TestEnum
{
	First,
	Second,
}
class TestClass
{
	int TestMethod (TestEnum a)
	{
		$if (a == TestEnum.First) {
			return 1;
		} else {
			return -1;
		}
	}
}", @"
enum TestEnum
{
	First,
	Second,
}
class TestClass
{
	int TestMethod (TestEnum a)
	{
		switch (a) {
		case TestEnum.First:
			return 1;
		default:
			return -1;
		}
	}
}");

			// string, bool, char, integral, nullable
			TestValidType ("string", "\"test\"");
			TestValidType ("bool", "true");
			TestValidType ("char", "'a'");
			TestValidType ("byte", "0");
			TestValidType ("sbyte", "0");
			TestValidType ("short", "0");
			TestValidType ("long", "0");
			TestValidType ("ushort", "0");
			TestValidType ("uint", "0");
			TestValidType ("ulong", "0");
			TestValidType ("bool?", "null");
		}

		void TestValidType (string type, string caseValue)
		{
			Test<ConvertIfToSwitchAction> (@"
class TestClass
{
	int TestMethod (" + type + @" a)
	{
		$if (a == " + caseValue + @") {
			return 1;
		} else {
			return -1;
		}
	}
}", @"
class TestClass
{
	int TestMethod (" + type + @" a)
	{
		switch (a) {
		case " + caseValue + @":
			return 1;
		default:
			return -1;
		}
	}
}");
		}

		[Test]
		public void TestInvalidType ()
		{
			TestWrongContext<ConvertIfToSwitchAction> (@"
class TestClass
{
	void TestMethod (double a)
	{
		int b;
		$if (a == 0) {
			b = 0;
		} else {
			b = 3;
		}
	}
}");
		}

		[Test]
		public void TestNoElse()
		{
			Test<ConvertIfToSwitchAction> (@"
class TestClass
{
	void TestMethod (int a)
	{
		int b;
		$if (a == 0) {
			b = 0;
		} else if (a == 1) {
			b = 1;
		}
	}
}", @"
class TestClass
{
	void TestMethod (int a)
	{
		int b;
		switch (a) {
		case 0:
			b = 0;
			break;
		case 1:
			b = 1;
			break;
		}
	}
}");
		}
	
	}
}
