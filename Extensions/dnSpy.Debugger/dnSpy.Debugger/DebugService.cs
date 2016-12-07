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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.CallStack;
using dnSpy.Debugger.Dialogs;
using dnSpy.Debugger.IMModules;
using dnSpy.Debugger.Properties;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger {
	interface IDebugService {
		IDsDocument GetCurrentExecutableAssembly(IMenuItemContext context);
		IDebuggerSettings DebuggerSettings { get; }
		bool DebugAssembly();
		bool DebugAssembly(DebugProcessOptions options);
		bool DebugCoreCLRAssembly();
		bool Attach();
		bool Attach(AttachProcessOptions options);
		void Restart();
		void Break();
		void Stop();
		void Detach();
		void Continue();
		void StepOver();
		void StepOver(CorFrame frame);
		void StepInto();
		void StepInto(CorFrame frame);
		void StepOut();
		void StepOut(CorFrame frame);
		bool CanRunTo(CorFrame frame);
		bool RunTo(CorFrame frame);
		bool SetOffset(uint ilOffset, out string errMsg);
		bool SetOffset(CorFrame frame, uint ilOffset, out string errMsg);
		bool SetNativeOffset(uint ilOffset, out string errMsg);
		bool SetNativeOffset(CorFrame frame, uint ilOffset, out string errMsg);
		bool HasAttached { get; }
		bool IsEvaluating { get; }
		bool EvalCompleted { get; }
		IStackFrameService StackFrameService { get; }
	}

	[ExportDocumentListListener]
	sealed class DebugServiceFileListListener : IDocumentListListener {
		public bool CanLoad => !debugService.Value.TheDebugger.IsDebugging;
		public bool CanReload => !debugService.Value.TheDebugger.IsDebugging;

		readonly Lazy<DebugService> debugService;

		[ImportingConstructor]
		DebugServiceFileListListener(Lazy<DebugService> debugService) {
			this.debugService = debugService;
		}

		public void AfterLoad(bool isReload) { }
		public void BeforeLoad(bool isReload) { }
		public bool CheckCanLoad(bool isReload) => true;
	}

	[Export, Export(typeof(IDebugService))]
	sealed class DebugService : IDebugService {
		readonly IAppWindow appWindow;
		readonly IDocumentTabService documentTabService;
		readonly IMessageBoxService messageBoxService;
		readonly ITheDebugger theDebugger;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly Lazy<IInMemoryModuleService> inMemoryModuleService;
		readonly IModuleIdProvider moduleIdProvider;

		public ITheDebugger TheDebugger => theDebugger;
		public IStackFrameService StackFrameService { get; }
		public IDebuggerSettings DebuggerSettings { get; }

		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly ITextElementProvider textElementProvider;

		[ImportingConstructor]
		DebugService(IAppWindow appWindow, IDocumentTabService documentTabService, IMessageBoxService messageBoxService, IDebuggerSettings debuggerSettings, ITheDebugger theDebugger, IStackFrameService stackFrameService, Lazy<IModuleLoader> moduleLoader, Lazy<IInMemoryModuleService> inMemoryModuleService, IModuleIdProvider moduleIdProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			this.appWindow = appWindow;
			this.documentTabService = documentTabService;
			this.messageBoxService = messageBoxService;
			DebuggerSettings = debuggerSettings;
			this.theDebugger = theDebugger;
			StackFrameService = stackFrameService;
			this.moduleLoader = moduleLoader;
			this.inMemoryModuleService = inMemoryModuleService;
			this.moduleIdProvider = moduleIdProvider;
			this.classificationFormatMapService = classificationFormatMapService;
			this.textElementProvider = textElementProvider;
			stackFrameService.PropertyChanged += StackFrameService_PropertyChanged;
			theDebugger.ProcessRunning += TheDebugger_ProcessRunning;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			appWindow.MainWindowClosing += AppWindow_MainWindowClosing;
			debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
		}

		void StackFrameService_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(IStackFrameService.SelectedThread))
				UpdateCurrentLocation(StackFrameService.FirstILFrame);
		}

		void TheDebugger_ProcessRunning(object sender, EventArgs e) {
			try {
				var e2 = (DebuggedProcessRunningEventArgs)e;
				var hWnd = e2.Process.MainWindowHandle;
				if (hWnd != IntPtr.Zero)
					NativeMethods.SetForegroundWindow(hWnd);
			}
			catch {
			}
		}

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(IDebuggerSettings.IgnoreBreakInstructions)) {
				if (TheDebugger.Debugger != null)
					TheDebugger.Debugger.Options.IgnoreBreakInstructions = DebuggerSettings.IgnoreBreakInstructions;
			}
			else if (e.PropertyName == nameof(IDebuggerSettings.UseMemoryModules)) {
				if (ProcessState != DebuggerProcessState.Terminated && DebuggerSettings.UseMemoryModules)
					UpdateCurrentLocationToInMemoryModule();
			}
		}

		public DebuggerProcessState ProcessState => TheDebugger.ProcessState;
		public bool IsDebugging => ProcessState != DebuggerProcessState.Terminated;

		/// <summary>
		/// true if we've attached to a process
		/// </summary>
		public bool HasAttached => IsDebugging && TheDebugger.Debugger.HasAttached;
		public bool IsEvaluating => IsDebugging && TheDebugger.Debugger.IsEvaluating;
		public bool EvalCompleted => IsDebugging && TheDebugger.Debugger.EvalCompleted;

		void SetRunningStatusMessage() => appWindow.StatusBar.Show(dnSpy_Debugger_Resources.StatusBar_Running);

		void SetReadyStatusMessage(string msg) {
			if (string.IsNullOrEmpty(msg))
				appWindow.StatusBar.Show(dnSpy_Debugger_Resources.StatusBar_Ready);
			else
				appWindow.StatusBar.Show(string.Format(dnSpy_Debugger_Resources.StatusBar_Ready2, msg));
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (ProcessState) {
			case DebuggerProcessState.Starting:
				DebugCallbackEvent_counter = 0;
				dbg.DebugCallbackEvent += DnDebugger_DebugCallbackEvent;
				currentLocation = null;
				currentMethod = null;
				appWindow.StatusBar.Open();
				SetRunningStatusMessage();
				appWindow.AddTitleInfo(dnSpy_Debugger_Resources.AppTitle_Debugging);
				Application.Current.Resources["IsDebuggingKey"] = true;
				break;

			case DebuggerProcessState.Continuing:
				break;

			case DebuggerProcessState.Running:
				if (dbg.IsEvaluating)
					break;
				SetRunningStatusMessage();
				break;

			case DebuggerProcessState.Paused:
				// If we're evaluating, or if eval has completed, don't do a thing. This code
				// should only be executed when a BP hits or if a stepping operation has completed.
				if (dbg.IsEvaluating || dbg.EvalCompleted)
					break;

				BringMainWindowToFrontAndActivate();

				UpdateCurrentLocation();
				if (currentMethod != null && currentLocation != null)
					JumpToCurrentStatement(documentTabService.GetOrCreateActiveTab());

				SetReadyStatusMessage(new StoppedMessageCreator().GetMessage(dbg));
				break;

			case DebuggerProcessState.Terminated:
				dbg.DebugCallbackEvent -= DnDebugger_DebugCallbackEvent;
				currentLocation = null;
				currentMethod = null;
				appWindow.StatusBar.Close();
				lastDebugProcessOptions = null;
				appWindow.RemoveTitleInfo(dnSpy_Debugger_Resources.AppTitle_Debugging);
				Application.Current.Resources["IsDebuggingKey"] = false;
				break;
			}

			// This is sometimes needed. Press Ctrl+Shift+F5 a couple of times and the toolbar
			// debugger icons aren't updated until you release Ctrl+Shift.
			if (dbg.ProcessState == DebuggerProcessState.Paused || !IsDebugging)
				CommandManager.InvalidateRequerySuggested();

			if (dbg.ProcessState == DebuggerProcessState.Paused)
				ShowExceptionMessage();
		}
		CodeLocation? currentLocation = null;

		void BringMainWindowToFrontAndActivate() {
			NativeMethods.SetWindowPos(new WindowInteropHelper(appWindow.MainWindow).Handle, IntPtr.Zero, 0, 0, 0, 0, 3);
			appWindow.MainWindow.Activate();
		}

		struct StoppedMessageCreator {
			StringBuilder sb;

			public string GetMessage(DnDebugger debugger) {
				if (debugger == null || debugger.ProcessState != DebuggerProcessState.Paused)
					return null;

				if (sb == null)
					sb = new StringBuilder();
				else
					sb.Clear();

				bool seenCodeBp = false;
				foreach (var state in debugger.DebuggerStates) {
					foreach (var pauseState in state.PauseStates) {
						switch (pauseState.Reason) {
						case DebuggerPauseReason.Other:
							Append(dnSpy_Debugger_Resources.Debug_StopReason_Unknown);
							break;

						case DebuggerPauseReason.UnhandledException:
							Append(dnSpy_Debugger_Resources.Debug_StopReason_UnhandledException);
							break;

						case DebuggerPauseReason.Exception:
							Append(dnSpy_Debugger_Resources.Debug_StopReason_Exception);
							break;

						case DebuggerPauseReason.DebugEventBreakpoint:
							if (state.EventArgs != null)
								Append(GetEventDescription(state.EventArgs));
							else
								Append(dnSpy_Debugger_Resources.Debug_StopReason_DebugEventBreakpoint);
							break;

						case DebuggerPauseReason.AnyDebugEventBreakpoint:
							if (state.EventArgs != null)
								Append(GetEventDescription(state.EventArgs));
							else
								Append(dnSpy_Debugger_Resources.Debug_StopReason_AnyDebugEventBreakpoint);
							break;

						case DebuggerPauseReason.Break:
							Append(dnSpy_Debugger_Resources.Debug_StopReason_BreakInstruction);
							break;

						case DebuggerPauseReason.ILCodeBreakpoint:
							if (seenCodeBp)
								break;
							seenCodeBp = true;
							Append(dnSpy_Debugger_Resources.Debug_StopReason_ILCodeBreakpoint);
							break;

						case DebuggerPauseReason.NativeCodeBreakpoint:
							if (seenCodeBp)
								break;
							seenCodeBp = true;
							Append(dnSpy_Debugger_Resources.Debug_StopReason_Breakpoint);
							break;

						case DebuggerPauseReason.Step:
							break;
						}
					}
				}

				return sb.ToString();
			}

			string GetEventDescription(DebugCallbackEventArgs e) {
				CorModule mod;
				switch (e.Kind) {
				case DebugCallbackKind.Exception:
					var ex1Args = (ExceptionDebugCallbackEventArgs)e;
					return ex1Args.Unhandled ? dnSpy_Debugger_Resources.Debug_EventDescription_UnhandledException : dnSpy_Debugger_Resources.Debug_EventDescription_Exception;

				case DebugCallbackKind.CreateProcess:
					var cpArgs = (CreateProcessDebugCallbackEventArgs)e;
					var p = cpArgs.CorProcess;
					if (p == null)
						break;
					return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_CreateProcess, p.ProcessId, p.CLRVersion);

				case DebugCallbackKind.CreateThread:
					var ctArgs = (CreateThreadDebugCallbackEventArgs)e;
					var t = ctArgs.CorThread;
					if (t == null)
						break;
					return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_CreateThread, t.ThreadId, t.VolatileThreadId);

				case DebugCallbackKind.LoadModule:
					var lmArgs = (LoadModuleDebugCallbackEventArgs)e;
					mod = lmArgs.CorModule;
					if (mod == null)
						break;
					if (mod.IsDynamic || mod.IsInMemory)
						return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_LoadModule1, mod.IsDynamic ? 1 : 0, mod.IsInMemory ? 1 : 0, mod.Address, mod.Size, mod.Name);
					return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_LoadModule2, mod.Address, mod.Size, mod.Name);

				case DebugCallbackKind.LoadClass:
					var lcArgs = (LoadClassDebugCallbackEventArgs)e;
					var cls = lcArgs.CorClass;
					mod = cls?.Module;
					if (mod == null)
						break;
					return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_LoadClass, cls.Token, FilterLongName(cls.ToString()), mod.Name);

				case DebugCallbackKind.DebuggerError:
					var deArgs = (DebuggerErrorDebugCallbackEventArgs)e;
					return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_DebuggerError, deArgs.HError, deArgs.ErrorCode);

				case DebugCallbackKind.CreateAppDomain:
					var cadArgs = (CreateAppDomainDebugCallbackEventArgs)e;
					var ad = cadArgs.CorAppDomain;
					if (ad == null)
						break;
					return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_CreateAppDomain, ad.Id, ad.Name);

				case DebugCallbackKind.LoadAssembly:
					var laArgs = (LoadAssemblyDebugCallbackEventArgs)e;
					var asm = laArgs.CorAssembly;
					if (asm == null)
						break;
					return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_LoadAssembly, asm.Name);

				case DebugCallbackKind.ControlCTrap:
					return dnSpy_Debugger_Resources.Debug_EventDescription_ControlCPressed;

				case DebugCallbackKind.BreakpointSetError:
					var bpseArgs = (BreakpointSetErrorDebugCallbackEventArgs)e;
					return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_BreakpointSetError, bpseArgs.Error);

				case DebugCallbackKind.Exception2:
					var ex2Args = (Exception2DebugCallbackEventArgs)e;
					var sb = new StringBuilder();
					sb.Append(string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_Exception2 + " ", ex2Args.Offset));
					switch (ex2Args.EventType) {
					case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_FIRST_CHANCE:
						sb.Append(dnSpy_Debugger_Resources.Debug_EventDescription_Exception2_FirstChance);
						break;
					case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_USER_FIRST_CHANCE:
						sb.Append(dnSpy_Debugger_Resources.Debug_EventDescription_Exception2_UserFirstChance);
						break;
					case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_CATCH_HANDLER_FOUND:
						sb.Append(dnSpy_Debugger_Resources.Debug_EventDescription_Exception2_CatchHandlerFound);
						break;
					case CorDebugExceptionCallbackType.DEBUG_EXCEPTION_UNHANDLED:
						sb.Append(dnSpy_Debugger_Resources.Debug_EventDescription_Exception2_Unhandled);
						break;
					default:
						sb.Append(dnSpy_Debugger_Resources.Debug_EventDescription_Exception2_Unknown);
						break;
					}
					return sb.ToString();

				case DebugCallbackKind.MDANotification:
					var mdan = (MDANotificationDebugCallbackEventArgs)e;
					var mda = mdan.CorMDA;
					if (mda == null)
						return dnSpy_Debugger_Resources.Debug_EventDescription_MDA_Notification;
					return string.Format(dnSpy_Debugger_Resources.Debug_EventDescription_MDA_Notification2, mda.OSThreadId, mda.Name, mda.Description);
				}

				return e.Kind.ToString();
			}

			void Append(string msg) {
				if (sb.Length > 0)
					sb.Append(", ");
				sb.Append(msg);
			}

			static string FilterLongName(string s) {
				const int MAX_LEN = 128;
				if (s.Length <= MAX_LEN)
					return s;
				return s.Substring(0, MAX_LEN / 2) + "..." + s.Substring(s.Length - (MAX_LEN - MAX_LEN / 2));
			}
		}

		void AppWindow_MainWindowClosing(object sender, CancelEventArgs e) {
			if (IsDebugging) {
				var result = messageBoxService.ShowIgnorableMessage(new Guid("B4B8E13C-B7B7-490A-953B-8ED8EAE7C170"), dnSpy_Debugger_Resources.AskAppWindowClosingStopDebugging, MsgBoxButton.Yes | MsgBoxButton.No);
				if (result == MsgBoxButton.None || result == MsgBoxButton.No)
					e.Cancel = true;
			}
		}

		static string GetIncompatiblePlatformErrorMessage() {
			if (IntPtr.Size == 4)
				return dnSpy_Debugger_Resources.UseDnSpyExeToDebug64;
			return dnSpy_Debugger_Resources.UseDnSpy64ExeToDebug32;
		}

		bool DebugProcess(DebugProcessOptions options, bool isInteractive) {
			if (IsDebugging)
				return false;
			if (options == null)
				return false;

			TheDebugger.RemoveDebugger();

			DnDebugger newDebugger;
			try {
				newDebugger = DnDebugger.DebugProcess(options);
			}
			catch (Exception ex) {
				var cex = ex as COMException;
				const int ERROR_NOT_SUPPORTED = unchecked((int)0x80070032);
				string errMsg;
				if (cex != null && cex.ErrorCode == ERROR_NOT_SUPPORTED)
					errMsg = string.Format(dnSpy_Debugger_Resources.Error_CouldNotStartDebugger, GetIncompatiblePlatformErrorMessage());
				else if (cex != null && cex.ErrorCode == CordbgErrors.CORDBG_E_UNCOMPATIBLE_PLATFORMS)
					errMsg = string.Format(dnSpy_Debugger_Resources.Error_CouldNotStartDebugger, GetIncompatiblePlatformErrorMessage());
				else if (cex != null && cex.ErrorCode == unchecked((int)0x800702E4))
					errMsg = dnSpy_Debugger_Resources.Error_CouldNotStartDebuggerRequireAdminPrivLvl;
				else
					errMsg = string.Format(dnSpy_Debugger_Resources.Error_CouldNotStartDebuggerCheckAccessToFile, options.Filename, ex.Message);
				if (isInteractive)
					messageBoxService.Show(errMsg);
				return false;
			}
			TheDebugger.Initialize(newDebugger);

			return true;
		}

		void ShowExceptionMessage() {
			var dbg = TheDebugger.Debugger;
			if (dbg == null)
				return;
			if (dbg.Current.GetPauseState(DebuggerPauseReason.Exception) == null)
				return;
			var thread = dbg.Current.Thread;
			if (thread == null)
				return;
			var exValue = thread.CorThread.CurrentException;
			if (exValue == null)
				return;
			var exType = exValue.ExactType;
			var name = exType?.ToString(TypePrinterFlags.ShowNamespaces) ?? "???";
			var msg = string.Format(dnSpy_Debugger_Resources.ExceptionThrownMessage, name, Path.GetFileName(thread.Process.Filename));
			BringMainWindowToFrontAndActivate();
			messageBoxService.Show(msg);
		}

		void DnDebugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			try {
				DebugCallbackEvent_counter++;

				if (DebugCallbackEvent_counter > 1)
					return;
				if (e.Kind == DebugCallbackKind.Exception2) {
					var ee = (Exception2DebugCallbackEventArgs)e;
					if (ee.EventType == CorDebugExceptionCallbackType.DEBUG_EXCEPTION_UNHANDLED)
						UnhandledException(ee);
				}
				else if (e.Kind == DebugCallbackKind.DebuggerError)
					OnDebuggerError((DebuggerErrorDebugCallbackEventArgs)e);
			}
			finally {
				DebugCallbackEvent_counter--;
			}
		}
		int DebugCallbackEvent_counter = 0;

		void UnhandledException(Exception2DebugCallbackEventArgs e) {
			if (UnhandledException_counter != 0)
				return;
			try {
				UnhandledException_counter++;
				theDebugger.SetUnhandledException(UnhandledException_counter != 0);

				Debug.Assert(e.EventType == CorDebugExceptionCallbackType.DEBUG_EXCEPTION_UNHANDLED);
				var thread = e.CorThread;
				var exValue = thread?.CurrentException;

				var sb = new StringBuilder();
				AddExceptionInfo(sb, exValue, dnSpy_Debugger_Resources.ExceptionInfo_Exception);
				var innerExValue = EvalUtils.ReflectionReadExceptionInnerException(exValue);
				if (innerExValue != null && innerExValue.IsReference && !innerExValue.IsNull)
					AddExceptionInfo(sb, innerExValue, "\n\n" + dnSpy_Debugger_Resources.ExceptionInfo_InnerException);

				var process = TheDebugger.Debugger.Processes.FirstOrDefault(p => p.Threads.Any(t => t.CorThread == thread));
				CorProcess cp;
				var processName = process != null ? Path.GetFileName(process.Filename) : string.Format("pid {0}", (cp = thread.Process) == null ? 0 : cp.ProcessId);
				BringMainWindowToFrontAndActivate();
				var res = messageBoxService.Show(string.Format(dnSpy_Debugger_Resources.Error_UnhandledExceptionOccurred, processName, sb), MsgBoxButton.OK | MsgBoxButton.Cancel);
				if (res != MsgBoxButton.Cancel)
					e.AddPauseReason(DebuggerPauseReason.UnhandledException);
			}
			finally {
				UnhandledException_counter--;
				theDebugger.SetUnhandledException(UnhandledException_counter != 0);
			}
		}
		int UnhandledException_counter = 0;

		void OnDebuggerError(DebuggerErrorDebugCallbackEventArgs e) {
			string msg;
			if (e.HError == CordbgErrors.CORDBG_E_UNCOMPATIBLE_PLATFORMS)
				msg = GetIncompatiblePlatformErrorMessage();
			else
				msg = string.Format(dnSpy_Debugger_Resources.Error_CLRDebuggerErrorOccurred, e.HError, e.ErrorCode);
			BringMainWindowToFrontAndActivate();
			messageBoxService.Show(msg);
		}

		static void AddExceptionInfo(StringBuilder sb, CorValue exValue, string msg) {
			var exType = exValue?.ExactType;
			int? hr = EvalUtils.ReflectionReadExceptionHResult(exValue);
			string exMsg;
			EvalUtils.ReflectionReadExceptionMessage(exValue, out exMsg);
			string exTypeString = exType?.ToString() ?? dnSpy_Debugger_Resources.UnknownExceptionType;
			var s = string.Format(dnSpy_Debugger_Resources.ExceptionInfoFormat, msg, exTypeString, exMsg ?? dnSpy_Debugger_Resources.ExceptionMessageIsNull, hr ?? -1);
			sb.Append(s);
		}

		public bool CanDebugCurrentAssembly(object parameter) => GetCurrentExecutableAssembly(parameter as IMenuItemContext) != null;

		public void DebugCurrentAssembly(object parameter) {
			var asm = GetCurrentExecutableAssembly(parameter as IMenuItemContext);
			if (asm == null)
				return;
			DebugAssembly2(GetDebugAssemblyOptions(CreateDebugProcessVM(asm)));
		}

		public IDsDocument GetCurrentExecutableAssembly(IMenuItemContext context) {
			if (context == null)
				return null;
			if (IsDebugging)
				return null;

			DocumentTreeNodeData node;
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID)) {
				var uiContext = context.Find<IDocumentViewer>();
				if (uiContext == null)
					return null;
				var nodes = uiContext.DocumentTab.Content.Nodes.ToArray();
				if (nodes.Length == 0)
					return null;
				node = nodes[0];
			}
			else if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID)) {
				var nodes = context.Find<DocumentTreeNodeData[]>();
				if (nodes == null || nodes.Length == 0)
					return null;
				node = nodes[0];
			}
			else
				return null;

			return GetCurrentExecutableAssembly(node, true);
		}

		static IDsDocument GetCurrentExecutableAssembly(TreeNodeData node, bool mustBeNetExe) {
			var fileNode = (node as DocumentTreeNodeData).GetDocumentNode();
			if (fileNode == null)
				return null;

			var file = fileNode.Document;
			var peImage = file.PEImage;
			if (peImage == null)
				return null;
			if ((peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) != 0)
				return null;
			if (mustBeNetExe) {
				var mod = file.ModuleDef;
				if (mod == null)
					return null;
				if (mod.Assembly == null || mod.Assembly.ManifestModule != mod)
					return null;
				if (mod.ManagedEntryPoint == null && mod.NativeEntryPoint == 0)
					return null;
			}

			return file;
		}

		IDsDocument GetCurrentExecutableAssembly(bool mustBeNetExe) => GetCurrentExecutableAssembly(documentTabService.DocumentTreeView.TreeView.SelectedItem, mustBeNetExe);

		public bool CanStartWithoutDebugging => !IsDebugging && GetCurrentExecutableAssembly(false) != null;

		public void StartWithoutDebugging() {
			var asm = GetCurrentExecutableAssembly(false);
			if (asm == null || !File.Exists(asm.Filename))
				return;
			try {
				Process.Start(asm.Filename);
			}
			catch (Exception ex) {
				messageBoxService.Show(string.Format(dnSpy_Debugger_Resources.Error_StartWithoutDebuggingCouldNotStart, asm.Filename, ex.Message));
			}
		}

		DebugCoreCLRVM CreateDebugCoreCLRVM(IDsDocument asm = null) {
			// Re-use the previous one if it's the same file
			if (lastDebugCoreCLRVM != null && asm != null) {
				if (StringComparer.OrdinalIgnoreCase.Equals(lastDebugCoreCLRVM.Filename, asm.Filename))
					return lastDebugCoreCLRVM.Clone();
			}

			var vm = new DebugCoreCLRVM();
			if (asm != null)
				vm.Filename = asm.Filename;
			vm.DbgShimFilename = DebuggerSettings.CoreCLRDbgShimFilename;
			vm.BreakProcessKind = DebuggerSettings.BreakProcessKind;
			return vm;
		}

		public bool CanDebugCoreCLRAssembly => !IsDebugging;

		public bool DebugCoreCLRAssembly() {
			if (!CanDebugAssembly)
				return false;
			DebugCoreCLRVM vm = null;
			if (vm == null) {
				var asm = GetCurrentExecutableAssembly(true);
				if (asm != null)
					vm = CreateDebugCoreCLRVM(asm);
			}
			if (vm == null)
				vm = lastDebugCoreCLRVM ?? CreateDebugCoreCLRVM();
			return DebugAssembly2(GetDebugAssemblyOptions(vm.Clone()));
		}
		DebugCoreCLRVM lastDebugCoreCLRVM;

		DebugProcessOptions GetDebugAssemblyOptions(DebugCoreCLRVM vm, bool askUser = true) {
			if (askUser) {
				var win = new DebugCoreCLRDlg();
				win.DataContext = vm;
				win.Owner = appWindow.MainWindow;
				if (win.ShowDialog() != true)
					return null;
			}

			var opts = new DebugProcessOptions(new CoreCLRTypeDebugInfo(vm.DbgShimFilename, vm.HostFilename, vm.HostCommandLine));
			opts.DebugMessageDispatcher = WpfDebugMessageDispatcher.Instance;
			opts.CurrentDirectory = vm.CurrentDirectory;
			opts.Filename = vm.Filename;
			opts.CommandLine = vm.CommandLine;
			opts.BreakProcessKind = vm.BreakProcessKind;
			lastDebugCoreCLRVM = vm;
			return opts;
		}

		DebugProcessVM CreateDebugProcessVM(IDsDocument asm = null) {
			// Re-use the previous one if it's the same file
			if (lastDebugProcessVM != null && asm != null) {
				if (StringComparer.OrdinalIgnoreCase.Equals(lastDebugProcessVM.Filename, asm.Filename))
					return lastDebugProcessVM.Clone();
			}

			var vm = new DebugProcessVM();
			if (asm != null)
				vm.Filename = asm.Filename;
			vm.BreakProcessKind = DebuggerSettings.BreakProcessKind;
			return vm;
		}

		public bool CanDebugAssembly => !IsDebugging;

		public bool DebugAssembly() {
			if (!CanDebugAssembly)
				return false;
			DebugProcessVM vm = null;
			if (vm == null) {
				var asm = GetCurrentExecutableAssembly(true);
				if (asm != null)
					vm = CreateDebugProcessVM(asm);
			}
			if (vm == null)
				vm = lastDebugProcessVM ?? CreateDebugProcessVM();
			return DebugAssembly2(GetDebugAssemblyOptions(vm.Clone()));
		}
		DebugProcessVM lastDebugProcessVM;

		DebugProcessOptions GetDebugAssemblyOptions(DebugProcessVM vm, bool askUser = true) {
			if (askUser) {
				var win = new DebugProcessDlg();
				win.DataContext = vm;
				win.Owner = appWindow.MainWindow;
				if (win.ShowDialog() != true)
					return null;
			}

			var opts = new DebugProcessOptions(new DesktopCLRTypeDebugInfo());
			opts.DebugMessageDispatcher = WpfDebugMessageDispatcher.Instance;
			opts.CurrentDirectory = vm.CurrentDirectory;
			opts.Filename = vm.Filename;
			opts.CommandLine = vm.CommandLine;
			opts.BreakProcessKind = vm.BreakProcessKind;
			opts.DebugOptions.IgnoreBreakInstructions = DebuggerSettings.IgnoreBreakInstructions;
			lastDebugProcessVM = vm;
			return opts;
		}

		public bool CanRestart => IsDebugging && lastDebugProcessOptions != null && !HasAttached;

		public void Restart() {
			if (!CanRestart)
				return;

			var oldOpts = lastDebugProcessOptions;
			Stop();
			TheDebugger.RemoveAndRaiseEvent();
			lastDebugProcessOptions = oldOpts;

			DebugAssembly2(lastDebugProcessOptions);
		}

		public bool DebugAssembly(DebugProcessOptions options) => DebugAssembly2(options, false);

		bool DebugAssembly2(DebugProcessOptions options, bool isInteractive = true) {
			if (options == null)
				return false;
			var optionsCopy = options.Clone();
			if (!DebugProcess(options, isInteractive))
				return false;
			lastDebugProcessOptions = optionsCopy;
			return true;
		}
		DebugProcessOptions lastDebugProcessOptions = null;

		public bool CanAttach => !IsDebugging;

		public bool Attach() {
			if (!CanAttach)
				return false;

			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			var data = new AttachProcessVM(Dispatcher.CurrentDispatcher, DebuggerSettings.SyntaxHighlightAttach, classificationFormatMap, textElementProvider);
			var win = new AttachProcessDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			var res = win.ShowDialog();
			data.Dispose();
			if (res != true)
				return false;

			var processVM = data.SelectedProcess;
			if (processVM == null)
				return false;

			var options = new AttachProcessOptions(processVM.CLRTypeInfo);
			options.ProcessId = processVM.PID;
			options.DebugMessageDispatcher = WpfDebugMessageDispatcher.Instance;
			string errMsg;
			if (!Attach(options, out errMsg)) {
				if (!string.IsNullOrEmpty(errMsg))
					messageBoxService.Show(errMsg);
				return false;
			}
			return true;
		}

		public bool Attach(AttachProcessOptions options) {
			string errMsg;
			return Attach(options, out errMsg);
		}

		bool Attach(AttachProcessOptions options, out string errMsg) {
			errMsg = null;
			if (IsDebugging)
				return false;
			if (options == null)
				return false;

			TheDebugger.RemoveDebugger();

			DnDebugger newDebugger;
			try {
				newDebugger = DnDebugger.Attach(options);
			}
			catch (Exception ex) {
				errMsg = string.Format(dnSpy_Debugger_Resources.Error_CouldNotStartDebugger2, ex.Message);
				return false;
			}
			if (newDebugger.Processes.Length == 0) {
				errMsg = string.Format(dnSpy_Debugger_Resources.Error_CouldNotStartDebugger2, "Could not attach to the process");
				return false;
			}
			TheDebugger.Initialize(newDebugger);

			return true;
		}

		public bool CanBreak => ProcessState == DebuggerProcessState.Starting || ProcessState == DebuggerProcessState.Running;

		public void Break() {
			if (!CanBreak)
				return;

			int hr = TheDebugger.Debugger.TryBreakProcesses();
			if (hr < 0)
				messageBoxService.Show(string.Format(dnSpy_Debugger_Resources.Error_CouldNotBreakProcess, hr));
		}

		public bool CanStop => IsDebugging;

		public void Stop() {
			if (!CanStop)
				return;
			TheDebugger.Debugger.TerminateProcesses();
		}

		public bool CanDetach => ProcessState != DebuggerProcessState.Continuing && ProcessState != DebuggerProcessState.Terminated;

		public void Detach() {
			if (!CanDetach)
				return;
			int hr = TheDebugger.Debugger.TryDetach();
			if (hr < 0)
				messageBoxService.Show(string.Format(dnSpy_Debugger_Resources.Error_CouldNotDetachProcess, hr));
		}

		public bool CanContinue => ProcessState == DebuggerProcessState.Paused;

		public void Continue() {
			if (!CanContinue)
				return;
			TheDebugger.Debugger.Continue();
		}

		public void JumpToCurrentStatement(IDocumentTab tab) => JumpToCurrentStatement(tab, true);

		void JumpToCurrentStatement(IDocumentTab tab, bool canRefreshMethods) {
			if (tab == null)
				return;
			if (currentMethod == null)
				return;

			// The file could've been added lazily to the list so add a short delay before we select it
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				tab.FollowReference(currentMethod, false, e => {
					Debug.Assert(e.Tab == tab);
					Debug.Assert(e.Tab.UIContext is IDocumentViewer);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretToCurrentStatement(e.Tab.UIContext as IDocumentViewer, canRefreshMethods);
						e.HasMovedCaret = true;
					}
				});
			}));
		}

		bool MoveCaretToCurrentStatement(IDocumentViewer documentViewer, bool canRefreshMethods) {
			if (documentViewer == null)
				return false;
			if (currentLocation == null)
				return false;
			if (DebugUtils.MoveCaretTo(documentViewer, currentLocation.Value.SerializedDnToken, currentLocation.Value.Offset))
				return true;
			if (!canRefreshMethods)
				return false;

			RefreshMethodBodies(documentViewer);

			return false;
		}

		void RefreshMethodBodies(IDocumentViewer documentViewer) {
			if (currentLocation == null)
				return;
			if (currentMethod == null)
				return;
			if (documentViewer == null)
				return;

			// If this fails, we're probably in the prolog or epilog. Shouldn't normally happen.
			if (!currentLocation.Value.IsExact && !currentLocation.Value.IsApproximate)
				return;
			var body = currentMethod.Body;
			if (body == null)
				return;
			// If the offset is a valid instruction in the body, the method is probably not encrypted
			if (body.Instructions.Any(i => i.Offset == currentLocation.Value.Offset))
				return;

			// No instruction with the current offset: it must be encrypted, and since we're executing
			// the method, we must be using an invalid method body. Use a copy of the module in
			// memory, and refresh the method bodies in case it's already loaded, and re-decompile
			// the method.

			var mod = currentMethod.Module;
			if (mod == null)
				return;
			var modNode = documentTabService.DocumentTreeView.FindNode(mod);
			if (modNode == null)
				return;
			var memFile = modNode.Document as MemoryModuleDefFile;
			IDsDocument document = memFile;
			if (memFile == null) {
				if (modNode.Document is CorModuleDefFile)
					return;
				var corMod = currentLocation.Value.Function.Module;
				if (corMod == null || corMod.IsDynamic)
					return;
				var dnMod = moduleLoader.Value.GetDnModule(corMod);
				document = inMemoryModuleService.Value.LoadDocument(dnMod, true);
				Debug.Assert(document != null);
				memFile = document as MemoryModuleDefFile;
			}
			if (document == null)
				return;
			// It's null if we couldn't load the file from memory because the PE / COR20 headers
			// are corrupt (eg. an obfuscator overwrote various fields with garbage). In that case,
			// file is a CorModuleDefFile and it's using the MD API to read the MD.
			if (memFile != null)
				inMemoryModuleService.Value.UpdateModuleMemory(memFile);
			UpdateCurrentMethod(document);
			JumpToCurrentStatement(documentViewer.DocumentTab, false);
		}

		void UpdateCurrentLocationToInMemoryModule() {
			UpdateCurrentMethod();
			if (currentMethod != null && currentLocation != null)
				JumpToCurrentStatement(documentTabService.GetOrCreateActiveTab());
		}

		struct CodeLocation {
			public CorFunction Function { get; }
			public uint Offset { get; }
			public CorDebugMappingResult Mapping { get; }

			public uint Token => Function.Token;
			public bool IsExact => (Mapping & CorDebugMappingResult.MAPPING_EXACT) != 0;
			public bool IsApproximate => (Mapping & CorDebugMappingResult.MAPPING_APPROXIMATE) != 0;

			public ModuleTokenId SerializedDnToken {
				get {
					var mod = Function.Module;
					if (mod == null)
						return new ModuleTokenId();
					return new ModuleTokenId(mod.DnModuleId.ToModuleId(), Function.Token);
				}
			}

			public CodeLocation(CorFunction func, uint offset, CorDebugMappingResult mapping) {
				Function = func;
				Offset = offset;
				Mapping = mapping;
			}

			public static bool SameMethod(CodeLocation a, CodeLocation b) => a.Function == b.Function;
		}

		void UpdateCurrentLocation() => UpdateCurrentLocation(TheDebugger.Debugger.Current.ILFrame);

		public void UpdateCurrentLocation(CorFrame frame) {
			var newLoc = GetCodeLocation(frame);

			if (currentLocation == null || newLoc == null) {
				currentLocation = newLoc;
				UpdateCurrentMethod();
				return;
			}
			if (!CodeLocation.SameMethod(currentLocation.Value, newLoc.Value)) {
				currentLocation = newLoc;
				UpdateCurrentMethod();
				return;
			}

			currentLocation = newLoc;
		}

		void UpdateCurrentMethod(IDsDocument document = null) {
			if (currentLocation == null) {
				currentMethod = null;
				return;
			}

			if (document == null)
				document = moduleLoader.Value.LoadModule(currentLocation.Value.Function.Module, canLoadDynFile: true, isAutoLoaded: true);
			Debug.Assert(document != null);
			var loadedMod = document?.ModuleDef;
			if (loadedMod == null) {
				currentMethod = null;
				return;
			}

			currentMethod = loadedMod.ResolveToken(currentLocation.Value.Token) as MethodDef;
			Debug.Assert(currentMethod != null);
		}
		MethodDef currentMethod;

		CodeLocation? GetCodeLocation(CorFrame frame) {
			if (ProcessState != DebuggerProcessState.Paused)
				return null;
			if (frame == null)
				return null;
			var func = frame.Function;
			if (func == null)
				return null;

			return new CodeLocation(func, frame.GetILOffset(moduleLoader.Value), frame.ILFrameIP.Mapping);
		}

		StepRange[] GetStepRanges(DnDebugger debugger, CorFrame frame, bool isStepInto) {
			if (frame == null)
				return null;
			if (!frame.IsILFrame)
				return null;
			if (frame.ILFrameIP.IsUnmappedAddress)
				return null;

			var key = CreateMethodKey(debugger, frame);
			if (key == null)
				return null;

			MethodDebugInfo info;
			var tab = documentTabService.GetOrCreateActiveTab();
			var documentViewer = tab.TryGetDocumentViewer();
			var methodDebugService = documentViewer.GetMethodDebugService();
			if ((info = methodDebugService.TryGetMethodDebugInfo(key.Value)) == null) {
				// User has decompiled some other code or switched to another tab
				UpdateCurrentMethod();
				JumpToCurrentStatement(tab);

				// It could be cached and immediately available. Check again
				documentViewer = tab.TryGetDocumentViewer();
				methodDebugService = documentViewer.GetMethodDebugService();
				if ((info = methodDebugService.TryGetMethodDebugInfo(key.Value)) == null)
					return null;
			}

			var sourceStatement = info.GetSourceStatementByCodeOffset(frame.GetILOffset(moduleLoader.Value));
			uint[] ranges;
			if (sourceStatement == null)
				ranges = info.GetUnusedRanges();
			else
				ranges = info.GetRanges(sourceStatement.Value);

			if (ranges.Length == 0)
				return null;
			return CreateStepRanges(ranges);
		}

		static StepRange[] CreateStepRanges(uint[] ilSpans) {
			var stepRanges = new StepRange[ilSpans.Length / 2];
			if (stepRanges.Length == 0)
				return null;
			for (int i = 0; i < stepRanges.Length; i++)
				stepRanges[i] = new StepRange(ilSpans[i * 2], ilSpans[i * 2 + 1]);
			return stepRanges;
		}

		static ModuleTokenId? CreateMethodKey(DnDebugger debugger, CorFrame frame) {
			var sma = frame.DnModuleId;
			if (sma == null)
				return null;

			return new ModuleTokenId(sma.Value.ToModuleId(), frame.Token);
		}

		CorFrame GetCurrentILFrame() {
			if (ProcessState != DebuggerProcessState.Paused)
				return null;
			return StackFrameService.FirstILFrame;
		}

		CorFrame GetCurrentMethodILFrame() {
			if (ProcessState != DebuggerProcessState.Paused)
				return null;
			return StackFrameService.SelectedFrame;
		}

		public bool CanStepInto() => CanStepInto(GetCurrentILFrame());
		public bool CanStepInto(CorFrame frame) => ProcessState == DebuggerProcessState.Paused && frame != null;
		public void StepInto() => StepInto(GetCurrentILFrame());

		public void StepInto(CorFrame frame) {
			if (!CanStepInto(frame))
				return;

			var ranges = GetStepRanges(TheDebugger.Debugger, frame, true);
			TheDebugger.Debugger.StepInto(frame, ranges);
		}

		public bool CanStepOver() => CanStepOver(GetCurrentILFrame());
		public bool CanStepOver(CorFrame frame) => ProcessState == DebuggerProcessState.Paused && frame != null;
		public void StepOver() => StepOver(GetCurrentILFrame());

		public void StepOver(CorFrame frame) {
			if (!CanStepOver(frame))
				return;

			var ranges = GetStepRanges(TheDebugger.Debugger, frame, false);
			TheDebugger.Debugger.StepOver(frame, ranges);
		}

		public bool CanStepOut() => CanStepOut(GetCurrentILFrame());
		public bool CanStepOut(CorFrame frame) => ProcessState == DebuggerProcessState.Paused && frame != null;
		public void StepOut() => StepOut(GetCurrentILFrame());

		public void StepOut(CorFrame frame) {
			if (!CanStepOut(frame))
				return;

			TheDebugger.Debugger.StepOut(frame);
		}

		public bool CanRunTo(CorFrame frame) => ProcessState == DebuggerProcessState.Paused && TheDebugger.Debugger.CanRunTo(frame);

		public bool RunTo(CorFrame frame) {
			if (!CanRunTo(frame))
				return false;

			return TheDebugger.Debugger.RunTo(frame);
		}

		public bool CanShowNextStatement => ProcessState == DebuggerProcessState.Paused && GetCurrentILFrame() != null;

		public void ShowNextStatement() {
			if (!CanShowNextStatement)
				return;

			var tab = documentTabService.GetOrCreateActiveTab();
			if (!TryShowNextStatement(tab.TryGetDocumentViewer())) {
				UpdateCurrentMethod();
				JumpToCurrentStatement(tab);
			}
		}

		bool TryShowNextStatement(IDocumentViewer documentViewer) {
			// Always reset the selected frame
			StackFrameService.SelectedFrameNumber = 0;
			if (currentLocation == null)
				return false;
			return DebugUtils.MoveCaretTo(documentViewer, currentLocation.Value.SerializedDnToken, currentLocation.Value.Offset);
		}

		IDocumentViewer TryGetDocumentViewer(object parameter) {
			var ctx = parameter as IMenuItemContext;
			if (ctx == null)
				return null;
			if (ctx.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID)) {
				var tab = ctx.CreatorObject.Object as IDocumentTab;
				return tab == null ? null : tab.UIContext as IDocumentViewer;
			}
			return null;
		}

		public bool CanSetNextStatement(object parameter) {
			if (!IsDebugging)
				return false;

			MethodSourceStatement methodStatement;
			string errMsg;
			if (!DebugGetMethodSourceStatementForSetNextStatement(TryGetDocumentViewer(parameter), out errMsg, out methodStatement))
				return false;

			return CanSetNextStatement(methodStatement.Statement.BinSpan.Start);
		}

		bool CanSetNextStatement(uint ilOffset) => CanSetNextStatement(currentLocation, ilOffset);

		bool CanSetNextStatement(CodeLocation? loc, uint ilOffset) {
			if (!IsDebugging)
				return false;
			if (loc != null && loc.Value.IsExact)
				return loc.Value.Offset != ilOffset;
			return true;
		}

		public bool SetNextStatement(object parameter) {
			string errMsg;
			if (!DebugSetNextStatement(parameter, out errMsg)) {
				if (string.IsNullOrEmpty(errMsg))
					errMsg = dnSpy_Debugger_Resources.Error_CouldNotSetNextStatement_UnknownReason;
				messageBoxService.Show(errMsg);
				return false;
			}

			return true;
		}

		bool DebugSetNextStatement(object parameter, out string errMsg) {
			MethodSourceStatement methodStatement;
			if (!DebugGetMethodSourceStatementForSetNextStatement(TryGetDocumentViewer(parameter), out errMsg, out methodStatement))
				return false;
			return SetOffset(methodStatement.Statement.BinSpan.Start, out errMsg);
		}

		public bool SetOffset(uint ilOffset, out string errMsg) => SetOffset(currentLocation, ilOffset, GetCurrentMethodILFrame(), out errMsg);

		public bool SetOffset(CorFrame frame, uint ilOffset, out string errMsg) {
			var loc = GetCodeLocation(frame);
			return SetOffset(loc, ilOffset, frame, out errMsg);
		}

		bool SetOffset(CodeLocation? loc, uint ilOffset, CorFrame frame, out string errMsg) {
			if (frame == null || !CanSetNextStatement(loc, ilOffset)) {
				errMsg = dnSpy_Debugger_Resources.Error_CouldNotSetNextStatement_UnknownReason;
				return false;
			}

			bool failed = !frame.SetILFrameIP(ilOffset);

			// All frames are invalidated
			TheDebugger.CallOnProcessStateChanged();

			if (failed) {
				errMsg = dnSpy_Debugger_Resources.Error_CouldNotSetNextStatement;
				return false;
			}

			errMsg = null;
			return true;
		}

		public bool SetNativeOffset(uint ilOffset, out string errMsg) => SetNativeOffset(ilOffset, GetCurrentMethodILFrame(), out errMsg);
		public bool SetNativeOffset(CorFrame frame, uint ilOffset, out string errMsg) => SetNativeOffset(ilOffset, frame, out errMsg);

		bool SetNativeOffset(uint ilOffset, CorFrame frame, out string errMsg) {
			if (frame == null) {
				errMsg = dnSpy_Debugger_Resources.Error_CouldNotSetNextStatement_UnknownReason;
				return false;
			}

			bool failed = !frame.SetNativeFrameIP(ilOffset);

			// All frames are invalidated
			TheDebugger.CallOnProcessStateChanged();

			if (failed) {
				errMsg = dnSpy_Debugger_Resources.Error_CouldNotSetNextStatement;
				return false;
			}

			errMsg = null;
			return true;
		}

		bool DebugGetMethodSourceStatementForSetNextStatement(IDocumentViewer documentViewer, out string errMsg, out MethodSourceStatement methodStatement) {
			errMsg = string.Empty;
			methodStatement = default(MethodSourceStatement);

			if (ProcessState == DebuggerProcessState.Terminated) {
				errMsg = dnSpy_Debugger_Resources.Error_NotDebugging;
				return false;
			}
			if (ProcessState == DebuggerProcessState.Starting || ProcessState == DebuggerProcessState.Continuing || ProcessState == DebuggerProcessState.Running) {
				errMsg = dnSpy_Debugger_Resources.Error_CantSetNextStatementWhenNotStopped;
				return false;
			}

			if (documentViewer == null) {
				documentViewer = documentTabService.ActiveTab.TryGetDocumentViewer();
				if (documentViewer == null) {
					errMsg = dnSpy_Debugger_Resources.Error_NoTabAvailableDecompileCurrentMethod;
					return false;
				}
			}

			IMethodDebugService methodDebugService;
			if (currentLocation == null || !DebugUtils.VerifyAndGetCurrentDebuggedMethod(documentViewer, currentLocation.Value.SerializedDnToken, out methodDebugService)) {
				errMsg = dnSpy_Debugger_Resources.Error_NoDebugInfoAvailable;
				return false;
			}
			Debug.Assert(currentLocation != null);

			var methodStatements = methodDebugService.FindByTextPosition(documentViewer.Caret.Position.BufferPosition.Position, sameMethod: false);
			if (methodStatements.Count == 0) {
				errMsg = dnSpy_Debugger_Resources.Error_CantSetNextStatementHere;
				return false;
			}

			if (GetCurrentMethodILFrame() == null) {
				errMsg = dnSpy_Debugger_Resources.Error_CantSetNextStatementNoILFrame;
				return false;
			}

			foreach (var info in methodStatements) {
				var md = info.Method;
				if (currentLocation.Value.Function.Token != md.MDToken.Raw)
					continue;
				var moduleId = moduleIdProvider.Create(md.Module);
				if (!moduleId.Equals(currentLocation.Value.SerializedDnToken.Module))
					continue;

				methodStatement = info;
				break;
			}
			if (methodStatement.Method == null) {
				errMsg = dnSpy_Debugger_Resources.Error_CantSetNextStatementToAnotherMethod;
				return false;
			}

			return true;
		}
	}
}
