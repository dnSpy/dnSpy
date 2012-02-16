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
using NUnit.Framework;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
	public class TypeReferenceExpressionTests
	{
		[Test]
		public void GlobalTypeReferenceExpression()
		{
			TypeReferenceExpression tr = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("global::System");
			Assert.IsTrue (tr.IsMatch (new TypeReferenceExpression () {
				Type = new MemberType () {
					Target = new SimpleType ("global"),
					IsDoubleColon = true,
					MemberName = "System"
				}
			}));
		}
		
		[Test]
		public void IntReferenceExpression()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("int.MaxValue");
			Assert.IsTrue (fre.IsMatch (new MemberReferenceExpression () {
				Target = new TypeReferenceExpression () {
					Type = new PrimitiveType("int")
				},
				MemberName = "MaxValue"
			}));
		}
		
		[Test]
		public void Bool_TrueString()
		{
			ParseUtilCSharp.AssertExpression("bool.TrueString", new PrimitiveType("bool").Member("TrueString"));
		}
		
	/*	[Test]
		public void StandaloneIntReferenceExpression()
		{
		// doesn't work because a = int; gives a compiler error.
		// But how do we handle this case for code completion?
			TypeReferenceExpression tre = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("int");
			Assert.IsNotNull (tre.Match (new TypeReferenceExpression () {
				Type = new SimpleType ("int")
			}));
		}*/
		
	}
}
