// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

using ILSpy.Debugger.Models;
using ILSpy.Debugger.Services;

namespace ILSpy.Debugger.UI
{
	/// <summary>
	/// Interaction logic for AttachToProcessWindow.xaml
	/// </summary>
	public partial class AttachToProcessWindow : Window
	{
		public static IDebugger Debugger { get; private set; }
		
		static AttachToProcessWindow()
		{
			Debugger = new WindowsDebugger();
		}
		
		public AttachToProcessWindow()
		{
			InitializeComponent();
			
			Loaded += OnLoaded;
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
								m => m.ModuleName.StartsWith("mscor", StringComparison.InvariantCultureIgnoreCase));
							
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
			
			// start attaching
			var process = ((RunningProcess)this.RunningProcesses.SelectedItem).Process;
			Debugger.Attach(process);
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