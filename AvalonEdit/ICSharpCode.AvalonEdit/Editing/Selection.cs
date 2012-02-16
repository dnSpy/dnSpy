// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Base class for selections.
	/// </summary>
	public abstract class Selection
	{
		/// <summary>
		/// Creates a new simple selection that selects the text from startOffset to endOffset.
		/// </summary>
		public static Selection Create(TextArea textArea, int startOffset, int endOffset)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			if (startOffset == endOffset)
				return textArea.emptySelection;
			else
				return new SimpleSelection(textArea,
				                           new TextViewPosition(textArea.Document.GetLocation(startOffset)),
				                           new TextViewPosition(textArea.Document.GetLocation(endOffset)));
		}
		
		internal static Selection Create(TextArea textArea, TextViewPosition start, TextViewPosition end)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			if (textArea.Document.GetOffset(start.Location) == textArea.Document.GetOffset(end.Location) && start.VisualColumn == end.VisualColumn)
				return textArea.emptySelection;
			else
				return new SimpleSelection(textArea, start, end);
		}
		
		/// <summary>
		/// Creates a new simple selection that selects the text in the specified segment.
		/// </summary>
		public static Selection Create(TextArea textArea, ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException("segment");
			return Create(textArea, segment.Offset, segment.EndOffset);
		}
		
		internal readonly TextArea textArea;
		
		/// <summary>
		/// Constructor for Selection.
		/// </summary>
		protected Selection(TextArea textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			this.textArea = textArea;
		}
		
		/// <summary>
		/// Gets the selected text segments.
		/// </summary>
		public abstract IEnumerable<SelectionSegment> Segments { get; }
		
		/// <summary>
		/// Gets the smallest segment that contains all segments in this selection.
		/// May return null if the selection is empty.
		/// </summary>
		public abstract ISegment SurroundingSegment { get; }
		
		/// <summary>
		/// Replaces the selection with the specified text.
		/// </summary>
		public abstract void ReplaceSelectionWithText(string newText);
		
		internal string AddSpacesIfRequired(string newText, TextViewPosition start, TextViewPosition end)
		{
			if (EnableVirtualSpace && InsertVirtualSpaces(newText, start, end)) {
				var line = textArea.Document.GetLineByNumber(start.Line);
				string lineText = textArea.Document.GetText(line);
				var vLine = textArea.TextView.GetOrConstructVisualLine(line);
				int colDiff = start.VisualColumn - vLine.VisualLengthWithEndOfLineMarker;
				if (colDiff > 0) {
					string additionalSpaces = "";
					if (!textArea.Options.ConvertTabsToSpaces && lineText.Trim('\t').Length == 0) {
						int tabCount = (int)(colDiff / textArea.Options.IndentationSize);
						additionalSpaces = new string('\t', tabCount);
						colDiff -= tabCount * textArea.Options.IndentationSize;
					}
					additionalSpaces += new string(' ', colDiff);
					return additionalSpaces + newText;
				}
			}
			return newText;
		}
		
		bool InsertVirtualSpaces(string newText, TextViewPosition start, TextViewPosition end)
		{
			return (!string.IsNullOrEmpty(newText) || !(IsInVirtualSpace(start) && IsInVirtualSpace(end)))
				&& newText != "\r\n"
				&& newText != "\n" 
				&& newText != "\r";
		}
		
		bool IsInVirtualSpace(TextViewPosition pos)
		{
			return pos.VisualColumn > textArea.TextView.GetOrConstructVisualLine(textArea.Document.GetLineByNumber(pos.Line)).VisualLength;
		}
		
		/// <summary>
		/// Updates the selection when the document changes.
		/// </summary>
		public abstract Selection UpdateOnDocumentChange(DocumentChangeEventArgs e);
		
		/// <summary>
		/// Gets whether the selection is empty.
		/// </summary>
		public virtual bool IsEmpty {
			get { return Length == 0; }
		}
		
		/// <summary>
		/// Gets whether virtual space is enabled for this selection.
		/// </summary>
		public virtual bool EnableVirtualSpace {
			get { return textArea.Options.EnableVirtualSpace; }
		}
		
		/// <summary>
		/// Gets the selection length.
		/// </summary>
		public abstract int Length { get; }
		
		/// <summary>
		/// Returns a new selection with the changed end point.
		/// </summary>
		/// <exception cref="NotSupportedException">Cannot set endpoint for empty selection</exception>
		public abstract Selection SetEndpoint(TextViewPosition endPosition);
		
		/// <summary>
		/// If this selection is empty, starts a new selection from <paramref name="startPosition"/> to
		/// <paramref name="endPosition"/>, otherwise, changes the endpoint of this selection.
		/// </summary>
		public abstract Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition);
		
		/// <summary>
		/// Gets whether the selection is multi-line.
		/// </summary>
		public virtual bool IsMultiline {
			get {
				ISegment surroundingSegment = this.SurroundingSegment;
				if (surroundingSegment == null)
					return false;
				int start = surroundingSegment.Offset;
				int end = start + surroundingSegment.Length;
				var document = textArea.Document;
				if (document == null)
					throw ThrowUtil.NoDocumentAssigned();
				return document.GetLineByOffset(start) != document.GetLineByOffset(end);
			}
		}
		
		/// <summary>
		/// Gets the selected text.
		/// </summary>
		public virtual string GetText()
		{
			var document = textArea.Document;
			if (document == null)
				throw ThrowUtil.NoDocumentAssigned();
			StringBuilder b = null;
			string text = null;
			foreach (ISegment s in Segments) {
				if (text != null) {
					if (b == null)
						b = new StringBuilder(text);
					else
						b.Append(text);
				}
				text = document.GetText(s);
			}
			if (b != null) {
				if (text != null) b.Append(text);
				return b.ToString();
			} else {
				return text ?? string.Empty;
			}
		}
		
		/// <summary>
		/// Creates a HTML fragment for the selected text.
		/// </summary>
		public string CreateHtmlFragment(HtmlOptions options)
		{
			if (options == null)
				throw new ArgumentNullException("options");
			IHighlighter highlighter = textArea.GetService(typeof(IHighlighter)) as IHighlighter;
			StringBuilder html = new StringBuilder();
			bool first = true;
			foreach (ISegment selectedSegment in this.Segments) {
				if (first)
					first = false;
				else
					html.AppendLine("<br>");
				html.Append(HtmlClipboard.CreateHtmlFragment(textArea.Document, highlighter, selectedSegment, options));
			}
			return html.ToString();
		}
		
		/// <inheritdoc/>
		public abstract override bool Equals(object obj);
		
		/// <inheritdoc/>
		public abstract override int GetHashCode();
		
		/// <summary>
		/// Gets whether the specified offset is included in the selection.
		/// </summary>
		/// <returns>True, if the selection contains the offset (selection borders inclusive);
		/// otherwise, false.</returns>
		public virtual bool Contains(int offset)
		{
			if (this.IsEmpty)
				return false;
			if (this.SurroundingSegment.Contains(offset)) {
				foreach (ISegment s in this.Segments) {
					if (s.Contains(offset)) {
						return true;
					}
				}
			}
			return false;
		}
		
		/// <summary>
		/// Creates a data object containing the selection's text.
		/// </summary>
		public virtual DataObject CreateDataObject(TextArea textArea)
		{
			string text = GetText();
			// Ensure we use the appropriate newline sequence for the OS
			DataObject data = new DataObject(TextUtilities.NormalizeNewLines(text, Environment.NewLine));
			// we cannot use DataObject.SetText - then we cannot drag to SciTe
			// (but dragging to Word works in both cases)
			
			// Also copy text in HTML format to clipboard - good for pasting text into Word
			// or to the SharpDevelop forums.
			HtmlClipboard.SetHtml(data, CreateHtmlFragment(new HtmlOptions(textArea.Options)));
			return data;
		}
	}
}
