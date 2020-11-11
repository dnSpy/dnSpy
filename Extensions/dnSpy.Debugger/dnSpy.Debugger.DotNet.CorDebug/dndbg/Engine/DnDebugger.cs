/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaHost;

namespace dndbg.Engine {
	delegate void DebugCallbackEventHandler(DnDebugger dbg, DebugCallbackEventArgs e);

	sealed class DnDebugger : IDisposable {
		readonly IDebugMessageDispatcher debugMessageDispatcher;
		readonly ICorDebug corDebug;
		readonly DebuggerCollection<ICorDebugProcess, DnProcess> processes;
		readonly DebugEventBreakpointList<DnDebugEventBreakpoint> debugEventBreakpointList = new DebugEventBreakpointList<DnDebugEventBreakpoint>();
		readonly DebugEventBreakpointList<DnAnyDebugEventBreakpoint> anyDebugEventBreakpointList = new DebugEventBreakpointList<DnAnyDebugEventBreakpoint>();
		readonly BreakpointList<DnILCodeBreakpoint> ilCodeBreakpointList = new BreakpointList<DnILCodeBreakpoint>();
		readonly BreakpointList<DnNativeCodeBreakpoint> nativeCodeBreakpointList = new BreakpointList<DnNativeCodeBreakpoint>();
		readonly Dictionary<CorStepper, StepInfo> stepInfos = new Dictionary<CorStepper, StepInfo>();
		readonly Dictionary<CorModule, DnModule> toDnModule = new Dictionary<CorModule, DnModule>();
		readonly List<(DnModule module, CorClass cls)> customNotificationList;
		PipeReaderInfo? outputPipe;
		PipeReaderInfo? errorPipe;
		DebugOptions debugOptions;

		sealed class StepInfo {
			public readonly Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? OnCompleted;

			public StepInfo(Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action) => OnCompleted = action;
		}

		public DebugOptions Options {
			get { DebugVerifyThread(); return debugOptions; }
			set { DebugVerifyThread(); debugOptions = value ?? new DebugOptions(); }
		}

		public DebuggerProcessState ProcessState {
			get {
				DebugVerifyThread();
				if (forceProcessTerminated)
					return DebuggerProcessState.Terminated;
				return ProcessStateInternal;
			}
		}
		bool hasReceivedCreateProcessEvent = false;

