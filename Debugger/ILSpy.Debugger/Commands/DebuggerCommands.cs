// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern bool SetWindowPos(
			IntPtr hWnd,
			IntPtr hWndInsertAfter,
			int X,
			int Y,
			int cx,
			int cy,
			uint uFlags);

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

		public static void StartExecutable(string fileName, string workingDirectory, string arguments)
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger == null)
				return;
			debugger.BreakAtBeginning = DebuggerSettings.Instance.BreakAtBeginning;
			Finish();
			debugger.Start(new ProcessStartInfo {
			                      	FileName = fileName,
			                      	WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(fileName),
			                      	Arguments = arguments
			                      });
		}

		public static void StartAttaching(Process process)
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger == null)
				return;
			debugger.BreakAtBeginning = DebuggerSettings.Instance.BreakAtBeginning;
			Finish();
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
				debugger.DebugStopped -= OnDebugStopped;
				return true;
			}

			return false;
		}

		static void Finish()
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger == null)
				return;
			EnableDebuggerUI(false);
			debugger.DebugStopped += OnDebugStopped;
			debugger.IsProcessRunningChanged += CurrentDebugger_IsProcessRunningChanged;
			
			MainWindow.Instance.SetStatus("Running...", Brushes.Black);
		}

		static void OnDebugStopped(object sender, EventArgs e)
		{
			var debugger = DebuggerService.CurrentDebugger;
			if (debugger == null)
				return;
			EnableDebuggerUI(true);
			debugger.DebugStopped -= OnDebugStopped;
			debugger.IsProcessRunningChanged -= CurrentDebugger_IsProcessRunningChanged;
			
			MainWindow.Instance.HideStatus();
			CallStackPanel.Instance.CloseIfActive();
		}

		static void EnableDebuggerUI(bool enable)
		{
			// internal types
			if (enable)
				MainWindow.Instance.SessionSettings.FilterSettings.ShowInternalApi = true;
		}

		static void CurrentDebugger_IsProcessRunningChanged(object sender, EventArgs e)
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
			
			// jump to type & expand folding
			if (DebugInformation.MustJumpToReference)
				JumpToMethod();
			
			inst.SetStatus("Debugging...", Brushes.Red);
		}

		static void JumpToMethod()
		{
			var info = DebugInformation.DebugStepInformation;
			if (info == null)
				return;
			DebugUtils.JumpToReference(info.Item3);
		}

		void IPlugin.OnLoaded()
		{
			MainWindow.Instance.KeyUp += OnKeyUp;
			MainWindow.Instance.KeyDown += OnKeyDown;
		}

		void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F5) {
				DebugContinue();
				e.Handled = true;
				return;
			}
		}

		void OnKeyDown(object sender, KeyEventArgs e)
		{
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
				DebugDeleteAllBreakpoints();
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == ModifierKeys.None && (e.SystemKey == Key.F10 ? e.SystemKey : e.Key) == Key.F10) {
				DebugStepOver();
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && (e.SystemKey == Key.F10 ? e.SystemKey : e.Key) == Key.F10) {
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
			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F11) {
				DebugStepInto();
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.F11) {
				DebugStepOut();
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Cancel) {
				DebugBreak();
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Multiply) {
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
				StackFrameStatementBookmark.Remove(true);
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
			if (MessageBox.Show(MainWindow.Instance, "Do you want to delete all breakpoints?", "dnSpy", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
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
				var location = MainWindow.Instance.TextView.TextEditor.TextArea.Caret.Location;
				BreakpointHelper.Toggle(location.Line, location.Column);
				return true;
			}

			return false;
		}

		public static bool DebugEnableDisableBreakpoint()
		{
			var location = MainWindow.Instance.TextView.TextEditor.TextArea.Caret.Location;
			var bpm = BreakpointHelper.GetBreakpointBookmark(location.Line, location.Column);
			if (bpm != null) {
				bpm.IsEnabled = !bpm.IsEnabled;
				return true;
			}

			return false;
		}

		public static bool DebugEnableDisableBreakpointPossible()
		{
			return DebuggerService.CurrentDebugger != null && HasBPAtCurrentCaretPosition();
		}

		public static bool HasBPAtCurrentCaretPosition()
		{
			var location = MainWindow.Instance.TextView.TextEditor.TextArea.Caret.Location;
			return BreakpointHelper.GetBreakpointBookmark(location.Line, location.Column) != null;
		}

		public static bool DebugShowNextStatement()
		{
			if (!TryShowNextStatement()) {
				JumpToMethod();
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

		static bool TryShowNextStatement()
		{
			if (!DebugShowNextStatementPossible())
				return false;

			// Always reset the selected frame
			StackFrameStatementBookmark.SelectedFrame = 0;

			Tuple<MethodKey, int, IMemberRef> info;
			MethodKey currentKey;
			Dictionary<MethodKey, MemberMapping> cm;
			if (!DebugUtils.VerifyAndGetCurrentDebuggedMethod(out info, out currentKey, out cm))
				return false;

			NR.TextLocation location, endLocation;
			if (!cm[currentKey].GetInstructionByTokenAndOffset((uint)info.Item2, out location, out endLocation))
				return false;

			MainWindow.Instance.TextView.ScrollAndMoveCaretTo(location.Line, location.Column);
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

			Tuple<MethodKey, int, IMemberRef> info;
			MethodKey currentKey;
			Dictionary<MethodKey, MemberMapping> cm;
			if (!DebugUtils.VerifyAndGetCurrentDebuggedMethod(out info, out currentKey, out cm)) {
				errMsg = "No debug information found. Make sure that only the debugged method is selected in the treeview (press 'Alt+Num *' to go to current statement)";
				return false;
			}

			var location = MainWindow.Instance.TextView.TextEditor.TextArea.Caret.Location;
			mapping = BreakpointHelper.Find(cm, location.Line, location.Column);
			if (mapping == null) {
				errMsg = "It's not possible to set the next statement here";
				return false;
			}
			if (mapping.MemberMapping.MethodDefinition != info.Item3) {
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

	public abstract class DebuggerCommand : SimpleCommand
	{
		readonly bool? needsDebuggerActive;
		readonly bool? mustBePaused;

		protected DebuggerCommand(bool? needsDebuggerActive, bool? mustBePaused = null)
		{
			this.needsDebuggerActive = needsDebuggerActive;
			this.mustBePaused = mustBePaused;
		}

		public override bool CanExecute(object parameter)
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
		
		public override void Execute(object parameter)
		{
		}
	}

	class SavedDebuggedOptions
	{
		static readonly SavedDebuggedOptions Instance = new SavedDebuggedOptions();

		string Executable;
		string WorkingDirectory;
		string Arguments;

		public static ExecuteProcessWindow CreateExecWindow(string fileName, out bool? result)
		{
			var window = new ExecuteProcessWindow {
				Owner = MainWindow.Instance
			};
			var fn = fileName ?? Instance.Executable;
			if (fn != null) {
				window.SelectedExecutable = fn;
				if (fileName == null && !string.IsNullOrEmpty(Instance.WorkingDirectory))
					window.WorkingDirectory = Instance.WorkingDirectory;
			}
			window.Arguments = Instance.Arguments ?? string.Empty;

			result = window.ShowDialog();
			if (result == true) {
				Instance.Executable = window.SelectedExecutable;
				Instance.WorkingDirectory = window.WorkingDirectory;
				Instance.Arguments = window.Arguments;
			}
			return window;
		}
	}
	
	[ExportContextMenuEntryAttribute(Header = "_Debug Assembly", Icon = "Images/application-x-executable.png")]
	internal sealed class DebugExecutableNodeCommand : DebuggerCommand, IContextMenuEntry
	{
		public DebugExecutableNodeCommand() : base(false)
		{
		}

		public string GetMenuHeader(TextViewContext context)
		{
			return string.Format("_Debug {0}", ((AssemblyTreeNode)context.SelectedTreeNodes[0]).LoadedAssembly.ShortName);
		}

		public bool IsVisible(TextViewContext context)
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
			if (DebuggerService.CurrentDebugger != null && !DebuggerService.CurrentDebugger.IsDebugging) {
				AssemblyTreeNode n = context.SelectedTreeNodes[0] as AssemblyTreeNode;
				
				if (DebuggerSettings.Instance.AskForArguments) {
					bool? result;
					var window = SavedDebuggedOptions.CreateExecWindow(n.LoadedAssembly.FileName, out result);
					if (result == true) {
						string fileName = window.SelectedExecutable;
						
						// execute the process
						DebuggerPlugin.StartExecutable(fileName, window.WorkingDirectory, window.Arguments);
					}
				} else {
					DebuggerPlugin.StartExecutable(n.LoadedAssembly.FileName, null, null);
				}
			}
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Debug an executable",
	                      ToolbarIcon = "Images/application-x-executable.png",
	                      ToolbarCategory = "Debugger",
	                      Tag = "Debugger",
	                      ToolbarOrder = 0)]
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/application-x-executable.png",
	                       MenuCategory = "Start",
	                       Header = "Debug an _executable",
	                       MenuOrder = 0)]
	internal sealed class DebugExecutableCommand : DebuggerCommand
	{
		public DebugExecutableCommand() : base(false)
		{
		}

		public override void Execute(object parameter)
		{
			if (DebuggerService.CurrentDebugger != null && !DebuggerService.CurrentDebugger.IsDebugging) {
				if (DebuggerSettings.Instance.AskForArguments)
				{
					bool? result;
					var window = SavedDebuggedOptions.CreateExecWindow(null, out result);
					if (result == true) {
						string fileName = window.SelectedExecutable;
						
						// add it to references
						MainWindow.Instance.OpenFiles(new [] { fileName }, false);
						
						// execute the process
						DebuggerPlugin.StartExecutable(fileName, window.WorkingDirectory, window.Arguments);
					}
				} else {
					OpenFileDialog dialog = new OpenFileDialog() {
						Filter = ".NET Executable (*.exe) | *.exe",
						RestoreDirectory = true,
						DefaultExt = "exe"
					};
					if (dialog.ShowDialog() == true) {
						string fileName = dialog.FileName;
						
						// add it to references
						MainWindow.Instance.OpenFiles(new [] { fileName }, false);
						
						// execute the process
						DebuggerPlugin.StartExecutable(fileName, null, null);
					}
				}
			}
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuCategory = "Start",
	                       Header = "Attach to _Process...",
	                       MenuOrder = 1)]
	internal sealed class AttachCommand : DebuggerCommand
	{
		public AttachCommand() : base(false)
		{
		}

		public override void Execute(object parameter)
		{
			if (DebuggerService.CurrentDebugger != null && !DebuggerService.CurrentDebugger.IsDebugging) {
				
				if (DebuggerSettings.Instance.ShowWarnings)
					MessageBox.Show("Warning: When attaching to an application, some local variables might not be available. If possible, use the \"Start Executable\" command.",
				                "Attach to a process", MessageBoxButton.OK, MessageBoxImage.Warning);
				
				var window = new AttachToProcessWindow { Owner = MainWindow.Instance };
				if (window.ShowDialog() == true) {
					DebuggerPlugin.StartAttaching(window.SelectedProcess);
				}
			}
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/ContinueDebugging.png",
	                       MenuCategory = "SteppingArea",
	                       Header = "_Continue",
	                       InputGestureText = "F5",
	                       MenuOrder = 2)]
	internal sealed class ContinueDebuggingCommand : DebuggerCommand
	{
		public ContinueDebuggingCommand() : base(true, true)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugContinue();
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug",
						   MenuIcon = "Images/Break.png",
						   MenuCategory = "SteppingArea",
						   Header = "Brea_k",
						   InputGestureText = "Ctrl+Break",
						   MenuOrder = 2.1)]
	internal sealed class BreakDebuggingCommand : DebuggerCommand
	{
		public BreakDebuggingCommand() : base(true, false)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugBreak();
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/StepInto.png",
	                       MenuCategory = "SteppingArea",
	                       Header = "Step _Into",
	                       InputGestureText = "F11",
	                       MenuOrder = 3)]
	internal sealed class StepIntoCommand : DebuggerCommand
	{
		public StepIntoCommand() : base(true, true)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugStepInto();
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/StepOver.png",
	                       MenuCategory = "SteppingArea",
	                       Header = "Step _Over",
	                       InputGestureText = "F10",
	                       MenuOrder = 4)]
	internal sealed class StepOverCommand : DebuggerCommand
	{
		public StepOverCommand() : base(true, true)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugStepOver();
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuIcon = "Images/StepOut.png",
	                       MenuCategory = "SteppingArea",
	                       Header = "Step Ou_t",
						   InputGestureText = "Shift+F11",
	                       MenuOrder = 5)]
	internal sealed class StepOutCommand : DebuggerCommand
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
	                       MenuCategory = "SteppingArea",
	                       Header = "Det_ach",
	                       MenuOrder = 6)]
	internal sealed class DetachCommand : DebuggerCommand
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
	                       Header = "_Delete all breakpoints",
						   InputGestureText = "Ctrl+Shift+F9",
	                       MenuOrder = 7.9)]
	internal sealed class RemoveBreakpointsCommand : DebuggerCommand
	{
		public RemoveBreakpointsCommand() : base(null)
		{
		}

		public override void Execute(object parameter)
		{
			DebuggerPlugin.DebugDeleteAllBreakpoints();
		}

		public override bool CanExecute(object parameter)
		{
			return base.CanExecute(parameter) &&
				DebuggerPlugin.DebugDeleteAllBreakpointsPossible();
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug",
	                       MenuCategory = "Breakpoints",
	                       Header = "To_ggle Breakpoint",
						   InputGestureText = "F9",
	                       MenuOrder = 7)]
	internal sealed class ToggleBreakpointCommand : DebuggerCommand
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
							Order = 1.0)]
	internal sealed class InsertBreakpointContextMenuEntry : IContextMenuEntry
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

		public string GetMenuHeader(TextViewContext context)
		{
			var location = MainWindow.Instance.TextView.TextEditor.TextArea.Caret.Location;
			var bpm = BreakpointHelper.GetBreakpointBookmark(location.Line, location.Column);
			return bpm == null ? "_Add Breakpoint" : "_Clear Breakpoint";
		}

		bool CanToggleBP()
		{
			var location = MainWindow.Instance.TextView.TextEditor.TextArea.Caret.Location;
			return BreakpointHelper.Find(location.Line, location.Column) != null;
		}
	}

	[ExportContextMenuEntry(Icon = "images/DisabledBreakpoint.png",
							InputGestureText = "Shift+F9",
							Category = "Debug",
							Order = 1.1)]
	internal sealed class DisableBreakpointContextMenuEntry : IContextMenuEntry
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

		public string GetMenuHeader(TextViewContext context)
		{
			var location = MainWindow.Instance.TextView.TextEditor.TextArea.Caret.Location;
			var bpm = BreakpointHelper.GetBreakpointBookmark(location.Line, location.Column);
			if (bpm == null)
				return null;
			return bpm.IsEnabled ? "Disable _Breakpoint" : "Enable _Breakpoint";
		}
	}

	[ExportContextMenuEntry(Header = "S_how Next Statement",
							InputGestureText = "Alt+Num *",
							Category = "Debug",
							Order = 2.0)]
	internal sealed class ShowNextStatementContextMenuEntry : IContextMenuEntry
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

		public string GetMenuHeader(TextViewContext context)
		{
			return null;
		}
	}

	[ExportContextMenuEntry(Header = "Set Ne_xt Statement",
							InputGestureText = "Ctrl+Shift+F10",
							Category = "Debug",
							Order = 3.0)]
	internal sealed class SetNextStatementContextMenuEntry : IContextMenuEntry
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

		public string GetMenuHeader(TextViewContext context)
		{
			return null;
		}
	}
}
