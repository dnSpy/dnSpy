// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.SharpDevelop;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks
{
	public class CurrentLineBookmark : MarkerBookmark
	{
		public static HighlightingColor HighlightingColor = new HighlightingColor {
			Background = new SimpleHighlightingBrush(Colors.Yellow),
			Foreground = new SimpleHighlightingBrush(Colors.Blue),
		};
		static CurrentLineBookmark instance;
		
		public static CurrentLineBookmark Instance {
			get { return instance; }
		}
		
		static int endColumn;
		
		public static void SetPosition(IMemberRef memberReference, int makerStartLine, int makerStartColumn, int makerEndLine, int makerEndColumn, int ilOffset)
		{
			Remove();
			
			instance = new CurrentLineBookmark(memberReference, new TextLocation(makerStartLine, makerStartColumn), new TextLocation(makerEndLine, makerEndColumn), ilOffset);
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
		
		private CurrentLineBookmark(IMemberRef member, TextLocation location, TextLocation endLocation, int ilOffset) : base(member, location, endLocation)
		{
			this.ILOffset = ilOffset;
		}
		
		public int ILOffset { get; private set; }
		
		public override ImageSource Image {
			get { return Images.CurrentLine; }
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
			ITextMarker marker = CreateMarkerInternal(markerService, offset - 1, length + 1);
			marker.HighlightingColor = () => HighlightingColor;
			marker.IsVisible = b => {
				var cm = DebugInformation.CodeMappings;
				return cm != null && b is MarkerBookmark &&
					cm.ContainsKey(new MethodKey(((MarkerBookmark)b).MemberReference));
			};
			marker.Bookmark = this;
			this.Marker = marker;
			return marker;
		}
	}
}
