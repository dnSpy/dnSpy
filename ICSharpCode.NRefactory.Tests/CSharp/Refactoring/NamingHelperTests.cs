//
// BaseRefactoringContextTests.cs
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
using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
		[TestFixture]
		public class NamingHelperTests
		{
				RefactoringContext MakeContext(string input, bool expectErrors = false)
				{
						var context = TestRefactoringContext.Create(input, expectErrors);
						return context;
				}

				[Test]
				public void GenerateVariableNameTest()
				{
						var context = MakeContext(@"
class A
{
	void F()
	{ $ }
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.AreEqual("i", name);
				}

				[Test]
				public void GenerateVariableNameIgnoresNamesUsedPreviouslyInScope()
				{
						var context = MakeContext(@"
class A
{
	void F()
	{
		int i;
		$
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.IsFalse(name == "i", "i was already used and should not be proposed.");
				}

				[Test]
				public void GenerateVariableNameIgnoresNamesUsedLaterInScope()
				{
						var context = MakeContext(@"
class A
{
	void F()
	{
		$
		int i;
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.IsFalse(name == "i", "i was already used and should not be proposed.");
				}

				[Test]
				public void GenerateVariableNameIgnoresNamesUsedInNestedScope()
				{
						var context = MakeContext(@"
class A
{
	void F()
	{
		$
		for (int i; i < 0; i++);
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.IsFalse(name == "i", "i was already used and should not be proposed.");
				}

				[Test]
				public void GenerateVariableNameInForIgnoresIterator()
				{
						var context = MakeContext(@"
class A
{
	void F()
	{
		for (int i; i < 0; i++)
			$
	}
}", true);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.IsFalse(name == "i", "i was already used and should not be proposed.");
				}

				[Test]
				public void GenerateVariableNameInMethodIgnoresParameters()
				{
						var context = MakeContext(@"
class A
{
	void F(int i)
	{
		$
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.IsFalse(name == "i", "i was already used and should not be proposed.");
				}

				[Test]
				public void GenerateVariableNameInForInitializerList()
				{
						var context = MakeContext(@"
class A
{
	void F(int i)
	{
		for($ ; i < 0; i++);
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.IsFalse(name == "i", "i was already used and should not be proposed.");
				}

				[Test]
				public void GenerateVariableNameWithNumberedVariableInParentBlock()
				{
						var context = MakeContext(@"
class A
{
	void F()
	{
		int i2;
		{
			int i, j, k;
			$
		}
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.AreEqual("i3", name);
				}

				[Test]
				public void GenerateVariableNameShouldNotIgnoreBasedOnMethodCallIdentifiers()
				{
						var context = MakeContext(@"
class B
{
	void i()
	{
	}
}
class A
{
	void F()
	{
		
		for($ ;;)
			B.i();
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.IsTrue(name == "i");
				}

				[Test]
				public void GenerateVariableNameIgnoresLinqIdentifiers()
				{
						// Snippet tests that identifiers from in, into and let clauses are found
						var context = MakeContext(@"
class A
{
	void F()
	{
		$
		var ints = from i in new int [] {} group i by i % 2 into j let k = 2 select j.Count() + k;
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.AreEqual("i2", name);
				}

				[Test]
				public void GenerateVariableNameIgnoresFixedVariables()
				{
						var context = MakeContext(@"
class A
{
	unsafe void F()
	{
		$
		fixed (int i = 13) {}
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.AreEqual("j", name);
				}

				[Test]
				public void GenerateVariableNameFallsBackToNumbering()
				{
						var context = MakeContext(@"
class A
{
	void F()
	{
		int i, j, k;
		$
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name);
						Assert.AreEqual("i2", name);
				}
		
				[Test]
				public void GenerateVariableNameForCustomType()
				{
						var context = MakeContext(@"
class A
{
	void F()
	{
		$
	}
}"
						);
						var name = new NamingHelper(context).GenerateVariableName(new SimpleType() { Identifier = "VariableNameGenerationTester" });
						Assert.NotNull(name);
						Assert.AreEqual("variableNameGenerationTester", name);
				}
		
				[Test]
				public void GenerateVariableNameDoesNotRepeatNames()
				{
						var context = MakeContext(@"
class A
{
	void F()
	{
		$
	}
}"
						);
						var namingHelper = new NamingHelper(context);
						var name1 = namingHelper.GenerateVariableName(new PrimitiveType("int"));
						var name2 = namingHelper.GenerateVariableName(new PrimitiveType("int"));
						Assert.NotNull(name1);
						Assert.NotNull(name2);
						Assert.AreNotEqual(name1, name2, "The generated names should not repeat.");
				}
		}
}
