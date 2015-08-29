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
using System.Diagnostics;
using System.Windows.Input;
using dndbg.Engine;
using dnSpy.AsmEditor;

namespace dnSpy.Debugger {
	public sealed class DebugManager {
		public static readonly DebugManager Instance = new DebugManager(DebuggerPluginLoader.CreateAskDebugAssembly());

		readonly IAskDebugAssembly askDebugAssembly;

		DebugManager(IAskDebugAssembly askDebugAssembly) {
			this.askDebugAssembly = askDebugAssembly;
		}

		public ICommand DebugCurrentAssemblyCommand {
			get { return new RelayCommand(a => DebugCurrentAssembly(), a => CanDebugCurrentAssembly); }
		}

		public ICommand DebugAssemblyCommand {
			get { return new RelayCommand(a => DebugAssembly(), a => CanDebugAssembly); }
		}

		public ICommand AttachCommand {
			get { return new RelayCommand(a => Attach(), a => CanAttach); }
		}

		public ICommand BreakCommand {
			get { return new RelayCommand(a => Break(), a => CanBreak); }
		}

		public ICommand RestartCommand {
			get { return new RelayCommand(a => Restart(), a => CanRestart); }
		}

		public ICommand StopCommand {
			get { return new RelayCommand(a => Stop(), a => CanStop); }
		}

		public ICommand DetachCommand {
			get { return new RelayCommand(a => Detach(), a => CanDetach); }
		}

		public ICommand ContinueCommand {
			get { return new RelayCommand(a => Continue(), a => CanContinue); }
		}

		public ICommand StepIntoCommand {
			get { return new RelayCommand(a => StepInto(), a => CanStepInto); }
		}

		public ICommand StepOverCommand {
			get { return new RelayCommand(a => StepOver(), a => CanStepOver); }
		}

		public ICommand StepOutCommand {
			get { return new RelayCommand(a => StepOut(), a => CanStepOut); }
		}

		public ICommand DeleteAllBreakpointsCommand {
			get { return new RelayCommand(a => DeleteAllBreakpoints(), a => CanDeleteAllBreakpoints); }
		}

		public ICommand ToggleBreakpointCommand {
			get { return new RelayCommand(a => ToggleBreakpoint(), a => CanToggleBreakpoint); }
		}

		public ICommand DisableBreakpointCommand {
			get { return new RelayCommand(a => DisableBreakpoint(), a => CanDisableBreakpoint); }
		}

		public ICommand ShowNextStatementCommand {
			get { return new RelayCommand(a => ShowNextStatement(), a => CanShowNextStatement); }
		}

		public ICommand SetNextStatementCommand {
			get { return new RelayCommand(a => SetNextStatement(), a => CanSetNextStatement); }
		}

		public DebuggerProcessState ProcessState {
			get { return Debugger == null ? DebuggerProcessState.Terminated : Debugger.ProcessState; }
		}

		/// <summary>
		/// true if we're debugging
		/// </summary>
		public bool IsDebugging {
			get { return Debugger != null; }
		}

		/// <summary>
		/// true if debugged process is running
		/// </summary>
		public bool IsProcessRunning {
			get { return Debugger != null && Debugger.ProcessState == DebuggerProcessState.Running; }
		}

		/// <summary>
		/// Gets the current debugger. This is null if we're not debugging anything
		/// </summary>
		public DnDebugger Debugger {
			get { return debugger; }
		}
		DnDebugger debugger;

		public event EventHandler<DebuggerEventArgs> OnProcessStateChanged;

		DebugManager() {
		}

		public bool DebugProcess(DebugProcessOptions options) {
			if (IsDebugging)
				return false;
			if (options == null)
				return false;

			RemoveDebugger();

			oldDebuggerProcessState = null;
			var newDebugger = DnDebugger.DebugProcess(options);
			AddDebugger(newDebugger);
			Debug.Assert(debugger == newDebugger);
			if (OnProcessStateChanged != null)
				OnProcessStateChanged(debugger, DebuggerEventArgs.Empty);

			return true;
		}

		void DnDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			if (debugger == null || sender != debugger)
				return;

			var newState = debugger.ProcessState;
			if (newState == oldDebuggerProcessState)
				return;
			oldDebuggerProcessState = newState;

