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

// CLR error codes: https://github.com/dotnet/coreclr/blob/master/src/inc/corerror.xml

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dndbg.Engine.COM.CorDebug;
using dndbg.Engine.COM.MetaHost;

namespace dndbg.Engine {
	public delegate void DebugCallbackEventHandler(DnDebugger dbg, DebugCallbackEventArgs e);

	/// <summary>
	/// Only call debugger methods in the dndbg thread since it's not thread safe
	/// </summary>
	public sealed class DnDebugger : IDisposable {
		readonly IDebugMessageDispatcher debugMessageDispatcher;
		readonly ICorDebug corDebug;
		readonly DebuggerCollection<ICorDebugProcess, DnProcess> processes;
		readonly DebugEventBreakpointList debugEventBreakpointList = new DebugEventBreakpointList();
		readonly BreakpointList<ILCodeBreakpoint> ilCodeBreakpointList = new BreakpointList<ILCodeBreakpoint>();
		readonly Dictionary<ICorDebugStepper, StepInfo> stepInfos = new Dictionary<ICorDebugStepper, StepInfo>();
		DebugOptions debugOptions;

		sealed class StepInfo {
			public readonly Action<DnDebugger, StepCompleteDebugCallbackEventArgs> OnCompleted;

			public StepInfo(Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action) {
				this.OnCompleted = action;
			}
		}

		public DebugOptions Options {
			get { DebugVerifyThread(); return debugOptions; }
			set { DebugVerifyThread(); debugOptions = value ?? new DebugOptions(); }
		}

		public DebuggerRunningState RunningState {
			get {
				DebugVerifyThread();
				if (hasTerminated)
					return DebuggerRunningState.Terminated;
				if (managedCallbackCounter != 0)
					return DebuggerRunningState.Stopped;
				if (processes.Count == 0)
					return DebuggerRunningState.Starting;
				return DebuggerRunningState.Running;
			}
		}

		public event EventHandler<DebuggerEventArgs> OnRunningStateChanged;
		void CallOnRunningStateChanged() {
			if (OnRunningStateChanged != null)
				OnRunningStateChanged(this, DebuggerEventArgs.Empty);
		}

		public DebugEventBreakpoint[] DebugEventBreakpoints {
			get { DebugVerifyThread(); return debugEventBreakpointList.Breakpoints; }
		}

		public IEnumerable<ILCodeBreakpoint> ILCodeBreakpoints {
			get { DebugVerifyThread(); return ilCodeBreakpointList.GetBreakpoints(); }
		}

		/// <summary>
		/// Gets the current state
		/// </summary>
		public CurrentDebuggerState Current {
			get { DebugVerifyThread(); return currentDebuggerState; }
		}
		CurrentDebuggerState currentDebuggerState;

		readonly int debuggerManagedThreadId;

		DnDebugger(ICorDebug corDebug, DebugOptions debugOptions, IDebugMessageDispatcher debugMessageDispatcher) {
			if (debugMessageDispatcher == null)
				throw new ArgumentNullException("debugMessageDispatcher");
			this.debuggerManagedThreadId = Thread.CurrentThread.ManagedThreadId;
			this.processes = new DebuggerCollection<ICorDebugProcess, DnProcess>(CreateDnProcess);
			this.debugMessageDispatcher = debugMessageDispatcher;
			this.corDebug = corDebug;
			this.currentDebuggerState = new CurrentDebuggerState();
			this.debugOptions = debugOptions ?? new DebugOptions();

			corDebug.Initialize();
			corDebug.SetManagedHandler(new CorDebugManagedCallback(this));
		}

		DnProcess CreateDnProcess(ICorDebugProcess comProcess, int id) {
			return new DnProcess(this, comProcess, id);
		}

		[Conditional("DEBUG")]
		internal void DebugVerifyThread() {
			Debug.Assert(Thread.CurrentThread.ManagedThreadId == this.debuggerManagedThreadId);
		}

		static ICorDebug CreateCorDebug(string debuggeeVersion) {
			var clsid = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");
			var riid = typeof(ICLRMetaHost).GUID;
			var mh = (ICLRMetaHost)NativeMethods.CLRCreateInstance(ref clsid, ref riid);

			riid = typeof(ICLRRuntimeInfo).GUID;
			var ri = (ICLRRuntimeInfo)mh.GetRuntime(debuggeeVersion, ref riid);

			clsid = new Guid("DF8395B5-A4BA-450B-A77C-A9A47762C520");
			riid = typeof(ICorDebug).GUID;
			return (ICorDebug)ri.GetInterface(ref clsid, ref riid);
		}

