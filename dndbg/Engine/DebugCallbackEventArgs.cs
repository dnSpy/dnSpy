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
using System.Collections.Generic;
using System.Diagnostics;
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	public enum DebugCallbackType {
		Breakpoint,
		StepComplete,
		Break,
		Exception,
		EvalComplete,
		EvalException,
		CreateProcess,
		ExitProcess,
		CreateThread,
		ExitThread,
		LoadModule,
		UnloadModule,
		LoadClass,
		UnloadClass,
		DebuggerError,
		LogMessage,
		LogSwitch,
		CreateAppDomain,
		ExitAppDomain,
		LoadAssembly,
		UnloadAssembly,
		ControlCTrap,
		NameChange,
		UpdateModuleSymbols,
		EditAndContinueRemap,
		BreakpointSetError,
		FunctionRemapOpportunity,
		CreateConnection,
		ChangeConnection,
		DestroyConnection,
		Exception2,
		ExceptionUnwind,
		FunctionRemapComplete,
		MDANotification,
		CustomNotification,
	}

	public abstract class DebugCallbackEventArgs {
		/// <summary>
		/// true if the debugged process should be stopped
		/// </summary>
		public bool Stop {
			get { return debuggerStopStates.Count != 0; }
		}

		public DebuggerStopState[] StopStates {
			get { return debuggerStopStates.ToArray(); }
		}
		readonly List<DebuggerStopState> debuggerStopStates = new List<DebuggerStopState>();

		/// <summary>
		/// Type of event
		/// </summary>
		public abstract DebugCallbackType Type { get; }

		/// <summary>
		/// Debug controller
		/// </summary>
		public ICorDebugController CorDebugController {
			get { return ctrl; }
		}
		readonly ICorDebugController ctrl;

		protected DebugCallbackEventArgs(ICorDebugController ctrl) {
			this.ctrl = ctrl;
		}

		public void AddStopReason(DebuggerStopReason reason) {
			AddStopState(new DebuggerStopState(reason));
		}

		public void AddStopState(DebuggerStopState state) {
			if (state == null)
				throw new ArgumentNullException();
			debuggerStopStates.Add(state);
		}

		public DebuggerStopState GetStopState(DebuggerStopReason reason) {
			foreach (var state in debuggerStopStates) {
				if (state.Reason == reason)
					return state;
			}
			return null;
		}
	}

	public sealed class BreakpointDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.Breakpoint; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public ICorDebugBreakpoint Breakpoint { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public CorFunctionBreakpoint CorFunctionBreakpoint {
			get {
				var fbp = Breakpoint as ICorDebugFunctionBreakpoint;
				return fbp == null ? null : new CorFunctionBreakpoint(fbp);
			}
		}

		public BreakpointDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.Breakpoint = pBreakpoint;
		}
	}

	public sealed class StepCompleteDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.StepComplete; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public ICorDebugStepper Stepper { get; private set; }
		public CorDebugStepReason Reason { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public CorStepper CorStepper {
			get { return Stepper == null ? null : new CorStepper(Stepper); }
		}

		public StepCompleteDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugStepper pStepper, CorDebugStepReason reason)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.Stepper = pStepper;
			this.Reason = reason;
		}
	}

	public sealed class BreakDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.Break; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public BreakDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = thread;
		}
	}

	public sealed class ExceptionDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.Exception; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public bool Unhandled { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public ExceptionDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int unhandled)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.Unhandled = unhandled != 0;
		}
	}

	public abstract class EvalDebugCallbackEventArgs : DebugCallbackEventArgs {
		public bool CompletedSuccessfully {
			get { return Type == DebugCallbackType.EvalComplete; }
		}

		public bool WasException {
			get { return Type == DebugCallbackType.EvalException; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public ICorDebugEval Eval { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public CorEval CorEval {
			get { return Eval == null ? null : new CorEval(Eval); }
		}

		protected EvalDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.Eval = pEval;
		}
	}

	public sealed class EvalCompleteDebugCallbackEventArgs : EvalDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.EvalComplete; }
		}

		public EvalCompleteDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval)
			: base(pAppDomain, pThread, pEval) {
		}
	}

	public sealed class EvalExceptionDebugCallbackEventArgs : EvalDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.EvalException; }
		}

		public EvalExceptionDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval)
			: base(pAppDomain, pThread, pEval) {
		}
	}

	public abstract class ProcessDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugProcess Process { get; private set; }

		public CorProcess CorProcess {
			get { return Process == null ? null : new CorProcess(Process); }
		}

		protected ProcessDebugCallbackEventArgs(ICorDebugProcess pProcess)
			: base(pProcess) {
			this.Process = pProcess;
		}
	}

	public sealed class CreateProcessDebugCallbackEventArgs : ProcessDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.CreateProcess; }
		}

		public CreateProcessDebugCallbackEventArgs(ICorDebugProcess pProcess)
			: base(pProcess) {
		}
	}

	public sealed class ExitProcessDebugCallbackEventArgs : ProcessDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.ExitProcess; }
		}

		public ExitProcessDebugCallbackEventArgs(ICorDebugProcess pProcess)
			: base(pProcess) {
		}
	}

	public abstract class ThreadDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		protected ThreadDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = thread;
		}
	}

	public sealed class CreateThreadDebugCallbackEventArgs : ThreadDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.CreateThread; }
		}

		public CreateThreadDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
			: base(pAppDomain, thread) {
		}
	}

	public sealed class ExitThreadDebugCallbackEventArgs : ThreadDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.ExitThread; }
		}

		public ExitThreadDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
			: base(pAppDomain, thread) {
		}
	}

	public abstract class ModuleDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugModule Module { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorModule CorModule {
			get { return Module == null ? null : new CorModule(Module); }
		}

		protected ModuleDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Module = pModule;
		}
	}

	public sealed class LoadModuleDebugCallbackEventArgs : ModuleDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.LoadModule; }
		}

		public LoadModuleDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
			: base(pAppDomain, pModule) {
		}
	}

	public sealed class UnloadModuleDebugCallbackEventArgs : ModuleDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.UnloadModule; }
		}

		public UnloadModuleDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
			: base(pAppDomain, pModule) {
		}
	}

	public abstract class ClassDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugClass Class { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorClass CorClass {
			get { return Class == null ? null : new CorClass(Class); }
		}

		protected ClassDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Class = c;
		}
	}

	public sealed class LoadClassDebugCallbackEventArgs : ClassDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.LoadClass; }
		}

		public LoadClassDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
			: base(pAppDomain, c) {
		}
	}

	public sealed class UnloadClassDebugCallbackEventArgs : ClassDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.UnloadClass; }
		}

		public UnloadClassDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
			: base(pAppDomain, c) {
		}
	}

	public sealed class DebuggerErrorDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.DebuggerError; }
		}

		public ICorDebugProcess Process { get; private set; }
		public int HError { get; private set; }
		public uint ErrorCode { get; private set; }

		public CorProcess CorProcess {
			get { return Process == null ? null : new CorProcess(Process); }
		}

		public DebuggerErrorDebugCallbackEventArgs(ICorDebugProcess pProcess, int errorHR, uint errorCode)
			: base(pProcess) {
			this.Process = pProcess;
			this.HError = errorHR;
			this.ErrorCode = errorCode;
		}
	}

	public sealed class LogMessageDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.LogMessage; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public LoggingLevelEnum Level { get; private set; }
		public string LowSwitchName { get; private set; }
		public string Message { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public LogMessageDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, LoggingLevelEnum lLevel, string pLogSwitchName, string pMessage)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.Level = lLevel;
			this.LowSwitchName = pLogSwitchName;
			this.Message = pMessage;
		}
	}

	public sealed class LogSwitchDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.LogSwitch; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public LoggingLevelEnum Level { get; private set; }
		public LogSwitchCallReason Reason { get; private set; }
		public string LowSwitchName { get; private set; }
		public string ParentName { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public LogSwitchDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, LoggingLevelEnum lLevel, LogSwitchCallReason ulReason, string pLogSwitchName, string pParentName)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.Level = lLevel;
			this.Reason = ulReason;
			this.LowSwitchName = pLogSwitchName;
			this.ParentName = pParentName;
		}
	}

	public abstract class AppDomainDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugProcess Process { get; private set; }
		public ICorDebugAppDomain AppDomain { get; private set; }

		public CorProcess CorProcess {
			get { return Process == null ? null : new CorProcess(Process); }
		}

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		protected AppDomainDebugCallbackEventArgs(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
			: base(pProcess) {
			this.Process = pProcess;
			this.AppDomain = pAppDomain;
		}
	}

	public sealed class CreateAppDomainDebugCallbackEventArgs : AppDomainDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.CreateAppDomain; }
		}

		public CreateAppDomainDebugCallbackEventArgs(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
			: base(pProcess, pAppDomain) {
		}
	}

	public sealed class ExitAppDomainDebugCallbackEventArgs : AppDomainDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.ExitAppDomain; }
		}

		public ExitAppDomainDebugCallbackEventArgs(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
			: base(pProcess, pAppDomain) {
		}
	}

	public abstract class AssemblyDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugAssembly Assembly { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorAssembly CorAssembly {
			get { return Assembly == null ? null : new CorAssembly(Assembly); }
		}

		protected AssemblyDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Assembly = pAssembly;
		}
	}

	public sealed class LoadAssemblyDebugCallbackEventArgs : AssemblyDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.LoadAssembly; }
		}

		public LoadAssemblyDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
			: base(pAppDomain, pAssembly) {
		}
	}

	public sealed class UnloadAssemblyDebugCallbackEventArgs : AssemblyDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.UnloadAssembly; }
		}

		public UnloadAssemblyDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
			: base(pAppDomain, pAssembly) {
		}
	}

	public sealed class ControlCTrapDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.ControlCTrap; }
		}

		public ICorDebugProcess Process { get; private set; }

		public CorProcess CorProcess {
			get { return Process == null ? null : new CorProcess(Process); }
		}

		public ControlCTrapDebugCallbackEventArgs(ICorDebugProcess pProcess)
			: base(pProcess) {
			this.Process = pProcess;
		}
	}

	public sealed class NameChangeDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.NameChange; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public NameChangeDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
			: base(GetAppDomain(pAppDomain, pThread)) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
		}

		static ICorDebugAppDomain GetAppDomain(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread) {
			if (pAppDomain == null && pThread != null)
				pThread.GetAppDomain(out pAppDomain);
			Debug.WriteLineIf(pAppDomain == null, "GetAppDomain: Could not get AppDomain");
			return pAppDomain;
		}
	}

	public sealed class UpdateModuleSymbolsDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.UpdateModuleSymbols; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugModule Module { get; private set; }
		public IStream SymbolStream { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorModule CorModule {
			get { return Module == null ? null : new CorModule(Module); }
		}

		public UpdateModuleSymbolsDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule, IStream pSymbolStream)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Module = pModule;
			this.SymbolStream = pSymbolStream;
		}
	}

	public sealed class EditAndContinueRemapDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.EditAndContinueRemap; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public ICorDebugFunction Function { get; private set; }
		public bool Accurate { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public CorFunction CorFunction {
			get { return Function == null ? null : new CorFunction(Function); }
		}

		public EditAndContinueRemapDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction, int fAccurate)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.Function = pFunction;
			this.Accurate = fAccurate != 0;
		}
	}

	public sealed class BreakpointSetErrorDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.BreakpointSetError; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public ICorDebugBreakpoint Breakpoint { get; private set; }
		public uint Error { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public CorFunctionBreakpoint CorFunctionBreakpoint {
			get {
				var fbp = Breakpoint as ICorDebugFunctionBreakpoint;
				return fbp == null ? null : new CorFunctionBreakpoint(fbp);
			}
		}

		public BreakpointSetErrorDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint, uint dwError)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.Breakpoint = pBreakpoint;
			this.Error = dwError;
		}
	}

	public sealed class FunctionRemapOpportunityDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.FunctionRemapOpportunity; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public ICorDebugFunction OldFunction { get; private set; }
		public ICorDebugFunction NewFunction { get; private set; }
		public uint OldILOffset { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public CorFunction OldCorFunction {
			get { return OldFunction == null ? null : new CorFunction(OldFunction); }
		}

		public CorFunction NewCorFunction {
			get { return NewFunction == null ? null : new CorFunction(NewFunction); }
		}

		public FunctionRemapOpportunityDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pOldFunction, ICorDebugFunction pNewFunction, uint oldILOffset)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.OldFunction = pOldFunction;
			this.NewFunction = pNewFunction;
			this.OldILOffset = oldILOffset;
		}
	}

	public abstract class ConnectionDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugProcess Process { get; private set; }
		public uint Id { get; private set; }

		public CorProcess CorProcess {
			get { return Process == null ? null : new CorProcess(Process); }
		}

		protected ConnectionDebugCallbackEventArgs(ICorDebugProcess pProcess, uint dwConnectionId)
			: base(pProcess) {
			this.Process = pProcess;
			this.Id = dwConnectionId;
		}
	}

	public sealed class CreateConnectionDebugCallbackEventArgs : ConnectionDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.CreateConnection; }
		}

		public string Name { get; private set; }

		public CreateConnectionDebugCallbackEventArgs(ICorDebugProcess pProcess, uint dwConnectionId, string pConnName)
			: base(pProcess, dwConnectionId) {
			this.Name = pConnName;
		}
	}

	public sealed class ChangeConnectionDebugCallbackEventArgs : ConnectionDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.ChangeConnection; }
		}

		public ChangeConnectionDebugCallbackEventArgs(ICorDebugProcess pProcess, uint dwConnectionId)
			: base(pProcess, dwConnectionId) {
		}
	}

	public sealed class DestroyConnectionDebugCallbackEventArgs : ConnectionDebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.DestroyConnection; }
		}

		public DestroyConnectionDebugCallbackEventArgs(ICorDebugProcess pProcess, uint dwConnectionId)
			: base(pProcess, dwConnectionId) {
		}
	}

	public sealed class Exception2DebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.Exception2; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public ICorDebugFrame Frame { get; private set; }
		public uint Offset { get; private set; }
		public CorDebugExceptionCallbackType EventType { get; private set; }
		public CorDebugExceptionFlags Flags { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public CorFrame CorFrame {
			get { return Frame == null ? null : new CorFrame(Frame); }
		}

		public Exception2DebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFrame pFrame, uint nOffset, CorDebugExceptionCallbackType dwEventType, CorDebugExceptionFlags dwFlags)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.Frame = pFrame;
			this.Offset = nOffset;
			this.EventType = dwEventType;
			this.Flags = dwFlags;
		}
	}

	public sealed class ExceptionUnwindDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.ExceptionUnwind; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public CorDebugExceptionUnwindCallbackType EventType { get; private set; }
		public CorDebugExceptionFlags Flags { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public ExceptionUnwindDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, CorDebugExceptionUnwindCallbackType dwEventType, CorDebugExceptionFlags dwFlags)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.EventType = dwEventType;
			this.Flags = dwFlags;
		}
	}

	public sealed class FunctionRemapCompleteDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.FunctionRemapComplete; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public ICorDebugFunction Function { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public CorFunction CorFunction {
			get { return Function == null ? null : new CorFunction(Function); }
		}

		public FunctionRemapCompleteDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction)
			: base(pAppDomain) {
			this.AppDomain = pAppDomain;
			this.Thread = pThread;
			this.Function = pFunction;
		}
	}

	public sealed class MDANotificationDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.MDANotification; }
		}

		public ICorDebugController Controller { get; private set; }
		public ICorDebugThread Thread { get; private set; }
		public ICorDebugMDA MDA { get; private set; }

		public CorProcess CorProcess {
			get {
				var p = Controller as ICorDebugProcess;
				return p == null ? null : new CorProcess(p);
			}
		}

		public CorAppDomain CorAppDomain {
			get {
				var ad = Controller as ICorDebugAppDomain;
				return ad == null ? null : new CorAppDomain(ad);
			}
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public CorMDA CorMDA {
			get { return MDA == null ? null : new CorMDA(MDA); }
		}

		public MDANotificationDebugCallbackEventArgs(ICorDebugController pController, ICorDebugThread pThread, ICorDebugMDA pMDA)
			: base(pController) {
			this.Controller = pController;
			this.Thread = pThread;
			this.MDA = pMDA;
		}
	}

	public sealed class CustomNotificationDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackType Type {
			get { return DebugCallbackType.CustomNotification; }
		}

		public ICorDebugAppDomain AppDomain { get; private set; }
		public ICorDebugThread Thread { get; private set; }

		public CorAppDomain CorAppDomain {
			get { return AppDomain == null ? null : new CorAppDomain(AppDomain); }
		}

		public CorThread CorThread {
			get { return Thread == null ? null : new CorThread(Thread); }
		}

		public CustomNotificationDebugCallbackEventArgs(ICorDebugThread pThread, ICorDebugAppDomain pAppDomain)
			: base(pAppDomain) {
			this.Thread = pThread;
			this.AppDomain = pAppDomain;
		}
	}
}
