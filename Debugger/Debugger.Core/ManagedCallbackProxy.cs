// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Forms;
using Debugger.Interop;
using Debugger.Interop.CorDebug;

// Regular expresion:
// ^{\t*}{(:Ll| )*{:i} *\(((.# {:i}, |\))|())^6\)*}\n\t*\{(.|\n)@\t\}
// Output: \1 - intention   \2 - declaration \3 - function name  \4-9 parameters

// Replace with:
// \1\2\n\1{\n\1\tCallbackReceived("\3", new object[] {\4, \5, \6, \7, \8, \9});\n\1}
// \1\2\n\1{\n\1\tCall(delegate {\n\1\t     \trealCallback.\3(\n\1\t     \t\tMTA2STA.MarshalIntPtrTo(\4),\n\1\t     \t\tMTA2STA.MarshalIntPtrTo(\5),\n\1\t     \t\tMTA2STA.MarshalIntPtrTo(\6),\n\1\t     \t\tMTA2STA.MarshalIntPtrTo(\7),\n\1\t     \t\tMTA2STA.MarshalIntPtrTo(\8),\n\1\t     \t\tMTA2STA.MarshalIntPtrTo(\9),\n\1\t     \t);\n\1\t     });\n\1}

namespace Debugger
{
	/// <summary>
	/// This proxy marshals the callback to the appropriate thread
	/// </summary>
	class ManagedCallbackProxy : ICorDebugManagedCallback, ICorDebugManagedCallback2
	{
		NDebugger debugger;
		ManagedCallbackSwitch callbackSwitch;
		
		public NDebugger Debugger {
			get {
				return debugger;
			}
		}
		
		public ManagedCallbackProxy(NDebugger debugger, ManagedCallbackSwitch callbackSwitch)
		{
			this.debugger = debugger;
			this.callbackSwitch = callbackSwitch;
		}
		
		void Call(MethodInvoker callback)
		{
			debugger.MTA2STA.Call(callback);
		}
			
		public void StepComplete(System.IntPtr pAppDomain, System.IntPtr pThread, System.IntPtr pStepper, CorDebugStepReason reason)
		{
			Call(delegate {
			     	callbackSwitch.StepComplete(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugStepper>(pStepper),
			     		reason
			     	);
			     });
		}
		
		public void Break(System.IntPtr pAppDomain, System.IntPtr pThread)
		{
			Call(delegate {
			     	callbackSwitch.Break(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread)
			     	);
			     });
		}
		
		public void ControlCTrap(System.IntPtr pProcess)
		{
			Call(delegate {
			     	callbackSwitch.ControlCTrap(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugProcess>(pProcess)
			     	);
			     });
		}
		
		public void Exception(System.IntPtr pAppDomain, System.IntPtr pThread, int unhandled)
		{
			Call(delegate {
			     	callbackSwitch.Exception(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		unhandled
			     	);
			     });
		}
		
		public void Breakpoint(System.IntPtr pAppDomain, System.IntPtr pThread, System.IntPtr pBreakpoint)
		{
			Call(delegate {
			     	callbackSwitch.Breakpoint(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		// This fails in .NET 1.1:
			     		MTA2STA.MarshalIntPtrTo<ICorDebugBreakpoint>(pBreakpoint)
			     	);
			     });
		}
		
		public void CreateProcess(System.IntPtr pProcess)
		{
			Call(delegate {
			     	callbackSwitch.CreateProcess(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugProcess>(pProcess)
			     	);
			     });
		}
		
