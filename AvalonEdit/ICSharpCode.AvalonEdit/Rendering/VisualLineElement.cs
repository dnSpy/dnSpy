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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Represents a visual element in the document.
	/// </summary>
	public abstract class VisualLineElement
	{
		/// <summary>
		/// Creates a new VisualLineElement.
		/// </summary>
		/// <param name="visualLength">The length of the element in VisualLine coordinates. Must be positive.</param>
		/// <param name="documentLength">The length of the element in the document. Must be non-negative.</param>
		protected VisualLineElement(int visualLength, int documentLength)
		{
			if (visualLength < 1)
				throw new ArgumentOutOfRangeException("visualLength", visualLength, "Value must be at least 1");
			if (documentLength < 0)
				throw new ArgumentOutOfRangeException("documentLength", documentLength, "Value must be at least 0");
			this.VisualLength = visualLength;
			this.DocumentLength = documentLength;
		}
		
		/// <summary>
		/// Gets the length of this element in visual columns.
		/// </summary>
		public int VisualLength { get; private set; }
		
		/// <summary>
		/// Gets the length of this element in the text document.
		/// </summary>
		public int DocumentLength { get; private set; }
		
		/// <summary>
		/// Gets the visual column where this element starts.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods",
		                                                 Justification = "This property holds the start visual column, use GetVisualColumn to get inner visual columns.")]
		public int VisualColumn { get; internal set; }
		
		/// <summary>
		/// Gets the text offset where this element starts, relative to the start text offset of the visual line.
		/// </summary>
		public int RelativeTextOffset { get; internal set; }
		
		/// <summary>
		/// Gets the text run properties.
		/// A unique <see cref="VisualLineElementTextRunProperties"/> instance is used for each
		/// <see cref="VisualLineElement"/>; colorizing code may assume that modifying the
		/// <see cref="VisualLineElementTextRunProperties"/> will affect only this
		/// <see cref="VisualLineElement"/>.
		/// </summary>
		public VisualLineElementTextRunProperties TextRunProperties { get; private set; }
		
		/// <summary>
		/// Gets/sets the brush used for the background of this <see cref="VisualLineElement" />.
		/// </summary>
		public Brush BackgroundBrush { get; set; }
		
		internal void SetTextRunProperties(VisualLineElementTextRunProperties p)
		{
			this.TextRunProperties = p;
		}
		
		/// <summary>
		/// Creates the TextRun for this line element.
		/// </summary>
		/// <param name="startVisualColumn">
		/// The visual column from which the run should be constructed.
		/// Normally the same value as the <see cref="VisualColumn"/> property is used to construct the full run;
		/// but when word-wrapping is active, partial runs might be created.
		/// </param>
		/// <param name="context">
		/// Context object that contains information relevant for text run creation.
		/// </param>
		public abstract TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context);
		
		/// <summary>
		/// Retrieves the text span immediately before the visual column.
		/// </summary>
		/// <remarks>This method is used for word-wrapping in bidirectional text.</remarks>
		public virtual TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int visualColumnLimit, ITextRunConstructionContext context)
		{
			return null;
		}
		
		/// <summary>
		/// Gets if this VisualLineElement can be split.
		/// </summary>
		public virtual bool CanSplit {
			get { return false; }
		}
		
		/// <summary>
		/// Splits the element.
		/// </summary>
		/// <param name="splitVisualColumn">Position inside this element at which it should be broken</param>
		/// <param name="elements">The collection of line elements</param>
		/// <param name="elementIndex">The index at which this element is in the elements list.</param>
		public virtual void Split(int splitVisualColumn, IList<VisualLineElement> elements, int elementIndex)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>
		/// Helper method for splitting this line element into two, correctly updating the
		/// <see cref="VisualLength"/>, <see cref="DocumentLength"/>, <see cref="VisualColumn"/>
		/// and <see cref="RelativeTextOffset"/> properties.
		/// </summary>
		/// <param name="firstPart">The element before the split position.</param>
		/// <param name="secondPart">The element after the split position.</param>
		/// <param name="splitVisualColumn">The split position as visual column.</param>
		/// <param name="splitRelativeTextOffset">The split position as text offset.</param>
		protected void SplitHelper(VisualLineElement firstPart, VisualLineElement secondPart, int splitVisualColumn, int splitRelativeTextOffset)
		{
			if (firstPart == null)
				throw new ArgumentNullException("firstPart");
			if (secondPart == null)
				throw new ArgumentNullException("secondPart");
			int relativeSplitVisualColumn = splitVisualColumn - VisualColumn;
			int relativeSplitRelativeTextOffset = splitRelativeTextOffset - RelativeTextOffset;
			
			if (relativeSplitVisualColumn <= 0 || relativeSplitVisualColumn >= VisualLength)
				throw new ArgumentOutOfRangeException("splitVisualColumn", splitVisualColumn, "Value must be between " + (VisualColumn + 1) + " and " + (VisualColumn + VisualLength - 1));
			if (relativeSplitRelativeTextOffset < 0 || relativeSplitRelativeTextOffset > DocumentLength)
				throw new ArgumentOutOfRangeException("splitRelativeTextOffset", splitRelativeTextOffset, "Value must be between " + (RelativeTextOffset) + " and " + (RelativeTextOffset + DocumentLength));
			int oldVisualLength = VisualLength;
			int oldDocumentLength = DocumentLength;
			int oldVisualColumn = VisualColumn;
			int oldRelativeTextOffset = RelativeTextOffset;
			firstPart.VisualColumn = oldVisualColumn;
			secondPart.VisualColumn = oldVisualColumn + relativeSplitVisualColumn;
			firstPart.RelativeTextOffset = oldRelativeTextOffset;
			secondPart.RelativeTextOffset = oldRelativeTextOffset + relativeSplitRelativeTextOffset;
			firstPart.VisualLength = relativeSplitVisualColumn;
			secondPart.VisualLength = oldVisualLength - relativeSplitVisualColumn;
			firstPart.DocumentLength = relativeSplitRelativeTextOffset;
			secondPart.DocumentLength = oldDocumentLength - relativeSplitRelativeTextOffset;
			if (firstPart.TextRunProperties == null)
				firstPart.TextRunProperties = TextRunProperties.Clone();
			if (secondPart.TextRunProperties == null)
				secondPart.TextRunProperties = TextRunProperties.Clone();
		}
		
		/// <summary>
		/// Gets the visual column of a text location inside this element.
		/// The text offset is given relative to the visual line start.
		/// </summary>
		public virtual int GetVisualColumn(int relativeTextOffset)
		{
			if (relativeTextOffset >= this.RelativeTextOffset + DocumentLength)
				return VisualColumn + VisualLength;
			else
				return VisualColumn;
		}
		
		/// <summary>
		/// Gets the text offset of a visual column inside this element.
		/// </summary>
		/// <returns>A text offset relative to the visual line start.</returns>
		public virtual int GetRelativeOffset(int visualColumn)
		{
			if (visualColumn >= this.VisualColumn + VisualLength)
				return RelativeTextOffset + DocumentLength;
			else
				return RelativeTextOffset;
		}
		
		/// <summary>
		/// Gets the next caret position inside this element.
		/// </summary>
		/// <param name="visualColumn">The visual column from which the search should be started.</param>
		/// <param name="direction">The search direction (forwards or backwards).</param>
		/// <param name="mode">Whether to stop only at word borders.</param>
		/// <returns>The visual column of the next caret position, or -1 if there is no next caret position.</returns>
		/// <remarks>
		/// In the space between two line elements, it is sufficient that one of them contains a caret position;
		/// though in many cases, both of them contain one.
		/// </remarks>
		public virtual int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
		{
			int stop1 = this.VisualColumn;
			int stop2 = this.VisualColumn + this.VisualLength;
			if (direction == LogicalDirection.Backward) {
				if (visualColumn > stop2 && mode != CaretPositioningMode.WordStart && mode != CaretPositioningMode.WordStartOrSymbol)
					return stop2;
				else if (visualColumn > stop1)
					return stop1;
			} else {
				if (visualColumn < stop1)
					return stop1;
				else if (visualColumn < stop2 && mode != CaretPositioningMode.WordStart && mode != CaretPositioningMode.WordStartOrSymbol)
					return stop2;
			}
			return -1;
		}
		
		/// <summary>
		/// Gets whether the specified offset in this element is considered whitespace.
		/// </summary>
		public virtual bool IsWhitespace(int visualColumn)
		{
			return false;
		}
		
		/// <summary>
		/// Gets whether the <see cref="GetNextCaretPosition"/> implementation handles line borders.
		/// If this property returns false, the caller of GetNextCaretPosition should handle the line
		/// borders (i.e. place caret stops at the start and end of the line).
		/// This property has an effect only for VisualLineElements that are at the start or end of a
		/// <see cref="VisualLine"/>.
		/// </summary>
		public virtual bool HandlesLineBorders {
			get { return false; }
		}
		
		/// <summary>
		/// Queries the cursor over the visual line element.
		/// </summary>
		protected internal virtual void OnQueryCursor(QueryCursorEventArgs e)
		{
		}
		
		/// <summary>
		/// Allows the visual line element to handle a mouse event.
		/// </summary>
		protected internal virtual void OnMouseDown(MouseButtonEventArgs e)
		{
		}
		
		/// <summary>
		/// Allows the visual line element to handle a mouse event.
		/// </summary>
		protected internal virtual void OnMouseUp(MouseButtonEventArgs e)
		{
		}
	}
}
