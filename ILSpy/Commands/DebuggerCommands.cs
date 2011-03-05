// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

using ILSpy.Debugger;
using ILSpy.Debugger.Bookmarks;
using ILSpy.Debugger.Services;
using ILSpy.Debugger.UI;
using Microsoft.Win32;

namespace ICSharpCode.ILSpy.Commands
{
	internal abstract class DebuggerCommands : SimpleCommand
	{
		public DebuggerCommands()
		{
			MainWindow.Instance.KeyUp += OnKeyUp;
		}

		void OnKeyUp(object sender, KeyEventArgs e)
		{
			switch (e.Key) {
				case Key.F5:
					if (this is ContinueDebuggingCommand) {
						((ContinueDebuggingCommand)this).Execute(null);
						e.Handled = true;
					}
					break;
				case Key.System:
					if (this is StepOverCommand) {
						((StepOverCommand)this).Execute(null);
						e.Handled = true;
					}
					break;
				case Key.F11:
					if (this is StepIntoCommand) {
						((StepIntoCommand)this).Execute(null);
						e.Handled = true;
					}
					break;
				default:
					// do nothing
					break;
			}
		}
		
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
		
		protected static IDebugger CurrentDebugger {
			get {
				return DebuggerService.CurrentDebugger;
			}
		}
		
		protected void StartDebugging(Process process)
		{
			CurrentDebugger.Attach(process);
			EnableDebuggerUI(false);
			CurrentDebugger.DebugStopped += OnDebugStopped;
			CurrentDebugger.IsProcessRunningChanged += CurrentDebugger_IsProcessRunningChanged;
		}
		
		protected void OnDebugStopped(object sender, EventArgs e)
		{
			EnableDebuggerUI(true);
			CurrentDebugger.DebugStopped -= OnDebugStopped;
			CurrentDebugger.IsProcessRunningChanged -= CurrentDebugger_IsProcessRunningChanged;
		}
		
		protected void EnableDebuggerUI(bool enable)
		{
			var menuItems = MainWindow.Instance.mainMenu.Items;
			var toolbarItems = MainWindow.Instance.toolBar.Items;
			
			// menu
			var items = menuItems.OfType<MenuItem>().Where(m => (m.Header as string) == "_Debugger");
			foreach (var item in items.First().Items.OfType<MenuItem>()) {
				string header = (string)item.Header;
				if (header.StartsWith("Attach") || header.StartsWith("Debug"))
					item.IsEnabled = enable;
				else
					item.IsEnabled = !enable;
			}
			
			//toolbar
			var buttons = toolbarItems.OfType<Button>().Where(b => (b.Tag as string) == "Debugger");
			foreach (var item in buttons) {
				item.IsEnabled = enable;
			}
		}
		
