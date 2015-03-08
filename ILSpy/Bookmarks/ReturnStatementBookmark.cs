
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks
{
	public class ReturnStatementBookmark : MarkerBookmark
	{
		public static HighlightingColor HighlightingColor = new HighlightingColor {
			Background = new SimpleHighlightingBrush(Color.FromArgb(0x62, 0xEE, 0xEF, 0xE6)),
			Foreground = new SimpleHighlightingBrush(Colors.Transparent),
		};
		public static HighlightingColor SelectedHighlightingColor = new HighlightingColor {
			Background = new SimpleHighlightingBrush(Color.FromArgb(0x68, 0xB4, 0xE4, 0xB4)),
			Foreground = new SimpleHighlightingBrush(Colors.Black),
		};

		bool isSelected;

		public ReturnStatementBookmark(IMemberRef member, TextLocation location, TextLocation endLocation, bool isSelected)
			: base(member, location, endLocation)
		{
			this.isSelected = isSelected;
		}

		public override bool CanToggle {
			get { return false; }
		}

		public override int ZOrder {
			get { return isSelected ? 80 : 70; }
		}

		public override ImageSource Image {
			get { return isSelected ? Images.CurrentLine : null; }//TODO: Use another similar image
		}

		public override ITextMarker CreateMarker(ITextMarkerService markerService)
		{
			ITextMarker marker = CreateMarkerInternal(markerService);
			marker.ZOrder = ZOrder;
			marker.HighlightingColor = () => isSelected ? SelectedHighlightingColor : HighlightingColor;
			marker.IsVisible = b => {
				var cm = DebugInformation.CodeMappings;
				return cm != null && b is MarkerBookmark &&
					cm.ContainsKey(new MethodKey(((MarkerBookmark)b).MemberReference));
			};
			marker.Bookmark = this;
			this.Marker = marker;
			return marker;
		}

		/// <summary>
		/// Gets/sets the selected frame number. 0 is the current frame.
		/// </summary>
		public static int SelectedFrame {
			get { return selectedFrame; }
			set {
				if (value != selectedFrame) {
					selectedFrame = value;
					UpdateReturnStatementBookmarks(false);
				}
			}
		}
		static int selectedFrame = 0;

		public static void Remove(bool removeSelected)
		{
			if (removeSelected)
				selectedFrame = 0;
			foreach (var rs in returnStatementBookmarks)
				BookmarkManager.RemoveMark(rs);
			returnStatementBookmarks.Clear();
		}

		/// <summary>
		/// Should be called each time the IL offset has been updated
		/// </summary>
		public static void UpdateReturnStatementBookmarks(bool removeSelected)
		{
			Remove(removeSelected);
			bool updateReturnStatements =
				DebugInformation.CodeMappings != null &&
				DebuggerService.CurrentDebugger != null &&
				DebuggerService.CurrentDebugger.IsDebugging &&
				!DebuggerService.CurrentDebugger.IsProcessRunning;
			if (updateReturnStatements) {
				var cm = DebugInformation.CodeMappings;
				// The first frame is the current frame. Ignore it.
				int frameNo = 1;
				foreach (var frame in DebuggerService.CurrentDebugger.GetStackFrames(100).Skip(frameNo)) {
					bool isSelected = selectedFrame == frameNo;
					frameNo++;
					if (frame.ILOffset == null)
						continue;
					var key = frame.MethodKey;
					int offset = frame.ILOffset.Value;
					MethodDef methodDef;
					ICSharpCode.NRefactory.TextLocation location, endLocation;
					if (cm != null && cm.ContainsKey(key) &&
						cm[key].GetInstructionByTokenAndOffset((uint)offset, out methodDef, out location, out endLocation)) {
						var rs = new ReturnStatementBookmark(methodDef, location, endLocation, isSelected);
						returnStatementBookmarks.Add(rs);
						BookmarkManager.AddMark(rs);
					}
				}
			}
		}
		static readonly List<ReturnStatementBookmark> returnStatementBookmarks = new List<ReturnStatementBookmark>();
	}
}
