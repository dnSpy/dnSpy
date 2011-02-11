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
/*
using System;
using NUnit.Framework;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.CSharp.Completion;
using Mono.TextEditor;
using MonoDevelop.CSharp.Formatting;

namespace MonoDevelop.CSharpBinding.FormattingTests
{
	[TestFixture()]
	public class TestTypeLevelIndentation : UnitTests.TestBase
	{
		[Test()]
		public void TestClassIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"			class Test {}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.DoNotChange;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {}", data.Document.Text);
		}
		
		[Test()]
		public void TestIndentClassBody ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"class Test
{
				Test a;
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IndentClassBody = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test a;
}", data.Document.Text);
			policy.IndentClassBody = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
Test a;
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIndentInterfaceBody ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"interface Test
{
				Test Foo ();
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IndentInterfaceBody = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"interface Test
{
	Test Foo ();
}", data.Document.Text);
			policy.IndentInterfaceBody = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"interface Test
{
Test Foo ();
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIndentStructBody ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"struct Test
{
				Test a;
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IndentStructBody = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"struct Test
{
	Test a;
}", data.Document.Text);
			policy.IndentStructBody = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"struct Test
{
Test a;
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIndentEnumBody ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"enum Test
{
								A
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IndentEnumBody = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"enum Test
{
	A
}", data.Document.Text);
			policy.IndentEnumBody = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"enum Test
{
A
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIndentMethodBody ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"class Test
{
	Test Foo ()
	{
;
								;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IndentMethodBody = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test Foo ()
	{
		;
		;
	}
}", data.Document.Text);
			policy.IndentMethodBody = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test Foo ()
	{
	;
	;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIndentMethodBodyOperatorCase ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"class Test
{
	static Test operator+(Test left, Test right)
	{
;
								;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IndentMethodBody = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	static Test operator+(Test left, Test right)
	{
		;
		;
	}
}", data.Document.Text);
			policy.IndentMethodBody = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	static Test operator+(Test left, Test right)
	{
	;
	;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIndentPropertyBody ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"class Test
{
	Test TestMe {
			get;
set;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IndentPropertyBody = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMe {
		get;
		set;
	}
}", data.Document.Text);
			policy.IndentPropertyBody = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test TestMe {
	get;
	set;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestIndentPropertyBodyIndexerCase ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
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
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IndentPropertyBody = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test this[int a] {
		get {
			return null;
		}
		set {
			;
		}
	}
}", data.Document.Text);
			policy.IndentPropertyBody = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	Test this[int a] {
	get {
		return null;
	}
	set {
		;
	}
	}
}", data.Document.Text);
		}
		
		
		[Test()]
		[Ignore("currently failing because namespaces are not inserted")]
		public void TestIndentNamespaceBody ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"			namespace Test {
class FooBar {}
		}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.DoNotChange;
			policy.IndentNamespaceBody = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"namespace Test {
	class FooBar {}
}", data.Document.Text);
			
			policy.IndentNamespaceBody = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"namespace Test {
class FooBar {}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestMethodIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"class Test
{
MyType TestMethod () {}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.MethodBraceStyle =  BraceStyle.DoNotChange;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	MyType TestMethod () {}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestPropertyIndentation ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"class Test
{
				public int Prop { get; set; }
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PropertyBraceStyle =  BraceStyle.DoNotChange;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	public int Prop { get; set; }
}", data.Document.Text);
		}
		
		[Test()]
		public void TestPropertyIndentationCase2 ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
@"class Test
{
				public int Prop {
 get;
set;
}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	public int Prop {
		get;
		set;
	}
}", data.Document.Text);
		}
		
		
		[Test()]
		public void TestIndentEventBody ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = 
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
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.IndentEventBody = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	public event EventHandler TestMe {
		add {
			;
		}
		remove {
			;
		}
	}
}", data.Document.Text);
			policy.IndentEventBody = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	public event EventHandler TestMe {
	add {
		;
	}
	remove {
		;
	}
	}
}", data.Document.Text);
		}
	}
}*/
