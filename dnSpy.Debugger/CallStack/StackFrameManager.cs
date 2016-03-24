/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.CallStack {
	sealed class StackFramesUpdatedEventArgs : EventArgs {
		public readonly DnDebugger Debugger;

		public StackFramesUpdatedEventArgs(DnDebugger debugger) {
			this.Debugger = debugger;
		}
	}

	interface IStackFrameManager : INotifyPropertyChanged {
		/// <summary>
		/// Gets/sets the selected frame number. 0 is the current frame.
		/// </summary>
		int SelectedFrameNumber { get; set; }
		DnThread SelectedThread { get; set; }
		CorFrame FirstILFrame { get; }
		CorFrame SelectedFrame { get; }
		event EventHandler<StackFramesUpdatedEventArgs> StackFramesUpdated;
		List<CorFrame> GetFrames(out bool tooManyFrames);
	}

	[Export, Export(typeof(IStackFrameManager)), Export(typeof(ILoadBeforeDebug)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class StackFrameManager : ViewModelBase, IStackFrameManager, ILoadBeforeDebug {
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

		readonly ITheDebugger theDebugger;
		readonly IFileTabManager fileTabManager;
		readonly ITextLineObjectManager textLineObjectManager;
		readonly Lazy<IModuleLoader> moduleLoader;

		[ImportingConstructor]
		StackFrameManager(ITheDebugger theDebugger, IFileTabManager fileTabManager, ITextLineObjectManager textLineObjectManager, Lazy<IModuleLoader> moduleLoader, ITextEditorUIContextManager textEditorUIContextManager) {
			this.theDebugger = theDebugger;
			this.fileTabManager = fileTabManager;
			this.textLineObjectManager = textLineObjectManager;
			this.moduleLoader = moduleLoader;
			textEditorUIContextManager.Add(OnTextEditorUIContextEvent, TextEditorUIContextManagerConstants.ORDER_DEBUGGER_CALLSTACK);
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			theDebugger.ProcessRunning += TheDebugger_ProcessRunning;
		}

		bool IsPaused {
			get { return theDebugger.ProcessState == DebuggerProcessState.Paused; }
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var oldState = currentState;
			currentState = new CurrentState();
			var dbg = (DnDebugger)sender;
			switch (theDebugger.ProcessState) {
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

			case DebuggerProcessState.Paused:
				if (dbg.IsEvaluating)
					break;

				// Don't update the selected thread if we just evaluated something
				if (UpdateState(savedEvalState)) {
					currentState.Thread = dbg.Current.Thread;
					SelectedFrameNumber = 0;
				}
				else
					currentState = savedEvalState;
				savedEvalState = null;

				foreach (var tab in fileTabManager.VisibleFirstTabs)
					UpdateStackFrameLines(tab.UIContext as ITextEditorUIContext, false);
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

		void TheDebugger_ProcessRunning(object sender, EventArgs e) {
			ClearStackFrameLines();
		}

		void ClearStackFrameLines() {
			foreach (var tab in fileTabManager.VisibleFirstTabs)
				Remove(tab.UIContext as ITextEditorUIContext);
		}

		void OnTextEditorUIContextEvent(TextEditorUIContextListenerEvent @event, ITextEditorUIContext uiContext, object data) {
			if (@event == TextEditorUIContextListenerEvent.NewContent) {
				Remove(uiContext);
				UpdateStackFrameLines(uiContext, false);
			}
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
				if (!IsPaused)
					return null;
				return GetFrameByNumber(SelectedFrameNumber);
			}
		}

		public CorFrame FirstILFrame {
			get {
				if (!IsPaused)
					return null;
				var thread = currentState.Thread;
				if (thread == null)
					return null;
				return thread.AllFrames.FirstOrDefault(f => f.IsILFrame);
			}
		}

		public DnThread SelectedThread {
			get { return !IsPaused ? null : currentState.Thread; }
			set {
				if (!IsPaused)
					return;
				if (currentState.Thread != value) {
					var oldThread = currentState.Thread;
					currentState.Thread = value;
					currentState.FrameNumber = 0;
					UpdateStackFrameLinesInTextViews();
					OnPropertyChanged(new VMPropertyChangedEventArgs<DnThread>("SelectedThread", oldThread, currentState.Thread));
					OnPropertyChanged(new VMPropertyChangedEventArgs<int>("SelectedFrameNumber", -1, currentState.FrameNumber));
				}
			}
		}

		public int SelectedFrameNumber {
			get { return !IsPaused ? 0 : currentState.FrameNumber; }
			set {
				if (!IsPaused)
					return;
				if (value != currentState.FrameNumber) {
					var old = currentState.FrameNumber;
					currentState.FrameNumber = value;
					UpdateStackFrameLinesInTextViews();
					OnPropertyChanged(new VMPropertyChangedEventArgs<int>("SelectedFrameNumber", old, currentState.FrameNumber));
				}
			}
		}

		void UpdateStackFrameLinesInTextViews() {
			foreach (var tab in fileTabManager.VisibleFirstTabs)
				UpdateStackFrameLines(tab.UIContext as ITextEditorUIContext);
		}

		void Remove(ITextEditorUIContext uiContext) {
			if (uiContext == null)
				return;
			for (int i = stackFrameLines.Count - 1; i >= 0; i--) {
				if (stackFrameLines[i].TextView == uiContext) {
					textLineObjectManager.Remove(stackFrameLines[i]);
					stackFrameLines.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Should be called each time the IL offset has been updated
		/// </summary>
		bool UpdateStackFrameLines(ITextEditorUIContext uiContext, bool moveCaret = false) {
			if (uiContext == null)
				return false;
			Remove(uiContext);
			bool movedCaret = false;
			var cm = uiContext.TryGetCodeMappings();
			bool updateReturnStatements = cm != null && theDebugger.ProcessState == DebuggerProcessState.Paused;
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
					var key = new SerializedDnToken(serAsm.Value, token);
					uint offset = frame.GetILOffset(moduleLoader.Value);
					MethodDef methodDef;
					TextPosition location, endLocation;
					var mm = cm.TryGetMapping(key);
					if (mm != null && mm.GetInstructionByTokenAndOffset(offset, out methodDef, out location, out endLocation)) {
						var rs = new StackFrameLine(type, uiContext, key, offset);
						stackFrameLines.Add(rs);
						textLineObjectManager.Add(rs);

						if (moveCaret && frameNo == currentState.FrameNumber) {
							uiContext.ScrollAndMoveCaretTo(location.Line, location.Column);
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
