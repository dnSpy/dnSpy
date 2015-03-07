
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.ILSpy.SharpDevelop;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks
{
	public class ReturnStatementBookmark : MarkerBookmark
	{
		public static HighlightingColor HighlightingColor = new HighlightingColor {
			Background = new SimpleHighlightingBrush(Color.FromArgb(0x68, 0xB4, 0xE4, 0xB4)),
			Foreground = new SimpleHighlightingBrush(Colors.Black),
		};

		public ReturnStatementBookmark(IMemberRef member, TextLocation location, TextLocation endLocation)
			: base(member, location, endLocation)
		{
		}

		public override bool CanToggle {
			get { return false; }
		}

		public override int ZOrder {
			get { return 80; }
		}

		public override ImageSource Image {
			get { return Images.CurrentLine; }//TODO: Use another similar image
		}

		public override ITextMarker CreateMarker(ITextMarkerService markerService)
		{
			ITextMarker marker = CreateMarkerInternal(markerService);
			marker.ZOrder = ZOrder;
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

		public static void Remove()
		{
			foreach (var rs in returnStatementBookmarks)
				BookmarkManager.RemoveMark(rs);
			returnStatementBookmarks.Clear();
		}

		/// <summary>
		/// Should be called each time the IL offset has been updated
		/// </summary>
		public static void UpdateReturnStatementBookmarks()
		{
			Remove();
			bool updateReturnStatements =
				DebugInformation.CodeMappings != null &&
				DebuggerService.CurrentDebugger != null &&
				DebuggerService.CurrentDebugger.IsDebugging &&
				!DebuggerService.CurrentDebugger.IsProcessRunning;
			if (updateReturnStatements) {
				var cm = DebugInformation.CodeMappings;
				// The first frame is the current frame. Ignore it.
				foreach (var frame in DebuggerService.CurrentDebugger.GetStackFrames(100).Skip(1)) {
					if (frame.ILOffset == null)
						continue;
					var key = frame.MethodKey;
					int offset = frame.ILOffset.Value;
					MethodDef methodDef;
					ICSharpCode.NRefactory.TextLocation location, endLocation;
					if (cm != null && cm.ContainsKey(key) &&
						cm[key].GetInstructionByTokenAndOffset((uint)offset, out methodDef, out location, out endLocation)) {
						var rs = new ReturnStatementBookmark(methodDef, location, endLocation);
						returnStatementBookmarks.Add(rs);
						BookmarkManager.AddMark(rs);
					}
				}
			}
		}
		static readonly List<ReturnStatementBookmark> returnStatementBookmarks = new List<ReturnStatementBookmark>();
	}
}
