//
// CodeCompletionAccessibleTests.cs
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
using NUnit.Framework;
using System.Diagnostics;

namespace ICSharpCode.NRefactory.CSharp.CodeCompletion
{
	[TestFixture]
	public class CodeCompletionAccessibleTests : TestBase
	{
		
		static string testClass = @"
using System;

public class TestClass
{
	public int PubField;
	public int PubProperty { get; set; }
	public void PubMethod () { }

	protected int ProtField;
	protected int ProtProperty { get; set; }
	protected void ProtMethod () { }

	internal protected int ProtOrInternalField;
	internal protected int ProtOrInternalProperty { get; set; }
	internal protected void ProtOrInternalMethod () { }
	
	protected internal int ProtAndInternalField;
	protected internal int ProtAndInternalProperty { get; set; }
	protected internal void ProtAndInternalMethod () { }

	internal int InternalField;
	internal int InternalProperty { get; set; }
	internal void InternalMethod () { }

	private int PrivField;
	private int PrivProperty { get; set; }
	private void PrivMethod () { }

	public static int PubStaticField;
	public static int PubStaticProperty { get; set; }
	public static void PubStaticMethod () { }

	protected static int ProtStaticField;
	protected static int ProtStaticProperty { get; set; }
	protected static void ProtStaticMethod () { }
	
	private static int PrivStaticField;
	private static int PrivStaticProperty { get; set; }
	private static void PrivStaticMethod () { }
";
		[Test()]
		public void TestDerivedClassGeneralAccess ()
		{
			CodeCompletionBugTests.CombinedProviderTest(testClass + @"}
// from
class Test : TestClass {
	public void Foo ()
	{
		$a$
	}
}", provider => {
				Assert.IsNotNull (provider.Find ("PubField"), "'PubField' not found.");
				Assert.IsNotNull (provider.Find ("PubProperty"), "'PubProperty' not found.");
				Assert.IsNotNull (provider.Find ("PubMethod"), "'PubMethod' not found.");
	
				Assert.IsNotNull (provider.Find ("ProtField"), "'ProtField' not found.");
				Assert.IsNotNull (provider.Find ("ProtProperty"), "'ProtProperty' not found.");
				Assert.IsNotNull (provider.Find ("ProtMethod"), "'ProtMethod' not found.");
	
				Assert.IsNotNull (provider.Find ("ProtOrInternalField"), "'ProtOrInternalField' not found.");
				Assert.IsNotNull (provider.Find ("ProtOrInternalProperty"), "'ProtOrInternalProperty' not found.");
				Assert.IsNotNull (provider.Find ("ProtOrInternalMethod"), "'ProtOrInternalMethod' not found.");
	
				Assert.IsNotNull (provider.Find ("ProtAndInternalField"), "'ProtAndInternalField' not found.");
				Assert.IsNotNull (provider.Find ("ProtAndInternalProperty"), "'ProtAndInternalProperty' not found.");
				Assert.IsNotNull (provider.Find ("ProtAndInternalMethod"), "'ProtAndInternalMethod' not found.");
	
				Assert.IsNotNull (provider.Find ("InternalField"), "'InternalField' not found.");
				Assert.IsNotNull (provider.Find ("InternalProperty"), "'InternalProperty' not found.");
				Assert.IsNotNull (provider.Find ("InternalMethod"), "'InternalMethod' not found.");
	
				Assert.IsNotNull (provider.Find ("PubStaticField"), "'PubStaticField' not found.");
				Assert.IsNotNull (provider.Find ("PubStaticProperty"), "'PubStaticProperty' not found.");
				Assert.IsNotNull (provider.Find ("PubStaticMethod"), "'PubStaticMethod' not found.");
				
				Assert.IsNotNull (provider.Find ("ProtStaticField"), "'ProtStaticField' not found.");
				Assert.IsNotNull (provider.Find ("ProtStaticProperty"), "'ProtStaticProperty' not found.");
				Assert.IsNotNull (provider.Find ("ProtStaticMethod"), "'ProtStaticMethod' not found.");
				
				Assert.IsNull (provider.Find ("PrivField"), "'PrivField' found.");
				Assert.IsNull (provider.Find ("PrivProperty"), "'PrivProperty' found.");
				Assert.IsNull (provider.Find ("PrivMethod"), "'PrivMethod' found.");

				Assert.IsNull (provider.Find ("PrivStaticField"), "'PrivStaticField' found.");
				Assert.IsNull (provider.Find ("PrivStaticProperty"), "'PrivStaticProperty' found.");
				Assert.IsNull (provider.Find ("PrivStaticMethod"), "'PrivStaticMethod' found.");
			});
		}
	
