// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Debugger.Interop.CorDebug;
using Debugger.Interop.CorSym;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Visitors;

namespace Debugger
{
	internal enum DebuggeeStateAction { Keep, Clear }
	
	/// <summary>
	/// Debug Mode Flags.
	/// </summary>
	public enum DebugModeFlag
	{
		/// <summary>
		/// Run in the same mode as without debugger.
		/// </summary>
		Default,
		/// <summary>
		/// Run in forced optimized mode.
		/// </summary>
		Optimized,
		/// <summary>
		/// Run in debug mode (easy inspection) but slower.
		/// </summary>
		Debug,
		/// <summary>
		/// Run in ENC mode (ENC possible) but even slower than debug
		/// </summary>
		Enc
	}
	
	public class Process: DebuggerObject
	{
		NDebugger debugger;
		
		ICorDebugProcess corProcess;
		ManagedCallback callbackInterface;
		
		EvalCollection activeEvals;
		ModuleCollection modules;
		ThreadCollection threads;
		AppDomainCollection appDomains;
		
		string workingDirectory;
		
		
		public NDebugger Debugger {
			get { return debugger; }
		}
		
		internal ICorDebugProcess CorProcess {
			get { return corProcess; }
		}
		
		public Options Options {
			get { return debugger.Options; }
		}
		
		public string DebuggeeVersion {
			get { return debugger.DebuggeeVersion; }
		}
		
		internal ManagedCallback CallbackInterface {
			get { return callbackInterface; }
		}
		
		public EvalCollection ActiveEvals {
			get { return activeEvals; }
		}
		
		internal bool Evaluating {
			get { return activeEvals.Count > 0; }
		}
		
		public ModuleCollection Modules {
			get { return modules; }
		}
		
		public ThreadCollection Threads {
			get { return threads; }
		}
		
		public Thread SelectedThread {
			get { return this.Threads.Selected; }
			set { this.Threads.Selected = value; }
		}
		
		public StackFrame SelectedStackFrame {
			get {
				if (SelectedThread == null) {
					return null;
				} else {
					return SelectedThread.SelectedStackFrame;
				}
			}
		}
		
		public SourcecodeSegment NextStatement {
			get {
				if (SelectedStackFrame == null || IsRunning) {
					return null;
				} else {
					return SelectedStackFrame.NextStatement;
				}
			}
		}
		
		public bool BreakAtBeginning {
			get;
			set;
		}
		
		public AppDomainCollection AppDomains {
			get { return appDomains; }
		}
		
		List<Stepper> steppers = new List<Stepper>();
		
		internal List<Stepper> Steppers {
			get { return steppers; }
		}
		
		public string WorkingDirectory {
			get { return workingDirectory; }
		}
		
		public static DebugModeFlag DebugMode { get; set; }
		
		internal Process(NDebugger debugger, ICorDebugProcess corProcess, string workingDirectory)
		{
			this.debugger = debugger;
			this.corProcess = corProcess;
			this.workingDirectory = workingDirectory;
			
			this.callbackInterface = new ManagedCallback(this);
			
			activeEvals = new EvalCollection(debugger);
			modules = new ModuleCollection(debugger);
			modules.Added += OnModulesAdded;
			threads = new ThreadCollection(debugger);
			appDomains = new AppDomainCollection(debugger);
		}
		
