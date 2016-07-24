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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Output;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Properties;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger.Logger {
	[ExportAutoLoaded]
	sealed class CreateOutputLogger : IAutoLoaded {
		[ImportingConstructor]
		CreateOutputLogger(OutputLogger logger) {
			// Nothing
		}
	}

	[Export, Export(typeof(ILoadBeforeDebug))]
	sealed class OutputLogger : ILoadBeforeDebug {
		public static readonly Guid GUID_OUTPUT_LOGGER_DEBUG = new Guid("7B6E802A-B58C-4689-877E-3358FCDCEFAC");

		readonly IOutputManager outputManager;
		readonly IOutputTextPane textPane;
		readonly IOutputLoggerSettings outputLoggerSettings;

		[ImportingConstructor]
		OutputLogger(IOutputManager outputManager, ITheDebugger theDebugger, IOutputLoggerSettings outputLoggerSettings, IContentTypeRegistryService contentTypeRegistryService) {
			this.outputManager = outputManager;
			this.textPane = outputManager.Create(GUID_OUTPUT_LOGGER_DEBUG, dnSpy_Debugger_Resources.DebugLoggerName, contentTypeRegistryService.GetContentType(ContentTypes.OutputDebug));
			this.outputLoggerSettings = outputLoggerSettings;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (dbg.ProcessState) {
			case DebuggerProcessState.Starting:
				if (outputLoggerSettings.ShowDebugOutputLog)
					outputManager.Select(GUID_OUTPUT_LOGGER_DEBUG);

				debugState?.Dispose();
				debugState = null;

				textPane.Clear();
				dbg.DebugCallbackEvent += DnDebugger_DebugCallbackEvent;
				debugState = new DebugState(dbg);
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Paused:
				Debug.Assert(debugState != null && debugState.dbg == dbg);
				break;

			case DebuggerProcessState.Terminated:
				Debug.Assert(debugState != null);
				Debug.Assert(debugState?.dbg == dbg);

				if (outputLoggerSettings.ShowProcessExitMessages) {
					int processExitCode;
					if (!NativeMethods.GetExitCodeProcess(debugState.hProcess_debuggee, out processExitCode))
						processExitCode = -1;
					textPane.WriteLine(BoxedOutputColor.DebugLogExitProcess,
						string.Format(dnSpy_Debugger_Resources.DebugLogExitProcess,
								GetProcessNameWithPID(debugState.debuggedProcess),
								processExitCode));
				}

				debugState?.Dispose();
				debugState = null;
				break;
			}
		}
		DebugState debugState;

		sealed class DebugState : IDisposable {
			public readonly DnDebugger dbg;
			public readonly IntPtr hProcess_debuggee;
			public readonly DnProcess debuggedProcess;

			public DebugState(DnDebugger dbg) {
				this.dbg = dbg;

				Debug.Assert(dbg.Processes.Length == 1);
				this.debuggedProcess = dbg.Processes[0];

				const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
				this.hProcess_debuggee = NativeMethods.OpenProcess2(PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)this.debuggedProcess.ProcessId);
				Debug.Assert(this.hProcess_debuggee != IntPtr.Zero, string.Format("OpenProcess() failed: 0x{0:X8}", Marshal.GetLastWin32Error()));
			}

			public void Dispose() {
				bool b = NativeMethods.CloseHandle(hProcess_debuggee);
				Debug.Assert(b, string.Format("CloseHandle() failed: 0x{0:X8}", Marshal.GetLastWin32Error()));
			}
		}

		string GetProcessName(DnProcess process) {
			if (process == null)
				return "???";
			var name = process.Filename;
			if (string.IsNullOrEmpty(name))
				return string.Format("0x{0:X}", process.ProcessId);
			return Path.GetFileName(name);
		}

		string GetProcessFullPath(DnProcess process) {
			if (process == null)
				return "???";
			var name = process.Filename;
			if (string.IsNullOrEmpty(name))
				return string.Format("0x{0:X}", process.ProcessId);
			return name;
		}

		string GetProcessNameWithPID(DnProcess process) => string.Format("[0x{0:X}] {1}", process?.ProcessId, GetProcessName(process));

		string GetModuleName(DnModule module) {
			if (module == null)
				return "???";
			if (module.IsInMemory)
				return module.Name;
			try {
				return Path.GetFileName(module.Name);
			}
			catch {
			}
			return module.Name;
		}

		string GetRuntimeVersion(DnDebugger dbg) {
			var s = dbg.DebuggeeVersion;
			if (!string.IsNullOrEmpty(s))
				return "CLR " + s;
			return "CoreCLR";
		}

		string FilterUserMessage(string s) {
			if (s == null)
				return string.Empty;
			const int MAX_USER_MSG_LEN = 16 * 1024;
			if (s.Length > MAX_USER_MSG_LEN) {
				const string ELLIPSIS = "[...]";
				var sb = new StringBuilder(MAX_USER_MSG_LEN + ELLIPSIS.Length + Environment.NewLine.Length);
				bool endsInNewLine = s[s.Length - 1] == '\n';
				sb.Append(s, 0, MAX_USER_MSG_LEN);
				sb.Append(ELLIPSIS);
				if (endsInNewLine)
					sb.Append(Environment.NewLine);
				s = sb.ToString();
			}
			return s;
		}

		void DnDebugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			if (debugState?.dbg != dbg)
				return;

			switch (e.Kind) {
			case DebugCallbackKind.Breakpoint:
			case DebugCallbackKind.StepComplete:
			case DebugCallbackKind.Break:
			case DebugCallbackKind.Exception:
			case DebugCallbackKind.EvalComplete:
			case DebugCallbackKind.EvalException:
			case DebugCallbackKind.CreateProcess:
			case DebugCallbackKind.ExitProcess:
			case DebugCallbackKind.CreateThread:
			case DebugCallbackKind.LoadClass:
			case DebugCallbackKind.UnloadClass:
			case DebugCallbackKind.DebuggerError:
			case DebugCallbackKind.LogSwitch:
			case DebugCallbackKind.CreateAppDomain:
			case DebugCallbackKind.ExitAppDomain:
			case DebugCallbackKind.LoadAssembly:
			case DebugCallbackKind.UnloadAssembly:
			case DebugCallbackKind.ControlCTrap:
			case DebugCallbackKind.NameChange:
			case DebugCallbackKind.UpdateModuleSymbols:
			case DebugCallbackKind.EditAndContinueRemap:
			case DebugCallbackKind.BreakpointSetError:
			case DebugCallbackKind.FunctionRemapOpportunity:
			case DebugCallbackKind.CreateConnection:
			case DebugCallbackKind.ChangeConnection:
			case DebugCallbackKind.DestroyConnection:
			case DebugCallbackKind.ExceptionUnwind:
			case DebugCallbackKind.FunctionRemapComplete:
			case DebugCallbackKind.CustomNotification:
				break;

			case DebugCallbackKind.ExitThread:
				if (outputLoggerSettings.ShowThreadExitMessages) {
					var etArgs = (ExitThreadDebugCallbackEventArgs)e;
					int threadExitCode;
					if (!NativeMethods.GetExitCodeThread(etArgs.CorThread?.Handle ?? IntPtr.Zero, out threadExitCode))
						threadExitCode = -1;
					textPane.WriteLine(BoxedOutputColor.DebugLogExitThread,
						string.Format(dnSpy_Debugger_Resources.DebugLogExitThread,
								etArgs.CorThread?.ThreadId ?? 0,
								threadExitCode));
				}
				break;

			case DebugCallbackKind.LoadModule:
				if (outputLoggerSettings.ShowModuleLoadMessages) {
					var lmArgs = (LoadModuleDebugCallbackEventArgs)e;
					var module = dbg.Modules.FirstOrDefault(a => a.CorModule == lmArgs.CorModule);
					Debug.Assert(module != null);
					textPane.WriteLine(BoxedOutputColor.DebugLogLoadModule,
						string.Format(dnSpy_Debugger_Resources.DebugLogLoadModule,
								GetProcessName(module?.Process),
								GetRuntimeVersion(dbg),
								module?.AppDomain?.Name ?? "???",
								module?.Name));
				}
				break;

			case DebugCallbackKind.UnloadModule:
				if (outputLoggerSettings.ShowModuleUnloadMessages) {
					var ulmArgs = (UnloadModuleDebugCallbackEventArgs)e;
					var module = dbg.Modules.FirstOrDefault(a => a.CorModule == ulmArgs.CorModule);
					Debug.Assert(module != null);
					textPane.WriteLine(BoxedOutputColor.DebugLogUnloadModule,
						string.Format(dnSpy_Debugger_Resources.DebugLogUnloadModule,
								GetProcessName(module?.Process),
								GetRuntimeVersion(dbg),
								module?.AppDomain?.Name ?? "???",
								module?.Name));
				}
				break;

			case DebugCallbackKind.LogMessage:
				if (outputLoggerSettings.ShowProgramOutputMessages) {
					var lmsgArgs = (LogMessageDebugCallbackEventArgs)e;
					var msg = FilterUserMessage(lmsgArgs.Message);
					textPane.Write(BoxedOutputColor.DebugLogProgramOutput, msg);
				}
				break;

			case DebugCallbackKind.Exception2:
				if (outputLoggerSettings.ShowExceptionMessages) {
					var ex2Args = (Exception2DebugCallbackEventArgs)e;
					CorValue exValue;
					DnModule exModule;
					CorModule module;
					string exMsg;

					switch (ex2Args.EventType) {
					case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_FIRST_CHANCE:
					case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_USER_FIRST_CHANCE:
						break;

					case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_CATCH_HANDLER_FOUND:
						exValue = ex2Args.CorThread?.CurrentException;
						module = ex2Args.CorFrame?.Function?.Module;
						exModule = dbg.Modules.FirstOrDefault(a => a.CorModule == module);
						textPane.WriteLine(BoxedOutputColor.DebugLogExceptionHandled,
							string.Format(dnSpy_Debugger_Resources.DebugLogExceptionHandled,
									exValue?.ExactType?.ToString() ?? "???",
									GetModuleName(exModule)));
						exMsg = FilterUserMessage(EvalUtils.ReflectionReadExceptionMessage(exValue) ?? "???");
						textPane.WriteLine(BoxedOutputColor.DebugLogExceptionHandled,
							string.Format(dnSpy_Debugger_Resources.DebugLogAdditionalInformation, exMsg));
						break;

					case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_UNHANDLED:
						exValue = ex2Args.CorThread?.CurrentException;
						module = ex2Args.CorFrame?.Function?.Module;
						exModule = dbg.Modules.FirstOrDefault(a => a.CorModule == module);
						textPane.WriteLine(BoxedOutputColor.DebugLogExceptionUnhandled,
							string.Format(dnSpy_Debugger_Resources.DebugLogExceptionUnhandled,
									exValue?.ExactType?.ToString() ?? "???",
									GetModuleName(exModule)));
						exMsg = FilterUserMessage(EvalUtils.ReflectionReadExceptionMessage(exValue) ?? "???");
						textPane.WriteLine(BoxedOutputColor.DebugLogExceptionUnhandled,
							string.Format(dnSpy_Debugger_Resources.DebugLogAdditionalInformation, exMsg));
						break;

					default:
						Debug.Fail("Unknown exception event type: " + ex2Args.EventType);
						break;
					}
				}
				break;

			case DebugCallbackKind.MDANotification:
				if (outputLoggerSettings.ShowMDAMessages) {
					var mdaArgs = (MDANotificationDebugCallbackEventArgs)e;
					var mda = mdaArgs.CorMDA;
					var corProcess = mdaArgs.CorProcess ?? mdaArgs.CorThread?.Process;
					var process = dbg.Processes.FirstOrDefault(a => a.CorProcess == corProcess);
					textPane.WriteLine(BoxedOutputColor.DebugLogMDA,
						string.Format(dnSpy_Debugger_Resources.DebugLogMDA,
								mda.Name ?? "???",
								GetProcessFullPath(process)));
					textPane.WriteLine(BoxedOutputColor.DebugLogMDA,
						string.Format(dnSpy_Debugger_Resources.DebugLogAdditionalInformation, mda.Description ?? "???"));
				}
				break;
			}
		}
	}
}
