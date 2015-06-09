// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32.SafeHandles;
using ICSharpCode.ILSpy.Controls;
using ICSharpCode.ILSpy.Debugger.Models;

namespace ICSharpCode.ILSpy.Debugger.UI
{
	/// <summary>
	/// Interaction logic for AttachToProcessWindow.xaml
	/// </summary>
	public partial class AttachToProcessWindow : MetroWindow
	{
		public AttachToProcessWindow()
		{
			InitializeComponent();
			Loaded += OnLoaded;
			if (IntPtr.Size == 4)
				this.Title += " (32-bit only)";
			else
				this.Title += " (64-bit only)";
		}
		
		public Process SelectedProcess {
			get {
				if (this.RunningProcesses.SelectedItem != null)
					return ((RunningProcess)this.RunningProcesses.SelectedItem).Process;
				
				return null;
			}
		}

		[DllImport("kernel32")]
		static extern SafeFileHandle OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
		const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
		const int SYNCHRONIZE = 0x00100000;
		const int PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFFF;

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool Wow64Process);

		void RefreshProcessList()
		{
			ObservableCollection<RunningProcess> list = new ObservableCollection<RunningProcess>();
			
			Process currentProcess = Process.GetCurrentProcess();
			foreach (Process process in Process.GetProcesses()) {
				try {
					// Prevent slow exceptions by filtering out processes we can't access
					using (var fh = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id)) {
						if (fh.IsInvalid)
							continue;
					}
					bool isWow64Process;
					if (IsWow64Process(process.Handle, out isWow64Process)) {
						if (IntPtr.Size == 4 && !isWow64Process)
							continue;
					}

					if (process.HasExited) continue;
					// Prevent attaching to our own process.
					if (currentProcess.Id != process.Id) {
						bool managed = false;
						try {
							var modules = process.Modules.Cast<ProcessModule>().Where(IsNetModule);
							managed = modules.Count() > 0;
						} catch { }
						
						if (managed) {
							list.Add(new RunningProcess {
							         	ProcessId = process.Id,
							         	ProcessName = Path.GetFileName(process.MainModule.FileName),
							         	FileName = process.MainModule.FileName,
							         	WindowTitle = process.MainWindowTitle,
							         	Managed = "Managed",
							         	Process = process
							         });
						}
					}
				} catch (Win32Exception) {
					// Do nothing.
				}
			}
			
			RunningProcesses.ItemsSource = list;
			if (list.Count > 0) {
				RunningProcesses.SelectedIndex = 0;
				RunningProcesses.Focus();
				// We must wait a little bit for it to create the ListViewItem
				Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => {
					var item = RunningProcesses.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
					if (item != null)
						item.Focus();
				}));
			}
		}

		static bool IsNetModule(ProcessModule m)
		{
			var s = m.ModuleName;
			if (s.Equals("mscorwks.dll", StringComparison.OrdinalIgnoreCase))
				return true;
			if (s.Equals("mscorsvr.dll", StringComparison.OrdinalIgnoreCase))
				return true;
			if (s.Equals("clr.dll", StringComparison.OrdinalIgnoreCase))
				return true;
			return false;
		}
		
		void Attach()
		{
			if (this.RunningProcesses.SelectedItem == null)
				return;
			
			this.DialogResult = true;
		}
		
		void OnLoaded(object sender, RoutedEventArgs e)
		{
			RefreshProcessList();
		}
		
		void AttachButton_Click(object sender, RoutedEventArgs e)
		{
			Attach();
		}
		
		void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
		
		void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshProcessList();
		}

		void RunningProcesses_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!UIUtils.IsLeftDoubleClick<ListViewItem>(RunningProcesses, e))
				return;
			Attach();
		}
	}
}