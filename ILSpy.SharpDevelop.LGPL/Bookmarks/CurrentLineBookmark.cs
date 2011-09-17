// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Media;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.SharpDevelop;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks
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
		
		public static void SetPosition(MemberReference memberReference, int makerStartLine, int makerStartColumn, int makerEndLine, int makerEndColumn, int ilOffset)
		{
			Remove();
			
			startLine   = makerStartLine;
			startColumn = makerStartColumn;
			endLine     = makerEndLine;
			endColumn   = makerEndColumn;
			
			instance = new CurrentLineBookmark(memberReference, new TextLocation(startLine, startColumn), ilOffset);
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
		
		private CurrentLineBookmark(MemberReference member, TextLocation location, int ilOffset) : base(member, location)
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
			ITextMarker marker = markerService.Create(offset + startColumn - 1, length + 1);
			marker.BackgroundColor = Colors.Yellow;
			marker.ForegroundColor = Colors.Blue;
			marker.IsVisible = b => b is MarkerBookmark && DebugInformation.DecompiledMemberReferences != null &&
				DebugInformation.DecompiledMemberReferences.ContainsKey(((MarkerBookmark)b).MemberReference.MetadataToken.ToInt32());
			marker.Bookmark = this;
			this.Marker = marker;
			return marker;
		}
	}
}
