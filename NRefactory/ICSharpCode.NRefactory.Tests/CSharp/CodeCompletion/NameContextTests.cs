// 
// NameContextTests.cs
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

namespace ICSharpCode.NRefactory.CSharp.CodeCompletion
{
	[TestFixture()]
	public class NameContextTests : TestBase
	{
		[Test()]
		public void TestNamespaceName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$namespace n$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		[Test()]
		public void TestNamespaceNameCase2 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$namespace $");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		[Ignore("Parser bug.")]
		[Test()]
		public void TestNamespaceNameCase3 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$namespace Foo.b$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		[Test()]
		public void TestNamespaceNameCase4 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$namespace Foo.$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		[Test()]
		public void TestClassName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$class n$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestStructName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$struct n$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestInterfaceName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$interface n$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestEnumName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$enum n$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestDelegateName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$delegate void n$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestClassTypeParameter ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$class MyClass<T$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestFieldName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	$int f$
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestParameterName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	$void SomeMethod(int f$
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestLocalVariableName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	void Test() 
	{
		$int f$
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test()]
		public void TestForeachLocalVariableName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	void Test() 
	{
		$foreach (int f$
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		[Test()]
		public void TestForLoopLocalVariableName ()
		{

			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	void Test() 
	{
		$for (int f$
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		[Test()]
		public void TestCatchExceptionName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	void Test() 
	{
		$try {
		} catch (Exception e$
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
			/// <summary>
		/// Bug 2198 - Typing generic argument to a class/method pops up type completion window
		/// </summary>
		[Test()]
		public void TestBug2198 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$class Klass <T$", provider => {
				Assert.AreEqual (0, provider.Count, "provider needs to be empty");
			});
		}
		
		[Test()]
		public void TestBug2198Case2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$class Klass { void Test<T$", provider => {
				Assert.AreEqual (0, provider.Count, "provider needs to be empty");
			});
		}
	}
}

