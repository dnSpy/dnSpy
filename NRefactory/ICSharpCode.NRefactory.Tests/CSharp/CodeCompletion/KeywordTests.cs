// 
// KeywordTests.cs
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
using ICSharpCode.NRefactory.CSharp.Parser;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.CodeCompletion
{
	[TestFixture()]
	public class KeywordTests : TestBase
	{
		[Test()]
		public void CaseKeywordTest ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
@"
class Class
{
	void Test (string t)
	{
		switch (t) {
			$c$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("case"), "keyword 'case' not found.");
		}
		
		[Test()]
		public void CaseKeywordTestCase2 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
@"
class Class
{
	void Test (string t)
	{
		$c$
	}
}
");
	}
		
		[Test()]
		public void CaseKeywordTestCase3 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
@"
class Class
{
void Test (string t)
{
	switch (t) {
		case ""test"":
		$c$
	}
}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("case"), "keyword 'case' not found.");
		}
		
		[Test()]
		public void ModifierKeywordTest ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"
$p$
", provider => {
				Assert.IsNotNull (provider.Find ("public"), "keyword 'public' not found.");
				Assert.IsNotNull (provider.Find ("namespace"), "keyword 'namespace' not found.");
			});
		}
		
		[Test()]
		public void ModifierKeywordTestCase2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"
class Test 
{
	$p$
}
", provider => {
				Assert.IsNotNull (provider.Find ("public"), "keyword 'public' not found.");
				Assert.IsNull (provider.Find ("namespace"), "keyword 'namespace' found.");
			});
		}
		
		[Test()]
		public void GetSetKeywordTest ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"class Test
{
	public int MyProperty {
		$g$
}
", provider => {
				Assert.IsNotNull (provider.Find ("public"), "keyword 'public' not found.");
				Assert.IsNotNull (provider.Find ("get"), "keyword 'get' not found.");
				Assert.IsNotNull (provider.Find ("set"), "keyword 'set' not found.");
			});
		}
		
		[Test()]
		public void GetSetKeywordTestAfterModifier ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"class Test
{
	public int MyProperty {
		internal $g$
}
", provider => {
				Assert.IsNotNull (provider.Find ("get"), "keyword 'get' not found.");
				Assert.IsNotNull (provider.Find ("set"), "keyword 'set' not found.");
			});
		}
		
		[Test()]
		public void AddRemoveKeywordTest ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;
class Test
{
	public event EventHandler MyProperty {
		$g$
}
", (provider) => {
				Assert.AreEqual (2, provider.Count);
				Assert.IsNotNull (provider.Find ("add"), "keyword 'add' not found.");
				Assert.IsNotNull (provider.Find ("remove"), "keyword 'remove' not found.");
			});
		}
		
		[Test()]
		public void IsAsKeywordTest ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;
class Test
{
	public void Method ()
	{
		void TestMe (object o)
		{
			if (o $i$
		}
	}
}
", (provider) => {
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("is"), "keyword 'is' not found.");
			Assert.IsNotNull (provider.Find ("as"), "keyword 'as' not found.");
			});
		}
		
		[Test()]
		public void PublicClassContextTest ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$c$",
provider => {
				Assert.IsNotNull (provider.Find ("class"), "keyword 'class' not found.");
				Assert.IsNotNull (provider.Find ("static"), "keyword 'static' not found.");
				Assert.IsNotNull (provider.Find ("sealed"), "keyword 'sealed' not found.");
				
			});
		}
		
		[Test()]
		public void PublicClassContextTest2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"public $c$",
provider => {
				Assert.IsNotNull (provider.Find ("class"), "keyword 'class' not found.");
				Assert.IsNotNull (provider.Find ("static"), "keyword 'static' not found.");
				Assert.IsNotNull (provider.Find ("sealed"), "keyword 'sealed' not found.");
				
			});
		}
		
		[Test()]
		public void PublicClassContextTestContinuation1 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"public static $c$",
provider => {
				Assert.IsNotNull (provider.Find ("class"), "keyword 'class' not found.");
				
			});
		}
		
		[Test()]
		public void PublicClassContextTestContinuation2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"public sealed $c$",
provider => {
				Assert.IsNotNull (provider.Find ("class"), "keyword 'class' not found.");
				
			});
		}
		
		[Test()]
		public void StatementKeywordTests ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;
