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
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.CallStack {
	sealed class StackFramesUpdatedEventArgs : EventArgs {
		public DnDebugger Debugger { get; }

		public StackFramesUpdatedEventArgs(DnDebugger debugger) {
			Debugger = debugger;
		}
	}

	interface IStackFrameService : INotifyPropertyChanged {
		/// <summary>
		/// Gets/sets the selected frame number. 0 is the current frame.
		/// </summary>
		int SelectedFrameNumber { get; set; }
		DnThread SelectedThread { get; set; }
		CorFrame FirstILFrame { get; }
		CorFrame SelectedFrame { get; }
		event EventHandler<StackFramesUpdatedEventArgs> StackFramesUpdated;
		event EventHandler<NewFramesEventArgs> NewFrames;
		List<CorFrame> GetFrames(out bool tooManyFrames);
	}

	enum NewFramesKind {
		/// <summary>
		/// New frames available
		/// </summary>
		NewFrames,

		/// <summary>
		/// No frame exists (eg. debuggee has terminated or it's running)
		/// </summary>
		Cleared,

		/// <summary>
		/// Selected frame number changed
		/// </summary>
		NewFrameNumber,
	}

	sealed class NewFramesEventArgs : EventArgs {
		public NewFramesKind Kind { get; }
		public NewFramesEventArgs(NewFramesKind kind) {
			Kind = kind;
		}
	}

	[Export(typeof(IStackFrameService)), Export(typeof(ILoadBeforeDebug))]
	sealed class StackFrameService : ViewModelBase, IStackFrameService, ILoadBeforeDebug {
		const int MaxShownFrames = 50000;

		public event EventHandler<StackFramesUpdatedEventArgs> StackFramesUpdated;
		public event EventHandler<NewFramesEventArgs> NewFrames;

		sealed class CurrentState {
			public int FrameNumber;
			public DnThread Thread;
		}
		CurrentState currentState = new CurrentState();

		readonly ITheDebugger theDebugger;

		[ImportingConstructor]
		StackFrameService(ITheDebugger theDebugger) {
			this.theDebugger = theDebugger;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			theDebugger.ProcessRunning += TheDebugger_ProcessRunning;
		}

		bool IsPaused => theDebugger.ProcessState == DebuggerProcessState.Paused;

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
					RaiseClearFrames();
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

				RaiseNewFrames();
				break;

			case DebuggerProcessState.Terminated:
				savedEvalState = null;
				RaiseClearFrames();
				break;

			default:
				throw new InvalidOperationException();
			}

			StackFramesUpdated?.Invoke(this, new StackFramesUpdatedEventArgs(dbg));
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

		void TheDebugger_ProcessRunning(object sender, EventArgs e) => RaiseClearFrames();
		void RaiseClearFrames() => NewFrames?.Invoke(this, new NewFramesEventArgs(NewFramesKind.Cleared));
		void RaiseNewFrames() => NewFrames?.Invoke(this, new NewFramesEventArgs(NewFramesKind.NewFrames));
		void RaiseNewFrameNumber() => NewFrames?.Invoke(this, new NewFramesEventArgs(NewFramesKind.NewFrameNumber));

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
					RaiseNewFrames();
					OnPropertyChanged(new VMPropertyChangedEventArgs<DnThread>(nameof(SelectedThread), oldThread, currentState.Thread));
					OnPropertyChanged(new VMPropertyChangedEventArgs<int>(nameof(SelectedFrameNumber), -1, currentState.FrameNumber));
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
					RaiseNewFrameNumber();
					OnPropertyChanged(new VMPropertyChangedEventArgs<int>(nameof(SelectedFrameNumber), old, currentState.FrameNumber));
				}
			}
		}

		public List<CorFrame> GetFrames(out bool tooManyFrames) => GetFrames(MaxShownFrames, out tooManyFrames);

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
