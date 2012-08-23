//
// ConvertAnonymousDelegateToExpressionTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class ConvertAnonymousDelegateToLambdaTests : ContextActionTestBase
	{
		[Test]
		public void BasicTest ()
		{
			Test<ConvertAnonymousDelegateToLambdaAction>(@"
class A
{
	void F ()
	{
		System.Action<int, int> action = delegate$ (int i1, int i2) { System.Console.WriteLine (i1); };
	}
}", @"
class A
{
	void F ()
	{
		System.Action<int, int> action = (i1, i2) =>  {
	System.Console.WriteLine (i1);
};
	}
}");
		}

		[Test]
		public void VarDeclaration ()
		{
			Test<ConvertAnonymousDelegateToLambdaAction>(@"
class A
{
	void F ()
	{
		var action = delegate$ (int i1, int i2) { System.Console.WriteLine (i1); };
	}
}", @"
class A
{
	void F ()
	{
		var action = (int i1, int i2) =>  {
	System.Console.WriteLine (i1);
};
	}
}");
		}

		[Test]
		public void IgnoresParameterLessDelegates ()
		{
			TestWrongContext<ConvertAnonymousDelegateToLambdaAction>(@"
class A
{
	void F ()
	{
		System.Action<int> = delegate$ { System.Console.WriteLine (i); };
	}
}");
		}
	}
}

