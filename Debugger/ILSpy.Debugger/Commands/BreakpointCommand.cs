// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Windows;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Debugger.Services;

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
			if (DebugData.CodeMappings != null && DebugData.CodeMappings.Count > 0) {
				
				// check if the codemappings exists for this line
				var storage = DebugData.CodeMappings;
				int token = 0;
				foreach (var key in storage.Keys) {
					var instruction = storage[key].GetInstructionByLineNumber(line, out token);
					
					if (instruction == null) {
						continue;
					}
					
					// no bookmark on the line: create a new breakpoint
					DebuggerService.ToggleBreakpointAt(
						DebugData.DecompiledMemberReferences[key],
						line,
						instruction.ILInstructionOffset,
						DebugData.Language);
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
}