		public event DebugCallbackEventHandler DebugCallbackEvent;

		// Could be called from any thread
		internal void OnManagedCallbackFromAnyThread(DebugCallbackEventArgs e) {
			debugMessageDispatcher.ExecuteAsync(() => OnManagedCallbackInDebuggerThread(e));
		}

		// Called in our dndbg thread
		void OnManagedCallbackInDebuggerThread(DebugCallbackEventArgs e) {
			DebugVerifyThread();
			if (hasTerminated)
				return;
			managedCallbackCounter++;

			try {
				HandleManagedCallback(e);
				CheckBreakpoints(e);
				if (DebugCallbackEvent != null)
					DebugCallbackEvent(this, e);
			}
			catch (Exception ex) {
				Debug.WriteLine(string.Format("dndbg: EX:\n\n{0}", ex));
				currentDebuggerState = new CurrentDebuggerState();
				throw;
			}

			if (e.Stop) {
				currentDebuggerState.StopStates = e.StopStates;
				CallOnRunningStateChanged();
			}
			else {
				currentDebuggerState = new CurrentDebuggerState();

				if (e.Type != DebugCallbackType.ExitProcess) {
					if (Continue(e.CorDebugController))
						managedCallbackCounter--;
				}
				else
					managedCallbackCounter--;
			}
		}
		int managedCallbackCounter;

		bool Continue(ICorDebugController controller) {
			Debug.Assert(controller != null);
			if (controller == null)
				return false;

			int hr = controller.Continue(0);
			const int CORDBG_E_PROCESS_TERMINATED = unchecked((int)0x80131301);
			bool success = hr >= 0 || hr == CORDBG_E_PROCESS_TERMINATED;
			Debug.WriteLineIf(!success, string.Format("dndbg: ICorDebugController::Continue() failed: 0x{0:X8}", hr));
			return success;
		}

		/// <summary>
		/// true if <see cref="Continue()"/> can be called
		/// </summary>
		public bool CanContinue {
			get { DebugVerifyThread(); return RunningState == DebuggerRunningState.Stopped; }
		}

		/// <summary>
		/// Continue debugging the stopped process
		/// </summary>
		public void Continue() {
			DebugVerifyThread();
			if (!CanContinue)
				return;
			Debug.Assert(managedCallbackCounter > 0);
			if (managedCallbackCounter <= 0)
				return;

			var controller = currentDebuggerState.Controller;
			Debug.Assert(controller != null);
			if (controller == null)
				return;

			currentDebuggerState = new CurrentDebuggerState();
			while (managedCallbackCounter > 0) {
				if (!Continue(controller))
					return;
				managedCallbackCounter--;
			}

			CallOnRunningStateChanged();
		}

		/// <summary>
		/// true if we can step into, step out or step over
		/// </summary>
		public bool CanStep() {
			return CanStep(Current.Thread);
		}

		/// <summary>
		/// true if we can step into, step out or step over
		/// </summary>
		/// <param name="thread">Thread</param>
		public bool CanStep(DnThread thread) {
			DebugVerifyThread();
			return RunningState == DebuggerRunningState.Stopped && thread != null;
		}

		ICorDebugStepper CreateStepper(DnThread thread) {
			if (thread == null)
				return null;

			ICorDebugStepper stepper;
			if (thread.RawObject.CreateStepper(out stepper) < 0)
				return null;
			if (stepper.SetInterceptMask(debugOptions.StepperInterceptMask) < 0)
				return null;
			if (stepper.SetUnmappedStopMask(debugOptions.StepperUnmappedStopMask) < 0)
				return null;
			var stepper2 = stepper as ICorDebugStepper2;
			if (stepper2 != null) {
				if (stepper2.SetJMC(debugOptions.StepperJMC ? 1 : 0) < 0)
					return null;
			}

			return stepper;
		}

		/// <summary>
		/// Step out of current method in the current thread
		/// </summary>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOut(Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			return StepOut(Current.Thread, action);
		}

