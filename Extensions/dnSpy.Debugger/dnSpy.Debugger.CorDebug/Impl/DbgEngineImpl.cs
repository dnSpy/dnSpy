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
		public override event EventHandler<DbgEngineMessage> Message;

		readonly DebuggerThread debuggerThread;
		readonly Dispatcher debuggerDispatcher;
		readonly object lockObj;
		DnDebugger dnDebugger;

		protected DbgEngineImpl(DbgStartKind startKind) {
			StartKind = startKind;
			lockObj = new object();

			DebuggerThread.Input threadInput = null;
			try {
				threadInput = new DebuggerThread.Input();
				debuggerThread = new DebuggerThread(threadInput);
				threadInput.AutoResetEvent.WaitOne();
				threadInput.AutoResetEvent.Dispose();
				threadInput.AutoResetEvent = null;
				debuggerDispatcher = threadInput.Dispatcher;
			}
			catch {
				debuggerThread?.Terminate(threadInput);
				throw;
			}
		}

		void ExecDebugThreadAsync(Action<object> action, object arg) =>
			debuggerDispatcher.BeginInvoke(DispatcherPriority.Send, action, arg);

		public override void EnableMessages() {
			Debug.Assert(!messagesEnabled);
			DbgMessageConnected msg;
			lock (lockObj) {
				messagesEnabled = true;
				msg = connectedErrorMessage;
				connectedErrorMessage = null;
			}
			if (msg != null)
				SendMessage(msg);
			debuggerThread.CallDispatcherRun();
		}
		volatile bool messagesEnabled;
		DbgMessageConnected connectedErrorMessage;

		void DnDebugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			Debug.Assert(messagesEnabled);
			switch (e.Kind) {
			case DebugCallbackKind.CreateProcess:
				var cp = (CreateProcessDebugCallbackEventArgs)e;
				dnDebugger.OnProcessStateChanged += DnDebugger_OnProcessStateChanged;
				SendMessage(new DbgMessageConnected(cp.CorProcess.ProcessId));
				break;
			}
		}

		void DnDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			//TODO:
			Debug.Assert(messagesEnabled);
		}

		void SendMessage(DbgEngineMessage message) {
			Debug.Assert(messagesEnabled);
			Message?.Invoke(this, message);
		}

		protected abstract CLRTypeDebugInfo CreateDebugInfo(CorDebugStartDebuggingOptions options);

		internal void Start(CorDebugStartDebuggingOptions options) =>
			ExecDebugThreadAsync(StartCore, options);

		protected void StartCore(object arg) {
			CorDebugStartDebuggingOptions options = null;
			try {
				options = (CorDebugStartDebuggingOptions)arg;
				var dbgOptions = new DebugProcessOptions(CreateDebugInfo(options)) {
					DebugMessageDispatcher = new WpfDebugMessageDispatcher(debuggerDispatcher),
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

				DbgMessageConnected msg = null;
				lock (lockObj) {
					connectedErrorMessage = new DbgMessageConnected(errMsg);
					if (messagesEnabled) {
						msg = connectedErrorMessage;
						connectedErrorMessage = null;
					}
				}
				if (msg != null)
					SendMessage(msg);
				return;
			}
		}

		static string GetIncompatiblePlatformErrorMessage() {
			if (IntPtr.Size == 4)
				return dnSpy_Debugger_CorDebug_Resources.UseDnSpyExeToDebug64;
			return dnSpy_Debugger_CorDebug_Resources.UseDnSpy64ExeToDebug32;
		}

		protected abstract CorDebugRuntimeKind CorDebugRuntimeKind { get; }
		public override DbgRuntime CreateRuntime(DbgProcess process) =>
			new DbgClrRuntimeImpl(process, CorDebugRuntimeKind, dnDebugger.DebuggeeVersion ?? string.Empty, dnDebugger.CLRPath, dnDebugger.RuntimeDirectory);
	}
}
