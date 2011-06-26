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
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.AvalonEdit
{
	public class IconBarMargin : AbstractMargin, IDisposable
	{
		readonly IconBarManager manager;
		
		public IconBarMargin(IconBarManager manager)
		{
			BookmarkManager.Added += delegate { InvalidateVisual(); };
			BookmarkManager.Removed += delegate { InvalidateVisual(); };
			
			this.manager = manager;
		}
		
		public IconBarManager Manager {
			get { return manager; }
		}
		
		public IList<MemberReference> DecompiledMembers { get; set; }
		
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
			
			ICSharpCode.AvalonEdit.Rendering.TextView textView = this.TextView;
			if (textView != null && textView.VisualLinesValid) {
				// create a dictionary line number => first bookmark
				Dictionary<int, IBookmark> bookmarkDict = new Dictionary<int, IBookmark>();
				foreach (var bm in BookmarkManager.Bookmarks) {
					if (DebugInformation.DecompiledMemberReferences == null || DebugInformation.DecompiledMemberReferences.Count == 0 ||
					    !DebugInformation.DecompiledMemberReferences.ContainsKey(bm.MemberReference.MetadataToken.ToInt32()))
						continue;
					
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
				
				Size pixelSize = PixelSnapHelpers.GetPixelSize(this);
				foreach (VisualLine line in textView.VisualLines) {
					int lineNumber = line.FirstDocumentLine.LineNumber;
					IBookmark bm;
					if (bookmarkDict.TryGetValue(lineNumber, out bm)) {
						Rect rect = new Rect(0, PixelSnapHelpers.Round(line.VisualTop - textView.VerticalOffset, pixelSize.Height), 16, 16);
						if (dragDropBookmark == bm && dragStarted)
							drawingContext.PushOpacity(0.5);
						drawingContext.DrawImage(bm.Image, rect);
						if (dragDropBookmark == bm && dragStarted)
							drawingContext.Pop();
					}
				}
				if (dragDropBookmark != null && dragStarted) {
					Rect rect = new Rect(0, PixelSnapHelpers.Round(dragDropCurrentPoint - 8, pixelSize.Height), 16, 16);
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
				if (bm.LineNumber == line &&
				    this.DecompiledMembers != null && this.DecompiledMembers.Contains(bm.MemberReference)) {
					if (result == null || bm.ZOrder > result.ZOrder)
						return result;
				}
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
		
		public void SyncBookmarks()
		{
			var storage = DebugInformation.CodeMappings;
			if (storage == null || storage.Count == 0)
				return;
			
			//remove existing bookmarks and create new ones
			List<BreakpointBookmark> newBookmarks = new List<BreakpointBookmark>();
			for (int i = BookmarkManager.Bookmarks.Count - 1; i >= 0; --i) {
				var breakpoint = BookmarkManager.Bookmarks[i] as BreakpointBookmark;
				if (breakpoint == null)
					continue;
				
				var key = breakpoint.MemberReference.MetadataToken.ToInt32();
				if (!storage.ContainsKey(key))
					continue;
				
				var member = DebugInformation.DecompiledMemberReferences[key];
				
				bool isMatch;
				SourceCodeMapping map = storage[key].GetInstructionByTokenAndOffset(
					member.MetadataToken.ToInt32(), breakpoint.ILRange.From, out isMatch);
				
				if (map != null) {
					newBookmarks.Add(new BreakpointBookmark(
						member, new AstLocation(map.SourceCodeLine, 0),
						map.ILInstructionOffset, BreakpointAction.Break, DebugInformation.Language));
					
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
			
			var oldMappings = DebugInformation.OldCodeMappings;
			var newMappings = DebugInformation.CodeMappings;

			if (oldMappings == null || newMappings == null)
				return;
			
			// 1. Save it's data
			int line = CurrentLineBookmark.Instance.LineNumber;
			var markerType = CurrentLineBookmark.Instance.MemberReference;
			
			if (!oldMappings.ContainsKey(markerType.MetadataToken.ToInt32()) || !newMappings.ContainsKey(markerType.MetadataToken.ToInt32()))
				return;
			
			// 2. Remove it
			CurrentLineBookmark.Remove();
			
			// 3. map the marker line
			int token;
			var instruction = oldMappings[markerType.MetadataToken.ToInt32()].GetInstructionByLineNumber(line, out token);
			if (instruction == null)
				return;

			MemberReference memberReference;
			int newline;
			if (newMappings[markerType.MetadataToken.ToInt32()].GetInstructionByTokenAndOffset(token, instruction.ILInstructionOffset.From, out memberReference, out newline)) {
				// 4. create breakpoint for new languages
				CurrentLineBookmark.SetPosition(memberReference, newline, 0, newline, 0);
			}
		}
	}
}
