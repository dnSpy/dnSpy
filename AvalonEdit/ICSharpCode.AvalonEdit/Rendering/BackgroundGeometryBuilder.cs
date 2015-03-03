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
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Helper for creating a PathGeometry.
	/// </summary>
	public sealed class BackgroundGeometryBuilder
	{
		double cornerRadius;
		
		/// <summary>
		/// Gets/sets the radius of the rounded corners.
		/// </summary>
		public double CornerRadius {
			get { return cornerRadius; }
			set { cornerRadius = value; }
		}
		
		/// <summary>
		/// Gets/Sets whether to align the geometry to whole pixels.
		/// </summary>
		public bool AlignToWholePixels { get; set; }
		
		/// <summary>
		/// Gets/Sets whether to align the geometry to the middle of pixels.
		/// </summary>
		public bool AlignToMiddleOfPixels { get; set; }
		
		/// <summary>
		/// Gets/Sets whether to extend the rectangles to full width at line end.
		/// </summary>
		public bool ExtendToFullWidthAtLineEnd { get; set; }
		
		/// <summary>
		/// Creates a new BackgroundGeometryBuilder instance.
		/// </summary>
		public BackgroundGeometryBuilder()
		{
		}
		
		/// <summary>
		/// Adds the specified segment to the geometry.
		/// </summary>
		public void AddSegment(TextView textView, ISegment segment)
		{
			if (textView == null)
				throw new ArgumentNullException("textView");
			Size pixelSize = PixelSnapHelpers.GetPixelSize(textView);
			foreach (Rect r in GetRectsForSegment(textView, segment, ExtendToFullWidthAtLineEnd)) {
				AddRectangle(pixelSize, r);
			}
		}
		
		/// <summary>
		/// Adds a rectangle to the geometry.
		/// </summary>
		/// <remarks>
		/// This overload will align the coordinates according to
		/// <see cref="AlignToWholePixels"/> or <see cref="AlignToMiddleOfPixels"/>.
		/// Use the <see cref="AddRectangle(double,double,double,double)"/>-overload instead if the coordinates should not be aligned.
		/// </remarks>
		public void AddRectangle(TextView textView, Rect rectangle)
		{
			AddRectangle(PixelSnapHelpers.GetPixelSize(textView), rectangle);
		}

		void AddRectangle(Size pixelSize, Rect r)
		{
			if (AlignToWholePixels) {
				AddRectangle(PixelSnapHelpers.Round(r.Left, pixelSize.Width),
				             PixelSnapHelpers.Round(r.Top + 1, pixelSize.Height),
				             PixelSnapHelpers.Round(r.Right, pixelSize.Width),
				             PixelSnapHelpers.Round(r.Bottom + 1, pixelSize.Height));
			} else if (AlignToMiddleOfPixels) {
				AddRectangle(PixelSnapHelpers.PixelAlign(r.Left, pixelSize.Width),
				             PixelSnapHelpers.PixelAlign(r.Top + 1, pixelSize.Height),
				             PixelSnapHelpers.PixelAlign(r.Right, pixelSize.Width),
				             PixelSnapHelpers.PixelAlign(r.Bottom + 1, pixelSize.Height));
			} else {
				AddRectangle(r.Left, r.Top + 1, r.Right, r.Bottom + 1);
			}
		}
		
		/// <summary>
		/// Calculates the list of rectangle where the segment in shown.
		/// This method usually returns one rectangle for each line inside the segment
		/// (but potentially more, e.g. when bidirectional text is involved).
		/// </summary>
		public static IEnumerable<Rect> GetRectsForSegment(TextView textView, ISegment segment, bool extendToFullWidthAtLineEnd = false)
		{
			if (textView == null)
				throw new ArgumentNullException("textView");
			if (segment == null)
				throw new ArgumentNullException("segment");
			return GetRectsForSegmentImpl(textView, segment, extendToFullWidthAtLineEnd);
		}
		
		static IEnumerable<Rect> GetRectsForSegmentImpl(TextView textView, ISegment segment, bool extendToFullWidthAtLineEnd)
		{
			int segmentStart = segment.Offset;
			int segmentEnd = segment.Offset + segment.Length;
			
			segmentStart = segmentStart.CoerceValue(0, textView.Document.TextLength);
			segmentEnd = segmentEnd.CoerceValue(0, textView.Document.TextLength);
			
			TextViewPosition start;
			TextViewPosition end;
			
			if (segment is SelectionSegment) {
				SelectionSegment sel = (SelectionSegment)segment;
				start = new TextViewPosition(textView.Document.GetLocation(sel.StartOffset), sel.StartVisualColumn);
				end = new TextViewPosition(textView.Document.GetLocation(sel.EndOffset), sel.EndVisualColumn);
			} else {
				start = new TextViewPosition(textView.Document.GetLocation(segmentStart));
				end = new TextViewPosition(textView.Document.GetLocation(segmentEnd));
			}
			
			foreach (VisualLine vl in textView.VisualLines) {
				int vlStartOffset = vl.FirstDocumentLine.Offset;
				if (vlStartOffset > segmentEnd)
					break;
				int vlEndOffset = vl.LastDocumentLine.Offset + vl.LastDocumentLine.Length;
				if (vlEndOffset < segmentStart)
					continue;
				
				int segmentStartVC;
				if (segmentStart < vlStartOffset)
					segmentStartVC = 0;
				else
					segmentStartVC = vl.ValidateVisualColumn(start, extendToFullWidthAtLineEnd);
				
				int segmentEndVC;
				if (segmentEnd > vlEndOffset)
					segmentEndVC = extendToFullWidthAtLineEnd ? int.MaxValue : vl.VisualLengthWithEndOfLineMarker;
				else
					segmentEndVC = vl.ValidateVisualColumn(end, extendToFullWidthAtLineEnd);
				
				foreach (var rect in ProcessTextLines(textView, vl, segmentStartVC, segmentEndVC))
					yield return rect;
			}
		}
		
		/// <summary>
		/// Calculates the rectangles for the visual column segment.
		/// This returns one rectangle for each line inside the segment.
		/// </summary>
		public static IEnumerable<Rect> GetRectsFromVisualSegment(TextView textView, VisualLine line, int startVC, int endVC)
		{
			if (textView == null)
				throw new ArgumentNullException("textView");
			if (line == null)
				throw new ArgumentNullException("line");
			return ProcessTextLines(textView, line, startVC, endVC);
		}

		static IEnumerable<Rect> ProcessTextLines(TextView textView, VisualLine visualLine, int segmentStartVC, int segmentEndVC)
		{
			TextLine lastTextLine = visualLine.TextLines.Last();
			Vector scrollOffset = textView.ScrollOffset;
			
			for (int i = 0; i < visualLine.TextLines.Count; i++) {
				TextLine line = visualLine.TextLines[i];
				double y = visualLine.GetTextLineVisualYPosition(line, VisualYPosition.LineTop);
				int visualStartCol = visualLine.GetTextLineVisualStartColumn(line);
				int visualEndCol = visualStartCol + line.Length;
				if (line != lastTextLine)
					visualEndCol -= line.TrailingWhitespaceLength;
				
				if (segmentEndVC < visualStartCol)
					break;
				if (lastTextLine != line && segmentStartVC > visualEndCol)
					continue;
				int segmentStartVCInLine = Math.Max(segmentStartVC, visualStartCol);
				int segmentEndVCInLine = Math.Min(segmentEndVC, visualEndCol);
				y -= scrollOffset.Y;
				if (segmentStartVCInLine == segmentEndVCInLine) {
					// GetTextBounds crashes for length=0, so we'll handle this case with GetDistanceFromCharacterHit
					// We need to return a rectangle to ensure empty lines are still visible
					double pos = visualLine.GetTextLineVisualXPosition(line, segmentStartVCInLine);
					pos -= scrollOffset.X;
					// The following special cases are necessary to get rid of empty rectangles at the end of a TextLine if "Show Spaces" is active.
					// If not excluded once, the same rectangle is calculated (and added) twice (since the offset could be mapped to two visual positions; end/start of line), if there is no trailing whitespace.
					// Skip this TextLine segment, if it is at the end of this line and this line is not the last line of the VisualLine and the selection continues and there is no trailing whitespace.
					if (segmentEndVCInLine == visualEndCol && i < visualLine.TextLines.Count - 1 && segmentEndVC > segmentEndVCInLine && line.TrailingWhitespaceLength == 0)
						continue;
					if (segmentStartVCInLine == visualStartCol && i > 0 && segmentStartVC < segmentStartVCInLine && visualLine.TextLines[i - 1].TrailingWhitespaceLength == 0)
						continue;
					yield return new Rect(pos, y, 1, line.Height);
				} else {
					Rect lastRect = Rect.Empty;
					if (segmentStartVCInLine <= visualEndCol) {
						foreach (TextBounds b in line.GetTextBounds(segmentStartVCInLine, segmentEndVCInLine - segmentStartVCInLine)) {
							double left = b.Rectangle.Left - scrollOffset.X;
							double right = b.Rectangle.Right - scrollOffset.X;
							if (!lastRect.IsEmpty)
								yield return lastRect;
							// left>right is possible in RTL languages
							lastRect = new Rect(Math.Min(left, right), y, Math.Abs(right - left), line.Height);
						}
					}
					if (segmentEndVC >= visualLine.VisualLengthWithEndOfLineMarker) {
						double left = (segmentStartVC > visualLine.VisualLengthWithEndOfLineMarker ? visualLine.GetTextLineVisualXPosition(lastTextLine, segmentStartVC) : line.Width) - scrollOffset.X;
						double right = ((segmentEndVC == int.MaxValue || line != lastTextLine) ? Math.Max(((IScrollInfo)textView).ExtentWidth, ((IScrollInfo)textView).ViewportWidth) : visualLine.GetTextLineVisualXPosition(lastTextLine, segmentEndVC)) - scrollOffset.X;
						Rect extendSelection = new Rect(Math.Min(left, right), y, Math.Abs(right - left), line.Height);
						if (!lastRect.IsEmpty) {
							if (extendSelection.IntersectsWith(lastRect)) {
								lastRect.Union(extendSelection);
								yield return lastRect;
							} else {
								yield return lastRect;
								yield return extendSelection;
							}
						} else
							yield return extendSelection;
					} else
						yield return lastRect;
				}
			}
		}
		
		PathFigureCollection figures = new PathFigureCollection();
		PathFigure figure;
		int insertionIndex;
		double lastTop, lastBottom;
		double lastLeft, lastRight;
		
		/// <summary>
		/// Adds a rectangle to the geometry.
		/// </summary>
		/// <remarks>
		/// This overload assumes that the coordinates are aligned properly
		/// (see <see cref="AlignToWholePixels"/>, <see cref="AlignToMiddleOfPixels"/>).
		/// Use the <see cref="AddRectangle(TextView,Rect)"/>-overload instead if the coordinates are not yet aligned.
		/// </remarks>
		public void AddRectangle(double left, double top, double right, double bottom)
		{
			if (!top.IsClose(lastBottom)) {
				CloseFigure();
			}
			if (figure == null) {
				figure = new PathFigure();
				figure.StartPoint = new Point(left, top + cornerRadius);
				if (Math.Abs(left - right) > cornerRadius) {
					figure.Segments.Add(MakeArc(left + cornerRadius, top, SweepDirection.Clockwise));
					figure.Segments.Add(MakeLineSegment(right - cornerRadius, top));
					figure.Segments.Add(MakeArc(right, top + cornerRadius, SweepDirection.Clockwise));
				}
				figure.Segments.Add(MakeLineSegment(right, bottom - cornerRadius));
				insertionIndex = figure.Segments.Count;
				//figure.Segments.Add(MakeArc(left, bottom - cornerRadius, SweepDirection.Clockwise));
			} else {
				if (!lastRight.IsClose(right)) {
					double cr = right < lastRight ? -cornerRadius : cornerRadius;
					SweepDirection dir1 = right < lastRight ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
					SweepDirection dir2 = right < lastRight ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;
					figure.Segments.Insert(insertionIndex++, MakeArc(lastRight + cr, lastBottom, dir1));
					figure.Segments.Insert(insertionIndex++, MakeLineSegment(right - cr, top));
					figure.Segments.Insert(insertionIndex++, MakeArc(right, top + cornerRadius, dir2));
				}
				figure.Segments.Insert(insertionIndex++, MakeLineSegment(right, bottom - cornerRadius));
				figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft, lastTop + cornerRadius));
				if (!lastLeft.IsClose(left)) {
					double cr = left < lastLeft ? cornerRadius : -cornerRadius;
					SweepDirection dir1 = left < lastLeft ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;
					SweepDirection dir2 = left < lastLeft ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
					figure.Segments.Insert(insertionIndex, MakeArc(lastLeft, lastBottom - cornerRadius, dir1));
					figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft - cr, lastBottom));
					figure.Segments.Insert(insertionIndex, MakeArc(left + cr, lastBottom, dir2));
				}
			}
			this.lastTop = top;
			this.lastBottom = bottom;
			this.lastLeft = left;
			this.lastRight = right;
		}
		
		ArcSegment MakeArc(double x, double y, SweepDirection dir)
		{
			ArcSegment arc = new ArcSegment(
				new Point(x, y),
				new Size(cornerRadius, cornerRadius),
				0, false, dir, true);
			arc.Freeze();
			return arc;
		}
		
		static LineSegment MakeLineSegment(double x, double y)
		{
			LineSegment ls = new LineSegment(new Point(x, y), true);
			ls.Freeze();
			return ls;
		}
		
		/// <summary>
		/// Closes the current figure.
		/// </summary>
		public void CloseFigure()
		{
			if (figure != null) {
				figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft, lastTop + cornerRadius));
				if (Math.Abs(lastLeft - lastRight) > cornerRadius) {
					figure.Segments.Insert(insertionIndex, MakeArc(lastLeft, lastBottom - cornerRadius, SweepDirection.Clockwise));
					figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft + cornerRadius, lastBottom));
					figure.Segments.Insert(insertionIndex, MakeArc(lastRight - cornerRadius, lastBottom, SweepDirection.Clockwise));
				}
				
				figure.IsClosed = true;
				figures.Add(figure);
				figure = null;
			}
		}
		
		/// <summary>
		/// Creates the geometry.
		/// Returns null when the geometry is empty!
		/// </summary>
		public Geometry CreateGeometry()
		{
			CloseFigure();
			if (figures.Count != 0) {
				PathGeometry g = new PathGeometry(figures);
				g.Freeze();
				return g;
			} else {
				return null;
			}
		}
	}
}
