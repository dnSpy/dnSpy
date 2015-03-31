// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Xml.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.ILSpy.Debugger.UI;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using Microsoft.Win32;
using dnlib.DotNet;

using NR = ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.Debugger.Commands
{
	[Export(typeof(IPlugin))]
	public class DebuggerPlugin : IPlugin
	{
		#region Static members
		[DllImport("user32.dll")]
		static extern bool SetWindowPos(
			IntPtr hWnd,
			IntPtr hWndInsertAfter,
			int X,
			int Y,
			int cx,
			int cy,
			uint uFlags);
		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		const UInt32 SWP_NOSIZE = 0x0001;
		const UInt32 SWP_NOMOVE = 0x0002;

		static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
		static readonly IntPtr HWND_TOP = new IntPtr(0);

		static void SendWpfWindowPos(Window window, IntPtr place)
		{
			var hWnd = new WindowInteropHelper(window).Handle;
			SetWindowPos(hWnd, place, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
		}
		#endregion

		sealed class SavedDebuggedOptions
		{
			public static readonly SavedDebuggedOptions Instance = new SavedDebuggedOptions();

			public string Executable;
			public string WorkingDirectory;
			public string Arguments;
			public bool BreakAtBeginning;

			public static SavedDebuggedOptions ShowDebugExecutableDialogBox(string fileName, out bool? result)
			{
				var window = new DebugProcessWindow {
					Owner = MainWindow.Instance
				};
				var fn = fileName ?? Instance.Executable;
				if (fn != null) {
					window.SelectedExecutable = fn;
					if (fileName == null && !string.IsNullOrEmpty(Instance.WorkingDirectory))
						window.WorkingDirectory = Instance.WorkingDirectory;
				}
				window.Arguments = Instance.Arguments ?? string.Empty;
				window.BreakAtBeginning = DebuggerSettings.Instance.BreakAtBeginning;

				result = window.ShowDialog();
				if (result == true) {
					Instance.Executable = window.SelectedExecutable;
					Instance.WorkingDirectory = window.WorkingDirectory;
					Instance.Arguments = window.Arguments;
					Instance.BreakAtBeginning = window.BreakAtBeginning;
				}
				return Instance;
			}
		}

		public static bool Start()
		{
			if (DebuggerService.CurrentDebugger == null || DebuggerService.CurrentDebugger.IsDebugging)
				return false;

			if (DebuggerSettings.Instance.AskForArguments) {
				bool? result;
				var debugOpts = SavedDebuggedOptions.ShowDebugExecutableDialogBox(null, out result);
				if (result == true) {
					MainWindow.Instance.OpenFiles(new[] { debugOpts.Executable }, false);
					DebuggerPlugin.StartExecutable(debugOpts.Executable, debugOpts.WorkingDirectory, debugOpts.Arguments, debugOpts.BreakAtBeginning);
					return true;
				}
			}
			else {
				OpenFileDialog dialog = new OpenFileDialog() {
					Filter = ".NET Executable (*.exe) | *.exe",
					RestoreDirectory = true,
					DefaultExt = "exe"
				};
				if (dialog.ShowDialog() == true) {
					MainWindow.Instance.OpenFiles(new[] { dialog.FileName }, false);
					DebuggerPlugin.StartExecutable(dialog.FileName, null, null, DebuggerSettings.Instance.BreakAtBeginning);
					return true;
				}
			}
			return false;
		}

		public static bool Start(string filename)
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger == null || debugger.IsDebugging)
				return false;

			if (DebuggerSettings.Instance.AskForArguments) {
				bool? result;
				var debugOpts = SavedDebuggedOptions.ShowDebugExecutableDialogBox(filename, out result);
				if (result == true) {
					DebuggerPlugin.StartExecutable(debugOpts.Executable, debugOpts.WorkingDirectory, debugOpts.Arguments, debugOpts.BreakAtBeginning);
					return true;
				}
				return false;
			}
			else {
				DebuggerPlugin.StartExecutable(filename, null, null, DebuggerSettings.Instance.BreakAtBeginning);
				return true;
			}
		}

		public static bool Stop()
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger == null || !debugger.IsDebugging)
				return false;

			debugger.Stop();
			return true;
		}

		public static bool RestartPossible()
		{
			return DebuggerService.CurrentDebugger != null &&
				DebuggerService.CurrentDebugger.IsDebugging;
		}

		public static bool Restart()
		{
			if (!Stop())
				return false;

			var inst = SavedDebuggedOptions.Instance;
			MainWindow.Instance.OpenFiles(new[] { inst.Executable }, false);
			DebuggerPlugin.StartExecutable(inst.Executable, inst.WorkingDirectory, inst.Arguments, inst.BreakAtBeginning);
			return true;
		}

		public static bool Attach()
		{
			if (DebuggerService.CurrentDebugger == null || DebuggerService.CurrentDebugger.IsDebugging)
				return false;

			MainWindow.Instance.ShowIgnorableMessageBox("debug: attach warning",
					"Warning: When attaching to an application, some local variables might not be available. If possible, use the \"Debug an Executable\" command.",
					MessageBoxButton.OK);

			var window = new AttachToProcessWindow { Owner = MainWindow.Instance };
			if (window.ShowDialog() == true) {
				DebuggerPlugin.StartAttaching(window.SelectedProcess);
				return true;
			}

			return false;
		}

		public static void StartExecutable(string fileName, string workingDirectory, string arguments, bool breakAtBeginning)
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger == null)
				return;
			debugger.BreakAtBeginning = breakAtBeginning;
			DebugStarted();
			debugger.Start(new ProcessStartInfo {
			                      	FileName = fileName,
			                      	WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(fileName),
			                      	Arguments = arguments ?? string.Empty,
			                      });
		}

		public static void StartAttaching(Process process)
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger == null)
				return;
			debugger.BreakAtBeginning = DebuggerSettings.Instance.BreakAtBeginning;
			DebugStarted();
			debugger.Attach(process);
		}

		public static bool Detach()
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger == null)
				return false;
			if (debugger.IsDebugging) {
				debugger.Detach();

				DebuggerPlugin.EnableDebuggerUI(true);
				DebuggerService.DebugStopped -= OnDebugStopped;
				return true;
			}

			return false;
		}

		static void DebugStarted()
		{
			EnableDebuggerUI(false);
			DebuggerService.DebugStopped += OnDebugStopped;
			DebuggerService.ProcessRunningChanged += OnProcessRunningChanged;
			
			MainWindow.Instance.SetStatus("Running...", Brushes.Black);
		}

		static void OnDebugStopped(object sender, EventArgs e)
		{
			EnableDebuggerUI(true);
			DebuggerService.DebugStopped -= OnDebugStopped;
			DebuggerService.ProcessRunningChanged -= OnProcessRunningChanged;
			
			MainWindow.Instance.HideStatus();
		}

		static void EnableDebuggerUI(bool enable)
		{
			// internal types
			if (enable)
				MainWindow.Instance.SessionSettings.FilterSettings.ShowInternalApi = true;
		}

		static void OnProcessRunningChanged(object sender, EventArgs e)
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger == null)
				return;
			if (debugger.IsProcessRunning) {
				//SendWpfWindowPos(this, HWND_BOTTOM);
				MainWindow.Instance.SetStatus("Running...", Brushes.Black);
				return;
			}
			
			var inst = MainWindow.Instance;
			
			// breakpoint was hit => bring to front the main window
			SendWpfWindowPos(inst, HWND_TOP); inst.Activate();
			
			if (DebugInformation.MustJumpToReference)
				DebugUtils.JumpToCurrentStatement(MainWindow.Instance.SafeActiveTextView);
			
			inst.SetStatus("Debugging...", Brushes.Red);
		}

		void IPlugin.OnLoaded()
		{
			MainWindow.Instance.PreviewKeyDown += OnPreviewKeyDown;
			MainWindow.Instance.KeyDown += OnKeyDown;
			MainWindow.Instance.Closing += OnClosing;
			BreakpointSettings.Instance.Load();
			new BringDebuggedProgramWindowToFront();
		}

		void OnClosing(object sender, CancelEventArgs e)
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger != null && debugger.IsDebugging) {
				var result = MainWindow.Instance.ShowIgnorableMessageBox("debug: exit program", "Do you want to stop debugging?", MessageBoxButton.YesNo);
				if (result == MsgBoxButton.None || result == MsgBoxButton.No)
					e.Cancel = true;
			}
		}

		sealed class BringDebuggedProgramWindowToFront
		{
			// Millisecs to wait before we bring the debugged process' window to the front
			const int WAIT_TIME_MS = 1000;

			public BringDebuggedProgramWindowToFront()
			{
				DebuggerService.ProcessRunningChanged += DebuggerService_ProcessRunningChanged;
			}

			bool isRunning;
			int isRunningId;

			void DebuggerService_ProcessRunningChanged(object sender, EventArgs e)
			{
				var debugger = DebuggerService.CurrentDebugger;
				bool newIsRunning = debugger != null && debugger.IsProcessRunning;
				if (newIsRunning == isRunning)
					return;

				isRunning = newIsRunning;
				int id = Interlocked.Increment(ref isRunningId);
				if (!isRunning)
					return;

				var process = GetProcessById(debugger.DebuggedProcessId);
				if (process == null)
					return;

				Timer timer = null;
				timer = new Timer(a => {
					timer.Dispose();
					if (id == isRunningId)
						SwitchToDebuggedProcessWindow(process);
				}, null, WAIT_TIME_MS, Timeout.Infinite);
			}

			Process GetProcessById(int pid)
			{
				try {
					return Process.GetProcessById(pid);
				}
				catch {
				}
				return null;
			}

			void SwitchToDebuggedProcessWindow(Process process)
			{
				try {
					var hWnd = process.MainWindowHandle;
					if (hWnd != IntPtr.Zero)
						SetForegroundWindow(hWnd);
				}
				catch {
				}
			}
		}

		void OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			var debugger = DebuggerService.CurrentDebugger;
			bool debugging = debugger != null && debugger.IsDebugging;

			if (debugging && Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F5) {
				DebugContinue();
				e.Handled = true;
				return;
			}
		}

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			var debugger = DebuggerService.CurrentDebugger;
			bool debugging = debugger != null && debugger.IsDebugging;

			if (debugging && Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.F5) {
				Stop();
				e.Handled = true;
				return;
			}
			if (debugging && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.F5) {
				if (RestartPossible())
					Restart();
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F9) {
				DebugToggleBreakpoint();
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.F9) {
				if (DebugEnableDisableBreakpointPossible())
					DebugEnableDisableBreakpoint();
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.F9) {
				if (DebugDeleteAllBreakpointsPossible())
					DebugDeleteAllBreakpoints();
				e.Handled = true;
				return;
			}
			if (debugging && Keyboard.Modifiers == ModifierKeys.None && (e.SystemKey == Key.F10 ? e.SystemKey : e.Key) == Key.F10) {
				DebugStepOver();
				e.Handled = true;
				return;
			}
			if (debugging && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && (e.SystemKey == Key.F10 ? e.SystemKey : e.Key) == Key.F10) {
				if (DebugCanSetNextStatement())
					DebugSetNextStatement();
				else {
					// Show the error message
					SourceCodeMapping mapping;
					string errMsg;
					if (!DebugGetSourceCodeMappingForSetNextStatement(out errMsg, out mapping))
						MessageBox.Show(MainWindow.Instance, errMsg);
				}
				e.Handled = true;
				return;
			}
			if (debugging && Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F11) {
				DebugStepInto();
				e.Handled = true;
				return;
			}
			if (debugging && Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.F11) {
				DebugStepOut();
				e.Handled = true;
				return;
			}
			if (debugging && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Cancel) {
				DebugBreak();
				e.Handled = true;
				return;
			}
			if (debugging && Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Multiply) {
				if (DebugShowNextStatementPossible())
					DebugShowNextStatement();
				e.Handled = true;
				return;
			}
		}

		public static bool DebugContinue()
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger != null && debugger.IsDebugging && !debugger.IsProcessRunning) {
				StackFrameStatementManager.Remove(true);
				debugger.Continue();
				MainWindow.Instance.SetStatus("Running...", Brushes.Black);
				return true;
			}

			return false;
		}

		public static bool DebugBreak()
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger != null && debugger.IsDebugging && debugger.IsProcessRunning) {
				debugger.Break();
				MainWindow.Instance.SetStatus("Debugging...", Brushes.Red);
				return true;
			}

			return false;
		}

		public static bool DebugStepInto()
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger != null && debugger.IsDebugging && !debugger.IsProcessRunning) {
				debugger.StepInto();
				return true;
			}

			return false;
		}

		public static bool DebugStepOver()
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger != null && debugger.IsDebugging && !debugger.IsProcessRunning) {
				debugger.StepOver();
				return true;
			}
			return false;
		}

		public static bool DebugStepOut()
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger != null && debugger.IsDebugging && !debugger.IsProcessRunning) {
				debugger.StepOut();
				return true;
			}

			return false;
		}

		public static bool DebugDeleteAllBreakpoints()
		{
			var res = MainWindow.Instance.ShowIgnorableMessageBox("debug: delete all bps", "Do you want to delete all breakpoints?", MessageBoxButton.YesNo);
			if (res != null && res != MsgBoxButton.OK)
				return false;

			for (int i = BookmarkManager.Bookmarks.Count - 1; i >= 0; --i) {
				var bookmark = BookmarkManager.Bookmarks[i];
				if (bookmark is BreakpointBookmark) {
					BookmarkManager.RemoveMark(bookmark);
				}
			}
			return true;
		}

		public static bool DebugDeleteAllBreakpointsPossible()
		{
			return BookmarkManager.Bookmarks.Any(b => b is BreakpointBookmark);
		}

		public static bool DebugToggleBreakpoint()
		{
			if (DebuggerService.CurrentDebugger != null) {
				var textView = MainWindow.Instance.ActiveTextView;
				if (textView == null)
					return false;
				var location = textView.TextEditor.TextArea.Caret.Location;
				BreakpointHelper.Toggle(textView, location.Line, location.Column);
				return true;
			}

			return false;
		}

		public static bool DebugEnableDisableBreakpoint()
		{
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null)
				return false;
			var location = textView.TextEditor.TextArea.Caret.Location;
			var bpms = BreakpointHelper.GetBreakpointBookmarks(textView, location.Line, location.Column);
			bool isEnabled = bpms.IsEnabled();
			foreach (var bpm in bpms)
				bpm.IsEnabled = !isEnabled;
			return bpms.Count > 0;
		}

		public static bool DebugEnableDisableBreakpointPossible()
		{
			return DebuggerService.CurrentDebugger != null && HasBPAtCurrentCaretPosition();
		}

		public static bool HasBPAtCurrentCaretPosition()
		{
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null)
				return false;
			var location = textView.TextEditor.TextArea.Caret.Location;
			return BreakpointHelper.GetBreakpointBookmarks(textView, location.Line, location.Column).Count != 0;
		}

		public static bool DebugShowNextStatement()
		{
			var textView = MainWindow.Instance.SafeActiveTextView;
			if (!TryShowNextStatement(textView)) {
				DebugUtils.JumpToCurrentStatement(textView);
				return true;
			}

			return false;
		}

		public static bool DebugShowNextStatementPossible()
		{
			return DebuggerService.CurrentDebugger != null &&
			DebuggerService.CurrentDebugger.IsDebugging &&
			!DebuggerService.CurrentDebugger.IsProcessRunning;
		}

		static bool TryShowNextStatement(DecompilerTextView textView)
		{
			if (!DebugShowNextStatementPossible())
				return false;

			// Always reset the selected frame
			StackFrameStatementManager.SelectedFrame = 0;

			if (textView == null)
				return false;

			Tuple<MethodKey, int, IMemberRef> info;
			MethodKey currentKey;
			Dictionary<MethodKey, MemberMapping> cm;
			if (!DebugUtils.VerifyAndGetCurrentDebuggedMethod(textView, out info, out currentKey, out cm))
				return false;

			NR.TextLocation location, endLocation;
			if (!cm[currentKey].GetInstructionByTokenAndOffset((uint)info.Item2, out location, out endLocation))
				return false;

			textView.ScrollAndMoveCaretTo(location.Line, location.Column);
			return true;
		}

		public static bool DebugCanSetNextStatement()
		{
			if (DebuggerService.CurrentDebugger == null ||
				!DebuggerService.CurrentDebugger.IsDebugging ||
				DebuggerService.CurrentDebugger.IsProcessRunning)
				return false;

			SourceCodeMapping mapping;
			string errMsg;
			if (!DebugGetSourceCodeMappingForSetNextStatement(out errMsg, out mapping))
				return false;

			var info = DebugInformation.DebugStepInformation;
			return info == null || info.Item2 != mapping.ILInstructionOffset.From;
		}

		public static bool DebugSetNextStatement()
		{
			string errMsg;
			if (!DebugSetNextStatement(out errMsg)) {
				if (string.IsNullOrEmpty(errMsg))
					errMsg = "Could not set next statement (unknown reason)";
				MessageBox.Show(MainWindow.Instance, errMsg);
				return false;
			}

			return true;
		}

		public static bool DebugSetNextStatement(out string errMsg)
		{
			SourceCodeMapping mapping;
			if (!DebugGetSourceCodeMappingForSetNextStatement(out errMsg, out mapping))
				return false;

			int ilOffset = (int)mapping.ILInstructionOffset.From;
			if (!DebuggerService.CurrentDebugger.SetInstructionPointer(ilOffset)) {
				errMsg = "Setting the next statement failed.";
				return false;
			}

			return true;
		}

		public static bool DebugGetSourceCodeMappingForSetNextStatement(out string errMsg, out SourceCodeMapping mapping)
		{
			errMsg = string.Empty;
			mapping = null;

			if (DebuggerService.CurrentDebugger == null) {
				errMsg = "No debugger exists";
				return false;
			}
			if (!DebuggerService.CurrentDebugger.IsDebugging) {
				errMsg = "We're not debugging";
				return false;
			}
			if (DebuggerService.CurrentDebugger.IsProcessRunning) {
				errMsg = "Can't set next statement when the process is running";
				return false;
			}

			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null) {
				errMsg = "No tab is available. Decompile the current method!";
				return false;
			}

			Tuple<MethodKey, int, IMemberRef> info;
			MethodKey currentKey;
			Dictionary<MethodKey, MemberMapping> cm;
			if (!DebugUtils.VerifyAndGetCurrentDebuggedMethod(textView, out info, out currentKey, out cm)) {
				errMsg = "No debug information found. Make sure that only the debugged method is selected in the treeview (press 'Alt+Num *' to go to current statement)";
				return false;
			}

			var location = textView.TextEditor.TextArea.Caret.Location;
			var bps = BreakpointHelper.Find(cm, location.Line, location.Column);
			if (bps.Count == 0) {
				errMsg = "It's not possible to set the next statement here";
				return false;
			}

			// The method def could be different now if the debugged assembly was reloaded from disk
			// so use SigComparer and not object references to compare the methods.
			var flags = SigComparerOptions.CompareDeclaringTypes |
				SigComparerOptions.CompareAssemblyPublicKeyToken |
				SigComparerOptions.CompareAssemblyVersion |
				SigComparerOptions.CompareAssemblyLocale |
				SigComparerOptions.PrivateScopeIsComparable;
			foreach (var bp in bps) {
				if (new SigComparer(flags).Equals(bp.MemberMapping.MethodDefinition, info.Item3)) {
					mapping = bp;
					break;
				}
			}
			if (mapping == null) {
				errMsg = "The next statement cannot be set to another method";
				return false;
			}

			int ilOffset = (int)mapping.ILInstructionOffset.From;
			if (!DebuggerService.CurrentDebugger.CanSetInstructionPointer(ilOffset)) {
				errMsg = "It's not safe to set the next statement here";
				return false;
			}

			return true;
		}
	}

	public abstract class DebuggerCommand : ICommand
	{
		readonly bool? needsDebuggerActive;
		readonly bool? mustBePaused;
		bool? cachedCanExecuteState;
		bool? cachedIsVisibleState;

		public event EventHandler CanExecuteChanged;

		static void Register(DebuggerCommand cmd)
		{
			commands.Add(cmd);
		}
		static readonly List<DebuggerCommand> commands = new List<DebuggerCommand>();

		static DebuggerCommand()
		{
			DebuggerService.DebugStarting += delegate { UpdateState(); };
			DebuggerService.DebugStarted += delegate { UpdateState(); };
			DebuggerService.DebugStopped += delegate { UpdateState(); };
			DebuggerService.ProcessRunningChanged += delegate { UpdateState(); };
		}

		protected DebuggerCommand(bool? needsDebuggerActive, bool? mustBePaused = null)
		{
			this.needsDebuggerActive = needsDebuggerActive;
			this.mustBePaused = mustBePaused;
			cachedCanExecuteState = CanExecuteInternal();
			cachedIsVisibleState = IsVisibleInternal;
			Register(this);
		}

		protected static void UpdateState(DebuggerCommand cmd)
		{
			UpdateState(new[] { cmd });
		}

		static void UpdateState()
		{
			UpdateState(commands);
		}

		static void UpdateState(IList<DebuggerCommand> commands)
		{
			bool updateToolbar = false;
			bool updateMainMenu = false;
			foreach (var cmd in commands) {
				var newState = cmd.CanExecuteInternal();
				var oldState = cmd.cachedCanExecuteState;
				if (oldState.Value != newState) {
					cmd.cachedCanExecuteState = newState;

					if (cmd.CanExecuteChanged != null)
						cmd.CanExecuteChanged(cmd, EventArgs.Empty);
				}

				newState = cmd.IsVisibleInternal;
				oldState = cmd.cachedIsVisibleState;
				if (oldState.Value != newState) {
					cmd.cachedIsVisibleState = newState;

					if (cmd is IToolbarCommand)
						updateToolbar = true;
					if (cmd is IMainMenuCommand)
						updateMainMenu = true;
				}
			}
			if (updateToolbar)
				MainWindow.Instance.UpdateToolbar();
			if (updateMainMenu)
				MainWindow.Instance.UpdateMainSubMenu("_Debug");
		}

		public bool IsVisible {
			get {
				return cachedIsVisibleState.Value;
			}
		}

		protected virtual bool IsVisibleInternal {
			get {
				if (needsDebuggerActive == null)
					return true;
				return needsDebuggerActive == (DebuggerService.CurrentDebugger != null &&
												DebuggerService.CurrentDebugger.IsDebugging);
			}
		}

		public bool CanExecute(object parameter)
		{
			return cachedCanExecuteState.Value;
		}

		protected virtual bool CanExecuteInternal()
		{
			if (needsDebuggerActive == null)
				return true;
			bool b = needsDebuggerActive == (DebuggerService.CurrentDebugger != null &&
											DebuggerService.CurrentDebugger.IsDebugging);
			if (!b)
				return false;

			if (mustBePaused == null)
				return true;
			return mustBePaused == !DebuggerService.CurrentDebugger.IsProcessRunning;
		}

		public virtual void Execute(object parameter)
		{
		}
	}
	
	[ExportContextMenuEntryAttribute(Header = "_Debug Assembly",
									Icon = "Images/application-x-executable.png",
									Order = 200,
									Category = "Debug")]
	internal sealed class DebugExecutableNodeCommand : DebuggerCommand, IContextMenuEntry2
	{
		public DebugExecutableNodeCommand() : base(false)
		{
		}

		public void Initialize(TextViewContext context, MenuItem menuItem)
		{
			menuItem.Header = string.Format("_Debug {0}", ((AssemblyTreeNode)context.SelectedTreeNodes[0]).LoadedAssembly.ShortName);
		}

		public new bool IsVisible(TextViewContext context)
		{
			return DebuggerService.CurrentDebugger != null && !DebuggerService.CurrentDebugger.IsDebugging &&
				context.SelectedTreeNodes != null && context.SelectedTreeNodes.All(
				delegate (SharpTreeNode n) {
					AssemblyTreeNode a = n as AssemblyTreeNode;
					if (a == null)
						return false;
					AssemblyDef asm = a.LoadedAssembly.AssemblyDefinition;
					return asm != null && asm.ManifestModule != null && (asm.ManifestModule.ManagedEntryPoint != null || asm.ManifestModule.NativeEntryPoint != 0);
				});
		}
		
		public bool IsEnabled(TextViewContext context)
		{
			return DebuggerService.CurrentDebugger != null && !DebuggerService.CurrentDebugger.IsDebugging &&
				context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length == 1 &&
				context.SelectedTreeNodes[0] is AssemblyTreeNode;
		}
		
		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			AssemblyTreeNode n = context.SelectedTreeNodes[0] as AssemblyTreeNode;
			DebuggerPlugin.Start(n.LoadedAssembly.FileName);
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Debug an Executable",
	                      ToolbarIcon = "Images/application-x-executable.png",
	                      ToolbarCategory = "Debug1",
	                      ToolbarOrder = 0)]
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/application-x-executable.png",
	                       MenuCategory = "Start",
	                       Header = "Debug an _Executable",
	                       MenuOrder = 4000)]
	internal sealed class DebugExecutableCommand : DebuggerCommand, IToolbarCommand, IMainMenuCommand
	{
		public DebugExecutableCommand() : base(false)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.Start();
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuCategory = "Start",
	                       Header = "Attach to _Process...",
	                       MenuOrder = 4010)]
	internal sealed class AttachCommand : DebuggerCommand, IMainMenuCommand
	{
		public AttachCommand() : base(false)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.Attach();
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Continue (F5)",
						  ToolbarIcon = "Images/ContinueDebugging.png",
	                      ToolbarCategory = "Debug2",
	                      ToolbarOrder = 0)]
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/ContinueDebugging.png",
	                       MenuCategory = "Debug1",
	                       Header = "_Continue",
	                       InputGestureText = "F5",
	                       MenuOrder = 4100)]
	internal sealed class ContinueDebuggingCommand : DebuggerCommand, IToolbarCommand, IMainMenuCommand
	{
		public ContinueDebuggingCommand() : base(true, true)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugContinue();
		}
	}

	[ExportToolbarCommand(ToolTip = "Break (Ctrl+Break)",
						  ToolbarIcon = "Images/Break.png",
						  ToolbarCategory = "Debug2",
						  ToolbarOrder = 1)]
	[ExportMainMenuCommand(Menu = "_Debug",
						   MenuIcon = "Images/Break.png",
						   MenuCategory = "Debug1",
						   Header = "Brea_k",
						   InputGestureText = "Ctrl+Break",
						   MenuOrder = 4110)]
	internal sealed class BreakDebuggingCommand : DebuggerCommand, IToolbarCommand, IMainMenuCommand
	{
		public BreakDebuggingCommand() : base(true, false)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugBreak();
		}
	}

	[ExportToolbarCommand(ToolTip = "Stop Debugging (Shift+F5)",
						  ToolbarIcon = "Images/StopProcess.png",
						  ToolbarCategory = "Debug2",
						  ToolbarOrder = 2)]
	[ExportMainMenuCommand(Menu = "_Debug",
						   MenuIcon = "Images/StopProcess.png",
						   MenuCategory = "Debug1",
						   Header = "Stop D_ebugging",
						   InputGestureText = "Shift+F5",
						   MenuOrder = 4120)]
	internal sealed class StopDebuggingCommand : DebuggerCommand, IToolbarCommand, IMainMenuCommand
	{
		public StopDebuggingCommand() : base(true, null)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.Stop();
		}
	}

	[ExportToolbarCommand(ToolTip = "Restart (Ctrl+Shift+F5)",
						  ToolbarIcon = "Images/RestartProcess.png",
						  ToolbarCategory = "Debug2",
						  ToolbarOrder = 3)]
	[ExportMainMenuCommand(Menu = "_Debug",
						   MenuIcon = "Images/RestartProcess.png",
						   MenuCategory = "Debug1",
						   Header = "_Restart",
						   InputGestureText = "Ctrl+Shift+F5",
						   MenuOrder = 4140)]
	internal sealed class RestartDebuggingCommand : DebuggerCommand, IToolbarCommand, IMainMenuCommand
	{
		public RestartDebuggingCommand() : base(true, null)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.Restart();
		}
	}

	[ExportToolbarCommand(ToolTip = "Step Into (F11)",
						  ToolbarIcon = "Images/StepInto.png",
						  ToolbarCategory = "Debug3",
						  ToolbarOrder = 1)]
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/StepInto.png",
						   MenuCategory = "Debug2",
	                       Header = "Step _Into",
	                       InputGestureText = "F11",
	                       MenuOrder = 4200)]
	internal sealed class StepIntoCommand : DebuggerCommand, IToolbarCommand, IMainMenuCommand
	{
		public StepIntoCommand() : base(true, true)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugStepInto();
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Step Over (F10)",
						  ToolbarIcon = "Images/StepOver.png",
						  ToolbarCategory = "Debug3",
						  ToolbarOrder = 2)]
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/StepOver.png",
						   MenuCategory = "Debug2",
	                       Header = "Step _Over",
	                       InputGestureText = "F10",
	                       MenuOrder = 4210)]
	internal sealed class StepOverCommand : DebuggerCommand, IToolbarCommand, IMainMenuCommand
	{
		public StepOverCommand() : base(true, true)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugStepOver();
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Step Out (Shift+F11)",
						  ToolbarIcon = "Images/StepOut.png",
						  ToolbarCategory = "Debug3",
						  ToolbarOrder = 3)]
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/StepOut.png",
						   MenuCategory = "Debug2",
	                       Header = "Step Ou_t",
						   InputGestureText = "Shift+F11",
	                       MenuOrder = 4220)]
	internal sealed class StepOutCommand : DebuggerCommand, IToolbarCommand, IMainMenuCommand
	{
		public StepOutCommand() : base(true, true)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugStepOut();
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debug",
						   MenuCategory = "Debug1",
	                       Header = "Det_ach",
	                       MenuOrder = 4130)]
	internal sealed class DetachCommand : DebuggerCommand, IMainMenuCommand
	{
		public DetachCommand() : base(true)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.Detach();
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/DeleteAllBreakpoints.png",
						   MenuCategory = "Breakpoints",
						   Header = "_Delete All Breakpoints",
						   InputGestureText = "Ctrl+Shift+F9",
	                       MenuOrder = 4310)]
	internal sealed class RemoveBreakpointsCommand : DebuggerCommand, IMainMenuCommand
	{
		public RemoveBreakpointsCommand() : base(null)
		{
			BookmarkManager.Added += (sender, e) => {
				if (e.Bookmark is BreakpointBookmark)
					UpdateState(this);
			};
			BookmarkManager.Removed += (sender, e) => {
				if (e.Bookmark is BreakpointBookmark)
					UpdateState(this);
			};
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugDeleteAllBreakpoints();
		}

		protected override bool CanExecuteInternal()
		{
			return base.CanExecuteInternal() &&
				DebuggerPlugin.DebugDeleteAllBreakpointsPossible();
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuCategory = "Breakpoints",
	                       Header = "To_ggle Breakpoint",
						   InputGestureText = "F9",
	                       MenuOrder = 4300)]
	internal sealed class ToggleBreakpointCommand : DebuggerCommand, IMainMenuCommand
	{
		public ToggleBreakpointCommand() : base(null)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugToggleBreakpoint();
		}
	}

	[ExportContextMenuEntry(Icon = "images/Breakpoint.png",
							InputGestureText = "F9",
							Category = "Debug",
							Order = 210)]
	internal sealed class InsertBreakpointContextMenuEntry : IContextMenuEntry2
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.TextView != null &&
				DebuggerService.CurrentDebugger != null &&
				CanToggleBP();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return IsVisible(context);
		}

		public void Execute(TextViewContext context)
		{
			DebuggerPlugin.DebugToggleBreakpoint();
		}

		public void Initialize(TextViewContext context, MenuItem menuItem)
		{
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null)
				return;
			var location = textView.TextEditor.TextArea.Caret.Location;
			var bpms = BreakpointHelper.GetBreakpointBookmarks(textView, location.Line, location.Column);
			if (bpms.Count == 0)
				menuItem.Header = "_Add Breakpoint";
			else
				menuItem.Header = bpms.Count == 1 ? "C_lear Breakpoint" : "C_lear Breakpoints";
		}

		bool CanToggleBP()
		{
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null)
				return false;
			var location = textView.TextEditor.TextArea.Caret.Location;
			return BreakpointHelper.Find(textView, location.Line, location.Column).Count != 0;
		}
	}

	[ExportContextMenuEntry(InputGestureText = "Shift+F9",
							Category = "Debug",
							Order = 220)]
	internal sealed class DisableBreakpointContextMenuEntry : IContextMenuEntry2
	{
		public bool IsVisible(TextViewContext context)
		{
			return (context == null || context.TextView != null) &&
				DebuggerPlugin.DebugEnableDisableBreakpointPossible();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return IsVisible(context);
		}

		public void Execute(TextViewContext context)
		{
			DebuggerPlugin.DebugEnableDisableBreakpoint();
		}

		public void Initialize(TextViewContext context, MenuItem menuItem)
		{
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null)
				return;
			var location = textView.TextEditor.TextArea.Caret.Location;
			var bpms = BreakpointHelper.GetBreakpointBookmarks(textView, location.Line, location.Column);
			EnableAndDisableBreakpointCommand.InitializeMenuItem(bpms, menuItem);
		}
	}

	[ExportToolbarCommand(ToolTip = "Show Next Statement (Alt+Num *)",
						  ToolbarIcon = "Images/CurrentLineToolBar.png",
						  ToolbarCategory = "Debug3",
						  ToolbarOrder = 0)]
	internal sealed class ShowNextStatementCommand : DebuggerCommand, IToolbarCommand
	{
		public ShowNextStatementCommand() : base(true, true)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugShowNextStatement();
		}
	}

	[ExportContextMenuEntry(Icon = "images/CurrentLine.png",
							Header = "S_how Next Statement",
							InputGestureText = "Alt+Num *",
							Category = "Debug",
							Order = 230)]
	internal sealed class ShowNextStatementContextMenuEntry : IContextMenuEntry2
	{
		public bool IsVisible(TextViewContext context)
		{
			return (context == null || context.TextView != null) &&
				DebuggerPlugin.DebugShowNextStatementPossible();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return IsVisible(context);
		}

		public void Execute(TextViewContext context)
		{
			DebuggerPlugin.DebugShowNextStatement();
		}

		public void Initialize(TextViewContext context, MenuItem menuItem)
		{
		}
	}

	[ExportContextMenuEntry(Header = "Set Ne_xt Statement",
							InputGestureText = "Ctrl+Shift+F10",
							Category = "Debug",
							Order = 240)]
	internal sealed class SetNextStatementContextMenuEntry : IContextMenuEntry2
	{
		public bool IsVisible(TextViewContext context)
		{
			return (context == null || context.TextView != null) &&
				DebuggerPlugin.DebugCanSetNextStatement();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return IsVisible(context);
		}

		public void Execute(TextViewContext context)
		{
			DebuggerPlugin.DebugSetNextStatement();
		}

		public void Initialize(TextViewContext context, MenuItem menuItem)
		{
		}
	}
}
