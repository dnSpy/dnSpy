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
using System.Runtime.InteropServices;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorDebugManagedCallback : ICorDebugManagedCallback, ICorDebugManagedCallback2, ICorDebugManagedCallback3 {
		readonly DnDebugger dbg;

		public CorDebugManagedCallback(DnDebugger dbg) {
			this.dbg = dbg;
		}

		static T I<T>(IntPtr ptr) where T : class {
			if (ptr == IntPtr.Zero)
				return null;
			return (T)Marshal.GetObjectForIUnknown(ptr);
		}

		void ICorDebugManagedCallback.Breakpoint(IntPtr pAppDomain, IntPtr pThread, IntPtr pBreakpoint) =>
			dbg.OnManagedCallbackFromAnyThread(() => new BreakpointDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), I<ICorDebugBreakpoint>(pBreakpoint)));

		void ICorDebugManagedCallback.StepComplete(IntPtr pAppDomain, IntPtr pThread, IntPtr pStepper, CorDebugStepReason reason) =>
			dbg.OnManagedCallbackFromAnyThread(() => new StepCompleteDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), I<ICorDebugStepper>(pStepper), reason));

		void ICorDebugManagedCallback.Break(IntPtr pAppDomain, IntPtr thread) =>
			dbg.OnManagedCallbackFromAnyThread(() => new BreakDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(thread)));

		void ICorDebugManagedCallback.Exception(IntPtr pAppDomain, IntPtr pThread, int unhandled) =>
			dbg.OnManagedCallbackFromAnyThread(() => new ExceptionDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), unhandled));

		void ICorDebugManagedCallback.EvalComplete(IntPtr pAppDomain, IntPtr pThread, IntPtr pEval) =>
			dbg.OnManagedCallbackFromAnyThread(() => new EvalCompleteDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), I<ICorDebugEval>(pEval)));

		void ICorDebugManagedCallback.EvalException(IntPtr pAppDomain, IntPtr pThread, IntPtr pEval) =>
			dbg.OnManagedCallbackFromAnyThread(() => new EvalExceptionDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), I<ICorDebugEval>(pEval)));

		void ICorDebugManagedCallback.CreateProcess(IntPtr pProcess) =>
			dbg.OnManagedCallbackFromAnyThread2(() => new CreateProcessDebugCallbackEventArgs(I<ICorDebugProcess>(pProcess)));

		void ICorDebugManagedCallback.ExitProcess(IntPtr pProcess) =>
			dbg.OnManagedCallbackFromAnyThread(() => new ExitProcessDebugCallbackEventArgs(I<ICorDebugProcess>(pProcess)));

		void ICorDebugManagedCallback.CreateThread(IntPtr pAppDomain, IntPtr thread) =>
			dbg.OnManagedCallbackFromAnyThread(() => new CreateThreadDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(thread)));

		void ICorDebugManagedCallback.ExitThread(IntPtr pAppDomain, IntPtr thread) =>
			dbg.OnManagedCallbackFromAnyThread(() => new ExitThreadDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(thread)));

		void ICorDebugManagedCallback.LoadModule(IntPtr pAppDomain, IntPtr pModule) =>
			dbg.OnManagedCallbackFromAnyThread2(() => new LoadModuleDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugModule>(pModule)));

		void ICorDebugManagedCallback.UnloadModule(IntPtr pAppDomain, IntPtr pModule) =>
			dbg.OnManagedCallbackFromAnyThread(() => new UnloadModuleDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugModule>(pModule)));

		void ICorDebugManagedCallback.LoadClass(IntPtr pAppDomain, IntPtr c) =>
			dbg.OnManagedCallbackFromAnyThread(() => new LoadClassDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugClass>(c)));

		void ICorDebugManagedCallback.UnloadClass(IntPtr pAppDomain, IntPtr c) =>
			dbg.OnManagedCallbackFromAnyThread(() => new UnloadClassDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugClass>(c)));

		void ICorDebugManagedCallback.DebuggerError(IntPtr pProcess, int errorHR, uint errorCode) =>
			dbg.OnManagedCallbackFromAnyThread(() => new DebuggerErrorDebugCallbackEventArgs(I<ICorDebugProcess>(pProcess), errorHR, errorCode));

		void ICorDebugManagedCallback.LogMessage(IntPtr pAppDomain, IntPtr pThread, LoggingLevelEnum lLevel, string pLogSwitchName, string pMessage) =>
			dbg.OnManagedCallbackFromAnyThread(() => new LogMessageDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), lLevel, pLogSwitchName, pMessage));

		void ICorDebugManagedCallback.LogSwitch(IntPtr pAppDomain, IntPtr pThread, LoggingLevelEnum lLevel, LogSwitchCallReason ulReason, string pLogSwitchName, string pParentName) =>
			dbg.OnManagedCallbackFromAnyThread(() => new LogSwitchDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), lLevel, ulReason, pLogSwitchName, pParentName));

		void ICorDebugManagedCallback.CreateAppDomain(IntPtr pProcess, IntPtr pAppDomain) =>
			dbg.OnManagedCallbackFromAnyThread2(() => new CreateAppDomainDebugCallbackEventArgs(I<ICorDebugProcess>(pProcess), I<ICorDebugAppDomain>(pAppDomain)));

		void ICorDebugManagedCallback.ExitAppDomain(IntPtr pProcess, IntPtr pAppDomain) =>
			dbg.OnManagedCallbackFromAnyThread(() => new ExitAppDomainDebugCallbackEventArgs(I<ICorDebugProcess>(pProcess), I<ICorDebugAppDomain>(pAppDomain)));

		void ICorDebugManagedCallback.LoadAssembly(IntPtr pAppDomain, IntPtr pAssembly) =>
			dbg.OnManagedCallbackFromAnyThread(() => new LoadAssemblyDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugAssembly>(pAssembly)));

		void ICorDebugManagedCallback.UnloadAssembly(IntPtr pAppDomain, IntPtr pAssembly) =>
			dbg.OnManagedCallbackFromAnyThread(() => new UnloadAssemblyDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugAssembly>(pAssembly)));

		void ICorDebugManagedCallback.ControlCTrap(IntPtr pProcess) =>
			dbg.OnManagedCallbackFromAnyThread(() => new ControlCTrapDebugCallbackEventArgs(I<ICorDebugProcess>(pProcess)));

		void ICorDebugManagedCallback.NameChange(IntPtr pAppDomain, IntPtr pThread) =>
			dbg.OnManagedCallbackFromAnyThread(() => new NameChangeDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread)));

		void ICorDebugManagedCallback.UpdateModuleSymbols(IntPtr pAppDomain, IntPtr pModule, IntPtr pSymbolStream) =>
			dbg.OnManagedCallbackFromAnyThread2(() => new UpdateModuleSymbolsDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugModule>(pModule), I<IStream>(pSymbolStream)));

		void ICorDebugManagedCallback.EditAndContinueRemap(IntPtr pAppDomain, IntPtr pThread, IntPtr pFunction, int fAccurate) =>
			dbg.OnManagedCallbackFromAnyThread(() => new EditAndContinueRemapDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), I<ICorDebugFunction>(pFunction), fAccurate));

		void ICorDebugManagedCallback.BreakpointSetError(IntPtr pAppDomain, IntPtr pThread, IntPtr pBreakpoint, uint dwError) =>
			dbg.OnManagedCallbackFromAnyThread(() => new BreakpointSetErrorDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), I<ICorDebugBreakpoint>(pBreakpoint), dwError));

		void ICorDebugManagedCallback2.FunctionRemapOpportunity(IntPtr pAppDomain, IntPtr pThread, IntPtr pOldFunction, IntPtr pNewFunction, uint oldILOffset) =>
			dbg.OnManagedCallbackFromAnyThread(() => new FunctionRemapOpportunityDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), I<ICorDebugFunction>(pOldFunction), I<ICorDebugFunction>(pNewFunction), oldILOffset));

		void ICorDebugManagedCallback2.CreateConnection(IntPtr pProcess, uint dwConnectionId, string pConnName) =>
			dbg.OnManagedCallbackFromAnyThread(() => new CreateConnectionDebugCallbackEventArgs(I<ICorDebugProcess>(pProcess), dwConnectionId, pConnName));

		void ICorDebugManagedCallback2.ChangeConnection(IntPtr pProcess, uint dwConnectionId) =>
			dbg.OnManagedCallbackFromAnyThread(() => new ChangeConnectionDebugCallbackEventArgs(I<ICorDebugProcess>(pProcess), dwConnectionId));

		void ICorDebugManagedCallback2.DestroyConnection(IntPtr pProcess, uint dwConnectionId) =>
			dbg.OnManagedCallbackFromAnyThread(() => new DestroyConnectionDebugCallbackEventArgs(I<ICorDebugProcess>(pProcess), dwConnectionId));

		void ICorDebugManagedCallback2.Exception(IntPtr pAppDomain, IntPtr pThread, IntPtr pFrame, uint nOffset, CorDebugExceptionCallbackType dwEventType, CorDebugExceptionFlags dwFlags) =>
			dbg.OnManagedCallbackFromAnyThread(() => new Exception2DebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), I<ICorDebugFrame>(pFrame), nOffset, dwEventType, dwFlags));

		void ICorDebugManagedCallback2.ExceptionUnwind(IntPtr pAppDomain, IntPtr pThread, CorDebugExceptionUnwindCallbackType dwEventType, CorDebugExceptionFlags dwFlags) =>
			dbg.OnManagedCallbackFromAnyThread(() => new ExceptionUnwindDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), dwEventType, dwFlags));

		void ICorDebugManagedCallback2.FunctionRemapComplete(IntPtr pAppDomain, IntPtr pThread, IntPtr pFunction) =>
			dbg.OnManagedCallbackFromAnyThread(() => new FunctionRemapCompleteDebugCallbackEventArgs(I<ICorDebugAppDomain>(pAppDomain), I<ICorDebugThread>(pThread), I<ICorDebugFunction>(pFunction)));

		void ICorDebugManagedCallback2.MDANotification(IntPtr pController, IntPtr pThread, IntPtr pMDA) =>
			dbg.OnManagedCallbackFromAnyThread(() => new MDANotificationDebugCallbackEventArgs(I<ICorDebugController>(pController), I<ICorDebugThread>(pThread), I<ICorDebugMDA>(pMDA)));

		void ICorDebugManagedCallback3.CustomNotification(IntPtr pThread, IntPtr pAppDomain) =>
			dbg.OnManagedCallbackFromAnyThread(() => new CustomNotificationDebugCallbackEventArgs(I<ICorDebugThread>(pThread), I<ICorDebugAppDomain>(pAppDomain)));
	}
}