		static unsafe public Process CreateProcess(NDebugger debugger, string filename, string workingDirectory, string arguments)
		{
			debugger.TraceMessage("Executing " + filename + " " + arguments);
			
			uint[] processStartupInfo = new uint[17];
			processStartupInfo[0] = sizeof(uint) * 17;
			uint[] processInfo = new uint[4];
			
			ICorDebugProcess outProcess;
			
			if (workingDirectory == null || workingDirectory == "") {
				workingDirectory = System.IO.Path.GetDirectoryName(filename);
			}
			
			_SECURITY_ATTRIBUTES secAttr = new _SECURITY_ATTRIBUTES();
			secAttr.bInheritHandle = 0;
			secAttr.lpSecurityDescriptor = IntPtr.Zero;
			secAttr.nLength = (uint)sizeof(_SECURITY_ATTRIBUTES);
			
			fixed (uint* pprocessStartupInfo = processStartupInfo)
				fixed (uint* pprocessInfo = processInfo)
				outProcess =
				debugger.CorDebug.CreateProcess(
					filename,   // lpApplicationName
					// If we do not prepend " ", the first argument migh just get lost
					" " + arguments,                       // lpCommandLine
					ref secAttr,                       // lpProcessAttributes
					ref secAttr,                      // lpThreadAttributes
					1,//TRUE                    // bInheritHandles
					0x00000010 /*CREATE_NEW_CONSOLE*/,    // dwCreationFlags
					IntPtr.Zero,                       // lpEnvironment
					workingDirectory,                       // lpCurrentDirectory
					(uint)pprocessStartupInfo,        // lpStartupInfo
					(uint)pprocessInfo,               // lpProcessInformation,
					CorDebugCreateProcessFlags.DEBUG_NO_SPECIAL_OPTIONS   // debuggingFlags
				);
			
			return new Process(debugger, outProcess, workingDirectory);
		}
		
		/// <summary> Fired when System.Diagnostics.Trace.WriteLine() is called in debuged process </summary>
		public event EventHandler<MessageEventArgs> LogMessage;
		
		protected internal virtual void OnLogMessage(MessageEventArgs arg)
		{
			TraceMessage ("Debugger event: OnLogMessage");
			if (LogMessage != null) {
				LogMessage(this, arg);
			}
		}
		
		public void TraceMessage(string message, params object[] args)
		{
			if (args.Length > 0)
				message = string.Format(message, args);
			System.Diagnostics.Debug.WriteLine("Debugger:" + message);
			debugger.OnDebuggerTraceMessage(new MessageEventArgs(this, message));
		}
		
		/// <summary> Read the specified amount of memory at the given memory address </summary>
		/// <returns> The content of the memory.  The amount of the read memory may be less then requested. </returns>
		public unsafe byte[] ReadMemory(ulong address, int size)
		{
			byte[] buffer = new byte[size];
			int readCount;
			fixed(byte* pBuffer = buffer) {
				readCount = (int)corProcess.ReadMemory(address, (uint)size, new IntPtr(pBuffer));
			}
			if (readCount != size) Array.Resize(ref buffer, readCount);
			return buffer;
		}
		
		/// <summary> Writes the given buffer at the specified memory address </summary>
		/// <returns> The number of bytes written </returns>
		public unsafe int WriteMemory(ulong address, byte[] buffer)
		{
			if (buffer.Length == 0) return 0;
			int written;
			fixed(byte* pBuffer = buffer) {
				written = (int)corProcess.WriteMemory(address, (uint)buffer.Length, new IntPtr(pBuffer));
			}
			return written;
		}
		
		#region Exceptions
		
		bool pauseOnHandledException = false;
		
		public event EventHandler<ExceptionEventArgs> ExceptionThrown;
		
		public bool PauseOnHandledException {
			get { return pauseOnHandledException; }
			set { pauseOnHandledException = value; }
		}
		
		protected internal virtual void OnExceptionThrown(ExceptionEventArgs e)
		{
			TraceMessage ("Debugger event: OnExceptionThrown()");
			if (ExceptionThrown != null) {
				ExceptionThrown(this, e);
			}
		}
		
		#endregion
		
		// State control for the process
		
		internal bool TerminateCommandIssued = false;
		internal Queue<Breakpoint> BreakpointHitEventQueue = new Queue<Breakpoint>();
		internal Dictionary<AstNode, TypedValue> ExpressionsCache = new Dictionary<AstNode, TypedValue>();
		
		#region Events
		
		public event EventHandler<ProcessEventArgs> Paused;
		public event EventHandler<ProcessEventArgs> Resumed;
		
		// HACK: public
		public virtual void OnPaused()
		{
			AssertPaused();
			// No real purpose - just additional check
			if (callbackInterface.IsInCallback) throw new DebuggerException("Can not raise event within callback.");
			TraceMessage ("Debugger event: OnPaused()");
			if (Paused != null) {
				foreach(Delegate d in Paused.GetInvocationList()) {
					if (IsRunning) {
						TraceMessage ("Skipping OnPaused delegate because process has resumed");
						break;
					}
					if (this.TerminateCommandIssued || this.HasExited) {
						TraceMessage ("Skipping OnPaused delegate because process has exited");
						break;
					}
					d.DynamicInvoke(this, new ProcessEventArgs(this));
				}
			}
		}
		
