/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using dnSpy.Debugger.Breakpoints.Code.TextEditor;

namespace dnSpy.Debugger.DbgUI {
	abstract class Debugger {
		public abstract bool IsDebugging { get; }
		public abstract bool CanStartWithoutDebugging { get; }
		public abstract void StartWithoutDebugging();
		public abstract string? GetCurrentExecutableFilename();
		public abstract bool CanDebugProgram { get; }
		public abstract void DebugProgram(bool pauseAtEntryPoint);
		public abstract bool CanAttachProgram { get; }
		public abstract void AttachProgram();
		public abstract bool CanContinue { get; }
		public abstract void Continue();
		public abstract bool CanBreakAll { get; }
		public abstract void BreakAll();
		public abstract bool CanStopDebugging { get; }
		public abstract void StopDebugging();
		public abstract bool CanDetachAll { get; }
		public abstract void DetachAll();
		public abstract bool CanTerminateAll { get; }
		public abstract void TerminateAll();
		public abstract bool CanRestart { get; }
		public abstract void Restart();
		public abstract bool CanShowNextStatement { get; }
		public abstract void ShowNextStatement();
		public abstract bool CanSetNextStatement { get; }
		public abstract void SetNextStatement();
		public abstract bool CanStepInto { get; }
		public abstract void StepInto();
		public abstract bool CanStepOver { get; }
		public abstract void StepOver();
		public abstract bool CanStepOut { get; }
		public abstract void StepOut();
		public abstract bool CanStepIntoCurrentProcess { get; }
		public abstract void StepIntoCurrentProcess();
		public abstract bool CanStepOverCurrentProcess { get; }
		public abstract void StepOverCurrentProcess();
		public abstract bool CanStepOutCurrentProcess { get; }
		public abstract void StepOutCurrentProcess();
		public abstract bool CanGoToDisassembly { get; }
		public abstract void GoToDisassembly();
		public abstract bool CanToggleCreateBreakpoint { get; }
		public abstract void ToggleCreateBreakpoint();
		public abstract ToggleCreateBreakpointKind GetToggleCreateBreakpointKind();
		public abstract bool CanToggleEnableBreakpoint { get; }
		public abstract void ToggleEnableBreakpoint();
		public abstract ToggleEnableBreakpointKind GetToggleEnableBreakpointKind();
		public abstract bool CanDeleteAllBreakpoints { get; }
		public abstract void DeleteAllBreakpointsAskUser();
		public abstract bool CanEnableAllBreakpoints { get; }
		public abstract void EnableAllBreakpoints();
		public abstract bool CanDisableAllBreakpoints { get; }
		public abstract void DisableAllBreakpoints();

		public abstract bool CanContinueOrDebugProgram { get; }
		public abstract void ContinueOrDebugProgram();
		public abstract bool CanStepIntoOrDebugProgram { get; }
		public abstract void StepIntoOrDebugProgram();
		public abstract bool CanStepOverOrDebugProgram { get; }
		public abstract void StepOverOrDebugProgram();
	}
}
