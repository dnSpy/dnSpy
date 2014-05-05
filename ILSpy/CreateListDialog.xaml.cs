using System;
using System.Windows;
using System.Windows.Controls;

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