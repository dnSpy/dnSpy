// 
// DelegateContextTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

namespace ICSharpCode.NRefactory.CSharp.CodeCompletion
{
	[TestFixture()]
	public class DelegateContextTests : TestBase
	{
		/// <summary>
		/// Bug 4483 - Completion too aggressive for anonymous methods
		/// </summary>
		[Test()]
		public void TestBug4483()
		{
			// note 'string bar = new Test ().ToString ()' would be valid.
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;

public class Test
{
	void TestFoo()
	{
		$Action act = a$
	}
}
", provider => {
				Assert.IsFalse(provider.AutoSelect);
			});
		}

		[Test()]
		public void TestParameterContext()
		{
			// note 'string bar = new Test ().ToString ()' would be valid.
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;

public class Test
{
	void TestFoo(Action act)
	{
		$TestFoo(a$
	}
}
", provider => {
				Assert.IsFalse(provider.AutoSelect);
				Assert.IsNotNull(provider.Find("delegate"));
			});
		}

		/// <summary>
		/// Bug 5207 - [regression] delegate completion like event completion 
		/// </summary>
		[Test()]
		public void TestBug5207()
		{
			// note 'string bar = new Test ().ToString ()' would be valid.
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;

public class Test
{
	Action<int,int> foo;

	void TestFoo()
	{
		$foo = d$
	}
}
", provider => {
				Assert.IsFalse(provider.AutoSelect);
				Assert.IsNotNull(provider.Find("(arg1, arg2)"));
			});
		}


		[Test()]
		public void TestRefOutParams()
		{
			CodeCompletionBugTests.CombinedProviderTest(
				@"using System;
public delegate void FooBar (out int foo, ref int bar, params object[] additional);

public class Test
{
	FooBar foo;

	void TestFoo()
	{
		$foo = d$
	}
}
", provider => {
				Assert.IsFalse(provider.AutoSelect);
				Assert.IsNotNull(provider.Find("(out int foo, ref int bar, object[] additional)"));
				Assert.IsNull(provider.Find("(foo, bar, additional)"));
			});
		}
	}
}

