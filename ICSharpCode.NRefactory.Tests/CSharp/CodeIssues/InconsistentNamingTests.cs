// 
// InconsistentNamingTests.cs
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
using ICSharpCode.NRefactory.CSharp.CodeActions;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class InconsistentNamingTests : InspectionActionTestBase
	{
		void CheckNaming (string input, string output, bool shouldBeEmpty = false)
		{
			TestRefactoringContext context;
			var issues = GetIssues (new InconsistentNamingIssue (), input, out context);
			if (shouldBeEmpty) {
				Assert.AreEqual (0, issues.Count ());
				return;
			}
			Assert.Greater (issues.Count, 0);
			CheckFix (context, issues [0], output);
		}

		[Test]
		public void TestNamespaceName ()
		{
			var input = @"namespace anIssue {}";
			var output = @"namespace AnIssue {}";
			CheckNaming (input, output);
		}
		
		[Test]
		public void TestClassName ()
		{
			var input = @"public class anIssue {}";
			var output = @"public class AnIssue {}";
			CheckNaming (input, output);
		}

		[Test]
		public void TestAttributeName ()
		{
			var input = @"public class test : System.Attribute {}";
			var output = @"public class TestAttribute : System.Attribute {}";
			CheckNaming (input, output);
		}
		
		[Test]
		public void TestEventArgsName ()
		{
			var input = @"public class test : System.EventArgs {}";
			var output = @"public class TestEventArgs : System.EventArgs {}";
			CheckNaming (input, output);
		}

		[Test]
		public void TestException ()
		{
			var input = @"class test : System.Exception {}";
			var output = @"class TestException : System.Exception {}";
			CheckNaming (input, output);
		}
		
		[Test]
		public void TestStructName ()
		{
			var input = @"public struct anIssue {}";
			var output = @"public struct AnIssue {}";
			CheckNaming (input, output);
		}

		[Test]
		public void TestInterfaceName ()
		{
			var input = @"public interface anIssue {}";
			var output = @"public interface IAnIssue {}";
			CheckNaming (input, output);
		}

		[Test]
		public void TestEnumName ()
		{
			var input = @"public enum anIssue {}";
			var output = @"public enum AnIssue {}";
			CheckNaming (input, output);
		}

		[Test]
		public void TestDelegateName ()
		{
			var input = @"public delegate void anIssue ();";
			var output = @"public delegate void AnIssue ();";
			CheckNaming (input, output);
		}

//		[Test]
//		public void TestPrivateFieldName ()
//		{
//			var input = @"class AClass { int Field; }";
//			var output = @"class AClass { int field; }";
//			CheckNaming (input, output);
//		}
		
//		[Test]
//		public void TestUnderscoreFieldName ()
//		{
//			var input = @"class AClass { int _Field; }";
//			var output = @"class AClass { int _field; }";
//			CheckNaming (input, output);
//		}
		
		[Test]
		public void TestPublicFieldName ()
		{
			var input = @"class AClass { public int field; }";
			var output = @"class AClass { public int Field; }";
			CheckNaming (input, output);
		}
		
//		[Test]
//		public void TestPrivateConstantFieldName ()
//		{
//			var input = @"class AClass { const int field = 5; }";
//			var output = @"class AClass { const int Field = 5; }";
//			CheckNaming (input, output);
//		}
		
		[Test]
		public void TestPublicReadOnlyFieldName ()
		{
			var input = @"class AClass { public readonly int field; }";
			var output = @"class AClass { public readonly int Field; }";
			CheckNaming (input, output);
		}
		
		[Test]
		public void TestPrivateStaticReadOnlyFieldName ()
		{
			var input = @"class AClass { static readonly int Field; }";
			var output = @"class AClass { static readonly int Field; }";
			CheckNaming (input, output, true);
		}
		
		[Test]
		public void TestPrivateStaticReadOnlyFieldNameCase2 ()
		{
			var input = @"class AClass { static readonly int field; }";
			var output = @"class AClass { static readonly int field; }";
			CheckNaming (input, output, true);
		}

//		[Test]
//		public void TestPrivateStaticFieldName ()
//		{
//			var input = @"class AClass { static int Field; }";
//			var output = @"class AClass { static int field; }";
//			CheckNaming (input, output);
//		}

		[Test]
		public void TestPublicStaticReadOnlyFieldName ()
		{
			var input = @"class AClass { public static readonly int field = 5; }";
			var output = @"class AClass { public static readonly int Field = 5; }";
			CheckNaming (input, output);
		}
		
//		[Test]
//		public void TestPrivateReadOnlyFieldName ()
//		{
//			var input = @"class AClass { readonly int Field; }";
//			var output = @"class AClass { readonly int field; }";
//			CheckNaming (input, output);
//		}
		
		[Test]
		public void TestPublicConstantFieldName ()
		{
			var input = @"class AClass { public const int field = 5; }";
			var output = @"class AClass { public const int Field = 5; }";
			CheckNaming (input, output);
		}
		
		[Test]
		public void TestMethodName ()
		{
			var input = @"class AClass { public int method () {} }";
			var output = @"class AClass { public int Method () {} }";
			CheckNaming (input, output);
		}

		[Test]
		public void TestPropertyName ()
		{
			var input = @"class AClass { public int property { get; set; } }";
			var output = @"class AClass { public int Property { get; set; } }";
			CheckNaming (input, output);
		}

		[Test]
		public void TestParameterName ()
		{
			var input = @"class AClass { int Method (int Param) {} }";
			var output = @"class AClass { int Method (int param) {} }";
			CheckNaming (input, output);
		}

		[Test]
		public void TestTypeParameterName ()
		{
			var input = @"struct Str<K> { K k;}";
			var output = @"struct Str<TK> { TK k;}";
			CheckNaming (input, output);
		}


		[Test]
		public void TestOverrideMembers ()
		{
			var input = @"
class Base { public virtual int method (int Param) {} }
class MyClass : Base { public override int method (int Param) {} }";
			TestRefactoringContext context;
			var issues = GetIssues (new InconsistentNamingIssue (), input, out context);
			Assert.AreEqual (2, issues.Count);
		}

		[Test]
		public void TestOverrideMembersParameterNameCaseMismatch ()
		{
			var input = @"
class Base { public virtual int Method (int param) {} }
class MyClass : Base { public override int Method (int Param) {} }";
			TestRefactoringContext context;
			var issues = GetIssues (new InconsistentNamingIssue (), input, out context);
			foreach (var issue in issues)
				Console.WriteLine(issue.Description);
			Assert.AreEqual (1, issues.Count);
		}
	}

	[TestFixture]
	public class WordParserTests
	{
		[Test]
		public void TestPascalCaseWords ()
		{
			var result = WordParser.BreakWords ("SomeVeryLongName");
			Assert.AreEqual (4, result.Count);
			Assert.AreEqual ("Some", result [0]);
			Assert.AreEqual ("Very", result [1]);
			Assert.AreEqual ("Long", result [2]);
			Assert.AreEqual ("Name", result [3]);
		}

		[Test]
		public void TestCamelCaseWords ()
		{
			var result = WordParser.BreakWords ("someVeryLongName");
			Assert.AreEqual (4, result.Count);
			Assert.AreEqual ("some", result [0]);
			Assert.AreEqual ("Very", result [1]);
			Assert.AreEqual ("Long", result [2]);
			Assert.AreEqual ("Name", result [3]);
		}

		[Test]
		public void TestUpperCaseSubWord ()
		{
			var result = WordParser.BreakWords ("someVeryLongXMLName");
			Assert.AreEqual (5, result.Count);
			Assert.AreEqual ("some", result [0]);
			Assert.AreEqual ("Very", result [1]);
			Assert.AreEqual ("Long", result [2]);
			Assert.AreEqual ("XML", result [3]);
			Assert.AreEqual ("Name", result [4]);
		}

		[Test]
		public void TestUnderscore ()
		{
			var result = WordParser.BreakWords ("some_Very_long_NAME");
			Assert.AreEqual (4, result.Count);
			Assert.AreEqual ("some", result [0]);
			Assert.AreEqual ("Very", result [1]);
			Assert.AreEqual ("long", result [2]);
			Assert.AreEqual ("NAME", result [3]);
		}
	}
}

