// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Media;
using ICSharpCode.NRefactory;
using ILSpy.Debugger.Services;

namespace ILSpy.Debugger.Bookmarks
{
	public class CurrentLineBookmark : BookmarkBase
	{
		static CurrentLineBookmark instance;
		
		static int startLine;
		static int startColumn;
		static int endLine;
		static int endColumn;
		
		public static void SetPosition(string typeName, int makerStartLine, int makerStartColumn, int makerEndLine, int makerEndColumn)
		{
			Remove();
			
			startLine   = makerStartLine;
			startColumn = makerStartColumn;
			endLine     = makerEndLine;
			endColumn   = makerEndColumn;
			
			instance = new CurrentLineBookmark(typeName, new Location(startColumn, startLine));
			BookmarkManager.AddMark(instance);
		}
		
		public static void Remove()
		{
			if (instance != null) {
				BookmarkManager.RemoveMark(instance);
				instance = null;
			}
		}
		
		public override bool CanToggle {
			get { return false; }
		}
		
		public override int ZOrder {
			get { return 100; }
		}
		
		public CurrentLineBookmark(string typeName, Location location) : base(typeName, location)
		{
			
		}
		
		public override ImageSource Image {
			get { return ImageService.CurrentLine; }
		}
		
		public override bool CanDragDrop {
			get { return false; }
		}
		
		public override void Drop(int lineNumber)
		{
			// call async because the Debugger seems to use Application.DoEvents(), but we don't want to process events
			// because Drag'N'Drop operation has finished
//			WorkbenchSingleton.SafeThreadAsyncCall(
//				delegate {
//					DebuggerService.CurrentDebugger.SetInstructionPointer(this.FileName, lineNumber, 1);
//				});
		}
	}
}
