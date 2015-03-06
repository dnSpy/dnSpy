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
	[ExportBookmarkActionEntry(Icon = "images/Breakpoint.png", Category="Debugger")]
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
	
	/*
	[ExportBookmarkContextMenuEntry(Header="Disable Breakpoint", Category="Debugger")]
	public class DisableBreakpointCommand : IBookmarkContextMenuEntry
	{
    public bool IsVisible(IBookmark bookmark)
    {
      return b => b is BreakpointBookmark && (b as BreakpointBookmark).IsEnabled;
    }
  	  
    public bool IsEnabled(IBookmark[] bookmarks)
    {
      return true;
    }
  	  
    public void Execute(IBookmark[] bookmarks)
    {
      throw new NotImplementedException();
    }
	}*/
}