			if (OnProcessStateChanged != null)
				OnProcessStateChanged(sender, e);

			if (debugger.ProcessState == DebuggerProcessState.Terminated) {
				lastDebugProcessOptions = null;
				RemoveDebugger();
			}
		}
		DebuggerProcessState? oldDebuggerProcessState;

		void RemoveDebugger() {
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

		public bool CanDebugCurrentAssembly {
			get { return !IsDebugging && askDebugAssembly.CanDebugCurrentAssembly; }
		}

		public void DebugCurrentAssembly() {
			if (!CanDebugCurrentAssembly)
				return;
			DebugAssembly(askDebugAssembly.GetDebugCurrentAssemblyOptions());
		}

		public bool CanDebugAssembly {
			get { return !IsDebugging && askDebugAssembly.CanDebugAssembly; }
		}

		public void DebugAssembly() {
			if (!CanDebugAssembly)
				return;
			DebugAssembly(askDebugAssembly.GetDebugAssemblyOptions());
		}

		public bool CanRestart {
			get { return IsDebugging && lastDebugProcessOptions != null; }
		}

		public void Restart() {
			if (!CanRestart)
				return;
			DebugAssembly(lastDebugProcessOptions);
		}

		void DebugAssembly(DebugProcessOptions options) {
			if (options == null)
				return;
			var optionsCopy = options.Clone();
			if (!DebugProcess(options))
				return;
			lastDebugProcessOptions = optionsCopy;
		}
		DebugProcessOptions lastDebugProcessOptions = null;

		public bool CanAttach {
			get { return !IsDebugging; }
		}

		public void Attach() {
			if (!CanAttach)
				return;
			//TODO:
		}

		public bool CanBreak {
			get { return ProcessState == DebuggerProcessState.Starting || ProcessState == DebuggerProcessState.Running; }
		}

		public void Break() {
			if (!CanBreak)
				return;
			//TODO:
		}

		public bool CanStop {
			get { return ProcessState == DebuggerProcessState.Starting || ProcessState == DebuggerProcessState.Running; }
		}

		public void Stop() {
			if (!CanStop)
				return;
			//TODO:
		}

		public bool CanDetach {
			get { return ProcessState != DebuggerProcessState.Terminated; }
		}

		public void Detach() {
			if (!CanDetach)
				return;
			//TODO:
		}

		public bool CanContinue {
			get { return ProcessState == DebuggerProcessState.Stopped; }
		}

		public void Continue() {
			if (!CanContinue)
				return;
			Debugger.Continue();
		}

		public bool CanStepInto {
			get { return ProcessState == DebuggerProcessState.Stopped; }
		}

		public void StepInto() {
			if (!CanStepInto)
				return;
			//TODO:
		}

		public bool CanStepOver {
			get { return ProcessState == DebuggerProcessState.Stopped; }
		}

		public void StepOver() {
			if (!CanStepOver)
				return;
			//TODO:
		}

		public bool CanStepOut {
			get { return ProcessState == DebuggerProcessState.Stopped; }
		}

		public void StepOut() {
			if (!CanStepOut)
				return;
			//TODO:
		}

		public bool CanDeleteAllBreakpoints {
			get { return false/*TODO:*/; }
		}

		public void DeleteAllBreakpoints() {
			if (!CanDeleteAllBreakpoints)
				return;
			//TODO:
		}

		public bool CanToggleBreakpoint {
			get { return false/*TODO:*/; }
		}

		public void ToggleBreakpoint() {
			if (!CanToggleBreakpoint)
				return;
			//TODO:
		}

		public bool CanDisableBreakpoint {
			get { return false/*TODO:*/; }
		}

		public void DisableBreakpoint() {
			if (!CanDisableBreakpoint)
				return;
			//TODO:
		}

		public bool CanShowNextStatement {
			get { return false/*TODO:*/; }
		}

		public void ShowNextStatement() {
			if (!CanShowNextStatement)
				return;
			//TODO:
		}

		public bool CanSetNextStatement {
			get { return false/*TODO:*/; }
		}

		public void SetNextStatement() {
			if (!CanSetNextStatement)
				return;
			//TODO:
		}
	}
}
