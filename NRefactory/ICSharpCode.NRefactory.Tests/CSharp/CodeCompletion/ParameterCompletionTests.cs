//
// ParameterCompletionTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using NUnit.Framework;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.CodeCompletion
{
	[TestFixture()]
	public class ParameterCompletionTests : TestBase
	{
		class TestFactory : IParameterCompletionDataFactory
		{
			IProjectContent ctx;
			
			public TestFactory (IProjectContent ctx)
			{
				this.ctx = ctx;
			}
			
			class Provider : IParameterDataProvider
			{
				public IEnumerable<IMethod> Data { get; set; }
				#region IParameterDataProvider implementation
				public int StartOffset { get { return 0; } }
				
				public string GetHeading (int overload, string[] parameterDescription, int currentParameter)
				{
					return "";
				}

				public string GetDescription (int overload, int currentParameter)
				{
					return "";
				}

				public string GetParameterDescription (int overload, int paramIndex)
				{
					return "";
				}

				public int GetParameterCount (int overload)
				{
					var method = Data.ElementAt (overload);
					return method.Parameters.Count;
				}

				public bool AllowParameterList (int overload)
				{
					return false;
				}

				public int Count {
					get {
						return Data.Count ();
					}
				}
				#endregion
			}
			
			class IndexerProvider : IParameterDataProvider
			{
				public IEnumerable<IProperty> Data { get; set; }
				
				#region IParameterDataProvider implementation
				public int StartOffset { get { return 0; } }

				public string GetHeading (int overload, string[] parameterDescription, int currentParameter)
				{
					return "";
				}

				public string GetDescription (int overload, int currentParameter)
				{
					return "";
				}

				public string GetParameterDescription (int overload, int paramIndex)
				{
					return "";
				}

				public int GetParameterCount (int overload)
				{
					var method = Data.ElementAt (overload);
					return method.Parameters.Count;
				}

				public bool AllowParameterList (int overload)
				{
					return false;
				}

				public int Count {
					get {
						return Data.Count ();
					}
				}
				#endregion
			}
			
			
			class ArrayProvider : IParameterDataProvider
			{
				#region IParameterDataProvider implementation
				public int StartOffset { get { return 0; } }

				public string GetHeading (int overload, string[] parameterDescription, int currentParameter)
				{
					return "";
				}

				public string GetDescription (int overload, int currentParameter)
				{
					return "";
				}

				public string GetParameterDescription (int overload, int paramIndex)
				{
					return "";
				}

				public int GetParameterCount (int overload)
				{
					return 1;
				}

				public bool AllowParameterList (int overload)
				{
					return false;
				}

				public int Count {
					get {
						return 1;
					}
				}
				#endregion
			}
			
			class TypeParameterDataProvider : IParameterDataProvider
			{
				public IEnumerable<IType> Data { get; set; }
				#region IParameterDataProvider implementation
				public int StartOffset { get { return 0; } }

				public string GetHeading (int overload, string[] parameterDescription, int currentParameter)
				{
					return "";
				}

				public string GetDescription (int overload, int currentParameter)
				{
					return "";
				}

				public string GetParameterDescription (int overload, int paramIndex)
				{
					return "";
				}

				public int GetParameterCount (int overload)
				{
					var data = Data.ElementAt (overload);
					return data.TypeParameterCount;
				}

				public bool AllowParameterList (int overload)
				{
					return false;
				}

				public int Count {
					get {
						return Data.Count ();
					}
				}
				#endregion
			}
			
			#region IParameterCompletionDataFactory implementation
			public IParameterDataProvider CreateConstructorProvider(int startOffset, ICSharpCode.NRefactory.TypeSystem.IType type)
			{
				Assert.IsTrue(type.Kind != TypeKind.Unknown);
				return new Provider () {
					Data = type.GetConstructors (m => m.Accessibility == Accessibility.Public)
				};
			}

			public IParameterDataProvider CreateMethodDataProvider (int startOffset, IEnumerable<IMethod> methods)
			{
				return new Provider () {
					Data = methods
				};
			}

			public IParameterDataProvider CreateDelegateDataProvider(int startOffset, ICSharpCode.NRefactory.TypeSystem.IType type)
			{
				Assert.IsTrue(type.Kind != TypeKind.Unknown);
				return new Provider () {
					Data = new [] { type.GetDelegateInvokeMethod () }
				};
			}
			
			public IParameterDataProvider CreateIndexerParameterDataProvider(int startOffset, IType type, AstNode resolvedNode)
			{
				Assert.IsTrue(type.Kind != TypeKind.Unknown);
				if (type.Kind == TypeKind.Array)
					return new ArrayProvider ();
				return new IndexerProvider () {
					Data = type.GetProperties (p => p.IsIndexer)
				};
			}
			
			public IParameterDataProvider CreateTypeParameterDataProvider (int startOffset, IEnumerable<IType> types)
			{
				return new TypeParameterDataProvider () {
					Data = types
				};
			}
			#endregion
		}
		
		internal static IParameterDataProvider CreateProvider (string text)
		{
			string parsedText;
			string editorText;
			int cursorPosition = text.IndexOf ('$');
			int endPos = text.IndexOf ('$', cursorPosition + 1);
			if (endPos == -1)
				parsedText = editorText = text.Substring (0, cursorPosition) + text.Substring (cursorPosition + 1);
			else {
				parsedText = text.Substring (0, cursorPosition) + new string (' ', endPos - cursorPosition) + text.Substring (endPos + 1);
				editorText = text.Substring (0, cursorPosition) + text.Substring (cursorPosition + 1, endPos - cursorPosition - 1) + text.Substring (endPos + 1);
				cursorPosition = endPos - 1; 
			}
			var doc = new ReadOnlyDocument (editorText);
			
			IProjectContent pctx = new CSharpProjectContent ();
			pctx = pctx.AddAssemblyReferences (new [] { CecilLoaderTests.Mscorlib, CecilLoaderTests.SystemCore });
			
			var compilationUnit = new CSharpParser ().Parse (parsedText, "program.cs");
			compilationUnit.Freeze ();
			
			var parsedFile = compilationUnit.ToTypeSystem ();
			pctx = pctx.UpdateProjectContent (null, parsedFile);
			var cmp = pctx.CreateCompilation ();
			var loc = doc.GetLocation (cursorPosition);
			
			
			var rctx = new CSharpTypeResolveContext (cmp.MainAssembly);
			rctx = rctx.WithUsingScope (parsedFile.GetUsingScope (loc).Resolve (cmp));
			var curDef = parsedFile.GetInnermostTypeDefinition (loc);
			if (curDef != null) {
				rctx = rctx.WithCurrentTypeDefinition (curDef.Resolve (rctx).GetDefinition ());
				var curMember = parsedFile.GetMember (loc);
				if (curMember != null)
					rctx = rctx.WithCurrentMember (curMember.CreateResolved (rctx));
			}
			var engine = new CSharpParameterCompletionEngine (doc, new TestFactory (pctx), pctx, rctx, compilationUnit, parsedFile);
			return engine.GetParameterDataProvider (cursorPosition, doc.GetCharAt (cursorPosition - 1));
		}
		
		/// <summary>
		/// Bug 427448 - Code Completion: completion of constructor parameters not working
		/// </summary>
		[Test()]
		public void TestBug427448 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class Test
{
	public Test (int a)
	{
	}
	
	public Test (string b)
	{
	}
	protected Test ()
	{
	}
	Test (double d, float m)
	{
	}
}

class AClass
{
	void A()
	{
		$Test t = new Test ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.Count);
		}

		/// <summary>
		/// Bug 432437 - No completion when invoking delegates
		/// </summary>
		[Test()]
		public void TestBug432437 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"public delegate void MyDel (int value);

class Test
{
	MyDel d;

	void A()
	{
		$d ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}

		/// <summary>
		/// Bug 432658 - Incorrect completion when calling an extension method from inside another extension method
		/// </summary>
		[Test()]
		public void TestBug432658 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"static class Extensions
{
	public static void Ext1 (this int start)
	{
	}
	public static void Ext2 (this int end)
	{
		$Ext1($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count, "There should be one overload");
			Assert.AreEqual (1, provider.GetParameterCount(0), "Parameter 'start' should exist");
		}

		/// <summary>
		/// Bug 432727 - No completion if no constructor
		/// </summary>
		[Test()]
		public void TestBug432727 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class A
{
	void Method ()
	{
		$A aTest = new A ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}

		/// <summary>
		/// Bug 434705 - No autocomplete offered if not assigning result of 'new' to a variable
		/// </summary>
		[Test()]
		public void TestBug434705 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class Test
{
	public Test (int a)
	{
	}
}

class AClass
{
	Test A()
	{
		$return new Test ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		/// <summary>
		/// Bug 434705 - No autocomplete offered if not assigning result of 'new' to a variable
		/// </summary>
		[Test()]
		public void TestBug434705B ()
		{
			IParameterDataProvider provider = CreateProvider (
@"
class Test<T>
{
	public Test (T t)
	{
	}
}
class TestClass
{
	void TestMethod ()
	{
		$Test<int> l = new Test<int> ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
	
		
		/// <summary>
		/// Bug 434701 - No autocomplete in attributes
		/// </summary>
		[Test()]
		public void TestBug434701 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"namespace Test {
class TestAttribute : System.Attribute
{
	public Test (int a)
	{
	}
}

$[Test ($
class AClass
{
}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		/// <summary>
		/// Bug 447985 - Exception display tip is inaccurate for derived (custom) exceptions
		/// </summary>
		[Test()]
		public void TestBug447985 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"
namespace System {
	public class Exception
	{
		public Exception () {}
	}
}

class MyException : System.Exception
{
	public MyException (int test)
	{}
}

class AClass
{
	public void Test ()
	{
		$throw new MyException($
	}

}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
			Assert.AreEqual (1, provider.GetParameterCount(0), "Parameter 'test' should exist");
		}
		
		
		/// <summary>
		/// Bug 1760 - [New Resolver] Parameter tooltip not shown for indexers 
		/// </summary>
		[Test()]
		public void Test1760 ()
		{
			var provider = CreateProvider (
@"
class TestClass
{
	public static void Main (string[] args)
	{
		$args[$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		[Test()]
		public void TestSecondIndexerParameter ()
		{
			var provider = CreateProvider (
@"
class TestClass
{
	public int this[int i, int j] { get { return 0; } } 
	public void Test ()
	{
		$this[1,$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		[Test()]
		public void TestSecondMethodParameter ()
		{
			var provider = CreateProvider (
@"
class TestClass
{
	public int TestMe (int i, int j) { return 0; } 
	public void Test ()
	{
		$TestMe (1,$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		
		/// Bug 599 - Regression: No intellisense over Func delegate
		[Test()]
		public void TestBug599 ()
		{
			var provider = CreateProvider (
@"using System;
using System.Core;

class TestClass
{
	void A (Func<int, int> f)
	{
		$f ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		/// Bug 3307 - Chained linq methods do not work correctly
		[Test()]
		public void TestBug3307 ()
		{
			var provider = CreateProvider (
@"using System;
using System.Linq;

class TestClass
{
	public static void Main (string[] args)
	{
		$args.Select ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (6, provider.Count);
		}
		
		[Test()]
		public void TestBug3307FollowUp ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
@"using System;
using System.Linq;

public class MainClass
{
	static void TestMe (Action<int> act)
	{
	}
	
	public static void Main (string[] args)
	{
		$TestMe (x$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.IsFalse (provider.AutoSelect, "auto select enabled !");
		}
		
		[Test()]
		public void TestBug3307FollowUp2 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
@"using System;
using System.Linq;

public class MainClass
{
	public static void Main (string[] args)
	{
		$args.Select (x$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.IsFalse (provider.AutoSelect, "auto select enabled !");
		}
		
		[Test()]
		public void TestConstructor ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class Foo { public Foo (int a) {} }

class A
{
	void Method ()
	{
		$Bar = new Foo ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		
		[Test()]
		public void TestConstructorCase2 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"
namespace Test 
{
	struct TestMe 
	{
		public TestMe (string a)
		{
		}
	}
	
	class A
	{
		void Method ()
		{
			$new TestMe ($
		}
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.Count);
		}
		
		[Test()]
		public void TestTypeParameter ()
		{
			IParameterDataProvider provider = CreateProvider (
@"using System;

namespace Test 
{
	class A
	{
		void Method ()
		{
			$Action<$
		}
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (16, provider.Count);
		}
		
		[Test()]
		public void TestSecondTypeParameter ()
		{
			IParameterDataProvider provider = CreateProvider (
@"using System;

namespace Test 
{
	class A
	{
		void Method ()
		{
			$Action<string,$
		}
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (16, provider.Count);
		}
		
		[Ignore("TODO")]
		[Test()]
		public void TestMethodTypeParameter ()
		{
			IParameterDataProvider provider = CreateProvider (
@"using System;

namespace Test 
{
	class A
	{
		void TestMethod<T, S>()
		{
		}

		void Method ()
		{
			$TestMethod<$
		}
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		[Ignore("TODO")]
		[Test()]
		public void TestSecondMethodTypeParameter ()
		{
			IParameterDataProvider provider = CreateProvider (
@"using System;

namespace Test 
{
	class A
	{
		void TestMethod<T, S>()
		{
		}

		void Method ()
		{
			$TestMethod<string,$
		}
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}		
	
		[Test()]
		public void TestArrayParameter ()
		{
			var provider = CreateProvider (
@"
class TestClass
{
	public void Method()
	{
		int[,,,] arr;
		$arr[$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		[Test()]
		public void TestSecondArrayParameter ()
		{
			var provider = CreateProvider (
@"
class TestClass
{
	public void Method()
	{
		int[,,,] arr;
		$arr[5,$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		[Ignore("TODO!")]
		[Test()]
		public void TestTypeParameterInBaseType ()
		{
			IParameterDataProvider provider = CreateProvider (
@"using System;

namespace Test 
{
	$class A : Tuple<$
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (16, provider.Count);
		}
		
		
		[Test()]
		public void TestBaseConstructorCall ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class Base
{
	public Base (int i)
	{
			
	}
	public Base (int i, string s)
	{
			
	}
}

namespace Test 
{
	class A : Base
	{
		$public A () : base($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.Count);
		}
		
		[Test()]
		public void TestThisConstructorCall ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class Base
{
	public Base (int i)
	{
			
	}
	public Base (int i, string s)
	{
			
	}
}

namespace Test 
{
	class A : Base
	{
		public A (int a, int b) : base(a) {}

		$public A () : this($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		/// <summary>
		/// Bug 3645 - [New Resolver]Parameter completion shows all static and non-static overloads
		/// </summary>
		[Test()]
		public void TestBug3645 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class Main
{
	public static void FooBar (string str)
	{
	}
	
	public void FooBar (int i)
	{
		
	}
	
	public static void Main (string[] args)
	{
		$FooBar ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}

		/// <summary>
		/// Bug 3991 - constructor argument completion not working for attributes applied to methods or parameters
		/// </summary>
		[Test()]
		public void TestBug3991()
		{
			IParameterDataProvider provider = CreateProvider(
@"using System;
namespace Test
{
	class TestClass
	{
		[Obsolete$($]
		TestClass()
		{
		}
	}
}
");
			Assert.IsNotNull(provider, "provider was not created.");
			Assert.Greater(provider.Count, 0);
		}

		/// <summary>
		/// Bug 4087 - code completion handles object and collection initializers (braces) incorrectly in method calls
		/// </summary>
		[Test()]
		public void TestBug4087()
		{
			IParameterDataProvider provider = CreateProvider(
@"using System;
class TestClass
{
	TestClass()
	{
		$Console.WriteLine (new int[]{ 4, 5,$
	}
}
");
			Assert.IsTrue (provider == null || provider.Count == 0);
		}
	}
}