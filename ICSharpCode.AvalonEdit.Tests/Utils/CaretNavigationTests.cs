// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;
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
	}
}