		DebuggerProcessState ProcessStateInternal {
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

		public void SetProcessTerminated() => forceProcessTerminated = true;
		bool forceProcessTerminated;

		public event EventHandler? OnAttachComplete;

		public event EventHandler<ThreadDebuggerEventArgs>? OnThreadAdded;
		void CallOnThreadAdded(DnThread thread, bool added, out bool shouldPause) {
			var e = new ThreadDebuggerEventArgs(thread, added);
			OnThreadAdded?.Invoke(this, e);
			shouldPause = e.ShouldPause;
		}

		public event EventHandler<AppDomainDebuggerEventArgs>? OnAppDomainAdded;
		void CallOnAppDomainAdded(DnAppDomain appDomain, bool added, out bool shouldPause) {
			var e = new AppDomainDebuggerEventArgs(appDomain, added);
			OnAppDomainAdded?.Invoke(this, e);
			shouldPause = e.ShouldPause;
		}

		public event EventHandler<AssemblyDebuggerEventArgs>? OnAssemblyAdded;
		void CallOnAssemblyAdded(DnAssembly assembly, bool added, out bool shouldPause) {
			var e = new AssemblyDebuggerEventArgs(assembly, added);
			OnAssemblyAdded?.Invoke(this, e);
			shouldPause = e.ShouldPause;
		}

		public event EventHandler<ModuleDebuggerEventArgs>? OnModuleAdded;
		void CallOnModuleAdded(DnModule module, bool added, out bool shouldPause) {
			var e = new ModuleDebuggerEventArgs(module, added);
			OnModuleAdded?.Invoke(this, e);
			shouldPause = e.ShouldPause;
		}

		public event EventHandler<NameChangedDebuggerEventArgs>? OnNameChanged;
		void CallOnNameChanged(DnAppDomain? appDomain, DnThread? thread) =>
			OnNameChanged?.Invoke(this, new NameChangedDebuggerEventArgs(appDomain, thread));

		public event EventHandler<DebuggerEventArgs>? OnProcessStateChanged;
		void CallOnProcessStateChanged() =>
			OnProcessStateChanged?.Invoke(this, DebuggerEventArgs.Empty);

		public event EventHandler<CorModuleDefCreatedEventArgs>? OnCorModuleDefCreated;
		internal void CorModuleDefCreated(DnModule module) {
			DebugVerifyThread();
			Debug2.Assert(module.CorModuleDef is not null);
			OnCorModuleDefCreated?.Invoke(this, new CorModuleDefCreatedEventArgs(module, module.CorModuleDef));
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

		public DebuggerState Current {
			get {
				DebugVerifyThread();
				if (debuggerStates.Count == 0)
					return new DebuggerState(null);
				return debuggerStates[debuggerStates.Count - 1];
			}
		}

		public DebuggerState[] DebuggerStates {
			get { DebugVerifyThread(); return debuggerStates.ToArray(); }
		}
		readonly List<DebuggerState> debuggerStates = new List<DebuggerState>();

		/// <summary>
		/// This is the debuggee version or an empty string if it's not known (eg. if it's CoreCLR)
		/// </summary>
		public string DebuggeeVersion { get; }

		/// <summary>
		/// Other version, used by .NET
		/// </summary>
		public string OtherVersion { get; }

		/// <summary>
		/// Path to the CLR dll (clr.dll, mscorwks.dll, mscorsvr.dll, coreclr.dll)
		/// </summary>
		public string CLRPath { get; }

		/// <summary>
		/// Path to the runtime directory
		/// </summary>
		public string RuntimeDirectory { get; }

		readonly int debuggerManagedThreadId;
		readonly bool isAttach;
		bool isAttaching;

		DnDebugger(ICorDebug corDebug, DebugOptions debugOptions, IDebugMessageDispatcher debugMessageDispatcher, string clrPath, string? debuggeeVersion, string? otherVersion, bool isAttach) {
			debuggerManagedThreadId = Thread.CurrentThread.ManagedThreadId;
			processes = new DebuggerCollection<ICorDebugProcess, DnProcess>(CreateDnProcess);
			this.isAttach = isAttach;
			isAttaching = isAttach;
			this.debugMessageDispatcher = debugMessageDispatcher ?? throw new ArgumentNullException(nameof(debugMessageDispatcher));
			this.corDebug = corDebug;
			customNotificationList = new List<(DnModule, CorClass)>();
			this.debugOptions = debugOptions ?? new DebugOptions();
			DebuggeeVersion = debuggeeVersion ?? string.Empty;
			OtherVersion = otherVersion ?? string.Empty;
			CLRPath = clrPath ?? throw new ArgumentNullException(nameof(clrPath));
			RuntimeDirectory = Path.GetDirectoryName(clrPath)!;

			// I have not tested debugging with CLR 1.x. It's too old to support it so this is a won't fix
			if (DebuggeeVersion.StartsWith("1."))
				throw new NotImplementedException("Can't debug .NET 1.x assemblies. Add an app.config file to force using .NET 2.0 or later");

			corDebug.Initialize();
			corDebug.SetManagedHandler(new CorDebugManagedCallback(this));
		}

		void ResetDebuggerStates() => debuggerStates.Clear();
		void AddDebuggerState(DebuggerState debuggerState) => debuggerStates.Add(debuggerState);
		DnProcess CreateDnProcess(ICorDebugProcess comProcess) => new DnProcess(this, comProcess, GetNextProcessId());

		[Conditional("DEBUG")]
		internal void DebugVerifyThread() => Debug.Assert(Thread.CurrentThread.ManagedThreadId == debuggerManagedThreadId);

		static ICorDebug CreateCorDebug(string debuggeeVersion, out string clrPath) {
			var clsid = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");
			var riid = typeof(ICLRMetaHost).GUID;
			var mh = (ICLRMetaHost)NativeMethods.CLRCreateInstance(ref clsid, ref riid);

			riid = typeof(ICLRRuntimeInfo).GUID;
			var rtInfo = (ICLRRuntimeInfo)mh.GetRuntime(debuggeeVersion, ref riid);
			clrPath = GetCLRPathDesktop(rtInfo, debuggeeVersion);

			clsid = new Guid("DF8395B5-A4BA-450B-A77C-A9A47762C520");
			riid = typeof(ICorDebug).GUID;
			return (ICorDebug)rtInfo.GetInterface(ref clsid, ref riid);
		}

		static string GetCLRPathDesktop(ICLRRuntimeInfo rtInfo, string debuggeeVersion) {
			uint chBuffer = 0;
			var sb = new StringBuilder(300);
			int hr = rtInfo.GetRuntimeDirectory(sb, ref chBuffer);
			sb.EnsureCapacity((int)chBuffer);
			hr = rtInfo.GetRuntimeDirectory(sb, ref chBuffer);

			string[] files;
			if (debuggeeVersion.StartsWith("v2."))
				files = clrFiles_v2;
			else
				files = clrFiles_v4;
			var basePath = sb.ToString();
			if (basePath.Length != 0) {
				foreach (var file in files) {
					var filename = Path.Combine(basePath, file);
					if (File.Exists(filename))
						return filename;
				}
			}
			throw new InvalidOperationException("Couldn't find the CLR dll file");
		}
		static readonly string[] clrFiles_v2 = new string[] { "mscorwks.dll", "mscorsvr.dll" };
		static readonly string[] clrFiles_v4 = new string[] { "clr.dll" };

		public event DebugCallbackEventHandler? DebugCallbackEvent;

		// Could be called from any thread
		internal void OnManagedCallbackFromAnyThread(Func<DebugCallbackEventArgs> func) => debugMessageDispatcher.ExecuteAsync(() => {
			try {
				var e = func();
				OnManagedCallbackInDebuggerThread(e);
			}
			catch {
				// most likely debugger has already stopped
				return;
			}
		});

		// Same as above method but called by CreateProcess, LoadModule, CreateAppDomain because
		// certain methods must be called before we return to the CLR debugger.
		internal void OnManagedCallbackFromAnyThread2(Func<DebugCallbackEventArgs> func) {
			using (var ev = new ManualResetEvent(false)) {
				debugMessageDispatcher.ExecuteAsync(() => {
					try {
						var e = func();
						OnManagedCallbackInDebuggerThread(e);
					}
					catch {
						// most likely debugger has already stopped
						return;
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
		public uint ContinueCounter { get; private set; }

		void DisposeOfHandles() {
			DebugVerifyThread();
			foreach (var value in disposeValues)
				value.DisposeHandle();
			disposeValues.Clear();
		}

		void OnManagedCallbackInDebuggerThread(DebugCallbackEventArgs e) {
			DebugVerifyThread();
			if (hasTerminated)
				return;
			managedCallbackCounter++;

			if (disposeValues.Count != 0)
				DisposeOfHandles();

			try {
				HandleManagedCallback(e);
				CheckBreakpoints(e);
				DebugCallbackEvent?.Invoke(this, e);
			}
			catch (Exception ex) {
				Debug.WriteLine($"dndbg: EX:\n\n{ex}");
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
			if (thread is null)
				return false;
			if (e.CorDebugController is null)
				return false;
			int hr = e.CorDebugController.HasQueuedCallbacks(thread.CorThread.RawObject, out int qcbs);
			return hr >= 0 && qcbs != 0;
		}

		bool HasAnyQueuedCallbacks(DebugCallbackEventArgs e) {
			if (e.CorDebugController is null)
				return false;
			int hr = e.CorDebugController.HasQueuedCallbacks(null, out int qcbs);
			return hr >= 0 && qcbs != 0;
		}

		// This method must be called just before returning to the caller. No fields can be accessed
		// and no methods can be called because the CLR debugger could call us before this method
		// returns.
		bool Continue(ICorDebugController? controller, bool callOnProcessStateChanged) {
			Debug2.Assert(controller is not null);
			if (controller is null)
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
			ContinueCounter++;

			if (callOnProcessStateChanged && managedCallbackCounter == 0)
				CallOnProcessStateChanged();

			// As soon as we call Continue(), the CLR debugger could send us another message so it's
			// important that we don't access any of our fields and don't call any methods after
			// Continue() has been called!
			int hr = controller.Continue(0);
			bool success = hr >= 0 || hr == CordbgErrors.CORDBG_E_PROCESS_TERMINATED || hr == CordbgErrors.CORDBG_E_OBJECT_NEUTERED;
			Debug.WriteLineIf(!success, $"dndbg: ICorDebugController::Continue() failed: 0x{hr:X8}");
			return success;
		}
		bool continuing = false;

		public bool CanContinue {
			get { DebugVerifyThread(); return ProcessStateInternal == DebuggerProcessState.Paused; }
		}

		public void Continue() {
			DebugVerifyThread();
			if (!CanContinue)
				return;
			Debug.Assert(managedCallbackCounter > 0);
			if (managedCallbackCounter <= 0)
				return;

			var controller = Current.Controller;
			Debug2.Assert(controller is not null);
			if (controller is null)
				return;

			ResetDebuggerStates();
			while (managedCallbackCounter > 0) {
				if (!Continue(controller, true))	// Also decrements managedCallbackCounter
					return;
			}
		}

		public bool CanStep() => CanStep(Current.ILFrame);

		public bool CanStep(CorFrame? frame) {
			DebugVerifyThread();
			return ProcessStateInternal == DebuggerProcessState.Paused && frame is not null;
		}

		CorStepper? CreateStepper(CorFrame frame) {
			if (frame is null)
				return null;

			var stepper = frame.CreateStepper();
			if (stepper is null)
				return null;
			if (!stepper.SetInterceptMask(debugOptions.StepperInterceptMask))
				return null;
			if (!stepper.SetUnmappedStopMask(debugOptions.StepperUnmappedStopMask))
				return null;
			if (!stepper.SetJMC(debugOptions.StepperJMC))
				return null;

			return stepper;
		}

		public CorStepper? StepOut(Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) => StepOut(Current.ILFrame, action);
		public CorStepper? StepOut(CorFrame? frame, Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			DebugVerifyThread();
			return Step(frame, StepKind.StepOut, action);
		}

		public CorStepper? StepInto(Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			DebugVerifyThread();
			return StepInto(Current.ILFrame, action);
		}

		public CorStepper? StepInto(CorFrame? frame, Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			DebugVerifyThread();
			return Step(frame, StepKind.StepInto, action);
		}

		public CorStepper? StepOver(Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			DebugVerifyThread();
			return StepOver(Current.ILFrame, action);
		}

		public CorStepper? StepOver(CorFrame? frame, Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			DebugVerifyThread();
			return Step(frame, StepKind.StepOver, action);
		}

		enum StepKind {
			StepInto,
			StepOver,
			StepOut,
		}

		CorStepper? Step(CorFrame? frame, StepKind step, Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			if (!CanStep(frame))
				return null;
			Debug2.Assert(frame is not null);

			var stepper = CreateStepper(frame);
			if (stepper is null)
				return null;
			switch (step) {
			case StepKind.StepInto:
				if (!stepper.Step(true))
					return null;
				break;
			case StepKind.StepOver:
				if (!stepper.Step(false))
					return null;
				break;
			case StepKind.StepOut:
				if (!stepper.StepOut())
					return null;
				break;
			default: throw new ArgumentOutOfRangeException(nameof(step));
			}

			stepInfos.Add(stepper, new StepInfo(action));
			return stepper;
		}

		public CorStepper? StepInto(StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			DebugVerifyThread();
			return StepInto(Current.ILFrame, ranges, action);
		}

		public CorStepper? StepInto(CorFrame? frame, StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			DebugVerifyThread();
			return StepIntoOver(frame, ranges, true, action);
		}

		public CorStepper? StepOver(StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			DebugVerifyThread();
			return StepOver(Current.ILFrame, ranges, action);
		}

		public CorStepper? StepOver(CorFrame? frame, StepRange[] ranges, Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			DebugVerifyThread();
			return StepIntoOver(frame, ranges, false, action);
		}

		CorStepper? StepIntoOver(CorFrame? frame, StepRange[] ranges, bool stepInto, Action<DnDebugger, StepCompleteDebugCallbackEventArgs?, bool>? action = null) {
			if (ranges is null)
				return Step(frame, stepInto ? StepKind.StepInto : StepKind.StepOver, action);
			if (!CanStep(frame))
				return null;
			Debug2.Assert(frame is not null);

			var stepper = CreateStepper(frame);
			if (stepper is null)
				return null;
			if (!stepper.StepRange(stepInto, ranges))
				return null;

			stepInfos.Add(stepper, new StepInfo(action));
			return stepper;
		}

		internal void CancelStep(CorStepper stepper) {
			DebugVerifyThread();
			stepper.Deactivate();
			if (stepInfos.TryGetValue(stepper, out var stepInfo)) {
				stepInfos.Remove(stepper);
				stepInfo.OnCompleted?.Invoke(this, null, true);
			}
		}

		void SetDefaultCurrentProcess(DebugCallbackEventArgs e) {
			var ps = Processes;
			AddDebuggerState(new DebuggerState(e, ps.Length == 0 ? null : ps[0], null, null));
		}

		void InitializeCurrentDebuggerState(DebugCallbackEventArgs e, ICorDebugProcess? comProcess, ICorDebugAppDomain? comAppDomain, ICorDebugThread? comThread) {
			if (comThread is not null) {
				if (comProcess is null)
					comThread.GetProcess(out comProcess);
				if (comAppDomain is null)
					comThread.GetAppDomain(out comAppDomain);
			}

			if (comAppDomain is not null) {
				if (comProcess is null)
					comAppDomain.GetProcess(out comProcess);
			}

			var process = TryGetValidProcess(comProcess);
			DnAppDomain? appDomain;
			DnThread? thread;
			if (process is not null) {
				appDomain = process.TryGetAppDomain(comAppDomain);
				thread = process.TryGetThread(comThread);
			}
			else {
				appDomain = null;
				thread = null;
			}

			if (thread is null && appDomain is null && process is not null)
				appDomain = process.AppDomains.FirstOrDefault();
			if (thread is null) {
				if (appDomain is not null)
					thread = appDomain.Threads.FirstOrDefault();
				else if (process is not null)
					thread = process.Threads.FirstOrDefault();
			}

			if (process is null)
				SetDefaultCurrentProcess(e);
			else
				AddDebuggerState(new DebuggerState(e, process, appDomain, thread));
		}

		void InitializeCurrentDebuggerState(DebugCallbackEventArgs e, DnProcess? process) {
			if (process is null) {
				SetDefaultCurrentProcess(e);
				return;
			}

			AddDebuggerState(new DebuggerState(e, process, null, null));
		}

		void OnProcessTerminated(DnProcess? process) {
			if (process is null)
				return;

			foreach (var appDomain in process.AppDomains) {
				OnAppDomainUnloaded(appDomain);
				process.AppDomainExited(appDomain.CorAppDomain.RawObject);
			}
		}

		void OnAppDomainUnloaded(DnAppDomain? appDomain) {
			if (appDomain is null)
				return;
			foreach (var assembly in appDomain.Assemblies) {
				OnAssemblyUnloaded(assembly);
				appDomain.AssemblyUnloaded(assembly.CorAssembly.RawObject);
			}
		}

		void OnAssemblyUnloaded(DnAssembly? assembly) {
			if (assembly is null)
				return;
			foreach (var module in assembly.Modules)
				OnModuleUnloaded(module);
		}

		void OnModuleUnloaded(DnModule module) {
			bool b = toDnModule.Remove(module.CorModule);
			Debug.Assert(b);
			module.Assembly.ModuleUnloaded(module);
			RemoveModuleFromBreakpoints(module);
		}

		void RemoveModuleFromBreakpoints(DnModule module) {
			if (module is null)
				return;
			foreach (var bp in ilCodeBreakpointList.GetBreakpoints(module.DnModuleId))
				bp.RemoveModule(module);
			foreach (var bp in nativeCodeBreakpointList.GetBreakpoints(module.DnModuleId))
				bp.RemoveModule(module);
		}

		void HandleManagedCallback(DebugCallbackEventArgs e) {
			bool b, shouldPause;
			DnProcess? process;
			DnAppDomain? appDomain;
			DnAssembly? assembly;
			CorClass? cls;
			switch (e.Kind) {
			case DebugCallbackKind.Breakpoint:
				var bpArgs = (BreakpointDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, bpArgs.AppDomain, bpArgs.Thread);
				break;

			case DebugCallbackKind.StepComplete:
				var scArgs = (StepCompleteDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, scArgs.AppDomain, scArgs.Thread);
				StepInfo? stepInfo;
				var stepperKey = scArgs.CorStepper;
				if (stepperKey is not null && stepInfos.TryGetValue(stepperKey, out stepInfo)) {
					stepInfos.Remove(stepperKey);
					stepInfo.OnCompleted?.Invoke(this, scArgs, false);
				}
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
				if (process is not null) {
					process.CorProcess.EnableLogMessages(debugOptions.LogMessages);
					process.CorProcess.DesiredNGENCompilerFlags = debugOptions.DebugOptionsProvider.GetDesiredNGENCompilerFlags(process);
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
				if (process is not null)
					process.SetHasExited();
				processes.Remove(epArgs.Process);
				OnProcessTerminated(process);
				if (processes.Count == 0)
					ProcessesTerminated();
				break;

			case DebugCallbackKind.CreateThread:
				var ctArgs = (CreateThreadDebugCallbackEventArgs)e;
				process = TryGetValidProcess(ctArgs.Thread);
				if (process is not null) {
					var dnThread = process.TryAdd(ctArgs.Thread);
					if (dnThread is not null) {
						CallOnThreadAdded(dnThread, true, out shouldPause);
						if (shouldPause)
							e.AddPauseReason(DebuggerPauseReason.Other);
					}
				}
				InitializeCurrentDebuggerState(e, null, ctArgs.AppDomain, ctArgs.Thread);
				if (isAttaching && !HasAnyQueuedCallbacks(e)) {
					isAttaching = false;
					OnAttachComplete?.Invoke(this, EventArgs.Empty);
				}
				break;

			case DebugCallbackKind.ExitThread:
				var etArgs = (ExitThreadDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, etArgs.AppDomain, etArgs.Thread);
				process = TryGetValidProcess(etArgs.Thread);
				if (process is not null) {
					var dnThread = process.ThreadExited(etArgs.Thread);
					if (dnThread is not null) {
						CallOnThreadAdded(dnThread, false, out shouldPause);
						if (shouldPause)
							e.AddPauseReason(DebuggerPauseReason.Other);
					}
				}
				break;

			case DebugCallbackKind.LoadModule:
				var lmArgs = (LoadModuleDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, lmArgs.AppDomain, null);
				assembly = TryGetValidAssembly(lmArgs.AppDomain, lmArgs.Module);
				if (assembly is not null) {
					var module = assembly.TryAdd(lmArgs.Module)!;
					toDnModule.Add(module.CorModule, module);

					var moduleOptions = debugOptions.DebugOptionsProvider.GetModuleLoadOptions(module);
					module.CorModule.EnableJITDebugging(moduleOptions.ModuleTrackJITInfo, moduleOptions.ModuleAllowJitOptimizations);
					module.CorModule.JITCompilerFlags = moduleOptions.JITCompilerFlags;
					module.CorModule.SetJMCStatus(moduleOptions.JustMyCode);
					module.CorModule.EnableClassLoadCallbacks(false);

					module.InitializeCachedValues();
					AddBreakpoints(module);

					CallOnModuleAdded(module, true, out shouldPause);
					if (shouldPause)
						e.AddPauseReason(DebuggerPauseReason.Other);
				}
				break;

			case DebugCallbackKind.UnloadModule:
				var umArgs = (UnloadModuleDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, umArgs.AppDomain, null);
				assembly = TryGetValidAssembly(umArgs.AppDomain, umArgs.Module);
				if (assembly is not null) {
					var module = assembly.TryGetModule(umArgs.Module);
					if (module is not null) {
						OnModuleUnloaded(module);
						CallOnModuleAdded(module, false, out shouldPause);
						if (shouldPause)
							e.AddPauseReason(DebuggerPauseReason.Other);
					}
				}
				break;

			case DebugCallbackKind.LoadClass:
				var lcArgs = (LoadClassDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, lcArgs.AppDomain, null);

				cls = lcArgs.CorClass;
				if (cls is not null) {
					var module = TryGetModule(lcArgs.CorAppDomain, cls);
					if (module is not null) {
						if (module.CorModuleDef is not null)
							module.CorModuleDef.LoadClass(cls.Token);
						if (module.IsDynamic) {
							foreach (var bp in ilCodeBreakpointList.GetBreakpoints(module.DnModuleId))
								bp.AddBreakpoint(module);
							foreach (var bp in nativeCodeBreakpointList.GetBreakpoints(module.DnModuleId))
								bp.AddBreakpoint(module);
						}
					}
				}
				break;

			case DebugCallbackKind.UnloadClass:
				var ucArgs = (UnloadClassDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, ucArgs.AppDomain, null);

				cls = ucArgs.CorClass;
				if (cls is not null) {
					var module = TryGetModule(ucArgs.CorAppDomain, cls);
					if (module is not null && module.CorModuleDef is not null)
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
				appDomain = null;
				if (process is not null && cadArgs.AppDomain is not null) {
					b = cadArgs.AppDomain.Attach() >= 0;
					Debug.WriteLineIf(!b, $"CreateAppDomain: could not attach to AppDomain: {cadArgs.AppDomain.GetHashCode():X8}");
					if (b)
						appDomain = process.TryAdd(cadArgs.AppDomain);
				}
				InitializeCurrentDebuggerState(e, cadArgs.Process, cadArgs.AppDomain, null);
				if (appDomain is not null) {
					CallOnAppDomainAdded(appDomain, true, out shouldPause);
					if (shouldPause)
						e.AddPauseReason(DebuggerPauseReason.Other);
				}
				break;

			case DebugCallbackKind.ExitAppDomain:
				var eadArgs = (ExitAppDomainDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, eadArgs.Process, eadArgs.AppDomain, null);
				process = processes.TryGet(eadArgs.Process);
				if (process is not null) {
					UpdateCustomNotificationList(eadArgs.CorAppDomain);
					OnAppDomainUnloaded(appDomain = process.TryGetAppDomain(eadArgs.AppDomain));
					if (appDomain is not null) {
						CallOnAppDomainAdded(appDomain, false, out shouldPause);
						if (shouldPause)
							e.AddPauseReason(DebuggerPauseReason.Other);
					}
					process.AppDomainExited(eadArgs.AppDomain);
				}
				break;

			case DebugCallbackKind.LoadAssembly:
				var laArgs = (LoadAssemblyDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, laArgs.AppDomain, null);
				appDomain = TryGetValidAppDomain(laArgs.AppDomain);
				if (appDomain is not null) {
					assembly = appDomain.TryAdd(laArgs.Assembly);
					if (assembly is not null) {
						CallOnAssemblyAdded(assembly, true, out shouldPause);
						if (shouldPause)
							e.AddPauseReason(DebuggerPauseReason.Other);
					}
				}
				break;

			case DebugCallbackKind.UnloadAssembly:
				var uaArgs = (UnloadAssemblyDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, uaArgs.AppDomain, null);
				appDomain = TryGetAppDomain(uaArgs.AppDomain);
				if (appDomain is not null) {
					OnAssemblyUnloaded(assembly = appDomain.TryGetAssembly(uaArgs.Assembly));
					if (assembly is not null) {
						CallOnAssemblyAdded(assembly, false, out shouldPause);
						if (shouldPause)
							e.AddPauseReason(DebuggerPauseReason.Other);
					}
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
				if (appDomain is not null)
					appDomain.NameChanged();
				var thread = TryGetValidThread(ncArgs.Thread);
				if (thread is not null)
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
				var moduleId = TryGetModuleId(bpseArgs.CorFunctionBreakpoint?.Function?.Module);
				if (moduleId is not null) {
					foreach (var bp in ilCodeBreakpointList.GetBreakpoints(moduleId.Value)) {
						if (bp.IsBreakpoint(bpseArgs.Breakpoint))
							bp.SetError(DnCodeBreakpointError.CouldNotCreateBreakpoint);
					}
					foreach (var bp in nativeCodeBreakpointList.GetBreakpoints(moduleId.Value)) {
						if (bp.IsBreakpoint(bpseArgs.Breakpoint))
							bp.SetError(DnCodeBreakpointError.CouldNotCreateBreakpoint);
					}
				}
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
				InitializeCurrentDebuggerState(e, mdanArgs.Controller as ICorDebugProcess, mdanArgs.Controller as ICorDebugAppDomain, mdanArgs.Thread);
				break;

			case DebugCallbackKind.CustomNotification:
				var cnArgs = (CustomNotificationDebugCallbackEventArgs)e;
				InitializeCurrentDebuggerState(e, null, cnArgs.AppDomain, cnArgs.Thread);
				break;

			default:
				InitializeCurrentDebuggerState(e, null);
				Debug.Fail($"Unknown debug callback type: {e.Kind}");
				break;
			}
		}

		void CheckBreakpoints(DebugCallbackEventArgs e) {
			// Never check breakpoints when we're evaluating
			if (IsEvaluating)
				return;

			var type = DnDebugEventBreakpoint.GetDebugEventBreakpointKind(e);
			if (type is not null) {
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

					if (bp.IsEnabled && bp.Condition(new ILCodeBreakpointConditionContext(this, bp, bpArgs)))
						e.AddPauseState(new ILCodeBreakpointPauseState(bp, bpArgs.CorAppDomain, bpArgs.CorThread));
					break;
				}
				foreach (var bp in nativeCodeBreakpointList.GetBreakpoints()) {
					if (!bp.IsBreakpoint(bpArgs.Breakpoint))
						continue;

					if (bp.IsEnabled && bp.Condition(new NativeCodeBreakpointConditionContext(this, bp, bpArgs)))
						e.AddPauseState(new NativeCodeBreakpointPauseState(bp, bpArgs.CorAppDomain, bpArgs.CorThread));
					break;
				}
			}

			if (e.Kind == DebugCallbackKind.Break && !debugOptions.IgnoreBreakInstructions) {
				var b = (BreakDebugCallbackEventArgs)e;
				e.AddPauseState(new BreakPauseState(b.CorAppDomain, b.CorThread));
			}
		}

		void ProcessesTerminated() {
			if (!hasTerminated) {
				hasTerminated = true;
				corDebug.Terminate();
				ResetDebuggerStates();
				CallOnProcessStateChanged();
				foreach (var kv in stepInfos)
					kv.Value.OnCompleted?.Invoke(this, null, true);
				stepInfos.Clear();
				outputPipe?.Dispose();
				errorPipe?.Dispose();
				outputPipe = null!;
				errorPipe = null!;
			}
		}
		volatile bool hasTerminated = false;

		public static DnDebugger DebugProcess(DebugProcessOptions options) {
			if (options.DebugMessageDispatcher is null)
				throw new ArgumentException("DebugMessageDispatcher is null");
			var dbg = CreateDnDebugger(options);
			if (dbg is null)
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
			var debuggeeVersion = clrType.DebuggeeVersion ?? DebuggeeVersionDetector.GetVersion(options.Filename!);
			var corDebug = CreateCorDebug(debuggeeVersion, out string clrPath);
			if (corDebug is null)
				throw new Exception("Could not create an ICorDebug instance");
			var dbg = new DnDebugger(corDebug, options.DebugOptions!, options.DebugMessageDispatcher!, clrPath, debuggeeVersion, null, isAttach: false);
			if (options.BreakProcessKind != BreakProcessKind.None)
				new BreakProcessHelper(dbg, options.BreakProcessKind);
			dbg.CreateProcess(options);
			return dbg;
		}

		static (PipeReaderInfo outputPipe, PipeReaderInfo errorPipe) CreatePipes(DebugProcessOptions options) {
			if (!options.RedirectConsoleOutput)
				return default;
			// It's very likely that the encodings will match but there's no guarantee, eg. it writes to the property
			var encoding = Console.OutputEncoding;
			var outputPipe = new PipeReaderInfo(encoding);
			var errorPipe = new PipeReaderInfo(encoding);
			return (outputPipe, errorPipe);
		}

		static DnDebugger CreateDnDebuggerCoreCLR(DebugProcessOptions options) {
			var clrType = (CoreCLRTypeDebugInfo)options.CLRTypeDebugInfo;
			var pipeInfo = CreatePipes(options);
			try {
				var dbg2 = CoreCLRHelper.CreateDnDebugger(options, clrType, pipeInfo.outputPipe?.DangerousGetClientHandle() ?? default,
					pipeInfo.errorPipe?.DangerousGetClientHandle() ?? default, () => false, (cd, coreclrFilename, pid, version) => {
					var dbg = new DnDebugger(cd, options.DebugOptions!, options.DebugMessageDispatcher!, coreclrFilename, null, version, isAttach: false);
					(dbg.outputPipe, dbg.errorPipe) = pipeInfo;
					if (options.BreakProcessKind != BreakProcessKind.None)
						new BreakProcessHelper(dbg, options.BreakProcessKind);
					cd.DebugActiveProcess((int)pid, 0, out var comProcess);
					var dnProcess = dbg.TryAdd(comProcess);
					if (dnProcess is not null)
						dnProcess.Initialize(options.Filename, options.CurrentDirectory, options.CommandLine);
					if (options.RedirectConsoleOutput)
						dbg.ReadPipesAsync();
					return dbg;
				});
				if (dbg2 is null)
					throw new Exception("Could not create a debugger instance");
				return dbg2;
			}
			catch {
				pipeInfo.outputPipe?.Dispose();
				pipeInfo.errorPipe?.Dispose();
				throw;
			}
		}

		sealed class PipeReaderInfo {
			readonly AnonymousPipeServerStream pipe;
			readonly StreamReader streamReader;
			readonly char[] buffer;
			Task<int>? task;
			const int bufferSize = 0x200;

			public PipeReaderInfo(Encoding encoding) {
				pipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
				streamReader = new StreamReader(pipe, encoding, detectEncodingFromByteOrderMarks: true);
				buffer = new char[bufferSize];
			}

			public IntPtr DangerousGetClientHandle() => pipe.ClientSafePipeHandle.DangerousGetHandle();

			public Task<int> Read() {
				if (task is null)
					task = streamReader.ReadAsync(buffer, 0, buffer.Length);
				return task;
			}

			public string? TryGetString() {
				var t = task;
				task = null;
				int length = t!.GetAwaiter().GetResult();
				return length == 0 ? null : new string(buffer, 0, length);
			}

			public void Dispose() {
				pipe.DisposeLocalCopyOfClientHandle();
				pipe.Dispose();
			}
		}

		public event EventHandler<RedirectedOutputEventArgs>? OnRedirectedOutput;
		async void ReadPipesAsync() {
			var waitTasks = new Task[2];
			var outputPipe = this.outputPipe!;
			var errorPipe = this.errorPipe!;
			for (;;) {
				if (hasTerminated)
					return;
				var outputTask = outputPipe.Read();
				var errorTask = errorPipe.Read();
				waitTasks[0] = outputTask;
				waitTasks[1] = errorTask;
				Debug.Assert(waitTasks.Length == 2);
				var task = await Task.WhenAny(waitTasks);
				if (hasTerminated)
					return;
				PipeReaderInfo pipe;
				if (task == outputTask)
					pipe = outputPipe;
				else if (task == errorTask)
					pipe = errorPipe;
				else
					throw new InvalidOperationException();
				var text = pipe.TryGetString();
				if (text is null)
					return;
				OnRedirectedOutput?.Invoke(this, new RedirectedOutputEventArgs(text, isStandardOutput: task == outputTask));
			}
		}

		void CreateProcess(DebugProcessOptions options) {
			ICorDebugProcess comProcess;
			PROCESS_INFORMATION pi = default;
			try {
				(outputPipe, errorPipe) = CreatePipes(options);
				var dwCreationFlags = options.ProcessCreationFlags ?? DebugProcessOptions.DefaultProcessCreationFlags;
				var si = new STARTUPINFO();
				si.cb = (uint)(4 * 1 + IntPtr.Size * 3 + 4 * 8 + 2 * 2 + IntPtr.Size * 4);
				if (options.RedirectConsoleOutput) {
					si.hStdOutput = outputPipe.DangerousGetClientHandle();
					si.hStdError = errorPipe.DangerousGetClientHandle();
					si.dwFlags |= STARTUPINFO.STARTF_USESTDHANDLES;
				}
				var cmdline = "\"" + options.Filename + "\"";
				if (!string.IsNullOrEmpty(options.CommandLine))
					cmdline = cmdline + " " + options.CommandLine;
				var env = Win32EnvironmentStringBuilder.CreateEnvironmentUnicodeString(options.Environment!);
				dwCreationFlags |= ProcessCreationFlags.CREATE_UNICODE_ENVIRONMENT;
				bool inheritHandles = options.InheritHandles || options.RedirectConsoleOutput;
				corDebug.CreateProcess(options.Filename ?? string.Empty, cmdline, IntPtr.Zero, IntPtr.Zero,
							inheritHandles ? 1 : 0, dwCreationFlags, env, options.CurrentDirectory,
							ref si, ref pi, CorDebugCreateProcessFlags.DEBUG_NO_SPECIAL_OPTIONS, out comProcess);
				if (options.RedirectConsoleOutput)
					ReadPipesAsync();
			}
			catch {
				ProcessesTerminated();
				throw;
			}
			finally {
				if (pi.hProcess != IntPtr.Zero)
					NativeMethods.CloseHandle(pi.hProcess);
				if (pi.hThread != IntPtr.Zero)
					NativeMethods.CloseHandle(pi.hThread);
			}

			var process = TryAdd(comProcess);
			if (process is not null)
				process.Initialize(options.Filename, options.CurrentDirectory, options.CommandLine);
		}

		public static DnDebugger Attach(AttachProcessOptions options) {
			string? filename;
			using (var process = Process.GetProcessById(options.ProcessId))
				filename = process.MainModule?.FileName;
			var corDebug = CreateCorDebug(options, out var debuggeeVersion, out var clrPath, out var otherVersion);
			if (corDebug is null)
				throw new Exception("An ICorDebug instance couldn't be created");
			var dbg = new DnDebugger(corDebug, options.DebugOptions, options.DebugMessageDispatcher!, clrPath, debuggeeVersion, otherVersion, isAttach: true);
			corDebug.DebugActiveProcess(options.ProcessId, 0, out var comProcess);
			var dnProcess = dbg.TryAdd(comProcess);
			if (dnProcess is not null)
				dnProcess.Initialize(filename, string.Empty, string.Empty);
			return dbg;
		}

		static ICorDebug? CreateCorDebug(AttachProcessOptions options, out string? debuggeeVersion, out string clrPath, out string? otherVersion) {
			switch (options.CLRTypeAttachInfo.CLRType) {
			case CLRType.Desktop:	return CreateCorDebugDesktop(options, out debuggeeVersion, out clrPath, out otherVersion);
			case CLRType.CoreCLR:	return CreateCorDebugCoreCLR(options, out debuggeeVersion, out clrPath, out otherVersion);
			default:
				Debug.Fail("Invalid CLRType");
				throw new InvalidOperationException();
			}
		}

		static ICorDebug CreateCorDebugDesktop(AttachProcessOptions options, out string debuggeeVersion, out string clrPath, out string? otherVersion) {
			otherVersion = null;
			var clrType = (DesktopCLRTypeAttachInfo)options.CLRTypeAttachInfo;
			var dbgVersion = clrType.DebuggeeVersion;
			ICLRRuntimeInfo? rtInfo = null;
			using (var process = Process.GetProcessById(options.ProcessId)) {
				foreach (var t in GetCLRRuntimeInfos(process)) {
					if (string.IsNullOrEmpty(clrType.DebuggeeVersion) || t.versionString == clrType.DebuggeeVersion) {
						rtInfo = t.rtInfo;
						if (string.IsNullOrEmpty(dbgVersion))
							dbgVersion = t.versionString;
						break;
					}
				}
			}
			if (rtInfo is null || dbgVersion is null)
				throw new Exception("Couldn't find a .NET runtime or the correct .NET runtime");
			debuggeeVersion = dbgVersion;

			clrPath = GetCLRPathDesktop(rtInfo, dbgVersion);

			var clsid = new Guid("DF8395B5-A4BA-450B-A77C-A9A47762C520");
			var riid = typeof(ICorDebug).GUID;
			return (ICorDebug)rtInfo.GetInterface(ref clsid, ref riid);
		}

		static ICorDebug? CreateCorDebugCoreCLR(AttachProcessOptions options, out string? debuggeeVersion, out string clrPath, out string? otherVersion) {
			debuggeeVersion = null;
			var clrType = (CoreCLRTypeAttachInfo)options.CLRTypeAttachInfo;
			return CoreCLRHelper.CreateCorDebug(options.ProcessId, clrType, out clrPath, out otherVersion);
		}

		static IEnumerable<(string versionString, ICLRRuntimeInfo rtInfo)> GetCLRRuntimeInfos(Process process) {
			var clsid = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");
			var riid = typeof(ICLRMetaHost).GUID;
			var mh = (ICLRMetaHost)NativeMethods.CLRCreateInstance(ref clsid, ref riid);

			int hr = mh.EnumerateLoadedRuntimes(process.Handle, out var iter);
			if (hr < 0)
				yield break;
			for (;;) {
				hr = iter.Next(1, out object obj, out uint fetched);
				if (hr < 0 || fetched == 0)
					break;

				var rtInfo = (ICLRRuntimeInfo)obj;
				uint chBuffer = 0;
				var sb = new StringBuilder(300);
				hr = rtInfo.GetVersionString(sb, ref chBuffer);
				sb.EnsureCapacity((int)chBuffer);
				hr = rtInfo.GetVersionString(sb, ref chBuffer);

				yield return (sb.ToString(), rtInfo);
			}
		}

		DnProcess? TryAdd(ICorDebugProcess? comProcess) {
			if (comProcess is null)
				return null;

			// This method is called twice, once from DebugProcess() and once from the CreateProcess
			// handler. It's possible that it's been terminated before DebugProcess() calls this method.

			// Check if it's terminated. Error should be 0x8013134F: CORDBG_E_OBJECT_NEUTERED
			if (comProcess.IsRunning(out int running) < 0)
				return null;

			return processes.Add(comProcess);
		}

		public DnProcess[] Processes {
			get {
				DebugVerifyThread();
				var list = processes.GetAll();
				Array.Sort(list, (a, b) => a.UniqueId.CompareTo(b.UniqueId));
				return list;
			}
		}

		public DnProcess? TryGetValidProcess(ICorDebugProcess? comProcess) {
			DebugVerifyThread();
			var process = processes.TryGet(comProcess);
			if (process is null)
				return null;
			if (!process.CheckValid())
				return null;
			return process;
		}

		public DnProcess? TryGetValidProcess(ICorDebugAppDomain comAppDomain) {
			DebugVerifyThread();
			if (comAppDomain is null)
				return null;
			int hr = comAppDomain.GetProcess(out var comProcess);
			if (hr < 0)
				return null;
			return TryGetValidProcess(comProcess);
		}

		public DnProcess? TryGetValidProcess(ICorDebugThread? comThread) {
			DebugVerifyThread();
			if (comThread is null)
				return null;
			int hr = comThread.GetProcess(out var comProcess);
			if (hr < 0)
				return null;
			return TryGetValidProcess(comProcess);
		}

		DnAppDomain? TryGetAppDomain(ICorDebugAppDomain? comAppDomain) {
			DebugVerifyThread();
			if (comAppDomain is null)
				return null;
			int hr = comAppDomain.GetProcess(out var comProcess);
			if (hr < 0)
				return null;
			var process = processes.TryGet(comProcess);
			return process?.TryGetAppDomain(comAppDomain);
		}

		public DnAppDomain? TryGetValidAppDomain(ICorDebugAppDomain? comAppDomain) {
			DebugVerifyThread();
			if (comAppDomain is null)
				return null;
			int hr = comAppDomain.GetProcess(out var comProcess);
			if (hr < 0)
				return null;
			return TryGetValidAppDomain(comProcess, comAppDomain);
		}

		public DnAppDomain? TryGetValidAppDomain(ICorDebugProcess comProcess, ICorDebugAppDomain comAppDomain) {
			DebugVerifyThread();
			var process = TryGetValidProcess(comProcess);
			if (process is null)
				return null;
			return process.TryGetValidAppDomain(comAppDomain);
		}

		public DnAssembly? TryGetValidAssembly(ICorDebugAppDomain? comAppDomain, ICorDebugModule? comModule) {
			DebugVerifyThread();
			if (comModule is null)
				return null;

			var appDomain = TryGetValidAppDomain(comAppDomain);
			if (appDomain is null)
				return null;

			int hr = comModule.GetAssembly(out var comAssembly);
			if (hr < 0)
				return null;

			return appDomain.TryGetAssembly(comAssembly);
		}

		public DnThread? TryGetValidThread(ICorDebugThread? comThread) {
			DebugVerifyThread();
			var process = TryGetValidProcess(comThread);
			return process?.TryGetValidThread(comThread);
		}

		public DnModule? TryGetModule(CorAppDomain? appDomain, CorClass? cls) {
			if (appDomain is null || cls is null)
				return null;
			var clsMod = cls.Module;
			if (clsMod is null)
				return null;
			var ad = TryGetAppDomain(appDomain.RawObject);
			if (ad is null)
				return null;

			var asm = TryGetValidAssembly(appDomain.RawObject, clsMod.RawObject);
			if (asm is null)
				return null;
			return asm.TryGetModule(clsMod.RawObject);
		}

		public void AddBreakpoints(DnModule module) {
			foreach (var bp in ilCodeBreakpointList.GetBreakpoints(module.DnModuleId))
				bp.AddBreakpoint(module);
			foreach (var bp in nativeCodeBreakpointList.GetBreakpoints(module.DnModuleId))
				bp.AddBreakpoint(module);
		}

		public DnDebugEventBreakpoint CreateBreakpoint(DebugEventBreakpointKind eventKind, Func<DebugEventBreakpointConditionContext, bool>? cond) {
			DebugVerifyThread();
			var bp = new DnDebugEventBreakpoint(eventKind, cond);
			debugEventBreakpointList.Add(bp);
			return bp;
		}

		public DnAnyDebugEventBreakpoint CreateAnyDebugEventBreakpoint(Func<AnyDebugEventBreakpointConditionContext, bool>? cond) {
			DebugVerifyThread();
			var bp = new DnAnyDebugEventBreakpoint(cond);
			anyDebugEventBreakpointList.Add(bp);
			return bp;
		}

		public DnILCodeBreakpoint CreateBreakpoint(DnModuleId module, uint token, uint offset, Func<ILCodeBreakpointConditionContext, bool>? cond) {
			DebugVerifyThread();
			var bp = new DnILCodeBreakpoint(module, token, offset, cond);
			ilCodeBreakpointList.Add(module, bp);
			foreach (var dnMod in GetLoadedDnModules(module))
				bp.AddBreakpoint(dnMod);
			return bp;
		}

		public DnNativeCodeBreakpoint CreateNativeBreakpoint(DnModuleId module, uint token, uint offset, Func<NativeCodeBreakpointConditionContext, bool>? cond) {
			DebugVerifyThread();
			var bp = new DnNativeCodeBreakpoint(module, token, offset, cond);
			nativeCodeBreakpointList.Add(module, bp);
			foreach (var dnMod in GetLoadedDnModules(module))
				bp.AddBreakpoint(dnMod);
			return bp;
		}

		public DnNativeCodeBreakpoint CreateNativeBreakpoint(CorCode code, uint offset, Func<NativeCodeBreakpointConditionContext, bool>? cond) {
			DebugVerifyThread();
			var module = TryGetModuleId(code.Function?.Module) ?? new DnModuleId();
			var bp = new DnNativeCodeBreakpoint(module, code, offset, cond);
			nativeCodeBreakpointList.Add(module, bp);
			foreach (var dnMod in GetLoadedDnModules(module))
				bp.AddBreakpoint(dnMod);
			return bp;
		}

		public DnModuleId? TryGetModuleId(CorModule? module) {
			if (module is not null && toDnModule.TryGetValue(module, out var dnModule))
				return dnModule.DnModuleId;
			Debug.Fail("Couldn't get module id");
			return null;
		}

		IEnumerable<DnModule> GetLoadedDnModules(DnModuleId module) {
			foreach (var process in processes.GetAll()) {
				foreach (var appDomain in process.AppDomains) {
					foreach (var assembly in appDomain.Assemblies) {
						foreach (var dnMod in assembly.Modules) {
							if (dnMod.DnModuleId.Equals(module))
								yield return dnMod;
						}
					}
				}
			}
		}

		public void RemoveBreakpoint(DnBreakpoint bp) {
			DebugVerifyThread();
			if (bp is DnILCodeBreakpoint ilbp) {
				ilCodeBreakpointList.Remove(ilbp.Module, ilbp);
				ilbp.OnRemoved();
				return;
			}

			if (bp is DnDebugEventBreakpoint debp) {
				debugEventBreakpointList.Remove(debp);
				debp.OnRemoved();
				return;
			}

			if (bp is DnAnyDebugEventBreakpoint adebp) {
				anyDebugEventBreakpointList.Remove(adebp);
				adebp.OnRemoved();
				return;
			}

			if (bp is DnNativeCodeBreakpoint nbp) {
				nativeCodeBreakpointList.Remove(nbp.Module, nbp);
				nbp.OnRemoved();
				return;
			}
		}

		public int TryBreakProcesses() => TryBreakProcesses(true);

		int TryBreakProcesses(bool callProcessStopped) {
			// At least with .NET, we'll get a DebuggerError (hr=0x80004005 (unspecified error))
			// if we try to break the process before the CreateProcess event.
			if (ProcessStateInternal == DebuggerProcessState.Starting)
				return -1;
			if (ProcessStateInternal != DebuggerProcessState.Running)
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
			managedCallbackCounter++;
			if (!addStopState)
				return;

			var thread = process.GetMainThread();
			var appDomain = thread is null ? process.GetMainAppDomain() : thread.AppDomain;
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
			if (ProcessStateInternal == DebuggerProcessState.Starting || ProcessStateInternal == DebuggerProcessState.Running) {
				int hr = TryBreakProcesses(false);
				if (hr < 0)
					return hr;
			}

			DisposeOfHandles();

			foreach (var bp in ilCodeBreakpointList.GetBreakpoints())
				bp.OnRemoved();
			foreach (var bp in nativeCodeBreakpointList.GetBreakpoints())
				bp.OnRemoved();

			foreach (var kv in stepInfos) {
				if (kv.Key.IsActive)
					kv.Key.Deactivate();
				kv.Value.OnCompleted?.Invoke(this, null, true);
			}
			stepInfos.Clear();

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

		void Dispose(bool disposing) => TerminateAllProcessesInternal();

		void TerminateAllProcessesInternal() {
			bool forceNotify = false;
			foreach (var process in processes.GetAll()) {
				try {
					int hr = process.CorProcess.RawObject.Stop(uint.MaxValue);
					hr = process.CorProcess.RawObject.Terminate(uint.MaxValue);
					if (hr != 0) {
						Debug.Assert(hr == CordbgErrors.CORDBG_E_UNRECOVERABLE_ERROR || hr == CordbgErrors.CORDBG_E_PROCESS_NOT_SYNCHRONIZED);
						bool b = NativeMethods.TerminateProcess(process.CorProcess.Handle, uint.MaxValue);
						Debug.Assert(b);
						forceNotify = true;
					}
				}
				catch {
				}
			}
			if (forceNotify) {
				// Make sure listeners get notified of the termination
				ProcessesTerminated();
			}
		}

		int nextThreadId = -1, nextProcessId = -1, nextModuleId = -1, nextAssemblyId = -1, nextAppDomainId = -1;
		internal int GetNextThreadId() => Interlocked.Increment(ref nextThreadId);
		internal int GetNextProcessId() => Interlocked.Increment(ref nextProcessId);
		internal int GetNextModuleId() => Interlocked.Increment(ref nextModuleId);
		internal int GetNextAssemblyId() => Interlocked.Increment(ref nextAssemblyId);
		internal int GetNextAppDomainId() => Interlocked.Increment(ref nextAppDomainId);

		public void AddCustomNotificationClassToken(DnModule module, uint token) {
			var cls = module.CorModule.GetClassFromToken(token);
			Debug2.Assert(cls is not null);
			if (cls is not null)
				customNotificationList.Add((module, cls));
		}

		void UpdateCustomNotificationList(CorAppDomain? removedAppDomain) {
			for (int i = customNotificationList.Count - 1; i >= 0; i--) {
				var info = customNotificationList[i];
				if (info.module.AppDomain.CorAppDomain.Equals(removedAppDomain))
					customNotificationList.RemoveAt(i);
			}
		}

		public DnEval CreateEval(CancellationToken cancellationToken, bool suspendOtherThreads) {
			DebugVerifyThread();
			Debug.Assert(ProcessStateInternal == DebuggerProcessState.Paused);

			return new DnEval(this, debugMessageDispatcher, suspendOtherThreads, customNotificationList, cancellationToken);
		}

		public bool IsEvaluating => evalCounter != 0 && ProcessStateInternal != DebuggerProcessState.Terminated;
		public bool EvalCompleted => evalCompletedCounter != 0;

		internal void EvalStarted() {
			DebugVerifyThread();
			Debug.Assert(ProcessStateInternal == DebuggerProcessState.Paused);

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
			Debug.Assert(!IsEvaluating && ProcessStateInternal == DebuggerProcessState.Paused);
			evalCompletedCounter++;
			try {
				CallOnProcessStateChanged();
			}
			finally {
				evalCompletedCounter--;
			}
		}
		int evalCompletedCounter;

		public void DisposeHandle(CorValue? value) {
			if (value is null || !value.IsHandle)
				return;
			if (ProcessStateInternal != DebuggerProcessState.Running)
				value.DisposeHandle();
			else
				disposeValues.Add(value);
		}
		readonly List<CorValue> disposeValues = new List<CorValue>();

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
