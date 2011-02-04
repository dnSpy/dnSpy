// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.AvalonEdit.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Creates/Deletes lines when text is inserted/removed.
	/// </summary>
	sealed class LineManager
	{
		#region Constructor
		readonly TextDocument document;
		readonly DocumentLineTree documentLineTree;
		
		/// <summary>
		/// A copy of the line trackers. We need a copy so that line trackers may remove themselves
		/// while being notified (used e.g. by WeakLineTracker)
		/// </summary>
		ILineTracker[] lineTrackers;
		
		internal void UpdateListOfLineTrackers()
		{
			this.lineTrackers = document.LineTrackers.ToArray();
		}
		
		public LineManager(DocumentLineTree documentLineTree, TextDocument document)
		{
			this.document = document;
			this.documentLineTree = documentLineTree;
			UpdateListOfLineTrackers();
			
			Rebuild();
		}
		#endregion
		
		#region Change events
		/*
		HashSet<DocumentLine> deletedLines = new HashSet<DocumentLine>();
		readonly HashSet<DocumentLine> changedLines = new HashSet<DocumentLine>();
		HashSet<DocumentLine> deletedOrChangedLines = new HashSet<DocumentLine>();
		
		/// <summary>
		/// Gets the list of lines deleted since the last RetrieveChangedLines() call.
		/// The returned list is unsorted.
		/// </summary>
		public ICollection<DocumentLine> RetrieveDeletedLines()
		{
			var r = deletedLines;
			deletedLines = new HashSet<DocumentLine>();
			return r;
		}
		
		/// <summary>
		/// Gets the list of lines changed since the last RetrieveChangedLines() call.
		/// The returned list is sorted by line number and does not contain deleted lines.
		/// </summary>
		public List<DocumentLine> RetrieveChangedLines()
		{
			var list = (from line in changedLines
			            where !line.IsDeleted
			            let number = line.LineNumber
			            orderby number
			            select line).ToList();
			changedLines.Clear();
			return list;
		}
		
		/// <summary>
		/// Gets the list of lines changed since the last RetrieveDeletedOrChangedLines() call.
		/// The returned list is not sorted.
		/// </summary>
		public ICollection<DocumentLine> RetrieveDeletedOrChangedLines()
		{
			var r = deletedOrChangedLines;
			deletedOrChangedLines = new HashSet<DocumentLine>();
			return r;
		}
		 */
		#endregion
		
		#region Rebuild
		public void Rebuild()
		{
			// keep the first document line
			DocumentLine ls = documentLineTree.GetByNumber(1);
			SimpleSegment ds = NewLineFinder.NextNewLine(document, 0);
			List<DocumentLine> lines = new List<DocumentLine>();
			int lastDelimiterEnd = 0;
			while (ds != SimpleSegment.Invalid) {
				ls.TotalLength = ds.Offset + ds.Length - lastDelimiterEnd;
				ls.DelimiterLength = ds.Length;
				lastDelimiterEnd = ds.Offset + ds.Length;
				lines.Add(ls);
				
				ls = new DocumentLine(document);
				ds = NewLineFinder.NextNewLine(document, lastDelimiterEnd);
			}
			ls.ResetLine();
			ls.TotalLength = document.TextLength - lastDelimiterEnd;
			lines.Add(ls);
			documentLineTree.RebuildTree(lines);
			foreach (ILineTracker lineTracker in lineTrackers)
				lineTracker.RebuildDocument();
		}
		#endregion
		
		#region Remove
		public void Remove(int offset, int length)
		{
			Debug.Assert(length >= 0);
			if (length == 0) return;
			DocumentLine startLine = documentLineTree.GetByOffset(offset);
			int startLineOffset = startLine.Offset;
			
			Debug.Assert(offset < startLineOffset + startLine.TotalLength);
			if (offset > startLineOffset + startLine.Length) {
				Debug.Assert(startLine.DelimiterLength == 2);
				// we are deleting starting in the middle of a delimiter
				
				// remove last delimiter part
				SetLineLength(startLine, startLine.TotalLength - 1);
				// remove remaining text
				Remove(offset, length - 1);
				return;
			}
			
			if (offset + length < startLineOffset + startLine.TotalLength) {
				// just removing a part of this line
				//startLine.RemovedLinePart(ref deferredEventList, offset - startLineOffset, length);
				SetLineLength(startLine, startLine.TotalLength - length);
				return;
			}
			// merge startLine with another line because startLine's delimiter was deleted
			// possibly remove lines in between if multiple delimiters were deleted
			int charactersRemovedInStartLine = startLineOffset + startLine.TotalLength - offset;
			Debug.Assert(charactersRemovedInStartLine > 0);
			//startLine.RemovedLinePart(ref deferredEventList, offset - startLineOffset, charactersRemovedInStartLine);
			
			
			DocumentLine endLine = documentLineTree.GetByOffset(offset + length);
			if (endLine == startLine) {
				// special case: we are removing a part of the last line up to the
				// end of the document
				SetLineLength(startLine, startLine.TotalLength - length);
				return;
			}
			int endLineOffset = endLine.Offset;
			int charactersLeftInEndLine = endLineOffset + endLine.TotalLength - (offset + length);
			//endLine.RemovedLinePart(ref deferredEventList, 0, endLine.TotalLength - charactersLeftInEndLine);
			//startLine.MergedWith(endLine, offset - startLineOffset);
			
			// remove all lines between startLine (excl.) and endLine (incl.)
			DocumentLine tmp = startLine.NextLine;
			DocumentLine lineToRemove;
			do {
				lineToRemove = tmp;
				tmp = tmp.NextLine;
				RemoveLine(lineToRemove);
			} while (lineToRemove != endLine);
			
			SetLineLength(startLine, startLine.TotalLength - charactersRemovedInStartLine + charactersLeftInEndLine);
		}

		void RemoveLine(DocumentLine lineToRemove)
		{
			foreach (ILineTracker lt in lineTrackers)
				lt.BeforeRemoveLine(lineToRemove);
			documentLineTree.RemoveLine(lineToRemove);
//			foreach (ILineTracker lt in lineTracker)
//				lt.AfterRemoveLine(lineToRemove);
//			deletedLines.Add(lineToRemove);
//			deletedOrChangedLines.Add(lineToRemove);
		}

		#endregion
		
		#region Insert
		public void Insert(int offset, string text)
		{
			DocumentLine line = documentLineTree.GetByOffset(offset);
			int lineOffset = line.Offset;
			
			Debug.Assert(offset <= lineOffset + line.TotalLength);
			if (offset > lineOffset + line.Length) {
				Debug.Assert(line.DelimiterLength == 2);
				// we are inserting in the middle of a delimiter
				
				// shorten line
				SetLineLength(line, line.TotalLength - 1);
				// add new line
				line = InsertLineAfter(line, 1);
				line = SetLineLength(line, 1);
			}
			
			SimpleSegment ds = NewLineFinder.NextNewLine(text, 0);
			if (ds == SimpleSegment.Invalid) {
				// no newline is being inserted, all text is inserted in a single line
				//line.InsertedLinePart(offset - line.Offset, text.Length);
				SetLineLength(line, line.TotalLength + text.Length);
				return;
			}
			//DocumentLine firstLine = line;
			//firstLine.InsertedLinePart(offset - firstLine.Offset, ds.Offset);
			int lastDelimiterEnd = 0;
			while (ds != SimpleSegment.Invalid) {
				// split line segment at line delimiter
				int lineBreakOffset = offset + ds.Offset + ds.Length;
				lineOffset = line.Offset;
				int lengthAfterInsertionPos = lineOffset + line.TotalLength - (offset + lastDelimiterEnd);
				line = SetLineLength(line, lineBreakOffset - lineOffset);
				DocumentLine newLine = InsertLineAfter(line, lengthAfterInsertionPos);
				newLine = SetLineLength(newLine, lengthAfterInsertionPos);
				
				line = newLine;
				lastDelimiterEnd = ds.Offset + ds.Length;
				
				ds = NewLineFinder.NextNewLine(text, lastDelimiterEnd);
			}
			//firstLine.SplitTo(line);
			// insert rest after last delimiter
			if (lastDelimiterEnd != text.Length) {
				//line.InsertedLinePart(0, text.Length - lastDelimiterEnd);
				SetLineLength(line, line.TotalLength + text.Length - lastDelimiterEnd);
			}
		}
		
		DocumentLine InsertLineAfter(DocumentLine line, int length)
		{
			DocumentLine newLine = documentLineTree.InsertLineAfter(line, length);
			foreach (ILineTracker lt in lineTrackers)
				lt.LineInserted(line, newLine);
			return newLine;
		}
		#endregion
		
		#region SetLineLength
		/// <summary>
		/// Sets the total line length and checks the delimiter.
		/// This method can cause line to be deleted when it contains a single '\n' character
		/// and the previous line ends with '\r'.
		/// </summary>
		/// <returns>Usually returns <paramref name="line"/>, but if line was deleted due to
		/// the "\r\n" merge, returns the previous line.</returns>
		DocumentLine SetLineLength(DocumentLine line, int newTotalLength)
		{
//			changedLines.Add(line);
//			deletedOrChangedLines.Add(line);
			int delta = newTotalLength - line.TotalLength;
			if (delta != 0) {
				foreach (ILineTracker lt in lineTrackers)
					lt.SetLineLength(line, newTotalLength);
				line.TotalLength = newTotalLength;
				DocumentLineTree.UpdateAfterChildrenChange(line);
			}
			// determine new DelimiterLength
			if (newTotalLength == 0) {
				line.DelimiterLength = 0;
			} else {
				int lineOffset = line.Offset;
				char lastChar = document.GetCharAt(lineOffset + newTotalLength - 1);
				if (lastChar == '\r') {
					line.DelimiterLength = 1;
				} else if (lastChar == '\n') {
					if (newTotalLength >= 2 && document.GetCharAt(lineOffset + newTotalLength - 2) == '\r') {
						line.DelimiterLength = 2;
					} else if (newTotalLength == 1 && lineOffset > 0 && document.GetCharAt(lineOffset - 1) == '\r') {
						// we need to join this line with the previous line
						DocumentLine previousLine = line.PreviousLine;
						RemoveLine(line);
						return SetLineLength(previousLine, previousLine.TotalLength + 1);
					} else {
						line.DelimiterLength = 1;
					}
				} else {
					line.DelimiterLength = 0;
				}
			}
			return line;
		}
		#endregion
	}
}
