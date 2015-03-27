using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ICSharpCode.ILSpy
{
	[Flags]
	public enum MsgBoxButton
	{
		None = 0,
		OK = 1,
		No = 2,
		Cancel = 4,
	}

	/// <summary>
	/// Interaction logic for MsgBox.xaml
	/// </summary>
	public partial class MsgBox : Window
	{
		public MsgBoxButton ButtonClicked { get; private set; }

		public MsgBox()
		{
			InitializeComponent();
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			this.ButtonClicked = MsgBoxButton.OK;
			Close();
		}

		private void noButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.ButtonClicked = MsgBoxButton.No;
			Close();
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.ButtonClicked = MsgBoxButton.Cancel;
			Close();
		}
	}
}
