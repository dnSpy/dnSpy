// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
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
						if (manager != null) {
							DocumentLine startLine = manager.document.GetLineByOffset(StartOffset);
							DocumentLine endLine = manager.document.GetLineByOffset(EndOffset);
							if (startLine != endLine) {
								DocumentLine startLinePlusOne = startLine.NextLine;
								collapsedSections = manager.CollapseLines(startLinePlusOne, endLine);
							}
						}
					} else {
						RemoveCollapsedLineSection();
					}
					if (manager != null)
						manager.Redraw(this);
				}
			}
		}
		
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
