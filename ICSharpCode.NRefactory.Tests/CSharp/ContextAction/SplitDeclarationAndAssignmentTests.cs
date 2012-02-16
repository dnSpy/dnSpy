// 
// SplitDeclarationAndAssignmentTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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

namespace ICSharpCode.NRefactory.CSharp.ContextActions
{
	[TestFixture]
	public class SplitDeclarationAndAssignmentTests : ContextActionTestBase
	{
		[Test()]
		public void TestSimpleExpression ()
		{
			string result = RunContextAction (
				new SplitDeclarationAndAssignment (),
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		int $myInt = 5 + 3 * (2 - 10);" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}"
			);
			
			Assert.AreEqual (
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		int myInt;" + Environment.NewLine +
				"		myInt = 5 + 3 * (2 - 10);" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}
		
		[Test()]
		public void TestVarType ()
		{
			string result = RunContextAction (
				new SplitDeclarationAndAssignment (),
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		var $aVar = this;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}"
			);
			Assert.AreEqual (
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		var aVar;" + Environment.NewLine +
				"		aVar = this;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}
		
		[Test()]
		public void TestForStatement ()
		{
			string result = RunContextAction (
				new SplitDeclarationAndAssignment (),
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		for (int $i = 1; i < 10; i++) {}" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}"
			);
			Assert.AreEqual (
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		int i;" + Environment.NewLine +
				"		for (i = 1; i < 10; i++) {}" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}
	}
}

