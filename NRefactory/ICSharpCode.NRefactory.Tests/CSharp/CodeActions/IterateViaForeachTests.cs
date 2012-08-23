//
// IterateViaForeachTests.cs
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
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class IterateViaForeachTests : ContextActionTestBase
	{
		[Test]
		public void HandlesNonGenericCase()
		{
			Test<IterateViaForeachAction>(@"
using System.Collections;
class TestClass
{
	public void F()
	{
		var $list = new ArrayList ();
	}
}", @"
using System.Collections;
class TestClass
{
	public void F()
	{
		var list = new ArrayList ();
		foreach (var o in list) {
		}
	}
}");
		}

		[Test]
		public void HandlesExpressionStatement()
		{
			Test<IterateViaForeachAction>(@"
using System.Collections.Generic;
class TestClass
{
	public IEnumerable<int> GetInts()
	{
		return new int[] { };
	}

	public void F()
	{
		GetInts$();
	}
}", @"
using System.Collections.Generic;
class TestClass
{
	public IEnumerable<int> GetInts()
	{
		return new int[] { };
	}

	public void F()
	{
		foreach (var i in GetInts ()) {
		}
	}
}");
		}

		[Test]
		public void HandlesAssignmentExpressionStatement()
		{
			Test<IterateViaForeachAction>(@"
using System.Collections.Generic;
class TestClass
{
	public IEnumerable<int> GetInts ()
	{
		return new int[] { };
	}

	public void F()
	{
		IEnumerable<int> ints;
		ints = GetIn$ts ();
	}
}", @"
using System.Collections.Generic;
class TestClass
{
	public IEnumerable<int> GetInts ()
	{
		return new int[] { };
	}

	public void F()
	{
		IEnumerable<int> ints;
		ints = GetInts ();
		foreach (var i in ints) {
		}
	}
}");
		}

		[Test]
		public void HandlesAsExpressionStatement()
		{
			Test<IterateViaForeachAction>(@"
using System.Collections.Generic;
class TestClass
{
	public void F()
	{
		object s = """";
		s as IEnumerable$<char>;
	}
}", @"
using System.Collections.Generic;
class TestClass
{
	public void F()
	{
		object s = """";
		foreach (var c in s as IEnumerable<char>) {
		}
	}
}", 0, true);
		}

		[Test]
		public void NonKnownTypeNamingTest()
		{
			Test<IterateViaForeachAction>(@"
using System.Collections.Generic;
class TestClass
{
	public void F()
	{
		var items$ = new List<TestClass> ();
	}
}", @"
using System.Collections.Generic;
class TestClass
{
	public void F()
	{
		var items = new List<TestClass> ();
		foreach (var testClass in items) {
		}
	}
}");
		}

		[Test]
		public void HandlesAsExpression()
		{
			Test<IterateViaForeachAction>(@"
using System.Collections.Generic;
class TestClass
{
	public void F()
	{
		object s = """";
		s as IEnumerable$<char>
	}
}", @"
using System.Collections.Generic;
class TestClass
{
	public void F()
	{
		object s = """";
		foreach (var c in s as IEnumerable<char>) {
		}
	}
}", 0, true);
		}

		[Test]
		public void HandlesLinqExpressionAssignment()
		{
			Test<IterateViaForeachAction>(@"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	public IEnumerable<int> GetInts()
	{
		return new int[] { };
	}

	public void F()
	{
		var $filteredInts = from item in GetInts ()
							where item > 0
							select item;
	}
}", @"
using System.Collections.Generic;
using System.Linq;
class TestClass
{
	public IEnumerable<int> GetInts()
	{
		return new int[] { };
	}

	public void F()
	{
		var filteredInts = from item in GetInts ()
							where item > 0
							select item;
		foreach (var i in filteredInts) {
		}
	}
}");
		}

		[Test]
		public void IgnoresExpressionsInForeachStatement()
		{
			TestWrongContext<IterateViaForeachAction>(@"
using System.Collections.Generic;
class TestClass
{
	public IEnumerable<int> GetInts()
	{
		return new int[] { };
	}

	public void F()
	{
		foreach (var i in $GetInts ()) {
		}
	}
}");
		}

		[Test]
		public void IgnoresInitializersInForStatement()
		{
			TestWrongContext<IterateViaForeachAction>(@"
class TestClass
{
	public void F()
	{
		for (int[] i = new $int[] {} ;;) {
		}
	}
}");
		}

		[Test]
		public void AddsForToBodyOfUsingStatement()
		{
			Test<IterateViaForeachAction>(@"
class TestClass
{
	public void F()
	{
		using (int[] i = new $int[] {}) {
		}
	}
}",@"
class TestClass
{
	public void F()
	{
		using (int[] i = new int[] {}) {
			foreach (var j in i) {
			}
		}
	}
}");
		}

		[Test]
		public void AddsBlockStatementToUsingStatement()
		{
			Test<IterateViaForeachAction>(@"
class TestClass
{
	public void F()
	{
		using (int[] i = new $int[] {});
	}
}",@"
class TestClass
{
	public void F()
	{
		using (int[] i = new int[] {}) {
			foreach (var j in i) {
			}
		}
	}
}");
		}
		
		[Test]
		public void IgnoresFieldDeclarations()
		{
			TestWrongContext<IterateViaForeachAction>(@"
using System.Collections.Generic;
class TestClass
{
	List<int> list = $new List<int>();
}");
		}
	}
}

