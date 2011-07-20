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
	/// <summary>
	/// Interaction logic for Create.xaml
	/// </summary>
	public partial class CreateListDialog : Window
	{
		public CreateListDialog()
		{
			InitializeComponent();
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			okButton.IsEnabled = !string.IsNullOrWhiteSpace(ListName.Text);
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(ListName.Text))
			{
				this.DialogResult = true;
			}
		}

		public string NewListName
		{
			get
			{
				return ListName.Text;
			}
		}

	}
}