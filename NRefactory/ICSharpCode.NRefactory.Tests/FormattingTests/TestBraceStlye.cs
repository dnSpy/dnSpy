// 
// TestBraceStyle.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.FormattingTests
{
	[TestFixture()]
	public class TestBraceStyle : TestBase
	{
		[Test()]
		public void TestNamespaceBraceStyle ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.NamespaceBraceStyle = BraceStyle.EndOfLine;
			policy.ClassBraceStyle = BraceStyle.DoNotChange;
			
			var adapter = Test (policy, @"namespace A
{
namespace B {
	class Test {}
}
}",
@"namespace A {
	namespace B {
		class Test {}
	}
}");
			
			policy.NamespaceBraceStyle = BraceStyle.NextLineShifted;
			Continue (policy, adapter,
@"namespace A
	{
	namespace B
		{
		class Test {}
		}
	}");
		}

		[Test()]
		public void TestClassBraceStlye ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.ClassBraceStyle = BraceStyle.EndOfLine;
			
			Test (policy,
@"class Test {}",
@"class Test {
}");
		}

		[Test()]
		public void TestStructBraceStyle ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.StructBraceStyle = BraceStyle.NextLine;
			
			Test (policy,
@"struct Test {}",
@"struct Test
{
}");
		}

		[Test()]
		public void TestInterfaceBraceStyle ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.InterfaceBraceStyle = BraceStyle.NextLine;
			
			Test (policy,
@"interface Test {}",
@"interface Test
{
}");
		}

		[Test()]
		public void TestEnumBraceStyle ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.EnumBraceStyle = BraceStyle.NextLineShifted;
			
			Test (policy, @"enum Test {
	A
}",
@"enum Test
	{
	A
	}");
		}

		[Test()]
		public void TestMethodBraceStlye ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.MethodBraceStyle = BraceStyle.NextLine;
			
			Test (policy, @"class Test
{
	Test MyMethod() {}
}",
@"class Test
{
	Test MyMethod ()
	{
	}
}");
		}

		[Test()]
		public void TestConstructorBraceStyle ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.ConstructorBraceStyle = BraceStyle.NextLine;
			
			Test (policy, @"class Test
{
	Test() {}
}",
@"class Test
{
	Test ()
	{
	}
}");
		}

		[Test()]
		public void TestDestructorBraceStyle ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.DestructorBraceStyle = BraceStyle.NextLine;
			
			Test (policy, @"class Test
{
	~Test() {}
}",
@"class Test
{
	~Test ()
	{
	}
}");
		}

		[Test()]
		public void TestPropertyBraceStyle ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.PropertyBraceStyle = BraceStyle.NextLine;
			
			Test (policy, @"class Test
{
	Test A {
		get;
		set;
	}
}",
@"class Test
{
	Test A
	{
		get;
		set;
	}
}");
		}

		[Test()]
		public void TestPropertyGetBraceStyle ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.PropertyGetBraceStyle = BraceStyle.NextLine;
			
			Test (policy, @"class Test
{
	Test A {
		get {
			return null;
		}
		set;
	}
}",
@"class Test
{
	Test A {
		get
		{
			return null;
		}
		set;
	}
}");
		}

		[Test()]
		public void TestAllowPropertyGetBlockInline ()
		{
			
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.PropertyBraceStyle = BraceStyle.DoNotChange;
			policy.AllowPropertyGetBlockInline = true;
			policy.AllowPropertySetBlockInline = false;
			
			var adapter = Test (policy, @"class Test
{
	Test A {
		get { return null; }
		set { ; }
	}
}",
@"class Test
{
	Test A {
		get { return null; }
		set {
			;
		}
	}
}");
			
			policy.AllowPropertyGetBlockInline = false;
			Continue (policy, adapter,
@"class Test
{
	Test A {
		get {
			return null;
		}
		set {
			;
		}
	}
}");
		}

		[Test()]
		public void TestAllowPropertySetBlockInline ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.PropertyBraceStyle = BraceStyle.DoNotChange;
			policy.AllowPropertyGetBlockInline = false;
			policy.AllowPropertySetBlockInline = true;
			
			var adapter = Test (policy, @"class Test
{
	Test A {
		get { return null; }
		set { ; }
	}
}",
@"class Test
{
	Test A {
		get {
			return null;
		}
		set { ; }
	}
}");
			
			policy.AllowPropertySetBlockInline = false;
			Continue (policy, adapter,
@"class Test
{
	Test A {
		get {
			return null;
		}
		set {
			;
		}
	}
}");
		}

		[Test()]
		public void TestPropertySetBraceStyle ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.PropertySetBraceStyle = BraceStyle.NextLine;
			
			Test (policy, @"class Test
{
	Test A {
		get;
		set {
			;
		}
	}
}",
@"class Test
{
	Test A {
		get;
		set
		{
			;
		}
	}
}");
		}

		[Test()]
		public void TestEventBraceStyle ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.EventBraceStyle = BraceStyle.NextLine;
			policy.EventAddBraceStyle = BraceStyle.NextLine;
			policy.EventRemoveBraceStyle = BraceStyle.NextLine;
			
			Test (policy, @"class Test
{
	public event EventHandler Handler {
		add {
}
		remove {
}
	}
}",
@"class Test
{
	public event EventHandler Handler
	{
		add
		{
		}
		remove
		{
		}
	}
}");
		}

		[Test()]
		public void TestAllowEventAddBlockInline ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.AllowEventAddBlockInline = true;
			policy.AllowEventRemoveBlockInline = false;
			
			Test (policy, @"class Test
{
	public event EventHandler Handler {
		add { ; }
		remove { ; }
	}
}",
@"class Test
{
	public event EventHandler Handler {
		add { ; }
		remove {
			;
		}
	}
}");
		}

		[Test()]
		public void TestAllowEventRemoveBlockInline ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.AllowEventAddBlockInline = false;
			policy.AllowEventRemoveBlockInline = true;
			
			Test (policy, @"class Test
{
	public event EventHandler Handler {
		add { ; }
		remove { ; }
	}
}",
@"class Test
{
	public event EventHandler Handler {
		add {
			;
		}
		remove { ; }
	}
}");
		}
	}
}