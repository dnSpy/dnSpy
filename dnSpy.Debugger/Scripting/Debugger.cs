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
using System.Threading;
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

		public DebuggerProcessState State {
			get { return dispatcher.UI(() => Utils.Convert(theDebugger.ProcessState)); }
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
			get { return dispatcher.UI(() => debugManager.Value.HasAttached);			}
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
			InitializePausedOrTerminatedEvent();
		}

		public event EventHandler<DebuggerEventArgs> OnProcessStateChanged;

		void TheDebugger_OnProcessStateChanged_Last(object sender, DBG.DebuggerEventArgs e) {
			switch (theDebugger.ProcessState) {
			case DBG.DebuggerProcessState.Starting:
				foreach (var bp in breakpointsToInitialize)
					Initialize(bp);
				breakpointsToInitialize.Clear();
				break;

			case DBG.DebuggerProcessState.Terminated:
				Debug.Assert(breakpointsToInitialize.Count == 0);
				breakpointsToInitialize.Clear();
				break;
			}

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
			bool eval = (dbg != null && (dbg.IsEvaluating || dbg.EvalCompleted));
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

		public bool Wait(int millisecondsTimeout) {
			Debug.Assert(!dispatcher.CheckAccess());
			if (dispatcher.CheckAccess())
				throw new InvalidOperationException("Wait() can't be called on the UI / debugger thread");
			return pausedOrTerminatedEvent.WaitOne(millisecondsTimeout);
		}

		const int POLL_WAIT_MS = 50;
		public void Wait(CancellationToken token) {
			Debug.Assert(!dispatcher.CheckAccess());
			if (dispatcher.CheckAccess())
				throw new InvalidOperationException("Wait() can't be called on the UI / debugger thread");

			if (token.Equals(CancellationToken.None)) {
				Wait(Timeout.Infinite);
				return;
			}

			for (;;) {
				token.ThrowIfCancellationRequested();
				if (pausedOrTerminatedEvent.WaitOne(POLL_WAIT_MS))
					return;
			}
		}

		public bool WaitRun(int millisecondsTimeout) {
			Debug.Assert(!dispatcher.CheckAccess());
			if (dispatcher.CheckAccess())
				throw new InvalidOperationException("WaitRun() can't be called on the UI / debugger thread");
			return runningEvent.WaitOne(millisecondsTimeout);
		}

		public void WaitRun(CancellationToken token) {
			Debug.Assert(!dispatcher.CheckAccess());
			if (dispatcher.CheckAccess())
				throw new InvalidOperationException("WaitRun() can't be called on the UI / debugger thread");

			if (token.Equals(CancellationToken.None)) {
				WaitRun(Timeout.Infinite);
				return;
			}

			for (;;) {
				token.ThrowIfCancellationRequested();
				if (runningEvent.WaitOne(POLL_WAIT_MS))
					return;
			}
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

		public void StepInto() {
			dispatcher.UI(() => debugManager.Value.StepInto());
		}

		public void StepInto(IStackFrame frame) {
			dispatcher.UI(() => debugManager.Value.StepInto(((StackFrame)frame).CorFrame));
		}

		public void StepOver() {
			dispatcher.UI(() => debugManager.Value.StepOver());
		}

		public void StepOver(IStackFrame frame) {
			dispatcher.UI(() => debugManager.Value.StepOver(((StackFrame)frame).CorFrame));
		}

		public void StepOut() {
			dispatcher.UI(() => debugManager.Value.StepOut());
		}

		public void StepOut(IStackFrame frame) {
			dispatcher.UI(() => debugManager.Value.StepOut(((StackFrame)frame).CorFrame));
		}

		public bool RunTo(IStackFrame frame) {
			return dispatcher.UI(() => debugManager.Value.RunTo(((StackFrame)frame).CorFrame));
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

		public IEnumerable<IThread> Threads {
			get { return dispatcher.UIIter(GetThreadsUI); }
		}

		IEnumerable<IThread> GetThreadsUI() {
			var dbg = theDebugger.Debugger;
			if (dbg == null)
				yield break;
			foreach (var p in dbg.Processes) {
				foreach (var t in p.Threads) {
					yield return new DebuggerThread(this, t);
				}
			}
		}

		public IThread ActiveThread {
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

		public int ActiveStackFrameIndex {
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

		internal IThread FindThreadUI(DBG.CorThread thread) {
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

		public void BreakOnLoad(string name, Action action) {
			if (name == null)
				throw new ArgumentNullException();
			CreateBreakpoint(DebugEventKind.LoadModule, (bp, ctx) => {
				var c = (ModuleEventContext)ctx;
				if (!IsSameModule(c.Module.ModuleName.Name, name))
					return false;
				bp.Remove();
				if (action != null)
					action();
				return true;
			});
		}

		static bool IsSameModule(string modName, string s) {
			if (StringComparer.OrdinalIgnoreCase.Equals(modName, s))
				return true;
			if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(modName), s))
				return true;
			if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileNameWithoutExtension(modName), s))
				return true;

			return false;
		}

		public void BreakOnLoadAssembly(string assemblyName, Action action, AssemblyNameComparerFlags flags) {
			BreakOnLoadAssembly(new AssemblyNameInfo(assemblyName), action, flags);
		}

		public void BreakOnLoadAssembly(IAssembly assembly, Action action, AssemblyNameComparerFlags flags) {
			if (assembly == null)
				throw new ArgumentNullException();
			assembly = assembly.ToAssemblyRef();// Prevent storing AssemblyDef refs
			// Use the LoadModule event since without a module, we won't know the full assembly name
			CreateBreakpoint(DebugEventKind.LoadModule, (bp, ctx) => {
				var c = (ModuleEventContext)ctx;
				var comparer = new AssemblyNameComparer(flags);
				if (!comparer.Equals(assembly, new AssemblyNameInfo(c.Module.Assembly.FullName)))
					return false;
				bp.Remove();
				if (action != null)
					action();
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
	}
}
