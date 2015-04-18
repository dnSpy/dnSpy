// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace ICSharpCode.ILSpy.Debugger.UI
{
	/// <summary>
	/// Interaction logic for DebugProcessWindow.xaml
	/// </summary>
	public partial class DebugProcessWindow : Window
	{
		public DebugProcessWindow()
		{
			InitializeComponent();
		}
		
		public string SelectedExecutable {
			get {
				return pathTextBox.Text;
			}
			set {
				pathTextBox.Text = value;
				workingDirectoryTextBox.Text = Path.GetDirectoryName(value);
			}
		}
		
		public string WorkingDirectory {
			get {
				return workingDirectoryTextBox.Text;
			}
			set {
				workingDirectoryTextBox.Text = value;
			}
		}
		
		public string Arguments {
			get {
				return argumentsTextBox.Text;
			}
			set {
				argumentsTextBox.Text = value;
			}
		}

		public bool BreakAtBeginning {
			get {
				return breakAtStartChkBox.IsChecked.Value;
			}
			set {
				breakAtStartChkBox.IsChecked = value;
			}
		}
		
		void pathButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog() {
				Filter = ".NET Executables (*.exe) | *.exe",
				InitialDirectory = workingDirectoryTextBox.Text,
				RestoreDirectory = true,
				DefaultExt = "exe"
			};
			
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				SelectedExecutable = dialog.FileName;
			}
		}
		
		void DebugButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(SelectedExecutable))
				return;
			this.DialogResult = true;
		}
		
		void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
		
		void workingDirectoryButton_Click(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog dialog = new FolderBrowserDialog() {
				SelectedPath = workingDirectoryTextBox.Text
			};
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				workingDirectoryTextBox.Text = dialog.SelectedPath;
			}
		}
	}
}