// 
// VariableDeclarationStatementTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
	public class VariableDeclarationStatementTests : TestBase
	{
		[Test()]
		public void TestDefaultBehavior ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
@"
class MyTest
{
	public void Test ()
	{
		$var v$
	}
}
");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestDefaultBehaviorInForeach ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
@"
class MyTest
{
	public void Test ()
	{
		$foreach (var v$
	}
}
");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestIntNameProposal ()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
class MyTest
{
	public void Test ()
	{
		$int $
	}
}
");
			Assert.IsNotNull (provider.Find ("i"), "name proposal 'i' not found.");
			Assert.IsNotNull (provider.Find ("j"), "name proposal 'j' not found.");
			Assert.IsNotNull (provider.Find ("k"), "name proposal 'k' not found.");
		}
		
		[Test()]
		public void TestNameProposal ()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
class MyTest
{
	public void Test ()
	{
		$MyTest $
	}
}
");
			Assert.IsNotNull (provider.Find ("myTest"), "name proposal 'myTest' not found.");
			Assert.IsNotNull (provider.Find ("test"), "name proposal 'test' not found.");
		}
		
		[Test()]
		public void TestNameProposalForeach ()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
class MyTest
{
	public void Test ()
	{
		$foreach (MyTest $
	}
}
");
			Assert.IsNotNull (provider.Find ("myTest"), "name proposal 'myTest' not found.");
			Assert.IsNotNull (provider.Find ("test"), "name proposal 'test' not found.");
		}
		
		/// <summary>
		/// Bug 1799 - [New Resolver] Invalid code completion when typing name of variable
		/// </summary>
		[Test()]
		public void TestBug1799 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
@"
class MyTest
{
	public void Test ()
	{
		for (int n=0;n<10;n++) {
			$string d$
		}
	}
}
");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
	}
}