		[Test()]
		public void TestDerivedClassMemberReferenceAccess ()
		{
			CodeCompletionBugTests.CombinedProviderTest(testClass + @"}
// from
class Test : TestClass {
	public void Foo ()
	{
		$this.$
	}
}", provider => {
				Assert.IsNotNull (provider.Find ("PubField"), "'PubField' not found.");
				Assert.IsNotNull (provider.Find ("PubProperty"), "'PubProperty' not found.");
				Assert.IsNotNull (provider.Find ("PubMethod"), "'PubMethod' not found.");
	
				Assert.IsNotNull (provider.Find ("ProtField"), "'ProtField' not found.");
				Assert.IsNotNull (provider.Find ("ProtProperty"), "'ProtProperty' not found.");
				Assert.IsNotNull (provider.Find ("ProtMethod"), "'ProtMethod' not found.");
	
				Assert.IsNotNull (provider.Find ("ProtOrInternalField"), "'ProtOrInternalField' not found.");
				Assert.IsNotNull (provider.Find ("ProtOrInternalProperty"), "'ProtOrInternalProperty' not found.");
				Assert.IsNotNull (provider.Find ("ProtOrInternalMethod"), "'ProtOrInternalMethod' not found.");
	
				Assert.IsNotNull (provider.Find ("ProtAndInternalField"), "'ProtAndInternalField' not found.");
				Assert.IsNotNull (provider.Find ("ProtAndInternalProperty"), "'ProtAndInternalProperty' not found.");
				Assert.IsNotNull (provider.Find ("ProtAndInternalMethod"), "'ProtAndInternalMethod' not found.");
	
				Assert.IsNotNull (provider.Find ("InternalField"), "'InternalField' not found.");
				Assert.IsNotNull (provider.Find ("InternalProperty"), "'InternalProperty' not found.");
				Assert.IsNotNull (provider.Find ("InternalMethod"), "'InternalMethod' not found.");
	
//				Assert.IsNotNull (provider.Find ("PubStaticField"), "'PubStaticField' not found.");
//				Assert.IsNotNull (provider.Find ("PubStaticProperty"), "'PubStaticProperty' not found.");
//				Assert.IsNotNull (provider.Find ("PubStaticMethod"), "'PubStaticMethod' not found.");
//				
//				Assert.IsNotNull (provider.Find ("ProtStaticField"), "'ProtStaticField' not found.");
//				Assert.IsNotNull (provider.Find ("ProtStaticProperty"), "'ProtStaticProperty' not found.");
//				Assert.IsNotNull (provider.Find ("ProtStaticMethod"), "'ProtStaticMethod' not found.");
//				
				Assert.IsNull (provider.Find ("PrivField"), "'PrivField' found.");
				Assert.IsNull (provider.Find ("PrivProperty"), "'PrivProperty' found.");
				Assert.IsNull (provider.Find ("PrivMethod"), "'PrivMethod' found.");

				Assert.IsNull (provider.Find ("PrivStaticField"), "'PrivStaticField' found.");
				Assert.IsNull (provider.Find ("PrivStaticProperty"), "'PrivStaticProperty' found.");
				Assert.IsNull (provider.Find ("PrivStaticMethod"), "'PrivStaticMethod' found.");
			});
		}
	


