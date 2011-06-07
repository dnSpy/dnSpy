/*
 * Created by SharpDevelop.
 * User: klier
 * Date: 05/13/2011
 * Time: 08:44
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace ICSharpCode.ILSpy.Debugger.UI
{
	/// <summary>
	/// Interaction logic for ExecuteProcessWindow.xaml
	/// </summary>
	public partial class ExecuteProcessWindow : Window
	{
		public ExecuteProcessWindow()
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
		}
		
		void pathButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog() {
				Filter = ".NET Executable (*.exe) | *.exe",
				InitialDirectory = workingDirectoryTextBox.Text,
				RestoreDirectory = true,
				DefaultExt = "exe"
			};
			
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				SelectedExecutable = dialog.FileName;
			}
		}
		
		void ExecuteButton_Click(object sender, RoutedEventArgs e)
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