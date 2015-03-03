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
			if (line <= 0)
				return;
			var cm = DebugInformation.CodeMappings;
			if (cm != null && cm.Count > 0) {
				int token = 0;
				foreach (var storageEntry in cm.Values) {
					var bp = storageEntry.GetInstructionByLineNumber(line, out token);
					if (bp != null) {
						DebuggerService.ToggleBreakpointAt(bp);
						break;
					}
				}
				if (token == 0) {
					var bp = GetClosest(cm, line, out token);
					if (bp != null)
						DebuggerService.ToggleBreakpointAt(bp);
				}
			}
		}

		SourceCodeMapping GetClosest(Dictionary<MethodKey, MemberMapping> cm, int line, out int token)
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
				if (map != null && closest == null || map.StartLocation < closest.StartLocation)
					closest = map;
			}

			token = closest == null ? 0 : closest.MemberMapping.MethodDefinition.MDToken.ToInt32();
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
