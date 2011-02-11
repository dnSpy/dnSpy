// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

// Regular expresion:
// ^{\t*}{(:Ll| )*{:i} *\(((.# {:i}, |\))|())^6\)*}\n\t*\{(.|\n)@^\1\}
// Output: \1 - intention   \2 - declaration \3 - function name  \4-9 parameters

// Replace with:
// \1\2\n\1{\n\1\tGetProcessCallbackInterface(\4).\3(\4, \5, \6, \7, \8, \9);\n\1}

using System;
using System.Runtime.InteropServices;
using Debugger.Interop;
using Debugger.Interop.CorDebug;

namespace Debugger
{
	/// <summary>
	/// This class forwards the callback the the approprite process
	/// </summary>
	class ManagedCallbackSwitch
	{
		NDebugger debugger;
		
		public NDebugger Debugger {
			get {
				return debugger;
			}
		}
		
		public ManagedCallbackSwitch(NDebugger debugger)
		{
			this.debugger = debugger;
		}
		
		public ManagedCallback GetProcessCallbackInterface(string name, ICorDebugController c)
		{
			if (c is ICorDebugAppDomain) {
				return GetProcessCallbackInterface(name, (ICorDebugAppDomain)c);
			} else if (c is ICorDebugProcess){
				return GetProcessCallbackInterface(name, (ICorDebugProcess)c);
			} else {
				throw new System.Exception("Unknown callback argument");
			}
		}
		
		public ManagedCallback GetProcessCallbackInterface(string name, ICorDebugThread pThread)
		{
			ICorDebugProcess pProcess;
			try {
				pProcess = pThread.GetProcess();
			} catch (COMException e) {
				debugger.TraceMessage("Ignoring callback \"" + name + "\": " + e.Message);
				return null;
			}
			return GetProcessCallbackInterface(name, pProcess);
		}
		
		public ManagedCallback GetProcessCallbackInterface(string name, ICorDebugAppDomain pAppDomain)
		{
			ICorDebugProcess pProcess;
			try {
				pProcess = pAppDomain.GetProcess();
			} catch (COMException e) {
				debugger.TraceMessage("Ignoring callback \"" + name + "\": " + e.Message);
				return null;
			}
			return GetProcessCallbackInterface(name, pProcess);
		}
		
		public ManagedCallback GetProcessCallbackInterface(string name, ICorDebugProcess pProcess)
		{
			Process process;
			// We have to wait until the created process is added into the collection
			lock(debugger.ProcessIsBeingCreatedLock) {
				process = debugger.Processes[pProcess];
			}
			// Make *really* sure the process is not dead
			if (process == null) {
				debugger.TraceMessage("Ignoring callback \"" + name + "\": Process not found");
				return null;
			}
			if (process.HasExited) {
				debugger.TraceMessage("Ignoring callback \"" + name + "\": Process has exited");
				return null;
			}
			if (process.TerminateCommandIssued && !(name == "ExitProcess")) {
				debugger.TraceMessage("Ignoring callback \"" + name + "\": Terminate command was issued for the process");
				return null;
			}
			// Check that the process is not exited
			try {
				int isRunning = process.CorProcess.IsRunning();
			} catch (COMException e) {
				process.TraceMessage("Ignoring callback \"" + name + "\": " + e.Message);
				return null;
			}
			return process.CallbackInterface;
		}
		
		public void ExitProcess(ICorDebugProcess pProcess)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("ExitProcess", pProcess);
			if (managedCallback != null) {
				managedCallback.ExitProcess(pProcess);
			}
		}
		
		#region Program folow control
		
