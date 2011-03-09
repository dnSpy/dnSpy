// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Runtime.InteropServices;
using Debugger.Interop;
using Debugger.Interop.CorDebug;

namespace Debugger
{
	/// <summary>
	/// Handles all callbacks of a given process
	/// </summary>
	/// <remarks>
	/// Note that there can be a queued callback after almost any callback.
	/// In particular:
	///  - After 'break' there may be more callbacks
	///  - After EvalComplete there may be more callbacks (eg CreateThread from other thread)
	/// </remarks>
	class ManagedCallback
	{
		Process process;
		bool pauseOnNextExit;
		bool isInCallback = false;
		
		[Debugger.Tests.Ignore]
		public Process Process {
			get { return process; }
		}
		
		public bool IsInCallback {
			get { return isInCallback; }
		}
		
		public ManagedCallback(Process process)
		{
			this.process = process;
		}
		
		void EnterCallback(PausedReason pausedReason, string name, ICorDebugProcess pProcess)
		{
			isInCallback = true;
			
			process.TraceMessage("Callback: " + name);
			System.Diagnostics.Debug.Assert(process.CorProcess == pProcess);
			
			// After break is pressed we may receive some messages that were already queued
			if (process.IsPaused && process.PauseSession.PausedReason == PausedReason.ForcedBreak) {
				// TODO: This does not work well if exception if being processed and the user continues it
				process.TraceMessage("Processing post-break callback");
				// This compensates for the break call and we are in normal callback handling mode
				process.AsyncContinue(DebuggeeStateAction.Keep, new Thread[] {}, null);
				// Start of call back - create new pause session (as usual)
				process.NotifyPaused(pausedReason);
				// Make sure we stay pause after the callback is handled
				pauseOnNextExit = true;
				return;
			}
			
			if (process.IsRunning) {
				process.NotifyPaused(pausedReason);
				return;
			}
			
			throw new DebuggerException("Invalid state at the start of callback");
		}
		
		void EnterCallback(PausedReason pausedReason, string name, ICorDebugAppDomain pAppDomain)
		{
			EnterCallback(pausedReason, name, pAppDomain.GetProcess());
		}
		
		void EnterCallback(PausedReason pausedReason, string name, ICorDebugThread pThread)
		{
			EnterCallback(pausedReason, name, pThread.GetProcess());
			process.SelectedThread = process.Threads[pThread];
		}
		
		void ExitCallback()
		{
			bool hasQueuedCallbacks = process.CorProcess.HasQueuedCallbacks();
			if (hasQueuedCallbacks)
				process.TraceMessage("Process has queued callbacks");
			
			if (hasQueuedCallbacks) {
				// Exception has Exception2 queued after it
				process.AsyncContinue(DebuggeeStateAction.Keep, null, null);
			} else if (process.Evaluating) {
				// Ignore events during property evaluation
				process.AsyncContinue(DebuggeeStateAction.Keep, null, null);
			} else if (pauseOnNextExit) {
				if (process.Options.Verbose)
					process.TraceMessage("Callback exit: Paused");
				pauseOnNextExit = false;
				Pause();
			} else {
				process.AsyncContinue(DebuggeeStateAction.Keep, null, null);
			}
			
			isInCallback = false;
		}
		
		void Pause()
		{
			if (process.PauseSession.PausedReason == PausedReason.EvalComplete ||
			    process.PauseSession.PausedReason == PausedReason.ExceptionIntercepted) {
				// TODO: There might be qued callback after EvalComplete making this unrealiable
				process.DisableAllSteppers();
				process.CheckSelectedStackFrames();
				// Do not set selected stack frame
				// Do not raise events
			} else {
				// Raise the pause event outside the callback
				// Warning: Make sure that process in not resumed in the meantime
				process.Debugger.MTA2STA.AsyncCall(process.RaisePausedEvents);
				
				// The event might probably get called out of order when the process is running again
			}
		}
		
		
		#region Program folow control
		
