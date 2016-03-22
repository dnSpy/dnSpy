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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaHost;

namespace dndbg.Engine {
	public delegate void DebugCallbackEventHandler(DnDebugger dbg, DebugCallbackEventArgs e);

	/// <summary>
	/// Only call debugger methods in the dndbg thread since it's not thread safe
	/// </summary>
	public sealed class DnDebugger : IDisposable {
		readonly IDebugMessageDispatcher debugMessageDispatcher;
		readonly ICorDebug corDebug;
		readonly DebuggerCollection<ICorDebugProcess, DnProcess> processes;
		readonly DebugEventBreakpointList<DnDebugEventBreakpoint> debugEventBreakpointList = new DebugEventBreakpointList<DnDebugEventBreakpoint>();
		readonly DebugEventBreakpointList<DnAnyDebugEventBreakpoint> anyDebugEventBreakpointList = new DebugEventBreakpointList<DnAnyDebugEventBreakpoint>();
		readonly BreakpointList<DnILCodeBreakpoint> ilCodeBreakpointList = new BreakpointList<DnILCodeBreakpoint>();
		readonly BreakpointList<DnNativeCodeBreakpoint> nativeCodeBreakpointList = new BreakpointList<DnNativeCodeBreakpoint>();
		readonly Dictionary<CorStepper, StepInfo> stepInfos = new Dictionary<CorStepper, StepInfo>();
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

		public DebuggerProcessState ProcessState {
			get {
				DebugVerifyThread();
				if (hasTerminated)
					return DebuggerProcessState.Terminated;
				if (continuing)
					return DebuggerProcessState.Continuing;
				if (managedCallbackCounter != 0)
					return DebuggerProcessState.Paused;
				if (!hasReceivedCreateProcessEvent)
					return DebuggerProcessState.Starting;
				return DebuggerProcessState.Running;
			}
		}
		bool hasReceivedCreateProcessEvent = false;

		public event EventHandler<ModuleDebuggerEventArgs> OnModuleAdded;
		void CallOnModuleAdded(DnModule module, bool added) {
			if (OnModuleAdded != null)
				OnModuleAdded(this, new ModuleDebuggerEventArgs(module, added));
		}

		public event EventHandler<NameChangedDebuggerEventArgs> OnNameChanged;
		void CallOnNameChanged(DnAppDomain appDomain, DnThread thread) {
			if (OnNameChanged != null)
				OnNameChanged(this, new NameChangedDebuggerEventArgs(appDomain, thread));
		}

		public event EventHandler<DebuggerEventArgs> OnProcessStateChanged;
		void CallOnProcessStateChanged() {
			if (OnProcessStateChanged != null)
				OnProcessStateChanged(this, DebuggerEventArgs.Empty);
		}

		public event EventHandler<CorModuleDefCreatedEventArgs> OnCorModuleDefCreated;
		internal void CorModuleDefCreated(DnModule module) {
			DebugVerifyThread();
			Debug.Assert(module.CorModuleDef != null);
			if (OnCorModuleDefCreated != null)
				OnCorModuleDefCreated(this, new CorModuleDefCreatedEventArgs(module, module.CorModuleDef));
		}

		public DnDebugEventBreakpoint[] DebugEventBreakpoints {
			get { DebugVerifyThread(); return debugEventBreakpointList.Breakpoints; }
		}

		public DnAnyDebugEventBreakpoint[] AnyDebugEventBreakpoints {
			get { DebugVerifyThread(); return anyDebugEventBreakpointList.Breakpoints; }
		}

		public IEnumerable<DnILCodeBreakpoint> ILCodeBreakpoints {
			get { DebugVerifyThread(); return ilCodeBreakpointList.GetBreakpoints(); }
		}

