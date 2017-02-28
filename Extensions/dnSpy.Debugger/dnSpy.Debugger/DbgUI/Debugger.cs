/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Debugger.DbgUI {
	abstract class Debugger {
		public abstract bool IsDebugging { get; }
		public abstract bool CanStartWithoutDebugging { get; }
		public abstract void StartWithoutDebugging();
		public abstract bool CanDebugProgram { get; }
		public abstract void DebugProgram();
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
		public abstract bool CanStepInto { get; }
		public abstract void StepInto();
		public abstract bool CanStepOver { get; }
		public abstract void StepOver();
		public abstract bool CanStepOut { get; }
		public abstract void StepOut();
	}
}
