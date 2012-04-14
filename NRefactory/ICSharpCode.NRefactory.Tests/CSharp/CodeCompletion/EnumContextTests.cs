// 
// EnumContextTests.cs
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
	[TestFixture()]
	public class EnumContextTests : TestBase
	{
		/// <summary>
		/// Bug 2142 - Enum member list triggers completion
		/// </summary>
		[Test()]
		public void Test2142 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"enum Name {
	$p$
}
", provider => {
				Assert.AreEqual (0, provider.Count);
			});
		}
		
		[Test()]
		public void Test2142Case2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"enum Name {
	Foo,
	Bar,
	$p$
}
", provider => {
				Assert.AreEqual (0, provider.Count);
			});
		}
		
		
		[Test()]
		public void TestEnumAssignment ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"enum Name {
	Member
	$, B = M$
}
", provider => {
				Assert.IsNotNull (provider.Find ("Member"), "value 'Member' not found.");
				Assert.IsNotNull (provider.Find ("Name"), "type 'Name' not found.");
			});
		}
		
		[Test()]
		public void TestEnumFlags ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;
[Flags]
enum Name {
	Flag1,
	Flag2,
	Combined = Flag1 $| F$
}
", provider => {
				Assert.IsNotNull (provider.Find ("Flag1"), "value 'Flag1' not found.");
				Assert.IsNotNull (provider.Find ("Flag2"), "value 'Flag2' not found.");
				Assert.IsNotNull (provider.Find ("Name"), "type 'Name' not found.");
			});
		}
		
		
		[Test()]
		public void TestEnumInitializerContinuation()
		{
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;
[Flags]
enum Name {
	Flag1,
	Flag2,
	Combined $= Name.$
}
", provider => {
				Assert.IsNotNull(provider.Find("Flag1"), "value 'Flag1' not found.");
				Assert.IsNotNull(provider.Find("Flag2"), "value 'Flag2' not found.");
			});
		}

		[Test()]
		public void TestEnumBaseTypes()
		{
			string[] integralTypes = { "byte", "sbyte", "short", "int", "long", "ushort", "uint", "ulong" };
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;
enum Name : $b$
{
	Flag1
}
", provider => {
				foreach (var type in integralTypes)
					Assert.IsNotNull(provider.Find(type), "value '" + type + "' not found.");
				Assert.IsNull(provider.Find("char"), "type 'char' found.");
			});
		}

		[Test()]
		public void TestEnumBaseTypesAutoPopup()
		{
			string[] integralTypes = { "byte", "sbyte", "short", "int", "long", "ushort", "uint", "ulong" };
			var provider = CodeCompletionBugTests.CreateProvider(
@"using System;
$enum Name : $
");
			foreach (var type in integralTypes)
				Assert.IsNotNull(provider.Find(type), "value '" + type + "' not found.");
			Assert.IsNull(provider.Find("char"), "type 'char' found.");
		}
		
	}
}
