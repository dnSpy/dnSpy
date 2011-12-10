// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.NRefactory;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.AvalonEdit
{
	public class IconBarMargin : AbstractMargin, IDisposable
	{
		readonly IconBarManager manager;
		
		public IconBarMargin(IconBarManager manager)
		{
			BookmarkManager.Added += new BookmarkEventHandler(OnBookmarkAdded);
			BookmarkManager.Removed += new BookmarkEventHandler(OnBookmarkRemoved);
			
			this.manager = manager;
		}
		
		public IconBarManager Manager {
			get { return manager; }
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
			// 16px wide icon + 1px each side padding + 1px right-side border
			return new Size(19, 0);
		}
		
		protected override void OnRender(DrawingContext drawingContext)
		{
			Size renderSize = this.RenderSize;
			drawingContext.DrawRectangle(SystemColors.ControlBrush, null,
			                             new Rect(0, 0, renderSize.Width, renderSize.Height));
			drawingContext.DrawLine(new Pen(SystemColors.ControlDarkBrush, 1),
			                        new Point(renderSize.Width - 0.5, 0),
			                        new Point(renderSize.Width - 0.5, renderSize.Height));
			
			ICSharpCode.AvalonEdit.Rendering.TextView textView = this.TextView;
			if (textView != null && textView.VisualLinesValid) {
				// create a dictionary line number => first bookmark
				Dictionary<int, IBookmark> bookmarkDict = new Dictionary<int, IBookmark>();
				foreach (var bm in BookmarkManager.Bookmarks) {
					if (bm is BreakpointBookmark) {
						if (DebugInformation.CodeMappings == null || DebugInformation.CodeMappings.Count == 0 ||
						    !DebugInformation.CodeMappings.ContainsKey(((BreakpointBookmark)bm).FunctionToken))
							continue;
					}
					int line = bm.LineNumber;
					IBookmark existingBookmark;
					if (!bookmarkDict.TryGetValue(line, out existingBookmark) || bm.ZOrder > existingBookmark.ZOrder)
						bookmarkDict[line] = bm;
				}
				
				foreach (var bm in manager.Bookmarks) {
					int line = bm.LineNumber;
					IBookmark existingBookmark;
					if (!bookmarkDict.TryGetValue(line, out existingBookmark) || bm.ZOrder > existingBookmark.ZOrder)
						bookmarkDict[line] = bm;
				}

				const double imagePadding = 1.0;
				Size pixelSize = PixelSnapHelpers.GetPixelSize(this);
				foreach (VisualLine line in textView.VisualLines) {
					int lineNumber = line.FirstDocumentLine.LineNumber;
					IBookmark bm;
					if (bookmarkDict.TryGetValue(lineNumber, out bm)) {
						Rect rect = new Rect(imagePadding, PixelSnapHelpers.Round(line.VisualTop - textView.VerticalOffset, pixelSize.Height), 16, 16);
						if (dragDropBookmark == bm && dragStarted)
							drawingContext.PushOpacity(0.5);
						drawingContext.DrawImage(bm.Image, rect);
						if (dragDropBookmark == bm && dragStarted)
							drawingContext.Pop();
					}
				}
				if (dragDropBookmark != null && dragStarted) {
					Rect rect = new Rect(imagePadding, PixelSnapHelpers.Round(dragDropCurrentPoint - 8, pixelSize.Height), 16, 16);
					drawingContext.DrawImage(dragDropBookmark.Image, rect);
				}
			}
		}
		
		IBookmark dragDropBookmark; // bookmark being dragged (!=null if drag'n'drop is active)
		double dragDropStartPoint;
		double dragDropCurrentPoint;
		bool dragStarted; // whether drag'n'drop operation has started (mouse was moved minimum distance)
		
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			int line = GetLineFromMousePosition(e);
			if (!e.Handled && line > 0) {
				IBookmark bm = GetBookmarkFromLine(line);
				if (bm != null) {
					bm.MouseDown(e);
					if (!e.Handled) {
						if (e.ChangedButton == MouseButton.Left && bm.CanDragDrop && CaptureMouse()) {
							StartDragDrop(bm, e);
							e.Handled = true;
						}
					}
				}
			}
			// don't allow selecting text through the IconBarMargin
			if (e.ChangedButton == MouseButton.Left)
				e.Handled = true;
		}
		
		IBookmark GetBookmarkFromLine(int line)
		{
			BookmarkBase result = null;
			foreach (BookmarkBase bm in BookmarkManager.Bookmarks) {
				if (bm.LineNumber != line)
					continue;
				if (DebugInformation.CodeMappings == null || DebugInformation.CodeMappings.Count == 0 ||
				    !DebugInformation.CodeMappings.ContainsKey(((BreakpointBookmark)bm).FunctionToken))
					continue;
				
				if (result == null || bm.ZOrder > result.ZOrder)
					return result;
			}
			
			return manager.Bookmarks.FirstOrDefault(b => b.LineNumber == line);
		}
		
		protected override void OnLostMouseCapture(MouseEventArgs e)
		{
			CancelDragDrop();
			base.OnLostMouseCapture(e);
		}
		
		void StartDragDrop(IBookmark bm, MouseEventArgs e)
		{
			dragDropBookmark = bm;
			dragDropStartPoint = dragDropCurrentPoint = e.GetPosition(this).Y;
			if (TextView != null) {
				TextArea area = TextView.Services.GetService(typeof(TextArea)) as TextArea;
				if (area != null)
					area.PreviewKeyDown += TextArea_PreviewKeyDown;
			}
		}
		
		void CancelDragDrop()
		{
			if (dragDropBookmark != null) {
				dragDropBookmark = null;
				dragStarted = false;
				if (TextView != null) {
					TextArea area = TextView.Services.GetService(typeof(TextArea)) as TextArea;
					if (area != null)
						area.PreviewKeyDown -= TextArea_PreviewKeyDown;
				}
				ReleaseMouseCapture();
				InvalidateVisual();
			}
		}
		
		void TextArea_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			// any key press cancels drag'n'drop
			CancelDragDrop();
			if (e.Key == Key.Escape)
				e.Handled = true;
		}
		
		public int GetLineFromMousePosition(MouseEventArgs e)
		{
			ICSharpCode.AvalonEdit.Rendering.TextView textView = this.TextView;
			if (textView == null)
				return 0;
			VisualLine vl = textView.GetVisualLineFromVisualTop(e.GetPosition(textView).Y + textView.ScrollOffset.Y);
			if (vl == null)
				return 0;
			return vl.FirstDocumentLine.LineNumber;
		}
		
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (dragDropBookmark != null) {
				dragDropCurrentPoint = e.GetPosition(this).Y;
				if (Math.Abs(dragDropCurrentPoint - dragDropStartPoint) > SystemParameters.MinimumVerticalDragDistance)
					dragStarted = true;
				InvalidateVisual();
			}
		}
		
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			int line = GetLineFromMousePosition(e);
			if (!e.Handled && dragDropBookmark != null) {
				if (dragStarted) {
					if (line != 0)
						dragDropBookmark.Drop(line);
					e.Handled = true;
				}
				CancelDragDrop();
			}
			if (!e.Handled && line != 0) {
				var bm = GetBookmarkFromLine(line);
				if (bm != null) {
					bm.MouseUp(e);
					
					if (bm is BookmarkBase) {
						if ((bm as BookmarkBase).CanToggle) {
							BookmarkManager.RemoveMark(bm as BookmarkBase);
							InvalidateVisual();
						}
					}
					if (e.Handled)
						return;
				}
				if (e.ChangedButton == MouseButton.Left) {
					// see IBookmarkActionEntry interface
				}
				InvalidateVisual();
			}
		}
		
		public void OnBookmarkAdded(object sender, BookmarkEventArgs args)
		{
			var breakpoint = args.Bookmark as BreakpointBookmark;
			if (null == breakpoint)
				return;
			var storage = DebugInformation.CodeMappings;
			if (storage == null || storage.Count == 0)
				return;
			var key = breakpoint.MemberReference.MetadataToken.ToInt32();
			if (storage.ContainsKey(key))
			{
				// register to show enabled/disabled state
				breakpoint.ImageChanged += delegate { InvalidateVisual(); };
				InvalidateVisual();
			}
		}
		
		public void OnBookmarkRemoved(object sender, BookmarkEventArgs args)
		{
			var breakpoint = args.Bookmark as BreakpointBookmark;
			if (null == breakpoint)
				return;
			var storage = DebugInformation.CodeMappings;
			if (storage == null || storage.Count == 0)
				return;
			var key = breakpoint.MemberReference.MetadataToken.ToInt32();
			if (storage.ContainsKey(key))
			{
				breakpoint.ImageChanged -= delegate { InvalidateVisual(); };
				InvalidateVisual();
			}
		}
		
		public void SyncBookmarks()
		{
			var storage = DebugInformation.CodeMappings;
			if (storage == null || storage.Count == 0)
				return;
			
			// TODO: handle other types of bookmarks
			// remove existing bookmarks and create new ones
			// update of existing bookmarks for new position does not update TextMarker
			// this is only done in TextMarkerService handlers for BookmarkManager.Added/Removed
			List<BreakpointBookmark> newBookmarks = new List<BreakpointBookmark>();
			for (int i = BookmarkManager.Bookmarks.Count - 1; i >= 0; --i) {
				var breakpoint = BookmarkManager.Bookmarks[i] as BreakpointBookmark;
				if (breakpoint == null)
					continue;
				
				var key = breakpoint.FunctionToken;
				if (!storage.ContainsKey(key))
					continue;
				
				bool isMatch;
				SourceCodeMapping map = storage[key].GetInstructionByTokenAndOffset(breakpoint.ILRange.From, out isMatch);
				
				if (map != null) {
					BreakpointBookmark newBookmark = new BreakpointBookmark(breakpoint.MemberReference,
					                                                        new TextLocation(map.StartLocation.Line, 0),
					                                                        breakpoint.FunctionToken,
					                                                        map.ILInstructionOffset,
					                                                        BreakpointAction.Break);
					newBookmark.IsEnabled = breakpoint.IsEnabled;
					
					newBookmarks.Add(newBookmark);

					BookmarkManager.RemoveMark(breakpoint);
				}
			}
			newBookmarks.ForEach(m => BookmarkManager.AddMark(m));
			SyncCurrentLineBookmark();
		}
		
		void SyncCurrentLineBookmark()
		{
			// checks
			if (CurrentLineBookmark.Instance == null)
				return;
			
			var codeMappings = DebugInformation.CodeMappings;
			if (codeMappings == null)
				return;
			
			// 1. Save it's data
			int line = CurrentLineBookmark.Instance.LineNumber;
			var markerType = CurrentLineBookmark.Instance.MemberReference;
			int token = markerType.MetadataToken.ToInt32();
			int offset = CurrentLineBookmark.Instance.ILOffset;
			
			if (!codeMappings.ContainsKey(token))
				return;
			
			// 2. map the marker line
			MemberReference memberReference;
			int newline;
			if (codeMappings[token].GetInstructionByTokenAndOffset(offset, out memberReference, out newline)) {
				// 3. create breakpoint for new languages
				DebuggerService.JumpToCurrentLine(memberReference, newline, 0, newline, 0, offset);
			}
		}
	}
}
