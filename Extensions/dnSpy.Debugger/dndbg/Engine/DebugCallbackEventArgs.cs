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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	public enum DebugCallbackKind {
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
		/// true if the debugged process should be paused
		/// </summary>
		public bool Pause => debuggerPauseStates.Count != 0;

		public DebuggerPauseState[] PauseStates => debuggerPauseStates.ToArray();
		readonly List<DebuggerPauseState> debuggerPauseStates = new List<DebuggerPauseState>();

		/// <summary>
		/// Type of event
		/// </summary>
		public abstract DebugCallbackKind Kind { get; }

		/// <summary>
		/// Debug controller
		/// </summary>
		public ICorDebugController CorDebugController { get; }

		protected DebugCallbackEventArgs(ICorDebugController ctrl) {
			CorDebugController = ctrl;
		}

		public void AddPauseReason(DebuggerPauseReason reason) => AddPauseState(new DebuggerPauseState(reason));

		public void AddPauseState(DebuggerPauseState state) {
			if (state == null)
				throw new ArgumentNullException(nameof(state));
			debuggerPauseStates.Add(state);
		}

		public DebuggerPauseState GetPauseState(DebuggerPauseReason reason) {
			foreach (var state in debuggerPauseStates) {
				if (state.Reason == reason)
					return state;
			}
			return null;
		}
	}

	public sealed class BreakpointDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.Breakpoint;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public ICorDebugBreakpoint Breakpoint { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);

		public CorFunctionBreakpoint CorFunctionBreakpoint {
			get {
				var fbp = Breakpoint as ICorDebugFunctionBreakpoint;
				return fbp == null ? null : new CorFunctionBreakpoint(fbp);
			}
		}

		public BreakpointDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			Breakpoint = pBreakpoint;
		}
	}

	public sealed class StepCompleteDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.StepComplete;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public ICorDebugStepper Stepper { get; }
		public CorDebugStepReason Reason { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);
		public CorStepper CorStepper => Stepper == null ? null : new CorStepper(Stepper);

		public StepCompleteDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugStepper pStepper, CorDebugStepReason reason)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			Stepper = pStepper;
			Reason = reason;
		}
	}

	public sealed class BreakDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.Break;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);

		public BreakDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = thread;
		}
	}

	public sealed class ExceptionDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.Exception;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public bool Unhandled { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);

		public ExceptionDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int unhandled)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			Unhandled = unhandled != 0;
		}
	}

	public abstract class EvalDebugCallbackEventArgs : DebugCallbackEventArgs {
		public bool CompletedSuccessfully => Kind == DebugCallbackKind.EvalComplete;
		public bool WasException => Kind == DebugCallbackKind.EvalException;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public ICorDebugEval Eval { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);
		public CorEval CorEval => Eval == null ? null : new CorEval(Eval);

		protected EvalDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			Eval = pEval;
		}
	}

	public sealed class EvalCompleteDebugCallbackEventArgs : EvalDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.EvalComplete;

		public EvalCompleteDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval)
			: base(pAppDomain, pThread, pEval) {
		}
	}

	public sealed class EvalExceptionDebugCallbackEventArgs : EvalDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.EvalException;

		public EvalExceptionDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval)
			: base(pAppDomain, pThread, pEval) {
		}
	}

	public abstract class ProcessDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugProcess Process { get; }
		public CorProcess CorProcess => Process == null ? null : new CorProcess(Process);

		protected ProcessDebugCallbackEventArgs(ICorDebugProcess pProcess)
			: base(pProcess) {
			Process = pProcess;
		}
	}

	public sealed class CreateProcessDebugCallbackEventArgs : ProcessDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.CreateProcess;

		public CreateProcessDebugCallbackEventArgs(ICorDebugProcess pProcess)
			: base(pProcess) {
		}
	}

	public sealed class ExitProcessDebugCallbackEventArgs : ProcessDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.ExitProcess;

		public ExitProcessDebugCallbackEventArgs(ICorDebugProcess pProcess)
			: base(pProcess) {
		}
	}

	public abstract class ThreadDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);

		protected ThreadDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = thread;
		}
	}

	public sealed class CreateThreadDebugCallbackEventArgs : ThreadDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.CreateThread;

		public CreateThreadDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
			: base(pAppDomain, thread) {
		}
	}

	public sealed class ExitThreadDebugCallbackEventArgs : ThreadDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.ExitThread;

		public ExitThreadDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
			: base(pAppDomain, thread) {
		}
	}

	public abstract class ModuleDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugModule Module { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorModule CorModule => Module == null ? null : new CorModule(Module);

		protected ModuleDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Module = pModule;
		}
	}

	public sealed class LoadModuleDebugCallbackEventArgs : ModuleDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.LoadModule;

		public LoadModuleDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
			: base(pAppDomain, pModule) {
		}
	}

	public sealed class UnloadModuleDebugCallbackEventArgs : ModuleDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.UnloadModule;

		public UnloadModuleDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
			: base(pAppDomain, pModule) {
		}
	}

	public abstract class ClassDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugClass Class { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorClass CorClass => Class == null ? null : new CorClass(Class);

		protected ClassDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Class = c;
		}
	}

	public sealed class LoadClassDebugCallbackEventArgs : ClassDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.LoadClass;

		public LoadClassDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
			: base(pAppDomain, c) {
		}
	}

	public sealed class UnloadClassDebugCallbackEventArgs : ClassDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.UnloadClass;

		public UnloadClassDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
			: base(pAppDomain, c) {
		}
	}

	public sealed class DebuggerErrorDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.DebuggerError;
		public ICorDebugProcess Process { get; }
		public int HError { get; }
		public uint ErrorCode { get; }

		public CorProcess CorProcess => Process == null ? null : new CorProcess(Process);

		public DebuggerErrorDebugCallbackEventArgs(ICorDebugProcess pProcess, int errorHR, uint errorCode)
			: base(pProcess) {
			Process = pProcess;
			HError = errorHR;
			ErrorCode = errorCode;
		}
	}

	public sealed class LogMessageDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.LogMessage;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public LoggingLevelEnum Level { get; }
		public string LowSwitchName { get; }
		public string Message { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);

		public LogMessageDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, LoggingLevelEnum lLevel, string pLogSwitchName, string pMessage)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			Level = lLevel;
			LowSwitchName = pLogSwitchName;
			Message = pMessage;
		}
	}

	public sealed class LogSwitchDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.LogSwitch;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public LoggingLevelEnum Level { get; }
		public LogSwitchCallReason Reason { get; }
		public string LowSwitchName { get; }
		public string ParentName { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);

		public LogSwitchDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, LoggingLevelEnum lLevel, LogSwitchCallReason ulReason, string pLogSwitchName, string pParentName)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			Level = lLevel;
			Reason = ulReason;
			LowSwitchName = pLogSwitchName;
			ParentName = pParentName;
		}
	}

	public abstract class AppDomainDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugProcess Process { get; }
		public ICorDebugAppDomain AppDomain { get; }
		public CorProcess CorProcess => Process == null ? null : new CorProcess(Process);
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);

		protected AppDomainDebugCallbackEventArgs(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
			: base(pProcess) {
			Process = pProcess;
			AppDomain = pAppDomain;
		}
	}

	public sealed class CreateAppDomainDebugCallbackEventArgs : AppDomainDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.CreateAppDomain;

		public CreateAppDomainDebugCallbackEventArgs(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
			: base(pProcess, pAppDomain) {
		}
	}

	public sealed class ExitAppDomainDebugCallbackEventArgs : AppDomainDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.ExitAppDomain;

		public ExitAppDomainDebugCallbackEventArgs(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
			: base(pProcess, pAppDomain) {
		}
	}

	public abstract class AssemblyDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugAssembly Assembly { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorAssembly CorAssembly => Assembly == null ? null : new CorAssembly(Assembly);

		protected AssemblyDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Assembly = pAssembly;
		}
	}

	public sealed class LoadAssemblyDebugCallbackEventArgs : AssemblyDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.LoadAssembly;

		public LoadAssemblyDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
			: base(pAppDomain, pAssembly) {
		}
	}

	public sealed class UnloadAssemblyDebugCallbackEventArgs : AssemblyDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.UnloadAssembly;

		public UnloadAssemblyDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
			: base(pAppDomain, pAssembly) {
		}
	}

	public sealed class ControlCTrapDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.ControlCTrap;
		public ICorDebugProcess Process { get; }
		public CorProcess CorProcess => Process == null ? null : new CorProcess(Process);

		public ControlCTrapDebugCallbackEventArgs(ICorDebugProcess pProcess)
			: base(pProcess) {
			Process = pProcess;
		}
	}

	public sealed class NameChangeDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.NameChange;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);

		public NameChangeDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
			: base(GetAppDomain(pAppDomain, pThread)) {
			AppDomain = pAppDomain;
			Thread = pThread;
		}

		static ICorDebugAppDomain GetAppDomain(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread) {
			if (pAppDomain == null && pThread != null)
				pThread.GetAppDomain(out pAppDomain);
			Debug.WriteLineIf(pAppDomain == null, "GetAppDomain: Could not get AppDomain");
			return pAppDomain;
		}
	}

	public sealed class UpdateModuleSymbolsDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.UpdateModuleSymbols;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugModule Module { get; }
		public IStream SymbolStream { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorModule CorModule => Module == null ? null : new CorModule(Module);

		public UpdateModuleSymbolsDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule, IStream pSymbolStream)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Module = pModule;
			SymbolStream = pSymbolStream;
		}
	}

	public sealed class EditAndContinueRemapDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.EditAndContinueRemap;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public ICorDebugFunction Function { get; }
		public bool Accurate { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);
		public CorFunction CorFunction => Function == null ? null : new CorFunction(Function);

		public EditAndContinueRemapDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction, int fAccurate)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			Function = pFunction;
			Accurate = fAccurate != 0;
		}
	}

	public sealed class BreakpointSetErrorDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.BreakpointSetError;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public ICorDebugBreakpoint Breakpoint { get; }
		public uint Error { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);

		public CorFunctionBreakpoint CorFunctionBreakpoint {
			get {
				var fbp = Breakpoint as ICorDebugFunctionBreakpoint;
				return fbp == null ? null : new CorFunctionBreakpoint(fbp);
			}
		}

		public BreakpointSetErrorDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint, uint dwError)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			Breakpoint = pBreakpoint;
			Error = dwError;
		}
	}

	public sealed class FunctionRemapOpportunityDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.FunctionRemapOpportunity;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public ICorDebugFunction OldFunction { get; }
		public ICorDebugFunction NewFunction { get; }
		public uint OldILOffset { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);
		public CorFunction OldCorFunction => OldFunction == null ? null : new CorFunction(OldFunction);
		public CorFunction NewCorFunction => NewFunction == null ? null : new CorFunction(NewFunction);

		public FunctionRemapOpportunityDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pOldFunction, ICorDebugFunction pNewFunction, uint oldILOffset)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			OldFunction = pOldFunction;
			NewFunction = pNewFunction;
			OldILOffset = oldILOffset;
		}
	}

	public abstract class ConnectionDebugCallbackEventArgs : DebugCallbackEventArgs {
		public ICorDebugProcess Process { get; }
		public uint Id { get; }
		public CorProcess CorProcess => Process == null ? null : new CorProcess(Process);

		protected ConnectionDebugCallbackEventArgs(ICorDebugProcess pProcess, uint dwConnectionId)
			: base(pProcess) {
			Process = pProcess;
			Id = dwConnectionId;
		}
	}

	public sealed class CreateConnectionDebugCallbackEventArgs : ConnectionDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.CreateConnection;
		public string Name { get; }

		public CreateConnectionDebugCallbackEventArgs(ICorDebugProcess pProcess, uint dwConnectionId, string pConnName)
			: base(pProcess, dwConnectionId) {
			Name = pConnName;
		}
	}

	public sealed class ChangeConnectionDebugCallbackEventArgs : ConnectionDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.ChangeConnection;

		public ChangeConnectionDebugCallbackEventArgs(ICorDebugProcess pProcess, uint dwConnectionId)
			: base(pProcess, dwConnectionId) {
		}
	}

	public sealed class DestroyConnectionDebugCallbackEventArgs : ConnectionDebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.DestroyConnection;

		public DestroyConnectionDebugCallbackEventArgs(ICorDebugProcess pProcess, uint dwConnectionId)
			: base(pProcess, dwConnectionId) {
		}
	}

	public sealed class Exception2DebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.Exception2;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public ICorDebugFrame Frame { get; }
		public uint Offset { get; }
		public CorDebugExceptionCallbackType EventType { get; }
		public CorDebugExceptionFlags Flags { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);
		public CorFrame CorFrame => Frame == null ? null : new CorFrame(Frame);

		public Exception2DebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFrame pFrame, uint nOffset, CorDebugExceptionCallbackType dwEventType, CorDebugExceptionFlags dwFlags)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			Frame = pFrame;
			Offset = nOffset;
			EventType = dwEventType;
			Flags = dwFlags;
		}
	}

	public sealed class ExceptionUnwindDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.ExceptionUnwind;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public CorDebugExceptionUnwindCallbackType EventType { get; }
		public CorDebugExceptionFlags Flags { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);

		public ExceptionUnwindDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, CorDebugExceptionUnwindCallbackType dwEventType, CorDebugExceptionFlags dwFlags)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			EventType = dwEventType;
			Flags = dwFlags;
		}
	}

	public sealed class FunctionRemapCompleteDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.FunctionRemapComplete;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public ICorDebugFunction Function { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);
		public CorFunction CorFunction => Function == null ? null : new CorFunction(Function);

		public FunctionRemapCompleteDebugCallbackEventArgs(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction)
			: base(pAppDomain) {
			AppDomain = pAppDomain;
			Thread = pThread;
			Function = pFunction;
		}
	}

	public sealed class MDANotificationDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.MDANotification;
		public ICorDebugController Controller { get; }
		public ICorDebugThread Thread { get; }
		public ICorDebugMDA MDA { get; }

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

		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);
		public CorMDA CorMDA => MDA == null ? null : new CorMDA(MDA);

		public MDANotificationDebugCallbackEventArgs(ICorDebugController pController, ICorDebugThread pThread, ICorDebugMDA pMDA)
			: base(pController) {
			Controller = pController;
			Thread = pThread;
			MDA = pMDA;
		}
	}

	public sealed class CustomNotificationDebugCallbackEventArgs : DebugCallbackEventArgs {
		public override DebugCallbackKind Kind => DebugCallbackKind.CustomNotification;
		public ICorDebugAppDomain AppDomain { get; }
		public ICorDebugThread Thread { get; }
		public CorAppDomain CorAppDomain => AppDomain == null ? null : new CorAppDomain(AppDomain);
		public CorThread CorThread => Thread == null ? null : new CorThread(Thread);

		public CustomNotificationDebugCallbackEventArgs(ICorDebugThread pThread, ICorDebugAppDomain pAppDomain)
			: base(pAppDomain) {
			Thread = pThread;
			AppDomain = pAppDomain;
		}
	}
}
