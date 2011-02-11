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
	public class TestBraceStyle : UnitTests.TestBase
	{
		[Test()]
		[Ignore("currently failing because namespaces are not inserted")]
		public void TestNamespaceBraceStyle ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"namespace A
{
namespace B {
	class Test {}
}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.NamespaceBraceStyle = BraceStyle.EndOfLine;
			policy.ClassBraceStyle = BraceStyle.DoNotChange;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"namespace A {
	namespace B {
		class Test {}
	}
}", data.Document.Text);
			
			policy.NamespaceBraceStyle = BraceStyle.NextLineShifted;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"namespace A 
	{
	namespace B
		{
		class Test {}
		}
	}", data.Document.Text);
		}
		
		
		[Test()]
		public void TestClassBraceStlye ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test {}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ClassBraceStyle =  BraceStyle.EndOfLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test {
}", data.Document.Text);
		}
		
		[Test()]
		public void TestStructBraceStyle ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"struct Test {}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.StructBraceStyle =  BraceStyle.NextLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"struct Test
{
}", data.Document.Text);
		}
		
		[Test()]
		public void TestInterfaceBraceStyle ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"interface Test {}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.InterfaceBraceStyle =  BraceStyle.NextLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"interface Test
{
}", data.Document.Text);
		}
		
		[Test()]
		public void TestEnumBraceStyle ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"enum Test {
	A
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.EnumBraceStyle =  BraceStyle.NextLineShifted;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"enum Test
	{
	A
	}", data.Document.Text);
		}
		
		[Test()]
		public void TestMethodBraceStlye ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test MyMethod() {}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.MethodBraceStyle = BraceStyle.NextLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test MyMethod()
	{
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestConstructorBraceStyle ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test() {}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.ConstructorBraceStyle = BraceStyle.NextLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test()
	{
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestDestructorBraceStyle ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	~Test() {}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.DestructorBraceStyle = BraceStyle.NextLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	~Test()
	{
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestPropertyBraceStyle ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test A {
		get;
		set;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PropertyBraceStyle = BraceStyle.NextLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test A
	{
		get;
		set;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestPropertyGetBraceStyle ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test A {
		get {
			return null;
		}
		set;
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PropertyGetBraceStyle = BraceStyle.NextLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test A {
		get
		{
			return null;
		}
		set;
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestAllowPropertyGetBlockInline ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test A {
		get { return null; }
		set { ; }
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PropertyBraceStyle = BraceStyle.DoNotChange;
			policy.AllowPropertyGetBlockInline = true;
			policy.AllowPropertySetBlockInline = false;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test A {
		get { return null; }
		set {
			;
		}
	}
}", data.Document.Text);
			
			policy.AllowPropertyGetBlockInline = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test
{
	Test A {
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
		public void TestAllowPropertySetBlockInline ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test A {
		get { return null; }
		set { ; }
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PropertyBraceStyle = BraceStyle.DoNotChange;
			policy.AllowPropertyGetBlockInline = false;
			policy.AllowPropertySetBlockInline = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test A {
		get {
			return null;
		}
		set { ; }
	}
}", data.Document.Text);
			
			policy.AllowPropertySetBlockInline = false;
			compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Assert.AreEqual (@"class Test
{
	Test A {
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
		public void TestPropertySetBraceStyle ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	Test A {
		get;
		set {
			;
		}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PropertySetBraceStyle = BraceStyle.NextLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	Test A {
		get;
		set
		{
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestEventBraceStyle ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	public event EventHandler Handler {
		add {
}
		remove {
}
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.EventBraceStyle = BraceStyle.NextLine;
			policy.EventAddBraceStyle = BraceStyle.NextLine;
			policy.EventRemoveBraceStyle = BraceStyle.NextLine;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
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
}", data.Document.Text);
		}
		
		[Test()]
		public void TestAllowEventAddBlockInline ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	public event EventHandler Handler {
		add { ; }
		remove { ; }
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AllowEventAddBlockInline = true;
			policy.AllowEventRemoveBlockInline = false;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	public event EventHandler Handler {
		add { ; }
		remove {
			;
		}
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestAllowEventRemoveBlockInline ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	public event EventHandler Handler {
		add { ; }
		remove { ; }
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.AllowEventAddBlockInline = false;
			policy.AllowEventRemoveBlockInline = true;
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			
			Console.WriteLine (data.Document.Text);
			Assert.AreEqual (@"class Test
{
	public event EventHandler Handler {
		add {
			;
		}
		remove { ; }
	}
}", data.Document.Text);
		}
		
		
		
	}
}*/
