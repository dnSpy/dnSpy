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
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture]
	public class OperatorDeclarationTests
	{
		[Test]
		public void ImplictOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static implicit operator double(MyObject f)  { return 0.5d; }");
			Assert.AreEqual(OperatorType.Implicit, od.OperatorType);
			Assert.AreEqual(1, od.Parameters.Count());
			Assert.AreEqual("double", ((PrimitiveType)od.ReturnType).Keyword);
			Assert.AreEqual("op_Implicit", od.Name);
		}
		
		[Test]
		public void ExplicitOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static explicit operator double(MyObject f)  { return 0.5d; }");
			Assert.AreEqual(OperatorType.Explicit, od.OperatorType);
			Assert.AreEqual(1, od.Parameters.Count());
			Assert.AreEqual("double", ((PrimitiveType)od.ReturnType).Keyword);
			Assert.AreEqual("op_Explicit", od.Name);
		}
		
		[Test]
		public void BinaryPlusOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static MyObject operator +(MyObject a, MyObject b)  {}");
			Assert.AreEqual(OperatorType.Addition, od.OperatorType);
			Assert.AreEqual(2, od.Parameters.Count());
			Assert.AreEqual("MyObject", ((SimpleType)od.ReturnType).Identifier);
			Assert.AreEqual("op_Addition", od.Name);
		}
		
		[Test]
		public void UnaryPlusOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static MyObject operator +(MyObject a)  {}");
			Assert.AreEqual(OperatorType.UnaryPlus, od.OperatorType);
			Assert.AreEqual(1, od.Parameters.Count());
			Assert.AreEqual("MyObject", ((SimpleType)od.ReturnType).Identifier);
			Assert.AreEqual("op_UnaryPlus", od.Name);
		}
	}
}
