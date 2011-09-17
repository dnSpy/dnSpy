// 
// TestTypeLevelIndentation.cs
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
	public class TestTypeLevelIndentation : TestBase
	{
		[Test()]
		public void TestClassIndentation ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.ClassBraceStyle = BraceStyle.DoNotChange;
			
			Test (policy,
@"			class Test {}",
@"class Test {}");
		}
		
		[Test()]
		public void TestAttributeIndentation ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.ClassBraceStyle = BraceStyle.DoNotChange;
			
			Test (policy,
@"					[Attribute1]
		[Attribute2()]
          class Test {}",
@"[Attribute1]
[Attribute2()]
class Test {}");
		}
		
		[Test()]
		public void TestClassIndentationInNamespaces ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.NamespaceBraceStyle = BraceStyle.EndOfLine;
			policy.ClassBraceStyle = BraceStyle.DoNotChange;
			
			Test (policy,
@"namespace A { class Test {} }",
@"namespace A {
	class Test {}
}");
		}
		
		[Test()]
		public void TestNoIndentationInNamespaces ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.NamespaceBraceStyle = BraceStyle.EndOfLine;
			policy.ClassBraceStyle = BraceStyle.DoNotChange;
			policy.IndentNamespaceBody = false;
			
			Test (policy,
@"namespace A { class Test {} }",
@"namespace A {
class Test {}
}");
		}
		
		[Test()]
		public void TestClassIndentationInNamespacesCase2 ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.NamespaceBraceStyle = BraceStyle.NextLine;
			policy.ClassBraceStyle = BraceStyle.NextLine;
			policy.ConstructorBraceStyle = BraceStyle.NextLine;
			
			Test (policy,
@"using System;

namespace MonoDevelop.CSharp.Formatting {
	public class FormattingProfileService {
		public FormattingProfileService () {
		}
	}
}",
@"using System;

namespace MonoDevelop.CSharp.Formatting
{
	public class FormattingProfileService
	{
		public FormattingProfileService ()
		{
		}
	}
}");
		}
		
		[Test()]
		public void TestIndentClassBody ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.IndentClassBody = true;
			Test (policy,
@"class Test
{
				Test a;
}", @"class Test
{
	Test a;
}");
			
			policy.IndentClassBody = false;
			Test (policy,
@"class Test
{
	Test a;
}",
@"class Test
{
Test a;
}");
		}
		
		[Test()]
		public void TestIndentInterfaceBody ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.IndentInterfaceBody = true;
			
			Test (policy,
@"interface Test
{
				Test Foo ();
}", @"interface Test
{
	Test Foo ();
}");
			policy.IndentInterfaceBody = false;
			Test (policy,
@"interface Test
{
	Test Foo ();
}", @"interface Test
{
Test Foo ();
}");
		}
		
		[Test()]
		public void TestIndentStructBody ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.IndentStructBody = true;
			
			Test (policy,
@"struct Test
{
				Test Foo ();
}", @"struct Test
{
	Test Foo ();
}");
			policy.IndentStructBody = false;
			Test (policy,
@"struct Test
{
	Test Foo ();
}", @"struct Test
{
Test Foo ();
}");
		}
		
		[Test()]
		public void TestIndentEnumBody ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.IndentEnumBody = true;
			
			Test (policy,
@"enum Test
{
				A
}", @"enum Test
{
	A
}");
			policy.IndentEnumBody = false;
			Test (policy,
@"enum Test
{
	A
}", @"enum Test
{
A
}");
		}
		
		[Test()]
		public void TestIndentMethodBody ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.IndentMethodBody = true;
			
			Test (policy,
@"class Test
{
	Test Foo ()
	{
;
								;
	}
}",
@"class Test
{
	Test Foo ()
	{
		;
		;
	}
}");
			policy.IndentMethodBody = false;
			Test (policy,
@"class Test
{
	Test Foo ()
	{
		;
		;
	}
}",
@"class Test
{
	Test Foo ()
	{
	;
	;
	}
}");
		}
		
		[Test()]
		public void TestIndentMethodBodyOperatorCase ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.IndentMethodBody = true;

			var adapter = Test (policy,
@"class Test
{
	static Test operator+(Test left, Test right)
	{
;
								;
	}
}",
@"class Test
{
	static Test operator+ (Test left, Test right)
	{
		;
		;
	}
}");
			policy.IndentMethodBody = false;
			Continue (policy, adapter, @"class Test
{
	static Test operator+ (Test left, Test right)
	{
	;
	;
	}
}");
		}
		
		[Test()]
		public void TestIndentPropertyBody ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.IndentPropertyBody = true;
			
			var adapter = Test (policy,
@"class Test
{
	Test TestMe {
			get;
set;
	}
}",
@"class Test
{
	Test TestMe {
		get;
		set;
	}
}");
			policy.IndentPropertyBody = false;
			
			Continue (policy, adapter,
@"class Test
{
	Test TestMe {
	get;
	set;
	}
}");
		}
		
		[Test()]
		public void TestIndentPropertyOneLine ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.PropertyFormatting = PropertyFormatting.AllowOneLine;
			policy.AllowPropertyGetBlockInline = true;
			policy.AllowPropertySetBlockInline = true;
			
			Test (policy,
@"class Test
{
	Test TestMe {      get;set;                  }
}",
@"class Test
{
	Test TestMe { get; set; }
}");
		}
		
		[Test()]
		public void TestIndentPropertyOneLineCase2 ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.PropertyFormatting = PropertyFormatting.AllowOneLine;
			policy.AllowPropertyGetBlockInline = true;
			policy.AllowPropertySetBlockInline = true;
			
			Test (policy,
@"class Test
{
	Test TestMe {      get { ; }set{;}                  }
}",
@"class Test
{
	Test TestMe { get { ; } set { ; } }
}");
		}
		
		[Test()]
		public void TestIndentPropertyBodyIndexerCase ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.IndentPropertyBody = true;
			
			var adapter = Test (policy,
@"class Test
{
	Test this[int a] {
			get {
	return null;
}
set {
	;
}
	}
}",
@"class Test
{
	Test this [int a] {
		get {
			return null;
		}
		set {
			;
		}
	}
}");
			policy.IndentPropertyBody = false;
			Continue (policy, adapter,
@"class Test
{
	Test this [int a] {
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
		public void TestPropertyAlignment ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.PropertyFormatting = PropertyFormatting.AllowOneLine;
			var adapter = Test (policy,
@"class Test
{
	Test TestMe { get; set; }
}",
@"class Test
{
	Test TestMe { get; set; }
}");
			policy.PropertyFormatting = PropertyFormatting.ForceNewLine;
			Continue (policy, adapter,
@"class Test
{
	Test TestMe {
		get;
		set;
	}
}");
			policy.PropertyFormatting = PropertyFormatting.ForceOneLine;
			
			Continue (policy, adapter,
@"class Test
{
	Test TestMe { get; set; }
}");
		}

		
		[Test()]
		public void TestIndentNamespaceBody ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.ClassBraceStyle = BraceStyle.DoNotChange;
			policy.NamespaceBraceStyle = BraceStyle.EndOfLine;
			policy.IndentNamespaceBody = true;
			var adapter = Test (policy,
@"			namespace Test {
class FooBar {}
		}",
@"namespace Test {
	class FooBar {}
}");
			
			policy.IndentNamespaceBody = false;
			Continue (policy, adapter,
@"namespace Test {
class FooBar {}
}");
		}
		
		
		[Test()]
		public void TestMethodIndentation ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.MethodBraceStyle = BraceStyle.DoNotChange;
			
			Test (policy,
@"class Test
{
MyType TestMethod () {}
}",
@"class Test
{
	MyType TestMethod () {}
}");
		}
		
		[Test()]
		public void TestPropertyIndentation ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.PropertyBraceStyle = BraceStyle.DoNotChange;
			
			Test (policy, 
@"class Test
{
				public int Prop { get; set; }
}",@"class Test
{
	public int Prop { get; set; }
}");
		}
		
		[Test()]
		public void TestPropertyIndentationCase2 ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			
			Test (policy, 
@"class Test
{
				public int Prop {
 get;
set;
}
}",
@"class Test
{
	public int Prop {
		get;
		set;
	}
}");
		}
		
		
		[Test()]
		public void TestIndentEventBody ()
		{
			CSharpFormattingOptions policy = new CSharpFormattingOptions ();
			policy.IndentEventBody = true;
			
			var adapter = Test (policy, 
@"class Test
{
	public event EventHandler TestMe {
								add {
							;
						}
remove {
	;
}
	}
}",
@"class Test
{
	public event EventHandler TestMe {
		add {
			;
		}
		remove {
			;
		}
	}
}");
			policy.IndentEventBody = false;
			Continue (policy, adapter,
@"class Test
{
	public event EventHandler TestMe {
	add {
		;
	}
	remove {
		;
	}
	}
}");
		}
	}
}