		[Test()]
		public void TestNonStaticClassAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (testClass +
@"
	void TestMethod () 
	{
		$this.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			
			CodeCompletionBugTests.CheckProtectedObjectMembers (provider); // 5 from System.Object
			Assert.IsNotNull (provider.Find ("PubField"));
			Assert.IsNotNull (provider.Find ("PubProperty"));
			Assert.IsNotNull (provider.Find ("PubMethod"));
			
			Assert.IsNotNull (provider.Find ("ProtField"));
			Assert.IsNotNull (provider.Find ("ProtProperty"));
			Assert.IsNotNull (provider.Find ("ProtMethod"));
			
			Assert.IsNotNull (provider.Find ("PrivField"));
			Assert.IsNotNull (provider.Find ("PrivProperty"));
			Assert.IsNotNull (provider.Find ("PrivMethod"));
		}
		
		[Test()]
		public void TestInternalAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (testClass +
@"
	void TestMethod () 
	{
		$this.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			
			Assert.IsNotNull (provider.Find ("InternalField"));
			Assert.IsNotNull (provider.Find ("InternalProperty"));
			Assert.IsNotNull (provider.Find ("InternalMethod"));
			
			Assert.IsNotNull (provider.Find ("ProtAndInternalField"));
			Assert.IsNotNull (provider.Find ("ProtAndInternalProperty"));
			Assert.IsNotNull (provider.Find ("ProtAndInternalMethod"));
			
			Assert.IsNotNull (provider.Find ("ProtOrInternalField"));
			Assert.IsNotNull (provider.Find ("ProtOrInternalProperty"));
			Assert.IsNotNull (provider.Find ("ProtOrInternalMethod"));
		}
		
		[Test()]
		public void TestInternalAccessOutside ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (testClass +
@"
} 
class Test2 {
	void TestMethod () 
	{
		TestClass tc;
		$tc.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			
			Assert.IsNotNull (provider.Find ("InternalField"), "InternalField == null");
			Assert.IsNotNull (provider.Find ("InternalProperty"), "InternalProperty == null");
			Assert.IsNotNull (provider.Find ("InternalMethod"), "InternalMethod == null");
			
			Assert.IsNotNull (provider.Find ("ProtOrInternalField"), "ProtOrInternalField == null");
			Assert.IsNotNull (provider.Find ("ProtOrInternalProperty"), "ProtOrInternalProperty == null");
			Assert.IsNotNull (provider.Find ("ProtOrInternalMethod"), "ProtOrInternalMethod == null");
		}
		
		[Test()]
		public void TestStaticClassAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (testClass +
@"
	void TestMethod () 
	{
		$TestClass.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			
			CodeCompletionBugTests.CheckStaticObjectMembers (provider); // 2 from System.Object
			Assert.IsNotNull (provider.Find ("PubStaticField"));
			Assert.IsNotNull (provider.Find ("PubStaticProperty"));
			Assert.IsNotNull (provider.Find ("PubStaticMethod"));
			
			Assert.IsNotNull (provider.Find ("ProtStaticField"));
			Assert.IsNotNull (provider.Find ("ProtStaticProperty"));
			Assert.IsNotNull (provider.Find ("ProtStaticMethod"));
			
			Assert.IsNotNull (provider.Find ("PrivStaticField"));
			Assert.IsNotNull (provider.Find ("PrivStaticProperty"));
			Assert.IsNotNull (provider.Find ("PrivStaticMethod"));
		}
		
		[Test()]
		public void TestExternalNonStaticClassAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (testClass +
@"}
class AClass {
	void TestMethod () 
	{
		TestClass c;
		$c.$ 
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			
			CodeCompletionBugTests.CheckObjectMembers (provider);
			Assert.IsNotNull (provider.Find ("PubField"));
			Assert.IsNotNull (provider.Find ("PubProperty"));
			Assert.IsNotNull (provider.Find ("PubMethod"));
		}
		
		[Test()]
		public void TestExternalStaticClassAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (testClass +
@"}
class AClass {
	void TestMethod () 
	{
		$TestClass.$ 
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			
			CodeCompletionBugTests.CheckStaticObjectMembers (provider); // 2 members
			Assert.IsNotNull (provider.Find ("PubStaticField"));
			Assert.IsNotNull (provider.Find ("PubStaticProperty"));
			Assert.IsNotNull (provider.Find ("PubStaticMethod"));
		}
		
		[Test()]
		public void TestExternalNonStaticSubclassAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (testClass +
@"}
class AClass : TestClass {
	void TestMethod () 
	{
		$this.$ 
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			
			CodeCompletionBugTests.CheckProtectedObjectMembers (provider);
			Assert.IsNotNull (provider.Find ("PubField"));
			Assert.IsNotNull (provider.Find ("PubProperty"));
			Assert.IsNotNull (provider.Find ("PubMethod"));
			Assert.IsNotNull (provider.Find ("ProtField"));
			Assert.IsNotNull (provider.Find ("ProtProperty"));
			Assert.IsNotNull (provider.Find ("ProtMethod"));
		}

		[Test()]
		public void TestThisProtectedMemberAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	protected void Test ()
	{
	}
}

class Test2 : Test
{
	void Test2 ()
	{
		$this.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}

		[Test()]
		public void TestBasePrivateMemberAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
testClass + @"
}

class Test : TestClass
{
	void Test ()
	{
		$base.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNull (provider.Find ("PrivField"), "field 'PrivField' found, but shouldn't.");
			Assert.IsNull (provider.Find ("PrivProperty"), "property 'PrivProperty' found, but shouldn't.");
			Assert.IsNull (provider.Find ("PrivMethod"), "method 'PrivMethod' found, but shouldn't.");
			
		}
		[Test()]
		public void TestBaseProtectedMemberAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	protected void Test ()
	{
	}
}

class Test2 : Test
{
	void Test2 ()
	{
		$base.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		[Test()]
		public void TestBasePublicMemberAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
testClass + @"
class Test : TestClass
{
	void Test ()
	{
		$base.$
	}
} }");
			Assert.IsNotNull (provider, "provider == null");
			CodeCompletionBugTests.CheckObjectMembers (provider);
			Assert.IsNotNull (provider.Find ("PubField"), "field 'PubField' not found.");
			Assert.IsNotNull (provider.Find ("PubProperty"), "property 'PubProperty' not found.");
			Assert.IsNotNull (provider.Find ("PubMethod"), "method 'PubMethod' not found.");
			
		}
		[Test()]
		public void TestProtectedMemberAccess2 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	protected void Test ()
	{
	}
}

class Test2
{
	void Test2 ()
	{
		$(new Test ()).$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNull (provider.Find ("Test"), "method 'Test' found, but shouldn't.");
		}

		[Test()]
		public void TestGenericParameter ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
class Foo<T>
{
	$public $
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("T"), "generic parameter 'T' not found");
		}
		
		[Test()]
		public void TestUnclosedMember ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"

class C
{
	
	public void Hello ()
	{
		$C$

}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("C"), "class 'C' not found");
		}
		
		
		[Test()]
		public void TestUnclosedMember2 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"using System;

namespace ConsoleTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
		}
		
		public void Hello ()
		{
		}
	}
	
	class Foo
	{
		void Hello ()
		{
			$M$
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("MainClass"), "class 'MainClass' not found");
		}
		
		[Test()]
		public void TestGenericParameterB ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
class Foo<T>
{
	public void Bar<TValue> ()
	{
		$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("T"), "generic parameter 'T' not found");
			Assert.IsNotNull (provider.Find ("TValue"), "generic parameter 'TValue' found");
		}
		
		[Test()]
		public void TestGenericParameterC ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
class Foo<T>
{
	public static void Bar<TValue> ()
	{
		$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("T"), "generic parameter 'T' not found");
			Assert.IsNotNull (provider.Find ("TValue"), "generic parameter 'TValue' not found");
		}
		
		[Test()]
		public void TestInheritedInnerClasses ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
public class A {
	public class B {
		public void MethodB () 
		{
		}
	}
}
public class C : A 
{
	public override void MethodA (B something)
	{
		$something.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("MethodB"), "method 'MethodB' not found");
		}
		
		[Test()]
		public void TestNamespaceAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
namespace Foo.Bar {
	class B
	{
	}
}

namespace Foo {
	class Test
	{
		void TestMethod ()
		{
			$Bar.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("B"), "class 'B' not found");
		}
		
		[Test()]
		public void TestNamespaceAccess2 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
namespace Foo.Bar {
	class B
	{
	}
}

namespace FooBar {
	using Foo;
	class Test
	{
		void TestMethod ()
		{
			$Bar.$
		}
	}
}");
			// either provider == null, or B not found
			if (provider != null)
				Assert.IsNull (provider.Find ("B"), "class 'B' found, but shouldn't");
		}
		
		
		[Test()]
		public void TestNamespaceAccess3 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
namespace SomeTest.TestNS {
	class TestClass 
	{
		
	}
}

namespace A {
	using SomeTest;
	
	public class Program2
	{
		public void Main () 
		{
			$$
		}
	}
}		
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNull (provider.Find ("TestNS"), "namespace 'TestNS' found, but shouldn't");
		}
		
		[Test()]
		public void TestNamespaceAccess4 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
namespace SomeTest.TestNS {
	class TestClass 
	{
		
	}
}

namespace SomeTest {
	
	public class Program2
	{
		public void Main () 
		{
			$$
		}
	}
}		
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestNS"), "namespace 'TestNS' not found");
		}
		
		[Test()]
		public void TestHideClassesWithPrivateConstructor ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class A
{
}

class TestClass : A
{
	TestClass ()
	{
	}
	
}

class Example
{
	void TestMe ()
	{
		$A a = new $
	}
}		
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("A"), "class 'A' not found");
			Assert.IsNull (provider.Find ("TestClass"), "class 'TestClass' found, but shouldn't.");
		}
		
		[Test()]
		public void TestAttributePropertyAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
public class TestAttribute : System.Attribute
{
	public int MyIntProperty {
		get;
		set;
	}
	
	public string MyStringProperty {
		get;
		set;
	}
}

[Test($M$)]
public class Program
{
	
}	
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("MyIntProperty"), "property 'MyIntProperty' not found");
			Assert.IsNotNull (provider.Find ("MyStringProperty"), "property 'MyStringProperty' not found");
		}
		
		[Test()]
		public void TestInnerClassEnumAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
