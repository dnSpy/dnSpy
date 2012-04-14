// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// A section that can be folded.
	/// </summary>
	public sealed class FoldingSection : TextSegment
	{
		readonly FoldingManager manager;
		bool isFolded;
		internal CollapsedLineSection[] collapsedSections;
		string title;
		
		/// <summary>
		/// Gets/sets if the section is folded.
		/// </summary>
		public bool IsFolded {
			get { return isFolded; }
			set {
				if (isFolded != value) {
					isFolded = value;
					if (value) {
						// Create collapsed sections
						if (manager != null) {
							DocumentLine startLine = manager.document.GetLineByOffset(StartOffset);
							DocumentLine endLine = manager.document.GetLineByOffset(EndOffset);
							if (startLine != endLine) {
								DocumentLine startLinePlusOne = startLine.NextLine;
								collapsedSections = new CollapsedLineSection[manager.textViews.Count];
								for (int i = 0; i < collapsedSections.Length; i++) {
									collapsedSections[i] = manager.textViews[i].CollapseLines(startLinePlusOne, endLine);
								}
							}
						}
					} else {
						// Destroy collapsed sections
						RemoveCollapsedLineSection();
					}
					if (manager != null)
						manager.Redraw(this);
				}
			}
		}
		
		/// <summary>
		/// Creates new collapsed section when a text view is added to the folding manager.
		/// </summary>
		internal CollapsedLineSection CollapseSection(TextView textView)
		{
			DocumentLine startLine = manager.document.GetLineByOffset(StartOffset);
			DocumentLine endLine = manager.document.GetLineByOffset(EndOffset);
			if (startLine != endLine) {
				DocumentLine startLinePlusOne = startLine.NextLine;
				return textView.CollapseLines(startLinePlusOne, endLine);
			}
			return null;
		}
		
		internal void ValidateCollapsedLineSections()
		{
			if (!isFolded) {
				RemoveCollapsedLineSection();
				return;
			}
			DocumentLine startLine = manager.document.GetLineByOffset(StartOffset);
			DocumentLine endLine = manager.document.GetLineByOffset(EndOffset);
			if (startLine == endLine) {
				RemoveCollapsedLineSection();
			} else {
				if (collapsedSections == null)
					collapsedSections = new CollapsedLineSection[manager.textViews.Count];
				// Validate collapsed line sections
				DocumentLine startLinePlusOne = startLine.NextLine;
				for (int i = 0; i < collapsedSections.Length; i++) {
					var collapsedSection = collapsedSections[i];
					if (collapsedSection == null || collapsedSection.Start != startLinePlusOne || collapsedSection.End != endLine) {
						// recreate this collapsed section
						Debug.WriteLine("CollapsedLineSection validation - recreate collapsed section from " + startLinePlusOne + " to " + endLine);
						if (collapsedSection != null)
							collapsedSection.Uncollapse();
						collapsedSections[i] = manager.textViews[i].CollapseLines(startLinePlusOne, endLine);
					}
				}
			}
		}
		
		/// <summary>
		/// Gets/Sets the text used to display the collapsed version of the folding section.
		/// </summary>
		public string Title {
			get {
				return title;
			}
			set {
				if (title != value) {
					title = value;
					if (this.IsFolded && manager != null)
						manager.Redraw(this);
				}
			}
		}
		
		/// <summary>
		/// Gets the content of the collapsed lines as text.
		/// </summary>
		public string TextContent {
			get {
				return manager.document.GetText(StartOffset, EndOffset - StartOffset);
			}
		}
		
		/// <summary>
		/// Gets the content of the collapsed lines as tooltip text.
		/// </summary>
		[Obsolete]
		public string TooltipText {
			get {
				// This fixes SD-1394:
				// Each line is checked for leading indentation whitespaces. If
				// a line has the same or more indentation than the first line,
				// it is reduced. If a line is less indented than the first line
				// the indentation is removed completely.
				//
				// See the following example:
				// 	line 1
				// 		line 2
				// 			line 3
				//  line 4
				//
				// is reduced to:
				// line 1
				// 	line 2
				// 		line 3
				// line 4
				
				var startLine = manager.document.GetLineByOffset(StartOffset);
				var endLine = manager.document.GetLineByOffset(EndOffset);
				var builder = new StringBuilder();
				
				var current = startLine;
				ISegment startIndent = TextUtilities.GetLeadingWhitespace(manager.document, startLine);
				
				while (current != endLine.NextLine) {
					ISegment currentIndent = TextUtilities.GetLeadingWhitespace(manager.document, current);
					
					if (current == startLine && current == endLine)
						builder.Append(manager.document.GetText(StartOffset, EndOffset - StartOffset));
					else if (current == startLine) {
						if (current.EndOffset - StartOffset > 0)
							builder.AppendLine(manager.document.GetText(StartOffset, current.EndOffset - StartOffset).TrimStart());
					} else if (current == endLine) {
						if (startIndent.Length <= currentIndent.Length)
							builder.Append(manager.document.GetText(current.Offset + startIndent.Length, EndOffset - current.Offset - startIndent.Length));
						else
							builder.Append(manager.document.GetText(current.Offset + currentIndent.Length, EndOffset - current.Offset - currentIndent.Length));
					} else {
						if (startIndent.Length <= currentIndent.Length)
							builder.AppendLine(manager.document.GetText(current.Offset + startIndent.Length, current.Length - startIndent.Length));
						else
							builder.AppendLine(manager.document.GetText(current.Offset + currentIndent.Length, current.Length - currentIndent.Length));
					}
					
					current = current.NextLine;
				}
				
				return builder.ToString();
			}
		}
		
		/// <summary>
		/// Gets/Sets an additional object associated with this folding section.
		/// </summary>
		public object Tag { get; set; }
		
		internal FoldingSection(FoldingManager manager, int startOffset, int endOffset)
		{
			this.manager = manager;
			this.StartOffset = startOffset;
			this.Length = endOffset - startOffset;
		}
		
		void RemoveCollapsedLineSection()
		{
			if (collapsedSections != null) {
				foreach (var collapsedSection in collapsedSections) {
					if (collapsedSection != null && collapsedSection.Start != null)
						collapsedSection.Uncollapse();
				}
				collapsedSections = null;
			}
		}
	}
}