		public void StepComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugStepper pStepper, CorDebugStepReason reason)
		{
			EnterCallback(PausedReason.StepComplete, "StepComplete (" + reason.ToString() + ")", pThread);
			
			Thread thread = process.Threads[pThread];
			Stepper stepper = process.GetStepper(pStepper);
			
			StackFrame currentStackFrame = process.SelectedThread.MostRecentStackFrame;
			process.TraceMessage(" - stopped at {0} because of {1}", currentStackFrame.MethodInfo.FullName, stepper.ToString());
			
			process.Steppers.Remove(stepper);
			stepper.OnStepComplete(reason);
			
			if (stepper.Ignore) {
				// The stepper is ignored
				process.TraceMessage(" - ignored");
			} else if (thread.CurrentStepIn != null &&
			           thread.CurrentStepIn.StackFrame.Equals(currentStackFrame) &&
			           thread.CurrentStepIn.IsInStepRanges((int)currentStackFrame.IP)) {
				Stepper.StepIn(currentStackFrame, thread.CurrentStepIn.StepRanges, "finishing step in");
				process.TraceMessage(" - finishing step in");
			} else if (currentStackFrame.MethodInfo.StepOver) {
				if (process.Options.EnableJustMyCode) {
					currentStackFrame.MethodInfo.MarkAsNonUserCode();
					process.TraceMessage(" - method {0} marked as non user code", currentStackFrame.MethodInfo.FullName);
					Stepper.StepIn(currentStackFrame, new int[] {0, int.MaxValue}, "seeking user code");
					process.TraceMessage(" - seeking user code");
				} else {
					Stepper.StepOut(currentStackFrame, "stepping out of non-user code");
					process.TraceMessage(" - stepping out of non-user code");
				}
			} else {
				// User-code method
				pauseOnNextExit = true;
				process.TraceMessage(" - pausing in user code");
			}
			
			ExitCallback();
		}
		
		// Warning! Marshaing of ICorBreakpoint fails in .NET 1.1
		public void Breakpoint(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint corBreakpoint)
		{
			EnterCallback(PausedReason.Breakpoint, "Breakpoint", pThread);
			
			Breakpoint breakpoint = process.Debugger.Breakpoints[corBreakpoint];
			// The event will be risen outside the callback
			process.BreakpointHitEventQueue.Enqueue(breakpoint);
			
			pauseOnNextExit = true;
			
			ExitCallback();
		}
		
		public void BreakpointSetError(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint, uint dwError)
		{
			EnterCallback(PausedReason.Other, "BreakpointSetError", pThread);
			
			ExitCallback();
		}
		
		public void Break(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
		{
			EnterCallback(PausedReason.Break, "Break", pThread);

			pauseOnNextExit = true;
			ExitCallback();
		}

		public void ControlCTrap(ICorDebugProcess pProcess)
		{
			EnterCallback(PausedReason.ControlCTrap, "ControlCTrap", pProcess);

			pauseOnNextExit = true;
			ExitCallback();
		}

		public void Exception(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int unhandled)
		{
			// Exception2 is used in .NET Framework 2.0
			
			if (process.DebuggeeVersion.StartsWith("v1.")) {
				// Forward the call to Exception2, which handles EnterCallback and ExitCallback
				ExceptionType exceptionType = (unhandled != 0) ? ExceptionType.Unhandled : ExceptionType.FirstChance;
				Exception2(pAppDomain, pThread, null, 0, (CorDebugExceptionCallbackType)exceptionType, 0);
			} else {
				// This callback should be ignored in v2 applications
				EnterCallback(PausedReason.Other, "Exception", pThread);
				
				ExitCallback();
			}
		}

		#endregion

		#region Various

		public void LogSwitch(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int lLevel, uint ulReason, string pLogSwitchName, string pParentName)
		{
			EnterCallback(PausedReason.Other, "LogSwitch", pThread);

			ExitCallback();
		}
		
		public void LogMessage(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int lLevel, string pLogSwitchName, string pMessage)
		{
			EnterCallback(PausedReason.Other, "LogMessage", pThread);

			process.OnLogMessage(new MessageEventArgs(process, lLevel, pMessage, pLogSwitchName));

			ExitCallback();
		}

		public void EditAndContinueRemap(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction, int fAccurate)
		{
			EnterCallback(PausedReason.Other, "EditAndContinueRemap", pThread);

			ExitCallback();
		}
		
		public void EvalException(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval corEval)
		{
			EnterCallback(PausedReason.EvalComplete, "EvalException", pThread);
			
			HandleEvalComplete(pAppDomain, pThread, corEval, true);
		}
		
		public void EvalComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval corEval)
		{
			EnterCallback(PausedReason.EvalComplete, "EvalComplete", pThread);
			
			HandleEvalComplete(pAppDomain, pThread, corEval, false);
		}
		
