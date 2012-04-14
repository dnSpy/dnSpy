// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Represents a visual line in the document.
	/// A visual line usually corresponds to one DocumentLine, but it can span multiple lines if
	/// all but the first are collapsed.
	/// </summary>
	public sealed class VisualLine
	{
		TextView textView;
		List<VisualLineElement> elements;
		internal bool hasInlineObjects;
		
		/// <summary>
		/// Gets the document to which this VisualLine belongs.
		/// </summary>
		public TextDocument Document { get; private set; }
		
		/// <summary>
		/// Gets the first document line displayed by this visual line.
		/// </summary>
		public DocumentLine FirstDocumentLine { get; private set; }
		
		/// <summary>
		/// Gets the last document line displayed by this visual line.
		/// </summary>
		public DocumentLine LastDocumentLine { get; private set; }
		
		/// <summary>
		/// Gets a read-only collection of line elements.
		/// </summary>
		public ReadOnlyCollection<VisualLineElement> Elements { get; private set; }
		
		/// <summary>
		/// Gets a read-only collection of text lines.
		/// </summary>
		public ReadOnlyCollection<TextLine> TextLines { get; private set; }
		
		/// <summary>
		/// Gets the start offset of the VisualLine inside the document.
		/// This is equivalent to <c>FirstDocumentLine.Offset</c>.
		/// </summary>
		public int StartOffset {
			get {
				return FirstDocumentLine.Offset;
			}
		}
		
		/// <summary>
		/// Length in visual line coordinates.
		/// </summary>
		public int VisualLength { get; private set; }
		
		/// <summary>
		/// Length in visual line coordinates including the end of line marker, if TextEditorOptions.ShowEndOfLine is enabled.
		/// </summary>
		public int VisualLengthWithEndOfLineMarker {
			get {
				int length = VisualLength;
				if (textView.Options.ShowEndOfLine && LastDocumentLine.NextLine != null) length++;
				return length;
			}
		}
		
		/// <summary>
		/// Gets the height of the visual line in device-independent pixels.
		/// </summary>
		public double Height { get; private set; }
		
		/// <summary>
		/// Gets the Y position of the line. This is measured in device-independent pixels relative to the start of the document.
		/// </summary>
		public double VisualTop { get; internal set; }
		
		internal VisualLine(TextView textView, DocumentLine firstDocumentLine)
		{
			Debug.Assert(textView != null);
			Debug.Assert(firstDocumentLine != null);
			this.textView = textView;
			this.Document = textView.Document;
			this.FirstDocumentLine = firstDocumentLine;
		}
		
		internal void ConstructVisualElements(ITextRunConstructionContext context, VisualLineElementGenerator[] generators)
		{
			foreach (VisualLineElementGenerator g in generators) {
				g.StartGeneration(context);
			}
			elements = new List<VisualLineElement>();
			PerformVisualElementConstruction(generators);
			foreach (VisualLineElementGenerator g in generators) {
				g.FinishGeneration();
			}
			
			this.Elements = elements.AsReadOnly();
			CalculateOffsets(context.GlobalTextRunProperties);
		}
		
		void PerformVisualElementConstruction(VisualLineElementGenerator[] generators)
		{
			TextDocument document = this.Document;
			int offset = FirstDocumentLine.Offset;
			int currentLineEnd = offset + FirstDocumentLine.Length;
			LastDocumentLine = FirstDocumentLine;
			int askInterestOffset = 0; // 0 or 1
			while (offset + askInterestOffset <= currentLineEnd) {
				int textPieceEndOffset = currentLineEnd;
				foreach (VisualLineElementGenerator g in generators) {
					g.cachedInterest = g.GetFirstInterestedOffset(offset + askInterestOffset);
					if (g.cachedInterest != -1) {
						if (g.cachedInterest < offset)
							throw new ArgumentOutOfRangeException(g.GetType().Name + ".GetFirstInterestedOffset",
							                                      g.cachedInterest,
							                                      "GetFirstInterestedOffset must not return an offset less than startOffset. Return -1 to signal no interest.");
						if (g.cachedInterest < textPieceEndOffset)
							textPieceEndOffset = g.cachedInterest;
					}
				}
				Debug.Assert(textPieceEndOffset >= offset);
				if (textPieceEndOffset > offset) {
					int textPieceLength = textPieceEndOffset - offset;
					elements.Add(new VisualLineText(this, textPieceLength));
					offset = textPieceEndOffset;
				}
				// If no elements constructed / only zero-length elements constructed:
				// do not asking the generators again for the same location (would cause endless loop)
				askInterestOffset = 1;
				foreach (VisualLineElementGenerator g in generators) {
					if (g.cachedInterest == offset) {
						VisualLineElement element = g.ConstructElement(offset);
						if (element != null) {
							elements.Add(element);
							if (element.DocumentLength > 0) {
								// a non-zero-length element was constructed
								askInterestOffset = 0;
								offset += element.DocumentLength;
								if (offset > currentLineEnd) {
									DocumentLine newEndLine = document.GetLineByOffset(offset);
									currentLineEnd = newEndLine.Offset + newEndLine.Length;
									this.LastDocumentLine = newEndLine;
									if (currentLineEnd < offset) {
										throw new InvalidOperationException(
											"The VisualLineElementGenerator " + g.GetType().Name +
											" produced an element which ends within the line delimiter");
									}
								}
								break;
							}
						}
					}
				}
			}
		}
		
		void CalculateOffsets(TextRunProperties globalTextRunProperties)
		{
			int visualOffset = 0;
			int textOffset = 0;
			foreach (VisualLineElement element in Elements) {
				element.VisualColumn = visualOffset;
				element.RelativeTextOffset = textOffset;
				element.SetTextRunProperties(new VisualLineElementTextRunProperties(globalTextRunProperties));
				visualOffset += element.VisualLength;
				textOffset += element.DocumentLength;
			}
			VisualLength = visualOffset;
			Debug.Assert(textOffset == LastDocumentLine.EndOffset - FirstDocumentLine.Offset);
		}
		
		internal void RunTransformers(ITextRunConstructionContext context, IVisualLineTransformer[] transformers)
		{
			foreach (IVisualLineTransformer transformer in transformers) {
				transformer.Transform(context, elements);
			}
		}
		
		internal void SetTextLines(List<TextLine> textLines)
		{
			this.TextLines = textLines.AsReadOnly();
			Height = 0;
			foreach (TextLine line in textLines)
				Height += line.Height;
		}
		
		/// <summary>
		/// Gets the visual column from a document offset relative to the first line start.
		/// </summary>
		public int GetVisualColumn(int relativeTextOffset)
		{
			ThrowUtil.CheckNotNegative(relativeTextOffset, "relativeTextOffset");
			foreach (VisualLineElement element in elements) {
				if (element.RelativeTextOffset <= relativeTextOffset
				    && element.RelativeTextOffset + element.DocumentLength >= relativeTextOffset)
				{
					return element.GetVisualColumn(relativeTextOffset);
				}
			}
			return VisualLength;
		}
		
		/// <summary>
		/// Gets the document offset (relative to the first line start) from a visual column.
		/// </summary>
		public int GetRelativeOffset(int visualColumn)
		{
			ThrowUtil.CheckNotNegative(visualColumn, "visualColumn");
			int documentLength = 0;
			foreach (VisualLineElement element in elements) {
				if (element.VisualColumn <= visualColumn
				    && element.VisualColumn + element.VisualLength > visualColumn)
				{
					return element.GetRelativeOffset(visualColumn);
				}
				documentLength += element.DocumentLength;
			}
			return documentLength;
		}
		
		/// <summary>
		/// Gets the text line containing the specified visual column.
		/// </summary>
		public TextLine GetTextLine(int visualColumn)
		{
			if (visualColumn < 0)
				throw new ArgumentOutOfRangeException("visualColumn");
			if (visualColumn >= VisualLengthWithEndOfLineMarker)
				return TextLines[TextLines.Count - 1];
			foreach (TextLine line in TextLines) {
				if (visualColumn < line.Length)
					return line;
				else
					visualColumn -= line.Length;
			}
			throw new InvalidOperationException("Shouldn't happen (VisualLength incorrect?)");
		}
		
		/// <summary>
		/// Gets the visual top from the specified text line.
		/// </summary>
		/// <returns>Distance in device-independent pixels
		/// from the top of the document to the top of the specified text line.</returns>
		public double GetTextLineVisualYPosition(TextLine textLine, VisualYPosition yPositionMode)
		{
			if (textLine == null)
				throw new ArgumentNullException("textLine");
			double pos = VisualTop;
			foreach (TextLine tl in TextLines) {
				if (tl == textLine) {
					switch (yPositionMode) {
						case VisualYPosition.LineTop:
							return pos;
						case VisualYPosition.LineMiddle:
							return pos + tl.Height / 2;
						case VisualYPosition.LineBottom:
							return pos + tl.Height;
						case VisualYPosition.TextTop:
							return pos + tl.Baseline - textView.DefaultBaseline;
						case VisualYPosition.TextBottom:
							return pos + tl.Baseline - textView.DefaultBaseline + textView.DefaultLineHeight;
						case VisualYPosition.TextMiddle:
							return pos + tl.Baseline - textView.DefaultBaseline + textView.DefaultLineHeight / 2;
						case VisualYPosition.Baseline:
							return pos + tl.Baseline;
						default:
							throw new ArgumentException("Invalid yPositionMode:" + yPositionMode);
					}
				} else {
					pos += tl.Height;
				}
			}
			throw new ArgumentException("textLine is not a line in this VisualLine");
		}
		
		/// <summary>
		/// Gets the start visual column from the specified text line.
		/// </summary>
		public int GetTextLineVisualStartColumn(TextLine textLine)
		{
			if (!TextLines.Contains(textLine))
				throw new ArgumentException("textLine is not a line in this VisualLine");
			int col = 0;
			foreach (TextLine tl in TextLines) {
				if (tl == textLine)
					break;
				else
					col += tl.Length;
			}
			return col;
		}
		
		/// <summary>
		/// Gets a TextLine by the visual position.
		/// </summary>
		public TextLine GetTextLineByVisualYPosition(double visualTop)
		{
			const double epsilon = 0.0001;
			double pos = this.VisualTop;
			foreach (TextLine tl in TextLines) {
				pos += tl.Height;
				if (visualTop + epsilon < pos)
					return tl;
			}
			return TextLines[TextLines.Count - 1];
		}
		
		/// <summary>
		/// Gets the visual position from the specified visualColumn.
		/// </summary>
		/// <returns>Position in device-independent pixels
		/// relative to the top left of the document.</returns>
		public Point GetVisualPosition(int visualColumn, VisualYPosition yPositionMode)
		{
			TextLine textLine = GetTextLine(visualColumn);
			double xPos = GetTextLineVisualXPosition(textLine, visualColumn);
			double yPos = GetTextLineVisualYPosition(textLine, yPositionMode);
			return new Point(xPos, yPos);
		}
		
		/// <summary>
		/// Gets the distance to the left border of the text area of the specified visual column.
		/// The visual column must belong to the specified text line.
		/// </summary>
		public double GetTextLineVisualXPosition(TextLine textLine, int visualColumn)
		{
			if (textLine == null)
				throw new ArgumentNullException("textLine");
			double xPos = textLine.GetDistanceFromCharacterHit(
				new CharacterHit(Math.Min(visualColumn, VisualLengthWithEndOfLineMarker), 0));
			if (visualColumn > VisualLengthWithEndOfLineMarker) {
				xPos += (visualColumn - VisualLengthWithEndOfLineMarker) * textView.WideSpaceWidth;
			}
			return xPos;
		}
		
		/// <summary>
		/// Gets the visual column from a document position (relative to top left of the document).
		/// If the user clicks between two visual columns, rounds to the nearest column.
		/// </summary>
		public int GetVisualColumn(Point point)
		{
			return GetVisualColumn(point, textView.Options.EnableVirtualSpace);
		}
		
		/// <summary>
		/// Gets the visual column from a document position (relative to top left of the document).
		/// If the user clicks between two visual columns, rounds to the nearest column.
		/// </summary>
		public int GetVisualColumn(Point point, bool allowVirtualSpace)
		{
			return GetVisualColumn(GetTextLineByVisualYPosition(point.Y), point.X, allowVirtualSpace);
		}
		
		/// <summary>
		/// Gets the visual column from a document position (relative to top left of the document).
		/// If the user clicks between two visual columns, rounds to the nearest column.
		/// </summary>
		public int GetVisualColumn(TextLine textLine, double xPos, bool allowVirtualSpace)
		{
			if (xPos > textLine.WidthIncludingTrailingWhitespace) {
				if (allowVirtualSpace && textLine == TextLines[TextLines.Count - 1]) {
					int virtualX = (int)Math.Round((xPos - textLine.WidthIncludingTrailingWhitespace) / textView.WideSpaceWidth);
					return VisualLengthWithEndOfLineMarker + virtualX;
				}
			}
			CharacterHit ch = textLine.GetCharacterHitFromDistance(xPos);
			return ch.FirstCharacterIndex + ch.TrailingLength;
		}
		
		/// <summary>
		/// Validates the visual column and returns the correct one.
		/// </summary>
		public int ValidateVisualColumn(TextViewPosition position, bool allowVirtualSpace)
		{
			return ValidateVisualColumn(Document.GetOffset(position.Location), position.VisualColumn, allowVirtualSpace);
		}
		
		/// <summary>
		/// Validates the visual column and returns the correct one.
		/// </summary>
		public int ValidateVisualColumn(int offset, int visualColumn, bool allowVirtualSpace)
		{
			int firstDocumentLineOffset = this.FirstDocumentLine.Offset;
			if (visualColumn < 0) {
				return GetVisualColumn(offset - firstDocumentLineOffset);
			} else {
				int offsetFromVisualColumn = GetRelativeOffset(visualColumn);
				offsetFromVisualColumn += firstDocumentLineOffset;
				if (offsetFromVisualColumn != offset) {
					return GetVisualColumn(offset - firstDocumentLineOffset);
				} else {
					if (visualColumn > VisualLength && !allowVirtualSpace) {
						return VisualLength;
					}
				}
			}
			return visualColumn;
		}
		
		/// <summary>
		/// Gets the visual column from a document position (relative to top left of the document).
		/// If the user clicks between two visual columns, returns the first of those columns.
		/// </summary>
		public int GetVisualColumnFloor(Point point)
		{
			return GetVisualColumnFloor(point, textView.Options.EnableVirtualSpace);
		}
		
		/// <summary>
		/// Gets the visual column from a document position (relative to top left of the document).
		/// If the user clicks between two visual columns, returns the first of those columns.
		/// </summary>
		public int GetVisualColumnFloor(Point point, bool allowVirtualSpace)
		{
			TextLine textLine = GetTextLineByVisualYPosition(point.Y);
			if (point.X > textLine.WidthIncludingTrailingWhitespace) {
				if (allowVirtualSpace && textLine == TextLines[TextLines.Count - 1]) {
					// clicking virtual space in the last line
					int virtualX = (int)((point.X - textLine.WidthIncludingTrailingWhitespace) / textView.WideSpaceWidth);
					return VisualLengthWithEndOfLineMarker + virtualX;
				} else {
					// GetCharacterHitFromDistance returns a hit with FirstCharacterIndex=last character in line
					// and TrailingLength=1 when clicking behind the line, so the floor function needs to handle this case
					// specially and return the line's end column instead.
					return GetTextLineVisualStartColumn(textLine) + textLine.Length;
				}
			}
			CharacterHit ch = textLine.GetCharacterHitFromDistance(point.X);
			return ch.FirstCharacterIndex;
		}
		
		/// <summary>
		/// Gets whether the visual line was disposed.
		/// </summary>
		public bool IsDisposed { get; internal set; }
		
		/// <summary>
		/// Gets the next possible caret position after visualColumn, or -1 if there is no caret position.
		/// </summary>
		public int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode, bool allowVirtualSpace)
		{
			if (!HasStopsInVirtualSpace(mode))
				allowVirtualSpace = false;
			
			if (elements.Count == 0) {
				// special handling for empty visual lines:
				if (allowVirtualSpace) {
					if (direction == LogicalDirection.Forward)
						return Math.Max(0, visualColumn + 1);
					else if (visualColumn > 0)
						return visualColumn - 1;
					else
						return -1;
				} else {
					// even though we don't have any elements,
					// there's a single caret stop at visualColumn 0
					if (visualColumn < 0 && direction == LogicalDirection.Forward)
						return 0;
					else if (visualColumn > 0 && direction == LogicalDirection.Backward)
						return 0;
					else
						return -1;
				}
			}
			
			int i;
			if (direction == LogicalDirection.Backward) {
				// Search Backwards:
				// If the last element doesn't handle line borders, return the line end as caret stop
				
				if (visualColumn > this.VisualLength && !elements[elements.Count-1].HandlesLineBorders && HasImplicitStopAtLineEnd(mode)) {
					if (allowVirtualSpace)
						return visualColumn - 1;
					else
						return this.VisualLength;
				}
				// skip elements that start after or at visualColumn
				for (i = elements.Count - 1; i >= 0; i--) {
					if (elements[i].VisualColumn < visualColumn)
						break;
				}
				// search last element that has a caret stop
				for (; i >= 0; i--) {
					int pos = elements[i].GetNextCaretPosition(
						Math.Min(visualColumn, elements[i].VisualColumn + elements[i].VisualLength + 1),
						direction, mode);
					if (pos >= 0)
						return pos;
				}
				// If we've found nothing, and the first element doesn't handle line borders,
				// return the line start as normal caret stop.
				if (visualColumn > 0 && !elements[0].HandlesLineBorders && HasImplicitStopAtLineStart(mode))
					return 0;
			} else {
				// Search Forwards:
				// If the first element doesn't handle line borders, return the line start as caret stop
				if (visualColumn < 0 && !elements[0].HandlesLineBorders && HasImplicitStopAtLineStart(mode))
					return 0;
				// skip elements that end before or at visualColumn
				for (i = 0; i < elements.Count; i++) {
					if (elements[i].VisualColumn + elements[i].VisualLength > visualColumn)
						break;
				}
				// search first element that has a caret stop
				for (; i < elements.Count; i++) {
					int pos = elements[i].GetNextCaretPosition(
						Math.Max(visualColumn, elements[i].VisualColumn - 1),
						direction, mode);
					if (pos >= 0)
						return pos;
				}
				// if we've found nothing, and the last element doesn't handle line borders,
				// return the line end as caret stop
				if ((allowVirtualSpace || !elements[elements.Count-1].HandlesLineBorders) && HasImplicitStopAtLineEnd(mode)) {
					if (visualColumn < this.VisualLength)
						return this.VisualLength;
					else if (allowVirtualSpace)
						return visualColumn + 1;
				}
			}
			// we've found nothing, return -1 and let the caret search continue in the next line
			return -1;
		}
		
		static bool HasStopsInVirtualSpace(CaretPositioningMode mode)
		{
			return mode == CaretPositioningMode.Normal;
		}
		
		static bool HasImplicitStopAtLineStart(CaretPositioningMode mode)
		{
			return mode == CaretPositioningMode.Normal;
		}
		
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "mode",
		                                                 Justification = "make method consistent with HasImplicitStopAtLineStart; might depend on mode in the future")]
		static bool HasImplicitStopAtLineEnd(CaretPositioningMode mode)
		{
			return true;
		}
		
		VisualLineDrawingVisual visual;
		
		internal VisualLineDrawingVisual Render()
		{
			if (visual == null)
				visual = new VisualLineDrawingVisual(this);
			return visual;
		}
	}
	
	sealed class VisualLineDrawingVisual : DrawingVisual
	{
		public readonly VisualLine VisualLine;
		public readonly double Height;
		internal bool IsAdded;
		
		public VisualLineDrawingVisual(VisualLine visualLine)
		{
			this.VisualLine = visualLine;
			var drawingContext = RenderOpen();
			double pos = 0;
			foreach (TextLine textLine in visualLine.TextLines) {
				textLine.Draw(drawingContext, new Point(0, pos), InvertAxes.None);
				pos += textLine.Height;
			}
			this.Height = pos;
			drawingContext.Close();
		}
		
		protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
		{
			return null;
		}
		
		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
		{
			return null;
		}
	}
}
