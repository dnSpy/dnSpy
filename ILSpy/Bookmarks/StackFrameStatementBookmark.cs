/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using dnlib.DotNet;
using dnSpy.Bookmarks;
using dnSpy.Images;
using dnSpy.Tabs;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace dnSpy.Debugger.Bookmarks {
	public enum StackFrameStatementType {
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

	[Export(typeof(IPlugin))]
	public class StackFrameStatementManager : IPlugin {
		StackFrameStatementManager() {
		}

		public void OnLoaded() {
			// Do nothing, we just want to load the cctor
		}

		static StackFrameStatementManager() {
			DebuggerService.DebugEvent += OnDebugEvent;
			MainWindow.Instance.ExecuteWhenLoaded(() => {
				MainWindow.Instance.OnTabStateChanged += (sender, e) => OnTabStateChanged(e.OldTabState, e.NewTabState);
				foreach (var tabState in MainWindow.Instance.AllVisibleDecompileTabStates)
					OnTabStateChanged(null, tabState);
			});
		}

		static void OnDebugEvent(object sender, DebuggerEventArgs e) {
			switch (e.DebuggerEvent) {
			case DebuggerEvent.Resumed:
				SelectedFrame = 0;
				foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
					Remove(textView);
				break;

			case DebuggerEvent.Stopped:
			case DebuggerEvent.Paused:
				SelectedFrame = 0;
				foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
					UpdateReturnStatementBookmarks(textView, false);
				break;
			}
		}

		static void OnTabStateChanged(TabState oldTabState, TabState newTabState) {
			var oldTsd = oldTabState as DecompileTabState;
			if (oldTsd != null) {
				oldTsd.TextView.OnBeforeShowOutput -= DecompilerTextView_OnBeforeShowOutput;
				oldTsd.TextView.OnShowOutput -= DecompilerTextView_OnShowOutput;
			}
			var newTsd = newTabState as DecompileTabState;
			if (newTsd != null) {
				newTsd.TextView.OnBeforeShowOutput += DecompilerTextView_OnBeforeShowOutput;
				newTsd.TextView.OnShowOutput += DecompilerTextView_OnShowOutput;
			}

			if (oldTsd != null)
				Remove(oldTsd.TextView);
			if (newTsd != null)
				UpdateReturnStatementBookmarks(newTsd.TextView);
		}

		static void DecompilerTextView_OnBeforeShowOutput(object sender, DecompilerTextView.ShowOutputEventArgs e) {
			Remove((DecompilerTextView)sender);
		}

		static void DecompilerTextView_OnShowOutput(object sender, DecompilerTextView.ShowOutputEventArgs e) {
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

		static void Remove(DecompilerTextView decompilerTextView) {
			for (int i = returnStatementBookmarks.Count - 1; i >= 0; i--) {
				if (returnStatementBookmarks[i].decompilerTextView == decompilerTextView) {
					BookmarkManager.RemoveMark(returnStatementBookmarks[i]);
					returnStatementBookmarks.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Should be called each time the IL offset has been updated
		/// </summary>
		static bool UpdateReturnStatementBookmarks(DecompilerTextView decompilerTextView, bool moveCaret = false) {
			Remove(decompilerTextView);
			bool movedCaret = false;
			var cm = decompilerTextView == null ? null : decompilerTextView.CodeMappings;
			bool updateReturnStatements =
				cm != null &&
				DebuggerService.CurrentDebugger != null &&
				DebuggerService.CurrentDebugger.IsDebugging &&
				!DebuggerService.CurrentDebugger.IsProcessRunning;
			if (updateReturnStatements) {
				int frameNo = -1;
				foreach (var frame in DebuggerService.CurrentDebugger.GetStackFrames(100)) {
					frameNo++;
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
				}
			}
			return movedCaret;
		}
		static readonly List<StackFrameStatementBookmark> returnStatementBookmarks = new List<StackFrameStatementBookmark>();
	}

	public class StackFrameStatementBookmark : MarkerBookmark {
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
			: base(member, ilOffset, location, endLocation) {
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
					return (int)TextMarkerZOrder.CurrentStatement;
				case StackFrameStatementType.SelectedReturnStatement:
					return (int)TextMarkerZOrder.SelectedReturnStatement;
				case StackFrameStatementType.ReturnStatement:
					return (int)TextMarkerZOrder.ReturnStatement;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		public override bool HasImage {
			get { return GetImageName() != null; }
		}

		public override ImageSource GetImage(Color bgColor) {
			var name = GetImageName();
			if (name != null)
				return ImageCache.Instance.GetImage(name, bgColor);
			return null;
		}

		string GetImageName() {
			switch (type) {
			case StackFrameStatementType.CurrentStatement:
				return "CurrentLine";
			case StackFrameStatementType.SelectedReturnStatement:
				return "SelectedReturnLine";
			case StackFrameStatementType.ReturnStatement:
				return null;
			default:
				throw new InvalidOperationException();
			}
		}

		public override bool IsVisible(DecompilerTextView textView) {
			return decompilerTextView == textView;
		}

		public override ITextMarker CreateMarker(ITextMarkerService markerService, DecompilerTextView textView) {
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
				if (cm == null)
					return false;
				var mbm = b as MarkerBookmark;
				if (mbm == null)
					return false;
				var key = MethodKey.Create(mbm.MemberReference);
				return key != null && cm.ContainsKey(key.Value);
			};
			marker.Bookmark = this;
			return marker;
		}
	}
}