class Test
{
	public void MyMethod ()
	{
		$s$
}
", (provider) => {
				Assert.IsNotNull (provider.Find ("bool"), "keyword 'bool' not found.");
				Assert.IsNotNull (provider.Find ("char"), "keyword 'char' not found.");
				Assert.IsNotNull (provider.Find ("byte"), "keyword 'byte' not found.");
				Assert.IsNotNull (provider.Find ("sbyte"), "keyword 'sbyte' not found.");
				Assert.IsNotNull (provider.Find ("int"), "keyword 'int' not found.");
				Assert.IsNotNull (provider.Find ("uint"), "keyword 'uint' not found.");
				Assert.IsNotNull (provider.Find ("short"), "keyword 'short' not found.");
				Assert.IsNotNull (provider.Find ("ushort"), "keyword 'ushort' not found.");
				Assert.IsNotNull (provider.Find ("long"), "keyword 'long' not found.");
				Assert.IsNotNull (provider.Find ("ulong"), "keyword 'ulong' not found.");
				Assert.IsNotNull (provider.Find ("float"), "keyword 'float' not found.");
				Assert.IsNotNull (provider.Find ("double"), "keyword 'double' not found.");
				Assert.IsNotNull (provider.Find ("decimal"), "keyword 'decimal' not found.");
				
				Assert.IsNotNull (provider.Find ("const"), "keyword 'const' not found.");
				Assert.IsNotNull (provider.Find ("dynamic"), "keyword 'dynamic' not found.");
				Assert.IsNotNull (provider.Find ("var"), "keyword 'var' not found.");
				
				Assert.IsNotNull (provider.Find ("do"), "keyword 'do' not found.");
				Assert.IsNotNull (provider.Find ("while"), "keyword 'while' not found.");
				Assert.IsNotNull (provider.Find ("for"), "keyword 'for' not found.");
				Assert.IsNotNull (provider.Find ("foreach"), "keyword 'foreach' not found.");
				
				Assert.IsNotNull (provider.Find ("goto"), "keyword 'goto' not found.");
				Assert.IsNotNull (provider.Find ("break"), "keyword 'break' not found.");
				Assert.IsNotNull (provider.Find ("continue"), "keyword 'continue' not found.");
				Assert.IsNotNull (provider.Find ("return"), "keyword 'return' not found.");
				Assert.IsNotNull (provider.Find ("throw"), "keyword 'throw' not found.");
				
				Assert.IsNotNull (provider.Find ("fixed"), "keyword 'fixed' not found.");
				Assert.IsNotNull (provider.Find ("using"), "keyword 'using' not found.");
				Assert.IsNotNull (provider.Find ("lock"), "keyword 'lock' not found.");
				
				Assert.IsNotNull (provider.Find ("true"), "keyword 'true' not found.");
				Assert.IsNotNull (provider.Find ("false"), "keyword 'false' not found.");
				
				Assert.IsNotNull (provider.Find ("null"), "keyword 'null' not found.");
				
				Assert.IsNotNull (provider.Find ("typeof"), "keyword 'typeof' not found.");
				Assert.IsNotNull (provider.Find ("sizeof"), "keyword 'sizeof' not found.");
				
				Assert.IsNotNull (provider.Find ("from"), "keyword 'from' not found.");
				Assert.IsNotNull (provider.Find ("yield"), "keyword 'yield' not found.");
				
				Assert.IsNotNull (provider.Find ("new"), "keyword 'new' not found.");
			});
		}
		
		[Test()]
		public void VariableDeclarationTestForCase ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;
class Test
{
	public void MyMethod ()
	{
		$for (s$
}
", (provider) => {
				Assert.IsNotNull (provider.Find ("bool"), "keyword 'bool' not found.");
				Assert.IsNotNull (provider.Find ("char"), "keyword 'char' not found.");
				Assert.IsNotNull (provider.Find ("byte"), "keyword 'byte' not found.");
				Assert.IsNotNull (provider.Find ("sbyte"), "keyword 'sbyte' not found.");
				Assert.IsNotNull (provider.Find ("int"), "keyword 'int' not found.");
				Assert.IsNotNull (provider.Find ("uint"), "keyword 'uint' not found.");
				Assert.IsNotNull (provider.Find ("short"), "keyword 'short' not found.");
				Assert.IsNotNull (provider.Find ("ushort"), "keyword 'ushort' not found.");
				Assert.IsNotNull (provider.Find ("long"), "keyword 'long' not found.");
				Assert.IsNotNull (provider.Find ("ulong"), "keyword 'ulong' not found.");
				Assert.IsNotNull (provider.Find ("float"), "keyword 'float' not found.");
				Assert.IsNotNull (provider.Find ("double"), "keyword 'double' not found.");
				Assert.IsNotNull (provider.Find ("decimal"), "keyword 'decimal' not found.");
				
				Assert.IsNotNull (provider.Find ("var"), "keyword 'var' not found.");
			});
		}
		
		[Test()]
		public void VariableDeclarationTestForeachCase ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;