public class TestInnerEnum
{
	enum InnerEnum { A, B, C }

	public class InnerClass
	{
		void TestMethod (InnerEnum e)
		{
			$e = InnerEnum.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("A"), "field 'A' not found");
			Assert.IsNotNull (provider.Find ("B"), "field 'B' not found");
			Assert.IsNotNull (provider.Find ("C"), "field 'C' not found");
		}
		
		[Test()]
		public void TestInnerClassPrivateOuterMembersAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
public class TestClass
{
	void TestMethod ()
	{
	}
	
	public class InnerClass
	{
		void TestMethod ()
		{
			TestClass tc = new TestClass ();
			$tc.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found");
		}
		
		[Test()]
		public void TestExplicitGenericMethodParameter ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
public class TestClass
{
	public static T TestMethod<T> ()
	{
		return default(T);
	}
}

public class Test
{
	public void TestMethod ()
	{
		$TestClass.TestMethod<Test> ().$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found");
		}

		[Test()]
		public void TestImplicitGenericMethodParameter ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
public class TestClass
{
	public static T TestMethod<T> (T t)
	{
		return t;
	}
}

public class Test
{
	public void TestMethod ()
	{
		$TestClass.TestMethod (this).$
	}
}
 ");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found");
		}
		
		[Test()]
		public void TestImplicitGenericMethodParameterComplex ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
using System;

class SomeTemplate<T>
{
	public T Val { get; set; }
	public SomeTemplate (T val)
	{
		this.Val = val;
	}
}

class Test
{
	public T GetVal<T> (SomeTemplate<T> t)
	{
		return t.Val;
	}
	
