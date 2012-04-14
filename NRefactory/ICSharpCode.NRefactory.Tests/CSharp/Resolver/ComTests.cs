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
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class ComTests : ResolverTestBase
	{
		[Test]
		public void NewCoClass()
		{
			string program = @"using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""698D8281-3890-41A6-8A2F-DBC29CBAB8BC""), CoClass(typeof(Test))]
public interface Dummy { }

public class Test : Dummy {
	public Test(int x = 1) {}
	
	static void Main() {
		$
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "var x = $new Dummy()$;"));
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("Dummy", rr.Type.ReflectionName);
			Assert.AreEqual(1, rr.Member.Parameters.Count);
		}
	}
}
