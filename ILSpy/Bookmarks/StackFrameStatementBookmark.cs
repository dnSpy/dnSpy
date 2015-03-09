
using System;
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
	public class StackFrameStatementBookmark : MarkerBookmark
	{
		public static HighlightingColor ReturnHighlightingColor = new HighlightingColor {
			Background = new SimpleHighlightingBrush(Color.FromArgb(0x62, 0xEE, 0xEF, 0xE6)),
			Foreground = new SimpleHighlightingBrush(Colors.Transparent),
		};
		public static HighlightingColor SelectedHighlightingColor = new HighlightingColor {
			Background = new SimpleHighlightingBrush(Color.FromArgb(0x68, 0xB4, 0xE4, 0xB4)),
			Foreground = new SimpleHighlightingBrush(Colors.Black),
		};
		public static HighlightingColor CurrentHighlightingColor = new HighlightingColor {
			Background = new SimpleHighlightingBrush(Colors.Yellow),
			Foreground = new SimpleHighlightingBrush(Colors.Blue),
		};

		public enum Type
		{
			/// <summary>
			/// This is the statement that will be executed next
			/// </summary>
			CurrentStatement,

			/// <summary>
			/// One of the return statements
			/// </summary>
			ReturnStatement,

			/// <summary>
			/// A selected return statement. See <see cref="SelectedFrame"/>.
			/// </summary>
			SelectedReturnStatement,
		}

		Type type;

		public StackFrameStatementBookmark(IMemberRef member, TextLocation location, TextLocation endLocation, Type type)
			: base(member, location, endLocation)
		{
			this.type = type;
		}

		public override bool CanToggle {
			get { return false; }
		}

		public override int ZOrder {
			get {
				switch (type) {
				case Type.CurrentStatement:
					return 100;
				case Type.SelectedReturnStatement:
					return 90;
				case Type.ReturnStatement:
					return 80;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		public override ImageSource Image {
			get {
				switch (type) {
				case Type.CurrentStatement:
					return Images.CurrentLine;
				case Type.SelectedReturnStatement:
					return Images.CurrentLine;//TODO: Use another similar image
				case Type.ReturnStatement:
					return null;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		public override ITextMarker CreateMarker(ITextMarkerService markerService)
		{
			ITextMarker marker = CreateMarkerInternal(markerService);
			marker.ZOrder = ZOrder;
			marker.HighlightingColor = () => {
				switch (type) {
				case Type.CurrentStatement:
					return CurrentHighlightingColor;
				case Type.SelectedReturnStatement:
					return SelectedHighlightingColor;
				case Type.ReturnStatement:
					return ReturnHighlightingColor;
				default:
					throw new InvalidOperationException();
				}
			};
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
					if (SelectedFrameChanged != null)
						SelectedFrameChanged(null, EventArgs.Empty);
				}
			}
		}
		public static event EventHandler SelectedFrameChanged;
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
				int frameNo = 0;
				foreach (var frame in DebuggerService.CurrentDebugger.GetStackFrames(100)) {
					Type type;
					if (frameNo == 0)
						type = Type.CurrentStatement;
					else
						type = selectedFrame == frameNo ? Type.SelectedReturnStatement : Type.ReturnStatement;
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
						var rs = new StackFrameStatementBookmark(methodDef, location, endLocation, type);
						returnStatementBookmarks.Add(rs);
						BookmarkManager.AddMark(rs);
					}
				}
			}
		}
		static readonly List<StackFrameStatementBookmark> returnStatementBookmarks = new List<StackFrameStatementBookmark>();
	}
}
