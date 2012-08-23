// 
// ConvertSwitchToIfTests.cs
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
	public class ConvertSwitchToIfTests : ContextActionTestBase
	{
		[Test]
		public void TestReturn ()
		{
			Test<ConvertSwitchToIfAction> (@"
class TestClass
{
	int TestMethod (int a)
	{
		$switch (a) {
		case 0:
			return 0;
		case 1:
		case 2:
			return 1;
		case 3:
		case 4:
		case 5:
			return 1;
		default:
			return 2;
		}
	}
}", @"
class TestClass
{
	int TestMethod (int a)
	{
		if (a == 0) {
			return 0;
		} else
			if (a == 1 || a == 2) {
			return 1;
		} else
				if (a == 3 || a == 4 || a == 5) {
			return 1;
		} else {
			return 2;
		}
	}
}");
		}

		[Test]
		public void TestWithoutDefault ()
		{
			Test<ConvertSwitchToIfAction> (@"
class TestClass
{
	int TestMethod (int a)
	{
		$switch (a) {
		case 0:
			return 0;
		case 1:
		case 2:
			return 1;
		case 3:
		case 4:
		case 5:
			return 1;
		}
	}
}", @"
class TestClass
{
	int TestMethod (int a)
	{
		if (a == 0) {
			return 0;
		} else
			if (a == 1 || a == 2) {
			return 1;
		} else
				if (a == 3 || a == 4 || a == 5) {
			return 1;
		}
	}
}");
		}

		[Test]
		public void TestBreak ()
		{
			Test<ConvertSwitchToIfAction> (@"
class TestClass
{
	void TestMethod (int a)
	{
		$switch (a) {
		case 0:
			int b = 1;
			break;
		case 1:
		case 2:
			break;
		case 3:
		case 4:
		case 5:
			break;
		default:
			break;
		}
	}
}", @"
class TestClass
{
	void TestMethod (int a)
	{
		if (a == 0) {
			int b = 1;
		} else
			if (a == 1 || a == 2) {
		} else
				if (a == 3 || a == 4 || a == 5) {
		} else {
		}
	}
}");
		}
		
		[Test]
		public void TestOperatorPriority ()
		{
			Test<ConvertSwitchToIfAction> (@"
class TestClass
{
	int TestMethod (int a)
	{
		$switch (a) {
		case 0:
			return 0;
		case 1 == 1 ? 1 : 2:
			return 1;
		default:
			return 2;
		}
	}
}", @"
class TestClass
{
	int TestMethod (int a)
	{
		if (a == 0) {
			return 0;
		} else
			if (a == (1 == 1 ? 1 : 2)) {
			return 1;
		} else {
			return 2;
		}
	}
}");
		}

		[Test]
		public void TestEmptySwitch ()
		{
			TestWrongContext<ConvertSwitchToIfAction> (@"
class TestClass
{
	void TestMethod (int a)
	{
		$switch (a)
		{
		}
	}
}");
		}

		[Test]
		public void TestSwitchWithDefaultOnly ()
		{
			TestWrongContext<ConvertSwitchToIfAction> (@"
class TestClass
{
	void TestMethod (int a)
	{
		$switch (a)
		{
			case 0:
			default:
			break;
		}
	}
}");
		}
		
		[Test]
		public void TestNonTrailingBreak ()
		{
			TestWrongContext<ConvertSwitchToIfAction> (@"
class TestClass
{
	void TestMethod (int a, int b)
	{
		$switch (a)
		{
			case 0:
				if (b == 0) break;
				b = 1;
				break;
			default:
			break;
		}
	}
}");
		}
	
	}
}
