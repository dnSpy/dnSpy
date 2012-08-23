// 
// CreateOverloadWithoutParameterTests.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
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

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class CreateOverloadWithoutParameterTests : ContextActionTestBase
	{
		[Test]
		public void Test ()
		{
			Test<CreateOverloadWithoutParameterAction> (@"
class Test
{
	void TestMethod (int $i)
	{
	}
}", @"
class Test
{
	void TestMethod ()
	{
		TestMethod (0);
	}
	void TestMethod (int i)
	{
	}
}");
		}

		[Test]
		public void TestByRefParameter ()
		{
			Test<CreateOverloadWithoutParameterAction> (
				@"class Test
{
	void TestMethod (ref int $i)
	{
	}
}",
				@"class Test
{
	void TestMethod ()
	{
		int i = 0;
		TestMethod (ref i);
	}
	void TestMethod (ref int i)
	{
	}
}");
		}
		
		[Test]
		public void TestOutParameter ()
		{
			Test<CreateOverloadWithoutParameterAction> (
				@"class Test
{
	void TestMethod (out int $i)
	{
	}
}",
				@"class Test
{
	void TestMethod ()
	{
		int i;
		TestMethod (out i);
	}
	void TestMethod (out int i)
	{
	}
}");
		}
		
		[Test]
		public void TestDefaultValue ()
		{
			TestDefaultValue ("object", "null");
			TestDefaultValue ("dynamic", "null");
			TestDefaultValue ("int?", "null");
			TestDefaultValue ("bool", "false");
			TestDefaultValue ("double", "0");
			TestDefaultValue ("char", "'\\0'");
			TestDefaultValue ("System.DateTime", "new System.DateTime ()");

			Test<CreateOverloadWithoutParameterAction> (@"
class Test
{
	void TestMethod<T> (T $i)
	{
	}
}", @"
class Test
{
	void TestMethod<T> ()
	{
		TestMethod (default(T));
	}
	void TestMethod<T> (T i)
	{
	}
}");
		}

		void TestDefaultValue (string type, string expectedValue)
		{
			Test<CreateOverloadWithoutParameterAction> (@"
class Test
{
	void TestMethod (" + type + @" $i)
	{
	}
}", @"
class Test
{
	void TestMethod ()
	{
		TestMethod (" + expectedValue + @");
	}
	void TestMethod (" + type + @" i)
	{
	}
}");
		}

		[Test]
		public void TestOptionalParameter ()
		{
			TestWrongContext<CreateOverloadWithoutParameterAction> (
				@"class Test
{
	void TestMethod (int $i = 0)
	{
	}
}");
		}

		[Test]
		public void TestExistingMethod ()
		{
			TestWrongContext<CreateOverloadWithoutParameterAction> (
				@"class Test
{
	void TestMethod (int c)
	{
	}
	void TestMethod (int a, int $b)
	{
	}
}");
			TestWrongContext<CreateOverloadWithoutParameterAction> (
				@"class Test
{
	void TestMethod <T> (T c)
	{
	}
	void TestMethod <T> (T a, T $b)
	{
	}
}");
		}
	
		[Test]
		public void TestExplicitImpl ()
		{
			TestWrongContext<CreateOverloadWithoutParameterAction> (
				@"
interface ITest
{
	void Test (int a, int b);
}
class Test : ITest
{
	void ITest.Test (int a, int $b)
	{
	}
}");
		}
	}
}
