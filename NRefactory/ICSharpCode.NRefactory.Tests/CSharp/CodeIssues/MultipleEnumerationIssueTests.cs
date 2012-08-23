// 
// MultipleEnumerationIssueTests.cs
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
	public class MultipleEnumerationIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestVariableInvocation ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod ()
	{
		IEnumerable<object> e = null;
		var type = e.GetType();
		var x = e.First ();
		var y = e.Count ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test] 
		public void TestVariableForeach ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod ()
	{
		IEnumerable<object> e = null;
		foreach (var x in e) ;
		foreach (var y in e) ;
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestVariableMixed ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod ()
	{
		IEnumerable<object> e = null;
		foreach (var x in e) ;
		var y = e.Count ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestParameter ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod (IEnumerable<object> e)
	{
		foreach (var x in e) ;
		var y = e.Count ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestObjectMethodInvocation ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod ()
	{
		IEnumerable<object> e;
		var a = e.GetType ();
		var b = e.ToString ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestIf ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod (int i)
	{
		IEnumerable<object> e;
		if (i > 0) {
			var a = e.Count ();
		} else {
			var b = e.First ();
			var c = e.Count ();
		}
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestIf2 ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod (int i)
	{
		IEnumerable<object> e;
		if (i > 0) {
			var a = e.Count ();
		} else {
			var b = e.First ();
		}
		var c = e.Count ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 3);
		}

		[Test]
		public void TestIf3 ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod (int i)
	{
		IEnumerable<object> e;
		if (i > 0) {
			var a = e.Count ();
		} else {
			var b = e.First ();
		}
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestFor ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod ()
	{
		IEnumerable<object> e;
		for (int i = 0; i < 10; i++) {
			var a = e.Count ();
		}
	}
}";
			Test<MultipleEnumerationIssue> (input, 1);
		}

		[Test]
		public void TestWhile ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod ()
	{
		IEnumerable<object> e;
		int i;
		while (i > 1) {
			var a = e.Count ();
		}
	}
}";
			Test<MultipleEnumerationIssue> (input, 1);
		}

		[Test]
		public void TestWhile2 ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod ()
	{
		IEnumerable<object> e;
		int i;
		while (i > e.Count ()) {
		}
	}
}";
			Test<MultipleEnumerationIssue> (input, 1);
		}

		[Test]
		public void TestWhile3 ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod ()
	{
		IEnumerable<object> e;
		int i;
		object x;
		while (true) {
			if (i > 1) {
				x = e.First ();
				break;
			} 
		}
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestWhile4 ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	IEnumerable<object> GetEnum () { }
	void TestMethod (int i)
	{
		IEnumerable<object> e = GetEnum ();
		var a1 = e.First ();
		while ((e = GetEnum ()) != null) {
			var a2 = e.First ();
		}
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestDo ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	IEnumerable<object> GetEnum () { }
	void TestMethod (int i)
	{
		IEnumerable<object> e = GetEnum ();
		var a1 = e.First ();
		do {
			var a2 = e.First ();
		} while ((e = GetEnum ()) != null);
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestDo2 ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	IEnumerable<object> GetEnum () { }
	void TestMethod (int i)
	{
		IEnumerable<object> e = GetEnum ();
		do {
			var a2 = e.First ();
		} while ((e = GetEnum ()) != null);
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestLambda ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod ()
	{
		IEnumerable<object> e;
		Action a = () => {
			var x = e.Count ();
			var y = e.Count ();
		};
		var z = e.Count ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestLambda2 ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void Test (object a, object b) { }
	void TestMethod ()
	{
		IEnumerable<object> e;
		Action a = () => Test(e.First (), e.Count ());
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestLambda3 ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void Test (object a, Action b) { }
	void TestMethod ()
	{
		IEnumerable<object> e;
		Test(e.First (), () => e.Count ());
		e = null;
		var x = e.First ();
		Action a = () => e.Count();
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestLambda4 ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void Test (object a, object b) { }
	void TestMethod ()
	{
		IEnumerable<object> e;
		Action a = () => Test(e.ToString (), e.ToString ());
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestConditionalExpression ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod (int i)
	{
		IEnumerable<object> e;
		var a = i > 0 ? e.First () : e.FirstOrDefault ();
		Action b = () => i > 0 ? e.First () : e.FirstOrDefault ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}
		[Test]
		public void TestConditionalExpression2 ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod (int i)
	{
		IEnumerable<object> e;
		var a = i > 0 ? e.First () : new object ();
		var b = e.First ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestConstantConditionalExpression ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod (int i)
	{
		IEnumerable<object> e;
		var a = 1 > 2 ? e.First () : new object ();
		var b = e.First ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestAssignmentInConditionalExpression ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	IEnumerable<object> GetEnum () { }
	void TestMethod (int i)
	{
		IEnumerable<object> e;
		var x1 = e.First ();
		var a = i > 0 ? e = GetEnum () : GetEnum ();
		var x2 = e.First ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestAssignmentInConditionalExpression2 ()
		{
			var input = @"
using System;
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	IEnumerable<object> GetEnum () { }
	void TestMethod (int i)
	{
		IEnumerable<object> e;
		var x1 = e.First ();
		var a = i > 0 ? e = GetEnum () : e = GetEnum ();
		var x2 = e.First ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestAssignment ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod (IEnumerable<object> e)
	{
		foreach (var x in e) ;
		e = null;
		var y = e.Count ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestAssignment2 ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod (IEnumerable<object> e)
	{
		foreach (var x in e) ;
		e = null;
		var y = e.Count ();
		e = null;
		var a = e.First ();
		var b = e.First ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestNoIssue ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void TestMethod (IEnumerable<object> e)
	{
		foreach (var x in e) ;
		IEnumerable<object> e2;
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestExpression ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	int Test (params object[] args) { }
	void TestMethod ()
	{
		IEnumerable<object> e = null;
		var type = e.GetType();
		var x = Test (e.First (), e.Count ());
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestExpression2 ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	int Test (params object[] args) { }
	void TestMethod ()
	{
		IEnumerable<object> e = null;
		var type = e.GetType();
		var x = Test (e.First (), e = new objct[0], e.Count ());
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestOutArgument ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void Test (out IEnumerable<object> e)
	{
		e = null;
	}

	void TestMethod (IEnumerable<object> e)
	{
		foreach (var x in e) ;
		Test (out e);
		var y = e.Count ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 0);
		}

		[Test]
		public void TestOutArgument2 ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void Test (out IEnumerable<object> e)
	{
		e = null;
	}

	void TestMethod (IEnumerable<object> e)
	{
		foreach (var x in e) ;
		Test (out e);
		var y = e.Count ();
		var z = e.Count ();
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}

		[Test]
		public void TestOutArgument3 ()
		{
			var input = @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	void Test (object arg1, out IEnumerable<object> e, object arg2)
	{
		e = null;
	}

	void TestMethod (IEnumerable<object> e)
	{
		Test (e.First (), out e, e.First ());
	}
}";
			Test<MultipleEnumerationIssue> (input, 2);
		}
	}
}
