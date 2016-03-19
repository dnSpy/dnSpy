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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.CallStack;
using dnSpy.Debugger.Dialogs;
using dnSpy.Debugger.IMModules;
using dnSpy.Debugger.Properties;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Debugger {
	interface IDebugManager {
		IDnSpyFile GetCurrentExecutableAssembly(IMenuItemContext context);
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
		IStackFrameManager StackFrameManager { get; }
	}

	[ExportFileListListener]
	sealed class DebugManagerFileListListener : IFileListListener {
		public bool CanLoad {
			get { return !debugManager.Value.TheDebugger.IsDebugging; }
		}

		public bool CanReload {
			get { return !debugManager.Value.TheDebugger.IsDebugging; }
		}

		readonly Lazy<DebugManager> debugManager;

		[ImportingConstructor]
		DebugManagerFileListListener(Lazy<DebugManager> debugManager) {
			this.debugManager = debugManager;
		}

		public void AfterLoad(bool isReload) {
		}

		public void BeforeLoad(bool isReload) {
		}

		public bool CheckCanLoad(bool isReload) {
			return true;
		}
	}

	[Export, Export(typeof(IDebugManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class DebugManager : IDebugManager {
		readonly IAppWindow appWindow;
		readonly IFileTabManager fileTabManager;
		readonly IMessageBoxManager messageBoxManager;
		readonly ITheDebugger theDebugger;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly Lazy<IInMemoryModuleManager> inMemoryModuleManager;
		readonly ISerializedDnModuleCreator serializedDnModuleCreator;

		public ITheDebugger TheDebugger {
			get { return theDebugger; }
		}

		public IStackFrameManager StackFrameManager {
			get { return stackFrameManager; }
		}
		readonly IStackFrameManager stackFrameManager;

		public IDebuggerSettings DebuggerSettings {
			get { return debuggerSettings; }
		}
		readonly IDebuggerSettings debuggerSettings;

		[ImportingConstructor]
		DebugManager(IAppWindow appWindow, IFileTabManager fileTabManager, IMessageBoxManager messageBoxManager, IDebuggerSettings debuggerSettings, ITheDebugger theDebugger, IStackFrameManager stackFrameManager, Lazy<IModuleLoader> moduleLoader, Lazy<IInMemoryModuleManager> inMemoryModuleManager, ISerializedDnModuleCreator serializedDnModuleCreator) {
			this.appWindow = appWindow;
			this.fileTabManager = fileTabManager;
			this.messageBoxManager = messageBoxManager;
			this.debuggerSettings = debuggerSettings;
			this.theDebugger = theDebugger;
			this.stackFrameManager = stackFrameManager;
			this.moduleLoader = moduleLoader;
			this.inMemoryModuleManager = inMemoryModuleManager;
			this.serializedDnModuleCreator = serializedDnModuleCreator;
			stackFrameManager.PropertyChanged += StackFrameManager_PropertyChanged;
			theDebugger.ProcessRunning += TheDebugger_ProcessRunning;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			appWindow.MainWindowClosing += AppWindow_MainWindowClosing;
			debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
		}

		void StackFrameManager_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SelectedThread")
				UpdateCurrentLocation(stackFrameManager.FirstILFrame);
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
			if (e.PropertyName == "IgnoreBreakInstructions") {
				if (TheDebugger.Debugger != null)
					TheDebugger.Debugger.Options.IgnoreBreakInstructions = debuggerSettings.IgnoreBreakInstructions;
			}
			else if (e.PropertyName == "UseMemoryModules") {
				if (ProcessState != DebuggerProcessState.Terminated && debuggerSettings.UseMemoryModules)
					UpdateCurrentLocationToInMemoryModule();
			}
		}

		public DebuggerProcessState ProcessState {
			get { return TheDebugger.ProcessState; }
		}

		public bool IsDebugging {
			get { return ProcessState != DebuggerProcessState.Terminated; }
		}

		/// <summary>
		/// true if we've attached to a process
		/// </summary>
		public bool HasAttached {
			get { return IsDebugging && TheDebugger.Debugger.HasAttached; }
		}

		public bool IsEvaluating {
			get { return IsDebugging && TheDebugger.Debugger.IsEvaluating; }
		}

		public bool EvalCompleted {
			get { return IsDebugging && TheDebugger.Debugger.EvalCompleted; }
		}

		void SetRunningStatusMessage() {
			appWindow.StatusBar.Show(dnSpy_Debugger_Resources.StatusBar_Running);
		}

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
					JumpToCurrentStatement(fileTabManager.GetOrCreateActiveTab());

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
					mod = cls == null ? null : cls.Module;
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
				var result = messageBoxManager.ShowIgnorableMessage(new Guid("B4B8E13C-B7B7-490A-953B-8ED8EAE7C170"), dnSpy_Debugger_Resources.AskAppWindowClosingStopDebugging, MsgBoxButton.Yes | MsgBoxButton.No);
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
					messageBoxManager.Show(errMsg);
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
			var name = exType == null ? null : exType.ToString(TypePrinterFlags.ShowNamespaces);
			var msg = string.Format(dnSpy_Debugger_Resources.ExceptionThrownMessage, name, Path.GetFileName(thread.Process.Filename));
			BringMainWindowToFrontAndActivate();
			messageBoxManager.Show(msg);
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
				var exValue = thread == null ? null : thread.CurrentException;

				var sb = new StringBuilder();
				AddExceptionInfo(sb, exValue, dnSpy_Debugger_Resources.ExceptionInfo_Exception);
				var innerExValue = EvalUtils.ReflectionReadExceptionInnerException(exValue);
				if (innerExValue != null && innerExValue.IsReference && !innerExValue.IsNull)
					AddExceptionInfo(sb, innerExValue, "\n\n" + dnSpy_Debugger_Resources.ExceptionInfo_InnerException);

				var process = TheDebugger.Debugger.Processes.FirstOrDefault(p => p.Threads.Any(t => t.CorThread == thread));
				CorProcess cp;
				var processName = process != null ? Path.GetFileName(process.Filename) : string.Format("pid {0}", (cp = thread.Process) == null ? 0 : cp.ProcessId);
				BringMainWindowToFrontAndActivate();
				var res = messageBoxManager.Show(string.Format(dnSpy_Debugger_Resources.Error_UnhandledExceptionOccurred, processName, sb), MsgBoxButton.OK | MsgBoxButton.Cancel);
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
			messageBoxManager.Show(msg);
		}

		static void AddExceptionInfo(StringBuilder sb, CorValue exValue, string msg) {
			var exType = exValue == null ? null : exValue.ExactType;
			int? hr = EvalUtils.ReflectionReadExceptionHResult(exValue);
			string exMsg = EvalUtils.ReflectionReadExceptionMessage(exValue);
			string exTypeString = exType == null ? dnSpy_Debugger_Resources.UnknownExceptionType : exType.ToString();
			var s = string.Format(dnSpy_Debugger_Resources.ExceptionInfoFormat, msg, exTypeString, exMsg, hr ?? -1);
			sb.Append(s);
		}

		public bool CanDebugCurrentAssembly(object parameter) {
			return GetCurrentExecutableAssembly(parameter as IMenuItemContext) != null;
		}

		public void DebugCurrentAssembly(object parameter) {
			var asm = GetCurrentExecutableAssembly(parameter as IMenuItemContext);
			if (asm == null)
				return;
			DebugAssembly2(GetDebugAssemblyOptions(CreateDebugProcessVM(asm)));
		}

		public IDnSpyFile GetCurrentExecutableAssembly(IMenuItemContext context) {
			if (context == null)
				return null;
			if (IsDebugging)
				return null;

			IFileTreeNodeData node;
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID)) {
				var uiContext = context.Find<ITextEditorUIContext>();
				if (uiContext == null)
					return null;
				var nodes = uiContext.FileTab.Content.Nodes.ToArray();
				if (nodes.Length == 0)
					return null;
				node = nodes[0];
			}
			else if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID)) {
				var nodes = context.Find<IFileTreeNodeData[]>();
				if (nodes == null || nodes.Length == 0)
					return null;
				node = nodes[0];
			}
			else
				return null;

			return GetCurrentExecutableAssembly(node, true);
		}

		static IDnSpyFile GetCurrentExecutableAssembly(ITreeNodeData node, bool mustBeNetExe) {
			var fileNode = (node as IFileTreeNodeData).GetDnSpyFileNode();
			if (fileNode == null)
				return null;

			var file = fileNode.DnSpyFile;
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

		IDnSpyFile GetCurrentExecutableAssembly(bool mustBeNetExe) {
			return GetCurrentExecutableAssembly(fileTabManager.FileTreeView.TreeView.SelectedItem, mustBeNetExe);
		}

		public bool CanStartWithoutDebugging {
			get { return !IsDebugging && GetCurrentExecutableAssembly(false) != null; }
		}

		public void StartWithoutDebugging() {
			var asm = GetCurrentExecutableAssembly(false);
			if (asm == null || !File.Exists(asm.Filename))
				return;
			try {
				Process.Start(asm.Filename);
			}
			catch (Exception ex) {
				messageBoxManager.Show(string.Format(dnSpy_Debugger_Resources.Error_StartWithoutDebuggingCouldNotStart, asm.Filename, ex.Message));
			}
		}

		DebugCoreCLRVM CreateDebugCoreCLRVM(IDnSpyFile asm = null) {
			// Re-use the previous one if it's the same file
			if (lastDebugCoreCLRVM != null && asm != null) {
				if (StringComparer.OrdinalIgnoreCase.Equals(lastDebugCoreCLRVM.Filename, asm.Filename))
					return lastDebugCoreCLRVM.Clone();
			}

			var vm = new DebugCoreCLRVM();
			if (asm != null)
				vm.Filename = asm.Filename;
			vm.DbgShimFilename = debuggerSettings.CoreCLRDbgShimFilename;
			vm.BreakProcessKind = debuggerSettings.BreakProcessKind;
			return vm;
		}

		public bool CanDebugCoreCLRAssembly {
			get { return !IsDebugging; }
		}

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

		DebugProcessVM CreateDebugProcessVM(IDnSpyFile asm = null) {
			// Re-use the previous one if it's the same file
			if (lastDebugProcessVM != null && asm != null) {
				if (StringComparer.OrdinalIgnoreCase.Equals(lastDebugProcessVM.Filename, asm.Filename))
					return lastDebugProcessVM.Clone();
			}

			var vm = new DebugProcessVM();
			if (asm != null)
				vm.Filename = asm.Filename;
			vm.BreakProcessKind = debuggerSettings.BreakProcessKind;
			return vm;
		}

		public bool CanDebugAssembly {
			get { return !IsDebugging; }
		}

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
			opts.DebugOptions.IgnoreBreakInstructions = this.debuggerSettings.IgnoreBreakInstructions;
			lastDebugProcessVM = vm;
			return opts;
		}

		public bool CanRestart {
			get { return IsDebugging && lastDebugProcessOptions != null && !HasAttached; }
		}

		public void Restart() {
			if (!CanRestart)
				return;

			var oldOpts = lastDebugProcessOptions;
			Stop();
			TheDebugger.RemoveAndRaiseEvent();
			lastDebugProcessOptions = oldOpts;

			DebugAssembly2(lastDebugProcessOptions);
		}

		public bool DebugAssembly(DebugProcessOptions options) {
			return DebugAssembly2(options, false);
		}

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

		public bool CanAttach {
			get { return !IsDebugging; }
		}

		public bool Attach() {
			if (!CanAttach)
				return false;

			var data = new AttachProcessVM(Dispatcher.CurrentDispatcher, debuggerSettings.SyntaxHighlightAttach);
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
					messageBoxManager.Show(errMsg);
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
			TheDebugger.Initialize(newDebugger);

			return true;
		}

		public bool CanBreak {
			get { return ProcessState == DebuggerProcessState.Starting || ProcessState == DebuggerProcessState.Running; }
		}

		public void Break() {
			if (!CanBreak)
				return;

			int hr = TheDebugger.Debugger.TryBreakProcesses();
			if (hr < 0)
				messageBoxManager.Show(string.Format(dnSpy_Debugger_Resources.Error_CouldNotBreakProcess, hr));
		}

		public bool CanStop {
			get { return IsDebugging; }
		}

		public void Stop() {
			if (!CanStop)
				return;
			TheDebugger.Debugger.TerminateProcesses();
		}

		public bool CanDetach {
			get { return ProcessState != DebuggerProcessState.Continuing && ProcessState != DebuggerProcessState.Terminated; }
		}

		public void Detach() {
			if (!CanDetach)
				return;
			int hr = TheDebugger.Debugger.TryDetach();
			if (hr < 0)
				messageBoxManager.Show(string.Format(dnSpy_Debugger_Resources.Error_CouldNotDetachProcess, hr));
		}

		public bool CanContinue {
			get { return ProcessState == DebuggerProcessState.Paused; }
		}

		public void Continue() {
			if (!CanContinue)
				return;
			TheDebugger.Debugger.Continue();
		}

		public void JumpToCurrentStatement(IFileTab tab) {
			JumpToCurrentStatement(tab, true);
		}

		void JumpToCurrentStatement(IFileTab tab, bool canRefreshMethods) {
			if (tab == null)
				return;
			if (currentMethod == null)
				return;

			// The file could've been added lazily to the list so add a short delay before we select it
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				tab.FollowReference(currentMethod, false, e => {
					Debug.Assert(e.Tab == tab);
					Debug.Assert(e.Tab.UIContext is ITextEditorUIContext);
					if (e.Success && !e.HasMovedCaret) {
						MoveCaretToCurrentStatement(e.Tab.UIContext as ITextEditorUIContext, canRefreshMethods);
						e.HasMovedCaret = true;
					}
				});
			}));
		}

		bool MoveCaretToCurrentStatement(ITextEditorUIContext uiContext, bool canRefreshMethods) {
			if (uiContext == null)
				return false;
			if (currentLocation == null)
				return false;
			if (DebugUtils.MoveCaretTo(uiContext, currentLocation.Value.SerializedDnToken, currentLocation.Value.Offset))
				return true;
			if (!canRefreshMethods)
				return false;

			RefreshMethodBodies(uiContext);

			return false;
		}

		void RefreshMethodBodies(ITextEditorUIContext uiContext) {
			if (currentLocation == null)
				return;
			if (currentMethod == null)
				return;
			if (uiContext == null)
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
			var modNode = fileTabManager.FileTreeView.FindNode(mod);
			if (modNode == null)
				return;
			var memFile = modNode.DnSpyFile as MemoryModuleDefFile;
			IDnSpyFile file = memFile;
			if (memFile == null) {
				if (modNode.DnSpyFile is CorModuleDefFile)
					return;
				var corMod = currentLocation.Value.Function.Module;
				if (corMod == null || corMod.IsDynamic)
					return;
				var dnMod = moduleLoader.Value.GetDnModule(corMod);
				file = inMemoryModuleManager.Value.LoadFile(dnMod, true);
				Debug.Assert(file != null);
				memFile = file as MemoryModuleDefFile;
			}
			if (file == null)
				return;
			// It's null if we couldn't load the file from memory because the PE / COR20 headers
			// are corrupt (eg. an obfuscator overwrote various fields with garbage). In that case,
			// file is a CorModuleDefFile and it's using the MD API to read the MD.
			if (memFile != null)
				inMemoryModuleManager.Value.UpdateModuleMemory(memFile);
			UpdateCurrentMethod(file);
			JumpToCurrentStatement(uiContext.FileTab, false);
		}

		void UpdateCurrentLocationToInMemoryModule() {
			UpdateCurrentMethod();
			if (currentMethod != null && currentLocation != null)
				JumpToCurrentStatement(fileTabManager.GetOrCreateActiveTab());
		}

		struct CodeLocation {
			public readonly CorFunction Function;
			public uint Offset;
			public CorDebugMappingResult Mapping;

			public uint Token {
				get { return Function.Token; }
			}

			public bool IsExact {
				get { return (Mapping & CorDebugMappingResult.MAPPING_EXACT) != 0; }
			}

			public bool IsApproximate {
				get { return (Mapping & CorDebugMappingResult.MAPPING_APPROXIMATE) != 0; }
			}

			public SerializedDnToken SerializedDnToken {
				get {
					var mod = Function.Module;
					if (mod == null)
						return new SerializedDnToken();
					return new SerializedDnToken(mod.SerializedDnModule, Function.Token);
				}
			}

			public CodeLocation(CorFunction func, uint offset, CorDebugMappingResult mapping) {
				this.Function = func;
				this.Offset = offset;
				this.Mapping = mapping;
			}

			public static bool SameMethod(CodeLocation a, CodeLocation b) {
				return a.Function == b.Function;
			}
		}

		void UpdateCurrentLocation() {
			UpdateCurrentLocation(TheDebugger.Debugger.Current.ILFrame);
		}

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

		void UpdateCurrentMethod(IDnSpyFile file = null) {
			if (currentLocation == null) {
				currentMethod = null;
				return;
			}

			if (file == null)
				file = moduleLoader.Value.LoadModule(currentLocation.Value.Function.Module, true);
			Debug.Assert(file != null);
			var loadedMod = file == null ? null : file.ModuleDef;
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

			MemberMapping mapping;
			var tab = fileTabManager.GetOrCreateActiveTab();
			var uiContext = tab.TryGetTextEditorUIContext();
			var cm = uiContext.GetCodeMappings();
			if ((mapping = cm.TryGetMapping(key.Value)) == null) {
				// User has decompiled some other code or switched to another tab
				UpdateCurrentMethod();
				JumpToCurrentStatement(tab);

				// It could be cached and immediately available. Check again
				uiContext = tab.TryGetTextEditorUIContext();
				cm = uiContext.GetCodeMappings();
				if ((mapping = cm.TryGetMapping(key.Value)) == null)
					return null;
			}

			bool isMatch;
			var scm = mapping.GetInstructionByOffset(frame.GetILOffset(moduleLoader.Value), out isMatch);
			uint[] ilRanges;
			if (scm == null)
				ilRanges = mapping.ToArray(null, false);
			else
				ilRanges = scm.ToArray(isMatch);

			if (ilRanges.Length == 0)
				return null;
			return CreateStepRanges(ilRanges);
		}

		static StepRange[] CreateStepRanges(uint[] ilRanges) {
			var stepRanges = new StepRange[ilRanges.Length / 2];
			if (stepRanges.Length == 0)
				return null;
			for (int i = 0; i < stepRanges.Length; i++)
				stepRanges[i] = new StepRange(ilRanges[i * 2], ilRanges[i * 2 + 1]);
			return stepRanges;
		}

		static SerializedDnToken? CreateMethodKey(DnDebugger debugger, CorFrame frame) {
			var sma = frame.SerializedDnModule;
			if (sma == null)
				return null;

			return new SerializedDnToken(sma.Value, frame.Token);
		}

		CorFrame GetCurrentILFrame() {
			if (ProcessState != DebuggerProcessState.Paused)
				return null;
			return stackFrameManager.FirstILFrame;
		}

		CorFrame GetCurrentMethodILFrame() {
			if (ProcessState != DebuggerProcessState.Paused)
				return null;
			return stackFrameManager.SelectedFrame;
		}

		public bool CanStepInto() {
			return CanStepInto(GetCurrentILFrame());
		}

		public bool CanStepInto(CorFrame frame) {
			return ProcessState == DebuggerProcessState.Paused && frame != null;
		}

		public void StepInto() {
			StepInto(GetCurrentILFrame());
		}

		public void StepInto(CorFrame frame) {
			if (!CanStepInto(frame))
				return;

			var ranges = GetStepRanges(TheDebugger.Debugger, frame, true);
			TheDebugger.Debugger.StepInto(frame, ranges);
		}

		public bool CanStepOver() {
			return CanStepOver(GetCurrentILFrame());
		}

		public bool CanStepOver(CorFrame frame) {
			return ProcessState == DebuggerProcessState.Paused && frame != null;
		}

		public void StepOver() {
			StepOver(GetCurrentILFrame());
		}

		public void StepOver(CorFrame frame) {
			if (!CanStepOver(frame))
				return;

			var ranges = GetStepRanges(TheDebugger.Debugger, frame, false);
			TheDebugger.Debugger.StepOver(frame, ranges);
		}

		public bool CanStepOut() {
			return CanStepOut(GetCurrentILFrame());
		}

		public bool CanStepOut(CorFrame frame) {
			return ProcessState == DebuggerProcessState.Paused && frame != null;
		}

		public void StepOut() {
			StepOut(GetCurrentILFrame());
		}

		public void StepOut(CorFrame frame) {
			if (!CanStepOut(frame))
				return;

			TheDebugger.Debugger.StepOut(frame);
		}

		public bool CanRunTo(CorFrame frame) {
			return ProcessState == DebuggerProcessState.Paused && TheDebugger.Debugger.CanRunTo(frame);
		}

		public bool RunTo(CorFrame frame) {
			if (!CanRunTo(frame))
				return false;

			return TheDebugger.Debugger.RunTo(frame);
		}

		public bool CanShowNextStatement {
			get { return ProcessState == DebuggerProcessState.Paused && GetCurrentILFrame() != null; }
		}

		public void ShowNextStatement() {
			if (!CanShowNextStatement)
				return;

			var tab = fileTabManager.GetOrCreateActiveTab();
			if (!TryShowNextStatement(tab.TryGetTextEditorUIContext())) {
				UpdateCurrentMethod();
				JumpToCurrentStatement(tab);
			}
		}

		bool TryShowNextStatement(ITextEditorUIContext uiContext) {
			// Always reset the selected frame
			stackFrameManager.SelectedFrameNumber = 0;
			if (currentLocation == null)
				return false;
			return DebugUtils.MoveCaretTo(uiContext, currentLocation.Value.SerializedDnToken, currentLocation.Value.Offset);
		}

		ITextEditorUIContext TryGetTextEditorUIContext(object parameter) {
			var ctx = parameter as IMenuItemContext;
			if (ctx == null)
				return null;
			if (ctx.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID)) {
				var tab = ctx.CreatorObject.Object as IFileTab;
				return tab == null ? null : tab.UIContext as ITextEditorUIContext;
			}
			return null;
		}

		public bool CanSetNextStatement(object parameter) {
			if (!IsDebugging)
				return false;

			SourceCodeMapping mapping;
			string errMsg;
			if (!DebugGetSourceCodeMappingForSetNextStatement(TryGetTextEditorUIContext(parameter), out errMsg, out mapping))
				return false;

			return CanSetNextStatement(mapping.ILRange.From);
		}

		bool CanSetNextStatement(uint ilOffset) {
			return CanSetNextStatement(currentLocation, ilOffset);
		}

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
				messageBoxManager.Show(errMsg);
				return false;
			}

			return true;
		}

		bool DebugSetNextStatement(object parameter, out string errMsg) {
			SourceCodeMapping mapping;
			if (!DebugGetSourceCodeMappingForSetNextStatement(TryGetTextEditorUIContext(parameter), out errMsg, out mapping))
				return false;
			return SetOffset(mapping.ILRange.From, out errMsg);
		}

		public bool SetOffset(uint ilOffset, out string errMsg) {
			return SetOffset(currentLocation, ilOffset, GetCurrentMethodILFrame(), out errMsg);
		}

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

		public bool SetNativeOffset(uint ilOffset, out string errMsg) {
			return SetNativeOffset(ilOffset, GetCurrentMethodILFrame(), out errMsg);
		}

		public bool SetNativeOffset(CorFrame frame, uint ilOffset, out string errMsg) {
			return SetNativeOffset(ilOffset, frame, out errMsg);
		}

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

		bool DebugGetSourceCodeMappingForSetNextStatement(ITextEditorUIContext uiContext, out string errMsg, out SourceCodeMapping mapping) {
			errMsg = string.Empty;
			mapping = null;

			if (ProcessState == DebuggerProcessState.Terminated) {
				errMsg = dnSpy_Debugger_Resources.Error_NotDebugging;
				return false;
			}
			if (ProcessState == DebuggerProcessState.Starting || ProcessState == DebuggerProcessState.Continuing || ProcessState == DebuggerProcessState.Running) {
				errMsg = dnSpy_Debugger_Resources.Error_CantSetNextStatementWhenNotStopped;
				return false;
			}

			if (uiContext == null) {
				uiContext = fileTabManager.ActiveTab.TryGetTextEditorUIContext();
				if (uiContext == null) {
					errMsg = dnSpy_Debugger_Resources.Error_NoTabAvailableDecompileCurrentMethod;
					return false;
				}
			}

			CodeMappings cm;
			if (currentLocation == null || !DebugUtils.VerifyAndGetCurrentDebuggedMethod(uiContext, currentLocation.Value.SerializedDnToken, out cm)) {
				errMsg = dnSpy_Debugger_Resources.Error_NoDebugInfoAvailable;
				return false;
			}
			Debug.Assert(currentLocation != null);

			var location = uiContext.Location;
			var bps = cm.Find(location.Line, location.Column);
			if (bps.Count == 0) {
				errMsg = dnSpy_Debugger_Resources.Error_CantSetNextStatementHere;
				return false;
			}

			if (GetCurrentMethodILFrame() == null) {
				errMsg = dnSpy_Debugger_Resources.Error_CantSetNextStatementNoILFrame;
				return false;
			}

			foreach (var bp in bps) {
				var md = bp.Mapping.Method;
				if (currentLocation.Value.Function.Token != md.MDToken.Raw)
					continue;
				var serAsm = serializedDnModuleCreator.Create(md.Module);
				if (!serAsm.Equals(currentLocation.Value.SerializedDnToken.Module))
					continue;

				mapping = bp;
				break;
			}
			if (mapping == null) {
				errMsg = dnSpy_Debugger_Resources.Error_CantSetNextStatementToAnotherMethod;
				return false;
			}

			return true;
		}
	}
}
