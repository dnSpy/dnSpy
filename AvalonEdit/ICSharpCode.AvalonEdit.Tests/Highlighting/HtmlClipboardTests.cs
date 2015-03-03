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
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	[TestFixture]
	public class HtmlClipboardTests
	{
		TextDocument document;
		DocumentHighlighter highlighter;
		
		public HtmlClipboardTests()
		{
			document = new TextDocument("using System.Text;\n\tstring text = SomeMethod();");
			highlighter = new DocumentHighlighter(document, HighlightingManager.Instance.GetDefinition("C#"));
		}
		
		[Test]
		public void FullDocumentTest()
		{
			var segment = new TextSegment { StartOffset = 0, Length = document.TextLength };
			string html = HtmlClipboard.CreateHtmlFragment(document, highlighter, segment, new HtmlOptions());
			Assert.AreEqual("<span style=\"color: #008000; font-weight: bold; \">using</span> System.Text;<br>" + Environment.NewLine +
			                "&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"color: #ff0000; \">string</span> " +
			                "text = <span style=\"color: #191970; font-weight: bold; \">SomeMethod</span>();", html);
		}
		
		[Test]
		public void PartOfHighlightedWordTest()
		{
			var segment = new TextSegment { StartOffset = 1, Length = 3 };
			string html = HtmlClipboard.CreateHtmlFragment(document, highlighter, segment, new HtmlOptions());
			Assert.AreEqual("<span style=\"color: #008000; font-weight: bold; \">sin</span>", html);
		}
	}
}