		public void CreateAppDomain(System.IntPtr pProcess, System.IntPtr pAppDomain)
		{
			Call(delegate {
			     	callbackSwitch.CreateAppDomain(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugProcess>(pProcess),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain)
			     	);
			     });
		}
		
		public void CreateThread(System.IntPtr pAppDomain, System.IntPtr pThread)
		{
			Call(delegate {
			     	callbackSwitch.CreateThread(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread)
			     	);
			     });
		}
		
		public void LoadAssembly(System.IntPtr pAppDomain, System.IntPtr pAssembly)
		{
			Call(delegate {
			     	callbackSwitch.LoadAssembly(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAssembly>(pAssembly)
			     	);
			     });
		}
		
		public void LoadModule(System.IntPtr pAppDomain, System.IntPtr pModule)
		{
			Call(delegate {
			     	callbackSwitch.LoadModule(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugModule>(pModule)
			     	);
			     });
		}
		
		public void NameChange(System.IntPtr pAppDomain, System.IntPtr pThread)
		{
			Call(delegate {
			     	callbackSwitch.NameChange(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread)
			     	);
			     });
		}
		
		public void LoadClass(System.IntPtr pAppDomain, System.IntPtr c)
		{
			Call(delegate {
			     	callbackSwitch.LoadClass(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugClass>(c)
			     	);
			     });
		}
		
		public void UnloadClass(System.IntPtr pAppDomain, System.IntPtr c)
		{
			Call(delegate {
			     	callbackSwitch.UnloadClass(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugClass>(c)
			     	);
			     });
		}
		
		public void ExitThread(System.IntPtr pAppDomain, System.IntPtr pThread)
		{
			Call(delegate {
			     	callbackSwitch.ExitThread(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread)
			     	);
			     });
		}
		
		public void UnloadModule(System.IntPtr pAppDomain, System.IntPtr pModule)
		{
			Call(delegate {
			     	callbackSwitch.UnloadModule(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugModule>(pModule)
			     	);
			     });
		}
		
		public void UnloadAssembly(System.IntPtr pAppDomain, System.IntPtr pAssembly)
		{
			Call(delegate {
			     	callbackSwitch.UnloadAssembly(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAssembly>(pAssembly)
			     	);
			     });
		}
		
		public void ExitAppDomain(System.IntPtr pProcess, System.IntPtr pAppDomain)
		{
			Call(delegate {
			     	callbackSwitch.ExitAppDomain(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugProcess>(pProcess),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain)
			     	);
			     });
		}
		
		public void ExitProcess(System.IntPtr pProcess)
		{
			Call(delegate {
			     	callbackSwitch.ExitProcess(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugProcess>(pProcess)
			     	);
			     });
		}
		
		public void BreakpointSetError(System.IntPtr pAppDomain, System.IntPtr pThread, System.IntPtr pBreakpoint, uint dwError)
		{
			Call(delegate {
			     	callbackSwitch.BreakpointSetError(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugBreakpoint>(pBreakpoint),
			     		dwError
			     	);
			     });
		}
		
		public void LogSwitch(System.IntPtr pAppDomain, System.IntPtr pThread, int lLevel, uint ulReason, System.IntPtr pLogSwitchName, System.IntPtr pParentName)
		{
			Call(delegate {
			     	callbackSwitch.LogSwitch(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		lLevel,
			     		ulReason,
			     		MTA2STA.MarshalIntPtrTo<string>(pLogSwitchName),
			     		MTA2STA.MarshalIntPtrTo<string>(pParentName)
			     	);
			     });
		}
		
		public void EvalException(System.IntPtr pAppDomain, System.IntPtr pThread, System.IntPtr pEval)
		{
			Call(delegate {
			     	callbackSwitch.EvalException(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugEval>(pEval)
			     	);
			     });
		}
		
		public void LogMessage(System.IntPtr pAppDomain, System.IntPtr pThread, int lLevel, System.IntPtr pLogSwitchName, System.IntPtr pMessage)
		{
			Call(delegate {
			     	callbackSwitch.LogMessage(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		lLevel,
			     		MTA2STA.MarshalIntPtrTo<string>(pLogSwitchName),
			     		MTA2STA.MarshalIntPtrTo<string>(pMessage)
			     	);
			     });
		}
		
		public void EditAndContinueRemap(System.IntPtr pAppDomain, System.IntPtr pThread, System.IntPtr pFunction, int fAccurate)
		{
			Call(delegate {
			     	callbackSwitch.EditAndContinueRemap(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugFunction>(pFunction),
			     		fAccurate
			     	);
			     });
		}
		
		public void EvalComplete(System.IntPtr pAppDomain, System.IntPtr pThread, System.IntPtr pEval)
		{
			Call(delegate {
			     	callbackSwitch.EvalComplete(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugEval>(pEval)
			     	);
			     });
		}
		
		public void DebuggerError(System.IntPtr pProcess, int errorHR, uint errorCode)
		{
			Call(delegate {
			     	callbackSwitch.DebuggerError(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugProcess>(pProcess),
			     		errorHR,
			     		errorCode
			     	);
			     });
		}
		
		public void UpdateModuleSymbols(System.IntPtr pAppDomain, System.IntPtr pModule, System.IntPtr pSymbolStream)
		{
			Call(delegate {
			     	callbackSwitch.UpdateModuleSymbols(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugModule>(pModule),
			     		MTA2STA.MarshalIntPtrTo<IStream>(pSymbolStream)
			     	);
			     });
		}



		#region ICorDebugManagedCallback2 Members

		public void ChangeConnection(IntPtr pProcess, uint dwConnectionId)
		{
			Call(delegate {
			     	callbackSwitch.ChangeConnection(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugProcess>(pProcess),
			     		dwConnectionId
			     	);
			     });
		}
		
		public void CreateConnection(IntPtr pProcess, uint dwConnectionId, IntPtr pConnName)
		{
			Call(delegate {
			     	callbackSwitch.CreateConnection(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugProcess>(pProcess),
			     		dwConnectionId,
			     		pConnName
			     	);
			     });
		}
		
		public void DestroyConnection(IntPtr pProcess, uint dwConnectionId)
		{
			Call(delegate {
			     	callbackSwitch.DestroyConnection(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugProcess>(pProcess),
			     		dwConnectionId
			     	);
			     });
		}
		
		public void Exception(IntPtr pAppDomain, IntPtr pThread, IntPtr pFrame, uint nOffset, CorDebugExceptionCallbackType dwEventType, uint dwFlags)
		{
			Call(delegate {
			     	callbackSwitch.Exception2(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugFrame>(pFrame),
			     		nOffset,
			     		dwEventType,
			     		dwFlags
			     	);
			     });
		}
		
		public void ExceptionUnwind(IntPtr pAppDomain, IntPtr pThread, CorDebugExceptionUnwindCallbackType dwEventType, uint dwFlags)
		{
			Call(delegate {
			     	callbackSwitch.ExceptionUnwind(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		dwEventType,
			     		dwFlags
			     	);
			     });
		}
		
		public void FunctionRemapComplete(IntPtr pAppDomain, IntPtr pThread, IntPtr pFunction)
		{
			Call(delegate {
			     	callbackSwitch.FunctionRemapComplete(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugFunction>(pFunction)
			     	);
			     });
		}
		
		public void FunctionRemapOpportunity(IntPtr pAppDomain, IntPtr pThread, IntPtr pOldFunction, IntPtr pNewFunction, uint oldILOffset)
		{
			Call(delegate {
			     	callbackSwitch.FunctionRemapOpportunity(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugAppDomain>(pAppDomain),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugFunction>(pOldFunction),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugFunction>(pNewFunction),
			     		oldILOffset
			     	);
			     });
		}
		
		public void MDANotification(IntPtr pController, IntPtr pThread, IntPtr pMDA)
		{
			Call(delegate {
			     	callbackSwitch.MDANotification(
			     		MTA2STA.MarshalIntPtrTo<ICorDebugController>(pController),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugThread>(pThread),
			     		MTA2STA.MarshalIntPtrTo<ICorDebugMDA>(pMDA)
			     	);
			     });
		}
		
		#endregion
	}

}
