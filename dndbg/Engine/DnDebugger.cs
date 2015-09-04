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
using System.Runtime.InteropServices;
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
		readonly BreakpointList<DnILCodeBreakpoint> ilCodeBreakpointList = new BreakpointList<DnILCodeBreakpoint>();
		readonly Dictionary<CorStepper, StepInfo> stepInfos = new Dictionary<CorStepper, StepInfo>();
		DebugOptions debugOptions;

		const int CORDBG_E_PROCESS_TERMINATED = unchecked((int)0x80131301);
		const int CORDBG_E_OBJECT_NEUTERED = unchecked((int)0x8013134F);

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

		public DebuggerProcessState ProcessState {
			get {
				DebugVerifyThread();
				if (hasTerminated)
					return DebuggerProcessState.Terminated;
				if (managedCallbackCounter != 0)
					return DebuggerProcessState.Stopped;
				if (!hasReceivedCreateProcessEvent)
					return DebuggerProcessState.Starting;
				return DebuggerProcessState.Running;
			}
		}
		bool hasReceivedCreateProcessEvent = false;

		public event EventHandler<DebuggerEventArgs> OnProcessStateChanged;
		void CallOnProcessStateChanged() {
			if (OnProcessStateChanged != null)
				OnProcessStateChanged(this, DebuggerEventArgs.Empty);
		}

		public DnDebugEventBreakpoint[] DebugEventBreakpoints {
			get { DebugVerifyThread(); return debugEventBreakpointList.Breakpoints; }
		}

		public IEnumerable<DnILCodeBreakpoint> ILCodeBreakpoints {
			get { DebugVerifyThread(); return ilCodeBreakpointList.GetBreakpoints(); }
		}

		/// <summary>
		/// Gets the last debugger state in <see cref="DebuggerStates"/>. This is never null even if
		/// <see cref="DebuggerStates"/> is empty.
		/// </summary>
		public DebuggerState Current {
			get {
				DebugVerifyThread();
				if (debuggerStates.Count == 0)
					return new DebuggerState(null);
				return debuggerStates[debuggerStates.Count - 1];
			}
		}

		/// <summary>
		/// All collected debugger states. It usually contains just one element, but can contain
		/// more if multiple CLR debugger events were queued, eg. an exception was caught or
		/// we stepped over a breakpoint (Step event + breakpoint hit event).
		/// </summary>
		public DebuggerState[] DebuggerStates {
			get { DebugVerifyThread(); return debuggerStates.ToArray(); }
		}
		readonly List<DebuggerState> debuggerStates = new List<DebuggerState>();

		readonly int debuggerManagedThreadId;

		DnDebugger(ICorDebug corDebug, DebugOptions debugOptions, IDebugMessageDispatcher debugMessageDispatcher) {
			if (debugMessageDispatcher == null)
				throw new ArgumentNullException("debugMessageDispatcher");
			this.debuggerManagedThreadId = Thread.CurrentThread.ManagedThreadId;
			this.processes = new DebuggerCollection<ICorDebugProcess, DnProcess>(CreateDnProcess);
			this.debugMessageDispatcher = debugMessageDispatcher;
			this.corDebug = corDebug;
			this.debugOptions = debugOptions ?? new DebugOptions();

			corDebug.Initialize();
			corDebug.SetManagedHandler(new CorDebugManagedCallback(this));
		}

		void ResetDebuggerStates() {
			debuggerStates.Clear();
		}

		void AddDebuggerState(DebuggerState debuggerState) {
			debuggerStates.Add(debuggerState);
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
		internal void OnManagedCallbackFromAnyThread(Func<DebugCallbackEventArgs> func) {
			debugMessageDispatcher.ExecuteAsync(() => OnManagedCallbackInDebuggerThread(func()));
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
				ResetDebuggerStates();
				throw;
			}

			Current.StopStates = e.StopStates;
			if (HasQueuedCallbacks(e))
				ContinueAndDecrementCounter(e);
			else if (ShouldStopQueued())
				CallOnProcessStateChanged();
			else {
				ResetDebuggerStates();
				ContinueAndDecrementCounter(e);
			}
		}
		int managedCallbackCounter;

		bool ShouldStopQueued() {
			foreach (var state in debuggerStates) {
				if (state.StopStates.Length != 0)
					return true;
			}
			return false;
		}

		void ContinueAndDecrementCounter(DebugCallbackEventArgs e) {
			if (e.Type != DebugCallbackType.ExitProcess) {
				if (Continue(e.CorDebugController))
					managedCallbackCounter--;
			}
			else
				managedCallbackCounter--;
		}

		bool HasQueuedCallbacks(DebugCallbackEventArgs e) {
			var thread = Current.Thread;
			if (thread == null)
				return false;
			int qcbs;
			int hr = e.CorDebugController.HasQueuedCallbacks(thread.CorThread.RawObject, out qcbs);
			return hr >= 0 && qcbs != 0;
		}

		bool Continue(ICorDebugController controller) {
			Debug.Assert(controller != null);
			if (controller == null)
				return false;

			int hr = controller.Continue(0);
			bool success = hr >= 0 || hr == CORDBG_E_PROCESS_TERMINATED || hr == CORDBG_E_OBJECT_NEUTERED;
			Debug.WriteLineIf(!success, string.Format("dndbg: ICorDebugController::Continue() failed: 0x{0:X8}", hr));
			return success;
		}

		/// <summary>
		/// true if <see cref="Continue()"/> can be called
		/// </summary>
		public bool CanContinue {
			get { DebugVerifyThread(); return ProcessState == DebuggerProcessState.Stopped; }
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

			var controller = Current.Controller;
			Debug.Assert(controller != null);
			if (controller == null)
				return;

			ResetDebuggerStates();
			while (managedCallbackCounter > 0) {
				if (!Continue(controller))
					return;
				managedCallbackCounter--;
			}

			CallOnProcessStateChanged();
		}

		/// <summary>
		/// true if we can step into, step out or step over
		/// </summary>
		public bool CanStep() {
			return CanStep(Current.ILFrame);
		}

		/// <summary>
		/// true if we can step into, step out or step over
		/// </summary>
		/// <param name="frame">Frame</param>
		public bool CanStep(CorFrame frame) {
			DebugVerifyThread();
			return ProcessState == DebuggerProcessState.Stopped && frame != null;
		}

		CorStepper CreateStepper(CorFrame frame) {
			if (frame == null)
				return null;

			var stepper = frame.CreateStepper();
			if (stepper == null)
				return null;
			if (!stepper.SetInterceptMask(debugOptions.StepperInterceptMask))
				return null;
			if (!stepper.SetUnmappedStopMask(debugOptions.StepperUnmappedStopMask))
				return null;
			if (!stepper.SetJMC(debugOptions.StepperJMC))
				return null;

			return stepper;
		}

		/// <summary>
		/// Step out of current method in the current IL frame
		/// </summary>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOut(Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			return StepOut(Current.ILFrame, action);
		}

		/// <summary>
		/// Step out of current method in the selected frame
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOut(CorFrame frame, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepOutInternal(frame, action);
		}

		bool StepOutInternal(CorFrame frame, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action) {
			if (!CanStep(frame))
				return false;

			var stepper = CreateStepper(frame);
			if (stepper == null)
				return false;
			if (!stepper.StepOut())
				return false;

			stepInfos.Add(stepper, new StepInfo(action));
			Continue();
			return true;
		}

		/// <summary>
		/// Step into
		/// </summary>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepInto(Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepInto(Current.ILFrame, action);
		}

		/// <summary>
		/// Step into
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepInto(CorFrame frame, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepIntoOver(frame, true, action);
		}

		/// <summary>
		/// Step over
		/// </summary>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOver(Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepOver(Current.ILFrame, action);
		}

		/// <summary>
		/// Step over
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOver(CorFrame frame, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepIntoOver(frame, false, action);
		}

		bool StepIntoOver(CorFrame frame, bool stepInto, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			if (!CanStep(frame))
				return false;

			var stepper = CreateStepper(frame);
			if (stepper == null)
				return false;
			if (!stepper.Step(stepInto))
				return false;

			stepInfos.Add(stepper, new StepInfo(action));
			Continue();
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
			return StepInto(Current.ILFrame, ranges, action);
		}

		/// <summary>
		/// Step into
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="ranges">Ranges to step over</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepInto(CorFrame frame, StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepIntoOver(frame, ranges, true, action);
		}

		/// <summary>
		/// Step over
		/// </summary>
		/// <param name="ranges">Ranges to step over</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOver(StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepOver(Current.ILFrame, ranges, action);
		}

		/// <summary>
		/// Step over
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="ranges">Ranges to step over</param>
		/// <param name="action">Delegate to call when completed or null</param>
		/// <returns></returns>
		public bool StepOver(CorFrame frame, StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			DebugVerifyThread();
			return StepIntoOver(frame, ranges, false, action);
		}

		bool StepIntoOver(CorFrame frame, StepRange[] ranges, bool stepInto, Action<DnDebugger, StepCompleteDebugCallbackEventArgs> action = null) {
			if (ranges == null)
				return StepIntoOver(frame, stepInto, action);
			if (!CanStep(frame))
				return false;

			var stepper = CreateStepper(frame);
			if (stepper == null)
				return false;
			if (!stepper.StepRange(stepInto, ranges))
				return false;

			stepInfos.Add(stepper, new StepInfo(action));
			Continue();
			return true;
		}

		CorFrame GetRunToCallee(CorFrame frame) {
			if (!CanStep(frame))
				return null;
			if (frame == null)
				return null;
			if (!frame.IsILFrame)
				return null;
			var callee = frame.Callee;
			if (callee == null)
				return null;
			if (!callee.IsILFrame)
				return null;

			return callee;
		}

		public bool CanRunTo(CorFrame frame) {
			DebugVerifyThread();
			return GetRunToCallee(frame) != null;
		}

		public bool RunTo(CorFrame frame) {
			DebugVerifyThread();
			var callee = GetRunToCallee(frame);
			if (callee == null)
				return false;

			return StepOutInternal(callee, null);
		}

		void SetDefaultCurrentProcess(DebugCallbackEventArgs e) {
			var ps = GetProcesses();
			AddDebuggerState(new DebuggerState(e, ps.Length == 0 ? null : ps[0], null, null));
		}

		void InitializeCurrentDebuggerState(DebugCallbackEventArgs e, ICorDebugProcess comProcess, ICorDebugAppDomain comAppDomain, ICorDebugThread comThread) {
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
				SetDefaultCurrentProcess(e);
			else
				AddDebuggerState(new DebuggerState(e, process, appDomain, thread));
		}

		void InitializeCurrentDebuggerState(DebugCallbackEventArgs e, DnProcess process) {
			if (process == null) {
				SetDefaultCurrentProcess(e);
				return;
			}

			AddDebuggerState(new DebuggerState(e, process, null, null));
		}

		void OnProcessTerminated(DnProcess process) {
			if (process == null)
				return;

			foreach (var appDomain in process.GetAppDomains()) {
				OnAppDomainUnloaded(appDomain);
				process.AppDomainExited(appDomain.CorAppDomain.RawObject);
			}
		}

		void OnAppDomainUnloaded(DnAppDomain appDomain) {
			if (appDomain == null)
				return;
			foreach (var assembly in appDomain.GetAssemblies()) {
				OnAssemblyUnloaded(assembly);
				appDomain.AssemblyUnloaded(assembly.CorAssembly.RawObject);
			}
		}

		void OnAssemblyUnloaded(DnAssembly assembly) {
			if (assembly == null)
				return;
			foreach (var module in assembly.GetModules()) {
				OnModuleUnloaded(module);
				assembly.ModuleUnloaded(module.CorModule.RawObject);
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
				InitializeCurrentDebuggerState(e, null, bpArgs.AppDomain, bpArgs.Thread);
				break;

			case DebugCallbackType.StepComplete:
				var scArgs = (StepCompleteDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, scArgs.AppDomain, scArgs.Thread);
				StepInfo stepInfo;
				bool calledStepInfoOnCompleted = false;
				var stepperKey = new CorStepper(scArgs.Stepper);
				if (stepInfos.TryGetValue(stepperKey, out stepInfo)) {
					stepInfos.Remove(stepperKey);
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
				InitializeCurrentDebuggerState(e, null, bArgs.AppDomain, bArgs.Thread);
				break;

			case DebugCallbackType.Exception:
				var ex1Args = (ExceptionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ex1Args.AppDomain, ex1Args.Thread);
				break;

			case DebugCallbackType.EvalComplete:
				var ecArgs = (EvalCompleteDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ecArgs.AppDomain, ecArgs.Thread);
				break;

			case DebugCallbackType.EvalException:
				var eeArgs = (EvalExceptionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, eeArgs.AppDomain, eeArgs.Thread);
				break;

			case DebugCallbackType.CreateProcess:
				var cpArgs = (CreateProcessDebugCallbackEventArgs)e;
				hasReceivedCreateProcessEvent = true;
				process = TryAdd(cpArgs.Process);
				if (process != null) {
					process.CorProcess.EnableLogMessages(debugOptions.LogMessages);
					process.CorProcess.DesiredNGENCompilerFlags = debugOptions.JITCompilerFlags;
					process.CorProcess.SetWriteableMetadataUpdateMode(WriteableMetadataUpdateMode.AlwaysShowUpdates);
					process.CorProcess.EnableExceptionCallbacksOutsideOfMyCode(debugOptions.ExceptionCallbacksOutsideOfMyCode);
					process.CorProcess.EnableNGENPolicy(debugOptions.NGENPolicy);
				}
				InitializeCurrentDebuggerState(e, process);
				break;

			case DebugCallbackType.ExitProcess:
				var epArgs = (ExitProcessDebugCallbackEventArgs)e;
				process = processes.TryGet(epArgs.Process);
				InitializeCurrentDebuggerState(e, process);
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
				InitializeCurrentDebuggerState(e, null, ctArgs.AppDomain, ctArgs.Thread);
				break;

			case DebugCallbackType.ExitThread:
				var etArgs = (ExitThreadDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, etArgs.AppDomain, etArgs.Thread);
				process = TryGetValidProcess(etArgs.Thread);
				if (process != null)
					process.ThreadExited(etArgs.Thread);
				break;

			case DebugCallbackType.LoadModule:
				var lmArgs = (LoadModuleDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, lmArgs.AppDomain, null);
				assembly = TryGetValidAssembly(lmArgs.AppDomain, lmArgs.Module);
				if (assembly != null) {
					var module = assembly.TryAdd(lmArgs.Module);
					module.CorModule.EnableJITDebugging(debugOptions.ModuleTrackJITInfo, debugOptions.ModuleAllowJitOptimizations);
					module.CorModule.EnableClassLoadCallbacks(debugOptions.ModuleClassLoadCallbacks);
					module.CorModule.JITCompilerFlags = debugOptions.JITCompilerFlags;
					module.CorModule.SetJMCStatus(true);

					foreach (var bp in ilCodeBreakpointList.GetBreakpoints(module.SerializedDnModule))
						bp.AddBreakpoint(module);
				}
				break;

			case DebugCallbackType.UnloadModule:
				var umArgs = (UnloadModuleDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, umArgs.AppDomain, null);
				assembly = TryGetValidAssembly(umArgs.AppDomain, umArgs.Module);
				if (assembly != null) {
					var module = assembly.TryGetModule(umArgs.Module);
					OnModuleUnloaded(module);
					assembly.ModuleUnloaded(umArgs.Module);
				}
				break;

			case DebugCallbackType.LoadClass:
				var lcArgs = (LoadClassDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, lcArgs.AppDomain, null);
				break;

			case DebugCallbackType.UnloadClass:
				var ucArgs = (UnloadClassDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ucArgs.AppDomain, null);
				break;

			case DebugCallbackType.DebuggerError:
				var deArgs = (DebuggerErrorDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, deArgs.Process, null, null);
				break;

			case DebugCallbackType.LogMessage:
				var lmsgArgs = (LogMessageDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, lmsgArgs.AppDomain, lmsgArgs.Thread);
				break;

			case DebugCallbackType.LogSwitch:
				var lsArgs = (LogSwitchDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, lsArgs.AppDomain, lsArgs.Thread);
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
				InitializeCurrentDebuggerState(e, cadArgs.Process, cadArgs.AppDomain, null);
				break;

			case DebugCallbackType.ExitAppDomain:
				var eadArgs = (ExitAppDomainDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, eadArgs.Process, eadArgs.AppDomain, null);
				process = processes.TryGet(eadArgs.Process);
				if (process != null) {
					OnAppDomainUnloaded(process.TryGetAppDomain(eadArgs.AppDomain));
					process.AppDomainExited(eadArgs.AppDomain);
				}
				break;

			case DebugCallbackType.LoadAssembly:
				var laArgs = (LoadAssemblyDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, laArgs.AppDomain, null);
				appDomain = TryGetValidAppDomain(laArgs.AppDomain);
				if (appDomain != null)
					appDomain.TryAdd(laArgs.Assembly);
				break;

			case DebugCallbackType.UnloadAssembly:
				var uaArgs = (UnloadAssemblyDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, uaArgs.AppDomain, null);
				appDomain = TryGetAppDomain(uaArgs.AppDomain);
				if (appDomain != null) {
					OnAssemblyUnloaded(appDomain.TryGetAssembly(uaArgs.Assembly));
					appDomain.AssemblyUnloaded(uaArgs.Assembly);
				}
				break;

			case DebugCallbackType.ControlCTrap:
				var cctArgs = (ControlCTrapDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, cctArgs.Process, null, null);
				break;

			case DebugCallbackType.NameChange:
				var ncArgs = (NameChangeDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ncArgs.AppDomain, ncArgs.Thread);
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
				InitializeCurrentDebuggerState(e, null, umsArgs.AppDomain, null);
				break;

			case DebugCallbackType.EditAndContinueRemap:
				var encrArgs = (EditAndContinueRemapDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, encrArgs.AppDomain, encrArgs.Thread);
				break;

			case DebugCallbackType.BreakpointSetError:
				var bpseArgs = (BreakpointSetErrorDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, bpseArgs.AppDomain, bpseArgs.Thread);
				break;

			case DebugCallbackType.FunctionRemapOpportunity:
				var froArgs = (FunctionRemapOpportunityDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, froArgs.AppDomain, froArgs.Thread);
				break;

			case DebugCallbackType.CreateConnection:
				var ccArgs = (CreateConnectionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, ccArgs.Process, null, null);
				break;

			case DebugCallbackType.ChangeConnection:
				var cc2Args = (ChangeConnectionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, cc2Args.Process, null, null);
				break;

			case DebugCallbackType.DestroyConnection:
				var dcArgs = (DestroyConnectionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, dcArgs.Process, null, null);
				break;

			case DebugCallbackType.Exception2:
				var ex2Args = (Exception2DebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ex2Args.AppDomain, ex2Args.Thread);
				break;

			case DebugCallbackType.ExceptionUnwind:
				var euArgs = (ExceptionUnwindDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, euArgs.AppDomain, euArgs.Thread);
				break;

			case DebugCallbackType.FunctionRemapComplete:
				var frcArgs = (FunctionRemapCompleteDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, frcArgs.AppDomain, frcArgs.Thread);
				break;

			case DebugCallbackType.MDANotification:
				var mdanArgs = (MDANotificationDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, mdanArgs.Controller as ICorDebugProcess, mdanArgs.Controller as ICorDebugAppDomain, mdanArgs.Thread);
				break;

			case DebugCallbackType.CustomNotification:
				var cnArgs = (CustomNotificationDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, cnArgs.AppDomain, cnArgs.Thread);
				break;

			default:
				InitializeCurrentDebuggerState(e, null);
				Debug.Fail(string.Format("Unknown debug callback type: {0}", e.Type));
				break;
			}
		}

		void CheckBreakpoints(DebugCallbackEventArgs e) {
			var type = DnDebugEventBreakpoint.GetDebugEventBreakpointType(e);
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
				ResetDebuggerStates();
				CallOnProcessStateChanged();
			}
		}
		bool hasTerminated = false;

		public static DnDebugger DebugProcess(DebugProcessOptions options) {
			if (options.DebugMessageDispatcher == null)
				throw new ArgumentException("DebugMessageDispatcher is null");

			var debuggeeVersion = options.DebuggeeVersion ?? DebuggeeVersionDetector.GetVersion(options.Filename);
			var dbg = new DnDebugger(CreateCorDebug(debuggeeVersion), options.DebugOptions, options.DebugMessageDispatcher);
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
		public DnDebugEventBreakpoint CreateBreakpoint(DebugEventBreakpointType eventType, Predicate<BreakpointConditionContext> cond) {
			DebugVerifyThread();
			return CreateBreakpoint(eventType, new DelegateBreakpointCondition(cond));
		}

		/// <summary>
		/// Creates a debug event breakpoint
		/// </summary>
		/// <param name="eventType">Debug event</param>
		/// <param name="bpCond">Condition or null</param>
		/// <returns></returns>
		public DnDebugEventBreakpoint CreateBreakpoint(DebugEventBreakpointType eventType, IBreakpointCondition bpCond = null) {
			DebugVerifyThread();
			var bp = new DnDebugEventBreakpoint(eventType, bpCond);
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
		public DnILCodeBreakpoint CreateBreakpoint(SerializedDnModule module, uint token, uint ilOffset, Predicate<BreakpointConditionContext> cond) {
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
		public DnILCodeBreakpoint CreateBreakpoint(SerializedDnModule module, uint token, uint ilOffset, IBreakpointCondition bpCond = null) {
			DebugVerifyThread();
			var bp = new DnILCodeBreakpoint(module, token, ilOffset, bpCond);
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

		public void RemoveBreakpoint(DnBreakpoint bp) {
			DebugVerifyThread();
			var debp = bp as DnDebugEventBreakpoint;
			if (debp != null) {
				debugEventBreakpointList.Remove(debp);
				debp.OnRemoved();
				return;
			}

			var ilbp = bp as DnILCodeBreakpoint;
			if (ilbp != null) {
				ilCodeBreakpointList.Remove(ilbp.Module, ilbp);
				ilbp.OnRemoved();
				return;
			}
		}

		public int TryBreakProcesses() {
			return TryBreakProcesses(true);
		}

		int TryBreakProcesses(bool callProcessStopped) {
			if (ProcessState != DebuggerProcessState.Starting && ProcessState != DebuggerProcessState.Running)
				return -1;

			int errorHR = 0;
			foreach (var process in processes.GetAll()) {
				try {
					int hr = process.CorProcess.RawObject.Stop(uint.MaxValue);
					if (hr < 0)
						errorHR = hr;
					else
						ProcessStopped(process, callProcessStopped);
				}
				catch {
					if (errorHR == 0)
						errorHR = -1;
				}
            }

			return errorHR;
		}

		void ProcessStopped(DnProcess process, bool addStopState) {
			this.managedCallbackCounter++;
			if (!addStopState)
				return;

			DnThread thread = process.GetMainThread();
			DnAppDomain appDomain = thread == null ? process.GetMainAppDomain() : thread.AppDomainOrNull;
			AddDebuggerState(new DebuggerState(null, process, appDomain, thread) {
				StopStates = new DebuggerStopState[] {
					new DebuggerStopState(DebuggerStopReason.UserBreak),
				}
			});

			CallOnProcessStateChanged();
		}

		public void TerminateProcesses() {
			TerminateAllProcessesInternal();
			Continue();
		}

		public int TryDetach() {
			if (ProcessState == DebuggerProcessState.Starting || ProcessState == DebuggerProcessState.Running) {
				int hr = TryBreakProcesses(false);
				if (hr < 0)
					return hr;
			}

			foreach (var bp in ilCodeBreakpointList.GetBreakpoints())
				bp.OnRemoved();

			foreach (var kv in stepInfos) {
				if (kv.Key.IsActive)
					kv.Key.Deactivate();
			}

			foreach (var process in processes.GetAll()) {
				try {
					int hr = process.CorProcess.RawObject.Detach();
					if (hr < 0)
						return hr;
				}
				catch (InvalidComObjectException) {
				}
			}

			hasTerminated = true;
			ResetDebuggerStates();
			CallOnProcessStateChanged();

			return 0;
		}

		~DnDebugger() {
			Dispose(false);
		}

		public void Dispose() {
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		void Dispose(bool disposing) {
			TerminateAllProcessesInternal();
		}

		void TerminateAllProcessesInternal() {
			foreach (var process in processes.GetAll()) {
				try {
					int hr = process.CorProcess.RawObject.Stop(uint.MaxValue);
					hr = process.CorProcess.RawObject.Terminate(uint.MaxValue);
				}
				catch (InvalidComObjectException) {
				}
			}
		}
	}
}