	public void TestMethod ()
	{
		SomeTemplate<Test> c = SomeTemplate<Test> (this);
		var x = GetVal (c);
		$x.$
		
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found");
		}

		[Test()]
		public void TestImplicitGenericArrayMethodParameter ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
public class TestClass
{
	public static T[] Test<T> ()
	{
		return default(T[]);
	}
}

public class Test
{
	public void TestMethod ()
	{
		var v = TestClass.Test<Test>();
		$v[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found");
		}
		
		[Test()]
		public void TestExplicitResolving ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
interface IMyInterface {
	object this [object i] { get; }
}

class MyClass<S, T> : IMyInterface
{
	object IMyInterface.this[object i] {
		get {
			return null;
		}
	}
	
	public S this[T i] {
		get {
			return default(S);
		}
	}
}
	
class TestClass
{
	void TestMethod ()
	{
		MyClass<TestClass, string> myClass = new MyClass<TestClass, string> ();
		$myClass[""test""].$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found");
		}
		
		[Test()]
		public void TestAlias ()
			
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
namespace A
{
	public class MyClass 
	{
		public void MyMethod ()
		{
		}
	}
}

namespace X
{
	using GG = A.MyClass;
	
	public abstract class I
	{
		protected virtual GG Foo ()
		{
			return null;
		}
	}
}

namespace X
{
	public class B : I
	{
		public void A ()
		{
			$Foo ().$
		}
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("MyMethod"), "method 'MyMethod' not found");			
		}

		[Test()]
		public void TestEnumInnerClass ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
using System;
namespace CaptainHook.Mail
{
	public class TestClass
	{
		enum ParsingState
		{
			Any,
			Start,
			InMacro,
			InMacroArgumentList,
			InQuotedMacroArgument,
			PlainText
		}

		ParsingState state;

		public TestClass ()
		{
			$state = $
		}
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNull (provider.Find ("CaptainHook.Mail.TestClass.ParsingState"), "class 'CaptainHook.Mail.TestClass.ParsingState' found!");
			Assert.IsNull (provider.Find ("TestClass.ParsingState"), "class 'TestClass.ParsingState' found!");
			Assert.IsNotNull (provider.Find ("ParsingState"), "class 'ParsingState' not found");
		}
		
		[Test()]
		public void TestInheritableTypeContext ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	public class Inner {}
	public static void Foo () {}
}

$class Test2 : Test.$
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Inner"), "class 'Inner' not found.");
			Assert.IsNull (provider.Find ("Foo"), "method 'Foo' found.");
		}
		
		[Test()]
		public void TestInheritableTypeContextCase2 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
namespace A {
	class Test
	{
		public class Inner {}
		public static void Foo () {}
	}
	
	class Test2 $: Test.$
	{
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Inner"), "class 'Inner' not found.");
			Assert.IsNull (provider.Find ("Foo"), "method 'Foo' found.");
		}
		
		
		[Test()]
		public void TestInheritableTypeWhereContext ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	public class Inner {}
	public static void Foo () {}
}

$class Test2<T> where T : Test.$
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Inner"), "class 'Inner' not found.");
			Assert.IsNull (provider.Find ("Foo"), "method 'Foo' found.");
		}
		