class Test
{
	public void MyMethod ()
	{
		$foreach (s$
}
", (provider) => {
				Assert.IsNotNull (provider.Find ("bool"), "keyword 'bool' not found.");
				Assert.IsNotNull (provider.Find ("char"), "keyword 'char' not found.");
				Assert.IsNotNull (provider.Find ("byte"), "keyword 'byte' not found.");
				Assert.IsNotNull (provider.Find ("sbyte"), "keyword 'sbyte' not found.");
				Assert.IsNotNull (provider.Find ("int"), "keyword 'int' not found.");
				Assert.IsNotNull (provider.Find ("uint"), "keyword 'uint' not found.");
				Assert.IsNotNull (provider.Find ("short"), "keyword 'short' not found.");
				Assert.IsNotNull (provider.Find ("ushort"), "keyword 'ushort' not found.");
				Assert.IsNotNull (provider.Find ("long"), "keyword 'long' not found.");
				Assert.IsNotNull (provider.Find ("ulong"), "keyword 'ulong' not found.");
				Assert.IsNotNull (provider.Find ("float"), "keyword 'float' not found.");
				Assert.IsNotNull (provider.Find ("double"), "keyword 'double' not found.");
				Assert.IsNotNull (provider.Find ("decimal"), "keyword 'decimal' not found.");
				
				Assert.IsNotNull (provider.Find ("var"), "keyword 'var' not found.");
			});
		}
		
		[Test()]
		public void VariableDeclarationTestUsingCase ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;
class Test
{
	public void MyMethod ()
	{
		$foreach (s$
}
", (provider) => {
				Assert.IsNotNull (provider.Find ("bool"), "keyword 'bool' not found.");
				Assert.IsNotNull (provider.Find ("char"), "keyword 'char' not found.");
				Assert.IsNotNull (provider.Find ("byte"), "keyword 'byte' not found.");
				Assert.IsNotNull (provider.Find ("sbyte"), "keyword 'sbyte' not found.");
				Assert.IsNotNull (provider.Find ("int"), "keyword 'int' not found.");
				Assert.IsNotNull (provider.Find ("uint"), "keyword 'uint' not found.");
				Assert.IsNotNull (provider.Find ("short"), "keyword 'short' not found.");
				Assert.IsNotNull (provider.Find ("ushort"), "keyword 'ushort' not found.");
				Assert.IsNotNull (provider.Find ("long"), "keyword 'long' not found.");
				Assert.IsNotNull (provider.Find ("ulong"), "keyword 'ulong' not found.");
				Assert.IsNotNull (provider.Find ("float"), "keyword 'float' not found.");
				Assert.IsNotNull (provider.Find ("double"), "keyword 'double' not found.");
				Assert.IsNotNull (provider.Find ("decimal"), "keyword 'decimal' not found.");
				
				Assert.IsNotNull (provider.Find ("var"), "keyword 'var' not found.");
			});
		}
		
		[Test()]
		public void VariableDeclarationTestMethodDeclarationCase ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;
class Test
{
	$public void MyMethod (s$
}
", (provider) => {
				Assert.IsNotNull (provider.Find ("bool"), "keyword 'bool' not found.");
				Assert.IsNotNull (provider.Find ("char"), "keyword 'char' not found.");
				Assert.IsNotNull (provider.Find ("byte"), "keyword 'byte' not found.");
				Assert.IsNotNull (provider.Find ("sbyte"), "keyword 'sbyte' not found.");
				Assert.IsNotNull (provider.Find ("int"), "keyword 'int' not found.");
				Assert.IsNotNull (provider.Find ("uint"), "keyword 'uint' not found.");
				Assert.IsNotNull (provider.Find ("short"), "keyword 'short' not found.");
				Assert.IsNotNull (provider.Find ("ushort"), "keyword 'ushort' not found.");
				Assert.IsNotNull (provider.Find ("long"), "keyword 'long' not found.");
				Assert.IsNotNull (provider.Find ("ulong"), "keyword 'ulong' not found.");
				Assert.IsNotNull (provider.Find ("float"), "keyword 'float' not found.");
				Assert.IsNotNull (provider.Find ("double"), "keyword 'double' not found.");
				Assert.IsNotNull (provider.Find ("decimal"), "keyword 'decimal' not found.");
				
				Assert.IsNull (provider.Find ("var"), "keyword 'var' found.");
			});
		}
		
		
		[Test()]
		public void ForeachInKeywordTest ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;
class Test
{
	public void Method ()
	{
		$foreach (var o i$
	}
}
", (provider) => {
			// Either empty list or in - both behaviours are ok.
			if (provider.Count > 0)
				Assert.IsNotNull (provider.Find ("in"), "keyword 'in' not found.");
			});
		}
	}
}