		protected virtual void OnResumed()
		{
			AssertRunning();
			if (callbackInterface.IsInCallback)
				throw new DebuggerException("Can not raise event within callback.");
			TraceMessage ("Debugger event: OnResumed()");
			if (Resumed != null) {
				Resumed(this, new ProcessEventArgs(this));
			}
		}
		
		#endregion
		
		#region PauseSession & DebugeeState
		
		PauseSession pauseSession;
		DebuggeeState debuggeeState;
		
		/// <summary>
		/// Indentification of the current debugger session. This value changes whenever debugger is continued
		/// </summary>
		public PauseSession PauseSession {
			get { return pauseSession; }
		}
		
		/// <summary>
		/// Indentification of the state of the debugee. This value changes whenever the state of the debugee significatntly changes
		/// </summary>
		public DebuggeeState DebuggeeState {
			get { return debuggeeState; }
		}
		
		/// <summary> Puts the process into a paused state </summary>
		internal void NotifyPaused(PausedReason pauseReason)
		{
			AssertRunning();
			pauseSession = new PauseSession(this, pauseReason);
			if (debuggeeState == null) {
				debuggeeState = new DebuggeeState(this);
			}
		}
		
		/// <summary> Puts the process into a resumed state </summary>
		internal void NotifyResumed(DebuggeeStateAction action)
		{
			AssertPaused();
			pauseSession = null;
			if (action == DebuggeeStateAction.Clear) {
				if (debuggeeState == null) throw new DebuggerException("Debugee state already cleared");
				debuggeeState = null;
				this.ExpressionsCache.Clear();
			}
		}
		
		/// <summary> Sets up the eviroment and raises user events </summary>
		internal void RaisePausedEvents()
		{
			AssertPaused();
			DisableAllSteppers();
			CheckSelectedStackFrames();
			SelectMostRecentStackFrameWithLoadedSymbols();
			
			if (this.PauseSession.PausedReason == PausedReason.Exception) {
				ExceptionEventArgs args = new ExceptionEventArgs(this, this.SelectedThread.CurrentException, this.SelectedThread.CurrentExceptionType, this.SelectedThread.CurrentExceptionIsUnhandled);
				OnExceptionThrown(args);
				// The event could have resumed or killed the process
				if (this.IsRunning || this.TerminateCommandIssued || this.HasExited) return;
			}
			
			while(BreakpointHitEventQueue.Count > 0) {
				Breakpoint breakpoint = BreakpointHitEventQueue.Dequeue();
				breakpoint.NotifyHit();
				// The event could have resumed or killed the process
				if (this.IsRunning || this.TerminateCommandIssued || this.HasExited) return;
			}
			
			OnPaused();
			// The event could have resumed the process
			if (this.IsRunning || this.TerminateCommandIssued || this.HasExited) return;
		}
		
		#endregion
		
		internal void AssertPaused()
		{
			if (IsRunning) {
				throw new DebuggerException("Process is not paused.");
			}
		}
		
		internal void AssertRunning()
		{
			if (IsPaused) {
				throw new DebuggerException("Process is not running.");
			}
		}
		
		public bool IsRunning {
			get { return pauseSession == null; }
		}
		
		public bool IsPaused {
			get { return !IsRunning; }
		}
		
		bool hasExited = false;
		
		public event EventHandler Exited;
		
		public bool HasExited {
			get {
				return hasExited;
			}
		}
		
		internal void NotifyHasExited()
		{
			if(!hasExited) {
				hasExited = true;
				if (Exited != null) {
					Exited(this, new ProcessEventArgs(this));
				}
				// Expire pause seesion first
				if (IsPaused) {
					NotifyResumed(DebuggeeStateAction.Clear);
				}
				debugger.Processes.Remove(this);
			}
		}
		
		public void Break()
		{
			AssertRunning();
			
			corProcess.Stop(uint.MaxValue); // Infinite; ignored anyway
			
			NotifyPaused(PausedReason.ForcedBreak);
			RaisePausedEvents();
		}
		
