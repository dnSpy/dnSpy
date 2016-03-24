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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Debugger.CallStack;
using dnSpy.Shared.Scripting;
using DBG = dndbg.Engine;

namespace dnSpy.Debugger.Scripting {
	[Export, Export(typeof(IDebugger)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class Debugger : IDebugger {
		public Dispatcher Dispatcher {
			get { return dispatcher; }
		}
		readonly Dispatcher dispatcher;

		readonly ITheDebugger theDebugger;
		readonly Lazy<IStackFrameManager> stackFrameManager;
		readonly Lazy<IDebugManager> debugManager;
		readonly ManualResetEvent pausedOrTerminatedEvent;
		readonly ManualResetEvent runningEvent;
		// Cache this so it's possible for scripts to read process memory from any thread
		IntPtr hProcess_debuggee;

		public DebuggerProcessState State {
			get { return dispatcher.UI(() => Utils.Convert(theDebugger.ProcessState)); }
		}

		public DebuggerPauseState[] PauseStates {
			get {
				return dispatcher.UI(() => {
					var list = new List<DebuggerPauseState>();

					var dbg = theDebugger.Debugger;
					if (dbg != null && dbg.ProcessState == DBG.DebuggerProcessState.Paused) {
						foreach (var ds in dbg.DebuggerStates) {
							foreach (var ps in ds.PauseStates)
								list.Add(Utils.Convert(ps));
						}
					}

					return list.ToArray();
				});
			}
		}

		public PauseReason PauseReason {
			get {
				return dispatcher.UI(() => {
					var dbg = theDebugger.Debugger;
					if (dbg == null || dbg.ProcessState == DBG.DebuggerProcessState.Terminated)
						return PauseReason.Terminated;
					var ps = PauseStates;
					return ps.Length == 0 ? PauseReason.Other : ps[0].Reason;
				});
			}
		}

		public bool IsStarting {
			get { return State == DebuggerProcessState.Starting; }
		}

		public bool IsContinuing {
			get { return State == DebuggerProcessState.Continuing; }
		}

		public bool IsRunning {
			get { return State == DebuggerProcessState.Running; }
		}

		public bool IsPaused {
			get { return State == DebuggerProcessState.Paused; }
		}

		public bool IsTerminated {
			get { return State == DebuggerProcessState.Terminated; }
		}

		public bool IsDebugging {
			get { return dispatcher.UI(() => theDebugger.IsDebugging); }
		}

		public bool HasAttached {
			get { return dispatcher.UI(() => debugManager.Value.HasAttached); }
		}

		public bool IsEvaluating {
			get { return dispatcher.UI(() => debugManager.Value.IsEvaluating); }
		}

		public bool EvalCompleted {
			get { return dispatcher.UI(() => debugManager.Value.EvalCompleted); }
		}

		[ImportingConstructor]
		Debugger(ITheDebugger theDebugger, Lazy<IStackFrameManager> stackFrameManager, Lazy<IDebugManager> debugManager) {
			this.dispatcher = Dispatcher.CurrentDispatcher;
			this.theDebugger = theDebugger;
			this.theDebugger.OnProcessStateChanged_Last += TheDebugger_OnProcessStateChanged_Last;
			this.stackFrameManager = stackFrameManager;
			this.debugManager = debugManager;
			this.pausedOrTerminatedEvent = new ManualResetEvent(false);
			this.runningEvent = new ManualResetEvent(false);
			InitializeProcessHandle();
			InitializePausedOrTerminatedEvent();
		}

		public event EventHandler<DebuggerEventArgs> OnProcessStateChanged;

		void InitializeProcessHandle() {
			this.hProcess_debuggee = GetProcessHandle();
		}

		IntPtr GetProcessHandle() {
			var dbg = theDebugger.Debugger;
			if (dbg == null || dbg.ProcessState == DBG.DebuggerProcessState.Terminated)
				return IntPtr.Zero;
			else {
				var ps = dbg.Processes;
				Debug.Assert(ps.Length > 0);
				if (ps.Length > 0) {
					Debug.Assert(ps.Length == 1);
					return ps[0].CorProcess.Handle;
				}
				return IntPtr.Zero;
			}
		}

		void TheDebugger_OnProcessStateChanged_Last(object sender, DBG.DebuggerEventArgs e) {
			switch (theDebugger.ProcessState) {
			case DBG.DebuggerProcessState.Starting:
				foreach (var bp in breakpointsToInitialize)
					Initialize(bp);
				breakpointsToInitialize.Clear();
				InitializeProcessHandle();
				Debug.Assert(hProcess_debuggee != IntPtr.Zero);
				break;

			case DBG.DebuggerProcessState.Terminated:
				Debug.Assert(breakpointsToInitialize.Count == 0);
				breakpointsToInitialize.Clear();
				hProcess_debuggee = IntPtr.Zero;
				break;
			}

			Debug.Assert((theDebugger.ProcessState == DBG.DebuggerProcessState.Terminated && hProcess_debuggee == IntPtr.Zero) ||
						 (theDebugger.ProcessState != DBG.DebuggerProcessState.Terminated && hProcess_debuggee != IntPtr.Zero && hProcess_debuggee == GetProcessHandle()));

			var c = OnProcessStateChanged;
			if (c != null) {
				try {
					c(this, DebuggerEventArgs.Empty);
				}
				catch {
					// Ignore buggy script exceptions
				}
			}
			InitializePausedOrTerminatedEvent();
		}

		void InitializePausedOrTerminatedEvent() {
			var dbg = theDebugger.Debugger;
			if (dbg != null && dbg.ProcessState == DBG.DebuggerProcessState.Terminated)
				dbg = null;
			bool eval = dbg != null && (dbg.IsEvaluating || dbg.EvalCompleted);
			if (eval)
				return;

			switch (theDebugger.ProcessState) {
			case DBG.DebuggerProcessState.Starting:
			case DBG.DebuggerProcessState.Continuing:
			case DBG.DebuggerProcessState.Running:
				pausedOrTerminatedEvent.Reset();
				runningEvent.Set();
				break;

			case DBG.DebuggerProcessState.Paused:
			case DBG.DebuggerProcessState.Terminated:
				pausedOrTerminatedEvent.Set();
				runningEvent.Reset();
				break;

			default:
				Debug.Fail("Unknown process state");
				goto case DBG.DebuggerProcessState.Terminated;
			}
		}

		static Task<bool> GetWaitTask(WaitHandle handle, int millisecondsTimeout) {
			var tcs = new TaskCompletionSource<bool>();
			var rwh = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) => tcs.TrySetResult(!timedOut), null, millisecondsTimeout, true);
			tcs.Task.ContinueWith(a => rwh.Unregister(null));
			return tcs.Task;
		}

		public Task<bool> WaitAsync(int millisecondsTimeout) {
			return GetWaitTask(pausedOrTerminatedEvent, millisecondsTimeout);
		}

		public Task<bool> WaitRunAsync(int millisecondsTimeout) {
			return GetWaitTask(runningEvent, millisecondsTimeout);
		}

		public bool Wait(int millisecondsTimeout) {
			Debug.Assert(!dispatcher.CheckAccess());
			if (dispatcher.CheckAccess())
				throw new InvalidOperationException("Wait() can't be called on the UI / debugger thread");
			return pausedOrTerminatedEvent.WaitOne(millisecondsTimeout);
		}

		public bool Wait(CancellationToken token, int millisecondsTimeout) {
			Debug.Assert(!dispatcher.CheckAccess());
			if (dispatcher.CheckAccess())
				throw new InvalidOperationException("Wait() can't be called on the UI / debugger thread");

			if (!token.CanBeCanceled)
				return Wait(millisecondsTimeout);

			var waitHandles = new[] { token.WaitHandle, pausedOrTerminatedEvent };
			int index = WaitHandle.WaitAny(waitHandles, millisecondsTimeout);
			token.ThrowIfCancellationRequested();
			return index != WaitHandle.WaitTimeout;
		}

		public bool WaitRun(int millisecondsTimeout) {
			Debug.Assert(!dispatcher.CheckAccess());
			if (dispatcher.CheckAccess())
				throw new InvalidOperationException("WaitRun() can't be called on the UI / debugger thread");
			return runningEvent.WaitOne(millisecondsTimeout);
		}

		public bool WaitRun(CancellationToken token, int millisecondsTimeout) {
			Debug.Assert(!dispatcher.CheckAccess());
			if (dispatcher.CheckAccess())
				throw new InvalidOperationException("WaitRun() can't be called on the UI / debugger thread");

			if (!token.CanBeCanceled)
				return WaitRun(millisecondsTimeout);

			var waitHandles = new[] { token.WaitHandle, runningEvent };
			int index = WaitHandle.WaitAny(waitHandles, millisecondsTimeout);
			token.ThrowIfCancellationRequested();
			return index != WaitHandle.WaitTimeout;
		}

		public bool Start() {
			return dispatcher.UI(() => debugManager.Value.DebugAssembly());
		}

		public bool Start(DebugOptions options) {
			return dispatcher.UI(() => debugManager.Value.DebugAssembly(Utils.Convert(options, debugManager.Value.DebuggerSettings, new DBG.DesktopCLRTypeDebugInfo())));
		}

		public bool Start(string filename, string cmdLine, string cwd, BreakProcessKind breakKind) {
			var options = new DebugOptions {
				Filename = filename,
				CommandLine = cmdLine,
				CurrentDirectory = cwd,
				BreakProcessKind = breakKind,
			};
			return Start(options);
		}

		public bool StartCoreCLR() {
			return dispatcher.UI(() => debugManager.Value.DebugCoreCLRAssembly());
		}

		public bool StartCoreCLR(CoreCLRDebugOptions options) {
			return dispatcher.UI(() => debugManager.Value.DebugAssembly(Utils.Convert(options.Options, debugManager.Value.DebuggerSettings, new DBG.CoreCLRTypeDebugInfo(options.DbgShimFilename ?? debugManager.Value.DebuggerSettings.CoreCLRDbgShimFilename, options.HostFilename, options.HostCommandLine))));
		}

		public bool StartCoreCLR(string filename, string cmdLine, string cwd, BreakProcessKind breakKind, string hostFilename, string hostCommandLine) {
			var options = new CoreCLRDebugOptions();
			options.Options.Filename = filename;
			options.Options.CommandLine = cmdLine;
			options.Options.CurrentDirectory = cwd;
			options.Options.BreakProcessKind = breakKind;
			options.HostFilename = hostFilename;
			options.HostCommandLine = hostCommandLine;
			return StartCoreCLR(options);
		}

		public bool Attach() {
			return dispatcher.UI(() => debugManager.Value.Attach());
		}

		public bool Attach(AttachOptions options) {
			return dispatcher.UI(() => debugManager.Value.Attach(Utils.Convert(options, debugManager.Value.DebuggerSettings)));
		}

		public bool Attach(int pid) {
			var options = new AttachOptions {
				ProcessId = pid,
			};
			return Attach(options);
		}

		public void Restart() {
			dispatcher.UI(() => debugManager.Value.Restart());
		}

		public void Break() {
			dispatcher.UI(() => debugManager.Value.Break());
		}

		public void Stop() {
			dispatcher.UI(() => debugManager.Value.Stop());
		}

		public void Detach() {
			dispatcher.UI(() => debugManager.Value.Detach());
		}

		public void Continue() {
			dispatcher.UI(() => debugManager.Value.Continue());
		}

		public Task<bool> ContinueAsync(int millisecondsTimeout) {
			Continue();
			return WaitAsync(millisecondsTimeout);
		}

		public bool ContinueWait(int millisecondsTimeout) {
			Continue();
			return Wait(millisecondsTimeout);
		}

		public bool ContinueWait(CancellationToken token, int millisecondsTimeout) {
			Continue();
			return Wait(token, millisecondsTimeout);
		}

		public void StepInto() {
			dispatcher.UI(() => debugManager.Value.StepInto());
		}

		public void StepInto(IStackFrame frame) {
			dispatcher.UI(() => debugManager.Value.StepInto(((StackFrame)frame).CorFrame));
		}

		public Task<bool> StepIntoAsync(int millisecondsTimeout) {
			StepInto();
			return WaitAsync(millisecondsTimeout);
		}

		public bool StepIntoWait(int millisecondsTimeout) {
			StepInto();
			return Wait(millisecondsTimeout);
		}

		public bool StepIntoWait(CancellationToken token, int millisecondsTimeout) {
			StepInto();
			return Wait(token, millisecondsTimeout);
		}

		public Task<bool> StepIntoAsync(IStackFrame frame, int millisecondsTimeout) {
			StepInto(frame);
			return WaitAsync(millisecondsTimeout);
		}

		public bool StepIntoWait(IStackFrame frame, int millisecondsTimeout) {
			StepInto(frame);
			return Wait(millisecondsTimeout);
		}

		public bool StepIntoWait(IStackFrame frame, CancellationToken token, int millisecondsTimeout) {
			StepInto(frame);
			return Wait(token, millisecondsTimeout);
		}

		public void StepOver() {
			dispatcher.UI(() => debugManager.Value.StepOver());
		}

		public void StepOver(IStackFrame frame) {
			dispatcher.UI(() => debugManager.Value.StepOver(((StackFrame)frame).CorFrame));
		}

		public Task<bool> StepOverAsync(int millisecondsTimeout) {
			StepOver();
			return WaitAsync(millisecondsTimeout);
		}

		public bool StepOverWait(int millisecondsTimeout) {
			StepOver();
			return Wait(millisecondsTimeout);
		}

		public bool StepOverWait(CancellationToken token, int millisecondsTimeout) {
			StepOver();
			return Wait(token, millisecondsTimeout);
		}

		public Task<bool> StepOverAsync(IStackFrame frame, int millisecondsTimeout) {
			StepOver(frame);
			return WaitAsync(millisecondsTimeout);
		}

		public bool StepOverWait(IStackFrame frame, int millisecondsTimeout) {
			StepOver(frame);
			return Wait(millisecondsTimeout);
		}

		public bool StepOverWait(IStackFrame frame, CancellationToken token, int millisecondsTimeout) {
			StepOver(frame);
			return Wait(token, millisecondsTimeout);
		}

		public void StepOut() {
			dispatcher.UI(() => debugManager.Value.StepOut());
		}

		public void StepOut(IStackFrame frame) {
			dispatcher.UI(() => debugManager.Value.StepOut(((StackFrame)frame).CorFrame));
		}

		public Task<bool> StepOutAsync(int millisecondsTimeout) {
			StepOut();
			return WaitAsync(millisecondsTimeout);
		}

		public bool StepOutWait(int millisecondsTimeout) {
			StepOut();
			return Wait(millisecondsTimeout);
		}

		public bool StepOutWait(CancellationToken token, int millisecondsTimeout) {
			StepOut();
			return Wait(token, millisecondsTimeout);
		}

		public Task<bool> StepOutAsync(IStackFrame frame, int millisecondsTimeout) {
			StepOut(frame);
			return WaitAsync(millisecondsTimeout);
		}

		public bool StepOutWait(IStackFrame frame, int millisecondsTimeout) {
			StepOut(frame);
			return Wait(millisecondsTimeout);
		}

		public bool StepOutWait(IStackFrame frame, CancellationToken token, int millisecondsTimeout) {
			StepOut(frame);
			return Wait(token, millisecondsTimeout);
		}

		public bool RunTo(IStackFrame frame) {
			return dispatcher.UI(() => debugManager.Value.RunTo(((StackFrame)frame).CorFrame));
		}

		public Task<bool> RunToAsync(IStackFrame frame, int millisecondsTimeout) {
			RunTo(frame);
			return WaitAsync(millisecondsTimeout);
		}

		public bool RunToWait(IStackFrame frame, int millisecondsTimeout) {
			RunTo(frame);
			return Wait(millisecondsTimeout);
		}

		public bool RunToWait(IStackFrame frame, CancellationToken token, int millisecondsTimeout) {
			RunTo(frame);
			return Wait(token, millisecondsTimeout);
		}

		public bool SetOffset(int offset) {
			return dispatcher.UI(() => {
				string errMsg;
				return debugManager.Value.SetOffset((uint)offset, out errMsg);
			});
		}

		public bool SetOffset(uint offset) {
			return dispatcher.UI(() => {
				string errMsg;
				return debugManager.Value.SetOffset(offset, out errMsg);
			});
		}

		public bool SetNativeOffset(int offset) {
			return dispatcher.UI(() => {
				string errMsg;
				return debugManager.Value.SetNativeOffset((uint)offset, out errMsg);
			});
		}

		public bool SetNativeOffset(uint offset) {
			return dispatcher.UI(() => {
				string errMsg;
				return debugManager.Value.SetNativeOffset(offset, out errMsg);
			});
		}

		public bool SetOffset(IStackFrame frame, int offset) {
			return dispatcher.UI(() => {
				string errMsg;
				return debugManager.Value.SetOffset(((StackFrame)frame).CorFrame, (uint)offset, out errMsg);
			});
		}

		public bool SetOffset(IStackFrame frame, uint offset) {
			return dispatcher.UI(() => {
				string errMsg;
				return debugManager.Value.SetOffset(((StackFrame)frame).CorFrame, offset, out errMsg);
			});
		}

		public bool SetNativeOffset(IStackFrame frame, int offset) {
			return dispatcher.UI(() => {
				string errMsg;
				return debugManager.Value.SetNativeOffset(((StackFrame)frame).CorFrame, (uint)offset, out errMsg);
			});
		}

		public bool SetNativeOffset(IStackFrame frame, uint offset) {
			return dispatcher.UI(() => {
				string errMsg;
				return debugManager.Value.SetNativeOffset(((StackFrame)frame).CorFrame, offset, out errMsg);
			});
		}

		public IEnumerable<IDebuggerThread> Threads {
			get { return dispatcher.UIIter(GetThreadsUI); }
		}

		IEnumerable<IDebuggerThread> GetThreadsUI() {
			var dbg = theDebugger.Debugger;
			if (dbg == null)
				yield break;
			foreach (var p in dbg.Processes) {
				foreach (var t in p.Threads) {
					yield return new DebuggerThread(this, t);
				}
			}
		}

		public IDebuggerThread ActiveThread {
			get {
				return dispatcher.UI(() => {
					var thread = debugManager.Value.StackFrameManager.SelectedThread;
					return thread == null ? null : new DebuggerThread(this, thread);
				});
			}
			set { dispatcher.UI(() => debugManager.Value.StackFrameManager.SelectedThread = value == null ? null : ((DebuggerThread)value).DnThread); }
		}

		public IStackFrame ActiveFrame {
			get {
				return dispatcher.UI(() => {
					var frame = debugManager.Value.StackFrameManager.SelectedFrame;
					return frame == null ? null : new StackFrame(this, frame, debugManager.Value.StackFrameManager.SelectedFrameNumber);
				});
			}
			set { dispatcher.UI(() => debugManager.Value.StackFrameManager.SelectedFrameNumber = value == null ? 0 : ((StackFrame)value).Index); }
		}

		public IStackFrame ActiveILFrame {
			get {
				return dispatcher.UI(() => {
					var frame = debugManager.Value.StackFrameManager.FirstILFrame;
					return frame == null ? null : new StackFrame(this, frame, debugManager.Value.StackFrameManager.SelectedFrameNumber);
				});
			}
		}

		public int ActiveFrameIndex {
			get { return dispatcher.UI(() => debugManager.Value.StackFrameManager.SelectedFrameNumber); }
			set { dispatcher.UI(() => debugManager.Value.StackFrameManager.SelectedFrameNumber = value); }
		}

		public IEnumerable<IAppDomain> AppDomains {
			get { return dispatcher.UIIter(GetAppDomainsUI); }
		}

		IEnumerable<IAppDomain> GetAppDomainsUI() {
			var dbg = theDebugger.Debugger;
			if (dbg == null)
				yield break;
			foreach (var p in dbg.Processes) {
				foreach (var ad in p.AppDomains) {
					yield return new DebuggerAppDomain(this, ad);
				}
			}
		}

		public IAppDomain FirstAppDomain {
			get { return dispatcher.UI(() => AppDomains.FirstOrDefault()); }
		}

		public IEnumerable<IDebuggerAssembly> Assemblies {
			get { return dispatcher.UIIter(GetAssembliesUI); }
		}

		IEnumerable<IDebuggerAssembly> GetAssembliesUI() {
			var ad = FirstAppDomain;
			if (ad == null)
				yield break;
			foreach (var a in ad.Assemblies)
				yield return a;
		}

		public IEnumerable<IDebuggerModule> Modules {
			get { return dispatcher.UIIter(GetModulesUI); }
		}

		IEnumerable<IDebuggerModule> GetModulesUI() {
			var ad = FirstAppDomain;
			if (ad == null)
				yield break;
			foreach (var a in ad.Assemblies) {
				foreach (var m in a.Modules)
					yield return m;
			}
		}

		internal IDebuggerAssembly FindAssemblyUI(DBG.CorAssembly corAssembly) {
			dispatcher.VerifyAccess();
			var dbg = theDebugger.Debugger;
			if (dbg == null)
				return null;
			foreach (var a in dbg.Assemblies) {
				if (a.CorAssembly == corAssembly)
					return new DebuggerAssembly(this, a);
			}
			return null;
		}

		internal IDebuggerModule FindModuleUI(DBG.CorModule corModule) {
			dispatcher.VerifyAccess();
			var dbg = theDebugger.Debugger;
			if (dbg == null)
				return null;
			foreach (var m in dbg.Modules) {
				if (m.CorModule == corModule)
					return new DebuggerModule(this, m);
			}
			return null;
		}

		internal IDebuggerThread FindThreadUI(DBG.CorThread thread) {
			dispatcher.VerifyAccess();
			var dbg = theDebugger.Debugger;
			if (dbg == null)
				return null;
			foreach (var p in dbg.Processes) {
				foreach (var t in p.Threads) {
					if (t.CorThread == thread)
						return new DebuggerThread(this, t);
				}
			}
			return null;
		}

		internal IAppDomain FindAppDomainUI(DBG.CorAppDomain appDomain) {
			dispatcher.VerifyAccess();
			var dbg = theDebugger.Debugger;
			if (dbg == null)
				return null;
			foreach (var p in dbg.Processes) {
				foreach (var ad in p.AppDomains) {
					if (ad.CorAppDomain == appDomain)
						return new DebuggerAppDomain(this, ad);
				}
			}
			return null;
		}

		internal IStackFrame TryGetNewFrameUI(uint token, ulong stackStart, ulong stackEnd) {
			dispatcher.VerifyAccess();
			var dbg = theDebugger.Debugger;
			if (dbg == null || dbg.ProcessState != DBG.DebuggerProcessState.Paused)
				return null;
			foreach (var p in dbg.Processes) {
				foreach (var t in p.Threads) {
					int frameNo = 0;
					foreach (var f in t.AllFrames) {
						if (f.StackStart == stackStart && f.StackEnd == stackEnd && f.Token == token)
							return new StackFrame(this, f, frameNo);
						frameNo++;
					}
				}
			}
			return null;
		}

		public IILBreakpoint CreateBreakpoint(ModuleName module, uint token, uint offset, Func<IILBreakpoint, bool> cond) {
			return dispatcher.UI(() => {
				var bp = new ILBreakpoint(this, module, token, offset, cond);
				if (theDebugger.IsDebugging) {
					Debug.Assert(breakpointsToInitialize.Count == 0);
					Initialize(bp);
				}
				else
					breakpointsToInitialize.Add(bp);
				return bp;
			});
		}
		readonly List<IBreakpoint> breakpointsToInitialize = new List<IBreakpoint>();

		public IILBreakpoint CreateBreakpoint(ModuleName module, uint token, int offset, Func<IILBreakpoint, bool> cond) {
			return CreateBreakpoint(module, token, (uint)offset, cond);
		}

		public INativeBreakpoint CreateNativeBreakpoint(ModuleName module, uint token, uint offset, Func<INativeBreakpoint, bool> cond) {
			return dispatcher.UI(() => {
				if (!theDebugger.IsDebugging)
					throw new InvalidOperationException("Native breakpoints can only be set on jitted methods");
				var bp = new NativeBreakpoint(this, module, token, offset, cond);
				Initialize(bp);
				return bp;
			});
		}

		public INativeBreakpoint CreateNativeBreakpoint(ModuleName module, uint token, int offset, Func<INativeBreakpoint, bool> cond) {
			return CreateNativeBreakpoint(module, token, (uint)offset, cond);
		}

		public INativeBreakpoint CreateNativeBreakpoint(IDebuggerCode code, uint offset, Func<INativeBreakpoint, bool> cond) {
			return dispatcher.UI(() => {
				if (code == null)
					throw new ArgumentNullException();
				if (code.IsIL)
					throw new ArgumentException("code is IL code, not native code");
				Debug.Assert(theDebugger.IsDebugging);
				if (!theDebugger.IsDebugging)
					throw new InvalidOperationException();
				var bp = new NativeBreakpoint(this, (DebuggerCode)code, offset, cond);
				Initialize(bp);
				return bp;
			});
		}

		public INativeBreakpoint CreateNativeBreakpoint(IDebuggerCode code, int offset, Func<INativeBreakpoint, bool> cond) {
			return CreateNativeBreakpoint(code, (uint)offset, cond);
		}

		public IEventBreakpoint CreateBreakpoint(DebugEventKind eventKind, Func<IEventBreakpoint, IDebugEventContext, bool> cond) {
			return dispatcher.UI(() => {
				var bp = new EventBreakpoint(this, eventKind, cond);
				if (theDebugger.IsDebugging) {
					Debug.Assert(breakpointsToInitialize.Count == 0);
					Initialize(bp);
				}
				else
					breakpointsToInitialize.Add(bp);
				return bp;
			});
		}

		public IEventBreakpoint CreateLoadModuleBreakpoint(Func<IEventBreakpoint, IModuleEventContext, bool> cond) {
			return CreateBreakpoint(DebugEventKind.LoadModule, (a, b) => cond == null || cond(a, (IModuleEventContext)b));
		}

		public IAnyEventBreakpoint CreateAnyEventBreakpoint(Func<IAnyEventBreakpoint, IDebugEventContext, bool> cond) {
			return dispatcher.UI(() => {
				var bp = new AnyEventBreakpoint(this, cond);
				if (theDebugger.IsDebugging) {
					Debug.Assert(breakpointsToInitialize.Count == 0);
					Initialize(bp);
				}
				else
					breakpointsToInitialize.Add(bp);
				return bp;
			});
		}

		public void BreakOnLoad(string name, Action<IDebuggerModule> action) {
			if (name == null)
				throw new ArgumentNullException();
			CreateBreakpoint(DebugEventKind.LoadModule, (bp, ctx) => {
				var c = (ModuleEventContext)ctx;
				if (!Utils.IsSameFile(c.Module.ModuleName.Name, name))
					return false;
				bp.Remove();
				if (action != null)
					action(c.Module);
				return true;
			});
		}

		public void BreakOnLoadAssembly(string assemblyName, Action<IDebuggerAssembly> action, AssemblyNameComparerFlags flags) {
			BreakOnLoadAssembly(new AssemblyNameInfo(assemblyName), action, flags);
		}

		public void BreakOnLoadAssembly(IAssembly assembly, Action<IDebuggerAssembly> action, AssemblyNameComparerFlags flags) {
			if (assembly == null)
				throw new ArgumentNullException();
			assembly = assembly.ToAssemblyRef();// Prevent storing AssemblyDef refs
			// Use the LoadModule event since without a module, we won't know the full assembly name
			CreateBreakpoint(DebugEventKind.LoadModule, (bp, ctx) => {
				var c = (ModuleEventContext)ctx;
				var comparer = new AssemblyNameComparer(flags);
				var asm = c.Module.Assembly;
				if (!comparer.Equals(assembly, new AssemblyNameInfo(asm.FullName)))
					return false;
				bp.Remove();
				if (action != null)
					action(asm);
				return true;
			});
		}

		void Initialize(IBreakpoint bp) {
			Debug.Assert(theDebugger.IsDebugging);

			var bph = bp as IDnBreakpointHolder;
			if (bph != null) {
				bph.Initialize(theDebugger.Debugger);
				return;
			}

			Debug.Fail("Unknown breakpoint: " + bp);
		}

		public void Remove(IBreakpoint bp) {
			dispatcher.UI(() => {
				if (theDebugger.IsDebugging) {
					var bph = bp as IDnBreakpointHolder;
					if (bph != null) {
						var dnbp = bph.DnBreakpoint;
						if (dnbp != null)
							theDebugger.Debugger.RemoveBreakpoint(bph.DnBreakpoint);
					}
					else
						Debug.Fail("Unknown breakpoint: " + bp);
				}
				else {
					bool b = breakpointsToInitialize.Remove(bp);
					Debug.Assert(b);
				}
			});
		}

		public unsafe void ReadMemory(ulong address, byte[] array, long index, uint count) {
			if (hProcess_debuggee == IntPtr.Zero || (IntPtr.Size == 4 && address > uint.MaxValue)) {
				Clear(array, index, count);
				return;
			}
			ulong endAddr = IntPtr.Size == 4 ? (ulong)uint.MaxValue + 1 : 0;
			while (count != 0) {
				int len = (int)Math.Min((uint)Environment.SystemPageSize, count);

				ulong nextPage = (address + (ulong)Environment.SystemPageSize) & ~((ulong)Environment.SystemPageSize - 1);
				ulong pageSizeLeft = nextPage - address;
				if ((ulong)len > pageSizeLeft)
					len = (int)pageSizeLeft;

				IntPtr sizeRead;
				bool b;
				fixed (void* p = &array[index])
					b = NativeMethods.ReadProcessMemory(hProcess_debuggee, new IntPtr((void*)address), new IntPtr(p), len, out sizeRead);

				int read = !b ? 0 : (int)sizeRead.ToInt64();
				Debug.Assert(read <= len);
				Debug.Assert(read == 0 || read == len);
				if (read != len)
					Clear(array, index + read, len - read);

				address += (ulong)len;
				count -= (uint)len;
				index += len;

				if (address == endAddr) {
					Clear(array, index, count);
					break;
				}
			}
		}

		void Clear(byte[] array, long index, long len) {
			if (index <= int.MaxValue && len <= int.MaxValue)
				Array.Clear(array, (int)index, (int)len);
			else {
				long end = index + len;
				for (long i = index; i < len; i++)
					array[i] = 0;
			}
		}

		public void ReadMemory(ulong address, byte[] array, long index, int count) {
			if (count < 0)
				throw new ArgumentOutOfRangeException();
			ReadMemory(address, array, index, (uint)count);
		}

		public byte[] ReadMemory(ulong address, uint count) {
			var array = new byte[count];
			ReadMemory(address, array, 0, count);
			return array;
		}

		public byte[] ReadMemory(ulong address, int count) {
			if (count < 0)
				throw new ArgumentOutOfRangeException();
			return ReadMemory(address, (uint)count);
		}

		public unsafe uint WriteMemory(ulong address, byte[] array, long index, uint count) {
			if (hProcess_debuggee == IntPtr.Zero || (IntPtr.Size == 4 && address > uint.MaxValue))
				return 0;

			ulong endAddr = IntPtr.Size == 4 ? (ulong)uint.MaxValue + 1 : 0;
			uint totalWritten = 0;
			while (count != 0) {
				int len = (int)Math.Min((uint)Environment.SystemPageSize, count);

				ulong nextPage = (address + (ulong)Environment.SystemPageSize) & ~((ulong)Environment.SystemPageSize - 1);
				ulong pageSizeLeft = nextPage - address;
				if ((ulong)len > pageSizeLeft)
					len = (int)pageSizeLeft;

				uint oldProtect;
				bool restoreOldProtect = NativeMethods.VirtualProtectEx(hProcess_debuggee, new IntPtr((void*)address), len, NativeMethods.PAGE_EXECUTE_READWRITE, out oldProtect);
				IntPtr sizeWritten;
				bool b;
				fixed (void* p = &array[index])
					b = NativeMethods.WriteProcessMemory(hProcess_debuggee, new IntPtr((void*)address), new IntPtr(p), len, out sizeWritten);
				if (restoreOldProtect)
					NativeMethods.VirtualProtectEx(hProcess_debuggee, new IntPtr((void*)address), len, oldProtect, out oldProtect);

				address += (ulong)len;
				count -= (uint)len;
				index += len;
				totalWritten += (uint)len;

				if (address == endAddr)
					break;
			}
			return totalWritten;
		}

		public int WriteMemory(ulong address, byte[] array, long index, int count) {
			if (count < 0)
				throw new ArgumentOutOfRangeException();
			return (int)WriteMemory(address, array, index, (uint)count);
		}

		public void WriteMemory(ulong address, byte[] array) {
			if (array.LongLength > uint.MaxValue)
				throw new ArgumentException();
			uint writtenBytes = WriteMemory(address, array, 0, (uint)array.LongLength);
			if (writtenBytes != array.LongLength)
				throw new IOException(string.Format("Couldn't write all bytes. Wrote {0} bytes, expected {1} bytes", writtenBytes, array.LongLength));
		}

		// Assumes memory is writable
		internal unsafe int WriteMemory(ulong address, IntPtr sourceData, int sourceSize) {
			if (hProcess_debuggee == IntPtr.Zero || address == 0)
				return 0;
			IntPtr sizeWritten;
			bool b = NativeMethods.WriteProcessMemory(hProcess_debuggee, new IntPtr((void*)address), sourceData, sourceSize, out sizeWritten);
			return !b ? 0 : (int)sizeWritten.ToInt64();
		}

		public bool ReadBoolean(ulong address) {
			return BitConverter.ToBoolean(ReadMemory(address, 1), 0);
		}

		public char ReadChar(ulong address) {
			return BitConverter.ToChar(ReadMemory(address, 2), 0);
		}

		public sbyte ReadSByte(ulong address) {
			return (sbyte)ReadMemory(address, 1)[0];
		}

		public byte ReadByte(ulong address) {
			return ReadMemory(address, 1)[0];
		}

		public short ReadInt16(ulong address) {
			return BitConverter.ToInt16(ReadMemory(address, 2), 0);
		}

		public ushort ReadUInt16(ulong address) {
			return BitConverter.ToUInt16(ReadMemory(address, 2), 0);
		}

		public int ReadInt32(ulong address) {
			return BitConverter.ToInt32(ReadMemory(address, 4), 0);
		}

		public uint ReadUInt32(ulong address) {
			return BitConverter.ToUInt32(ReadMemory(address, 4), 0);
		}

		public long ReadInt64(ulong address) {
			return BitConverter.ToInt64(ReadMemory(address, 8), 0);
		}

		public ulong ReadUInt64(ulong address) {
			return BitConverter.ToUInt64(ReadMemory(address, 8), 0);
		}

		public float ReadSingle(ulong address) {
			return BitConverter.ToSingle(ReadMemory(address, 4), 0);
		}

		public double ReadDouble(ulong address) {
			return BitConverter.ToDouble(ReadMemory(address, 8), 0);
		}

		public decimal ReadDecimal(ulong address) {
			return Utils.ToDecimal(ReadMemory(address, 8));
		}

		public void Write(ulong address, bool value) {
			WriteMemory(address, BitConverter.GetBytes(value));
		}

		public void Write(ulong address, char value) {
			WriteMemory(address, BitConverter.GetBytes(value));
		}

		public void Write(ulong address, sbyte value) {
			WriteMemory(address, new byte[1] { (byte)value });
		}

		public void Write(ulong address, byte value) {
			WriteMemory(address, new byte[1] { value });
		}

		public void Write(ulong address, short value) {
			WriteMemory(address, BitConverter.GetBytes(value));
		}

		public void Write(ulong address, ushort value) {
			WriteMemory(address, BitConverter.GetBytes(value));
		}

		public void Write(ulong address, int value) {
			WriteMemory(address, BitConverter.GetBytes(value));
		}

		public void Write(ulong address, uint value) {
			WriteMemory(address, BitConverter.GetBytes(value));
		}

		public void Write(ulong address, long value) {
			WriteMemory(address, BitConverter.GetBytes(value));
		}

		public void Write(ulong address, ulong value) {
			WriteMemory(address, BitConverter.GetBytes(value));
		}

		public void Write(ulong address, float value) {
			WriteMemory(address, BitConverter.GetBytes(value));
		}

		public void Write(ulong address, double value) {
			WriteMemory(address, BitConverter.GetBytes(value));
		}

		public void Write(ulong address, decimal value) {
			WriteMemory(address, Utils.GetBytes(value));
		}

		public IDebuggerModule CorLib {
			get {
				if (corLib != null)
					return corLib;
				return dispatcher.UI(() => {
					if (corLib != null)
						return corLib;
					var ad = FirstAppDomain;
					if (ad == null)
						return null;
					corLib = ad.CorLib;
					return corLib;
				});
			}
		}
		IDebuggerModule corLib;

		public IDebuggerModule GetModule(Module module) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetModule(module);
			});
		}

		public IDebuggerModule GetModule(ModuleName name) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetModule(name);
			});
		}

		public IDebuggerModule GetModuleByName(string name) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetModuleByName(name);
			});
		}

		public IDebuggerAssembly GetAssembly(Assembly asm) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetAssembly(asm);
			});
		}

		public IDebuggerAssembly GetAssembly(string name) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetAssembly(name);
			});
		}

		public IDebuggerClass GetClass(string modName, string className) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetClass(modName, className);
			});
		}

		public IDebuggerMethod GetMethod(string modName, string className, string methodName) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetMethod(modName, className, methodName);
			});
		}

		public IDebuggerField GetField(string modName, string className, string fieldName) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetField(modName, className, fieldName);
			});
		}

		public IDebuggerProperty GetProperty(string modName, string className, string propertyName) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetProperty(modName, className, propertyName);
			});
		}

		public IDebuggerEvent GetEvent(string modName, string className, string eventName) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetEvent(modName, className, eventName);
			});
		}

		public IDebuggerType GetType(string modName, string className) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetType(modName, className);
			});
		}

		public IDebuggerType GetType(string modName, string className, params IDebuggerType[] genericArguments) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetType(modName, className, genericArguments);
			});
		}

		public IDebuggerType GetType(Type type) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetType(type);
			});
		}

		public IDebuggerField GetField(FieldInfo field) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetField(field);
			});
		}

		public IDebuggerMethod GetMethod(MethodBase method) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetMethod(method);
			});
		}

		public IDebuggerProperty GetProperty(PropertyInfo prop) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetProperty(prop);
			});
		}

		public IDebuggerEvent GetEvent(EventInfo evt) {
			return dispatcher.UI(() => {
				var ad = FirstAppDomain;
				return ad == null ? null : ad.GetEvent(evt);
			});
		}

		IDebuggerType IDebugger.Void {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Void;
				});
			}
		}

		IDebuggerType IDebugger.Boolean {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Boolean;
				});
			}
		}

		IDebuggerType IDebugger.Char {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Char;
				});
			}
		}

		IDebuggerType IDebugger.SByte {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.SByte;
				});
			}
		}

		IDebuggerType IDebugger.Byte {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Byte;
				});
			}
		}

		IDebuggerType IDebugger.Int16 {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Int16;
				});
			}
		}

		IDebuggerType IDebugger.UInt16 {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.UInt16;
				});
			}
		}

		IDebuggerType IDebugger.Int32 {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Int32;
				});
			}
		}

		IDebuggerType IDebugger.UInt32 {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.UInt32;
				});
			}
		}

		IDebuggerType IDebugger.Int64 {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Int64;
				});
			}
		}

		IDebuggerType IDebugger.UInt64 {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.UInt64;
				});
			}
		}

		IDebuggerType IDebugger.Single {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Single;
				});
			}
		}

		IDebuggerType IDebugger.Double {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Double;
				});
			}
		}

		IDebuggerType IDebugger.String {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.String;
				});
			}
		}

		IDebuggerType IDebugger.TypedReference {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.TypedReference;
				});
			}
		}

		IDebuggerType IDebugger.IntPtr {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.IntPtr;
				});
			}
		}

		IDebuggerType IDebugger.UIntPtr {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.UIntPtr;
				});
			}
		}

		IDebuggerType IDebugger.Object {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Object;
				});
			}
		}

		IDebuggerType IDebugger.Decimal {
			get {
				return dispatcher.UI(() => {
					var ad = FirstAppDomain;
					return ad == null ? null : ad.Decimal;
				});
			}
		}

		public IEval CreateEvalUI(DebuggerThread thread) {
			dispatcher.VerifyAccess();
			var dbg = theDebugger.Debugger;
			if (dbg == null || dbg.ProcessState != DBG.DebuggerProcessState.Paused)
				throw new InvalidOperationException("Can only evaluate when the debugged process has paused at a safe location. Wait for a breakpoint to hit.");
			if (dbg.IsEvaluating)
				throw new InvalidOperationException("Only one evaluation at a time can be in progress");
			return new Eval(this, thread.AppDomain, theDebugger.CreateEval(thread.DnThread.CorThread));
		}
	}
}
