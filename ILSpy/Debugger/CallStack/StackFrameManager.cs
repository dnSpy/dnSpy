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
using System.Diagnostics;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.AvalonEdit;
using dnSpy.MVVM;
using dnSpy.Tabs;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace dnSpy.Debugger.CallStack {
	sealed class StackFrameManager : ViewModelBase {
		public static readonly StackFrameManager Instance = new StackFrameManager();

		// VS2015 shows at most 5000 frames, so let's use that number as well
		const int MAX_SHOWN_FRAMES = 5000;

		readonly List<StackFrameLine> stackFrameLines = new List<StackFrameLine>();

		public event EventHandler StackFramesUpdated;

		sealed class CurrentState {
			public int FrameNumber;
			public DnThread Thread;
		}
		CurrentState currentState = new CurrentState();

		internal void OnLoaded() {
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
			MainWindow.Instance.ExecuteWhenLoaded(() => {
				MainWindow.Instance.OnTabStateChanged += (sender, e) => OnTabStateChanged(e.OldTabState, e.NewTabState);
				foreach (var tabState in MainWindow.Instance.AllVisibleDecompileTabStates)
					OnTabStateChanged(null, tabState);
			});
		}

		[Conditional("DEBUG")]
		void VerifyDebuggeeStopped() {
			Debug.Assert(DebugManager.Instance.ProcessState == DebuggerProcessState.Stopped);
		}

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			currentState = new CurrentState();
			switch (DebugManager.Instance.ProcessState) {
			case DebuggerProcessState.Starting:
			case DebuggerProcessState.Running:
				break;

			case DebuggerProcessState.Stopped:
				currentState.Thread = DebugManager.Instance.Debugger.Current.Thread;
				SelectedFrame = 0;
				foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
					UpdateReturnStatementBookmarks(textView, false);
				break;

			case DebuggerProcessState.Terminated:
				foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
					Remove(textView);
				break;

			default:
				throw new InvalidOperationException();
			}

			if (StackFramesUpdated != null)
				StackFramesUpdated(this, EventArgs.Empty);
		}

		void OnTabStateChanged(TabState oldTabState, TabState newTabState) {
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

		void DecompilerTextView_OnBeforeShowOutput(object sender, DecompilerTextView.ShowOutputEventArgs e) {
			Remove((DecompilerTextView)sender);
		}

		void DecompilerTextView_OnShowOutput(object sender, DecompilerTextView.ShowOutputEventArgs e) {
			e.HasMovedCaret |= UpdateReturnStatementBookmarks((DecompilerTextView)sender, !e.HasMovedCaret);
		}

		/// <summary>
		/// Gets/sets the selected frame number. 0 is the current frame.
		/// </summary>
		public int SelectedFrame {
			get { VerifyDebuggeeStopped(); return currentState.FrameNumber; }
			set {
				VerifyDebuggeeStopped();
				if (value != currentState.FrameNumber) {
					var old = currentState.FrameNumber;
					currentState.FrameNumber = value;
					foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
						UpdateReturnStatementBookmarks(textView);
					OnPropertyChanged(new VMPropertyChangedEventArgs<int>("SelectedFrame", old, currentState.FrameNumber));
				}
			}
		}

		void Remove(DecompilerTextView decompilerTextView) {
			for (int i = stackFrameLines.Count - 1; i >= 0; i--) {
				if (stackFrameLines[i].TextView == decompilerTextView) {
					TextLineObjectManager.Instance.Remove(stackFrameLines[i]);
					stackFrameLines.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Should be called each time the IL offset has been updated
		/// </summary>
		bool UpdateReturnStatementBookmarks(DecompilerTextView decompilerTextView, bool moveCaret = false) {
			Remove(decompilerTextView);
			bool movedCaret = false;
			var cm = decompilerTextView == null ? null : decompilerTextView.CodeMappings;
			bool updateReturnStatements = cm != null && DebugManager.Instance.ProcessState == DebuggerProcessState.Stopped;
			if (updateReturnStatements) {
				int frameNo = -1;
				bool tooManyFrames;
				foreach (var frame in GetFrames(out tooManyFrames)) {
					frameNo++;
					if (!frame.IsILFrame)
						continue;
					if (!frame.ILFrameIP.IsExact && !frame.ILFrameIP.IsApproximate)
						continue;
					uint token = frame.Token;
					if (token == 0)
						continue;
					var serAsm = frame.GetSerializedDnModuleWithAssembly();
					if (serAsm == null)
						continue;

					StackFrameLineType type;
					if (frameNo == 0)
						type = StackFrameLineType.CurrentStatement;
					else
						type = currentState.FrameNumber == frameNo ? StackFrameLineType.SelectedReturnStatement : StackFrameLineType.ReturnStatement;
					var key = MethodKey.Create(token, serAsm.Value.Module);
					uint offset = frame.ILFrameIP.Offset;
					MethodDef methodDef;
					TextLocation location, endLocation;
					if (cm != null && cm.ContainsKey(key) &&
						cm[key].GetInstructionByTokenAndOffset(offset, out methodDef, out location, out endLocation)) {
						var rs = new StackFrameLine(type, decompilerTextView, key, offset);
						stackFrameLines.Add(rs);
						TextLineObjectManager.Instance.Add(rs);

						if (moveCaret && frameNo == currentState.FrameNumber) {
							decompilerTextView.ScrollAndMoveCaretTo(location.Line, location.Column);
							movedCaret = true;
						}
					}
				}
			}
			return movedCaret;
		}

		public List<CorFrame> GetFrames(out bool tooManyFrames) {
			tooManyFrames = false;
			var list = new List<CorFrame>();

			var thread = currentState.Thread;
			if (thread != null) {
				foreach (var frame in thread.AllFrames) {
					if (list.Count >= MAX_SHOWN_FRAMES) {
						tooManyFrames = true;
						break;
					}
					list.Add(frame);
				}
			}

			return list;
		}
	}
}
