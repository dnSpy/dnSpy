// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Text;
using ICSharpCode.AvalonEdit.Document;
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