		void CurrentDebugger_IsProcessRunningChanged(object sender, EventArgs e)
		{
			if (CurrentDebugger.IsProcessRunning) {
				//SendWpfWindowPos(this, HWND_BOTTOM);
				return;
			}
			
			// breakpoint was hit => bring to front the main window
			SendWpfWindowPos(MainWindow.Instance, HWND_TOP);
			MainWindow.Instance.Activate();
			
			// jump to type & expand folding
			if (CurrentLineBookmark.Instance != null) {
				if (CurrentLineBookmark.Instance.Type != DebugData.CurrentType)
					MainWindow.Instance.JumpToReference(CurrentLineBookmark.Instance.Type);
				
				MainWindow.Instance.TextView.UnfoldAndScroll(CurrentLineBookmark.Instance.LineNumber);
			}
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Attach to running application",
	                      ToolbarIcon = "ILSpy.Debugger;component/Images/bug.png",
	                      ToolbarCategory = "Debugger",
	                      Tag = "Debugger",
	                      ToolbarOrder = 0)]
	[ExportMainMenuCommand(Menu = "_Debugger",
	                       MenuIcon = "ILSpy.Debugger;component/Images/bug.png",
	                       MenuCategory = "Debugger1",
	                       Header = "Attach to _running application",
	                       MenuOrder = 0)]
	internal sealed class AttachCommand : DebuggerCommands
	{
		public override void Execute(object parameter)
		{
			if (!CurrentDebugger.IsDebugging) {
				var window = new AttachToProcessWindow { Owner = MainWindow.Instance };
				if (window.ShowDialog() == true) {
					StartDebugging(window.SelectedProcess);
				}
			}
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debugger",
	                       MenuIcon = "ILSpy.Debugger;component/Images/ContinueDebugging.png",
	                       MenuCategory = "Debugger1",
	                       Header = "Continue debugging",
	                       MenuOrder = 1)]
	internal sealed class ContinueDebuggingCommand : DebuggerCommands
	{
		public override void Execute(object parameter)
		{
			if (CurrentDebugger.IsDebugging && !CurrentDebugger.IsProcessRunning)
				CurrentDebugger.Continue();
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debugger",
	                       MenuIcon = "ILSpy.Debugger;component/Images/StepInto.png",
	                       MenuCategory = "Debugger1",
	                       Header = "Step into",
	                       MenuOrder = 2)]
	internal sealed class StepIntoCommand : DebuggerCommands
	{
		public override void Execute(object parameter)
		{
			if (CurrentDebugger.IsDebugging && !CurrentDebugger.IsProcessRunning)
				CurrentDebugger.StepInto();
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debugger",
	                       MenuIcon = "ILSpy.Debugger;component/Images/StepOver.png",
	                       MenuCategory = "Debugger1",
	                       Header = "Step over",
	                       MenuOrder = 3)]
	internal sealed class StepOverCommand : DebuggerCommands
	{
		public override void Execute(object parameter)
		{
			if (CurrentDebugger.IsDebugging && !CurrentDebugger.IsProcessRunning)
				CurrentDebugger.StepOver();
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debugger",
	                       MenuIcon = "ILSpy.Debugger;component/Images/StepOut.png",
	                       MenuCategory = "Debugger1",
	                       Header = "Step out",
	                       MenuOrder = 4)]
	internal sealed class StepOutCommand : DebuggerCommands
	{
		public override void Execute(object parameter)
		{
			if (CurrentDebugger.IsDebugging && !CurrentDebugger.IsProcessRunning)
				CurrentDebugger.StepOut();
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debugger",
	                       MenuCategory = "Debugger1",
	                       Header = "_Detach from running application",
	                       MenuOrder = 5)]
	internal sealed class DetachCommand : DebuggerCommands
	{
		public override void Execute(object parameter)
		{
			if (CurrentDebugger.IsDebugging){
				CurrentDebugger.Detach();
				
				EnableDebuggerUI(true);
				CurrentDebugger.DebugStopped -= OnDebugStopped;
			}
		}
	}
	
	[ExportMainMenuCommand(Menu = "_Debugger",
	                       MenuIcon = "ILSpy.Debugger;component/Images/DeleteAllBreakpoints.png",
	                       MenuCategory = "Debugger2",
	                       Header = "Remove all _breakpoints",
	                       MenuOrder = 6)]
	internal sealed class RemoveBreakpointsCommand : DebuggerCommands
	{
		public override void Execute(object parameter)
		{
			for (int i = BookmarkManager.Bookmarks.Count - 1; i >= 0; --i) {
				var bookmark = BookmarkManager.Bookmarks[i];
				if (bookmark is BreakpointBookmark) {
					BookmarkManager.RemoveMark(bookmark);
				}
			}
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Debug an executable",
	                      ToolbarIcon = "ILSpy.Debugger;component/Images/application-x-executable.png",
	                      ToolbarCategory = "Debugger",
	                      Tag = "Debugger",
	                      ToolbarOrder = 0)]
	[ExportMainMenuCommand(Menu = "_Debugger",
	                       MenuIcon = "ILSpy.Debugger;component/Images/application-x-executable.png",
	                       MenuCategory = "Debugger3",
	                       Header = "Debug an _executable",
	                       MenuOrder = 7)]
	internal sealed class DebugExecutableCommand : DebuggerCommands
	{
		public override void Execute(object parameter)
		{
			OpenFileDialog dialog = new OpenFileDialog() {
				Filter = ".NET Executable (*.exe) | *.exe",
				RestoreDirectory = true,
				DefaultExt = "exe"
			};
			
			if (dialog.ShowDialog() == true) {
				string fileName = dialog.FileName;
				
				// add it to references
				MainWindow.Instance.OpenFiles(new [] { fileName }, false);
				
				if (!CurrentDebugger.IsDebugging) {
					// execute the process
					this.StartDebugging(Process.Start(fileName));
				}
			}
		}
	}
}
