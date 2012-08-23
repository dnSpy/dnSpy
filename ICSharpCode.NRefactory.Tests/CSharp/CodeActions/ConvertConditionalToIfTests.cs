// 
// ConvertConditionalToIfTests.cs
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

using ICSharpCode.NRefactory.CSharp.Refactoring.CodeActions;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class ConvertConditionalToIfTests : ContextActionTestBase
	{
		[Test]
		public void TestAssignment ()
		{
			Test<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod (int i)
	{
		int a;
		a = i > 0 $? 0 : 1;
	}
}", @"
class TestClass
{
	void TestMethod (int i)
	{
		int a;
		if (i > 0)
			a = 0;
		else
			a = 1;
	}
}");
			Test<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod (int i)
	{
		int a;
		a += i > 0 $? 0 : 1;
	}
}", @"
class TestClass
{
	void TestMethod (int i)
	{
		int a;
		if (i > 0)
			a += 0;
		else
			a += 1;
	}
}");
		}

		[Test]
		public void TestVariableInitializer ()
		{
			const string correctOutput = @"
class TestClass
{
	void TestMethod (int i)
	{
		int a;
		if (i > 0)
			a = 0;
		else
			a = 1;
	}
}";
			Test<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod (int i)
	{
		int a = i > 0 $? 0 : 1;
	}
}", correctOutput);
			Test<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod (int i)
	{
		int a = (i > 0 $? 0 : 1);
	}
}", correctOutput);
		}

		[Test]
		public void TestComplexVariableDeclaration ()
		{
			Test<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod (int i)
	{
		int a, b = i > 0 $? 0 : 1, c;
	}
}", @"
class TestClass
{
	void TestMethod (int i)
	{
		int a, b, c;
		if (i > 0)
			b = 0;
		else
			b = 1;
	}
}");
		}
		
		[Test]
		public void TestInvalidAssignmentContext ()
		{
			TestWrongContext<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod (int i)
	{
		int a;
		a = 1 + (i > 0 $? 0 : 1);
	}
}");
		}

		[Test]
		public void TestInvalidInitializerContext ()
		{
			TestWrongContext<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod (int i)
	{
		int a = 1 + (i > 0 $? 0 : 1);
	}
}");
		}
		
		[Test]
		public void TestFor ()
		{
			TestWrongContext<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod ()
	{
		int a;
		for (int i = 0; i < 10; i++, a = i > 10 $? 1 : 0) ;
	}
}");
			TestWrongContext<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod ()
	{
		int a;
		int i;
		for (i = 0, a = i > 10 $? 1 : 0; i < 10; i++) ;
	}
}");
			TestWrongContext<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod ()
	{
		int j;
		for (int i = 0, a = i > 10 $? 1 : 0; i < 10; i++) ;
	}
}");
		}

		[Test]
		public void TestUsing ()
		{
			TestWrongContext<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.IDisposable a;
		int i = 0;
		using (a = i > 0 $? null : null) {
		}
	}
}");
			TestWrongContext<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		using (System.IDisposable a = i > 0 $? null : null) {
		}
	}
}");
		}

		[Test]
		public void TestEmbeddedStatement ()
		{
			Test<ConvertConditionalToIfAction> (@"
class TestClass
{
	void TestMethod (int i)
	{
		int a;
		if (i < 10)
			a = i > 0 $? 0 : 1;
	}
}", @"
class TestClass
{
	void TestMethod (int i)
	{
		int a;
		if (i < 10)
			if (i > 0)
				a = 0;
			else
				a = 1;
	}
}");
		}

		[Test]
		public void TestReturn ()
		{
			Test<ConvertConditionalToIfAction> (@"
class TestClass
{
	int TestMethod (int i)
	{
		return i > 0 $? 1 : 0;
	}
}", @"
class TestClass
{
	int TestMethod (int i)
	{
		if (i > 0)
			return 1;
		else
			return 0;
	}
}");
		}
	}
}
