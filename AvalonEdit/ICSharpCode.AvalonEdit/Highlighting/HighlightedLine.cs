// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Represents a highlighted document line.
	/// </summary>
	public class HighlightedLine
	{
		/// <summary>
		/// Creates a new HighlightedLine instance.
		/// </summary>
		public HighlightedLine(IDocument document, IDocumentLine documentLine)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			//if (!document.Lines.Contains(documentLine))
			//	throw new ArgumentException("Line is null or not part of document");
			this.Document = document;
			this.DocumentLine = documentLine;
			this.Sections = new NullSafeCollection<HighlightedSection>();
		}
		
		/// <summary>
		/// Gets the document associated with this HighlightedLine.
		/// </summary>
		public IDocument Document { get; private set; }
		
		/// <summary>
		/// Gets the document line associated with this HighlightedLine.
		/// </summary>
		public IDocumentLine DocumentLine { get; private set; }
		
		/// <summary>
		/// Gets the highlighted sections.
		/// The sections are not overlapping, but they may be nested.
		/// In that case, outer sections come in the list before inner sections.
		/// The sections are sorted by start offset.
		/// </summary>
		public IList<HighlightedSection> Sections { get; private set; }
		
		/// <summary>
		/// Validates that the sections are sorted correctly, and that they are not overlapping.
		/// </summary>
		/// <seealso cref="Sections"/>
		public void ValidateInvariants()
		{
			var line = this;
			int lineStartOffset = line.DocumentLine.Offset;
			int lineEndOffset = line.DocumentLine.EndOffset;
			for (int i = 0; i < line.Sections.Count; i++) {
				HighlightedSection s1 = line.Sections[i];
				if (s1.Offset < lineStartOffset || s1.Length < 0 || s1.Offset + s1.Length > lineEndOffset)
					throw new InvalidOperationException("Section is outside line bounds");
				for (int j = i + 1; j < line.Sections.Count; j++) {
					HighlightedSection s2 = line.Sections[j];
					if (s2.Offset >= s1.Offset + s1.Length) {
						// s2 is after s1
					} else if (s2.Offset >= s1.Offset && s2.Offset + s2.Length <= s1.Offset + s1.Length) {
						// s2 is nested within s1
					} else {
						throw new InvalidOperationException("Sections are overlapping or incorrectly sorted.");
					}
				}
			}
		}
		
		#region Merge
		/// <summary>
		/// Merges the additional line into this line.
		/// </summary>
		public void MergeWith(HighlightedLine additionalLine)
		{
			if (additionalLine == null)
				return;
			#if DEBUG
			ValidateInvariants();
			additionalLine.ValidateInvariants();
			#endif
			
			int pos = 0;
			Stack<int> activeSectionEndOffsets = new Stack<int>();
			int lineEndOffset = this.DocumentLine.EndOffset;
			activeSectionEndOffsets.Push(lineEndOffset);
			foreach (HighlightedSection newSection in additionalLine.Sections) {
				int newSectionStart = newSection.Offset;
				// Track the existing sections using the stack, up to the point where
				// we need to insert the first part of the newSection
				while (pos < this.Sections.Count) {
					HighlightedSection s = this.Sections[pos];
					if (newSection.Offset < s.Offset)
						break;
					while (s.Offset > activeSectionEndOffsets.Peek()) {
						activeSectionEndOffsets.Pop();
					}
					activeSectionEndOffsets.Push(s.Offset + s.Length);
					pos++;
				}
				// Now insert the new section
				// Create a copy of the stack so that we can track the sections we traverse
				// during the insertion process:
				Stack<int> insertionStack = new Stack<int>(activeSectionEndOffsets.Reverse());
				// The stack enumerator reverses the order of the elements, so we call Reverse() to restore
				// the original order.
				int i;
				for (i = pos; i < this.Sections.Count; i++) {
					HighlightedSection s = this.Sections[i];
					if (newSection.Offset + newSection.Length <= s.Offset)
						break;
					// Insert a segment in front of s:
					Insert(ref i, ref newSectionStart, s.Offset, newSection.Color, insertionStack);
					
					while (s.Offset > insertionStack.Peek()) {
						insertionStack.Pop();
					}
					insertionStack.Push(s.Offset + s.Length);
				}
				Insert(ref i, ref newSectionStart, newSection.Offset + newSection.Length, newSection.Color, insertionStack);
			}
			
			#if DEBUG
			ValidateInvariants();
			#endif
		}
		
		void Insert(ref int pos, ref int newSectionStart, int insertionEndPos, HighlightingColor color, Stack<int> insertionStack)
		{
			if (newSectionStart >= insertionEndPos) {
				// nothing to insert here
				return;
			}
			
			while (insertionStack.Peek() <= newSectionStart) {
				insertionStack.Pop();
			}
			while (insertionStack.Peek() < insertionEndPos) {
				int end = insertionStack.Pop();
				// insert the portion from newSectionStart to end
				if (end > newSectionStart) {
					this.Sections.Insert(pos++, new HighlightedSection {
					                     	Offset = newSectionStart,
					                     	Length = end - newSectionStart,
					                     	Color = color
					                     });
					newSectionStart = end;
				}
			}
			if (insertionEndPos > newSectionStart) {
				this.Sections.Insert(pos++, new HighlightedSection {
				                     	Offset = newSectionStart,
				                     	Length = insertionEndPos - newSectionStart,
				                     	Color = color
				                     });
				newSectionStart = insertionEndPos;
			}
		}
		#endregion
		
		#region WriteTo / ToHtml
		sealed class HtmlElement : IComparable<HtmlElement>
		{
			internal readonly int Offset;
			internal readonly int Nesting;
			internal readonly bool IsEnd;
			internal readonly HighlightingColor Color;
			
			public HtmlElement(int offset, int nesting, bool isEnd, HighlightingColor color)
			{
				this.Offset = offset;
				this.Nesting = nesting;
				this.IsEnd = isEnd;
				this.Color = color;
			}
			
			public int CompareTo(HtmlElement other)
			{
				int r = Offset.CompareTo(other.Offset);
				if (r != 0)
					return r;
				if (IsEnd != other.IsEnd) {
					if (IsEnd)
						return -1;
					else
						return 1;
				} else {
					if (IsEnd)
						return other.Nesting.CompareTo(Nesting);
					else
						return Nesting.CompareTo(other.Nesting);
				}
			}
		}
		
		/// <summary>
		/// Writes the highlighted line to the RichTextWriter.
		/// </summary>
		internal void WriteTo(RichTextWriter writer)
		{
			int startOffset = this.DocumentLine.Offset;
			WriteTo(writer, startOffset, startOffset + this.DocumentLine.Length);
		}
		
		/// <summary>
		/// Writes a part of the highlighted line to the RichTextWriter.
		/// </summary>
		internal void WriteTo(RichTextWriter writer, int startOffset, int endOffset)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");
			int documentLineStartOffset = this.DocumentLine.Offset;
			int documentLineEndOffset = documentLineStartOffset + this.DocumentLine.Length;
			if (startOffset < documentLineStartOffset || startOffset > documentLineEndOffset)
				throw new ArgumentOutOfRangeException("startOffset", startOffset, "Value must be between " + documentLineStartOffset + " and " + documentLineEndOffset);
			if (endOffset < startOffset || endOffset > documentLineEndOffset)
				throw new ArgumentOutOfRangeException("endOffset", endOffset, "Value must be between startOffset and " + documentLineEndOffset);
			ISegment requestedSegment = new SimpleSegment(startOffset, endOffset - startOffset);
			
			List<HtmlElement> elements = new List<HtmlElement>();
			for (int i = 0; i < this.Sections.Count; i++) {
				HighlightedSection s = this.Sections[i];
				if (SimpleSegment.GetOverlap(s, requestedSegment).Length > 0) {
					elements.Add(new HtmlElement(s.Offset, i, false, s.Color));
					elements.Add(new HtmlElement(s.Offset + s.Length, i, true, s.Color));
				}
			}
			elements.Sort();
			
			IDocument document = this.Document;
			int textOffset = startOffset;
			foreach (HtmlElement e in elements) {
				int newOffset = Math.Min(e.Offset, endOffset);
				if (newOffset > startOffset) {
					document.WriteTextTo(writer, textOffset, newOffset - textOffset);
				}
				textOffset = Math.Max(textOffset, newOffset);
				if (e.IsEnd)
					writer.EndSpan();
				else
					writer.BeginSpan(e.Color);
			}
			document.WriteTextTo(writer, textOffset, endOffset - textOffset);
		}
		
		/// <summary>
		/// Produces HTML code for the line, with &lt;span class="colorName"&gt; tags.
		/// </summary>
		public string ToHtml(HtmlOptions options = null)
		{
			StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
			using (var htmlWriter = new HtmlRichTextWriter(stringWriter, options)) {
				WriteTo(htmlWriter);
			}
			return stringWriter.ToString();
		}
		
		/// <summary>
		/// Produces HTML code for a section of the line, with &lt;span class="colorName"&gt; tags.
		/// </summary>
		public string ToHtml(int startOffset, int endOffset, HtmlOptions options = null)
		{
			StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
			using (var htmlWriter = new HtmlRichTextWriter(stringWriter, options)) {
				WriteTo(htmlWriter, startOffset, endOffset);
			}
			return stringWriter.ToString();
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[" + GetType().Name + " " + ToHtml() + "]";
		}
		#endregion
		
		/// <summary>
		/// Creates a <see cref="HighlightedInlineBuilder"/> that stores the text and highlighting of this line.
		/// </summary>
		[Obsolete("Use ToRichText() instead")]
		public HighlightedInlineBuilder ToInlineBuilder()
		{
			HighlightedInlineBuilder builder = new HighlightedInlineBuilder(Document.GetText(DocumentLine));
			int startOffset = DocumentLine.Offset;
			foreach (HighlightedSection section in Sections) {
				builder.SetHighlighting(section.Offset - startOffset, section.Length, section.Color);
			}
			return builder;
		}
		
		/// <summary>
		/// Creates a <see cref="RichTextModel"/> that stores the highlighting of this line.
		/// </summary>
		public RichTextModel ToRichTextModel()
		{
			var builder = new RichTextModel();
			int startOffset = DocumentLine.Offset;
			foreach (HighlightedSection section in Sections) {
				builder.ApplyHighlighting(section.Offset - startOffset, section.Length, section.Color);
			}
			return builder;
		}
		
		/// <summary>
		/// Creates a <see cref="RichText"/> that stores the text and highlighting of this line.
		/// </summary>
		public RichText ToRichText()
		{
			return new RichText(Document.GetText(DocumentLine), ToRichTextModel());
		}
	}
}
