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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Output;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Exceptions;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger.ToolWindows.Logger {
	[ExportOutputServiceListener]
	sealed class OutputServiceListener : IOutputServiceListener2 {
		readonly OutputLogger outputLogger;

		[ImportingConstructor]
		OutputServiceListener(OutputLogger outputLogger) => this.outputLogger = outputLogger;

		void IOutputServiceListener2.Initialize(IOutputService outputService) => ((IOutputServiceListener2)outputLogger).Initialize(outputService);
	}

	[Export(typeof(IDbgManagerStartListener))]
	[Export(typeof(OutputLogger))]
	[Export(typeof(ITracepointMessageListener))]
	sealed class OutputLogger : IOutputServiceListener2, IDbgManagerStartListener, ITracepointMessageListener {
		public static readonly Guid GUID_OUTPUT_LOGGER_DEBUG = new Guid("7B6E802A-B58C-4689-877E-3358FCDCEFAC");

		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IOutputService> outputService;
		readonly Lazy<IContentTypeRegistryService> contentTypeRegistryService;
		readonly OutputLoggerSettings outputLoggerSettings;
		readonly Lazy<DbgExceptionFormatterService> dbgExceptionFormatterService;
		IOutputTextPane? textPane;

		[ImportingConstructor]
		OutputLogger(UIDispatcher uiDispatcher, Lazy<IOutputService> outputService, Lazy<IContentTypeRegistryService> contentTypeRegistryService, OutputLoggerSettings outputLoggerSettings, Lazy<DbgExceptionFormatterService> dbgExceptionFormatterService) {
			this.uiDispatcher = uiDispatcher;
			this.outputService = outputService;
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.outputLoggerSettings = outputLoggerSettings;
			this.dbgExceptionFormatterService = dbgExceptionFormatterService;
		}

		void IOutputServiceListener2.Initialize(IOutputService outputService) => Initialize();

		void UI(Action callback) => uiDispatcher.UI(callback);

		void Initialize() => UI(() => Initialize_UI());
		void Initialize_UI() {
			uiDispatcher.VerifyAccess();
			if (textPane is not null)
				return;
			textPane = outputService.Value.Create(GUID_OUTPUT_LOGGER_DEBUG, dnSpy_Debugger_Resources.DebugLoggerName, contentTypeRegistryService.Value.GetContentType(ContentTypes.OutputDebug));
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			Initialize();
			dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
			dbgManager.MessageProcessExited += DbgManager_MessageProcessExited;
			dbgManager.MessageModuleLoaded += DbgManager_MessageModuleLoaded;
			dbgManager.MessageModuleUnloaded += DbgManager_MessageModuleUnloaded;
			dbgManager.MessageThreadExited += DbgManager_MessageThreadExited;
			dbgManager.MessageExceptionThrown += DbgManager_MessageExceptionThrown;
			dbgManager.MessageProgramMessage += DbgManager_MessageProgramMessage;
			dbgManager.MessageAsyncProgramMessage += DbgManager_MessageAsyncProgramMessage;
			dbgManager.DbgManagerMessage += DbgManager_DbgManagerMessage;
		}

		void DbgManager_DbgManagerMessage(object? sender, DbgManagerMessageEventArgs e) {
			if (e.MessageKind == PredefinedDbgManagerMessageKinds.Output)
				UI(() => WriteLine_UI(BoxedTextColor.DebugLogExtensionMessage, e.Message));
			else if (e.MessageKind == PredefinedDbgManagerMessageKinds.StepFilter && outputLoggerSettings.ShowStepFilteringMessages)
				UI(() => WriteLine_UI(BoxedTextColor.DebugLogStepFiltering, e.Message));
		}

		void DbgManager_IsDebuggingChanged(object? sender, EventArgs e) {
			var dbgManager = (DbgManager)sender!;
			if (dbgManager.IsDebugging) {
				UI(() => {
					Initialize_UI();
					Debug2.Assert(textPane is not null);
					if (outputLoggerSettings.ShowDebugOutputLog)
						outputService.Value.Select(GUID_OUTPUT_LOGGER_DEBUG);
					textPane.Clear();
				});
			}
		}

		void WriteLine_UI(object color, string text) {
			uiDispatcher.VerifyAccess();
			if (textPane is null)
				Initialize_UI();
			Debug2.Assert(textPane is not null);
			textPane.WriteLine(color, text);
		}

		void Write_UI(object color, string text) {
			uiDispatcher.VerifyAccess();
			if (textPane is null)
				Initialize_UI();
			Debug2.Assert(textPane is not null);
			textPane.Write(color, text);
		}

		void ITracepointMessageListener.Message(string message) =>
			UI(() => WriteLine_UI(BoxedTextColor.DebugLogTrace, message));

		void DbgManager_MessageProcessExited(object? sender, DbgMessageProcessExitedEventArgs e) {
			if (outputLoggerSettings.ShowProcessExitMessages) {
				var msg = string.Format(dnSpy_Debugger_Resources.DebugLogExitProcess, GetProcessNameWithPID(e.Process), e.ExitCode);
				UI(() => WriteLine_UI(BoxedTextColor.DebugLogExitProcess, msg));
			}
		}

		void DbgManager_MessageModuleLoaded(object? sender, DbgMessageModuleLoadedEventArgs e) {
			if (outputLoggerSettings.ShowModuleLoadMessages) {
				var module = e.Module;
				var msg = GetProcessName(module.Process) + " (" + GetRuntimeAppDomainName(module) + "): " +
						string.Format(dnSpy_Debugger_Resources.DebugLogLoadModule, module.Filename);
				UI(() => WriteLine_UI(BoxedTextColor.DebugLogLoadModule, msg));
			}
		}

		void DbgManager_MessageModuleUnloaded(object? sender, DbgMessageModuleUnloadedEventArgs e) {
			if (outputLoggerSettings.ShowModuleUnloadMessages) {
				var module = e.Module;
				var msg = GetProcessName(module.Process) + " (" + GetRuntimeAppDomainName(module) + "): " +
						string.Format(dnSpy_Debugger_Resources.DebugLogUnloadModule, module.Filename);
				UI(() => WriteLine_UI(BoxedTextColor.DebugLogUnloadModule, msg));
			}
		}

		void DbgManager_MessageThreadExited(object? sender, DbgMessageThreadExitedEventArgs e) {
			if (outputLoggerSettings.ShowThreadExitMessages) {
				var msg = string.Format(dnSpy_Debugger_Resources.DebugLogExitThread, e.Thread.Id, e.ExitCode);
				UI(() => WriteLine_UI(BoxedTextColor.DebugLogExitThread, msg));
			}
		}

		void DbgManager_MessageExceptionThrown(object? sender, DbgMessageExceptionThrownEventArgs e) {
			var ex = e.Exception;
			if (ex.Id.Category == PredefinedExceptionCategories.MDA) {
				if (outputLoggerSettings.ShowMDAMessages) {
					var process = ex.Process;
					var msg1 = string.Format(dnSpy_Debugger_Resources.DebugLogMDA, GetExceptionName(ex.Id), GetProcessFullPath(process));
					var msg2 = ex.Message is null ? null : string.Format(dnSpy_Debugger_Resources.DebugLogAdditionalInformation, ex.Message);
					UI(() => {
						WriteLine_UI(BoxedTextColor.DebugLogMDA, msg1);
						if (msg2 is not null)
							WriteLine_UI(BoxedTextColor.DebugLogMDA, msg2);
					});
				}
			}
			else {
				if (outputLoggerSettings.ShowExceptionMessages) {
					if (ex.IsFirstChance) {
						var msg1 = string.Format(dnSpy_Debugger_Resources.DebugLogExceptionHandled, GetExceptionName(ex.Id), GetModuleName(ex.Module));
						var msg2 = ex.Message is null ? null : string.Format(dnSpy_Debugger_Resources.DebugLogAdditionalInformation, FilterUserMessage(ex.Message));
						UI(() => {
							WriteLine_UI(BoxedTextColor.DebugLogExceptionHandled, msg1);
							if (msg2 is not null)
								WriteLine_UI(BoxedTextColor.DebugLogExceptionHandled, msg2);
						});
					}
					else if (ex.IsUnhandled) {
						var msg1 = string.Format(dnSpy_Debugger_Resources.DebugLogExceptionUnhandled, GetExceptionName(ex.Id), GetModuleName(ex.Module));
						var msg2 = ex.Message is null ? null : string.Format(dnSpy_Debugger_Resources.DebugLogAdditionalInformation, FilterUserMessage(ex.Message));
						UI(() => {
							WriteLine_UI(BoxedTextColor.DebugLogExceptionUnhandled, msg1);
							if (msg2 is not null)
								WriteLine_UI(BoxedTextColor.DebugLogExceptionUnhandled, msg2);
						});
					}
				}
			}
		}

		void DbgManager_MessageProgramMessage(object? sender, DbgMessageProgramMessageEventArgs e) {
			if (outputLoggerSettings.ShowProgramOutputMessages) {
				var msg = FilterUserMessage(e.Message);
				UI(() => Write_UI(BoxedTextColor.DebugLogProgramOutput, msg));
			}
		}

		void DbgManager_MessageAsyncProgramMessage(object? sender, DbgMessageAsyncProgramMessageEventArgs e) {
			if (outputLoggerSettings.ShowProgramOutputMessages) {
				var msg = FilterUserMessage(e.Message);
				UI(() => Write_UI(BoxedTextColor.DebugLogProgramOutput, msg));
			}
		}

		string GetRuntimeAppDomainName(DbgModule module) {
			var runtime = module.Runtime;
			var appDomain = module.AppDomain;
			if (appDomain is null)
				return runtime.Name;
			return runtime.Name + ": " + appDomain.Name;
		}

		string GetExceptionName(DbgExceptionId id) => dbgExceptionFormatterService.Value.ToString(id);

		string GetProcessName(DbgProcess? process) {
			if (process is null)
				return "???";
			var name = process.Name;
			if (string.IsNullOrEmpty(name))
				return "0x" + process.Id.ToString("X");
			return name;
		}

		string GetProcessFullPath(DbgProcess process) {
			if (process is null)
				return "???";
			var filename = process.Filename;
			if (string.IsNullOrEmpty(filename))
				return "0x" + process.Id.ToString("X");
			return filename;
		}

		string GetProcessNameWithPID(DbgProcess? process) => $"[0x{process?.Id ?? -1:X}] {GetProcessName(process)}";
		string GetModuleName(DbgModule? module) => module?.Name ?? "???";

		string FilterUserMessage(string s) {
			if (s is null)
				return string.Empty;
			const int MAX_USER_MSG_LEN = 100 * 1024;
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
	}
}
