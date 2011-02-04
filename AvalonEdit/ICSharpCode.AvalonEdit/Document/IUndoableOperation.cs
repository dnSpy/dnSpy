// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// This Interface describes a the basic Undo/Redo operation
	/// all Undo Operations must implement this interface.
	/// </summary>
	public interface IUndoableOperation
	{
		/// <summary>
		/// Undo the last operation
		/// </summary>
		void Undo();
		
		/// <summary>
		/// Redo the last operation
		/// </summary>
		void Redo();
	}
	
	interface IUndoableOperationWithContext : IUndoableOperation
	{
		void Undo(UndoStack stack);
		void Redo(UndoStack stack);
	}
}
