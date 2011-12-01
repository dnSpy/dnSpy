// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class LocalTypeInferenceTests : ResolverTestBase
	{
		[Test]
		public void TypeInferenceTest()
		{
			string program = @"class TestClass {
	static void Test() {
		var a = 3;
		$a$.ToString();
	}
}
";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.FullName);
		}
		
		[Test]
		public void TypeInferenceCycleTest()
		{
			string program = @"class TestClass {
	static void Test() {
		var a = a;
		$a$.ToString();
	}
}
";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreSame(SpecialType.UnknownType, lrr.Type);
		}
		
		[Test]
		public void InvalidAnonymousTypeDeclaration()
		{
			// see SD-1393
			string program = @"using System;
class TestClass {
	static void Main() {
		var contact = {id = 54321};
		$contact$.ToString();
	} }";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual(SpecialType.UnknownType, lrr.Type);
		}
		
		[Test]
		public void Foreach_InferFromArrayType()
		{
			string program = @"using System;
class TestClass {
	static void Method(int[] arr) {
		foreach ($var$ x in arr) {}
	} }";
			var rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Int32", rr.Type.ReflectionName);
		}
		
		[Test]
		public void Foreach_InferFromDynamic()
		{
			string program = @"using System;
class TestClass {
	static void Method(dynamic c) {
		foreach ($var$ x in c) {}
	} }";
			var rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual(TypeKind.Dynamic, rr.Type.Kind);
		}
		
		[Test]
		public void Foreach_InferFromListOfInt()
		{
			string program = @"using System;
using System.Collections.Generic;
class TestClass {
	static void Method(List<int> c) {
		foreach ($var$ x in c) {}
	} }";
			var rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Int32", rr.Type.ReflectionName);
		}
		
		[Test]
		public void Foreach_InferFromICollectionOfInt()
		{
			string program = @"using System;
using System.Collections.Generic;
class TestClass {
	static void Method(ICollection<int> c) {
		foreach ($var$ x in c) {}
	} }";
			var rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Int32", rr.Type.ReflectionName);
		}
		
		[Test]
		public void Foreach_InferFromCustomCollection_WithoutIEnumerable()
		{
			string program = @"using System;
using System.Collections.Generic;
class TestClass {
	static void Method(CustomCollection c) {
		foreach ($var$ x in c) {}
	}
}
class CustomCollection {
	public MyEnumerator GetEnumerator() {}
	public struct MyEnumerator {
		public string Current { get { return null; } }
		public bool MoveNext() { return false; }
	}
}
";
			var rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.String", rr.Type.ReflectionName);
		}
		
		[Test]
		public void Foreach_InferFromCustomCollection_WithIEnumerableAndPublicGetEnumerator()
		{
			string program = @"using System;
using System.Collections.Generic;
class TestClass {
	static void Method(CustomCollection c) {
		foreach ($var$ x in c) {}
	}
}
class CustomCollection : IEnumerable<int> {
	public MyEnumerator GetEnumerator() {}
	public struct MyEnumerator {
		public string Current { get { return null; } }
		public bool MoveNext() { return false; }
	}
}
";
			var rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.String", rr.Type.ReflectionName);
		}
		
		[Test]
		public void Foreach_InferFromCustomCollection_WithIEnumerableAndInternalGetEnumerator()
		{
			string program = @"using System;
using System.Collections.Generic;
class TestClass {
	static void Method(CustomCollection c) {
		foreach ($var$ x in c) {}
	}
}
class CustomCollection : IEnumerable<int> {
	internal MyEnumerator GetEnumerator() {}
	public struct MyEnumerator {
		public string Current { get { return null; } }
		public bool MoveNext() { return false; }
	}
}
";
			var rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Int32", rr.Type.ReflectionName);
		}
		
		[Test]
		public void Foreach_InferFromCustomCollection_WithIEnumerableAndGetEnumeratorExtensionMethod()
		{
			string program = @"using System;
using System.Collections.Generic;
class TestClass {
	static void Method(CustomCollection c) {
		foreach ($var$ x in c) {}
	}
}
class CustomCollection : IEnumerable<int> {
	public struct MyEnumerator {
		public string Current { get { return null; } }
		public bool MoveNext() { return false; }
	}
}
static class ExtMethods {
	public static CustomCollection.MyEnumerator GetEnumerator(this CustomCollection c) {
		throw new NotImplementedException();
	}
}";
			var rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Int32", rr.Type.ReflectionName);
		}
	}
}
