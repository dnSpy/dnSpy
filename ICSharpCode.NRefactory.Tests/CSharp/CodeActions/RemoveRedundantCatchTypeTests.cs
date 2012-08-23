//
// RemoveRedundantCatchTypeTests.cs
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

using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{

	[TestFixture]
	public class RemoveRedundantCatchTypeTests : ContextActionTestBase
	{
		[Test]
		public void RemovesSimpleExceptionMatch()
		{
			Test<RemoveRedundantCatchTypeAction>(@"
class TestClass
{
	public void F()
	{
		try {
		}
		catch $(System.Exception) {
		}
	}
}", @"
class TestClass
{
	public void F()
	{
		try {
		}
		catch {
		}
	}
}");
		}

		[Test]
		public void PreservesBody()
		{
			Test<RemoveRedundantCatchTypeAction>(@"
class TestClass
{
	public void F()
	{
		try {
		}
		catch $(System.Exception e) {
			System.Console.WriteLine (""Hi"");
		}
	}
}", @"
class TestClass
{
	public void F()
	{
		try {
		}
		catch {
			System.Console.WriteLine (""Hi"");
		}
	}
}");
		}

		[Test]
		[Ignore("Needs whitespace ast nodes")]
		public void PreservesWhitespaceInBody()
		{
			Test<RemoveRedundantCatchTypeAction>(@"
class TestClass
{
	public void F()
	{
		try {
		}
		catch $(System.Exception e) {

		}
	}
}", @"
class TestClass
{
	public void F()
	{
		try {
		}
		catch {

		}
	}
}");
		}

		[Test]
		public void IgnoresReferencedExceptionMatch()
		{
			TestWrongContext<RemoveRedundantCatchTypeAction>(@"
class TestClass
{
	public void F()
	{	
		try {
		}
		catch $(System.Exception e) {
			System.Console.WriteLine (e);
		}
	}
}");
		}
	}
}