		/// <summary>
		/// Step out of current method in the selected thread
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOut(DnThread thread, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			if (!CanStep(thread))
				return false;

			var stepper = CreateStepper(thread);
			if (stepper == null)
				return false;
			if (stepper.StepOut() < 0)
				return false;

			stepInfos.Add(stepper, new StepInfo(action));
			return true;
		}

		/// <summary>
		/// Step into
		/// </summary>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepInto(Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepInto(Current.Thread, action);
		}

		/// <summary>
		/// Step into
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepInto(DnThread thread, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepIntoOver(thread, true, action);
		}

		/// <summary>
		/// Step over
		/// </summary>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOver(Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepOver(Current.Thread, action);
		}

		/// <summary>
		/// Step over
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOver(DnThread thread, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepIntoOver(thread, false, action);
		}

		bool StepIntoOver(DnThread thread, bool stepInto, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			if (!CanStep(thread))
				return false;

			var stepper = CreateStepper(thread);
			if (stepper == null)
				return false;
			if (stepper.Step(stepInto ? 1 : 0) < 0)
				return false;

			stepInfos.Add(stepper, new StepInfo(action));
			return true;
		}

		/// <summary>
		/// Step into
		/// </summary>
		/// <param name="ranges">Ranges to step over</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepInto(StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepInto(Current.Thread, ranges, action);
		}

		/// <summary>
		/// Step into
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="ranges">Ranges to step over</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepInto(DnThread thread, StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepIntoOver(thread, ranges, true, action);
		}

		/// <summary>
		/// Step over
		/// </summary>
		/// <param name="ranges">Ranges to step over</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOver(StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepOver(Current.Thread, ranges, action);
		}

		/// <summary>
		/// Step over
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="ranges">Ranges to step over</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOver(DnThread thread, StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepIntoOver(thread, ranges, false, action);
		}

		bool StepIntoOver(DnThread thread, StepRange[] ranges, bool stepInto, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			if (!CanStep(thread))
				return false;
			if (ranges == null)
				throw new ArgumentNullException();

			var stepper = CreateStepper(thread);
			if (stepper == null)
				return false;
			var rs = new uint[ranges.Length * 2];
			for (int i = 0; i < ranges.Length; i++) {
				rs[i * 2 + 0] = ranges[i].StartOffset;
				rs[i * 2 + 1] = ranges[i].EndOffset;
			}
			unsafe {
				fixed (uint* p = &rs[0]) {
					if (stepper.StepRange(stepInto ? 1 : 0, new IntPtr(p), (uint)ranges.Length) < 0)
						return false;
				}
			}

			stepInfos.Add(stepper, new StepInfo(action));
			return true;
		}

		void SetDefaultCurrentProcess() {
			var ps = GetProcesses();
			currentDebuggerState = new CurrentDebuggerState(ps.Length == 0 ? null : ps[0], null, null);
		}

		void InitializeCurrentDebuggerState(ICorDebugProcess comProcess, ICorDebugAppDomain comAppDomain, ICorDebugThread comThread) {
			if (comThread != null) {
				if (comProcess == null)
					comThread.GetProcess(out comProcess);
				if (comAppDomain == null)
					comThread.GetAppDomain(out comAppDomain);
			}

			if (comAppDomain != null) {
				if (comProcess == null)
					comAppDomain.GetProcess(out comProcess);
			}

			var process = TryGetValidProcess(comProcess);
			DnAppDomain appDomain;
			DnThread thread;
			if (processes != null) {
				appDomain = process.TryGetAppDomain(comAppDomain);
				thread = process.TryGetThread(comThread);
			}
			else {
				appDomain = null;
				thread = null;
			}

			if (process == null)
				SetDefaultCurrentProcess();
			else
				currentDebuggerState = new CurrentDebuggerState(process, appDomain, thread);
		}

		void InitializeCurrentDebuggerState(DnProcess process) {
			if (process == null) {
				SetDefaultCurrentProcess();
				return;
			}

			currentDebuggerState = new CurrentDebuggerState(process, null, null);
		}

		void OnProcessTerminated(DnProcess process) {
			if (process == null)
				return;

			foreach (var appDomain in process.GetAppDomains()) {
				OnAppDomainUnloaded(appDomain);
				process.AppDomainExited(appDomain.RawObject);
			}
		}

		void OnAppDomainUnloaded(DnAppDomain appDomain) {
			if (appDomain == null)
				return;
			foreach (var assembly in appDomain.GetAssemblies()) {
				OnAssemblyUnloaded(assembly);
				appDomain.AssemblyUnloaded(assembly.RawObject);
			}
		}

		void OnAssemblyUnloaded(DnAssembly assembly) {
			if (assembly == null)
				return;
			foreach (var module in assembly.GetModules()) {
				OnModuleUnloaded(module);
				assembly.ModuleUnloaded(module.RawObject);
			}
		}

		void OnModuleUnloaded(DnModule module) {
			RemoveModuleFromBreakpoints(module);
		}

		void RemoveModuleFromBreakpoints(DnModule module) {
			if (module == null)
				return;
			foreach (var bp in this.ilCodeBreakpointList.GetBreakpoints(module.SerializedDnModule))
				bp.RemoveModule(module);
		}

		void HandleManagedCallback(DebugCallbackEventArgs e) {
			bool b;
			DnProcess process;
			DnAppDomain appDomain;
			DnAssembly assembly;
			switch (e.Type) {
			case DebugCallbackType.Breakpoint:
				var bpArgs = (BreakpointDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, bpArgs.AppDomain, bpArgs.Thread);
				break;

			case DebugCallbackType.StepComplete:
				var scArgs = (StepCompleteDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, scArgs.AppDomain, scArgs.Thread);
				StepInfo stepInfo;
				bool calledStepInfoOnCompleted = false;
				if (stepInfos.TryGetValue(scArgs.Stepper, out stepInfo)) {
					stepInfos.Remove(scArgs.Stepper);
					if (stepInfo.OnCompleted != null) {
						calledStepInfoOnCompleted = true;
						stepInfo.OnCompleted(this, scArgs);
					}
				}
				if (!calledStepInfoOnCompleted)
					scArgs.AddStopState(new StepStopState(scArgs.Reason));
				break;

			case DebugCallbackType.Break:
				var bArgs = (BreakDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, bArgs.AppDomain, bArgs.Thread);
				break;

			case DebugCallbackType.Exception:
				var ex1Args = (ExceptionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, ex1Args.AppDomain, ex1Args.Thread);
				break;

			case DebugCallbackType.EvalComplete:
				var ecArgs = (EvalCompleteDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, ecArgs.AppDomain, ecArgs.Thread);
				break;

			case DebugCallbackType.EvalException:
				var eeArgs = (EvalExceptionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, eeArgs.AppDomain, eeArgs.Thread);
				break;

			case DebugCallbackType.CreateProcess:
				var cpArgs = (CreateProcessDebugCallbackEventArgs)e;
				process = TryAdd(cpArgs.Process);
				if (process != null) {
					process.EnableLogMessages(debugOptions.LogMessages);
					process.SetDesiredNGENCompilerFlags(debugOptions.JITCompilerFlags);
					process.SetWriteableMetadataUpdateMode(WriteableMetadataUpdateMode.AlwaysShowUpdates);
					process.EnableExceptionCallbacksOutsideOfMyCode(debugOptions.ExceptionCallbacksOutsideOfMyCode);
				}
				InitializeCurrentDebuggerState(process);
				break;

			case DebugCallbackType.ExitProcess:
				var epArgs = (ExitProcessDebugCallbackEventArgs)e;
				process = processes.TryGet(epArgs.Process);
				InitializeCurrentDebuggerState(process);
				if (process != null)
					process.SetHasExited();
				processes.Remove(epArgs.Process);
				OnProcessTerminated(process);
				if (processes.Count == 0)
					ProcessesTerminated();
				break;

			case DebugCallbackType.CreateThread:
				var ctArgs = (CreateThreadDebugCallbackEventArgs)e;
				process = TryGetValidProcess(ctArgs.Thread);
				if (process != null) {
					process.TryAdd(ctArgs.Thread);
					//TODO: ICorDebugThread::SetDebugState
				}
				InitializeCurrentDebuggerState(null, ctArgs.AppDomain, ctArgs.Thread);
				break;

			case DebugCallbackType.ExitThread:
				var etArgs = (ExitThreadDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, etArgs.AppDomain, etArgs.Thread);
				process = TryGetValidProcess(etArgs.Thread);
				if (process != null)
					process.ThreadExited(etArgs.Thread);
				break;

			case DebugCallbackType.LoadModule:
				var lmArgs = (LoadModuleDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, lmArgs.AppDomain, null);
				assembly = TryGetValidAssembly(lmArgs.AppDomain, lmArgs.Module);
				if (assembly != null) {
					var module = assembly.TryAdd(lmArgs.Module);
					module.RawObject.EnableJITDebugging(debugOptions.ModuleTrackJITInfo ? 1 : 0, debugOptions.ModuleAllowJitOptimizations ? 1 : 0);
					module.RawObject.EnableClassLoadCallbacks(debugOptions.ModuleClassLoadCallbacks ? 1 : 0);
					var mod2 = module.RawObject as ICorDebugModule2;
					if (mod2 != null) {
						mod2.SetJITCompilerFlags(debugOptions.JITCompilerFlags);
						mod2.SetJMCStatus(1, 0, IntPtr.Zero);
					}

					foreach (var bp in ilCodeBreakpointList.GetBreakpoints(module.SerializedDnModule))
						bp.AddBreakpoint(module);
				}
				break;

			case DebugCallbackType.UnloadModule:
				var umArgs = (UnloadModuleDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, umArgs.AppDomain, null);
				assembly = TryGetValidAssembly(umArgs.AppDomain, umArgs.Module);
				if (assembly != null) {
					var module = assembly.TryGetModule(umArgs.Module);
					OnModuleUnloaded(module);
					assembly.ModuleUnloaded(umArgs.Module);
				}
				break;

			case DebugCallbackType.LoadClass:
				var lcArgs = (LoadClassDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, lcArgs.AppDomain, null);
				break;

			case DebugCallbackType.UnloadClass:
				var ucArgs = (UnloadClassDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, ucArgs.AppDomain, null);
				break;

			case DebugCallbackType.DebuggerError:
				var deArgs = (DebuggerErrorDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(deArgs.Process, null, null);
				break;

			case DebugCallbackType.LogMessage:
				var lmsgArgs = (LogMessageDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, lmsgArgs.AppDomain, lmsgArgs.Thread);
				break;

			case DebugCallbackType.LogSwitch:
				var lsArgs = (LogSwitchDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, lsArgs.AppDomain, lsArgs.Thread);
				break;

			case DebugCallbackType.CreateAppDomain:
				var cadArgs = (CreateAppDomainDebugCallbackEventArgs)e;
				process = TryGetValidProcess(cadArgs.Process);
				if (process != null) {
					b = cadArgs.AppDomain.Attach() >= 0;
					Debug.WriteLineIf(!b, string.Format("CreateAppDomain: could not attach to AppDomain: {0:X8}", cadArgs.AppDomain.GetHashCode()));
					if (b)
						process.TryAdd(cadArgs.AppDomain);
				}
				InitializeCurrentDebuggerState(cadArgs.Process, cadArgs.AppDomain, null);
				break;

			case DebugCallbackType.ExitAppDomain:
				var eadArgs = (ExitAppDomainDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(eadArgs.Process, eadArgs.AppDomain, null);
				process = processes.TryGet(eadArgs.Process);
				if (process != null) {
					OnAppDomainUnloaded(process.TryGetAppDomain(eadArgs.AppDomain));
					process.AppDomainExited(eadArgs.AppDomain);
				}
				break;

			case DebugCallbackType.LoadAssembly:
				var laArgs = (LoadAssemblyDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, laArgs.AppDomain, null);
				appDomain = TryGetValidAppDomain(laArgs.AppDomain);
				if (appDomain != null)
					appDomain.TryAdd(laArgs.Assembly);
				break;

			case DebugCallbackType.UnloadAssembly:
				var uaArgs = (UnloadAssemblyDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, uaArgs.AppDomain, null);
				appDomain = TryGetAppDomain(uaArgs.AppDomain);
				if (appDomain != null) {
					OnAssemblyUnloaded(appDomain.TryGetAssembly(uaArgs.Assembly));
					appDomain.AssemblyUnloaded(uaArgs.Assembly);
				}
				break;

			case DebugCallbackType.ControlCTrap:
				var cctArgs = (ControlCTrapDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(cctArgs.Process, null, null);
				break;

			case DebugCallbackType.NameChange:
				var ncArgs = (NameChangeDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, ncArgs.AppDomain, ncArgs.Thread);
				if (ncArgs.AppDomain != null) {
					appDomain = TryGetValidAppDomain(ncArgs.AppDomain);
					if (appDomain != null)
						appDomain.NameChanged();
				}
				if (ncArgs.Thread != null) {
					var thread = TryGetValidThread(ncArgs.Thread);
					if (thread != null)
						thread.NameChanged();
				}
				break;

			case DebugCallbackType.UpdateModuleSymbols:
				var umsArgs = (UpdateModuleSymbolsDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, umsArgs.AppDomain, null);
				break;

			case DebugCallbackType.EditAndContinueRemap:
				var encrArgs = (EditAndContinueRemapDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, encrArgs.AppDomain, encrArgs.Thread);
				break;

			case DebugCallbackType.BreakpointSetError:
				var bpseArgs = (BreakpointSetErrorDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, bpseArgs.AppDomain, bpseArgs.Thread);
				break;

			case DebugCallbackType.FunctionRemapOpportunity:
				var froArgs = (FunctionRemapOpportunityDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, froArgs.AppDomain, froArgs.Thread);
				break;

			case DebugCallbackType.CreateConnection:
				var ccArgs = (CreateConnectionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(ccArgs.Process, null, null);
				break;

			case DebugCallbackType.ChangeConnection:
				var cc2Args = (ChangeConnectionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(cc2Args.Process, null, null);
				break;

			case DebugCallbackType.DestroyConnection:
				var dcArgs = (DestroyConnectionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(dcArgs.Process, null, null);
				break;

			case DebugCallbackType.Exception2:
				var ex2Args = (Exception2DebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, ex2Args.AppDomain, ex2Args.Thread);
				break;

			case DebugCallbackType.ExceptionUnwind:
				var euArgs = (ExceptionUnwindDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, euArgs.AppDomain, euArgs.Thread);
				break;

			case DebugCallbackType.FunctionRemapComplete:
				var frcArgs = (FunctionRemapCompleteDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, frcArgs.AppDomain, frcArgs.Thread);
				break;

			case DebugCallbackType.MDANotification:
				var mdanArgs = (MDANotificationDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(mdanArgs.Controller as ICorDebugProcess, mdanArgs.Controller as ICorDebugAppDomain, mdanArgs.Thread);
				break;

			case DebugCallbackType.CustomNotification:
				var cnArgs = (CustomNotificationDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(null, cnArgs.AppDomain, cnArgs.Thread);
				break;

			default:
				InitializeCurrentDebuggerState(null);
				Debug.Fail(string.Format("Unknown debug callback type: {0}", e.Type));
				break;
			}
		}

		void CheckBreakpoints(DebugCallbackEventArgs e) {
			var type = DebugEventBreakpoint.GetDebugEventBreakpointType(e);
			if (type != null) {
				foreach (var bp in DebugEventBreakpoints) {
					if (bp.IsEnabled && bp.EventType == type.Value && bp.Condition.Hit(new DebugEventBreakpointConditionContext(this, bp)))
						e.AddStopState(new DebugEventBreakpointStopState(bp));
				}
			}

			if (e.Type == DebugCallbackType.Breakpoint) {
				var bpArgs = (BreakpointDebugCallbackEventArgs)e;
				bool foundBp = false;
				foreach (var bp in ilCodeBreakpointList.GetBreakpoints()) {
					if (!bp.IsBreakpoint(bpArgs.Breakpoint))
						continue;

					foundBp = true;
					if (bp.IsEnabled && bp.Condition.Hit(new ILCodeBreakpointConditionContext(this, bp)))
						e.AddStopState(new ILCodeBreakpointStopState(bp));
					break;
				}
				Debug.WriteLineIf(!foundBp, "BP got triggered but no BP was found");
			}

			if (e.Type == DebugCallbackType.Break && !debugOptions.IgnoreBreakInstructions)
				e.AddStopReason(DebuggerStopReason.Break);

			//TODO: DebugCallbackType.BreakpointSetError
		}

		void ProcessesTerminated() {
			if (!hasTerminated) {
				hasTerminated = true;
				corDebug.Terminate();
				CallOnRunningStateChanged();
			}
		}
		bool hasTerminated = false;

		public static DnDebugger DebugProcess(DebugProcessOptions options) {
			if (options.DebugMessageDispatcher == null)
				throw new ArgumentException("DebugMessageDispatcher is null");

			var debuggeeVersion = options.DebuggeeVersion ?? DebuggeeVersionDetector.GetVersion(options.Filename);
			var dbg = new DnDebugger(CreateCorDebug(debuggeeVersion), options.DebugOptions, options.DebugMessageDispatcher);
			//TODO: This could fail so catch exceptions
			dbg.CreateProcess(options);
			return dbg;
		}

		void CreateProcess(DebugProcessOptions options) {
			ICorDebugProcess comProcess;
			try {
				var dwCreationFlags = options.ProcessCreationFlags ?? DebugProcessOptions.DefaultProcessCreationFlags;
				var si = new STARTUPINFO();
				si.cb = (uint)(4 * 1 + IntPtr.Size * 3 + 4 * 8 + 2 * 2 + IntPtr.Size * 4);
				var pi = new PROCESS_INFORMATION();
				corDebug.CreateProcess(options.Filename, options.CommandLine, IntPtr.Zero, IntPtr.Zero,
							options.InheritHandles ? 1 : 0, dwCreationFlags, IntPtr.Zero, options.CurrentDirectory,
							ref si, ref pi, CorDebugCreateProcessFlags.DEBUG_NO_SPECIAL_OPTIONS, out comProcess);
				// We don't need these
				NativeMethods.CloseHandle(pi.hProcess);
				NativeMethods.CloseHandle(pi.hThread);
			}
			catch {
				ProcessesTerminated();
				throw;
			}

			var process = TryAdd(comProcess);
			if (process != null)
				process.Initialize(options.Filename, options.CurrentDirectory, options.CommandLine);
		}

		public static DnDebugger Attach(uint pid) {
			//TODO:
			throw new NotImplementedException();
		}

		DnProcess TryAdd(ICorDebugProcess comProcess) {
			// This method is called twice, once from DebugProcess() and once from the CreateProcess
			// handler. It's possible that it's been terminated before DebugProcess() calls this method.

			// Check if it's terminated. Error should be 0x8013134F: CORDBG_E_OBJECT_NEUTERED
			int running;
			if (comProcess.IsRunning(out running) < 0)
				return null;

			return processes.Add(comProcess);
		}

		/// <summary>
		/// Gets all processes, sorted on the order they were created
		/// </summary>
		/// <returns></returns>
		public DnProcess[] GetProcesses() {
			DebugVerifyThread();
			var list = processes.GetAll();
			Array.Sort(list, (a, b) => a.IncrementedId.CompareTo(b.IncrementedId));
			return list;
		}

		/// <summary>
		/// Gets a process or null if it has exited
		/// </summary>
		/// <param name="comProcess">Process</param>
		/// <returns></returns>
		public DnProcess TryGetValidProcess(ICorDebugProcess comProcess) {
			DebugVerifyThread();
			var process = processes.TryGet(comProcess);
			if (process == null)
				return null;
			if (!process.CheckValid())
				return null;
			return process;
		}

		public DnProcess TryGetValidProcess(ICorDebugAppDomain comAppDomain) {
			DebugVerifyThread();
			if (comAppDomain == null)
				return null;
			ICorDebugProcess comProcess;
			int hr = comAppDomain.GetProcess(out comProcess);
			if (hr < 0)
				return null;
			return TryGetValidProcess(comProcess);
		}

		public DnProcess TryGetValidProcess(ICorDebugThread comThread) {
			DebugVerifyThread();
			if (comThread == null)
				return null;
			ICorDebugProcess comProcess;
			int hr = comThread.GetProcess(out comProcess);
			if (hr < 0)
				return null;
			return TryGetValidProcess(comProcess);
		}

		DnAppDomain TryGetAppDomain(ICorDebugAppDomain comAppDomain) {
			DebugVerifyThread();
			if (comAppDomain == null)
				return null;
			ICorDebugProcess comProcess;
			int hr = comAppDomain.GetProcess(out comProcess);
			if (hr < 0)
				return null;
			var process = processes.TryGet(comProcess);
			return process == null ? null : process.TryGetAppDomain(comAppDomain);
		}

		public DnAppDomain TryGetValidAppDomain(ICorDebugAppDomain comAppDomain) {
			DebugVerifyThread();
			if (comAppDomain == null)
				return null;
			ICorDebugProcess comProcess;
			int hr = comAppDomain.GetProcess(out comProcess);
			if (hr < 0)
				return null;
			return TryGetValidAppDomain(comProcess, comAppDomain);
		}

		public DnAppDomain TryGetValidAppDomain(ICorDebugProcess comProcess, ICorDebugAppDomain comAppDomain) {
			DebugVerifyThread();
			var process = TryGetValidProcess(comProcess);
			if (process == null)
				return null;
			return process.TryGetValidAppDomain(comAppDomain);
		}

		public DnAssembly TryGetValidAssembly(ICorDebugAppDomain comAppDomain, ICorDebugModule comModule) {
			DebugVerifyThread();
			if (comModule == null)
				return null;

			var appDomain = TryGetValidAppDomain(comAppDomain);
			if (appDomain == null)
				return null;

			ICorDebugAssembly comAssembly;
			int hr = comModule.GetAssembly(out comAssembly);
			if (hr < 0)
				return null;

			return appDomain.TryGetAssembly(comAssembly);
		}

		public DnThread TryGetValidThread(ICorDebugThread comThread) {
			DebugVerifyThread();
			var process = TryGetValidProcess(comThread);
			return process == null ? null : process.TryGetValidThread(comThread);
		}

		/// <summary>
		/// Creates a debug event breakpoint
		/// </summary>
		/// <param name="eventType">Debug event</param>
		/// <param name="cond">Condition</param>
		/// <returns></returns>
		public DebugEventBreakpoint CreateBreakpoint(DebugEventBreakpointType eventType, Predicate<BreakpointConditionContext> cond) {
			DebugVerifyThread();
			return CreateBreakpoint(eventType, new DelegateBreakpointCondition(cond));
		}

		/// <summary>
		/// Creates a debug event breakpoint
		/// </summary>
		/// <param name="eventType">Debug event</param>
		/// <param name="bpCond">Condition or null</param>
		/// <returns></returns>
		public DebugEventBreakpoint CreateBreakpoint(DebugEventBreakpointType eventType, IBreakpointCondition bpCond = null) {
			DebugVerifyThread();
			var bp = new DebugEventBreakpoint(eventType, bpCond);
			debugEventBreakpointList.Add(bp);
			return bp;
		}

		/// <summary>
		/// Creates an IL instruction breakpoint
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="ilOffset">IL offset</param>
		/// <param name="cond">Condition</param>
		/// <returns></returns>
		public ILCodeBreakpoint CreateBreakpoint(SerializedDnModule module, uint token, uint ilOffset, Predicate<BreakpointConditionContext> cond) {
			DebugVerifyThread();
			return CreateBreakpoint(module, token, ilOffset, new DelegateBreakpointCondition(cond));
		}

		/// <summary>
		/// Creates an IL instruction breakpoint
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="ilOffset">IL offset</param>
		/// <param name="bpCond">Condition or null</param>
		/// <returns></returns>
		public ILCodeBreakpoint CreateBreakpoint(SerializedDnModule module, uint token, uint ilOffset, IBreakpointCondition bpCond = null) {
			DebugVerifyThread();
			var bp = new ILCodeBreakpoint(module, token, ilOffset, bpCond);
			ilCodeBreakpointList.Add(module, bp);
			foreach (var dnMod in GetLoadedDnModules(module))
				bp.AddBreakpoint(dnMod);
			return bp;
		}

		IEnumerable<DnModule> GetLoadedDnModules(SerializedDnModule module) {
			foreach (var process in processes.GetAll()) {
				foreach (var appDomain in process.GetAppDomains()) {
					foreach (var assembly in appDomain.GetAssemblies()) {
						foreach (var dnMod in assembly.GetModules()) {
							if (dnMod.SerializedDnModule.Equals(module))
								yield return dnMod;
						}
					}
				}
			}
		}

		public void RemoveBreakpoint(Breakpoint bp) {
			DebugVerifyThread();
			var debp = bp as DebugEventBreakpoint;
			if (debp != null) {
				debugEventBreakpointList.Remove(debp);
				debp.OnRemoved();
				return;
			}

			var ilbp = bp as ILCodeBreakpoint;
			if (ilbp != null) {
				ilCodeBreakpointList.Remove(ilbp.Module, ilbp);
				ilbp.OnRemoved();
				return;
			}
		}

		~DnDebugger() {
			Dispose(false);
		}

		public void Dispose() {
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		void Dispose(bool disposing) {
			foreach (var process in processes.GetAll())
				process.RawObject.Terminate(uint.MaxValue);
		}
	}
}