		public void Detach()
		{
			if (IsRunning) {
				corProcess.Stop(uint.MaxValue);
				NotifyPaused(PausedReason.ForcedBreak);
			}
			
			// This is necessary for detach
			foreach(Stepper s in this.Steppers) {
				if (s.CorStepper.IsActive() == 1) {
					s.CorStepper.Deactivate();
				}
			}
			this.Steppers.Clear();
			
			corProcess.Detach();
			
			// modules
			foreach(Module m in this.Modules)
			{
				m.Dispose();
			}
			
			this.modules.Clear();
			
			// threads
			this.threads.Clear();
			
			NotifyHasExited();
		}
		
		public void Continue()
		{
			AsyncContinue();
			WaitForPause();
		}
		
		internal Thread[] UnsuspendedThreads {
			get {
				List<Thread> unsuspendedThreads = new List<Thread>(this.Threads.Count);
				foreach(Thread t in this.Threads) {
					if (!t.Suspended)
						unsuspendedThreads.Add(t);
				}
				return unsuspendedThreads.ToArray();
			}
		}
		
		/// <summary>
		/// Resume execution and run all threads not marked by the user as susspended.
		/// </summary>
		public void AsyncContinue()
		{
			AsyncContinue(DebuggeeStateAction.Clear, this.UnsuspendedThreads, CorDebugThreadState.THREAD_RUN);
		}
		
		internal CorDebugThreadState NewThreadState = CorDebugThreadState.THREAD_RUN;
		
		/// <param name="threadsToRun"> Null to keep current setting </param>
		/// <param name="newThreadState"> What happens to created threads.  Null to keep current setting </param>
		internal void AsyncContinue(DebuggeeStateAction action, Thread[] threadsToRun, CorDebugThreadState? newThreadState)
		{
			AssertPaused();
			
			if (threadsToRun != null) {
//				corProcess.SetAllThreadsDebugState(CorDebugThreadState.THREAD_SUSPEND, null);
//				Note: There is unreported thread, stopping it prevents the debugee from exiting
//				      It is not corProcess.GetHelperThreadID
//				ICorDebugThread[] ts = new ICorDebugThread[corProcess.EnumerateThreads().GetCount()];
//				corProcess.EnumerateThreads().Next((uint)ts.Length, ts);
				foreach(Thread t in this.Threads) {
					CorDebugThreadState state = Array.IndexOf(threadsToRun, t) == -1 ? CorDebugThreadState.THREAD_SUSPEND : CorDebugThreadState.THREAD_RUN;
					try {
						t.CorThread.SetDebugState(state);
					} catch (COMException e) {
						// The state of the thread is invalid. (Exception from HRESULT: 0x8013132D)
						// It can happen for example when thread has not started yet
						if ((uint)e.ErrorCode == 0x8013132D) {
							// TraceMessage("Can not suspend thread - The state of the thread is invalid.  Thread ID = " + t.CorThread.GetID());
						} else {
							throw;
						}
					}
				}
			}
			
			if (newThreadState != null) {
				this.NewThreadState = newThreadState.Value;
			}
			
			NotifyResumed(action);
			corProcess.Continue(0);
			if (this.Options.Verbose) {
				this.TraceMessage("Continue");
			}
			
			if (action == DebuggeeStateAction.Clear) {
				OnResumed();
			}
		}
		
		/// <summary> Terminates the execution of the process </summary>
		public void Terminate()
		{
			AsyncTerminate();
			// Wait until ExitProcess callback is received
			WaitForExit();
		}
		
		/// <summary> Terminates the execution of the process </summary>
		public void AsyncTerminate()
		{
			// Resume stoped tread
			if (this.IsPaused) {
				// We might get more callbacks so we should maintain consistent sate
				//AsyncContinue(); // Continue the process to get remaining callbacks
			}
			
			// Expose race condition - drain callback queue
			System.Threading.Thread.Sleep(0);
			
			// Stop&terminate - both must be called
			corProcess.Stop(uint.MaxValue);
			corProcess.Terminate(0);
			this.TerminateCommandIssued = true;
			
			// Do not mark the process as exited
			// This is done once ExitProcess callback is received
		}
		
