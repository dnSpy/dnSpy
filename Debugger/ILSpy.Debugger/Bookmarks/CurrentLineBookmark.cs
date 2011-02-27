// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Windows.Media;
using ICSharpCode.NRefactory.CSharp;
using ILSpy.Debugger.AvalonEdit;
using ILSpy.Debugger.Services;
using Mono.CSharp;

namespace ILSpy.Debugger.Bookmarks
{
	public class CurrentLineBookmark : MarkerBookmark
	{
		static CurrentLineBookmark instance;
		
		public static CurrentLineBookmark Instance {
			get { return instance; }
		}
		
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
			
			instance = new CurrentLineBookmark(typeName, new AstLocation(startLine, startColumn));
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
		
		public CurrentLineBookmark(string typeName, AstLocation location) : base(typeName, location)
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
		
		public override ITextMarker CreateMarker(ITextMarkerService markerService, int offset, int length)
		{
			ITextMarker marker = markerService.Create(offset + startColumn - 1, length);
			marker.BackgroundColor = Colors.Yellow;
			marker.ForegroundColor = Colors.Blue;
			return marker;
		}
	}
}
