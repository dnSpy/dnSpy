// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Services;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger.Commands
{
	[ExportBookmarkActionEntry(Icon = "images/Breakpoint.png", Category="Debug")]
	public class BreakpointCommand : IBookmarkActionEntry
	{
		public bool IsEnabled()
		{
			return true;
		}
		
		public void Execute(int line)
		{
			BreakpointHelper.Toggle(line, 0);
		}
	}

	static class BreakpointHelper
	{
		public static BreakpointBookmark GetBreakpointBookmark(int line, int column)
		{
			return GetBreakpointBookmark(Find(line, column));
		}

		public static BreakpointBookmark GetBreakpointBookmark(SourceCodeMapping mapping)
		{
			if (mapping == null)
				return null;
			foreach (var bm in BookmarkManager.Bookmarks) {
				var bpm = bm as BreakpointBookmark;
				if (bpm == null)
					continue;
				if (bpm.Location != mapping.StartLocation || bpm.EndLocation != mapping.EndLocation)
					continue;

				return bpm;
			}

			return null;
		}

		public static void Toggle(int line, int column)
		{
			var bp = Find(line, column);
			if (bp != null) {
				DebuggerService.ToggleBreakpointAt(bp);
				MainWindow.Instance.TextView.ScrollAndMoveCaretTo(bp.StartLocation.Line, bp.StartLocation.Column);
			}
		}

		public static SourceCodeMapping Find(int line, int column)
		{
			return Find(DebugInformation.CodeMappings, line, column);
		}

		public static SourceCodeMapping Find(Dictionary<MethodKey, MemberMapping> cm, int line, int column)
		{
			if (line <= 0)
				return null;
			if (cm == null || cm.Count == 0)
				return null;

			var bp = FindByLineColumn(cm, line, column);
			if (bp == null && column != 0)
				bp = FindByLineColumn(cm, line, 0);
			if (bp == null)
				bp = GetClosest(cm, line);

			return bp;
		}

		static SourceCodeMapping FindByLineColumn(Dictionary<MethodKey, MemberMapping> cm, int line, int column)
		{
			foreach (var storageEntry in cm.Values) {
				var bp = storageEntry.GetInstructionByLineNumber(line, column);
				if (bp != null)
					return bp;
			}
			return null;
		}

		static SourceCodeMapping GetClosest(Dictionary<MethodKey, MemberMapping> cm, int line)
		{
			SourceCodeMapping closest = null;
			foreach (var entry in cm.Values) {
				SourceCodeMapping map = null;
				foreach (var m in entry.MemberCodeMappings) {
					if (line > m.EndLocation.Line)
						continue;
					if (map == null || m.StartLocation < map.StartLocation)
						map = m;
				}
				if (map != null && (closest == null || map.StartLocation < closest.StartLocation))
					closest = map;
			}

			return closest;
		}
	}

	[ExportBookmarkContextMenuEntry(Category = "Debug", Icon = "images/DisabledBreakpoint.png")]
	public class EnableAndDisableBreakpointCommand : IBookmarkContextMenuEntry
	{
		public bool IsVisible(IBookmark bookmark)
		{
			return bookmark is BreakpointBookmark;
		}

		public bool IsEnabled(IBookmark bookmark)
		{
			return IsVisible(bookmark);
		}

		public void Execute(IBookmark bookmark)
		{
			var location = MainWindow.Instance.TextView.TextEditor.TextArea.Caret.Location;
			var bpm = BreakpointHelper.GetBreakpointBookmark(location.Line, location.Column);
			if (bpm != null)
				bpm.IsEnabled = !bpm.IsEnabled;
		}

		public string GetHeader(IBookmark bookmark)
		{
			return ((BreakpointBookmark)bookmark).IsEnabled ? "Disable _Breakpoint" : "Enable _Breakpoint";
		}
	}
}