		[Test()]
		public void TestEnumAssignment ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
public enum TestEnum { A, B, C}

class TestClass
{
	public void Foo ()
	{
		$TestEnum test = $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TestEnum"), "enum 'TestEnum' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.A"), "enum 'TestEnum.A' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.B"), "enum 'TestEnum.B' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.C"), "enum 'TestEnum.C' not found.");
		}
		
		[Test()]
		public void TestEnumAssignmentCase2 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
public enum TestEnum { A, B, C}

class TestClass
{
	public void Foo ()
	{
		TestEnum test;
		$test = $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TestEnum"), "enum 'TestEnum' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.A"), "enum 'TestEnum.A' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.B"), "enum 'TestEnum.B' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.C"), "enum 'TestEnum.C' not found.");
		}
		
		[Test()]
		public void TestEnumAsParameter ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
public enum TestEnum { A, B, C}

class TestClass
{
	void Bar (TestEnum test) {}
	public void Foo ()
	{
		$Bar ($
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TestEnum"), "enum 'TestEnum' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.A"), "enum 'TestEnum.A' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.B"), "enum 'TestEnum.B' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.C"), "enum 'TestEnum.C' not found.");
		}
	
		[Test()]
		public void TestEnumAsParameterCase2 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
public enum TestEnum { A, B, C}

class TestClass
{
	void Bar (int a, TestEnum test) {}
	public void Foo ()
	{
		$Bar (5, $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TestEnum"), "enum 'TestEnum' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.A"), "enum 'TestEnum.A' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.B"), "enum 'TestEnum.B' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.C"), "enum 'TestEnum.C' not found.");
		}
		
		[Test()]
		public void TestInnerEnums ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
public class InnerEnumTest
{
	public enum TestEnum { A, B, C}
	public void Bar (TestEnum test) {}
}

class TestClass
{
	public void Foo ()
	{
		InnerEnumTest test;
		$test.Bar ($
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("InnerEnumTest.TestEnum"), "enum 'InnerEnumTest.TestEnum' not found.");
			Assert.IsNotNull (provider.Find ("InnerEnumTest.TestEnum.A"), "enum 'InnerEnumTest.TestEnum.A' not found.");
			Assert.IsNotNull (provider.Find ("InnerEnumTest.TestEnum.B"), "enum 'InnerEnumTest.TestEnum.B' not found.");
			Assert.IsNotNull (provider.Find ("InnerEnumTest.TestEnum.C"), "enum 'InnerEnumTest.TestEnum.C' not found.");
		}

		[Test()]
		public void TestEnumInBinaryOperatorExpression ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"
[Flags]
public enum TestEnum { A, B, C}

class TestClass
{
	public void Foo ()
	{
		$TestEnum test = TestEnum.A | T$
	}
}", provider => {
			Assert.IsNotNull (provider.Find ("TestEnum"), "enum 'TestEnum' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.A"), "enum 'TestEnum.A' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.B"), "enum 'TestEnum.B' not found.");
			Assert.IsNotNull (provider.Find ("TestEnum.C"), "enum 'TestEnum.C' not found.");
			});
		}
		
