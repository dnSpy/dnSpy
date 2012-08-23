//
// ConvertToInitializerTests.cs
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
using ICSharpCode.NRefactory.PatternMatching;
using System;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class ConvertToInitializerTests : ContextActionTestBase
	{

		// TODO: Remove this when the formatter handles object and collection initializers
		// This tests the expected code vs the actual code based on their ASTs instead of the text they produce.
		public new void Test<T>(string input, string output, int action = 0, bool expectErrors = false) 
			where T : ICodeActionProvider, new ()
		{
			string result = RunContextAction(new T(), HomogenizeEol(input), action, expectErrors);

			var expectedContext = TestRefactoringContext.Create(output, expectErrors);
			var actualContext = TestRefactoringContext.Create(result, expectErrors);

			bool passed = expectedContext.RootNode.IsMatch(actualContext.RootNode);
			if (!passed) {
				Console.WriteLine("-----------Expected:");
				Console.WriteLine(output);
				Console.WriteLine("-----------Got:");
				Console.WriteLine(result);
			}
			Assert.IsTrue(passed, "The generated code and the expected code was not syntactically identical. See output for details.");
		}

		string baseText = @"
class TestClass
{
	public string Property { get; set; }

	public TestClass Nested { get; set; }

	void F ()
	{
		";

		[Test]
		public void SimpleObject()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		variable.Property = ""Value"";
	}
}", baseText + @"
		var variable = new TestClass () {
			Property = ""Value""
		};
	}
}");
		}
		
		[Test]
		public void SimpleCollection()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var collection = new System.Collections.Generic.List$<string> ();
		collection.Add(""string1"");
		collection.Add(""string2"");
	}
}", baseText + @"
		var collection = new System.Collections.Generic.List<string> () {
			""string1"",
			""string2""
		};
	}
}");
		}
		
		[Test]
		public void MultiElementCollection()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var tc0 = new TestClass();
		var collection = new System.Collections.Generic.Dictionary$<string, TestClass> ();
		var tc1 = new TestClass();
		tc1.Property = ""tc1"";
		collection.Add(""string1"", tc1);
		var tc2 = new TestClass();
		tc2.Property = ""tc2"";
		collection.Add(""string2"", tc2);
		collection.Add(""string0"", tc0);
	}
}", baseText + @"
		var tc0 = new TestClass();
		var collection = new System.Collections.Generic.Dictionary<string> () {
			{""string1"", new TestClass() { Property = ""tc1"" }},
			{""string2"", new TestClass() { Property = ""tc2"" }}
		};
		collection.Add(""string0"", tc0);
	}
}");
		}
		
		[Test]
		public void CollectionOfObjects()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var collection = new System.Collections.Generic.List$<string> ();
		var item = new TestClass ();
		item.Property = ""Value1"";
		collection.Add(item);
		item = new TestClass ();
		item.Property = ""Value2"";
		item.Nested = new TestClass ();
		item.Nested.Property = ""Value3"";
		collection.Add(item);
	}
}", baseText + @"
		var collection = new System.Collections.Generic.List<string> () {
			new TestClass() {
				Property = ""Value1""
			},
			new TestClass() {
				Property = ""Value2"",
				Nested = new TestClass() {
					Property = ""Value3""
				}
			}
		};
	}
}");
		}
		
		[Test]
		public void UnknownTargetForAdd()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var collection = new System.Collections.Generic.List<string> ();
		var item $= new TestClass ();
		item.Property = ""Value1"";
		collection.Add(item);
	}
}", baseText + @"
		var collection = new System.Collections.Generic.List<string> ();
		var item = new TestClass() {
			Property = ""Value1""
		};
		collection.Add(item);
	}
}");
		}

		[Test]
		public void ObjectContainingCollections()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		variable.Property = ""Value1"";
		var collection = new System.Collections.Generic.List<TestClass>();
		collection.Add (new TestClass());
		collection.Add (new TestClass());
		collection.Add (new TestClass());
		variable.Children = collection;
	}
	
	public System.Collections.Generic.IList<TestClass> Children;
}", baseText + @"
		var variable = new TestClass() {
			Property = ""Value1"",
			Children = new System.Collections.Generic.List<TestClass>() {
				new TestClass(),
				new TestClass(),
				new TestClass()
			}
		};
	}
	
	public System.Collections.Generic.IList<TestClass> Children;
}"
			);
		}

		[Test]
		public void SplitAssignmentAndInitialization()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		TestClass variable;
		variable = new Test$Class ();
		variable.Property = ""Value"";
	}
}", baseText + @"
		TestClass variable;
		variable = new TestClass () {
			Property = ""Value""
		};
	}
}"
			);
		}

		[Test]
		public void MemberAssignment()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		Nested = new Test$Class ();
		Nested.Property = ""Value"";
	}
}", baseText + @"
		Nested = new TestClass () {
			Property = ""Value""
		};
	}
}"
			);
		}

		[Test]
		public void NonCreatingInitialization()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		variable.Nested.Nested.Property = ""Value"";
	}
}", baseText + @"
		var variable = new TestClass () {
			Nested = {
				Nested = {
					Property = ""Value""
				}
			}
		};
	}
}"
			);
		}

		[Test]
		public void IgnoresLinesPreceedingInitialization()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		System.Console.WriteLine(""One"");
		for(;;) {
			System.Console.WriteLine(""Two"");
		}
		System.Console.WriteLine(""Three"");
		TestClass variable = new Test$Class ();
		variable.Property = ""Value"";
	}
}", baseText + @"
		System.Console.WriteLine(""One"");
		for(;;) {
			System.Console.WriteLine(""Two"");
		}
		System.Console.WriteLine(""Three"");
		TestClass variable = new TestClass () {
			Property = ""Value""
		};
	}
}"
			);
		}

		[Test]
		public void NestedObject()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		variable.Property = ""Value"";
		variable.Nested = new TestClass();
		variable.Nested.Property = ""NestedValue"";
		variable.Nested.Nested = new TestClass();
		variable.Nested.Nested.Property = ""NestedNestedValue"";
	}
}", baseText + @"
		var variable = new TestClass () {
			Property = ""Value"",
			Nested = new TestClass() {
				Property = ""NestedValue"",
				Nested = new TestClass() {
					Property = ""NestedNestedValue""
				}
			}
		};
	}
}"
			);
		}

		[Test]
		public void NestedObjectWithSkippedLevel()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		variable.Property = ""Value"";
		variable.Nested.Nested = new TestClass();
		variable.Nested.Nested.Property = ""NestedNestedValue"";
	}
}", baseText + @"
		var variable = new TestClass () {
			Property = ""Value"",
			Nested = {
				Nested = new TestClass() {
					Property = ""NestedNestedValue""
				}
			}
		};
	}
}"
			);
		}

		[Test]
		public void KeepsOriginalInitializer()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class () {
			Property = ""Value""
		};
		variable.Nested = new TestClass();
	}
}", baseText + @"
		var variable = new TestClass () {
			Property = ""Value"",
			Nested = new TestClass ()
		};
	}
}"
			);
		}

		[Test]
		[Ignore("The pattern matching comparison does not find differences in comments. UnIgnore this when the regular test method is used again.")]
		public void MovesComments()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class () {
			Property = ""Value""
		};
		// This comment should be placed above the initializer corresponding to the statement below
		// This line should too.
		variable.Nested = new TestClass();
	}
}", baseText + @"
		var variable = new TestClass () {
			Property = ""Value"",
			// This comment should be placed above the initializer corresponding to the statement below
			// This line should too.
			Nested = new TestClass ()
		};
	}
}"
			);
		}

		[Test]
		public void StopsAtArbitraryStatement()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		variable.Property = ""Value"";
		variable.Nested = new TestClass();
		System.Console.WriteLine("""");
		variable.Nested.Property = ""NestedValue"";
	}
}", baseText + @"
		var variable = new TestClass () {
			Property = ""Value"",
			Nested = new TestClass()
		};
		System.Console.WriteLine("""");
		variable.Nested.Property = ""NestedValue"";
	}
}"
			);
		}

		[Test]
		public void StopsAtVariableDependentStatement()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		variable.Property = ""Value"";
		variable.Nested = new TestClass();
		variable.Nested.Property = variable.ToString();
	}
}", baseText + @"
		var variable = new TestClass () {
			Property = ""Value"",
			Nested = new TestClass()
		};
		variable.Nested.Property = variable.ToString();
	}
}"
			);
		}

		[Test]
		public void StopsAtVariableDependentCreationStatement()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		variable.Property = ""Value"";
		var nested = new TestClass(variable);
		variable.Nested = nested;
	}
}", baseText + @"
		var variable = new TestClass () {
			Property = ""Value""
		};
		var nested = new TestClass(variable);
		variable.Nested = nested;
	}
}"
			);
		}

		[Test]
		public void HandlesIrrelevantAccessesAtTheEnd()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var tc = new TestClass();
		tc.Property = ""1"";
		var tc$2 = new TestClass();
		tc2.Property = ""2"";
		tc2.Nested = new TestClass();
		tc2.Nested.Property = ""3"";
		tc.Nested = tc2;
	}
}", baseText + @"
		var tc = new TestClass();
		tc.Property = ""1"";
		var tc2 = new TestClass() {
			Property = ""2"",
			Nested = new TestClass() {
				Property = ""3""
			}
		};
		tc.Nested = tc2;
	}
}");
		}
		
		[Test]
		public void HandlesAssignmentWhereRightExpressionIsNotVisited()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var tc = new TestClass();
		tc.Property = ""1"";
		var tc$2 = new TestClass();
		tc2.Property = ""2"";
		tc2.Nested = tc;
		tc.Nested = tc2;
	}
}", baseText + @"
		var tc = new TestClass();
		tc.Property = ""1"";
		var tc2 = new TestClass() {
			Property = ""2"",
			Nested = tc
		};
		tc.Nested = tc2;
	}
}");
		}

		[Test]
		public void HandlesAssignmentToExistingVariable()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		variable.Property = ""Value"";
		variable = new TestClass();
		variable.Nested = new TestClass();
	}
}", baseText + @"
		var variable = new TestClass () {
			Nested = new TestClass()
		};
	}
}");
		}
		
		[Test]
		public void HandlesAssignmentToFinalVariable()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var _variable = new Test$Class ();
		_variable.Property = ""Value"";
		var variable = _variable;
	}
}", baseText + @"
		var variable = new TestClass () {
			Property = ""Value""
		};
	}
}");
		}
		
		[Test]
		public void FinalVariableAssignmentOfOtherInitializer()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var tc $= new TestClass ();
		tc.Property = ""Value"";
		var _variable = new TestClass ();
		var variable = _variable;
	}
}", baseText + @"
		var tc = new TestClass () {
			Property = ""Value""
		};
		var _variable = new Test$Class ();
		var variable = _variable;
	}
}");
		}

		[Test]
		public void StopsAtMemberDependentStatement()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		Nested = new Test$Class ();
		Nested.Nested = new TestClass();
		Nested.Property = ""Value"";
		Nested.Nested.Property = Nested.Property;
		Nested.Nested.Nested = new TestClass();
	}
}", baseText + @"
		Nested = new TestClass () {
			Nested = new TestClass(),
			Property = ""Value""
		};
		Nested.Nested.Property = Nested.Property;
		Nested.Nested.Nested = new TestClass();
	}
}"
			);
		}

		[Test]
		public void KeepsStatementsWhichAreNotAdded()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var collection = new System.Collections.Generic.List$<string> ();
		var item = new TestClass ();
		item.Property = ""Value1"";
		collection.Add(item);
		item = new TestClass ();
		item.Property = ""Value2"";
		item.Nested = new TestClass ();
		item.Nested.Property = ""Value3"";
		collection.Add(item);
		// This should stay here
		item = new TestClass();
	}
}", baseText + @"
		var collection = new System.Collections.Generic.List<string> () {
			new TestClass() {
				Property = ""Value1""
			},
			new TestClass() {
				Property = ""Value2"",
				Nested = new TestClass() {
					Property = ""Value3""
				}
			}
		};
		// This should stay here
		item = new TestClass();
	}
}"
			);
		}
		
		[Test]
		public void StopsAtUnresolvableAssignmentTarget()
		{
			Test<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		variable.Property = ""Value"";
		Variable.Nested = new TestClass();
	}
}", baseText + @"
		var variable = new TestClass () {
			Property = ""Value""
		};
		Variable.Nested = new TestClass();
	}
}"
			                                 );
		}
		
		[Test]
		public void NoActionForSingleStepInitializers()
		{
			TestWrongContext<ConvertToInitializerAction>(baseText + @"
		var variable = new Test$Class ();
		var s = string.Empty;
	}
}");
		}

		[Test]
		public void IgnoresNonCreation()
		{
			TestWrongContext<ConvertToInitializerAction>(@"
class TestClass
{
	void F()
	{
		System.Console.$WriteLine("""");
		var variable = new TestClass();
	}
}"
			);
		}
		
		[Test]
		public void IgnoresSingleCreation()
		{
			TestWrongContext<ConvertToInitializerAction>(@"
class TestClass
{
	void F()
	{
		var variable = new $TestClass();
	}
}");
		}
		
		[Test]
		public void DoesNotCrashOnDeclarationInitializedToVariable()
		{
			TestWrongContext<ConvertToInitializerAction>(@"
class TestClass
{
	void F()
	{
		var s = """";
		s$ = ""2"";
		// And another statement to make the converter even try
		s = """";
	}
}");
		}
	}
}

