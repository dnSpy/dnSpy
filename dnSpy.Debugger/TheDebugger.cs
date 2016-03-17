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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using dndbg.Engine;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger {
	interface ITheDebugger {
		/// <summary>
		/// Gets the current debugger. This is null if we're not debugging anything
		/// </summary>
		DnDebugger Debugger { get; }
		event EventHandler<DebuggerEventArgs> OnProcessStateChanged;
		event EventHandler<DebuggerEventArgs> OnProcessStateChanged_First;
		event EventHandler<DebuggerEventArgs> OnProcessStateChanged_Last;

		/// <summary>
		/// Called when the process has been running for a short amount of time. Usually won't
		/// get called when stepping since it normally doesn't take a long time.
		/// </summary>
		event EventHandler ProcessRunning;
		DebuggerProcessState ProcessState { get; }
		bool IsDebugging { get; }
		void Initialize(DnDebugger newDebugger);
		void RemoveAndRaiseEvent();
		void RemoveDebugger();
		void CallOnProcessStateChanged();
		void DisposeHandle(CorValue value);

		/// <summary>
		/// Creates an eval. Should normally not be called if <see cref="EvalDisabled"/> is true
		/// since it means that an evaluation failed (eg. timed out)
		/// </summary>
		/// <param name="thread">Thread to use</param>
		/// <returns></returns>
		DnEval CreateEval(CorThread thread);
		bool EvalDisabled { get; }
		bool CanEvaluate { get; }
		bool EvalCompleted { get; }

		void SetUnhandledException(bool value);
	}

	[Export, Export(typeof(ITheDebugger)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class TheDebugger : ITheDebugger {
		readonly Dispatcher dispatcher;
		readonly IDebuggerSettings debuggerSettings;
		readonly DebuggedProcessRunningNotifier debuggedProcessRunningNotifier;
		readonly Lazy<ILoadBeforeDebug>[] loadBeforeDebugInsts;

		public DnDebugger Debugger {
			get { return debugger; }
		}
		DnDebugger debugger;

		public event EventHandler<DebuggerEventArgs> OnProcessStateChanged_First;
		public event EventHandler<DebuggerEventArgs> OnProcessStateChanged;
		public event EventHandler<DebuggerEventArgs> OnProcessStateChanged_Last;
		public event EventHandler ProcessRunning;

		public DebuggerProcessState ProcessState {
			get { return Debugger == null ? DebuggerProcessState.Terminated : Debugger.ProcessState; }
		}

		public bool IsDebugging {
			get { return ProcessState != DebuggerProcessState.Terminated; }
		}

		[ImportingConstructor]
		TheDebugger(IDebuggerSettings debuggerSettings, [ImportMany] IEnumerable<Lazy<ILoadBeforeDebug>> loadBeforeDebugInsts) {
			this.debuggerSettings = debuggerSettings;
			this.dispatcher = Dispatcher.CurrentDispatcher;
			this.loadBeforeDebugInsts = loadBeforeDebugInsts.ToArray();
			debuggedProcessRunningNotifier = new DebuggedProcessRunningNotifier(this);
			debuggedProcessRunningNotifier.ProcessRunning += DebuggedProcessRunningNotifier_ProcessRunning;
		}

		void DebuggedProcessRunningNotifier_ProcessRunning(object sender, DebuggedProcessRunningEventArgs e) {
			if (ProcessRunning != null)
				ProcessRunning(this, e);
		}

		public void Initialize(DnDebugger newDebugger) {
			foreach (var l in loadBeforeDebugInsts) {
				var o = l.Value;
			}
			if (debuggerSettings.DisableManagedDebuggerDetection)
				DisableSystemDebuggerDetection.Initialize(newDebugger);
			AddDebugger(newDebugger);
			Debug.Assert(debugger == newDebugger);
			CallOnProcessStateChanged();
		}

		public void CallOnProcessStateChanged() {
			CallOnProcessStateChanged(null);
		}

		void CallOnProcessStateChanged(DnDebugger dbg) {
			CallOnProcessStateChanged(dbg ?? debugger, DebuggerEventArgs.Empty);
		}

		void CallOnProcessStateChanged(object sender, DebuggerEventArgs e) {
			// InMemoryModuleManager should be notified here. It needs to execute first so it can
			// call LoadEverything() and load all dynamic modules so ResolveToken() of new methods
			// and types work.
			if (OnProcessStateChanged_First != null)
				OnProcessStateChanged_First(sender, e);

			if (OnProcessStateChanged != null)
				OnProcessStateChanged(sender, e ?? DebuggerEventArgs.Empty);

			// The script code uses this event to make sure it always executes last
			if (OnProcessStateChanged_Last != null)
				OnProcessStateChanged_Last(sender, e);
		}

		void DnDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			if (debugger == null || sender != debugger)
				return;

			switch (debugger.ProcessState) {
			case DebuggerProcessState.Starting:
				unhandledException = false;
				evalDisabled = false;
				break;

			case DebuggerProcessState.Paused:
				if (debugger.IsEvaluating || debugger.EvalCompleted)
					break;
				evalDisabled = false;
				break;
			}

			CallOnProcessStateChanged(sender, e);

			if (debugger.ProcessState == DebuggerProcessState.Terminated) {
				RemoveDebugger();
				evalDisabled = false;
				unhandledException = false;
			}
		}

		public void RemoveDebugger() {
			if (debugger == null)
				return;

			debugger.OnProcessStateChanged -= DnDebugger_OnProcessStateChanged;
			debugger = null;
		}

		void AddDebugger(DnDebugger newDebugger) {
			RemoveDebugger();

			debugger = newDebugger;
			newDebugger.OnProcessStateChanged += DnDebugger_OnProcessStateChanged;
		}

		public void RemoveAndRaiseEvent() {
			if (debugger != null) {
				var dbg = debugger;
				RemoveDebugger();
				CallOnProcessStateChanged(dbg);
			}
		}

		public void DisposeHandle(CorValue value) {
			var dbg = Debugger;
			if (dbg != null)
				dbg.DisposeHandle(value);
		}

		public DnEval CreateEval(CorThread thread) {
			Debug.WriteLineIf(ProcessState != DebuggerProcessState.Paused, dnSpy_Debugger_Resources.Error_CantEvalUnlessDebuggerStopped);
			if (ProcessState != DebuggerProcessState.Paused)
				throw new EvalException(-1, dnSpy_Debugger_Resources.Error_CantEvalUnlessDebuggerStopped);
			if (unhandledException)
				throw new EvalException(-1, dnSpy_Debugger_Resources.Error_CantEvalWhenUnhandledExceptionHasOccurred);
			var eval = Debugger.CreateEval();
			eval.EvalEvent += (s, e) => DnEval_EvalEvent(s, e, eval);
			eval.SetThread(thread);
			return eval;
		}

		public void SetUnhandledException(bool value) {
			unhandledException = value;
		}
		bool unhandledException;

		void DnEval_EvalEvent(object sender, EvalEventArgs e, DnEval eval) {
			if (eval == null || sender != eval)
				return;
			if (eval.EvalTimedOut)
				evalDisabled = true;
			if (callingEvalComplete)
				return;
			callingEvalComplete = true;
			if (!dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished) {
				dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
					callingEvalComplete = false;
					if (ProcessState == DebuggerProcessState.Paused)
						Debugger.SignalEvalComplete();
				}));
			}
		}
		volatile bool callingEvalComplete;

		public bool CanEvaluate {
			get { return Debugger != null && !Debugger.IsEvaluating && !unhandledException; }
		}

		public bool EvalCompleted {
			get { return Debugger != null && Debugger.EvalCompleted; }
		}

		public bool EvalDisabled {
			get { return evalDisabled; }
		}
		bool evalDisabled;
	}
}
