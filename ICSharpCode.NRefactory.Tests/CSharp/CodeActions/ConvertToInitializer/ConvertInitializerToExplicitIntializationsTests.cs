//
// ConvertInitializerToExplicitIntializationsTests.cs
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
	public class ConvertInitializerToExplicitIntializationsTests : ContextActionTestBase
	{
		string baseText = @"
class TestClass
{
	public string Property { get; set; }

	public TestClass Nested { get; set; }

	void F ()
	{
		";
		
		[Test]
		public void SingleLevelObjectIntializer ()
		{
			Test<ConvertInitializerToExplicitInitializationsAction>(baseText + @"
		var variable = new Test$Class () {
			Property = ""Value""
		};
	}
}", baseText + @"
		var testClass = new TestClass ();
		testClass.Property = ""Value"";
		var variable = testClass;
	}
}");
		}
		
		[Test]
		public void ObjectCreateExpressionWithoutObjectInitializer ()
		{
			Test<ConvertInitializerToExplicitInitializationsAction>(baseText + @"
		var variable $= new System.Collection.Generic.List<TestClass> () {
			new TestClass () {
				Property = ""One"",
				Nested = new TestClass ()
			}
		};
	}
}", baseText + @"
		var list = new System.Collection.Generic.List<TestClass> ();
		var testClass = new TestClass ();
		testClass.Property = ""One"";
		testClass.Nested = new TestClass ();
		list.Add (testClass);
		var variable = list;
	}
}");
		}
		
		[Test]
		public void SingleLevelObjectIntializerToExistingVariable ()
		{
			Test<ConvertInitializerToExplicitInitializationsAction>(baseText + @"
		var variable = new TestClass ();
		variable = new Test$Class () {
			Property = ""Value""
		};
	}
}", baseText + @"
		var variable = new TestClass ();
		var testClass = new TestClass ();
		testClass.Property = ""Value"";
		variable = testClass;
	}
}");
		}
		
		[Test]
		public void SingleLevelCollectionIntializer ()
		{
			Test<ConvertInitializerToExplicitInitializationsAction>(baseText + @"
		var variable $= new System.Collection.Generic.Dictionary<string, TestClass> () {
			{""Key1"", new TestClass { Property = ""Value1"", Nested = { Property = ""Value1b"" }}}
		};
	}
}", baseText + @"
		var dictionary = new System.Collection.Generic.Dictionary<string, TestClass> ();
		var testClass = new TestClass ();
		testClass.Property = ""Value1"";
		testClass.Nested.Property = ""Value1b"";
		dictionary.Add (""Key1"", testClass);
		var variable = dictionary;
	}
}");
		}
		
		[Test]
		public void CollectionOfIntializers ()
		{
			Test<ConvertInitializerToExplicitInitializationsAction>(baseText + @"
		var variable $= new System.Collection.Generic.Dictionary<string, TestClass> () {
			{""Key1"", new TestClass { Property = ""Value1"", Nested = { Property = ""Value1b"" }}}
		};
	}
}", baseText + @"
		var dictionary = new System.Collection.Generic.Dictionary<string, TestClass> ();
		var testClass = new TestClass ();
		testClass.Property = ""Value1"";
		testClass.Nested.Property = ""Value1b"";
		dictionary.Add (""Key1"", testClass);
		var variable = dictionary;
	}
}");
		}

	}
}

