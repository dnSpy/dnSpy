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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CorDebug;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.CorDebug.Properties;

namespace dnSpy.Debugger.CorDebug.Impl {
	abstract class DbgEngineImpl : DbgEngine {
		public override DbgStartKind StartKind { get; }
		public override string[] DebugTags => new[] { PredefinedDebugTags.DotNetDebugger };
		public override event EventHandler<DbgEngineMessage> Message;

		Dispatcher Dispatcher => debuggerThread.Dispatcher;

		readonly DebuggerThread debuggerThread;
		readonly object lockObj;
		readonly DbgManager dbgManager;
		DnDebugger dnDebugger;
		SafeHandle hProcess_debuggee;

		DbgClrRuntimeImpl ClrRuntime {
			get {
				if (clrRuntime != null)
					return clrRuntime;
				lock (lockObj) {
					if (clrRuntime != null)
						return clrRuntime;
					clrRuntime = new DbgClrRuntimeImpl(dbgManager, CorDebugRuntimeKind, dnDebugger.DebuggeeVersion ?? string.Empty, dnDebugger.CLRPath, dnDebugger.RuntimeDirectory);
				}
				return clrRuntime;
			}
		}
		DbgClrRuntimeImpl clrRuntime;

		protected DbgEngineImpl(DbgManager dbgManager, DbgStartKind startKind) {
			StartKind = startKind;
			lockObj = new object();
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			debuggerThread = new DebuggerThread("CorDebug");
			debuggerThread.CallDispatcherRun();
		}

		void ExecDebugThreadAsync(Action action) {
			if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
				Dispatcher.BeginInvoke(DispatcherPriority.Send, action);
		}

		void ExecDebugThreadAsync(Action<object> action, object arg) {
			if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
				Dispatcher.BeginInvoke(DispatcherPriority.Send, action, arg);
		}

		void DnDebugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			switch (e.Kind) {
			case DebugCallbackKind.CreateProcess:
				var cp = (CreateProcessDebugCallbackEventArgs)e;
				hProcess_debuggee = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)(cp.CorProcess?.ProcessId ?? -1));
				SendMessage(new DbgMessageConnected(cp.CorProcess.ProcessId));
				break;

