// 
// ConvertCastToAsTest.cs
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

using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class ConvertCastToAsTests : ContextActionTestBase
	{
		void TestType(string type)
		{
			string input = @"
using System;
class TestClass
{
	void Test (object a)
	{
		var b = ($" + type + @")a;
	}
}";
			string output = @"
using System;
class TestClass
{
	void Test (object a)
	{
		var b = a as " + type + @";
	}
}";
			Test<ConvertCastToAsAction> (input, output);
		}

		[Test]
		public void Test()
		{
			TestType ("Exception");
		}

		[Test]
		public void TestNullable()
		{
			TestType ("int?");	
		}

		[Test]
		public void TestNonReferenceType()
		{
			TestWrongContext<ConvertCastToAsAction> (@"
using System;
class TestClass
{
	void Test (object a)
	{
		var b = (int)$a;
	}
}");
		}
	}
}
