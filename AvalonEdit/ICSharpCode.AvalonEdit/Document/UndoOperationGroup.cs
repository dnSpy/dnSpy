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
using System.Diagnostics;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// This class stacks the last x operations from the undostack and makes
	/// one undo/redo operation from it.
	/// </summary>
	sealed class UndoOperationGroup : IUndoableOperationWithContext
	{
		IUndoableOperation[] undolist;
		
		public UndoOperationGroup(Deque<IUndoableOperation> stack, int numops)
		{
			if (stack == null)  {
				throw new ArgumentNullException("stack");
			}
			
			Debug.Assert(numops > 0 , "UndoOperationGroup : numops should be > 0");
			Debug.Assert(numops <= stack.Count);
			
			undolist = new IUndoableOperation[numops];
			for (int i = 0; i < numops; ++i) {
				undolist[i] = stack.PopBack();
			}
		}
		
		public void Undo()
		{
			for (int i = 0; i < undolist.Length; ++i) {
				undolist[i].Undo();
			}
		}
		
		public void Undo(UndoStack stack)
		{
			for (int i = 0; i < undolist.Length; ++i) {
				stack.RunUndo(undolist[i]);
			}
		}
		
		public void Redo()
		{
			for (int i = undolist.Length - 1; i >= 0; --i) {
				undolist[i].Redo();
			}
		}
		
		public void Redo(UndoStack stack)
		{
			for (int i = undolist.Length - 1; i >= 0; --i) {
				stack.RunRedo(undolist[i]);
			}
		}
	}
}
