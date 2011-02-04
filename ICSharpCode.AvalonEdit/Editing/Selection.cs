// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Base class for selections.
	/// </summary>
	public abstract class Selection
	{
		/// <summary>
		/// Gets the empty selection.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification="Empty selection is immutable")]
		public static readonly Selection Empty = new SimpleSelection(-1, -1);
		
		/// <summary>
		/// Gets the selected text segments.
		/// </summary>
		public abstract IEnumerable<ISegment> Segments { get; }
		
		/// <summary>
		/// Gets the smallest segment that contains all segments in this selection.
		/// May return null if the selection is empty.
		/// </summary>
		public abstract ISegment SurroundingSegment { get; }
		
		/// <summary>
		/// Replaces the selection with the specified text.
		/// </summary>
		public abstract void ReplaceSelectionWithText(TextArea textArea, string newText);
		
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
		/// Gets the selection length.
		/// </summary>
		public abstract int Length { get; }
		
		/// <summary>
		/// Returns a new selection with the changed end point.
		/// </summary>
		/// <exception cref="NotSupportedException">Cannot set endpoint for empty selection</exception>
		public abstract Selection SetEndpoint(int newEndOffset);
		
		/// <summary>
		/// If this selection is empty, starts a new selection from <paramref name="startOffset"/> to
		/// <paramref name="newEndOffset"/>, otherwise, changes the endpoint of this selection.
		/// </summary>
		public virtual Selection StartSelectionOrSetEndpoint(int startOffset, int newEndOffset)
		{
			if (IsEmpty)
				return new SimpleSelection(startOffset, newEndOffset);
			else
				return SetEndpoint(newEndOffset);
		}
		
		/// <summary>
		/// Gets whether the selection is multi-line.
		/// </summary>
		public virtual bool IsMultiline(TextDocument document)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			ISegment surroundingSegment = this.SurroundingSegment;
			if (surroundingSegment == null)
				return false;
			int start = surroundingSegment.Offset;
			int end = start + surroundingSegment.Length;
			return document.GetLineByOffset(start) != document.GetLineByOffset(end);
		}
		
		/// <summary>
		/// Gets the selected text.
		/// </summary>
		public virtual string GetText(TextDocument document)
		{
			if (document == null)
				throw new ArgumentNullException("document");
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
		public string CreateHtmlFragment(TextArea textArea, HtmlOptions options)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
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
			string text = GetText(textArea.Document);
			// Ensure we use the appropriate newline sequence for the OS
			DataObject data = new DataObject(TextUtilities.NormalizeNewLines(text, Environment.NewLine));
			// we cannot use DataObject.SetText - then we cannot drag to SciTe
			// (but dragging to Word works in both cases)
			
			// Also copy text in HTML format to clipboard - good for pasting text into Word
			// or to the SharpDevelop forums.
			HtmlClipboard.SetHtml(data, CreateHtmlFragment(textArea, new HtmlOptions(textArea.Options)));
			return data;
		}
	}
}