		[Test()]
		public void TestPrimimitiveTypeCompletionString ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"using System;

class Test
{
	public static void Foo () 
	{
		Console.WriteLine ($"""".$);
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("ToString"), "method 'ToString' not found.");
		}
		
		
		[Test()]
		public void TestUsingContext ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (@"$using System.$");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("IO"), "namespace 'IO' not found.");
		}
		
		[Test()]
		public void TestNamedArgumentContext1 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (@"
using System;

class Test {
public static void Query(MySqlConnection conn, string database, string table)
		{
			conn.Query(string.Format(""SELECT * FROM {0}"", table))
			.On(row: delegate (Row data) {
				$Console.$
			});
		}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("WriteLine"), "method 'WriteLine' not found.");
		}
		[Test()]
		public void TestAttributeContextClass ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"
using System;

$[O$
class Test {
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Obsolete"), "attribute 'Obsolete' not found.");
			Assert.IsNotNull (provider.Find ("Serializable"), "attribute 'Serializable' not found.");
		}
		
		[Test()]
		public void TestAttributeContextInNamespace ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"
using System;

namespace Test {
	$[O$
	class Test {
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Obsolete"), "attribute 'Obsolete' not found.");
			Assert.IsNotNull (provider.Find ("Serializable"), "attribute 'Serializable' not found.");
		}
		
		[Test()]
		public void TestAttributeContextMember ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"
using System;

class Test {
	$[O$
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Obsolete"), "attribute 'Obsolete' not found.");
			Assert.IsNotNull (provider.Find ("Serializable"), "attribute 'Serializable' not found.");
		}
		
		[Test()]
		public void TestAttributeInNonAttributeContext ()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (@"
using System;

class Test {
$$
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("ObsoleteAttribute"), "attribute 'ObsoleteAttribute' not found.");
		}
		
		// 'from' in comment activates linq context
		[Test()]
		public void TestBreakingComment ()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (@"
using System;
// from
class Test {
$$
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
		}
		
		[Test()]
		public void TestAttributeContextParameterCompletion ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$[Obsolete(System.$");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Console"), "'Console' not found.");
		}
		
		
		/// <summary>
		/// Bug 3320 - Constants accessed by class name do not show in completion list
		/// </summary>
		[Test()]
		public void TestBug3320 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"
public class Foo
{
    public const int Bar = 5;

    public void DoStuff()
    {
        $Foo.$
    } 
}", provider => {
				Assert.IsNotNull (provider.Find ("Bar"), "'Bar' not found.");
			});
		}
	}
}