		void SelectSomeThread()
		{
			if (this.SelectedThread != null && !this.SelectedThread.IsInValidState) {
				this.SelectedThread = null;
			}
			if (this.SelectedThread == null) {
				foreach(Thread thread in this.Threads) {
					if (thread.IsInValidState) {
						this.SelectedThread = thread;
						break;
					}
				}
			}
		}
		
		internal void CheckSelectedStackFrames()
		{
			foreach(Thread thread in this.Threads) {
				if (thread.IsInValidState) {
					if (thread.SelectedStackFrame != null && thread.SelectedStackFrame.IsInvalid) {
						thread.SelectedStackFrame = null;
					}
				} else {
					thread.SelectedStackFrame = null;
				}
			}
		}
		
		internal void SelectMostRecentStackFrameWithLoadedSymbols()
		{
			SelectSomeThread();
			if (this.SelectedThread != null) {
				this.SelectedThread.SelectedStackFrame = null;
				foreach (StackFrame stackFrame in this.SelectedThread.Callstack) {
					if (stackFrame.HasSymbols) {
						if (this.Options.StepOverDebuggerAttributes && stackFrame.MethodInfo.IsNonUserCode)
							continue;
						this.SelectedThread.SelectedStackFrame = stackFrame;
						break;
					}
				}
			}
		}
		
		internal Stepper GetStepper(ICorDebugStepper corStepper)
		{
			foreach(Stepper stepper in this.Steppers) {
				if (stepper.IsCorStepper(corStepper)) {
					return stepper;
				}
			}
			throw new DebuggerException("Stepper is not in collection");
		}
		
		internal void DisableAllSteppers()
		{
			foreach(Thread thread in this.Threads) {
				thread.CurrentStepIn = null;
			}
			foreach(Stepper stepper in this.Steppers) {
				stepper.Ignore = true;
			}
		}
		
		/// <summary>
		/// Waits until the debugger pauses unless it is already paused.
		/// Use PausedReason to find out why it paused.
		/// </summary>
		public void WaitForPause()
		{
			while(this.IsRunning && !this.HasExited) {
				debugger.MTA2STA.WaitForCall();
				debugger.MTA2STA.PerformAllCalls();
			}
			if (this.HasExited) throw new ProcessExitedException();
		}
		
		public void WaitForPause(TimeSpan timeout)
		{
			System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
			watch.Start();
			while(this.IsRunning && !this.HasExited) {
				TimeSpan timeLeft = timeout - watch.Elapsed;
				if (timeLeft <= TimeSpan.FromMilliseconds(10)) break;
				//this.TraceMessage("Time left: " + timeLeft.TotalMilliseconds);
				debugger.MTA2STA.WaitForCall(timeLeft);
				debugger.MTA2STA.PerformAllCalls();
			}
			if (this.HasExited) throw new ProcessExitedException();
		}
		
		/// <summary>
		/// Waits until the precesses exits.
		/// </summary>
		public void WaitForExit()
		{
			while(!this.HasExited) {
				debugger.MTA2STA.WaitForCall();
				debugger.MTA2STA.PerformAllCalls();
			}
		}
		
		#region Break at begining
		
		private void OnModulesAdded(object sender, CollectionItemEventArgs<Module> e)
		{
			if (BreakAtBeginning) {
				if (e.Item.SymReader == null) return; // No symbols
				
				try {
					// create a BP at entry point
					uint entryPoint = e.Item.SymReader.GetUserEntryPoint();
					if (entryPoint == 0) return; // no EP
					var mainFunction = e.Item.CorModule.GetFunctionFromToken(entryPoint);
					var corBreakpoint = mainFunction.CreateBreakpoint();
					corBreakpoint.Activate(1);
					
					// create a SD BP
					var breakpoint = new Breakpoint(this.debugger, corBreakpoint);
					this.debugger.Breakpoints.Add(breakpoint);
					breakpoint.Hit += delegate {
						if (breakpoint != null)
							breakpoint.Remove();
						breakpoint = null;
					};
				} catch {
					// the app does not have an entry point - COM exception
				}
				BreakAtBeginning = false;
			}
			
			if (ModulesAdded != null)
				ModulesAdded(this, new ModuleEventArgs(e.Item));
		}
		
		#endregion
		
		public event EventHandler<ModuleEventArgs> ModulesAdded;
	}
}
