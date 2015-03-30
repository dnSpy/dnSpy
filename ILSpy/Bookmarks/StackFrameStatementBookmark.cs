
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks
{
	public enum StackFrameStatementType
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

	public class StackFrameStatementManager
	{
		static StackFrameStatementManager()
		{
			MainWindow.Instance.ExecuteWhenLoaded(() => {
				MainWindow.Instance.OnDecompilerTextViewChanged += (sender, e) => OnDecompilerTextViewChanged(e.OldView, e.NewView);
				foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
					OnDecompilerTextViewChanged(null, textView);
			});
		}

		static void OnDecompilerTextViewChanged(DecompilerTextView oldView, DecompilerTextView newView)
		{
			if (oldView != null) {
				oldView.OnBeforeShowOutput -= DecompilerTextView_OnBeforeShowOutput;
				oldView.OnShowOutput -= DecompilerTextView_OnShowOutput;
			}
			if (newView != null) {
				newView.OnBeforeShowOutput += DecompilerTextView_OnBeforeShowOutput;
				newView.OnShowOutput += DecompilerTextView_OnShowOutput;
			}

			Remove(oldView);
			UpdateReturnStatementBookmarks(newView);
		}

		static void DecompilerTextView_OnBeforeShowOutput(object sender, TextView.DecompilerTextView.ShowOutputEventArgs e)
		{
			Remove((DecompilerTextView)sender);
		}

		static void DecompilerTextView_OnShowOutput(object sender, TextView.DecompilerTextView.ShowOutputEventArgs e)
		{
			e.HasMovedCaret |= UpdateReturnStatementBookmarks((DecompilerTextView)sender, !e.HasMovedCaret);
		}

		/// <summary>
		/// Gets/sets the selected frame number. 0 is the current frame.
		/// </summary>
		public static int SelectedFrame {
			get { return selectedFrame; }
			set {
				if (value != selectedFrame) {
					selectedFrame = value;
					foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
						UpdateReturnStatementBookmarks(textView);
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
				SelectedFrame = 0;
			foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
				Remove(textView);
		}

		static void Remove(DecompilerTextView decompilerTextView)
		{
			for (int i = returnStatementBookmarks.Count - 1; i >= 0; i--) {
				if (returnStatementBookmarks[i].decompilerTextView == decompilerTextView) {
					BookmarkManager.RemoveMark(returnStatementBookmarks[i]);
					returnStatementBookmarks.RemoveAt(i);
				}
			}
		}

		public static bool UpdateReturnStatementBookmarks(bool removeSelected, bool moveCaret = false)
		{
			bool result = false;
			if (removeSelected)
				SelectedFrame = 0;
			foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
				result |= UpdateReturnStatementBookmarks(textView, moveCaret);
			return result;
		}

		/// <summary>
		/// Should be called each time the IL offset has been updated
		/// </summary>
		static bool UpdateReturnStatementBookmarks(DecompilerTextView decompilerTextView, bool moveCaret = false)
		{
			Remove(decompilerTextView);
			bool movedCaret = false;
			var cm = decompilerTextView == null ? null : decompilerTextView.CodeMappings;
			bool updateReturnStatements =
				cm != null &&
				DebuggerService.CurrentDebugger != null &&
				DebuggerService.CurrentDebugger.IsDebugging &&
				!DebuggerService.CurrentDebugger.IsProcessRunning;
			if (updateReturnStatements) {
				int frameNo = 0;
				foreach (var frame in DebuggerService.CurrentDebugger.GetStackFrames(100)) {
					StackFrameStatementType type;
					if (frameNo == 0)
						type = StackFrameStatementType.CurrentStatement;
					else
						type = selectedFrame == frameNo ? StackFrameStatementType.SelectedReturnStatement : StackFrameStatementType.ReturnStatement;
					if (frame.ILOffset == null)
						continue;
					var key = frame.MethodKey;
					int offset = frame.ILOffset.Value;
					MethodDef methodDef;
					ICSharpCode.NRefactory.TextLocation location, endLocation;
					if (cm != null && cm.ContainsKey(key) &&
						cm[key].GetInstructionByTokenAndOffset((uint)offset, out methodDef, out location, out endLocation)) {
						var rs = new StackFrameStatementBookmark(decompilerTextView, methodDef, location, endLocation, type, (uint)offset);
						returnStatementBookmarks.Add(rs);
						BookmarkManager.AddMark(rs);

						if (moveCaret && frameNo == selectedFrame) {
							decompilerTextView.ScrollAndMoveCaretTo(location.Line, location.Column);
							movedCaret = true;
						}
					}
					frameNo++;
				}
			}
			return movedCaret;
		}
		static readonly List<StackFrameStatementBookmark> returnStatementBookmarks = new List<StackFrameStatementBookmark>();
	}

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

		readonly StackFrameStatementType type;
		internal readonly DecompilerTextView decompilerTextView;

		public StackFrameStatementBookmark(DecompilerTextView decompilerTextView, IMemberRef member, TextLocation location, TextLocation endLocation, StackFrameStatementType type, uint ilOffset)
			: base(member, ilOffset, location, endLocation)
		{
			this.decompilerTextView = decompilerTextView;
			this.type = type;
		}

		public override bool CanToggle {
			get { return false; }
		}

		public override int ZOrder {
			get {
				switch (type) {
				case StackFrameStatementType.CurrentStatement:
					return 100;
				case StackFrameStatementType.SelectedReturnStatement:
					return 90;
				case StackFrameStatementType.ReturnStatement:
					return 80;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		public override ImageSource Image {
			get {
				switch (type) {
				case StackFrameStatementType.CurrentStatement:
					return Images.CurrentLine;
				case StackFrameStatementType.SelectedReturnStatement:
					return Images.SelectedReturnLine;
				case StackFrameStatementType.ReturnStatement:
					return null;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		public override bool IsVisible(DecompilerTextView textView)
		{
			return decompilerTextView == textView;
		}

		public override ITextMarker CreateMarker(ITextMarkerService markerService, DecompilerTextView textView)
		{
			ITextMarker marker = CreateMarkerInternal(markerService, textView);
			var cm = textView == null ? null : textView.CodeMappings;
			marker.ZOrder = ZOrder;
			marker.HighlightingColor = () => {
				switch (type) {
				case StackFrameStatementType.CurrentStatement:
					return CurrentHighlightingColor;
				case StackFrameStatementType.SelectedReturnStatement:
					return SelectedHighlightingColor;
				case StackFrameStatementType.ReturnStatement:
					return ReturnHighlightingColor;
				default:
					throw new InvalidOperationException();
				}
			};
			marker.IsVisible = b => {
				return cm != null && b is MarkerBookmark &&
					cm.ContainsKey(new MethodKey(((MarkerBookmark)b).MemberReference));
			};
			marker.Bookmark = this;
			return marker;
		}
	}
}
