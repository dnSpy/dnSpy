using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	/// <summary>
	/// Interaction logic for SaveModuleOptions.xaml
	/// </summary>
	public partial class SaveModuleOptions : Window
	{
		public SaveModuleOptions()
		{
			InitializeComponent();
			SourceInitialized += (s, e) => this.HideMinimizeAndMaximizeButtons();
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			Close();
		}

		private void filenameButton_Click(object sender, RoutedEventArgs e)
		{
			var data = (SaveModuleOptionsVM)DataContext;
			var dialog = new SaveFileDialog() {
				Filter = ".NET Executable (*.exe, *.dll, *.netmodule)|*.exe;*.dll;*.netmodule|All files (*.*)|*.*",
				InitialDirectory = System.IO.Path.GetDirectoryName(filenameTextBox.Text),
				RestoreDirectory = true,
				DefaultExt = data.Extension,
			};

			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				filenameTextBox.Text = dialog.FileName;
		}
	}
}
