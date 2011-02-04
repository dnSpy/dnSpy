// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
