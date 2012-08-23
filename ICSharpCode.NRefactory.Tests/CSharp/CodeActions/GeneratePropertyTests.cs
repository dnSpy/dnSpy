// 
// GeneratePropertyTests.cs
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

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class GeneratePropertyTests : ContextActionTestBase
	{
		[Test()]
		public void Test ()
		{
			string result = RunContextAction (
				new GeneratePropertyAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	int $myField;" + Environment.NewLine +
					"}"
			);
			
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	public int MyField {" + Environment.NewLine +
				"		get {" + Environment.NewLine +
				"			return myField;" + Environment.NewLine +
				"		}" + Environment.NewLine +
				"		set {" + Environment.NewLine +
				"			myField = value;" + Environment.NewLine +
				"		}" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"	int myField;" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void HandlesFieldsWhichMatchThePropertyNameTest ()
		{
			Test<GeneratePropertyAction> (
@"
class TestClass
{
	public int $MyField;
}",
@"
class TestClass
{
	public int MyField {
		get {
			return myField;
		}
		set {
			myField = value;
		}
	}
	public int myField;
}");
		}

		[Test()]
		public void HandlesMultiDeclarationTest ()
		{
			Test<GeneratePropertyAction> (
@"
class TestClass
{
	int $MyField, myOtherField;
}",
@"
class TestClass
{
	public int MyField {
		get {
			return myField;
		}
		set {
			myField = value;
		}
	}
	int myField, myOtherField;
}");
		}
		
		[Test]
		public void CannotGeneratePropertyForReadOnlyField()
		{
			TestWrongContext (
				new GeneratePropertyAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	readonly int $myField;" + Environment.NewLine +
					"}"
			);
		}
		
		[Test]
		public void CannotGeneratePropertyForConst()
		{
			TestWrongContext (
				new GeneratePropertyAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	const int $myField = 0;" + Environment.NewLine +
					"}"
			);
		}
	}
}