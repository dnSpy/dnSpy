// 
// TestFormattingBugs.cs
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
using System.Collections.Generic;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharpBinding.FormattingTests
{
	[TestFixture()]
	public class TestFormattingBugs : UnitTests.TestBase
	{
		/// <summary>
		/// Bug 325187 - Bug in smart indent
		/// </summary>
		[Test()]
		public void TestBug325187 ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.PlaceElseOnNewLine = true;
			
			TestStatementFormatting (policy,
@"foreach (int i in myints)
if (i == 6)
Console.WriteLine (""Yeah"");
else
Console.WriteLine (""Bad indent"");",
@"foreach (int i in myints)
	if (i == 6)
		Console.WriteLine (""Yeah"");
	else
		Console.WriteLine (""Bad indent"");");
		}
		
		/// <summary>
		/// Bug 415469 - return ternary in a switch is not tabbed properly
		/// </summary>
		[Test()]
		[Ignore("currently failing because of 'string' has the wrong offset - mcs bug")]
		public void TestBug415469 () 
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			TestStatementFormatting (policy,
@"switch (condition) {
case CONDITION1:
return foo != null ? foo.Bar : null;
case CONDITION2:
string goo = foo != null ? foo.Bar : null;
return ""Should be indented like this"";
}", @"switch (condition) {
case CONDITION1:
	return foo != null ? foo.Bar : null;
case CONDITION2:
	string goo = foo != null ? foo.Bar : null;
	return ""Should be indented like this"";
}");
		}
		
		/// <summary>
		/// Bug 540043 - Format option for alignment of using-statements
		/// </summary>
		[Test()]
		public void TestBug540043 ()
		{
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			
			TestStatementFormatting (policy,
@"using (IDisposable a = null)
   using (IDisposable b = null) {
			int c;
   }
", @"using (IDisposable a = null)
using (IDisposable b = null) {
	int c;
}");
		}
		
		


		
		static void TestStatementFormatting (CSharpFormattingPolicy policy, string input, string expectedOutput)
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text =
@"class Test
{
	MyType TestMethod ()
	{
		" + input + @"
	}
}";
			
			Console.WriteLine (data.Document.Text);
			
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			DomSpacingVisitor domSpacingVisitor = new DomSpacingVisitor (policy, data);
			domSpacingVisitor.AutoAcceptChanges = false;
			compilationUnit.AcceptVisitor (domSpacingVisitor, null);
			
			DomIndentationVisitor domIndentationVisitor = new DomIndentationVisitor (policy, data);
			domIndentationVisitor.AutoAcceptChanges = false;
			compilationUnit.AcceptVisitor (domIndentationVisitor, null);
			
			List<Change> changes = new List<Change> ();
			changes.AddRange (domSpacingVisitor.Changes);
			changes.AddRange (domIndentationVisitor.Changes);
			RefactoringService.AcceptChanges (null, null, changes);
			
			for (int i = 0; i < data.Document.LineCount; i++) {
				LineSegment line = data.Document.GetLine (i);
				if (line.EditableLength < 2)
					continue;
				data.Remove (line.Offset, 2);
			}
			string text = data.Document.GetTextBetween (data.Document.GetLine (4).Offset,
			                                            data.Document.GetLine (data.Document.LineCount - 2).Offset).Trim ();
			Console.WriteLine (text);
			Assert.AreEqual (expectedOutput, text);
		}

		
	}
}
*/