		void HandleEvalComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval corEval, bool exception)
		{
			// Let the eval know that the CorEval has finished
			Eval eval = process.ActiveEvals[corEval];
			eval.NotifyEvaluationComplete(!exception);
			process.ActiveEvals.Remove(eval);
			
			pauseOnNextExit = true;
			ExitCallback();
		}
		
		public void DebuggerError(ICorDebugProcess pProcess, int errorHR, uint errorCode)
		{
			EnterCallback(PausedReason.DebuggerError, "DebuggerError", pProcess);

			string errorText = String.Format("Debugger error: \nHR = 0x{0:X} \nCode = 0x{1:X}", errorHR, errorCode);
			
			if ((uint)errorHR == 0x80131C30) {
				errorText += "\n\nDebugging 64-bit processes is currently not supported.\n" +
					"If you are running a 64-bit system, this setting might help:\n" +
					"Project -> Project Options -> Compiling -> Target CPU = 32-bit Intel";
			}
			
			if (Environment.UserInteractive)
				System.Windows.Forms.MessageBox.Show(errorText);
			else
				throw new DebuggerException(errorText);

			try {
				pauseOnNextExit = true;
				ExitCallback();
			} catch (COMException) {
			} catch (InvalidComObjectException) {
				// ignore errors during shutdown after debugger error
			}
		}

		public void UpdateModuleSymbols(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule, IStream pSymbolStream)
		{
			EnterCallback(PausedReason.Other, "UpdateModuleSymbols", pAppDomain);
			
			Module module = process.Modules[pModule];
			if (module.CorModule is ICorDebugModule3 && module.IsDynamic) {
				// In .NET 4.0, we use the LoadClass callback to load dynamic modules
				// because it always works - UpdateModuleSymbols does not.
				//  - Simple dynamic code generation seems to trigger both callbacks.
				//  - IronPython for some reason causes just the LoadClass callback
				//    so we choose to rely on it out of the two.
			} else {
				// In .NET 2.0, this is the the only method and it works fine
				module.LoadSymbolsFromMemory(pSymbolStream);
			}
			
			ExitCallback();
		}

		#endregion

		#region Start of Application

		public void CreateProcess(ICorDebugProcess pProcess)
		{
			EnterCallback(PausedReason.Other, "CreateProcess", pProcess);

			// Process is added in NDebugger.Start
			// disable NGen
			ICorDebugProcess2 pProcess2 = pProcess as ICorDebugProcess2;
			if (pProcess2 != null && Process.DebugMode == DebugModeFlag.Debug) {
				try {
					pProcess2.SetDesiredNGENCompilerFlags((uint)CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION);
				} catch (COMException) {
					// we cannot set the NGEN flag => no evaluation for optimized code.
				}
			}
			
			ExitCallback();
		}

		public void CreateAppDomain(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
		{
			EnterCallback(PausedReason.Other, "CreateAppDomain", pAppDomain);

			pAppDomain.Attach();
			process.AppDomains.Add(new AppDomain(process, pAppDomain));

			ExitCallback();
		}

		public void LoadAssembly(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
		{
			EnterCallback(PausedReason.Other, "LoadAssembly", pAppDomain);

			ExitCallback();
		}

		public void LoadModule(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
		{
			EnterCallback(PausedReason.Other, "LoadModule " + pModule.GetName(), pAppDomain);
			
			Module module = new Module(process.AppDomains[pAppDomain], pModule);
			process.Modules.Add(module);
			
			ExitCallback();
		}
		
		public void NameChange(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
		{
			if (pAppDomain != null) {
				
				EnterCallback(PausedReason.Other, "NameChange: pAppDomain", pAppDomain);
				
				ExitCallback();
				
			}
			if (pThread != null) {
				
				EnterCallback(PausedReason.Other, "NameChange: pThread", pThread);
				
				Thread thread = process.Threads[pThread];
				thread.NotifyNameChanged();
				
				ExitCallback();
				
			}
		}
		
		public void CreateThread(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
		{
			// We can not use pThread since it has not been added yet
			// and we continue from this callback anyway
			EnterCallback(PausedReason.Other, "CreateThread " + pThread.GetID(), pAppDomain);
			
			Thread thread = new Thread(process, pThread);
			process.Threads.Add(thread);
			
			thread.CorThread.SetDebugState(process.NewThreadState);
			
			ExitCallback();
		}
		
		public void LoadClass(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
		{
			EnterCallback(PausedReason.Other, "LoadClass", pAppDomain);
			
			Module module = process.Modules[c.GetModule()];
			
			// Dynamic module has been extended - reload symbols to inlude new class
			module.LoadSymbolsDynamic();
			
			ExitCallback();
		}
		
		#endregion
		
		#region Exit of Application
		
		public void UnloadClass(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
		{
			EnterCallback(PausedReason.Other, "UnloadClass", pAppDomain);
			
			ExitCallback();
		}
		
		public void UnloadModule(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
		{
			EnterCallback(PausedReason.Other, "UnloadModule", pAppDomain);
			
			process.Modules.Remove(process.Modules[pModule]);
			
			ExitCallback();
		}
		
		public void UnloadAssembly(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
		{
			EnterCallback(PausedReason.Other, "UnloadAssembly", pAppDomain);
			
			ExitCallback();
		}
		
		public void ExitThread(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
		{
			// ICorDebugThread is still not dead and can be used for some operations
			if (process.Threads.Contains(pThread)) {
				EnterCallback(PausedReason.Other, "ExitThread " + pThread.GetID(), pThread);
				
				process.Threads[pThread].NotifyExited();
			} else {
				EnterCallback(PausedReason.Other, "ExitThread " + pThread.GetID(), process.CorProcess);
				
				// .NET 4.0 - It seems that the API is reporting exits of threads without announcing their creation.
				// TODO: Remove in next .NET 4.0 beta and investigate
				process.TraceMessage("ERROR: Thread does not exist " + pThread.GetID());
			}
			
			try {
				ExitCallback();
			} catch (COMException e) {
				// For some reason this sometimes happens in .NET 1.1
				process.TraceMessage("Continue failed in ExitThread callback: " + e.Message);
			}
		}
		
		public void ExitAppDomain(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
		{
			EnterCallback(PausedReason.Other, "ExitAppDomain", pAppDomain);
			
			process.AppDomains.Remove(process.AppDomains[pAppDomain]);
			
			ExitCallback();
		}
		
		public void ExitProcess(ICorDebugProcess pProcess)
		{
			// ExitProcess may be called at any time when debuggee is killed
			process.TraceMessage("Callback: ExitProcess");
			
			process.NotifyHasExited();
		}
		
		#endregion
		
		#region ICorDebugManagedCallback2 Members
		
		public void ChangeConnection(ICorDebugProcess pProcess, uint dwConnectionId)
		{
			EnterCallback(PausedReason.Other, "ChangeConnection", pProcess);
			
			ExitCallback();
		}

		public void CreateConnection(ICorDebugProcess pProcess, uint dwConnectionId, IntPtr pConnName)
		{
			EnterCallback(PausedReason.Other, "CreateConnection", pProcess);
			
			ExitCallback();
		}

		public void DestroyConnection(ICorDebugProcess pProcess, uint dwConnectionId)
		{
			EnterCallback(PausedReason.Other, "DestroyConnection", pProcess);
			
			ExitCallback();
		}

		public void Exception2(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFrame pFrame, uint nOffset, CorDebugExceptionCallbackType exceptionType, uint dwFlags)
		{
			EnterCallback(PausedReason.Exception, "Exception2 (type=" + exceptionType.ToString() + ")", pThread);
			
			// This callback is also called from Exception(...)!!!! (the .NET 1.1 version)
			// Watch out for the zeros and null!
			// Exception -> Exception2(pAppDomain, pThread, null, 0, exceptionType, 0);
			
			if ((ExceptionType)exceptionType == ExceptionType.Unhandled || process.PauseOnHandledException) {
				process.SelectedThread.CurrentException = new Exception(new Value(process.AppDomains[pAppDomain], process.SelectedThread.CorThread.GetCurrentException()).GetPermanentReference());
				process.SelectedThread.CurrentException_DebuggeeState = process.DebuggeeState;
				process.SelectedThread.CurrentExceptionType = (ExceptionType)exceptionType;
				process.SelectedThread.CurrentExceptionIsUnhandled = (ExceptionType)exceptionType == ExceptionType.Unhandled;
			
				pauseOnNextExit = true;
			}
			ExitCallback();
		}

		public void ExceptionUnwind(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, CorDebugExceptionUnwindCallbackType dwEventType, uint dwFlags)
		{
			EnterCallback(PausedReason.ExceptionIntercepted, "ExceptionUnwind", pThread);
			
			if (dwEventType == CorDebugExceptionUnwindCallbackType.DEBUG_EXCEPTION_INTERCEPTED) {
				pauseOnNextExit = true;
			}
			ExitCallback();
		}

		public void FunctionRemapComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction)
		{
			EnterCallback(PausedReason.Other, "FunctionRemapComplete", pThread);
			
			ExitCallback();
		}

		public void FunctionRemapOpportunity(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pOldFunction, ICorDebugFunction pNewFunction, uint oldILOffset)
		{
			EnterCallback(PausedReason.Other, "FunctionRemapOpportunity", pThread);
			
			ExitCallback();
		}

		/// <exception cref="Exception">Unknown callback argument</exception>
		public void MDANotification(ICorDebugController c, ICorDebugThread t, ICorDebugMDA mda)
		{
			if (c is ICorDebugAppDomain) {
				EnterCallback(PausedReason.Other, "MDANotification", (ICorDebugAppDomain)c);
			} else if (c is ICorDebugProcess){
				EnterCallback(PausedReason.Other, "MDANotification", (ICorDebugProcess)c);
			} else {
				throw new System.Exception("Unknown callback argument");
			}
			
			ExitCallback();
		}

		#endregion
	}
}
