// 
// JoinStringTests.cs
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

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class JoinStringTests : ContextActionTestBase
	{
		public void Test (string input, string output)
		{
			Test<JoinStringAction> (@"
class TestClass
{
	string TestMethod (string arg)
	{
		return " + input + @";
	}
}", @"
class TestClass
{
	string TestMethod (string arg)
	{
		return " + output + @";
	}
}");
		}

		[Test]
		public void TestRegularString ()
		{
			Test ("\"a\" $+ \"a\"", "\"aa\"");
			Test ("arg + \"a\" $+ \"a\"", "arg + \"aa\"");
		}

		[Test]
		public void TestVerbatimString ()
		{
			Test ("@\"a\" $+ @\"a\"", "@\"aa\"");
		}

		public void TestWrongContext (string input)
		{
			TestWrongContext<JoinStringAction> (@"
class TestClass
{
	string TestMethod ()
	{
		return " + input + @";
	}
}");
		}

		[Test]
		public void TestWrongContext ()
		{
			TestWrongContext ("@\"a\" $+ \"a\"");
			TestWrongContext ("\"a\" $+ @\"a\"");
		}
	}
}
