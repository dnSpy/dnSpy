// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Text;
using ICSharpCode.AvalonEdit.Document;
#if NREFACTORY
using ICSharpCode.NRefactory;
#endif
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Editing
{
	[TestFixture]
	public class ChangeDocumentTests
	{
		[Test]
		public void ClearCaretAndSelectionOnDocumentChange()
		{
			TextArea textArea = new TextArea();
			textArea.Document = new TextDocument("1\n2\n3\n4th line");
			textArea.Caret.Offset = 6;
			textArea.Selection = Selection.Create(textArea, 3, 6);
			textArea.Document = new TextDocument("1\n2nd");
			Assert.AreEqual(0, textArea.Caret.Offset);
			Assert.AreEqual(new TextLocation(1, 1), textArea.Caret.Location);
			Assert.IsTrue(textArea.Selection.IsEmpty);
		}
		
		[Test]
		public void SetDocumentToNull()
		{
			TextArea textArea = new TextArea();
			textArea.Document = new TextDocument("1\n2\n3\n4th line");
			textArea.Caret.Offset = 6;
			textArea.Selection = Selection.Create(textArea, 3, 6);
			textArea.Document = null;
			Assert.AreEqual(0, textArea.Caret.Offset);
			Assert.AreEqual(new TextLocation(1, 1), textArea.Caret.Location);
			Assert.IsTrue(textArea.Selection.IsEmpty);
		}
		
		[Test]
		public void CheckEventOrderOnDocumentChange()
		{
			TextArea textArea = new TextArea();
			TextDocument newDocument = new TextDocument();
			StringBuilder b = new StringBuilder();
			textArea.TextView.DocumentChanged += delegate {
				b.Append("TextView.DocumentChanged;");
				Assert.AreSame(newDocument, textArea.TextView.Document);
				Assert.AreSame(newDocument, textArea.Document);
			};
			textArea.DocumentChanged += delegate {
				b.Append("TextArea.DocumentChanged;");
				Assert.AreSame(newDocument, textArea.TextView.Document);
				Assert.AreSame(newDocument, textArea.Document);
			};
			textArea.Document = newDocument;
			Assert.AreEqual("TextView.DocumentChanged;TextArea.DocumentChanged;", b.ToString());
		}
	}
}
