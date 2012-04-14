// 
// CreateDelegateTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
	public class CreateDelegateTests : ContextActionTestBase
	{
		[Test()]
		public void TestCreateDelegate ()
		{
			Test<CreateDelegateAction> (
@"
class TestClass
{
	event $MyEventHandler evt;
}
", @"
delegate void MyEventHandler (object sender, System.EventArgs e);
class TestClass
{
	event MyEventHandler evt;
}
");
		}
		
		[Test()]
		public void TestCreatePublicDelegate ()
		{
			Test<CreateDelegateAction> (
@"
class TestClass
{
	public event $MyEventHandler evt;
}
", @"
public delegate void MyEventHandler (object sender, System.EventArgs e);
class TestClass
{
	public event MyEventHandler evt;
}
");
		}

	}
}