		public IEnumerable<DnNativeCodeBreakpoint> NativeCodeBreakpoints {
			get { DebugVerifyThread(); return nativeCodeBreakpointList.GetBreakpoints(); }
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

		/// <summary>
		/// true if we attached to a process, false if we started all processes
		/// </summary>
		public bool HasAttached {
			get { return processes.GetAll().Any(p => p.WasAttached); }
		}

		/// <summary>
		/// This is the debuggee version or an empty string if it's not known (eg. if it's CoreCLR)
		/// </summary>
		public string DebuggeeVersion {
			get { return debuggeeVersion; }
		}
		readonly string debuggeeVersion;

		readonly int debuggerManagedThreadId;

		DnDebugger(ICorDebug corDebug, DebugOptions debugOptions, IDebugMessageDispatcher debugMessageDispatcher, string debuggeeVersion) {
			if (debugMessageDispatcher == null)
				throw new ArgumentNullException("debugMessageDispatcher");
			this.debuggerManagedThreadId = Thread.CurrentThread.ManagedThreadId;
			this.processes = new DebuggerCollection<ICorDebugProcess, DnProcess>(CreateDnProcess);
			this.debugMessageDispatcher = debugMessageDispatcher;
			this.corDebug = corDebug;
			this.debugOptions = debugOptions ?? new DebugOptions();
			this.debuggeeVersion = debuggeeVersion ?? string.Empty;

			// I have not tested debugging with CLR 1.x. It's too old to support it so this is a won't fix
			if (this.debuggeeVersion.StartsWith("1."))
				throw new NotImplementedException("Can't debug .NET 1.x assemblies. Add an App.config file to force using .NET 2.0 or later");

			corDebug.Initialize();
			corDebug.SetManagedHandler(new CorDebugManagedCallback(this));
		}

		void ResetDebuggerStates() {
			debuggerStates.Clear();
		}

		void AddDebuggerState(DebuggerState debuggerState) {
			debuggerStates.Add(debuggerState);
		}

		DnProcess CreateDnProcess(ICorDebugProcess comProcess) {
			return new DnProcess(this, comProcess, GetNextProcessId());
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
			debugMessageDispatcher.ExecuteAsync(() => {
				DebugCallbackEventArgs e;
				try {
					e = func();
				}
				catch {
					// most likely debugger has already stopped
					return;
				}
				OnManagedCallbackInDebuggerThread(e);
			});
		}

		// Same as above method but called by CreateProcess, LoadModule, CreateAppDomain because
		// certain methods must be called before we return to the CLR debugger.
		internal void OnManagedCallbackFromAnyThread2(Func<DebugCallbackEventArgs> func) {
			using (var ev = new ManualResetEvent(false)) {
				debugMessageDispatcher.ExecuteAsync(() => {
					try {
						DebugCallbackEventArgs e;
						try {
							e = func();
						}
						catch {
							// most likely debugger has already stopped
							return;
						}
						OnManagedCallbackInDebuggerThread(e);
					}
					finally {
						ev.Set();
					}
				});
				ev.WaitOne();
			}
		}

		/// <summary>
		/// Gets incremented each time Continue() is called
		/// </summary>
		public uint ContinueCounter {
			get { return continueCounter; }
		}
		uint continueCounter;

		// Called in our dndbg thread
		void OnManagedCallbackInDebuggerThread(DebugCallbackEventArgs e) {
			DebugVerifyThread();
			if (hasTerminated)
				return;
			managedCallbackCounter++;

			if (disposeValues.Count != 0) {
				foreach (var value in disposeValues)
					value.DisposeHandle();
				disposeValues.Clear();
			}

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

			Current.PauseStates = e.PauseStates;
			if (HasQueuedCallbacks(e)) {
				ContinueAndDecrementCounter(e);
				// DON'T call anything, DON'T write any fields now, the CLR debugger could've already called us again when Continue() was called
			}
			else if (ShouldStopQueued())
				CallOnProcessStateChanged();
			else {
				ResetDebuggerStates();
				ContinueAndDecrementCounter(e);
				// DON'T call anything, DON'T write any fields now, the CLR debugger could've already called us again when Continue() was called
			}
		}
		int managedCallbackCounter;

		bool ShouldStopQueued() {
			foreach (var state in debuggerStates) {
				if (state.PauseStates.Length != 0)
					return true;
			}
			return false;
		}

		// This method must be called just before returning to the caller. No fields can be accessed
		// and no methods can be called because the CLR debugger could call us before this method
		// returns.
		void ContinueAndDecrementCounter(DebugCallbackEventArgs e) {
			if (e.Kind != DebugCallbackKind.ExitProcess)
				Continue(e.CorDebugController, false);	// Also decrements managedCallbackCounter
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

		// This method must be called just before returning to the caller. No fields can be accessed
		// and no methods can be called because the CLR debugger could call us before this method
		// returns.
		bool Continue(ICorDebugController controller, bool callOnProcessStateChanged) {
			Debug.Assert(controller != null);
			if (controller == null)
				return false;

			if (callOnProcessStateChanged && managedCallbackCounter == 1) {
				try {
					continuing = true;
					CallOnProcessStateChanged();
				}
				finally {
					continuing = false;
				}
			}
			managedCallbackCounter--;
			continueCounter++;

			if (callOnProcessStateChanged && managedCallbackCounter == 0)
				CallOnProcessStateChanged();

			// As soon as we call Continue(), the CLR debugger could send us another message so it's
			// important that we don't access any of our fields and don't call any methods after
			// Continue() has been called!
			int hr = controller.Continue(0);
			bool success = hr >= 0 || hr == CordbgErrors.CORDBG_E_PROCESS_TERMINATED || hr == CordbgErrors.CORDBG_E_OBJECT_NEUTERED;
			Debug.WriteLineIf(!success, string.Format("dndbg: ICorDebugController::Continue() failed: 0x{0:X8}", hr));
			return success;
		}
		bool continuing = false;

		/// <summary>
		/// true if <see cref="Continue()"/> can be called
		/// </summary>
		public bool CanContinue {
			get { DebugVerifyThread(); return ProcessState == DebuggerProcessState.Paused; }
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
				if (!Continue(controller, true))	// Also decrements managedCallbackCounter
					return;
			}
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
			return ProcessState == DebuggerProcessState.Paused && frame != null;
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
			var ps = Processes;
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
			if (process != null) {
				appDomain = process.TryGetAppDomain(comAppDomain);
				thread = process.TryGetThread(comThread);
			}
			else {
				appDomain = null;
				thread = null;
			}

			if (thread == null && appDomain == null && process != null)
				appDomain = process.AppDomains.FirstOrDefault();
			if (thread == null) {
				if (appDomain != null)
					thread = appDomain.Threads.FirstOrDefault();
				else if (process != null)
					thread = process.Threads.FirstOrDefault();
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

			foreach (var appDomain in process.AppDomains) {
				OnAppDomainUnloaded(appDomain);
				process.AppDomainExited(appDomain.CorAppDomain.RawObject);
			}
		}

		void OnAppDomainUnloaded(DnAppDomain appDomain) {
			if (appDomain == null)
				return;
			foreach (var assembly in appDomain.Assemblies) {
				OnAssemblyUnloaded(assembly);
				appDomain.AssemblyUnloaded(assembly.CorAssembly.RawObject);
			}
		}

		void OnAssemblyUnloaded(DnAssembly assembly) {
			if (assembly == null)
				return;
			foreach (var module in assembly.Modules)
				OnModuleUnloaded(module);
		}

		void OnModuleUnloaded(DnModule module) {
			module.Assembly.ModuleUnloaded(module);
			CallOnModuleAdded(module, false);
			RemoveModuleFromBreakpoints(module);
		}

		void RemoveModuleFromBreakpoints(DnModule module) {
			if (module == null)
				return;
			foreach (var bp in this.ilCodeBreakpointList.GetBreakpoints(module.SerializedDnModule))
				bp.RemoveModule(module);
			foreach (var bp in this.nativeCodeBreakpointList.GetBreakpoints(module.SerializedDnModule))
				bp.RemoveModule(module);
		}

		void HandleManagedCallback(DebugCallbackEventArgs e) {
			bool b;
			DnProcess process;
			DnAppDomain appDomain;
			DnAssembly assembly;
			CorClass cls;
			switch (e.Kind) {
			case DebugCallbackKind.Breakpoint:
				var bpArgs = (BreakpointDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, bpArgs.AppDomain, bpArgs.Thread);
				break;

			case DebugCallbackKind.StepComplete:
				var scArgs = (StepCompleteDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, scArgs.AppDomain, scArgs.Thread);
				StepInfo stepInfo;
				bool calledStepInfoOnCompleted = false;
				var stepperKey = scArgs.CorStepper;
				if (stepperKey != null && stepInfos.TryGetValue(stepperKey, out stepInfo)) {
					stepInfos.Remove(stepperKey);
					if (stepInfo.OnCompleted != null) {
						calledStepInfoOnCompleted = true;
						stepInfo.OnCompleted(this, scArgs);
					}
				}
				// Don't stop on step/breakpoints when we're evaluating
				if (!calledStepInfoOnCompleted && !IsEvaluating)
					scArgs.AddPauseState(new StepPauseState(scArgs.Reason));
				break;

			case DebugCallbackKind.Break:
				var bArgs = (BreakDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, bArgs.AppDomain, bArgs.Thread);
				break;

			case DebugCallbackKind.Exception:
				var ex1Args = (ExceptionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ex1Args.AppDomain, ex1Args.Thread);
				break;

			case DebugCallbackKind.EvalComplete:
				var ecArgs = (EvalCompleteDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ecArgs.AppDomain, ecArgs.Thread);
				break;

			case DebugCallbackKind.EvalException:
				var eeArgs = (EvalExceptionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, eeArgs.AppDomain, eeArgs.Thread);
				break;

			case DebugCallbackKind.CreateProcess:
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

			case DebugCallbackKind.ExitProcess:
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

			case DebugCallbackKind.CreateThread:
				var ctArgs = (CreateThreadDebugCallbackEventArgs)e;
				process = TryGetValidProcess(ctArgs.Thread);
				if (process != null) {
					process.TryAdd(ctArgs.Thread);
					//TODO: ICorDebugThread::SetDebugState
				}
				InitializeCurrentDebuggerState(e, null, ctArgs.AppDomain, ctArgs.Thread);
				break;

			case DebugCallbackKind.ExitThread:
				var etArgs = (ExitThreadDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, etArgs.AppDomain, etArgs.Thread);
				process = TryGetValidProcess(etArgs.Thread);
				if (process != null)
					process.ThreadExited(etArgs.Thread);
				break;

			case DebugCallbackKind.LoadModule:
				var lmArgs = (LoadModuleDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, lmArgs.AppDomain, null);
				assembly = TryGetValidAssembly(lmArgs.AppDomain, lmArgs.Module);
				if (assembly != null) {
					var module = assembly.TryAdd(lmArgs.Module);
					module.CorModule.EnableJITDebugging(debugOptions.ModuleTrackJITInfo, debugOptions.ModuleAllowJitOptimizations);
					module.CorModule.EnableClassLoadCallbacks(debugOptions.ModuleClassLoadCallbacks);
					module.CorModule.JITCompilerFlags = debugOptions.JITCompilerFlags;
					module.CorModule.SetJMCStatus(true);

					module.InitializeCachedValues();
					AddBreakpoints(module);

					CallOnModuleAdded(module, true);
				}
				break;

			case DebugCallbackKind.UnloadModule:
				var umArgs = (UnloadModuleDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, umArgs.AppDomain, null);
				assembly = TryGetValidAssembly(umArgs.AppDomain, umArgs.Module);
				if (assembly != null) {
					var module = assembly.TryGetModule(umArgs.Module);
					OnModuleUnloaded(module);
				}
				break;

			case DebugCallbackKind.LoadClass:
				var lcArgs = (LoadClassDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, lcArgs.AppDomain, null);

				cls = lcArgs.CorClass;
				if (cls != null) {
					var module = TryGetModule(lcArgs.CorAppDomain, cls);
					if (module != null) {
						if (module.CorModuleDef != null)
							module.CorModuleDef.LoadClass(cls.Token);
						foreach (var bp in ilCodeBreakpointList.GetBreakpoints(module.SerializedDnModule))
							bp.AddBreakpoint(module);
						foreach (var bp in nativeCodeBreakpointList.GetBreakpoints(module.SerializedDnModule))
							bp.AddBreakpoint(module);
					}
				}
				break;

			case DebugCallbackKind.UnloadClass:
				var ucArgs = (UnloadClassDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ucArgs.AppDomain, null);

				cls = ucArgs.CorClass;
				if (cls != null) {
					var module = TryGetModule(ucArgs.CorAppDomain, cls);
					if (module != null && module.CorModuleDef != null)
						module.CorModuleDef.UnloadClass(cls.Token);
				}
				break;

			case DebugCallbackKind.DebuggerError:
				var deArgs = (DebuggerErrorDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, deArgs.Process, null, null);
				break;

			case DebugCallbackKind.LogMessage:
				var lmsgArgs = (LogMessageDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, lmsgArgs.AppDomain, lmsgArgs.Thread);
				break;

			case DebugCallbackKind.LogSwitch:
				var lsArgs = (LogSwitchDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, lsArgs.AppDomain, lsArgs.Thread);
				break;

			case DebugCallbackKind.CreateAppDomain:
				var cadArgs = (CreateAppDomainDebugCallbackEventArgs)e;
				process = TryGetValidProcess(cadArgs.Process);
				if (process != null && cadArgs.AppDomain != null) {
					b = cadArgs.AppDomain.Attach() >= 0;
					Debug.WriteLineIf(!b, string.Format("CreateAppDomain: could not attach to AppDomain: {0:X8}", cadArgs.AppDomain.GetHashCode()));
					if (b)
						process.TryAdd(cadArgs.AppDomain);
				}
				InitializeCurrentDebuggerState(e, cadArgs.Process, cadArgs.AppDomain, null);
				break;

			case DebugCallbackKind.ExitAppDomain:
				var eadArgs = (ExitAppDomainDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, eadArgs.Process, eadArgs.AppDomain, null);
				process = processes.TryGet(eadArgs.Process);
				if (process != null) {
					OnAppDomainUnloaded(process.TryGetAppDomain(eadArgs.AppDomain));
					process.AppDomainExited(eadArgs.AppDomain);
				}
				break;

			case DebugCallbackKind.LoadAssembly:
				var laArgs = (LoadAssemblyDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, laArgs.AppDomain, null);
				appDomain = TryGetValidAppDomain(laArgs.AppDomain);
				if (appDomain != null)
					appDomain.TryAdd(laArgs.Assembly);
				break;

			case DebugCallbackKind.UnloadAssembly:
				var uaArgs = (UnloadAssemblyDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, uaArgs.AppDomain, null);
				appDomain = TryGetAppDomain(uaArgs.AppDomain);
				if (appDomain != null) {
					OnAssemblyUnloaded(appDomain.TryGetAssembly(uaArgs.Assembly));
					appDomain.AssemblyUnloaded(uaArgs.Assembly);
				}
				break;

			case DebugCallbackKind.ControlCTrap:
				var cctArgs = (ControlCTrapDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, cctArgs.Process, null, null);
				break;

			case DebugCallbackKind.NameChange:
				var ncArgs = (NameChangeDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ncArgs.AppDomain, ncArgs.Thread);
				appDomain = TryGetValidAppDomain(ncArgs.AppDomain);
				if (appDomain != null)
					appDomain.NameChanged();
				var thread = TryGetValidThread(ncArgs.Thread);
				if (thread != null)
					thread.NameChanged();
				CallOnNameChanged(appDomain, thread);
				break;

			case DebugCallbackKind.UpdateModuleSymbols:
				var umsArgs = (UpdateModuleSymbolsDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, umsArgs.AppDomain, null);
				break;

			case DebugCallbackKind.EditAndContinueRemap:
				var encrArgs = (EditAndContinueRemapDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, encrArgs.AppDomain, encrArgs.Thread);
				break;

			case DebugCallbackKind.BreakpointSetError:
				var bpseArgs = (BreakpointSetErrorDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, bpseArgs.AppDomain, bpseArgs.Thread);
				break;

			case DebugCallbackKind.FunctionRemapOpportunity:
				var froArgs = (FunctionRemapOpportunityDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, froArgs.AppDomain, froArgs.Thread);
				break;

			case DebugCallbackKind.CreateConnection:
				var ccArgs = (CreateConnectionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, ccArgs.Process, null, null);
				break;

			case DebugCallbackKind.ChangeConnection:
				var cc2Args = (ChangeConnectionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, cc2Args.Process, null, null);
				break;

			case DebugCallbackKind.DestroyConnection:
				var dcArgs = (DestroyConnectionDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, dcArgs.Process, null, null);
				break;

			case DebugCallbackKind.Exception2:
				var ex2Args = (Exception2DebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ex2Args.AppDomain, ex2Args.Thread);
				break;

			case DebugCallbackKind.ExceptionUnwind:
				var euArgs = (ExceptionUnwindDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, euArgs.AppDomain, euArgs.Thread);
				break;

			case DebugCallbackKind.FunctionRemapComplete:
				var frcArgs = (FunctionRemapCompleteDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, frcArgs.AppDomain, frcArgs.Thread);
				break;

			case DebugCallbackKind.MDANotification:
				var mdanArgs = (MDANotificationDebugCallbackEventArgs)e;
#if DEBUG
				{
					var mda = mdanArgs.CorMDA;
					Debug.WriteLine(string.Format("MDA error **************************"));
					Debug.WriteLine(string.Format("Name : {0}", mda.Name));
					Debug.WriteLine(string.Format("Flags: {0}", mda.Flags));
					Debug.WriteLine(string.Format("OSTID: {0}", mda.OSThreadId));
					Debug.WriteLine(string.Format("Desc : {0}", mda.Description));
					Debug.WriteLine(string.Format("XML  : {0}", mda.XML));
				}
#endif
				InitializeCurrentDebuggerState(e, mdanArgs.Controller as ICorDebugProcess, mdanArgs.Controller as ICorDebugAppDomain, mdanArgs.Thread);
				break;

			case DebugCallbackKind.CustomNotification:
				var cnArgs = (CustomNotificationDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, cnArgs.AppDomain, cnArgs.Thread);
				break;

			default:
				InitializeCurrentDebuggerState(e, null);
				Debug.Fail(string.Format("Unknown debug callback type: {0}", e.Kind));
				break;
			}
		}

		void CheckBreakpoints(DebugCallbackEventArgs e) {
			// Never check breakpoints when we're evaluating
			if (IsEvaluating)
				return;

			var type = DnDebugEventBreakpoint.GetDebugEventBreakpointKind(e);
			if (type != null) {
				foreach (var bp in DebugEventBreakpoints) {
					if (bp.IsEnabled && bp.EventKind == type.Value && bp.Condition(new DebugEventBreakpointConditionContext(this, bp, e)))
						e.AddPauseState(new DebugEventBreakpointPauseState(bp));
				}
			}

			foreach (var bp in AnyDebugEventBreakpoints) {
				if (bp.IsEnabled && bp.Condition(new AnyDebugEventBreakpointConditionContext(this, bp, e)))
					e.AddPauseState(new AnyDebugEventBreakpointPauseState(bp));
			}

			if (e.Kind == DebugCallbackKind.Breakpoint) {
				var bpArgs = (BreakpointDebugCallbackEventArgs)e;
				//TODO: Use a dictionary instead of iterating over all breakpoints
				foreach (var bp in ilCodeBreakpointList.GetBreakpoints()) {
					if (!bp.IsBreakpoint(bpArgs.Breakpoint))
						continue;

					if (bp.IsEnabled && bp.Condition(new ILCodeBreakpointConditionContext(this, bp)))
						e.AddPauseState(new ILCodeBreakpointPauseState(bp));
					break;
				}
				foreach (var bp in nativeCodeBreakpointList.GetBreakpoints()) {
					if (!bp.IsBreakpoint(bpArgs.Breakpoint))
						continue;

					if (bp.IsEnabled && bp.Condition(new NativeCodeBreakpointConditionContext(this, bp)))
						e.AddPauseState(new NativeCodeBreakpointPauseState(bp));
					break;
				}
			}

			if (e.Kind == DebugCallbackKind.Break && !debugOptions.IgnoreBreakInstructions)
				e.AddPauseReason(DebuggerPauseReason.Break);

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
			var dbg = CreateDnDebugger(options);
			if (dbg == null)
				throw new Exception("Couldn't create a debugger instance");
			return dbg;
		}

		static DnDebugger CreateDnDebugger(DebugProcessOptions options) {
			switch (options.CLRTypeDebugInfo.CLRType) {
			case CLRType.Desktop:	return CreateDnDebuggerDesktop(options);
			case CLRType.CoreCLR:	return CreateDnDebuggerCoreCLR(options);
			default:
				Debug.Fail("Invalid CLRType");
				throw new InvalidOperationException();
			}
		}

		static DnDebugger CreateDnDebuggerDesktop(DebugProcessOptions options) {
			var clrType = (DesktopCLRTypeDebugInfo)options.CLRTypeDebugInfo;
			var debuggeeVersion = clrType.DebuggeeVersion ?? DebuggeeVersionDetector.GetVersion(options.Filename);
			var corDebug = CreateCorDebug(debuggeeVersion);
			if (corDebug == null)
				throw new Exception("Could not create an ICorDebug instance");
			var dbg = new DnDebugger(corDebug, options.DebugOptions, options.DebugMessageDispatcher, debuggeeVersion);
			if (options.BreakProcessKind != BreakProcessKind.None)
				new BreakProcessHelper(dbg, options.BreakProcessKind, options.Filename);
			dbg.CreateProcess(options);
			return dbg;
		}

		static DnDebugger CreateDnDebuggerCoreCLR(DebugProcessOptions options) {
			var clrType = (CoreCLRTypeDebugInfo)options.CLRTypeDebugInfo;
			var dbg2 = CoreCLRHelper.CreateDnDebugger(options, clrType, () => false, (cd, pid) => {
				var dbg = new DnDebugger(cd, options.DebugOptions, options.DebugMessageDispatcher, null);
				if (options.BreakProcessKind != BreakProcessKind.None)
					new BreakProcessHelper(dbg, options.BreakProcessKind, options.Filename);
				ICorDebugProcess comProcess;
				cd.DebugActiveProcess((int)pid, 0, out comProcess);
				var dnProcess = dbg.TryAdd(comProcess);
				if (dnProcess != null)
					dnProcess.Initialize(false, options.Filename, options.CurrentDirectory, options.CommandLine);
				return dbg;
			});
			if (dbg2 == null)
				throw new Exception("Could not create a debugger instance");
			return dbg2;
		}

		void CreateProcess(DebugProcessOptions options) {
			ICorDebugProcess comProcess;
			try {
				var dwCreationFlags = options.ProcessCreationFlags ?? DebugProcessOptions.DefaultProcessCreationFlags;
				var si = new STARTUPINFO();
				si.cb = (uint)(4 * 1 + IntPtr.Size * 3 + 4 * 8 + 2 * 2 + IntPtr.Size * 4);
				var pi = new PROCESS_INFORMATION();
				// We must add the space here if the beginning of the command line appears to be
				// the path to a file, eg. "/someOption" or "c:blah" or it won't be passed to the
				// debugged program.
				var cmdline = " " + (options.CommandLine ?? string.Empty);
				corDebug.CreateProcess(options.Filename ?? string.Empty, cmdline, IntPtr.Zero, IntPtr.Zero,
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
				process.Initialize(false, options.Filename, options.CurrentDirectory, options.CommandLine);
		}

		public static DnDebugger Attach(AttachProcessOptions options) {
			var process = Process.GetProcessById(options.ProcessId);
			var filename = process.MainModule.FileName;

			string debuggeeVersion;
			var corDebug = CreateCorDebug(options, out debuggeeVersion);
			if (corDebug == null)
				throw new Exception("An ICorDebug instance couldn't be created");
			var dbg = new DnDebugger(corDebug, options.DebugOptions, options.DebugMessageDispatcher, debuggeeVersion);
			ICorDebugProcess comProcess;
			corDebug.DebugActiveProcess(options.ProcessId, 0, out comProcess);
			var dnProcess = dbg.TryAdd(comProcess);
			if (dnProcess != null)
				dnProcess.Initialize(true, filename, string.Empty, string.Empty);
			return dbg;
		}

		static ICorDebug CreateCorDebug(AttachProcessOptions options, out string debuggeeVersion) {
			switch (options.CLRTypeAttachInfo.CLRType) {
			case CLRType.Desktop:	return CreateCorDebugDesktop(options, out debuggeeVersion);
			case CLRType.CoreCLR:	return CreateCorDebugCoreCLR(options, out debuggeeVersion);
			default:
				Debug.Fail("Invalid CLRType");
				throw new InvalidOperationException();
			}
		}

		static ICorDebug CreateCorDebugDesktop(AttachProcessOptions options, out string debuggeeVersion) {
			var clrType = (DesktopCLRTypeAttachInfo)options.CLRTypeAttachInfo;
			debuggeeVersion = clrType.DebuggeeVersion;
			ICLRRuntimeInfo rtInfo = null;
			var process = Process.GetProcessById(options.ProcessId);
			var filename = process.MainModule.FileName;
			foreach (var t in GetCLRRuntimeInfos(process)) {
				if (string.IsNullOrEmpty(clrType.DebuggeeVersion) || t.Item1 == clrType.DebuggeeVersion) {
					rtInfo = t.Item2;
					break;
				}
			}
			if (rtInfo == null)
				throw new Exception("Couldn't find a .NET runtime or the correct .NET runtime");

			var clsid = new Guid("DF8395B5-A4BA-450B-A77C-A9A47762C520");
			var riid = typeof(ICorDebug).GUID;
			return (ICorDebug)rtInfo.GetInterface(ref clsid, ref riid);
		}

		static ICorDebug CreateCorDebugCoreCLR(AttachProcessOptions options, out string debuggeeVersion) {
			debuggeeVersion = null;
			var clrType = (CoreCLRTypeAttachInfo)options.CLRTypeAttachInfo;
			return CoreCLRHelper.CreateCorDebug(clrType);
		}

		static IEnumerable<Tuple<string, ICLRRuntimeInfo>> GetCLRRuntimeInfos(Process process) {
			var clsid = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");
			var riid = typeof(ICLRMetaHost).GUID;
			var mh = (ICLRMetaHost)NativeMethods.CLRCreateInstance(ref clsid, ref riid);

			IEnumUnknown iter;
			int hr = mh.EnumerateLoadedRuntimes(process.Handle, out iter);
			if (hr < 0)
				yield break;
			for (;;) {
				object obj;
				uint fetched;
				hr = iter.Next(1, out obj, out fetched);
				if (hr < 0 || fetched == 0)
					break;

				var rtInfo = (ICLRRuntimeInfo)obj;
				uint chBuffer = 0;
				var sb = new StringBuilder(300);
				hr = rtInfo.GetVersionString(sb, ref chBuffer);
				sb.EnsureCapacity((int)chBuffer);
				hr = rtInfo.GetVersionString(sb, ref chBuffer);

				yield return Tuple.Create(sb.ToString(), rtInfo);
			}
		}

		DnProcess TryAdd(ICorDebugProcess comProcess) {
			if (comProcess == null)
				return null;

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
		public DnProcess[] Processes {
			get {
				DebugVerifyThread();
				var list = processes.GetAll();
				Array.Sort(list, (a, b) => a.UniqueId.CompareTo(b.UniqueId));
				return list;
			}
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

		public DnModule TryGetModule(CorAppDomain appDomain, CorClass cls) {
			if (appDomain == null || cls == null)
				return null;
			var clsMod = cls.Module;
			if (clsMod == null)
				return null;
			var ad = TryGetAppDomain(appDomain.RawObject);
			if (ad == null)
				return null;

			var asm = TryGetValidAssembly(appDomain.RawObject, clsMod.RawObject);
			if (asm == null)
				return null;
			return asm.TryGetModule(clsMod.RawObject);
		}

		/// <summary>
		/// Re-add breakpoints to the module. Should be called if the debugged module has breakpoints
		/// in decrypted methods and the methods have now been decrypted.
		/// </summary>
		/// <param name="module"></param>
		public void AddBreakpoints(DnModule module) {
			foreach (var bp in ilCodeBreakpointList.GetBreakpoints(module.SerializedDnModule))
				bp.AddBreakpoint(module);
			foreach (var bp in nativeCodeBreakpointList.GetBreakpoints(module.SerializedDnModule))
				bp.AddBreakpoint(module);
		}

		/// <summary>
		/// Creates a debug event breakpoint
		/// </summary>
		/// <param name="eventKind">Debug event</param>
		/// <param name="cond">Condition</param>
		/// <returns></returns>
		public DnDebugEventBreakpoint CreateBreakpoint(DebugEventBreakpointKind eventKind, Func<DebugEventBreakpointConditionContext, bool> cond) {
			DebugVerifyThread();
			var bp = new DnDebugEventBreakpoint(eventKind, cond);
			debugEventBreakpointList.Add(bp);
			return bp;
		}

		/// <summary>
		/// Creates a debug event breakpoint that gets hit on all debug events
		/// </summary>
		/// <param name="cond">Condition</param>
		/// <returns></returns>
		public DnAnyDebugEventBreakpoint CreateAnyDebugEventBreakpoint(Func<AnyDebugEventBreakpointConditionContext, bool> cond) {
			DebugVerifyThread();
			var bp = new DnAnyDebugEventBreakpoint(cond);
			anyDebugEventBreakpointList.Add(bp);
			return bp;
		}

		/// <summary>
		/// Creates an IL instruction breakpoint
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="offset">IL offset</param>
		/// <param name="cond">Condition</param>
		/// <returns></returns>
		public DnILCodeBreakpoint CreateBreakpoint(SerializedDnModule module, uint token, uint offset, Func<ILCodeBreakpointConditionContext, bool> cond) {
			DebugVerifyThread();
			var bp = new DnILCodeBreakpoint(module, token, offset, cond);
			ilCodeBreakpointList.Add(module, bp);
			foreach (var dnMod in GetLoadedDnModules(module))
				bp.AddBreakpoint(dnMod);
			return bp;
		}

		/// <summary>
		/// Creates a native instruction breakpoint
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="offset">Offset</param>
		/// <param name="cond">Condition</param>
		/// <returns></returns>
		public DnNativeCodeBreakpoint CreateNativeBreakpoint(SerializedDnModule module, uint token, uint offset, Func<NativeCodeBreakpointConditionContext, bool> cond) {
			DebugVerifyThread();
			var bp = new DnNativeCodeBreakpoint(module, token, offset, cond);
			nativeCodeBreakpointList.Add(module, bp);
			foreach (var dnMod in GetLoadedDnModules(module))
				bp.AddBreakpoint(dnMod);
			return bp;
		}

		/// <summary>
		/// Creates a native instruction breakpoint
		/// </summary>
		/// <param name="code">Native code</param>
		/// <param name="offset">Offset</param>
		/// <param name="cond">Condition</param>
		/// <returns></returns>
		public DnNativeCodeBreakpoint CreateNativeBreakpoint(CorCode code, uint offset, Func<NativeCodeBreakpointConditionContext, bool> cond) {
			DebugVerifyThread();
			var bp = new DnNativeCodeBreakpoint(code, offset, cond);
			var func = code.Function;
			var mod = func == null ? null : func.Module;
			var module = mod == null ? new SerializedDnModule() : mod.SerializedDnModule;
			nativeCodeBreakpointList.Add(module, bp);
			foreach (var dnMod in GetLoadedDnModules(module))
				bp.AddBreakpoint(dnMod);
			return bp;
		}

		IEnumerable<DnModule> GetLoadedDnModules(SerializedDnModule module) {
			foreach (var process in processes.GetAll()) {
				foreach (var appDomain in process.AppDomains) {
					foreach (var assembly in appDomain.Assemblies) {
						foreach (var dnMod in assembly.Modules) {
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

			var adebp = bp as DnAnyDebugEventBreakpoint;
			if (adebp != null) {
				anyDebugEventBreakpointList.Remove(adebp);
				adebp.OnRemoved();
				return;
			}

			var ilbp = bp as DnILCodeBreakpoint;
			if (ilbp != null) {
				ilCodeBreakpointList.Remove(ilbp.Module, ilbp);
				ilbp.OnRemoved();
				return;
			}

			var nbp = bp as DnNativeCodeBreakpoint;
			if (nbp != null) {
				nativeCodeBreakpointList.Remove(nbp.Module, nbp);
				nbp.OnRemoved();
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
				PauseStates = new DebuggerPauseState[] {
					new DebuggerPauseState(DebuggerPauseReason.UserBreak),
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
			foreach (var bp in nativeCodeBreakpointList.GetBreakpoints())
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
					if (hr != 0) {
						Debug.Assert(hr == CordbgErrors.CORDBG_E_UNRECOVERABLE_ERROR);
						bool b = NativeMethods.TerminateProcess(process.CorProcess.Handle, uint.MaxValue);
						Debug.Assert(b);
					}
				}
				catch {
				}
			}
		}

		int nextThreadId = -1, nextProcessId = -1, nextModuleId = -1, nextAssemblyId = -1, nextAppDomainId = -1;
		internal int GetNextThreadId() {
			return Interlocked.Increment(ref nextThreadId);
		}
		internal int GetNextProcessId() {
			return Interlocked.Increment(ref nextProcessId);
		}
		internal int GetNextModuleId() {
			return Interlocked.Increment(ref nextModuleId);
		}
		internal int GetNextAssemblyId() {
			return Interlocked.Increment(ref nextAssemblyId);
		}
		internal int GetNextAppDomainId() {
			return Interlocked.Increment(ref nextAppDomainId);
		}

		public DnEval CreateEval() {
			DebugVerifyThread();
			Debug.Assert(ProcessState == DebuggerProcessState.Paused);

			return new DnEval(this, debugMessageDispatcher);
		}

		/// <summary>
		/// true if we're evaluating
		/// </summary>
		public bool IsEvaluating {
			get { return evalCounter != 0 && ProcessState != DebuggerProcessState.Terminated; }
		}

		/// <summary>
		/// true if an eval has completed
		/// </summary>
		public bool EvalCompleted {
			get { return evalCompletedCounter != 0; }
		}

		internal void EvalStarted() {
			DebugVerifyThread();
			Debug.Assert(ProcessState == DebuggerProcessState.Paused);

			evalCounter++;
			Continue();
		}
		int evalCounter;

		internal void EvalStopped() {
			DebugVerifyThread();

			evalCounter--;
		}

		public void SignalEvalComplete() {
			DebugVerifyThread();
			Debug.Assert(!IsEvaluating && ProcessState == DebuggerProcessState.Paused);
			evalCompletedCounter++;
			try {
				CallOnProcessStateChanged();
			}
			finally {
				evalCompletedCounter--;
			}
		}
		int evalCompletedCounter;

		public void DisposeHandle(CorValue value) {
			if (value == null || !value.IsHandle)
				return;
			if (ProcessState != DebuggerProcessState.Running)
				value.DisposeHandle();
			else
				disposeValues.Add(value);
		}
		readonly List<CorValue> disposeValues = new List<CorValue>();

		/// <summary>
		/// Gets all modules from all processes and app domains
		/// </summary>
		public IEnumerable<DnModule> Modules {
			get {
				DebugVerifyThread();
				foreach (var p in Processes) {
					foreach (var ad in p.AppDomains) {
						foreach (var asm in ad.Assemblies) {
							foreach (var mod in asm.Modules)
								yield return mod;
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets all assemblies from all processes and app domains
		/// </summary>
		public IEnumerable<DnAssembly> Assemblies {
			get {
				DebugVerifyThread();
				foreach (var p in Processes) {
					foreach (var ad in p.AppDomains) {
						foreach (var asm in ad.Assemblies)
							yield return asm;
					}
				}
			}
		}
	}
}
