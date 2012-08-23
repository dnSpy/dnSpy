// 
// AccessToDisposedClosureTests.cs
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

using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	public class AccessToDisposedClosureTests : InspectionActionTestBase
	{
		void Test (string input, int issueCount)
		{
			TestRefactoringContext context;
			var issues = GetIssues (new AccessToDisposedClosureIssue (), input, out context);
			Assert.AreEqual (issueCount, issues.Count);
		}

		[Test]
		public void TestUsing ()
		{
			string input = @"
using System;
class TestClass : IDisposable
{
	public void Dispose () { }
}
class TestClass2
{
	void TestMethod ()
	{
		using (var x = new TestClass ()) {
			Action a = () => x.ToString ();
		}
	}
}";
			Test (input, 1);
		}

		[Test]
		public void TestUsing2 ()
		{
			string input = @"
using System;
class TestClass : IDisposable
{
	public void Dispose () { }
}
class TestClass2
{
	void TestMethod ()
	{
		var x = new TestClass ();
		using (x) {
			Action a = () => x.ToString ();
		}
	}
}";
			Test (input, 1);
		}

		[Test]
		public void TestDispose ()
		{
			string input = @"
using System;
class TestClass : IDisposable
{
	public void Dispose () { }
}
class TestClass2
{
	void TestMethod ()
	{
		var x = new TestClass ();
		Action a = () => x.ToString ();
		x.Dispose();	
	}
}";
			Test (input, 1);
		}
	}
}