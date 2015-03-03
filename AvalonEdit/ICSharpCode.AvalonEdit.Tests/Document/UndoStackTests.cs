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
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Document
{
	public class UndoStackTests
	{
		[Test]
		public void ContinueUndoGroup()
		{
			var doc = new TextDocument();
			doc.Insert(0, "a");
			doc.UndoStack.StartContinuedUndoGroup();
			doc.Insert(1, "b");
			doc.UndoStack.EndUndoGroup();
			doc.UndoStack.Undo();
			Assert.AreEqual("", doc.Text);
		}
		
		[Test]
		public void ContinueEmptyUndoGroup()
		{
			var doc = new TextDocument();
			doc.Insert(0, "a");
			doc.UndoStack.StartUndoGroup();
			doc.UndoStack.EndUndoGroup();
			doc.UndoStack.StartContinuedUndoGroup();
			doc.Insert(1, "b");
			doc.UndoStack.EndUndoGroup();
			doc.UndoStack.Undo();
			Assert.AreEqual("a", doc.Text);
		}
		
		[Test]
		public void ContinueEmptyUndoGroup_WithOptionalEntries()
		{
			var doc = new TextDocument();
			doc.Insert(0, "a");
			doc.UndoStack.StartUndoGroup();
			doc.UndoStack.PushOptional(new StubUndoableAction());
			doc.UndoStack.EndUndoGroup();
			doc.UndoStack.StartContinuedUndoGroup();
			doc.Insert(1, "b");
			doc.UndoStack.EndUndoGroup();
			doc.UndoStack.Undo();
			Assert.AreEqual("a", doc.Text);
		}
		
		[Test]
		public void EmptyContinuationGroup()
		{
			var doc = new TextDocument();
			doc.Insert(0, "a");
			doc.UndoStack.StartContinuedUndoGroup();
			doc.UndoStack.EndUndoGroup();
			doc.UndoStack.StartContinuedUndoGroup();
			doc.Insert(1, "b");
			doc.UndoStack.EndUndoGroup();
			doc.UndoStack.Undo();
			Assert.AreEqual("", doc.Text);
		}
		
		class StubUndoableAction : IUndoableOperation
		{
			public void Undo()
			{
			}
			
			public void Redo()
			{
			}
		}
	}
}
