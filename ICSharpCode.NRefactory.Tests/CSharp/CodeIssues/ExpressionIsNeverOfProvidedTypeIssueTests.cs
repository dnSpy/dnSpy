// 
// ExpressionIsNeverOfProvidedTypeIssueTests.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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

using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class ExpressionIsNeverOfProvidedTypeIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestClass ()
		{
			var input = @"
class AnotherClass { }
class TestClass
{
	void TestMethod (AnotherClass x)
	{
		if (x is TestClass) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 1);
		}

		[Test]
		public void TestClassNoIssue ()
		{
			var input = @"
interface ITest { }
class TestClass
{
	void TestMethod (object x)
	{
		if (x is ITest) ;
		if (x is TestClass) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 0);
		}

		[Test]
		public void TestSealedClass ()
		{
			var input = @"
interface ITest { }
sealed class TestClass
{
	void TestMethod (TestClass x)
	{
		if (x is ITest) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 1);
		}

		[Test]
		public void TestNull ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		if (null is object) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 1);
		}

		[Test]
		public void TestClassIsTypeParameter ()
		{
			var input = @"
class TestClass2 { }
class TestClass
{
	void TestMethod<T, T2> (TestClass x) where T : TestClass2 where T2 : struct
	{
		if (x is T) ;
		if (x is T2) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 2);
		}

		[Test]
		public void TestClassIsTypeParameter2 ()
		{
			var input = @"
interface ITest { }
class TestBase { }
class TestClass2 : TestClass { }
class TestClass : TestBase
{
	void TestMethod<T, T2, T3> (TestClass x) where T : TestBase where T2 : ITest where T3 : TestClass2
	{
		if (x is T3) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 0);
		}

		[Test]
		public void TestStructIsTypeParameter ()
		{
			var input = @"
interface ITest { }
struct TestStruct : ITest { }
class TestClass
{
	void TestMethod<T> (TestStruct x) where T : ITest
	{
		if (x is T) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 0);
		}

		[Test]
		public void TestStructIsTypeParameter2 ()
		{
			var input = @"
struct TestStruct { }
class TestClass
{
	void TestMethod<T> (TestStruct x) where T : class
	{
		if (x is T) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 1);
		}

		[Test]
		public void TestTypeParameter ()
		{
			var input = @"
class TestClass2 { }
class TestClass
{
	void TestMethod<T> (T x) where T : TestClass2
	{
		if (x is TestClass) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 1);
		}

		[Test]
		public void TestTypeParameter2 ()
		{
			var input = @"
class TestClass
{
	void TestMethod<T> (T x) where T : struct
	{
		if (x is TestClass) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 1);
		}

		[Test]
		public void TestTypeParameter3 ()
		{
			var input = @"
interface ITest { }
class TestClass
{
	void TestMethod<T> (T x) where T : ITest, new()
	{
		if (x is TestClass) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 0);
		}

		[Test]
		public void TestTypeParameter4 ()
		{
			var input = @"
interface ITest { }
sealed class TestClass
{
	void TestMethod<T> (T x) where T : ITest
	{
		if (x is TestClass) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 1);
		}

		[Test]
		public void TestTypeParameter5 ()
		{
			var input = @"
sealed class TestClass
{
	public TestClass (int i) { }
	void TestMethod<T> (T x) where T : new()
	{
		if (x is TestClass) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 1);
		}

		[Test]
		public void TestTypeParameterIsTypeParameter ()
		{
			var input = @"
class TestClass2 { }
class TestClass
{
	void TestMethod<T, T2> (T x) where T : TestClass where T2 : TestClass2
	{
		if (x is T2) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 1);
		}

		[Test]
		public void TestTypeParameterIsTypeParameter2 ()
		{
			var input = @"
interface ITest { }
class TestClass
{
	void TestMethod<T, T2> (T x) where T : TestClass where T2 : ITest, new()
	{
		if (x is T2) ;
	}
}";
			Test<ExpressionIsNeverOfProvidedTypeIssue> (input, 0);
		}
	}
}
