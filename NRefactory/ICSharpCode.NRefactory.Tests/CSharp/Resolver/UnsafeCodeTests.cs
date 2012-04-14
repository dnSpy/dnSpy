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
using ICSharpCode.NRefactory.Semantics;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class UnsafeCodeTests : ResolverTestBase
	{
		[Test]
		public void FixedStatement()
		{
			string program = @"using System;
class TestClass {
	static void Main(byte[] a) {
		fixed (byte* p = a) {
			a = $p$;
		} } }";
			
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Byte*", lrr.Type.ReflectionName);
			
			var rr = Resolve<OperatorResolveResult>(program.Replace("$p$", "$*p$"));
			Assert.AreEqual("System.Byte", rr.Type.ReflectionName);
		}
		
		[Test]
		public void FixedStatementArrayPointerConversion()
		{
			string program = @"using System;
class TestClass {
	static void Main(byte[] a) {
		fixed (byte* p = $a$) {
		} } }";
			Assert.AreEqual("System.Byte*", GetExpectedType(program).ReflectionName);
			Assert.AreEqual(Conversion.ImplicitPointerConversion, GetConversion(program));
		}
		
		[Test]
		public void FixedStatementStringPointerConversion()
		{
			string program = @"using System;
class TestClass {
	static void Main(string a) {
		fixed (char* p = $a$) {
		} } }";
			Assert.AreEqual("System.Char*", GetExpectedType(program).ReflectionName);
			Assert.AreEqual(Conversion.ImplicitPointerConversion, GetConversion(program));
		}
	}
}
