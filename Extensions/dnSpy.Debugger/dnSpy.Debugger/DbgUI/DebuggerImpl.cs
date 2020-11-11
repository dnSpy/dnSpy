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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Attach.Dialogs;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Debugger.StartDebugging.Dialog;
using dnSpy.Contracts.Debugger.Steppers;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Debugger.Breakpoints.Code.TextEditor;
using dnSpy.Debugger.Code.TextEditor;
using dnSpy.Debugger.Disassembly;
using dnSpy.Debugger.Exceptions;
using dnSpy.Debugger.Native;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.DbgUI {
	[Export(typeof(Debugger))]
	[Export(typeof(IDbgManagerStartListener))]
	sealed class DebuggerImpl : Debugger, IDbgManagerStartListener {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IMessageBoxService> messageBoxService;
		readonly Lazy<IAppWindow> appWindow;
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<StartDebuggingOptionsProvider> startDebuggingOptionsProvider;
		readonly Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog;
		readonly Lazy<TextViewBreakpointService> textViewBreakpointService;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgCallStackService> dbgCallStackService;
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;
		readonly Lazy<DbgTextViewCodeLocationService> dbgTextViewCodeLocationService;
		readonly Lazy<DbgExceptionFormatterService> dbgExceptionFormatterService;
		readonly Lazy<DbgShowNativeCodeService> dbgShowNativeCodeService;
		readonly DebuggerSettings debuggerSettings;

		public override bool IsDebugging => dbgManager.Value.IsDebugging;

		[ImportingConstructor]
		DebuggerImpl(UIDispatcher uiDispatcher, Lazy<IMessageBoxService> messageBoxService, Lazy<IAppWindow> appWindow, Lazy<IDocumentTabService> documentTabService, Lazy<DbgManager> dbgManager, Lazy<StartDebuggingOptionsProvider> startDebuggingOptionsProvider, Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog, Lazy<TextViewBreakpointService> textViewBreakpointService, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgCallStackService> dbgCallStackService, Lazy<ReferenceNavigatorService> referenceNavigatorService, Lazy<DbgTextViewCodeLocationService> dbgTextViewCodeLocationService, Lazy<DbgExceptionFormatterService> dbgExceptionFormatterService, Lazy<DbgShowNativeCodeService> dbgShowNativeCodeService, DebuggerSettings debuggerSettings) {
			this.uiDispatcher = uiDispatcher;
			this.messageBoxService = messageBoxService;
			this.appWindow = appWindow;
			this.documentTabService = documentTabService;
			this.dbgManager = dbgManager;
			this.startDebuggingOptionsProvider = startDebuggingOptionsProvider;
			this.showAttachToProcessDialog = showAttachToProcessDialog;
			this.textViewBreakpointService = textViewBreakpointService;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgCallStackService = dbgCallStackService;
			this.referenceNavigatorService = referenceNavigatorService;
			this.dbgTextViewCodeLocationService = dbgTextViewCodeLocationService;
			this.dbgExceptionFormatterService = dbgExceptionFormatterService;
			this.dbgShowNativeCodeService = dbgShowNativeCodeService;
			this.debuggerSettings = debuggerSettings;
			UI(() => appWindow.Value.MainWindowClosing += AppWindow_MainWindowClosing);
		}

		void AppWindow_MainWindowClosing(object? sender, CancelEventArgs e) {
			if (IsDebugging) {
				var result = messageBoxService.Value.ShowIgnorableMessage(new Guid("B4B8E13C-B7B7-490A-953B-8ED8EAE7C170"), dnSpy_Debugger_Resources.AskAppWindowClosingStopDebugging, MsgBoxButton.Yes | MsgBoxButton.No);
				if (result == MsgBoxButton.None || result == MsgBoxButton.No)
					e.Cancel = true;
			}
		}

		public override string? GetCurrentExecutableFilename() => startDebuggingOptionsProvider.Value.GetCurrentExecutableFilename();

		public override bool CanStartWithoutDebugging => startDebuggingOptionsProvider.Value.CanStartWithoutDebugging(out _);
		public override void StartWithoutDebugging() {
			if (!startDebuggingOptionsProvider.Value.CanStartWithoutDebugging(out var result))
				return;
			if ((result & StartDebuggingResult.WrongExtension) != 0) {
				if (messageBoxService.Value.Show(dnSpy_Debugger_Resources.RunWithInvalidExtension, MsgBoxButton.Yes | MsgBoxButton.No) != MsgBoxButton.Yes)
					return;
			}

			if (!startDebuggingOptionsProvider.Value.StartWithoutDebugging(out var error))
				messageBoxService.Value.Show(error);
		}

		public override bool CanDebugProgram => !showingDebugProgramDlgBox;
		public override void DebugProgram(bool pauseAtEntryPoint) {
			if (!CanDebugProgram)
				return;
			var breakKind = pauseAtEntryPoint ? PredefinedBreakKinds.EntryPoint : null;
			showingDebugProgramDlgBox = true;
			var (options, flags) = startDebuggingOptionsProvider.Value.GetStartDebuggingOptions(breakKind);
			showingDebugProgramDlgBox = false;
			if (options is null)
				return;
			if ((flags & StartDebuggingOptionsInfoFlags.WrongExtension) != 0) {
				if (messageBoxService.Value.Show(dnSpy_Debugger_Resources.DebugWithInvalidExtension, MsgBoxButton.Yes | MsgBoxButton.No) != MsgBoxButton.Yes)
					return;
			}

			var errMsg = dbgManager.Value.Start(options);
			if (errMsg is not null)
				messageBoxService.Value.Show(errMsg);
		}
		bool showingDebugProgramDlgBox;

		public override bool CanAttachProgram => true;
		public override void AttachProgram() => showAttachToProcessDialog.Value.Attach();

		bool CanExecutePauseCommand => dbgManager.Value.IsDebugging && dbgManager.Value.IsRunning != true;
		bool CanStepCommand => dbgManager.Value.CurrentThread.Current?.Process?.State == DbgProcessState.Paused;
		bool CanStepProcessCommand => CanStepCommand && !debuggerSettings.BreakAllProcesses;
		bool CanExecuteRunningCommand => dbgManager.Value.IsDebugging && dbgManager.Value.IsRunning != false;
		bool CanExecutePauseOrRunningCommand => dbgManager.Value.IsDebugging;

		public override bool CanContinue => CanExecutePauseCommand;
		public override void Continue() => dbgManager.Value.RunAll();
		public override bool CanBreakAll => CanExecuteRunningCommand;
		public override void BreakAll() => dbgManager.Value.BreakAll();
		public override bool CanStopDebugging => CanExecutePauseOrRunningCommand;
		public override void StopDebugging() => dbgManager.Value.StopDebuggingAll();

		public override bool CanDetachAll => CanExecutePauseOrRunningCommand;
		public override void DetachAll() {
			if (!dbgManager.Value.CanDetachWithoutTerminating) {
				//TODO: Show a msg box
			}
			dbgManager.Value.DetachAll();
		}

		public override bool CanTerminateAll => CanExecutePauseOrRunningCommand;
		public override void TerminateAll() => dbgManager.Value.TerminateAll();
		public override bool CanRestart => CanExecutePauseOrRunningCommand && dbgManager.Value.CanRestart;
		public override void Restart() => dbgManager.Value.Restart();

		public override bool CanShowNextStatement => CanExecutePauseCommand;
		public override void ShowNextStatement() {
			var info = GetCurrentStatementLocation();
			if (info.location is not null) {
				referenceNavigatorService.Value.GoTo(info.location);
				dbgCallStackService.Value.ActiveFrameIndex = info.frameIndex;
			}
		}

		(DbgCodeLocation? location, int frameIndex) GetCurrentStatementLocation() {
			var frames = dbgCallStackService.Value.Frames.Frames;
			for (int i = 0; i < frames.Count; i++) {
				var location = frames[i].Location;
				if (location is not null)
					return (location, i);
			}
			return (null, -1);
		}

		public override bool CanSetNextStatement {
			get {
				if (!CanExecutePauseCommand || dbgManager.Value.CurrentThread.Current is null)
					return false;
				using (var res = GetCurrentTextViewStatementLocation())
					return res.Location is not null;
			}
		}

		public override void SetNextStatement() {
			if (!CanSetNextStatement)
				return;
			using (var res = GetCurrentTextViewStatementLocation()) {
				if (res.Location is not null)
					dbgManager.Value.CurrentThread.Current?.SetIP(res.Location);
			}
		}

		readonly struct TextViewStatementLocationResult : IDisposable {
			readonly Lazy<DbgManager> dbgManager;
			readonly List<DbgCodeLocation> allLocations;

			public DbgCodeLocation? Location { get; }

			public TextViewStatementLocationResult(Lazy<DbgManager> dbgManager, List<DbgCodeLocation> allLocations, DbgCodeLocation? location) {
				this.dbgManager = dbgManager;
				this.allLocations = allLocations;
				Location = location;
			}

			public void Dispose() {
				if (allLocations is not null && allLocations.Count > 0)
					dbgManager.Value.Close(allLocations);
			}
		}

		TextViewStatementLocationResult GetCurrentTextViewStatementLocation() {
			var tab = documentTabService.Value.ActiveTab;
			if (tab is null)
				return default;
			var documentViewer = tab.TryGetDocumentViewer();
			if (documentViewer is null)
				return default;
			var textView = documentViewer.TextView;

			var allLocations = new List<DbgCodeLocation>();
			foreach (var res in dbgTextViewCodeLocationService.Value.CreateLocation(tab, textView, textView.Caret.Position.VirtualBufferPosition)) {
				allLocations.AddRange(res.Locations);
				if (res.Locations.Length != 0) {
					var location = res.Locations[0];
					if (res.Locations.Length > 1) {
						var thread = dbgManager.Value.CurrentThread.Current;
						foreach (var loc in res.Locations) {
							if (thread?.CanSetIP(loc) == true) {
								location = loc;
								break;
							}
						}
					}
					return new TextViewStatementLocationResult(dbgManager, allLocations, location);
				}
			}

			return new TextViewStatementLocationResult(dbgManager, allLocations, null);
		}

		public override bool CanStepInto => CanStepCommand;
		public override void StepInto() => Step(DbgStepKind.StepInto);

		public override bool CanStepOver => CanStepCommand;
		public override void StepOver() => Step(DbgStepKind.StepOver);

		public override bool CanStepOut => CanStepCommand;
		public override void StepOut() => Step(DbgStepKind.StepOut);

		public override bool CanStepIntoCurrentProcess => CanStepProcessCommand;
		public override void StepIntoCurrentProcess() => Step(DbgStepKind.StepIntoProcess);

		public override bool CanStepOverCurrentProcess => CanStepProcessCommand;
		public override void StepOverCurrentProcess() => Step(DbgStepKind.StepOverProcess);

		public override bool CanStepOutCurrentProcess => CanStepProcessCommand;
		public override void StepOutCurrentProcess() => Step(DbgStepKind.StepOutProcess);

		sealed class StepperState : IDisposable {
			public DbgStepper? ActiveStepper;

			public void SetStepper(DbgStepper stepper) {
				var oldStepper = ActiveStepper;
				ActiveStepper = stepper;
				oldStepper?.Cancel();
				oldStepper?.Close();
			}

			public void Cancel(DbgStepper oldStepper) {
				if (oldStepper == ActiveStepper) {
					var stepper = ActiveStepper;
					ActiveStepper = null;
					stepper?.Cancel();
					stepper?.Close();
				}
			}

			public void Dispose() {
				ActiveStepper?.Close();
				ActiveStepper = null;
			}
		}

		void Step(DbgStepKind step) {
			var thread = dbgManager.Value.CurrentThread.Current;
			if (thread is null)
				return;

			var state = thread.Runtime.GetOrCreateData<StepperState>();
			var stepper = thread.CreateStepper();
			// This cancels the old stepper, if any. An old stepper could still be active if
			// we eg. step out, but hit a BP before we step out, and then we step again.
			state.SetStepper(stepper);
			stepper.Closed += (s, e) => UI(() => state.Cancel(stepper));
			stepper.Step(step, autoClose: true);
		}

		public override bool CanGoToDisassembly {
			get {
				if (!CanExecutePauseCommand || dbgManager.Value.CurrentThread.Current is null)
					return false;
				using (var res = GetCurrentTextViewStatementLocation()) {
					if (res.Location is not null) {
						foreach (var runtime in GetRuntimes()) {
							if (dbgShowNativeCodeService.Value.CanShowNativeCode(runtime, res.Location))
								return true;
						}
					}
				}
				return false;
			}
		}

		public override void GoToDisassembly() {
			if (!CanGoToDisassembly)
				return;
			using (var res = GetCurrentTextViewStatementLocation()) {
				if (res.Location is not null) {
					foreach (var runtime in GetRuntimes()) {
						if (dbgShowNativeCodeService.Value.CanShowNativeCode(runtime, res.Location)) {
							if (!dbgShowNativeCodeService.Value.ShowNativeCode(runtime, res.Location))
								messageBoxService.Value.Show(dnSpy_Debugger_Resources.Error_CouldNotShowDisassembly);
							break;
						}
					}
				}
			}
		}
		IEnumerable<DbgRuntime> GetRuntimes() {
			var currentRuntime = dbgManager.Value.CurrentRuntime.Current;
			if (currentRuntime is not null)
				yield return currentRuntime;
			foreach (var process in dbgManager.Value.Processes) {
				foreach (var runtime in process.Runtimes) {
					if (runtime != currentRuntime)
						yield return runtime;
				}
			}
		}

		public override bool CanToggleCreateBreakpoint => textViewBreakpointService.Value.CanToggleCreateBreakpoint;
		public override void ToggleCreateBreakpoint() => textViewBreakpointService.Value.ToggleCreateBreakpoint();
		public override ToggleCreateBreakpointKind GetToggleCreateBreakpointKind() => textViewBreakpointService.Value.GetToggleCreateBreakpointKind();

		public override bool CanToggleEnableBreakpoint => textViewBreakpointService.Value.CanToggleEnableBreakpoint;
		public override void ToggleEnableBreakpoint() => textViewBreakpointService.Value.ToggleEnableBreakpoint();
		public override ToggleEnableBreakpointKind GetToggleEnableBreakpointKind() => textViewBreakpointService.Value.GetToggleEnableBreakpointKind();

		public override bool CanDeleteAllBreakpoints => dbgCodeBreakpointsService.Value.VisibleBreakpoints.Any();
		public override void DeleteAllBreakpointsAskUser() {
			var res = messageBoxService.Value.ShowIgnorableMessage(new Guid("37250D26-E844-49F4-904B-29600B90476C"), dnSpy_Debugger_Resources.AskDeleteAllBreakpoints, MsgBoxButton.Yes | MsgBoxButton.No);
			if (res is not null && res != MsgBoxButton.Yes)
				return;
			dbgCodeBreakpointsService.Value.Clear();
		}

		public override bool CanEnableAllBreakpoints => dbgCodeBreakpointsService.Value.VisibleBreakpoints.Any(a => !a.IsEnabled);
		public override void EnableAllBreakpoints() => EnableAllBreakpoints(true);
		public override bool CanDisableAllBreakpoints => dbgCodeBreakpointsService.Value.VisibleBreakpoints.Any(a => a.IsEnabled);
		public override void DisableAllBreakpoints() => EnableAllBreakpoints(false);

		void EnableAllBreakpoints(bool enable) {
			dbgCodeBreakpointsService.Value.Modify(dbgCodeBreakpointsService.Value.VisibleBreakpoints.Where(a => a.IsEnabled != enable).Select(a => {
				var s = a.Settings;
				s.IsEnabled = enable;
				return new DbgCodeBreakpointAndSettings(a, s);
			}).ToArray());
		}

		public override bool CanContinueOrDebugProgram => CanContinue || CanDebugProgram;
		public override void ContinueOrDebugProgram() {
			if (CanContinue)
				Continue();
			else if (CanDebugProgram && !dbgManager.Value.IsDebugging)
				DebugProgram(pauseAtEntryPoint: false);
		}

		public override bool CanStepIntoOrDebugProgram => CanStepInto || CanDebugProgram;
		public override void StepIntoOrDebugProgram() {
			if (CanStepInto)
				StepInto();
			else if (CanDebugProgram && !dbgManager.Value.IsDebugging)
				DebugProgram(pauseAtEntryPoint: true);
		}

		public override bool CanStepOverOrDebugProgram => CanStepOver || CanDebugProgram;
		public override void StepOverOrDebugProgram() {
			if (CanStepOver)
				StepOver();
			else if (CanDebugProgram && !dbgManager.Value.IsDebugging)
				DebugProgram(pauseAtEntryPoint: true);
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
			dbgManager.IsRunningChanged += DbgManager_IsRunningChanged;
			dbgManager.MessageSetIPComplete += DbgManager_MessageSetIPComplete;
			dbgManager.MessageUserMessage += DbgManager_MessageUserMessage;
			dbgManager.MessageExceptionThrown += DbgManager_MessageExceptionThrown;
			dbgManager.DbgManagerMessage += DbgManager_DbgManagerMessage;
		}

		void DbgManager_DbgManagerMessage(object? sender, DbgManagerMessageEventArgs e) {
			switch (e.MessageKind) {
			case PredefinedDbgManagerMessageKinds.ErrorUser:
				UI(() => ShowError_UI(e.Message));
				break;
			}
		}

		void DbgManager_MessageSetIPComplete(object? sender, DbgMessageSetIPCompleteEventArgs e) {
			if (e.Error is not null)
				UI(() => ShowError_UI(e.Error));
		}

		void DbgManager_MessageUserMessage(object? sender, DbgMessageUserMessageEventArgs e) =>
			UI(() => ShowError_UI(e.Message));

		void DbgManager_MessageExceptionThrown(object? sender, DbgMessageExceptionThrownEventArgs e) {
			if (!debuggerSettings.IgnoreUnhandledExceptions && e.Exception.IsUnhandled) {
				e.Pause = true;
				UI(() => ShowUnhandledException_UI(e));
			}
		}

		void UI(Action callback) => uiDispatcher.UI(callback);

		void ShowUnhandledException_UI(DbgMessageExceptionThrownEventArgs exm) {
			var sb = new StringBuilder();
			sb.AppendLine(string.Format(dnSpy_Debugger_Resources.UnhandledExceptionMessage_ProcessName_ProcessId, exm.Exception.Process.Name, exm.Exception.Process.Id));
			sb.AppendLine();
			sb.AppendLine(string.Format(dnSpy_Debugger_Resources.ExceptionName, dbgExceptionFormatterService.Value.ToString(exm.Exception.Id)));
			sb.AppendLine();
			sb.AppendLine(string.Format(dnSpy_Debugger_Resources.ExceptionMessage, exm.Exception.Message ?? dnSpy_Debugger_Resources.ExceptionMessageIsNull));
			ShowError_UI(sb.ToString());
		}

		void ActivateWindow_UI() {
			NativeMethods.SetForegroundWindow(new WindowInteropHelper(appWindow.Value.MainWindow).Handle);
			NativeMethods.SetWindowPos(new WindowInteropHelper(appWindow.Value.MainWindow).Handle, IntPtr.Zero, 0, 0, 0, 0, 3);
			appWindow.Value.MainWindow.Activate();
		}

		void ShowError_UI(string error) {
			ActivateWindow_UI();
			messageBoxService.Value.Show(error);
		}

		void DbgManager_IsDebuggingChanged(object? sender, EventArgs e) {
			var dbgManager = (DbgManager)sender!;
			UI(() => {
				var newIsDebugging = dbgManager.IsDebugging;
				if (newIsDebugging == oldIsDebugging)
					return;
				oldIsDebugging = newIsDebugging;
				Application.Current.Resources["IsDebuggingKey"] = newIsDebugging;
				if (newIsDebugging) {
					appWindow.Value.StatusBar.Open();
					SetRunningStatusMessage_UI();
					appWindow.Value.AddTitleInfo(dnSpy_Debugger_Resources.AppTitle_Debugging);
				}
				else {
					appWindow.Value.StatusBar.Close();
					appWindow.Value.RemoveTitleInfo(dnSpy_Debugger_Resources.AppTitle_Debugging);
				}
				appWindow.Value.RefreshToolBar();
			});
		}
		bool oldIsDebugging;

		void SetRunningStatusMessage_UI() => SetStatusMessage_UI(dnSpy_Debugger_Resources.StatusBar_Running);
		void SetStatusMessage_UI(string message) => appWindow.Value.StatusBar.Show(message);

		void DbgManager_IsRunningChanged(object? sender, EventArgs e) {
			var dbgManager = (DbgManager)sender!;
			var breakInfos = dbgManager.CurrentRuntime.Break?.BreakInfos ?? (IList<DbgBreakInfo>)Array.Empty<DbgBreakInfo>();
			UI(() => {
				var newIsRunning = dbgManager.IsRunning;
				if (newIsRunning == oldIsRunning)
					return;
				oldIsRunning = newIsRunning;
				SetStatusMessage_UI(GetStatusBarMessage(breakInfos));
				CommandManager.InvalidateRequerySuggested();
			});
		}
		bool? oldIsRunning;

		static string GetProcessName(DbgProcess process) {
			var processName = process.Name;
			if (string.IsNullOrEmpty(processName))
				processName = "???";
			return processName;
		}

		static string GetExceptionName(DbgException ex) {
			var id = ex.Id;
			if (id.HasCode)
				return id.ToString();
			if (id.HasName)
				return id.Name!;
			return "???";
		}

		static string GetStatusBarMessage(IList<DbgBreakInfo> breakInfos) {
			if (breakInfos.Count == 0)
				return dnSpy_Debugger_Resources.StatusBar_Running;

			var info = GetBreakInfo(breakInfos);
			DbgModule? module;
			switch (info.Kind) {
			case DbgBreakInfoKind.Message:
				var e = (DbgMessageEventArgs)info.Data!;
				switch (e.Kind) {
				case DbgMessageKind.ModuleLoaded:
					module = ((DbgMessageModuleLoadedEventArgs)e).Module;
					if (module.IsDynamic || module.IsInMemory)
						return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_LoadModule1, module.IsDynamic ? 1 : 0, module.IsInMemory ? 1 : 0, module.Address, module.Size, module.Name);
					return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_LoadModule2, module.Address, module.Size, module.Name);

				case DbgMessageKind.ExceptionThrown:
					var ex = ((DbgMessageExceptionThrownEventArgs)e).Exception;
					var exMsg = ex.IsUnhandled ? dnSpy_Debugger_Resources.Debug_EventDescription_UnhandledException : dnSpy_Debugger_Resources.Debug_EventDescription_Exception;
					exMsg += $" : pid={ex.Process.Id}({GetProcessName(ex.Process)}), {GetExceptionName(ex)}";
					if (!string.IsNullOrEmpty(ex.Message))
						exMsg += $" : {ex.Message}";
					return exMsg;

				case DbgMessageKind.BoundBreakpoint:
					var bbe = (DbgMessageBoundBreakpointEventArgs)e;
					var bpMsg = $"{dnSpy_Debugger_Resources.StatusBar_BreakpointHit} #{bbe.BoundBreakpoint.Breakpoint.Id} : pid={bbe.BoundBreakpoint.Process.Id}({GetProcessName(bbe.BoundBreakpoint.Process)})";
					module = bbe.BoundBreakpoint.Module;
					if (module is not null)
						bpMsg += $", {module.Name}";
					if (bbe.BoundBreakpoint.HasAddress)
						bpMsg += $", 0x{bbe.BoundBreakpoint.Address.ToString("X")}";
					//TODO: show bbe.BoundBreakpoint.Breakpoint.Location
					return bpMsg;

				case DbgMessageKind.ProcessCreated:
				case DbgMessageKind.ProcessExited:
				case DbgMessageKind.RuntimeCreated:
				case DbgMessageKind.RuntimeExited:
				case DbgMessageKind.AppDomainLoaded:
				case DbgMessageKind.AppDomainUnloaded:
				case DbgMessageKind.ModuleUnloaded:
				case DbgMessageKind.ThreadCreated:
				case DbgMessageKind.ThreadExited:
				case DbgMessageKind.EntryPointBreak:
				case DbgMessageKind.ProgramMessage:
				case DbgMessageKind.ProgramBreak:
				case DbgMessageKind.StepComplete:
				case DbgMessageKind.SetIPComplete:
				case DbgMessageKind.UserMessage:
				case DbgMessageKind.Break:
					break;

				default:
					Debug.Fail($"Unknown kind: {e.Kind}");
					break;
				}
				break;

			case DbgBreakInfoKind.Unknown:
			case DbgBreakInfoKind.Connected:
			default:
				break;
			}

			return dnSpy_Debugger_Resources.StatusBar_Ready;
		}

		static DbgBreakInfo GetBreakInfo(IList<DbgBreakInfo> breakInfos) {
			if (breakInfos.Count == 0)
				throw new InvalidOperationException();
			DbgBreakInfo result = default;
			int resultPrio = int.MaxValue;
			foreach (var info in breakInfos) {
				int prio = GetPriority(info);
				if (prio < resultPrio) {
					resultPrio = prio;
					result = info;
				}
			}
			return result;
		}

		static int GetPriority(DbgBreakInfo info) {
			const int defaultPrio = int.MaxValue - 1;
			if (info.Kind == DbgBreakInfoKind.Message) {
				var e = (DbgMessageEventArgs)info.Data!;
				switch (e.Kind) {
				case DbgMessageKind.ExceptionThrown:
					return 0;

				case DbgMessageKind.EntryPointBreak:
					return 1;

				case DbgMessageKind.BoundBreakpoint:
					return 2;

				case DbgMessageKind.ProgramBreak:
					return 3;

				case DbgMessageKind.StepComplete:
					return 4;

				case DbgMessageKind.ProcessCreated:
				case DbgMessageKind.ProcessExited:
				case DbgMessageKind.RuntimeCreated:
				case DbgMessageKind.RuntimeExited:
				case DbgMessageKind.AppDomainLoaded:
				case DbgMessageKind.AppDomainUnloaded:
				case DbgMessageKind.ModuleLoaded:
				case DbgMessageKind.ModuleUnloaded:
				case DbgMessageKind.ThreadCreated:
				case DbgMessageKind.ThreadExited:
				case DbgMessageKind.ProgramMessage:
				case DbgMessageKind.UserMessage:
				case DbgMessageKind.Break:
					return defaultPrio - 1;

				case DbgMessageKind.SetIPComplete:
					return defaultPrio;

				default:
					Debug.Fail($"Unknown kind: {e.Kind}");
					return defaultPrio;
				}
			}
			return defaultPrio;
		}
	}
}
