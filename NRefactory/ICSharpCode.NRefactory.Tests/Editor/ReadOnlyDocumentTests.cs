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
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Editor
{
	[TestFixture]
	public class ReadOnlyDocumentTests
	{
		[Test]
		public void EmptyReadOnlyDocument()
		{
			IDocument document = new ReadOnlyDocument(string.Empty);
			Assert.AreEqual(string.Empty, document.Text);
			Assert.AreEqual(0, document.TextLength);
			Assert.AreEqual(1, document.LineCount);
			Assert.AreEqual(0, document.GetOffset(1, 1));
			Assert.AreEqual(new TextLocation(1, 1), document.GetLocation(0));
			
			Assert.AreEqual(0, document.GetLineByNumber(1).Offset);
			Assert.AreEqual(0, document.GetLineByNumber(1).EndOffset);
			Assert.AreEqual(0, document.GetLineByNumber(1).Length);
			Assert.AreEqual(0, document.GetLineByNumber(1).TotalLength);
			Assert.AreEqual(0, document.GetLineByNumber(1).DelimiterLength);
			Assert.AreEqual(1, document.GetLineByNumber(1).LineNumber);
		}
		
		[Test]
		public void SimpleDocument()
		{
			string text = "Hello\nWorld!\r\n";
			IDocument document = new ReadOnlyDocument(text);
			Assert.AreEqual(text, document.Text);
			Assert.AreEqual(3, document.LineCount);
			
			Assert.AreEqual(0, document.GetLineByNumber(1).Offset);
			Assert.AreEqual(5, document.GetLineByNumber(1).EndOffset);
			Assert.AreEqual(5, document.GetLineByNumber(1).Length);
			Assert.AreEqual(6, document.GetLineByNumber(1).TotalLength);
			Assert.AreEqual(1, document.GetLineByNumber(1).DelimiterLength);
			Assert.AreEqual(1, document.GetLineByNumber(1).LineNumber);
			
			Assert.AreEqual(6, document.GetLineByNumber(2).Offset);
			Assert.AreEqual(12, document.GetLineByNumber(2).EndOffset);
			Assert.AreEqual(6, document.GetLineByNumber(2).Length);
			Assert.AreEqual(8, document.GetLineByNumber(2).TotalLength);
			Assert.AreEqual(2, document.GetLineByNumber(2).DelimiterLength);
			Assert.AreEqual(2, document.GetLineByNumber(2).LineNumber);

			Assert.AreEqual(14, document.GetLineByNumber(3).Offset);
			Assert.AreEqual(14, document.GetLineByNumber(3).EndOffset);
			Assert.AreEqual(0, document.GetLineByNumber(3).Length);
			Assert.AreEqual(0, document.GetLineByNumber(3).TotalLength);
			Assert.AreEqual(0, document.GetLineByNumber(3).DelimiterLength);
			Assert.AreEqual(3, document.GetLineByNumber(3).LineNumber);
		}
	}
}
