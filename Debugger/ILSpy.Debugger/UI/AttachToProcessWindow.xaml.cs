// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

using ICSharpCode.ILSpy.Debugger.Models;

namespace ICSharpCode.ILSpy.Debugger.UI
{
	/// <summary>
	/// Interaction logic for AttachToProcessWindow.xaml
	/// </summary>
	public partial class AttachToProcessWindow : Window
	{
		public AttachToProcessWindow()
		{
			InitializeComponent();
			
			Loaded += OnLoaded;
		}
		
		public Process SelectedProcess {
			get {
				if (this.RunningProcesses.SelectedItem != null)
					return ((RunningProcess)this.RunningProcesses.SelectedItem).Process;
				
				return null;
			}
		}

		void RefreshProcessList()
		{
			ObservableCollection<RunningProcess> list = new ObservableCollection<RunningProcess>();
			
			Process currentProcess = Process.GetCurrentProcess();
			foreach (Process process in Process.GetProcesses()) {
				try {
					if (process.HasExited) continue;
					// Prevent attaching to our own process.
					if (currentProcess.Id != process.Id) {
						bool managed = false;
						try {
							var modules = process.Modules.Cast<ProcessModule>().Where(
								m => m.ModuleName.StartsWith("mscor", StringComparison.OrdinalIgnoreCase));
							
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
		
		void RunningProcesses_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Attach();
		}
	}
}