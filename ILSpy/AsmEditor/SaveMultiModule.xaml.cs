using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace ICSharpCode.ILSpy.AsmEditor
{
	/// <summary>
	/// Interaction logic for SaveMultiModule.xaml
	/// </summary>
	public partial class SaveMultiModule : SaveModuleWindow
	{
		public SaveMultiModule()
		{
			InitializeComponent();
		}

		private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter is SaveModuleOptionsVM;
		}

		private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ShowOptions((SaveModuleOptionsVM)e.Parameter);
		}

		ListViewItem GetListViewItem(object o)
		{
			var depo = o as DependencyObject;
			while (depo != null && !(depo is ListViewItem) && depo != listView)
				depo = VisualTreeHelper.GetParent(depo);
			return depo as ListViewItem;
		}

		private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
				return;
			if (GetListViewItem(e.OriginalSource) == null)
				return;
			ShowOptions((SaveModuleOptionsVM)listView.SelectedItem);
		}
	}
}
