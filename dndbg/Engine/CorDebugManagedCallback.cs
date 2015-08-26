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

using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorDebugManagedCallback : ICorDebugManagedCallback, ICorDebugManagedCallback2, ICorDebugManagedCallback3 {
		readonly DnDebugger dbg;

		public CorDebugManagedCallback(DnDebugger dbg) {
			this.dbg = dbg;
		}

		void ICorDebugManagedCallback.Breakpoint(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint) {
			dbg.OnManagedCallbackFromAnyThread(new BreakpointDebugCallbackEventArgs(pAppDomain, pThread, pBreakpoint));
		}

		void ICorDebugManagedCallback.StepComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugStepper pStepper, CorDebugStepReason reason) {
			dbg.OnManagedCallbackFromAnyThread(new StepCompleteDebugCallbackEventArgs(pAppDomain, pThread, pStepper, reason));
		}

		void ICorDebugManagedCallback.Break(ICorDebugAppDomain pAppDomain, ICorDebugThread thread) {
			dbg.OnManagedCallbackFromAnyThread(new BreakDebugCallbackEventArgs(pAppDomain, thread));
		}

		void ICorDebugManagedCallback.Exception(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int unhandled) {
			dbg.OnManagedCallbackFromAnyThread(new ExceptionDebugCallbackEventArgs(pAppDomain, pThread, unhandled));
		}

		void ICorDebugManagedCallback.EvalComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval) {
			dbg.OnManagedCallbackFromAnyThread(new EvalCompleteDebugCallbackEventArgs(pAppDomain, pThread, pEval));
		}

		void ICorDebugManagedCallback.EvalException(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval) {
			dbg.OnManagedCallbackFromAnyThread(new EvalExceptionDebugCallbackEventArgs(pAppDomain, pThread, pEval));
		}

		void ICorDebugManagedCallback.CreateProcess(ICorDebugProcess pProcess) {
			dbg.OnManagedCallbackFromAnyThread(new CreateProcessDebugCallbackEventArgs(pProcess));
		}

		void ICorDebugManagedCallback.ExitProcess(ICorDebugProcess pProcess) {
			dbg.OnManagedCallbackFromAnyThread(new ExitProcessDebugCallbackEventArgs(pProcess));
		}

		void ICorDebugManagedCallback.CreateThread(ICorDebugAppDomain pAppDomain, ICorDebugThread thread) {
			dbg.OnManagedCallbackFromAnyThread(new CreateThreadDebugCallbackEventArgs(pAppDomain, thread));
		}

		void ICorDebugManagedCallback.ExitThread(ICorDebugAppDomain pAppDomain, ICorDebugThread thread) {
			dbg.OnManagedCallbackFromAnyThread(new ExitThreadDebugCallbackEventArgs(pAppDomain, thread));
		}

		void ICorDebugManagedCallback.LoadModule(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule) {
			dbg.OnManagedCallbackFromAnyThread(new LoadModuleDebugCallbackEventArgs(pAppDomain, pModule));
		}

		void ICorDebugManagedCallback.UnloadModule(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule) {
			dbg.OnManagedCallbackFromAnyThread(new UnloadModuleDebugCallbackEventArgs(pAppDomain, pModule));
		}

		void ICorDebugManagedCallback.LoadClass(ICorDebugAppDomain pAppDomain, ICorDebugClass c) {
			dbg.OnManagedCallbackFromAnyThread(new LoadClassDebugCallbackEventArgs(pAppDomain, c));
		}

		void ICorDebugManagedCallback.UnloadClass(ICorDebugAppDomain pAppDomain, ICorDebugClass c) {
			dbg.OnManagedCallbackFromAnyThread(new UnloadClassDebugCallbackEventArgs(pAppDomain, c));
		}

		void ICorDebugManagedCallback.DebuggerError(ICorDebugProcess pProcess, int errorHR, uint errorCode) {
			dbg.OnManagedCallbackFromAnyThread(new DebuggerErrorDebugCallbackEventArgs(pProcess, errorHR, errorCode));
		}

		void ICorDebugManagedCallback.LogMessage(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int lLevel, string pLogSwitchName, string pMessage) {
			dbg.OnManagedCallbackFromAnyThread(new LogMessageDebugCallbackEventArgs(pAppDomain, pThread, lLevel, pLogSwitchName, pMessage));
		}

		void ICorDebugManagedCallback.LogSwitch(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int lLevel, uint ulReason, string pLogSwitchName, string pParentName) {
			dbg.OnManagedCallbackFromAnyThread(new LogSwitchDebugCallbackEventArgs(pAppDomain, pThread, lLevel, ulReason, pLogSwitchName, pParentName));
		}

		void ICorDebugManagedCallback.CreateAppDomain(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain) {
			dbg.OnManagedCallbackFromAnyThread(new CreateAppDomainDebugCallbackEventArgs(pProcess, pAppDomain));
		}

		void ICorDebugManagedCallback.ExitAppDomain(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain) {
			dbg.OnManagedCallbackFromAnyThread(new ExitAppDomainDebugCallbackEventArgs(pProcess, pAppDomain));
		}

		void ICorDebugManagedCallback.LoadAssembly(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly) {
			dbg.OnManagedCallbackFromAnyThread(new LoadAssemblyDebugCallbackEventArgs(pAppDomain, pAssembly));
		}

		void ICorDebugManagedCallback.UnloadAssembly(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly) {
			dbg.OnManagedCallbackFromAnyThread(new UnloadAssemblyDebugCallbackEventArgs(pAppDomain, pAssembly));
		}

		void ICorDebugManagedCallback.ControlCTrap(ICorDebugProcess pProcess) {
			dbg.OnManagedCallbackFromAnyThread(new ControlCTrapDebugCallbackEventArgs(pProcess));
		}

		void ICorDebugManagedCallback.NameChange(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread) {
			dbg.OnManagedCallbackFromAnyThread(new NameChangeDebugCallbackEventArgs(pAppDomain, pThread));
		}

		void ICorDebugManagedCallback.UpdateModuleSymbols(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule, IStream pSymbolStream) {
			dbg.OnManagedCallbackFromAnyThread(new UpdateModuleSymbolsDebugCallbackEventArgs(pAppDomain, pModule, pSymbolStream));
		}

		void ICorDebugManagedCallback.EditAndContinueRemap(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction, int fAccurate) {
			dbg.OnManagedCallbackFromAnyThread(new EditAndContinueRemapDebugCallbackEventArgs(pAppDomain, pThread, pFunction, fAccurate));
		}

		void ICorDebugManagedCallback.BreakpointSetError(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint, uint dwError) {
			dbg.OnManagedCallbackFromAnyThread(new BreakpointSetErrorDebugCallbackEventArgs(pAppDomain, pThread, pBreakpoint, dwError));
		}

		void ICorDebugManagedCallback2.FunctionRemapOpportunity(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pOldFunction, ICorDebugFunction pNewFunction, uint oldILOffset) {
			dbg.OnManagedCallbackFromAnyThread(new FunctionRemapOpportunityDebugCallbackEventArgs(pAppDomain, pThread, pOldFunction, pNewFunction, oldILOffset));
		}

		void ICorDebugManagedCallback2.CreateConnection(ICorDebugProcess pProcess, uint dwConnectionId, string pConnName) {
			dbg.OnManagedCallbackFromAnyThread(new CreateConnectionDebugCallbackEventArgs(pProcess, dwConnectionId, pConnName));
		}

		void ICorDebugManagedCallback2.ChangeConnection(ICorDebugProcess pProcess, uint dwConnectionId) {
			dbg.OnManagedCallbackFromAnyThread(new ChangeConnectionDebugCallbackEventArgs(pProcess, dwConnectionId));
		}

		void ICorDebugManagedCallback2.DestroyConnection(ICorDebugProcess pProcess, uint dwConnectionId) {
			dbg.OnManagedCallbackFromAnyThread(new DestroyConnectionDebugCallbackEventArgs(pProcess, dwConnectionId));
		}

		void ICorDebugManagedCallback2.Exception(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFrame pFrame, uint nOffset, CorDebugExceptionCallbackType dwEventType, CorDebugExceptionFlags dwFlags) {
			dbg.OnManagedCallbackFromAnyThread(new Exception2DebugCallbackEventArgs(pAppDomain, pThread, pFrame, nOffset, dwEventType, dwFlags));
		}

		void ICorDebugManagedCallback2.ExceptionUnwind(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, CorDebugExceptionUnwindCallbackType dwEventType, CorDebugExceptionFlags dwFlags) {
			dbg.OnManagedCallbackFromAnyThread(new ExceptionUnwindDebugCallbackEventArgs(pAppDomain, pThread, dwEventType, dwFlags));
		}

		void ICorDebugManagedCallback2.FunctionRemapComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction) {
			dbg.OnManagedCallbackFromAnyThread(new FunctionRemapCompleteDebugCallbackEventArgs(pAppDomain, pThread, pFunction));
		}

		void ICorDebugManagedCallback2.MDANotification(ICorDebugController pController, ICorDebugThread pThread, ICorDebugMDA pMDA) {
			dbg.OnManagedCallbackFromAnyThread(new MDANotificationDebugCallbackEventArgs(pController, pThread, pMDA));
		}

		void ICorDebugManagedCallback3.CustomNotification(ICorDebugThread pThread, ICorDebugAppDomain pAppDomain) {
			dbg.OnManagedCallbackFromAnyThread(new CustomNotificationDebugCallbackEventArgs(pThread, pAppDomain));
		}
	}
}
