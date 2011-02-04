// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Allows for low-level line tracking.
	/// </summary>
	/// <remarks>
	/// The methods on this interface are called by the TextDocument's LineManager immediately after the document
	/// has changed, *while* the DocumentLineTree is updating.
	/// Thus, the DocumentLineTree may be in an invalid state when these methods are called.
	/// This interface should only be used to update per-line data structures like the HeightTree.
	/// Line trackers must not cause any events to be raised during an update to prevent other code from seeing
	/// the invalid state.
	/// Line trackers may be called while the TextDocument has taken a lock.
	/// You must be careful not to dead-lock inside ILineTracker callbacks.
	/// </remarks>
	public interface ILineTracker
	{
		/// <summary>
		/// Is called immediately before a document line is removed.
		/// </summary>
		void BeforeRemoveLine(DocumentLine line);
		
//		/// <summary>
//		/// Is called immediately after a document line is removed.
//		/// </summary>
//		void AfterRemoveLine(DocumentLine line);
		
		/// <summary>
		/// Is called immediately before a document line changes length.
		/// This method will be called whenever the line is changed, even when the length stays as it is.
		/// The method might be called multiple times for a single line because
		/// a replacement is internally handled as removal followed by insertion.
		/// </summary>
		void SetLineLength(DocumentLine line, int newTotalLength);
		
		/// <summary>
		/// Is called immediately after a line was inserted.
		/// </summary>
		/// <param name="newLine">The new line</param>
		/// <param name="insertionPos">The existing line before the new line</param>
		void LineInserted(DocumentLine insertionPos, DocumentLine newLine);
		
		/// <summary>
		/// Indicates that there were changes to the document that the line tracker was not notified of.
		/// The document is in a consistent state (but the line trackers aren't), and line trackers should
		/// throw away their data and rebuild the document.
		/// </summary>
		void RebuildDocument();
	}
}
