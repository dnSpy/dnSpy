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
using dnlib.DotNet;

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
				Dictionary<int, List<IBookmark>> bookmarkDict = new Dictionary<int, List<IBookmark>>();
				foreach (var bm in BookmarkManager.Bookmarks) {
					if (bm is BreakpointBookmark) {
						var cm = DebugInformation.CodeMappings;
						if (cm == null || cm.Count == 0 || !cm.ContainsKey(((BreakpointBookmark)bm).MethodKey))
							continue;
					}
					int line = bm.LineNumber;
					List<IBookmark> list;
					if (!bookmarkDict.TryGetValue(line, out list))
						bookmarkDict[line] = list = new List<IBookmark>();
					list.Add(bm);
				}
				
				foreach (var bm in manager.Bookmarks) {
					int line = bm.LineNumber;
					List<IBookmark> list;
					if (!bookmarkDict.TryGetValue(line, out list))
						bookmarkDict[line] = list = new List<IBookmark>();
					list.Add(bm);
				}

				const double imagePadding = 1.0;
				Size pixelSize = PixelSnapHelpers.GetPixelSize(this);
				foreach (VisualLine line in textView.VisualLines) {
					int lineNumber = line.FirstDocumentLine.LineNumber;
					List<IBookmark> list;
					if (!bookmarkDict.TryGetValue(lineNumber, out list))
						continue;
					list.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
					foreach (var bm in list) {
						Rect rect = new Rect(imagePadding, PixelSnapHelpers.Round(line.VisualTop - textView.VerticalOffset, pixelSize.Height), 16, 16);
						drawingContext.DrawImage(bm.Image, rect);
					}
				}
			}
		}
		
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			int line = GetLineFromMousePosition(e);
			if (!e.Handled && line > 0) {
				IBookmark bm = GetBookmarkFromLine(line);
				if (bm != null)
					bm.MouseDown(e);
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
				if (bm is BreakpointBookmark) {
					var cm = DebugInformation.CodeMappings;
					if (cm == null || cm.Count == 0 || !cm.ContainsKey(((BreakpointBookmark)bm).MethodKey))
						continue;
				}
				
				if (result == null || bm.ZOrder > result.ZOrder)
					return result;
			}
			
			return manager.Bookmarks.FirstOrDefault(b => b.LineNumber == line);
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
		
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			int line = GetLineFromMousePosition(e);
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
			var key = new MethodKey(breakpoint.MemberReference);
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
			var key = new MethodKey(breakpoint.MemberReference);
			if (storage.ContainsKey(key))
			{
				breakpoint.ImageChanged -= delegate { InvalidateVisual(); };
				InvalidateVisual();
			}
		}
		
		public void SyncBookmarks()
		{
			var storage = DebugInformation.CodeMappings;
			if (storage != null && storage.Count != 0) {
				// TODO: handle other types of bookmarks
				// remove existing bookmarks and create new ones
				// update of existing bookmarks for new position does not update TextMarker
				// this is only done in TextMarkerService handlers for BookmarkManager.Added/Removed
				for (int i = BookmarkManager.Bookmarks.Count - 1; i >= 0; --i) {
					var breakpoint = BookmarkManager.Bookmarks[i] as BreakpointBookmark;
					if (breakpoint == null)
						continue;

					var key = breakpoint.MethodKey;
					if (!storage.ContainsKey(key))
						continue;

					bool isMatch;
					SourceCodeMapping map = storage[key].GetInstructionByOffset(breakpoint.ILRange.From, out isMatch);

					if (map != null) {
						BreakpointBookmark newBookmark = new BreakpointBookmark(breakpoint.MemberReference,
																				map.StartLocation,
																				map.EndLocation,
																				map.ILInstructionOffset);
						newBookmark.IsEnabled = breakpoint.IsEnabled;

						BookmarkManager.ReplaceMark(i, newBookmark);
					}
				}
			}
		}
	}
}
