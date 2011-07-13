// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ICSharpCode.Editor
{
	/// <summary>
	/// Interface for text editors.
	/// </summary>
	public interface ITextEditor : IServiceProvider
	{
		/// <summary>
		/// Gets the document that is being edited.
		/// </summary>
		IDocument Document { get; }
		
		/// <summary>
		/// Gets an object that represents the caret inside this text editor.
		/// </summary>
		ITextEditorCaret Caret { get; }
		
		/// <summary>
		/// Sets the caret to the specified line/column and brings the caret into view.
		/// </summary>
		void JumpTo(int line, int column);
		
		/// <summary>
		/// Gets the start offset of the selection.
		/// </summary>
		int SelectionStart { get; }
		
		/// <summary>
		/// Gets the length of the selection.
		/// </summary>
		int SelectionLength { get; }
		
		/// <summary>
		/// Gets/Sets the selected text.
		/// </summary>
		string SelectedText { get; set; }
		
		/// <summary>
		/// Sets the selection.
		/// </summary>
		/// <param name="selectionStart">Start offset of the selection</param>
		/// <param name="selectionLength">Length of the selection</param>
		void Select(int selectionStart, int selectionLength);
		
		/// <summary>
		/// Shows the specified linked elements, and allows the user to edit them.
		/// </summary>
		/// <returns>
		/// Returns true when the user has finished editing the elements and pressed Return;
		/// or false when editing is aborted for any reason.
		/// </returns>
		/// <remarks>
		/// The user can also edit other parts of the document (or other documents) while in link mode.
		/// In case of success (true return value), this method will update the offsets of the linked elements
		/// to reflect the changes done by the user.
		/// If the text editor does not support link mode, it will immediately return false.
		/// </remarks>
//		Task<bool> ShowLinkedElements(IEnumerable<LinkedElement> linkedElements);
	}
	
	/// <summary>
	/// Represents the caret in a text editor.
	/// </summary>
	public interface ITextEditorCaret
	{
		/// <summary>
		/// Gets/Sets the caret offset;
		/// </summary>
		int Offset { get; set; }
		
		/// <summary>
		/// Gets/Sets the caret line number.
		/// Line numbers are counted starting from 1.
		/// </summary>
		int Line { get; set; }
		
		/// <summary>
		/// Gets/Sets the caret column number.
		/// Column numbers are counted starting from 1.
		/// </summary>
		int Column { get; set; }
		
		/// <summary>
		/// Gets/sets the caret location.
		/// </summary>
		TextLocation Location { get; set; }
		
		/// <summary>
		/// Is raised whenever the location of the caret has changed.
		/// </summary>
		event EventHandler LocationChanged;
	}
}
