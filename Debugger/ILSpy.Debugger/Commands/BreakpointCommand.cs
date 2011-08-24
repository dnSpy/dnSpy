// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Linq;
using System.Windows;

using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Services;
using Mono.Cecil;

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
			if (DebugInformation.CodeMappings != null && DebugInformation.CodeMappings.Count > 0) {
				
				// check if the codemappings exists for this line
				var storage = DebugInformation.CodeMappings;
				int token = 0;
				foreach (var storageEntry in storage) {
					var instruction = storageEntry.Value.GetInstructionByLineNumber(line, out token);
					
					if (instruction == null) {
						continue;
					}
					
					// no bookmark on the line: create a new breakpoint
					DebuggerService.ToggleBreakpointAt(
						instruction.MemberMapping.MemberReference,
						line,
						token, 
						instruction.ILInstructionOffset,
						DebugInformation.Language);
					break;
				}
				
				if (token == 0) {
					MessageBox.Show(string.Format("Missing code mappings at line {0}.", line),
					                "Code mappings", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}
			}
		}
	}
	
	[ExportBookmarkContextMenuEntry(Header="Disable Breakpoint", Category="Debugger")]
	public class DisableBreakpointCommand : IBookmarkContextMenuEntry
	{
    public bool IsVisible(IBookmark[] bookmarks)
    {
      return bookmarks.Any(b => b is BreakpointBookmark && (b as BreakpointBookmark).IsEnabled);
    }
  	  
    public bool IsEnabled(IBookmark[] bookmarks)
    {
      return true;
    }
  	  
    public void Execute(IBookmark[] bookmarks)
    {
      throw new NotImplementedException();
    }
	}
}
