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
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class ArrayCreateTests : ResolverTestBase
	{
		[Test]
		public void SimpleArrayCreation()
		{
			string program = @"using System.Collections.Generic;
class A {
	static void Main() {
		var a = $new int[] { 42 }$;
	}
}
";
			var result = Resolve(program);
			Assert.AreEqual("System.Int32[]", result.Type.ReflectionName);
		}
		
		[Test]
		public void NestedArrayCreation()
		{
			string program = @"using System.Collections.Generic;
class A {
	static void Main() {
		var a = $new int[2][,][,,]$;
	}
}
";
			var result = Resolve(program);
			// a one-dimensional array of two-dimensional arrays of three-dimensional arrays
			ArrayType a1 = (ArrayType)result.Type;
			Assert.AreEqual(1, a1.Dimensions);
			ArrayType a2 = (ArrayType)a1.ElementType;
			Assert.AreEqual(2, a2.Dimensions);
			ArrayType a3 = (ArrayType)a2.ElementType;
			Assert.AreEqual(3, a3.Dimensions);
			Assert.AreEqual("System.Int32", a3.ElementType.ReflectionName);
		}
		
		[Test]
		public void InferredType()
		{
			string program = @"using System.Collections.Generic;
class A {
	static void Main() {
		var a = $new [] { 1, 1L }$;
	}
}
";
			var result = Resolve(program);
			Assert.AreEqual("System.Int64[]", result.Type.ReflectionName);
		}
		
		[Test]
		public void InferredType2D()
		{
			string program = @"using System.Collections.Generic;
class A {
	static void Main() {
		var a = $new [,] { { 1 }, { 1L } }$;
	}
}
";
			var result = Resolve(program);
			Assert.AreEqual("System.Int64[,]", result.Type.ReflectionName);
		}
		
		[Test]
		public void ArrayInitializerExpression()
		{
			string program = @"using System.Collections.Generic;
class A {
	int[] a = ${ 1 }$;
}
";
			var result = Resolve(program);
			Assert.AreEqual("System.Int32[]", result.Type.ReflectionName);
		}
		
		[Test]
		public void ArrayInitializerExpression2D()
		{
			string program = @"using System.Collections.Generic;
class A {
	int[,] a = ${ { 1 }, { 2 } }$;
}
";
			var result = Resolve(program);
			Assert.AreEqual("System.Int32[,]", result.Type.ReflectionName);
		}
	}
}
