// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Rectangular selection.
	/// </summary>
	public sealed class RectangleSelection : Selection
	{
		TextDocument document;
		
		/// <summary>
		/// Gets the start position of the selection.
		/// </summary>
		public int StartOffset { get; private set; }
		
		/// <summary>
		/// Gets the end position of the selection.
		/// </summary>
		public int EndOffset { get; private set; }
		
		/// <summary>
		/// Creates a new rectangular selection.
		/// </summary>
		public RectangleSelection(TextDocument document, int start, int end)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			this.document = document;
			this.StartOffset = start;
			this.EndOffset = end;
		}
		
		/// <inheritdoc/>
		public override bool IsEmpty {
			get {
				TextLocation start = document.GetLocation(StartOffset);
				TextLocation end = document.GetLocation(EndOffset);
				return start.Column == end.Column;
			}
		}
		
		/// <inheritdoc/>
		public override bool Contains(int offset)
		{
			if (Math.Min(StartOffset, EndOffset) <= offset && offset <= Math.Max(StartOffset, EndOffset)) {
				foreach (ISegment s in this.Segments) {
					if (s.Contains(offset))
						return true;
				}
			}
			return false;
		}
		
		/// <inheritdoc/>
		public override string GetText(TextDocument document)
		{
			StringBuilder b = new StringBuilder();
			foreach (ISegment s in this.Segments) {
				if (b.Length > 0)
					b.AppendLine();
				b.Append(document.GetText(s));
			}
			return b.ToString();
		}
		
		/// <inheritdoc/>
		public override Selection StartSelectionOrSetEndpoint(int startOffset, int newEndOffset)
		{
			return SetEndpoint(newEndOffset);
		}
		
		/// <inheritdoc/>
		public override int Length {
			get {
				return this.Segments.Sum(s => s.Length);
			}
		}
		
		/// <inheritdoc/>
		public override ISegment SurroundingSegment {
			get {
				return new SimpleSegment(Math.Min(StartOffset, EndOffset), Math.Abs(EndOffset - StartOffset));
			}
		}
		
		/// <inheritdoc/>
		public override IEnumerable<ISegment> Segments {
			get {
				TextLocation start = document.GetLocation(StartOffset);
				TextLocation end = document.GetLocation(EndOffset);
				DocumentLine line = document.GetLineByNumber(Math.Min(start.Line, end.Line));
				int numberOfLines = Math.Abs(start.Line - end.Line);
				int startCol = Math.Min(start.Column, end.Column);
				int endCol = Math.Max(start.Column, end.Column);
				for (int i = 0; i <= numberOfLines; i++) {
					if (line.Length + 1 >= startCol) {
						int thisLineEndCol = Math.Min(endCol, line.Length + 1);
						yield return new SimpleSegment(line.Offset + startCol - 1, thisLineEndCol - startCol);
					}
					line = line.NextLine;
				}
			}
		}
		
		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			RectangleSelection r = obj as RectangleSelection;
			return r != null && r.document == this.document && r.StartOffset == this.StartOffset && r.EndOffset == this.EndOffset;
		}
		
		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return StartOffset ^ EndOffset;
		}
		
		/// <inheritdoc/>
		public override Selection SetEndpoint(int newEndOffset)
		{
			return new RectangleSelection(this.document, this.StartOffset, newEndOffset);
		}
		
		/// <inheritdoc/>
		public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
		{
			return new RectangleSelection(document,
			                              e.GetNewOffset(StartOffset, AnchorMovementType.AfterInsertion),
			                              e.GetNewOffset(EndOffset, AnchorMovementType.BeforeInsertion));
		}
		
		/// <inheritdoc/>
		public override void ReplaceSelectionWithText(TextArea textArea, string newText)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			if (newText == null)
				throw new ArgumentNullException("newText");
			using (textArea.Document.RunUpdate()) {
				TextLocation start = document.GetLocation(StartOffset);
				TextLocation end = document.GetLocation(EndOffset);
				int editColumn = Math.Min(start.Column, end.Column);
				if (NewLineFinder.NextNewLine(newText, 0) == SimpleSegment.Invalid) {
					// insert same text into every line
					foreach (ISegment lineSegment in this.Segments.Reverse()) {
						ReplaceSingleLineText(textArea, lineSegment, newText);
					}
					
					TextLocation newStart = new TextLocation(start.Line, editColumn + newText.Length);
					TextLocation newEnd = new TextLocation(end.Line, editColumn + newText.Length);
					textArea.Caret.Location = newEnd;
					textArea.Selection = new RectangleSelection(document, document.GetOffset(newStart), document.GetOffset(newEnd));
				} else {
					// convert all segment start/ends to anchors
					var segments = this.Segments.Select(s => new AnchorSegment(this.document, s)).ToList();
					SimpleSegment ds = NewLineFinder.NextNewLine(newText, 0);
					// we'll check whether all lines have the same length. If so, we can continue using a rectangular selection.
					int commonLength = -1;
					// now insert lines into rectangular selection
					int lastDelimiterEnd = 0;
					bool isAtEnd = false;
					int i;
					for (i = 0; i < segments.Count; i++) {
						string lineText;
						if (ds == SimpleSegment.Invalid || (i == segments.Count - 1)) {
							lineText = newText.Substring(lastDelimiterEnd);
							isAtEnd = true;
							// if we have more lines to insert than this selection is long, we cannot continue using a rectangular selection
							if (ds != SimpleSegment.Invalid)
								commonLength = -1;
						} else {
							lineText = newText.Substring(lastDelimiterEnd, ds.Offset - lastDelimiterEnd);
						}
						if (i == 0) {
							commonLength = lineText.Length;
						} else if (commonLength != lineText.Length) {
							commonLength = -1;
						}
						ReplaceSingleLineText(textArea, segments[i], lineText);
						if (isAtEnd)
							break;
						lastDelimiterEnd = ds.EndOffset;
						ds = NewLineFinder.NextNewLine(newText, lastDelimiterEnd);
					}
					if (commonLength >= 0) {
						TextLocation newStart = new TextLocation(start.Line, editColumn + commonLength);
						TextLocation newEnd = new TextLocation(start.Line + i, editColumn + commonLength);
						textArea.Selection = new RectangleSelection(document, document.GetOffset(newStart), document.GetOffset(newEnd));
					} else {
						textArea.Selection = Selection.Empty;
					}
				}
			}
		}
		
		static void ReplaceSingleLineText(TextArea textArea, ISegment lineSegment, string newText)
		{
			if (lineSegment.Length == 0) {
				if (newText.Length > 0 && textArea.ReadOnlySectionProvider.CanInsert(lineSegment.Offset)) {
					textArea.Document.Insert(lineSegment.Offset, newText);
				}
			} else {
				ISegment[] segmentsToDelete = textArea.GetDeletableSegments(lineSegment);
				for (int i = segmentsToDelete.Length - 1; i >= 0; i--) {
					if (i == segmentsToDelete.Length - 1) {
						textArea.Document.Replace(segmentsToDelete[i], newText);
					} else {
						textArea.Document.Remove(segmentsToDelete[i]);
					}
				}
			}
		}
		
		/// <summary>
		/// Performs a rectangular paste operation.
		/// </summary>
		public static bool PerformRectangularPaste(TextArea textArea, int startOffset, string text, bool selectInsertedText)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			if (text == null)
				throw new ArgumentNullException("text");
			int newLineCount = text.Count(c => c == '\n');
			TextLocation startLocation = textArea.Document.GetLocation(startOffset);
			TextLocation endLocation = new TextLocation(startLocation.Line + newLineCount, startLocation.Column);
			if (endLocation.Line <= textArea.Document.LineCount) {
				int endOffset = textArea.Document.GetOffset(endLocation);
				if (textArea.Document.GetLocation(endOffset) == endLocation) {
					RectangleSelection rsel = new RectangleSelection(textArea.Document, startOffset, endOffset);
					rsel.ReplaceSelectionWithText(textArea, text);
					if (selectInsertedText && textArea.Selection is RectangleSelection) {
						RectangleSelection sel = (RectangleSelection)textArea.Selection;
						textArea.Selection = new RectangleSelection(textArea.Document, startOffset, sel.EndOffset);
					}
					return true;
				}
			}
			return false;
		}
		
		/// <summary>
		/// Gets the name of the entry in the DataObject that signals rectangle selections.
		/// </summary>
		public const string RectangularSelectionDataType = "AvalonEditRectangularSelection";
		
		/// <inheritdoc/>
		public override System.Windows.DataObject CreateDataObject(TextArea textArea)
		{
			var data = base.CreateDataObject(textArea);
			
			MemoryStream isRectangle = new MemoryStream(1);
			isRectangle.WriteByte(1);
			data.SetData(RectangularSelectionDataType, isRectangle, false);
			return data;
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			// It's possible that ToString() gets called on old (invalid) selections, e.g. for "change from... to..." debug message
			// make sure we don't crash even when the desired locations don't exist anymore.
			if (StartOffset < document.TextLength && EndOffset < document.TextLength)
				return "[RectangleSelection " + document.GetLocation(StartOffset) + " to " + document.GetLocation(EndOffset) + "]";
			else
				return "[RectangleSelection " + StartOffset + " to " + EndOffset + "]";
		}
	}
}
