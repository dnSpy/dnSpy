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
using System.Linq;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.AvalonEdit;
using dnSpy.Files;
using dnSpy.MVVM;
using dnSpy.Tabs;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace dnSpy.Debugger.CallStack {
	class StackFramesUpdatedEventArgs : EventArgs {
		public readonly DnDebugger Debugger;

		public StackFramesUpdatedEventArgs(DnDebugger debugger) {
			this.Debugger = debugger;
		}
	}

	sealed class StackFrameManager : ViewModelBase {
		public static readonly StackFrameManager Instance = new StackFrameManager();

		// VS2015 shows at most 5000 frames but we can increase that to 50000, dnSpy had no trouble
		// showing 12K frames, which was the total number of frames until I got a SO in the test app.
		const int MAX_SHOWN_FRAMES = 50000;
		// We don't need to show all return lines above, a much smaller number should be enough.
		// A large number will only make everything slow down to a crawl.
		const int MAX_STACKFRAME_LINES = 500;

		readonly List<StackFrameLine> stackFrameLines = new List<StackFrameLine>();

		public event EventHandler<StackFramesUpdatedEventArgs> StackFramesUpdated;

		sealed class CurrentState {
			public int FrameNumber;
			public DnThread Thread;
		}
		CurrentState currentState = new CurrentState();

		internal void OnLoaded() {
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
			DebugManager.Instance.ProcessRunning += DebugManager_ProcessRunning;
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
			var oldState = currentState;
			currentState = new CurrentState();
			var dbg = (DnDebugger)sender;
			switch (DebugManager.Instance.ProcessState) {
			case DebuggerProcessState.Starting:
				savedEvalState = null;
				break;

			case DebuggerProcessState.Continuing:
				if (dbg.IsEvaluating && savedEvalState == null)
					savedEvalState = oldState;
				break;

			case DebuggerProcessState.Running:
				if (!dbg.IsEvaluating)
					ClearStackFrameLines();
				break;

			case DebuggerProcessState.Stopped:
				if (dbg.IsEvaluating)
					break;

				// Don't update the selected thread if we just evaluated something
				if (UpdateState(savedEvalState)) {
					currentState.Thread = DebugManager.Instance.Debugger.Current.Thread;
					SelectedFrameNumber = 0;
				}
				else
					currentState = savedEvalState;
				savedEvalState = null;

				foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
					UpdateStackFrameLines(textView, false);
				break;

			case DebuggerProcessState.Terminated:
				savedEvalState = null;
				ClearStackFrameLines();
				break;

			default:
				throw new InvalidOperationException();
			}

			if (StackFramesUpdated != null)
				StackFramesUpdated(this, new StackFramesUpdatedEventArgs(dbg));
		}
		CurrentState savedEvalState;

		bool UpdateState(CurrentState state) {
			if (state == null)
				return true;
			if (state.Thread == null)
				return true;
			if (!state.Thread.Process.Threads.Contains(state.Thread))
				return true;

			return false;
		}

		void DebugManager_ProcessRunning(object sender, EventArgs e) {
			ClearStackFrameLines();
		}

		void ClearStackFrameLines() {
			foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
				Remove(textView);
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
				UpdateStackFrameLines(newTsd.TextView);
		}

		void DecompilerTextView_OnBeforeShowOutput(object sender, DecompilerTextView.ShowOutputEventArgs e) {
			Remove((DecompilerTextView)sender);
		}

		void DecompilerTextView_OnShowOutput(object sender, DecompilerTextView.ShowOutputEventArgs e) {
			e.HasMovedCaret |= UpdateStackFrameLines((DecompilerTextView)sender, !e.HasMovedCaret);
		}

		CorFrame GetFrameByNumber(int number) {
			var thread = currentState.Thread;
			if (thread == null)
				return null;
			foreach (var frame in thread.AllFrames) {
				if (number-- == 0)
					return frame;
			}
			return null;
		}

		public CorFrame SelectedFrame {
			get {
				VerifyDebuggeeStopped();
				return GetFrameByNumber(SelectedFrameNumber);
			}
		}

		public CorFrame FirstILFrame {
			get {
				VerifyDebuggeeStopped();
				var thread = currentState.Thread;
				if (thread == null)
					return null;
				return thread.AllFrames.FirstOrDefault(f => f.IsILFrame);
			}
		}

		public DnThread SelectedThread {
			get { VerifyDebuggeeStopped(); return currentState.Thread; }
			set {
				VerifyDebuggeeStopped();
				if (currentState.Thread != value) {
					var oldThread = currentState.Thread;
					currentState.Thread = value;
					currentState.FrameNumber = 0;
					UpdateStackFrameLinesInTextViews();
					DebugManager.Instance.UpdateCurrentLocation(FirstILFrame);
					OnPropertyChanged(new VMPropertyChangedEventArgs<DnThread>("SelectedThread", oldThread, currentState.Thread));
					OnPropertyChanged(new VMPropertyChangedEventArgs<int>("SelectedFrameNumber", -1, currentState.FrameNumber));
				}
			}
		}

		/// <summary>
		/// Gets/sets the selected frame number. 0 is the current frame.
		/// </summary>
		public int SelectedFrameNumber {
			get { VerifyDebuggeeStopped(); return currentState.FrameNumber; }
			set {
				VerifyDebuggeeStopped();
				if (value != currentState.FrameNumber) {
					var old = currentState.FrameNumber;
					currentState.FrameNumber = value;
					UpdateStackFrameLinesInTextViews();
					OnPropertyChanged(new VMPropertyChangedEventArgs<int>("SelectedFrameNumber", old, currentState.FrameNumber));
				}
			}
		}

		void UpdateStackFrameLinesInTextViews() {
			foreach (var textView in MainWindow.Instance.AllVisibleTextViews)
				UpdateStackFrameLines(textView);
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
		bool UpdateStackFrameLines(DecompilerTextView decompilerTextView, bool moveCaret = false) {
			Remove(decompilerTextView);
			bool movedCaret = false;
			var cm = decompilerTextView == null ? null : decompilerTextView.CodeMappings;
			bool updateReturnStatements = cm != null && DebugManager.Instance.ProcessState == DebuggerProcessState.Stopped;
			if (updateReturnStatements) {
				int frameNo = -1;
				bool tooManyFrames;
				foreach (var frame in GetFrames(MAX_STACKFRAME_LINES, out tooManyFrames)) {
					frameNo++;
					if (!frame.IsILFrame)
						continue;
					var ip = frame.ILFrameIP;
					if (!ip.IsExact && !ip.IsApproximate && !ip.IsProlog && !ip.IsEpilog)
						continue;
					uint token = frame.Token;
					if (token == 0)
						continue;
					var serAsm = frame.SerializedDnModule;
					if (serAsm == null)
						continue;

					StackFrameLineType type;
					if (frameNo == 0)
						type = StackFrameLineType.CurrentStatement;
					else
						type = currentState.FrameNumber == frameNo ? StackFrameLineType.SelectedReturnStatement : StackFrameLineType.ReturnStatement;
					var key = new SerializedDnSpyToken(serAsm.Value.ToSerializedDnSpyModule(), token);
					uint offset = frame.GetILOffset();
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
			return GetFrames(MAX_SHOWN_FRAMES, out tooManyFrames);
		}

		List<CorFrame> GetFrames(int max, out bool tooManyFrames) {
			tooManyFrames = false;
			var list = new List<CorFrame>();

			var thread = currentState.Thread;
			if (thread != null) {
				foreach (var frame in thread.AllFrames) {
					if (list.Count >= max) {
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
