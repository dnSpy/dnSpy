// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Tooltips;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger.Services
{
	public enum DebuggerEvent
	{
		/// <summary>
		/// Just before attaching or starting the process
		/// </summary>
		Starting,

		/// <summary>
		/// The debugged process has started
		/// </summary>
		Started,

		/// <summary>
		/// The debugged process has stopped
		/// </summary>
		Stopped,

		/// <summary>
		/// The debugged process is being detached
		/// </summary>
		Detaching,

		/// <summary>
		/// The debugged process has paused
		/// </summary>
		Paused,

		/// <summary>
		/// The debugged process has resumed
		/// </summary>
		Resumed,

		/// <summary>
		/// A new process has been attached / started or the process has just stopped
		/// </summary>
		ProcessSelected,
	}

	public class DebuggerEventArgs : EventArgs
	{
		public DebuggerEvent DebuggerEvent { get; private set; }

		public DebuggerEventArgs(DebuggerEvent debuggerEvent)
		{
			this.DebuggerEvent = debuggerEvent;
		}
	}

	public static class DebuggerService
	{
		static IDebugger   currentDebugger;
		
		static DebuggerService()
		{
			BookmarkManager.Added   += BookmarkAdded;
			BookmarkManager.Removed += BookmarkRemoved;
		}
		
		static IDebugger GetCompatibleDebugger()
		{
			return currentDebugger;
		}
		
		/// <summary>
		/// Gets the current debugger. The debugger addin is loaded on demand; so if you
		/// just want to check a property like IsDebugging, check <see cref="IsDebuggerLoaded"/>
		/// before using this property.
		/// </summary>
		public static IDebugger CurrentDebugger {
			get {
				if (currentDebugger == null) {
					currentDebugger = GetCompatibleDebugger();
					if (currentDebugger == null)
						return null;
					currentDebugger.DebugEvent += OnDebugEvent;
				}
				return currentDebugger;
			}
		}
		
		static bool debuggerStarted;
		
		/// <summary>
		/// Gets whether the debugger is currently active.
		/// </summary>
		public static bool IsDebuggerStarted {
			get { return debuggerStarted; }
		}

		public static event EventHandler<DebuggerEventArgs> DebugEvent;

		static void OnDebugEvent(object sender, DebuggerEventArgs e)
		{
			switch (e.DebuggerEvent) {
			case DebuggerEvent.Starting:
				ClearDebugMessages();
				break;

			case DebuggerEvent.Started:
				debuggerStarted = true;
				break;

			case DebuggerEvent.Stopped:
				debuggerStarted = false;
				StackFrameStatementManager.UpdateReturnStatementBookmarks(true);
				break;
			}

			if (DebugEvent != null)
				DebugEvent(sender, e);
		}
		
		public static void ClearDebugMessages()
		{
		}

		public static void PrintDebugMessage(string msg)
		{
		}

		public static event EventHandler<BreakpointBookmarkEventArgs> BreakPointChanged;
		public static event EventHandler<BreakpointBookmarkEventArgs> BreakPointAdded;
		public static event EventHandler<BreakpointBookmarkEventArgs> BreakPointRemoved;
		
		static void OnBreakPointChanged(BreakpointBookmarkEventArgs e)
		{
			if (BreakPointChanged != null) {
				BreakPointChanged(null, e);
			}
		}
		
		static void OnBreakPointAdded(BreakpointBookmarkEventArgs e)
		{
			if (BreakPointAdded != null) {
				BreakPointAdded(null, e);
			}
		}
		
		static void OnBreakPointRemoved(BreakpointBookmarkEventArgs e)
		{
			if (BreakPointRemoved != null) {
				BreakPointRemoved(null, e);
			}
		}
		
		public static IList<BreakpointBookmark> Breakpoints {
			get {
				List<BreakpointBookmark> breakpoints = new List<BreakpointBookmark>();
				foreach (var bookmark in BookmarkManager.Bookmarks) {
					BreakpointBookmark breakpoint = bookmark as BreakpointBookmark;
					if (breakpoint != null) {
						breakpoints.Add(breakpoint);
					}
				}
				return breakpoints.AsReadOnly();
			}
		}
		
		static void BookmarkAdded(object sender, BookmarkEventArgs e)
		{
			BreakpointBookmark bb = e.Bookmark as BreakpointBookmark;
			if (bb != null) {
				OnBreakPointAdded(new BreakpointBookmarkEventArgs(bb));
			}
		}
		
		static void BookmarkRemoved(object sender, BookmarkEventArgs e)
		{
			BreakpointBookmark bb = e.Bookmark as BreakpointBookmark;
			if (bb != null) {
				OnBreakPointRemoved(new BreakpointBookmarkEventArgs(bb));
			}
		}
		
		static void BookmarkChanged(object sender, EventArgs e)
		{
			BreakpointBookmark bb = sender as BreakpointBookmark;
			if (bb != null) {
				OnBreakPointChanged(new BreakpointBookmarkEventArgs(bb));
			}
		}
		
		#region Tool tips
		/// <summary>
		/// Gets debugger tooltip information for the specified position.
		/// A descriptive string for the element or a DebuggerTooltipControl
		/// showing its current value (when in debugging mode) can be returned
		/// through the ToolTipRequestEventArgs.SetTooltip() method.
		/// </summary>
		public static void HandleToolTipRequest(ToolTipRequestEventArgs e)
		{
			if (!e.InDocument)
				return;
			
			var logicPos = e.LogicalPosition;
			var doc = (TextDocument)e.Editor.Document;
			var line = doc.GetLineByNumber(logicPos.Line);
			
			if (line.Offset + logicPos.Column >= doc.TextLength)
				return;
			
			var c = doc.GetText(line.Offset + logicPos.Column, 1);			
			if (string.IsNullOrEmpty(c) || c == "\n" || c == "\t")
				return;
			
			string variable =
				ParserService.SimpleParseAt(doc.Text, doc.GetOffset(new TextLocation(logicPos.Line, logicPos.Column)));
			
			if (currentDebugger == null || !currentDebugger.IsDebugging || !currentDebugger.CanEvaluate) {
				e.ContentToShow = null;
			}
			else {
				try {
					e.ContentToShow = currentDebugger.GetTooltipControl(e.LogicalPosition, variable);
				} catch {
					return;
				}
			}
		}
		#endregion
		
		public static void SetDebugger(Lazy<IDebugger> debugger)
		{
			if (currentDebugger != null)
				currentDebugger.DebugEvent -= OnDebugEvent;
			currentDebugger = debugger.Value;
			if (currentDebugger != null)
				currentDebugger.DebugEvent += OnDebugEvent;
		}
	}
}