			case DebugCallbackKind.ExitProcess:
				// Handled in DnDebugger_OnProcessStateChanged()
				break;
			}
		}

		void UnregisterEventsAndCloseProcessHandle() {
			if (dnDebugger != null) {
				dnDebugger.DebugCallbackEvent -= DnDebugger_DebugCallbackEvent;
				dnDebugger.OnProcessStateChanged -= DnDebugger_OnProcessStateChanged;
				dnDebugger.OnModuleAdded -= DnDebugger_OnModuleAdded;
			}
			hProcess_debuggee?.Close();
		}

		void DnDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			Debug.Assert(sender != null && sender == dnDebugger);

			if (dnDebugger.ProcessState == DebuggerProcessState.Terminated) {
				if (hProcess_debuggee == null || hProcess_debuggee.IsClosed || hProcess_debuggee.IsInvalid || !NativeMethods.GetExitCodeProcess(hProcess_debuggee.DangerousGetHandle(), out int exitCode))
					exitCode = -1;
				UnregisterEventsAndCloseProcessHandle();

				SendMessage(new DbgMessageDisconnected(exitCode));
				return;
			}
		}

		void DnDebugger_OnModuleAdded(object sender, ModuleDebuggerEventArgs e) {
			if (e.Added)
				ClrRuntime.AddModule(e.Module);
			else
				ClrRuntime.RemoveModule(e.Module);
		}

		void SendMessage(DbgEngineMessage message) => Message?.Invoke(this, message);

		protected abstract CLRTypeDebugInfo CreateDebugInfo(CorDebugStartDebuggingOptions options);

		public override void Start(StartDebuggingOptions options) =>
			ExecDebugThreadAsync(StartCore, options);

		void StartCore(object arg) {
			Dispatcher.VerifyAccess();
			CorDebugStartDebuggingOptions options = null;
			try {
				if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
					throw new InvalidOperationException("Dispatcher has shut down");
				options = (CorDebugStartDebuggingOptions)arg;
				var dbgOptions = new DebugProcessOptions(CreateDebugInfo(options)) {
					DebugMessageDispatcher = new WpfDebugMessageDispatcher(Dispatcher),
					CurrentDirectory = options.WorkingDirectory,
					Filename = options.Filename,
					CommandLine = options.CommandLine,
					BreakProcessKind = options.BreakProcessKind.ToDndbg(),
				};
				dbgOptions.DebugOptions.IgnoreBreakInstructions = options.IgnoreBreakInstructions;

				dnDebugger = DnDebugger.DebugProcess(dbgOptions);
				if (options.DisableManagedDebuggerDetection)
					DisableSystemDebuggerDetection.Initialize(dnDebugger);

				dnDebugger.DebugCallbackEvent += DnDebugger_DebugCallbackEvent;
				dnDebugger.OnProcessStateChanged += DnDebugger_OnProcessStateChanged;
				dnDebugger.OnModuleAdded += DnDebugger_OnModuleAdded;
				return;
			}
			catch (Exception ex) {
				var cex = ex as COMException;
				const int ERROR_NOT_SUPPORTED = unchecked((int)0x80070032);
				string errMsg;
				if (cex != null && cex.ErrorCode == ERROR_NOT_SUPPORTED)
					errMsg = string.Format(dnSpy_Debugger_CorDebug_Resources.Error_CouldNotStartDebugger, GetIncompatiblePlatformErrorMessage());
				else if (cex != null && cex.ErrorCode == CordbgErrors.CORDBG_E_UNCOMPATIBLE_PLATFORMS)
					errMsg = string.Format(dnSpy_Debugger_CorDebug_Resources.Error_CouldNotStartDebugger, GetIncompatiblePlatformErrorMessage());
				else if (cex != null && cex.ErrorCode == unchecked((int)0x800702E4))
					errMsg = dnSpy_Debugger_CorDebug_Resources.Error_CouldNotStartDebuggerRequireAdminPrivLvl;
				else
					errMsg = string.Format(dnSpy_Debugger_CorDebug_Resources.Error_CouldNotStartDebuggerCheckAccessToFile, options?.Filename ?? "<???>", ex.Message);

				SendMessage(new DbgMessageConnected(errMsg));
				return;
			}
		}

		static string GetIncompatiblePlatformErrorMessage() {
			if (IntPtr.Size == 4)
				return dnSpy_Debugger_CorDebug_Resources.UseDnSpyExeToDebug64;
			return dnSpy_Debugger_CorDebug_Resources.UseDnSpy64ExeToDebug32;
		}

		protected abstract CorDebugRuntimeKind CorDebugRuntimeKind { get; }

		public override DbgRuntime CreateRuntime(DbgProcess process) {
			var runtime = ClrRuntime;
			if (runtime.Process != null)
				throw new InvalidOperationException();
			runtime.SetProcess(process);
			return runtime;
		}

		protected override void CloseCore() {
			UnregisterEventsAndCloseProcessHandle();
			debuggerThread.Terminate();
		}

		bool HasConnected_DebugThread {
			get {
				Dispatcher.VerifyAccess();
				// If it's null, we haven't connected yet (most likely due to timeout, eg. trying to debug
				// a .NET Framework program with the .NET Core engine)
				return dnDebugger != null;
			}
		}

		public override void Break() => ExecDebugThreadAsync(BreakCore);
		void BreakCore() {
			Dispatcher.VerifyAccess();
			if (!HasConnected_DebugThread)
				return;
			if (dnDebugger.ProcessState == DebuggerProcessState.Starting || dnDebugger.ProcessState == DebuggerProcessState.Running) {
				int hr = dnDebugger.TryBreakProcesses();
				if (hr < 0)
					SendMessage(new DbgMessageBreak($"Couldn't break the process, hr=0x{hr:X8}"));
				else {
					Debug.Assert(dnDebugger.ProcessState == DebuggerProcessState.Paused);
					SendMessage(new DbgMessageBreak());
				}
			}
			else
				SendMessage(new DbgMessageBreak());
		}

		public override void Run() => ExecDebugThreadAsync(RunCore);
		void RunCore() {
			Dispatcher.VerifyAccess();
			if (!HasConnected_DebugThread)
				return;
			if (dnDebugger.ProcessState == DebuggerProcessState.Paused)
				dnDebugger.Continue();
		}

		public override void StopDebugging() {
			if (StartKind == DbgStartKind.Attach)
				Detach();
			else
				Terminate();
		}

		public override void Terminate() => ExecDebugThreadAsync(TerminateCore);
		void TerminateCore() {
			Dispatcher.VerifyAccess();
			if (!HasConnected_DebugThread)
				return;
			if (dnDebugger.ProcessState != DebuggerProcessState.Terminated)
				dnDebugger.TerminateProcesses();
		}

		public override bool CanDetach => true;

		public override void Detach() => ExecDebugThreadAsync(DetachCore);
		void DetachCore() {
			Dispatcher.VerifyAccess();
			if (!HasConnected_DebugThread)
				return;
			if (dnDebugger.ProcessState != DebuggerProcessState.Terminated) {
				int hr = dnDebugger.TryDetach();
				if (hr < 0) {
					Debug.Assert(hr == CordbgErrors.CORDBG_E_UNRECOVERABLE_ERROR);
					dnDebugger.TerminateProcesses();
				}
			}
		}
	}
}
