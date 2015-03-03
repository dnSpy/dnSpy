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
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;
#if NREFACTORY
using ICSharpCode.NRefactory.Editor;
#endif
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Utils
{
	[TestFixture]
	public class CaretNavigationTests
	{
		int GetNextCaretStop(string text, int offset, CaretPositioningMode mode)
		{
			return TextUtilities.GetNextCaretPosition(new StringTextSource(text), offset, LogicalDirection.Forward, mode);
		}
		
		int GetPrevCaretStop(string text, int offset, CaretPositioningMode mode)
		{
			return TextUtilities.GetNextCaretPosition(new StringTextSource(text), offset, LogicalDirection.Backward, mode);
		}
		
		[Test]
		public void CaretStopInEmptyString()
		{
			Assert.AreEqual(0, GetNextCaretStop("", -1, CaretPositioningMode.Normal));
			Assert.AreEqual(-1, GetNextCaretStop("", 0, CaretPositioningMode.Normal));
			Assert.AreEqual(-1, GetPrevCaretStop("", 0, CaretPositioningMode.Normal));
			Assert.AreEqual(0, GetPrevCaretStop("", 1, CaretPositioningMode.Normal));
			
			Assert.AreEqual(-1, GetNextCaretStop("", -1, CaretPositioningMode.WordStart));
			Assert.AreEqual(-1, GetNextCaretStop("", -1, CaretPositioningMode.WordBorder));
			Assert.AreEqual(-1, GetPrevCaretStop("", 1, CaretPositioningMode.WordStart));
			Assert.AreEqual(-1, GetPrevCaretStop("", 1, CaretPositioningMode.WordBorder));
		}
		
		[Test]
		public void StartOfDocumentWithWordStart()
		{
			Assert.AreEqual(0, GetNextCaretStop("word", -1, CaretPositioningMode.Normal));
			Assert.AreEqual(0, GetNextCaretStop("word", -1, CaretPositioningMode.WordStart));
			Assert.AreEqual(0, GetNextCaretStop("word", -1, CaretPositioningMode.WordBorder));
			
			Assert.AreEqual(0, GetPrevCaretStop("word", 1, CaretPositioningMode.Normal));
			Assert.AreEqual(0, GetPrevCaretStop("word", 1, CaretPositioningMode.WordStart));
			Assert.AreEqual(0, GetPrevCaretStop("word", 1, CaretPositioningMode.WordBorder));
		}
		
		[Test]
		public void StartOfDocumentNoWordStart()
		{
			Assert.AreEqual(0, GetNextCaretStop(" word", -1, CaretPositioningMode.Normal));
			Assert.AreEqual(1, GetNextCaretStop(" word", -1, CaretPositioningMode.WordStart));
			Assert.AreEqual(1, GetNextCaretStop(" word", -1, CaretPositioningMode.WordBorder));
			
			Assert.AreEqual(0, GetPrevCaretStop(" word", 1, CaretPositioningMode.Normal));
			Assert.AreEqual(-1, GetPrevCaretStop(" word", 1, CaretPositioningMode.WordStart));
			Assert.AreEqual(-1, GetPrevCaretStop(" word", 1, CaretPositioningMode.WordBorder));
		}
		
		[Test]
		public void EndOfDocumentWordBorder()
		{
			Assert.AreEqual(4, GetNextCaretStop("word", 3, CaretPositioningMode.Normal));
			Assert.AreEqual(-1, GetNextCaretStop("word", 3, CaretPositioningMode.WordStart));
			Assert.AreEqual(4, GetNextCaretStop("word", 3, CaretPositioningMode.WordBorder));
			
			Assert.AreEqual(4, GetPrevCaretStop("word", 5, CaretPositioningMode.Normal));
			Assert.AreEqual(0, GetPrevCaretStop("word", 5, CaretPositioningMode.WordStart));
			Assert.AreEqual(4, GetPrevCaretStop("word", 5, CaretPositioningMode.WordBorder));
		}
		
		[Test]
		public void EndOfDocumentNoWordBorder()
		{
			Assert.AreEqual(4, GetNextCaretStop("txt ", 3, CaretPositioningMode.Normal));
			Assert.AreEqual(-1, GetNextCaretStop("txt ", 3, CaretPositioningMode.WordStart));
			Assert.AreEqual(-1, GetNextCaretStop("txt ", 3, CaretPositioningMode.WordBorder));
			
			Assert.AreEqual(4, GetPrevCaretStop("txt ", 5, CaretPositioningMode.Normal));
			Assert.AreEqual(0, GetPrevCaretStop("txt ", 5, CaretPositioningMode.WordStart));
			Assert.AreEqual(3, GetPrevCaretStop("txt ", 5, CaretPositioningMode.WordBorder));
		}
		
		[Test]
		public void SingleCharacterOutsideBMP()
		{
			string c = "\U0001D49E";
			Assert.AreEqual(2, GetNextCaretStop(c, 0, CaretPositioningMode.Normal));
			Assert.AreEqual(0, GetPrevCaretStop(c, 2, CaretPositioningMode.Normal));
		}
		
		[Test]
		public void DetectWordBordersOutsideBMP()
		{
			string c = " a\U0001D49Eb ";
			Assert.AreEqual(1, GetNextCaretStop(c, 0, CaretPositioningMode.WordBorder));
			Assert.AreEqual(5, GetNextCaretStop(c, 1, CaretPositioningMode.WordBorder));
			
			Assert.AreEqual(5, GetPrevCaretStop(c, 6, CaretPositioningMode.WordBorder));
			Assert.AreEqual(1, GetPrevCaretStop(c, 5, CaretPositioningMode.WordBorder));
		}
		
		[Test]
		public void DetectWordBordersOutsideBMP2()
		{
			string c = " \U0001D49E\U0001D4AA ";
			Assert.AreEqual(1, GetNextCaretStop(c, 0, CaretPositioningMode.WordBorder));
			Assert.AreEqual(5, GetNextCaretStop(c, 1, CaretPositioningMode.WordBorder));
			
			Assert.AreEqual(5, GetPrevCaretStop(c, 6, CaretPositioningMode.WordBorder));
			Assert.AreEqual(1, GetPrevCaretStop(c, 5, CaretPositioningMode.WordBorder));
		}
		
		[Test]
		public void CombiningMark()
		{
			string str = " x͆ ";
			Assert.AreEqual(3, GetNextCaretStop(str, 1, CaretPositioningMode.Normal));
			Assert.AreEqual(1, GetPrevCaretStop(str, 3, CaretPositioningMode.Normal));
		}
		
		[Test]
		public void StackedCombiningMark()
		{
			string str = " x͆͆͆͆ ";
			Assert.AreEqual(6, GetNextCaretStop(str, 1, CaretPositioningMode.Normal));
			Assert.AreEqual(1, GetPrevCaretStop(str, 6, CaretPositioningMode.Normal));
		}
		
		[Test]
		public void SingleClosingBraceAtLineEnd()
		{
			string str = "\t\t}";
			Assert.AreEqual(2, GetNextCaretStop(str, 1, CaretPositioningMode.WordStart));
			Assert.AreEqual(-1, GetPrevCaretStop(str, 1, CaretPositioningMode.WordStart));
		}
	}
}