		public void StepComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugStepper pStepper, CorDebugStepReason reason)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("StepComplete", pAppDomain);
			if (managedCallback != null) {
				managedCallback.StepComplete(pAppDomain, pThread, pStepper, reason);
			}
		}
		
		// Do not pass the pBreakpoint parameter as ICorDebugBreakpoint - marshaling of it fails in .NET 1.1
		public void Breakpoint(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("Breakpoint", pAppDomain);
			if (managedCallback != null) {
				managedCallback.Breakpoint(pAppDomain, pThread, pBreakpoint);
			}
		}
		
		public void BreakpointSetError(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint, uint dwError)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("BreakpointSetError", pAppDomain);
			if (managedCallback != null) {
				managedCallback.BreakpointSetError(pAppDomain, pThread, pBreakpoint, dwError);
			}
		}
		
		public unsafe void Break(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("Break", pAppDomain);
			if (managedCallback != null) {
				managedCallback.Break(pAppDomain, pThread);
			}
		}

		public void ControlCTrap(ICorDebugProcess pProcess)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("ControlCTrap", pProcess);
			if (managedCallback != null) {
				managedCallback.ControlCTrap(pProcess);
			}
		}

		public unsafe void Exception(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int unhandled)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("Exception", pAppDomain);
			if (managedCallback != null) {
				managedCallback.Exception(pAppDomain, pThread, unhandled);
			}
		}

		#endregion

		#region Various

		public void LogSwitch(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int lLevel, uint ulReason, string pLogSwitchName, string pParentName)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("LogSwitch", pAppDomain);
			if (managedCallback != null) {
				managedCallback.LogSwitch(pAppDomain, pThread, lLevel, ulReason, pLogSwitchName, pParentName);
			}
		}
		
		public void LogMessage(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int lLevel, string pLogSwitchName, string pMessage)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("LogMessage", pAppDomain);
			if (managedCallback != null) {
				managedCallback.LogMessage(pAppDomain, pThread, lLevel, pLogSwitchName, pMessage);
			}
		}

		public void EditAndContinueRemap(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction, int fAccurate)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("EditAndContinueRemap", pAppDomain);
			if (managedCallback != null) {
				managedCallback.EditAndContinueRemap(pAppDomain, pThread, pFunction, fAccurate);
			}
		}
		
		public void EvalException(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval corEval)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("EvalException", pAppDomain);
			if (managedCallback != null) {
				managedCallback.EvalException(pAppDomain, pThread, corEval);
			}
		}
		
		public void EvalComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval corEval)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("EvalComplete", pAppDomain);
			if (managedCallback != null) {
				managedCallback.EvalComplete(pAppDomain, pThread, corEval);
			}
		}
		
		public void DebuggerError(ICorDebugProcess pProcess, int errorHR, uint errorCode)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("DebuggerError", pProcess);
			if (managedCallback != null) {
				managedCallback.DebuggerError(pProcess, errorHR, errorCode);
			}
		}

		public void UpdateModuleSymbols(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule, IStream pSymbolStream)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("UpdateModuleSymbols", pAppDomain);
			if (managedCallback != null) {
				managedCallback.UpdateModuleSymbols(pAppDomain, pModule, pSymbolStream);
			}
		}

		#endregion

		#region Start of Application

		public void CreateProcess(ICorDebugProcess pProcess)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("CreateProcess", pProcess);
			if (managedCallback != null) {
				managedCallback.CreateProcess(pProcess);
			}
		}

		public void CreateAppDomain(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("CreateAppDomain", pProcess);
			if (managedCallback != null) {
				managedCallback.CreateAppDomain(pProcess, pAppDomain);
			}
		}

		public void LoadAssembly(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("LoadAssembly", pAppDomain);
			if (managedCallback != null) {
				managedCallback.LoadAssembly(pAppDomain, pAssembly);
			}
		}

		public unsafe void LoadModule(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("LoadModule", pAppDomain);
			if (managedCallback != null) {
				managedCallback.LoadModule(pAppDomain, pModule);
			}
		}

		public void NameChange(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
		{
			ManagedCallback managedCallback = null;
			if (pAppDomain != null) {
				managedCallback = GetProcessCallbackInterface("NameChange", pAppDomain);
			}
			if (pThread != null) {
				managedCallback = GetProcessCallbackInterface("NameChange", pThread);
			}
			if (managedCallback != null) {
				managedCallback.NameChange(pAppDomain, pThread);
			}
		}

		public void CreateThread(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("CreateThread", pAppDomain);
			if (managedCallback != null) {
				managedCallback.CreateThread(pAppDomain, pThread);
			}
		}

		public void LoadClass(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("LoadClass", pAppDomain);
			if (managedCallback != null) {
				managedCallback.LoadClass(pAppDomain, c);
			}
		}

		#endregion

		#region Exit of Application

		public void UnloadClass(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("UnloadClass", pAppDomain);
			if (managedCallback != null) {
				managedCallback.UnloadClass(pAppDomain, c);
			}
		}

		public void UnloadModule(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("UnloadModule", pAppDomain);
			if (managedCallback != null) {
				managedCallback.UnloadModule(pAppDomain, pModule);
			}
		}

		public void UnloadAssembly(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("UnloadAssembly", pAppDomain);
			if (managedCallback != null) {
				managedCallback.UnloadAssembly(pAppDomain, pAssembly);
			}
		}

		public void ExitThread(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("ExitThread", pAppDomain);
			if (managedCallback != null) {
				managedCallback.ExitThread(pAppDomain, pThread);
			}
		}

		public void ExitAppDomain(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("ExitAppDomain", pProcess);
			if (managedCallback != null) {
				managedCallback.ExitAppDomain(pProcess, pAppDomain);
			}
		}
		
		#endregion
		
		#region ICorDebugManagedCallback2 Members
		
		public void ChangeConnection(ICorDebugProcess pProcess, uint dwConnectionId)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("ChangeConnection", pProcess);
			if (managedCallback != null) {
				managedCallback.ChangeConnection(pProcess, dwConnectionId);
			}
		}

		public void CreateConnection(ICorDebugProcess pProcess, uint dwConnectionId, IntPtr pConnName)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("CreateConnection", pProcess);
			if (managedCallback != null) {
				managedCallback.CreateConnection(pProcess, dwConnectionId, pConnName);
			}
		}

		public void DestroyConnection(ICorDebugProcess pProcess, uint dwConnectionId)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("DestroyConnection", pProcess);
			if (managedCallback != null) {
				managedCallback.DestroyConnection(pProcess, dwConnectionId);
			}
		}

		public void Exception2(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFrame pFrame, uint nOffset, CorDebugExceptionCallbackType exceptionType, uint dwFlags)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("Exception2", pAppDomain);
			if (managedCallback != null) {
				managedCallback.Exception2(pAppDomain, pThread, pFrame, nOffset, exceptionType, dwFlags);
			}
		}

		public void ExceptionUnwind(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, CorDebugExceptionUnwindCallbackType dwEventType, uint dwFlags)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("ExceptionUnwind", pAppDomain);
			if (managedCallback != null) {
				managedCallback.ExceptionUnwind(pAppDomain, pThread, dwEventType, dwFlags);
			}
		}

		public void FunctionRemapComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("FunctionRemapComplete", pAppDomain);
			if (managedCallback != null) {
				managedCallback.FunctionRemapComplete(pAppDomain, pThread, pFunction);
			}
		}

		public void FunctionRemapOpportunity(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pOldFunction, ICorDebugFunction pNewFunction, uint oldILOffset)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("FunctionRemapOpportunity", pAppDomain);
			if (managedCallback != null) {
				managedCallback.FunctionRemapOpportunity(pAppDomain, pThread, pOldFunction, pNewFunction, oldILOffset);
			}
		}

		public void MDANotification(ICorDebugController c, ICorDebugThread t, ICorDebugMDA mda)
		{
			ManagedCallback managedCallback = GetProcessCallbackInterface("MDANotification", c);
			if (managedCallback != null) {
				managedCallback.MDANotification(c, t, mda);
			}
		}

		#endregion
	}
}
