// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.Decompiler.Disassembler;
using ILSpy.Debugger.Bookmarks;
using ILSpy.Debugger.Services;
using ILSpy.Debugger.ToolTips;
using Mono.Cecil;

namespace ILSpy.Debugger.AvalonEdit
{
	public class IconBarMargin : AbstractMargin, IDisposable
	{
		static TypeDefinition currentTypeName;
		
		public static TypeDefinition CurrentType {
			get { return currentTypeName; }
			set { currentTypeName = value; }
		}
		
		public virtual void Dispose()
		{
			this.TextView = null; // detach from TextView (will also detach from manager)
		}
		
		/// <inheritdoc/>
		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
		{
			// accept clicks even when clicking on the background
			return new PointHitTestResult(this, hitTestParameters.HitPoint);
		}
		
		/// <inheritdoc/>
		protected override Size MeasureOverride(Size availableSize)
		{
			return new Size(18, 0);
		}
		
		protected override void OnRender(DrawingContext drawingContext)
		{
			Size renderSize = this.RenderSize;
			drawingContext.DrawRectangle(SystemColors.ControlBrush, null,
			                             new Rect(0, 0, renderSize.Width, renderSize.Height));
			drawingContext.DrawLine(new Pen(SystemColors.ControlDarkBrush, 1),
			                        new Point(renderSize.Width - 0.5, 0),
			                        new Point(renderSize.Width - 0.5, renderSize.Height));
			
			TextView textView = this.TextView;
			if (textView != null && textView.VisualLinesValid) {
				// create a dictionary line number => first bookmark
				Dictionary<int, BookmarkBase> bookmarkDict = new Dictionary<int, BookmarkBase>();
				foreach (var bm in BookmarkManager.Bookmarks) {
					if (IconBarMargin.CurrentType == null || bm.TypeName != IconBarMargin.CurrentType.FullName)
						continue;
					
					int line = bm.LineNumber;
					BookmarkBase existingBookmark;
					if (!bookmarkDict.TryGetValue(line, out existingBookmark) || bm.ZOrder > existingBookmark.ZOrder)
						bookmarkDict[line] = bm;
				}
				Size pixelSize = PixelSnapHelpers.GetPixelSize(this);
				foreach (VisualLine line in textView.VisualLines) {
					int lineNumber = line.FirstDocumentLine.LineNumber;
					BookmarkBase bm;
					if (bookmarkDict.TryGetValue(lineNumber, out bm)) {
						Rect rect = new Rect(0, PixelSnapHelpers.Round(line.VisualTop - textView.VerticalOffset, pixelSize.Height), 16, 16);
						drawingContext.DrawImage(bm.Image, rect);						
					}
				}				
			}
		}
		
		BookmarkBase GetBookmarkFromLine(int line)
		{
			BookmarkBase result = null;
			foreach (BookmarkBase bm in BookmarkManager.Bookmarks) {
				if (bm.LineNumber == line &&
				    IconBarMargin.CurrentType != null &&
				    bm.TypeName == IconBarMargin.CurrentType.FullName) {
					if (result == null || bm.ZOrder > result.ZOrder)
						result = bm;
				}
			}
			return result;
		}
		
		int GetLineFromMousePosition(MouseEventArgs e)
		{
			TextView textView = this.TextView;
			if (textView == null)
				return 0;
			VisualLine vl = textView.GetVisualLineFromVisualTop(e.GetPosition(textView).Y + textView.ScrollOffset.Y);
			if (vl == null)
				return 0;
			return vl.FirstDocumentLine.LineNumber;
		}
		
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			int line = GetLineFromMousePosition(e);
			if (!e.Handled && line != 0) {
				IBookmark bm = GetBookmarkFromLine(line);
				if (bm != null) {
					bm.MouseUp(e);
					if (CurrentType != null) {
						DebuggerService.ToggleBreakpointAt(CurrentType.FullName, line);
					}
					InvalidateVisual();
					if (e.Handled)
						return;
				}
				if (e.ChangedButton == MouseButton.Left) {
					if (CurrentType != null) {
						// no bookmark on the line: create a new breakpoint
						DebuggerService.ToggleBreakpointAt(CurrentType.FullName, line);
					}
				}
				InvalidateVisual();
			}
		}
	}
}
