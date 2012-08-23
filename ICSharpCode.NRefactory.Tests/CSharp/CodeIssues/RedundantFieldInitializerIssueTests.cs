// 
// RedundantFieldInitializerIssueTests.cs
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
	public class RedundantFieldInitializerIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestRedundantIntInitializer ()
		{
			var input = @"
class TestClass
{
	int i = 0;
	long l = 0L;
}";
			var output = @"
class TestClass
{
	int i;
	long l;
}";
			Test<RedundantFieldInitializerIssue> (input, 2, output);
		}

		[Test]
		public void TestRedundantFloatInitializer ()
		{
			var input = @"
class TestClass
{
	double d = 0;
	double d2 = 0.0;
}";
			var output = @"
class TestClass
{
	double d;
	double d2;
}";
			Test<RedundantFieldInitializerIssue> (input, 2, output);
		}

		[Test]
		public void TestRedundantBooleanInitializer ()
		{
			var input = @"
class TestClass
{
	bool x = false;
}";
			var output = @"
class TestClass
{
	bool x;
}";
			Test<RedundantFieldInitializerIssue> (input, 1, output);
		}

		[Test]
		public void TestRedundantCharInitializer ()
		{
			var input = @"
class TestClass
{
	char ch = '\0';
}";
			var output = @"
class TestClass
{
	char ch;
}";
			Test<RedundantFieldInitializerIssue> (input, 1, output);
		}

		[Test]
		public void TestRedundantReferenceTypeInitializer ()
		{
			var input = @"
class TestClass
{
	string str = null;
}";
			var output = @"
class TestClass
{
	string str;
}";
			Test<RedundantFieldInitializerIssue> (input, 1, output);
		}

		[Test]
		public void TestRedundantDynamicInitializer ()
		{
			var input = @"
class TestClass
{
	dynamic x = null, y = null;
}";
			var output = @"
class TestClass
{
	dynamic x, y;
}";
			Test<RedundantFieldInitializerIssue> (input, 2, output);
		}

		[Test]
		public void TestRedundantStructInitializer ()
		{
			var input = @"
struct TestStruct
{
}
class TestClass
{
	TestStruct x = new TestStruct ();
}";
			var output = @"
struct TestStruct
{
}
class TestClass
{
	TestStruct x;
}";
			Test<RedundantFieldInitializerIssue> (input, 1, output);
		}

		[Test]
		public void TestRedundantNullableInitializer ()
		{
			var input = @"
class TestClass
{
	int? i = null;
}";
			var output = @"
class TestClass
{
	int? i;
}";
			Test<RedundantFieldInitializerIssue> (input, 1, output);
		}
	}
}
